using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    float damage;
    int hitsLeft;
    float lifeLeft;
    string spellId;

    public void Launch(Vector2 dir, float speed, float damage, int pierce,
                       float lifetime, string spellId, float size)
    {
        this.damage = damage;
        this.hitsLeft = pierce + 1;
        this.lifeLeft = lifetime;
        this.spellId = spellId;
        transform.localScale *= size;
        transform.right = new Vector3(dir.x, dir.y, 0f);
        GetComponent<Rigidbody2D>().linearVelocity = dir * speed;
    }

    void Update()
    {
        lifeLeft -= Time.deltaTime;
        if (lifeLeft <= 0f) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hitsLeft <= 0) return;
        var e = other.GetComponentInParent<Enemy>();
        if (e == null) return;

        e.TakeDamage(damage, spellId);
        hitsLeft--;
        if (hitsLeft <= 0) Destroy(gameObject);
    }
}