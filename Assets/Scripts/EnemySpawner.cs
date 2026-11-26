using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpawnEntry
{
    public GameObject prefab;
    public float unlockTime;        // seconds when this archetype unlocks
    public float maxTime = -1f;     // optional time cutoff (-1 = no cutoff)
    public float weight = 10f;      // base selection weight
    public float lateWeight = -1f;  // optional weight after lateWeightTime (-1 = keep base weight)
    public float lateWeightTime = 360f;
    public bool includeInSwarmRush;
}

public class EnemySpawner : MonoBehaviour
{
    public Transform player;
    public SpawnEntry[] entries;

    [Header("Wave Pacing (10-Minute Progression)")]
    public float spawnDistance = 13.8f;
    public float startInterval = 1.00f;
    public float minInterval = 0.22f;
    public float intervalDecayPerMinute = 0.085f;
    public int maxAlive = 110;
    public float hpGraceSeconds = 45f;
    public float hpGrowthPerMinute = 0.22f;
    public float hpQuadraticGrowth = 0.11f;
    public float damageGrowthPerMinute = 0.12f;

    [Header("Swarm Rush Events")]
    public float swarmRushInterval = 120f;
    public float swarmRushDuration = 9f;
    public float swarmRushBurstInterval = 0.35f;
    public int swarmRushCountPerBurst = 2;

    float timer;
    float nextRushStartTime;
    float rushEndTime = -1f;
    float rushBurstTimer;

    readonly List<SpawnEntry> available = new List<SpawnEntry>(8);
    readonly List<SpawnEntry> rushAvailable = new List<SpawnEntry>(8);
    readonly Dictionary<GameObject, Stack<Enemy>> pools = new Dictionary<GameObject, Stack<Enemy>>();

    void Awake()
    {
        nextRushStartTime = swarmRushInterval;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.IsGameOver || player == null) return;

        float dt = Time.deltaTime;
        Enemy.TickAll(dt, player);

        float t = gm.ElapsedTime;

        UpdateSwarmRush(dt, t);

        timer -= dt;
        if (timer > 0f) return;

        timer = Mathf.Max(minInterval, startInterval - (t / 60f) * intervalDecayPerMinute);

        int count = t < 120f ? 1 : (t < 300f ? 2 : 3);
        for (int i = 0; i < count; i++)
        {
            if (Enemy.ActiveCount >= maxAlive) break;
            SpawnRegular(t);
        }
    }

    void UpdateSwarmRush(float dt, float t)
    {
        if (swarmRushInterval > 0f && t >= nextRushStartTime)
        {
            rushEndTime = nextRushStartTime + swarmRushDuration;
            nextRushStartTime += swarmRushInterval;
            rushBurstTimer = 0f;
        }

        if (t >= rushEndTime || Enemy.ActiveCount >= maxAlive) return;

        rushBurstTimer -= dt;
        if (rushBurstTimer > 0f) return;
        rushBurstTimer = Mathf.Max(0.20f, swarmRushBurstInterval);

        CollectAvailable(t, rushOnly: true, rushAvailable);
        List<SpawnEntry> poolSource = rushAvailable.Count > 0 ? rushAvailable : available;
        if (rushAvailable.Count == 0)
        {
            CollectAvailable(t, rushOnly: false, available);
            poolSource = available;
        }
        if (poolSource.Count == 0) return;

        int burstCount = Mathf.Clamp(swarmRushCountPerBurst, 1, 4);
        float baseAngle = Random.Range(0f, Mathf.PI * 2f);
        float step = (Mathf.PI * 2f) / burstCount;

        for (int i = 0; i < burstCount; i++)
        {
            if (Enemy.ActiveCount >= maxAlive) break;

            SpawnEntry chosen = PickWeighted(poolSource, t);
            if (chosen == null) break;

            float angle = baseAngle + i * step + Random.Range(-0.25f, 0.25f);
            Vector2 pos = (Vector2)player.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnDistance;
            SpawnAt(chosen.prefab, pos, t);
        }
    }

    void SpawnRegular(float t)
    {
        CollectAvailable(t, rushOnly: false, available);
        if (available.Count == 0) return;

        SpawnEntry chosen = PickWeighted(available, t);
        if (chosen == null) return;

        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector2 pos = (Vector2)player.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnDistance;
        SpawnAt(chosen.prefab, pos, t);
    }

    void CollectAvailable(float t, bool rushOnly, List<SpawnEntry> dst)
    {
        dst.Clear();
        if (entries == null) return;

        for (int i = 0; i < entries.Length; i++)
        {
            SpawnEntry e = entries[i];
            if (e == null || e.prefab == null) continue;
            if (e.unlockTime > t) continue;
            if (e.maxTime > 0f && t > e.maxTime) continue;
            if (rushOnly && !e.includeInSwarmRush) continue;
            if (GetEntryWeight(e, t) <= 0f) continue;

            dst.Add(e);
        }
    }

    static float GetEntryWeight(SpawnEntry e, float t)
    {
        if (e.lateWeight >= 0f && t >= e.lateWeightTime) return e.lateWeight;
        return e.weight;
    }

    static SpawnEntry PickWeighted(List<SpawnEntry> list, float t)
    {
        float totalWeight = 0f;
        for (int i = 0; i < list.Count; i++)
        {
            totalWeight += GetEntryWeight(list[i], t);
        }
        if (totalWeight <= 0f) return list[0];

        float roll = Random.Range(0f, totalWeight);
        float acc = 0f;
        for (int i = 0; i < list.Count; i++)
        {
            acc += GetEntryWeight(list[i], t);
            if (roll <= acc) return list[i];
        }
        return list[list.Count - 1];
    }

    void SpawnAt(GameObject prefab, Vector2 pos, float t)
    {
        Enemy enemy = GetFromPool(prefab, pos);
        if (enemy == null) return;

        float effMinutes = Mathf.Max(0f, (t - hpGraceSeconds) / 60f);
        float hpMult = 1f + (effMinutes * hpGrowthPerMinute) + (effMinutes * effMinutes * hpQuadraticGrowth);
        float dmgMult = 1f + effMinutes * damageGrowthPerMinute;
        enemy.Init(player, hpMult, dmgMult, this);
    }

    Enemy GetFromPool(GameObject prefab, Vector2 pos)
    {
        if (pools.TryGetValue(prefab, out var stack))
        {
            while (stack.Count > 0)
            {
                Enemy pooled = stack.Pop();
                if (pooled == null) continue;
                pooled.transform.SetPositionAndRotation(pos, Quaternion.identity);
                pooled.gameObject.SetActive(true);
                return pooled;
            }
        }

        GameObject go = Instantiate(prefab, pos, Quaternion.identity);
        Enemy enemy = go.GetComponent<Enemy>();
        if (enemy != null) enemy.SourcePrefab = prefab;
        return enemy;
    }

    public void ReturnToPool(Enemy enemy)
    {
        if (enemy == null) return;
        enemy.ResetForPool();

        if (enemy.SourcePrefab == null)
        {
            Destroy(enemy.gameObject);
            return;
        }

        if (!pools.TryGetValue(enemy.SourcePrefab, out var stack))
        {
            stack = new Stack<Enemy>(64);
            pools[enemy.SourcePrefab] = stack;
        }
        stack.Push(enemy);
    }
}