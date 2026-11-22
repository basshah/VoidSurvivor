using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    public bool destroyOffScreen = true;
    public float minHitRadius = 0.22f;

    Rigidbody2D rb;
    Collider2D col;
    float damage;
    int hitsLeft;
    float lifeLeft;
    string spellId;
    bool consumed;
    Vector2 lastPos;

    readonly HashSet<Enemy> hitEnemies = new HashSet<Enemy>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    public void Launch(Vector2 dir, float speed, float damage, int pierce,
                       float lifetime, string spellId, float size)
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
        if (col == null) col = GetComponent<Collider2D>();

        Vector2 direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;

        this.damage = damage;
        hitsLeft = Mathf.Max(1, pierce + 1);
        lifeLeft = lifetime;
        this.spellId = spellId;
        consumed = false;
        hitEnemies.Clear();

        transform.localScale *= size;
        transform.right = new Vector3(direction.x, direction.y, 0f);
        rb.linearVelocity = direction * speed;

        lastPos = transform.position;

        // Personajın dibində / güllənin yarandığı yerdə olan düşmənləri dərhal vur
        CheckOverlapAt(lastPos);
    }

    void Update()
    {
        if (consumed) return;

        lifeLeft -= Time.deltaTime;
        if (lifeLeft <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (destroyOffScreen && !Enemy.IsOnScreen(transform.position, Camera.main, -0.02f))
        {
            Destroy(gameObject);
        }
    }

    void FixedUpdate()
    {
        if (consumed) return;

        Vector2 currentPos = rb.position;
        Vector2 delta = currentPos - lastPos;
        float dist = delta.magnitude;

        if (dist > 0.0001f)
        {
            float radius = GetHitRadius();
            RaycastHit2D[] hits = Physics2D.CircleCastAll(lastPos, radius, delta / dist, dist);
            if (hits.Length > 1)
            {
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            }

            for (int i = 0; i < hits.Length; i++)
            {
                if (consumed) break;
                TryHitCollider(hits[i].collider);
            }
        }
        else
        {
            CheckOverlapAt(currentPos);
        }

        lastPos = currentPos;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed) return;
        TryHitCollider(other);
    }

    void CheckOverlapAt(Vector2 point)
    {
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(point, GetHitRadius());
        for (int i = 0; i < overlaps.Length; i++)
        {
            if (consumed) break;
            TryHitCollider(overlaps[i]);
        }
    }

    void TryHitCollider(Collider2D other)
    {
        if (consumed || other == null) return;

        var e = other.GetComponentInParent<Enemy>();
        if (e == null || e.IsDead || hitEnemies.Contains(e)) return;

        hitEnemies.Add(e);
        e.TakeDamage(damage, spellId);
        hitsLeft--;

        if (hitsLeft <= 0)
        {
            consumed = true;
            Destroy(gameObject);
        }
    }

    float GetHitRadius()
    {
        if (col is CircleCollider2D circle)
        {
            float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
            return Mathf.Max(minHitRadius, circle.radius * scale);
        }
        if (col != null)
        {
            return Mathf.Max(minHitRadius, Mathf.Max(col.bounds.extents.x, col.bounds.extents.y));
        }
        return minHitRadius;
    }
}