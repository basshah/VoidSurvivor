using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpawnEntry
{
    public GameObject prefab;
    public float unlockTime;   // saniyə, bu vaxtdan sonra çıxmağa başlayır
}

public class EnemySpawner : MonoBehaviour
{
    public Transform player;
    public SpawnEntry[] entries;

    public float spawnDistance = 11f;
    public float startInterval = 1.2f;
    public float minInterval = 0.3f;
    public float intervalDecayPerMinute = 0.4f;
    public int maxAlive = 150;
    public float hpGrowthPerMinute = 0.3f;
    public float damageGrowthPerMinute = 0.15f;

    float timer;
    readonly List<SpawnEntry> available = new List<SpawnEntry>();

    void Update()
    {
        if (GameManager.Instance.IsGameOver) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        float t = GameManager.Instance.ElapsedTime;
        timer = Mathf.Max(minInterval, startInterval - t / 60f * intervalDecayPerMinute);

        int count = 1 + (int)(t / 40f);
        for (int i = 0; i < count; i++)
        {
            if (Enemy.AliveCount >= maxAlive) break;
            Spawn(t);
        }
    }

    void Spawn(float t)
    {
        available.Clear();
        foreach (var e in entries)
            if (e.prefab != null && e.unlockTime <= t) available.Add(e);
        if (available.Count == 0) return;

        SpawnEntry chosen = available[Random.Range(0, available.Count)];

        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnDistance;
        Vector2 pos = (Vector2)player.position + offset;

        GameObject go = Instantiate(chosen.prefab, pos, Quaternion.identity);
        float minutes = t / 60f;
        go.GetComponent<Enemy>().Init(player,
            1f + minutes * hpGrowthPerMinute,
            1f + minutes * damageGrowthPerMinute);
    }
}