using System;
using UnityEngine;

public class XPManager : MonoBehaviour
{
    public static XPManager Instance { get; private set; }

    public Transform player;
    public GameObject gemPrefab;

    [Header("XP əyrisi: level L -> L+1 üçün lazım olan XP")]
    public int baseXP = 5;
    public int xpPerLevel = 4;

    public int CurrentXP { get; private set; }
    public int XPToNext => baseXP + (GameManager.Instance.Level - 1) * xpPerLevel;
    public float PickupRange => stats != null ? stats.pickupRange : 1.5f;

    public event Action<float> onXPChanged;   // 0..1
    public event Action<int> onLevelUp;       // yeni level

    PlayerStats stats;

    void Awake()
    {
        Instance = this;
        if (player != null) stats = player.GetComponent<PlayerStats>();
    }

    public void SpawnGem(Vector2 pos, int value)
    {
        if (gemPrefab == null) return;
        var go = Instantiate(gemPrefab, pos, gemPrefab.transform.rotation);
        go.GetComponent<XPGem>().value = value;
    }

    public void AddXP(int amount)
    {
        var gm = GameManager.Instance;
        CurrentXP += amount;
        while (CurrentXP >= XPToNext)
        {
            CurrentXP -= XPToNext;
            gm.Level++;
            onLevelUp?.Invoke(gm.Level);
        }
        onXPChanged?.Invoke((float)CurrentXP / XPToNext);
    }
}