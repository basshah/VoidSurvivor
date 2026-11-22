using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    static readonly HashSet<Enemy> all = new HashSet<Enemy>();
    public static int AliveCount => all.Count;
    public static void ClearAll() { all.Clear(); }

    public static Enemy Nearest(Vector2 from, float maxRange)
    {
        Enemy best = null;
        float bestSqr = maxRange * maxRange;
        foreach (var e in all)
        {
            float d = ((Vector2)e.transform.position - from).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = e; }
        }
        return best;
    }

    public EnemyData data;

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