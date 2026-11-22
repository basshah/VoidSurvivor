using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    static readonly HashSet<Enemy> all = new HashSet<Enemy>();
    public static int AliveCount => all.Count;
    public static void ClearAll() { all.Clear(); }

    public static bool IsOnScreen(Vector3 worldPos, Camera cam, float margin = 0.02f)
    {
        if (cam == null) return true;
        Vector3 vp = cam.WorldToViewportPoint(worldPos);
        return vp.z > 0f && vp.x >= margin && vp.x <= 1f - margin && vp.y >= margin && vp.y <= 1f - margin;
    }

    public static Enemy Nearest(Vector2 from, float maxRange, bool onlyOnScreen = false)
    {
        Camera cam = onlyOnScreen ? Camera.main : null;
        Enemy best = null;
        float bestSqr = maxRange * maxRange;

        foreach (var e in all)
        {
            if (e == null || e.dead) continue;
            if (onlyOnScreen && !IsOnScreen(e.transform.position, cam)) continue;

            float d = ((Vector2)e.transform.position - from).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = e; }
        }
        return best;
    }

    public static Enemy NearestUnvisited(Vector2 from, float maxRange, HashSet<Enemy> exclude, bool onlyOnScreen = true)
    {
        Camera cam = onlyOnScreen ? Camera.main : null;
        Enemy best = null;
        float bestSqr = maxRange * maxRange;

        foreach (var e in all)
        {
            if (e == null || e.dead) continue;
            if (exclude != null && exclude.Contains(e)) continue;
            if (onlyOnScreen && !IsOnScreen(e.transform.position, cam)) continue;

            float d = ((Vector2)e.transform.position - from).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = e; }
        }
        return best;
    }

    public static List<Enemy> NearestMany(Vector2 from, float maxRange, int count, bool onlyOnScreen = true)
    {
        Camera cam = onlyOnScreen ? Camera.main : null;
        float maxSqr = maxRange * maxRange;
        var candidates = new List<(Enemy enemy, float sqrDist)>();

        foreach (var e in all)
        {
            if (e == null || e.dead) continue;
            if (onlyOnScreen && !IsOnScreen(e.transform.position, cam)) continue;

            float d = ((Vector2)e.transform.position - from).sqrMagnitude;
            if (d <= maxSqr) candidates.Add((e, d));
        }

        candidates.Sort((a, b) => a.sqrDist.CompareTo(b.sqrDist));

        int take = Mathf.Min(count, candidates.Count);
        var result = new List<Enemy>(take);
        for (int i = 0; i < take; i++) result.Add(candidates[i].enemy);
        return result;
    }

    public EnemyData data;
    public bool IsDead => dead;

    Rigidbody2D rb;
    SpriteRenderer sr;
    Color baseColor;
    Transform target;
    PlayerHealth playerHealth;
    float health;
    float damage;
    float flashTimer;
    bool dead;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
    }

    void OnEnable() { all.Add(this); }
    void OnDisable() { all.Remove(this); }

    public void Init(Transform player, float hpMultiplier, float damageMultiplier)
    {
        target = player;
        playerHealth = player.GetComponent<PlayerHealth>();
        health = data.maxHealth * hpMultiplier;
        damage = data.contactDamage * damageMultiplier;
        dead = false;
    }

    void Update()
    {
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f && sr != null) sr.color = baseColor;
        }
    }

    void FixedUpdate()
    {
        if (target == null) return;
        Vector2 dir = ((Vector2)target.position - rb.position).normalized;
        rb.linearVelocity = dir * data.moveSpeed;
    }

    void OnCollisionStay2D(Collision2D c)
    {
        if (playerHealth != null && c.collider.CompareTag("Player"))
            playerHealth.TakeDamage(damage);
    }

    public void TakeDamage(float amount, string spellId = null)
    {
        if (dead) return;

        float dealt = Mathf.Min(amount, health);
        health -= amount;
        if (!string.IsNullOrEmpty(spellId)) DamageTracker.Add(spellId, dealt);

        if (sr != null) { sr.color = Color.white; flashTimer = 0.06f; }
        if (health <= 0f) Die();
    }

    void Die()
    {
        dead = true;
        GameManager.Instance.AddKill();
        if (XPManager.Instance != null)
            XPManager.Instance.SpawnGem(transform.position, data.xpValue);
        Destroy(gameObject);
    }
}