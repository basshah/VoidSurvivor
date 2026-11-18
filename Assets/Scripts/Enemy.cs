using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    public static int AliveCount { get; private set; }

    public EnemyData data;

    Rigidbody2D rb;
    Transform target;
    PlayerHealth playerHealth;
    float health;
    float damage;

    void Awake() { rb = GetComponent<Rigidbody2D>(); }
    void OnEnable() { AliveCount++; }
    void OnDisable() { AliveCount--; }

    public void Init(Transform player, float hpMultiplier, float damageMultiplier)
    {
        target = player;
        playerHealth = player.GetComponent<PlayerHealth>();
        health = data.maxHealth * hpMultiplier;
        damage = data.contactDamage * damageMultiplier;
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

    public void TakeDamage(float amount)   // Gün 3-də sehrlər bunu çağıracaq
    {
        health -= amount;
        if (health <= 0f) Die();
    }

    void Die()
    {
        GameManager.Instance.AddKill();
        Destroy(gameObject);
    }
}