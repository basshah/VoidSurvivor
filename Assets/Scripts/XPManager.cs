using System;
using System.Collections.Generic;
using UnityEngine;

public class XPManager : MonoBehaviour
{
    public static XPManager Instance { get; private set; }

    public Transform player;
    public GameObject gemPrefab;

    [Header("XP əyrisi: level L -> L+1 üçün lazım olan XP")]
    public int baseXP = 6;
    public int xpPerLevel = 6;

    [Header("Pooling & Global Magnet")]
    public int maxActiveGems = 220;
    [SerializeField] int globalMagnetEveryNLevels = 5;

    public int CurrentXP { get; private set; }
    public int XPToNext => baseXP + ((GameManager.Instance != null ? GameManager.Instance.Level : 1) - 1) * xpPerLevel;
    public float PickupRange => stats != null ? stats.pickupRange : 1.5f;

    public event Action<float> onXPChanged;   // 0..1
    public event Action<int> onLevelUp;       // yeni level

    PlayerStats stats;
    bool pendingGlobalSweep;
    readonly Stack<XPGem> gemPool = new Stack<XPGem>(256);
    readonly List<XPGem> activeGems = new List<XPGem>(256);

    void Awake()
    {
        Instance = this;
        if (player != null) stats = player.GetComponent<PlayerStats>();
    }

    public void SpawnGem(Vector2 pos, int value)
    {
        if (gemPrefab == null) return;

        if (activeGems.Count >= maxActiveGems)
        {
            XPGem mergeTarget = FindMergeTarget();
            if (mergeTarget != null)
            {
                mergeTarget.value += value;
                return;
            }
        }

        XPGem gem = GetGemFromPool(pos);
        if (gem == null) return;

        gem.ResetForPool(pos, value);
        gem.activeIndex = activeGems.Count;
        activeGems.Add(gem);
    }

    XPGem FindMergeTarget()
    {
        Vector2 playerPos = player != null ? (Vector2)player.position : Vector2.zero;
        XPGem best = null;
        float bestSqr = -1f;

        for (int i = 0; i < activeGems.Count; i++)
        {
            XPGem g = activeGems[i];
            if (g == null || g.attracted || g.globalAttracted) continue;

            float d2 = (g.position - playerPos).sqrMagnitude;
            if (d2 > bestSqr)
            {
                bestSqr = d2;
                best = g;
            }
        }

        return best != null ? best : (activeGems.Count > 0 ? activeGems[0] : null);
    }

    XPGem GetGemFromPool(Vector2 pos)
    {
        while (gemPool.Count > 0)
        {
            XPGem pooled = gemPool.Pop();
            if (pooled == null) continue;
            pooled.transform.SetPositionAndRotation(pos, gemPrefab.transform.rotation);
            pooled.gameObject.SetActive(true);
            return pooled;
        }

        var go = Instantiate(gemPrefab, pos, gemPrefab.transform.rotation);
        return go.GetComponent<XPGem>();
    }

    public void AttractAllGems()
    {
        if (player == null) return;

        for (int i = 0; i < activeGems.Count; i++)
        {
            XPGem g = activeGems[i];
            if (g == null) continue;
            g.globalAttracted = true;
            g.attracted = true;
            if (g.speed < g.globalPullSpeed) g.speed = g.globalPullSpeed;
        }

        SimpleVFX.SpawnRing(player.position, 0.5f, 4.5f, 0.45f, new Color(0.35f, 0.9f, 1f, 0.7f), 11);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f || Time.timeScale <= 0f || player == null) return;

        if (pendingGlobalSweep)
        {
            pendingGlobalSweep = false;
            AttractAllGems();
        }

        if (activeGems.Count == 0) return;

        Vector2 playerPos = player.position;
        float pickupRangeSqr = PickupRange * PickupRange;

        for (int i = activeGems.Count - 1; i >= 0; i--)
        {
            XPGem gem = activeGems[i];
            if (gem == null)
            {
                RemoveActiveAt(i);
                continue;
            }

            Vector2 to = playerPos - gem.position;
            float sqr = to.sqrMagnitude;

            if (!gem.attracted && !gem.globalAttracted)
            {
                if (sqr > pickupRangeSqr) continue;
                gem.attracted = true;
            }

            float collectDist = gem.collectDistance;
            if (sqr <= collectDist * collectDist)
            {
                int gained = gem.value;
                RemoveActiveAt(i);
                gem.gameObject.SetActive(false);
                gemPool.Push(gem);
                AddXP(gained);
                continue;
            }

            if (gem.globalAttracted)
            {
                gem.speed = Mathf.Min(Mathf.Max(gem.speed, gem.globalPullSpeed) + 45f * dt, 28f);
            }
            else
            {
                gem.speed = Mathf.Min(gem.speed + 40f * dt, 20f);
            }

            Vector2 dir = sqr > 0.000001f ? to / Mathf.Sqrt(sqr) : Vector2.zero;
            gem.position += dir * (gem.speed * dt);
            gem.transform.position = new Vector3(gem.position.x, gem.position.y, 0f);
        }
    }

    void RemoveActiveAt(int index)
    {
        int lastIdx = activeGems.Count - 1;
        XPGem last = activeGems[lastIdx];
        activeGems[index] = last;
        if (last != null) last.activeIndex = index;
        activeGems.RemoveAt(lastIdx);
    }

    public void AddXP(int amount)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        CurrentXP += amount;
        while (CurrentXP >= XPToNext)
        {
            CurrentXP -= XPToNext;
            gm.Level++;
            if (globalMagnetEveryNLevels > 0 && gm.Level > 0 && gm.Level % globalMagnetEveryNLevels == 0)
            {
                pendingGlobalSweep = true;
            }
            onLevelUp?.Invoke(gm.Level);
        }
        onXPChanged?.Invoke((float)CurrentXP / XPToNext);
    }
}