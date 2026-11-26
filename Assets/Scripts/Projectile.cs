using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public bool destroyOffScreen = true;
    public float minHitRadius = 0.22f;

    [Header("Combat Feel")]
    public float knockbackForce = 1.1f;

    [Header("Magic Bolt Visual")]
    public bool useProceduralBolt = true;
    public Color boltColor = Color.white;
    public float dotSpacing = 0.48f;        // Arxadakı nöqtələr arası məsafə
    public float dotSize = 0.24f;           // Nöqtələrin başlanğıc ölçüsü
    public float dotLifetime = 0.26f;       // Nöqtələrin sönmə müddəti
    public bool spawnMuzzleFlash = true;

    static readonly List<Enemy> sharedHits = new List<Enemy>(32);

    SpriteRenderer sr;
    Animator anim;
    Vector3 baseScale = Vector3.one;
    bool initialized;

    StarshotSpell ownerSpell;
    Vector2 velocity;
    float hitRadius;
    float damage;
    int hitsLeft;
    float lifeLeft;
    string spellId;
    bool consumed;
    Vector2 lastDotPos;
    float currentSize = 1f;

    readonly HashSet<Enemy> hitEnemies = new HashSet<Enemy>();

    void Awake()
    {
        EnsureInit();
    }

    void EnsureInit()
    {
        if (initialized) return;
        initialized = true;
        baseScale = transform.localScale;
        sr = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();
    }

    public void Launch(Vector2 dir, float speed, float damage, int pierce,
                       float lifetime, string spellId, float size, StarshotSpell owner = null)
    {
        EnsureInit();

        Vector2 direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;

        ownerSpell = owner;
        this.damage = damage;
        hitsLeft = Mathf.Max(1, pierce + 1);
        lifeLeft = lifetime;
        this.spellId = spellId;
        currentSize = Mathf.Max(0.2f, size);
        consumed = false;
        hitEnemies.Clear();

        if (useProceduralBolt && sr != null)
        {
            sr.sprite = SimpleVFX.GetBoltSprite();
            sr.color = boltColor;
            Material defaultMat = SimpleVFX.GetDefaultSpriteMaterial();
            if (defaultMat != null) sr.sharedMaterial = defaultMat;
            transform.localScale = Vector3.one * currentSize;
            if (anim != null) anim.enabled = false;
        }
        else
        {
            transform.localScale = baseScale * currentSize;
            if (anim != null)
            {
                if (anim.runtimeAnimatorController != null)
                {
                    anim.enabled = true;
                    anim.Play(0, -1, 0f);
                    anim.Update(0f);
                }
                else
                {
                    anim.enabled = false;
                }
            }
        }

        transform.right = new Vector3(direction.x, direction.y, 0f);
        velocity = direction * speed;

        float maxScale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        hitRadius = Mathf.Max(minHitRadius, 0.1f * maxScale);

        Vector2 startPos = transform.position;
        lastDotPos = startPos;

        if (spawnMuzzleFlash)
        {
            SimpleVFX.SpawnMuzzleFlash(transform.position + (Vector3)(direction * 0.25f), boltColor, 0.5f * currentSize);
        }

        Enemy.QueryCircle(startPos, hitRadius, sharedHits);
        for (int i = 0; i < sharedHits.Count; i++)
        {
            if (consumed) break;
            TryHitEnemy(sharedHits[i]);
        }
        sharedHits.Clear();
    }

    void Update()
    {
        if (consumed) return;

        float dt = Time.deltaTime;
        if (dt > 0f)
        {
            Vector2 prevPos = transform.position;
            Vector2 nextPos = prevPos + velocity * dt;
            transform.position = new Vector3(nextPos.x, nextPos.y, 0f);

            Enemy.QuerySegmentFirstOrAll(prevPos, nextPos, hitRadius, sharedHits);
            for (int i = 0; i < sharedHits.Count; i++)
            {
                if (consumed) break;
                TryHitEnemy(sharedHits[i]);
            }
            sharedHits.Clear();

            if (consumed) return;

            if (dotSpacing > 0.05f && (nextPos - lastDotPos).sqrMagnitude >= dotSpacing * dotSpacing)
            {
                Vector2 backOffset = -(Vector2)transform.right * (0.2f * currentSize);
                SimpleVFX.SpawnBoltDot(nextPos + backOffset, boltColor, dotSize * currentSize, dotLifetime);
                lastDotPos = nextPos;
            }

            lifeLeft -= dt;
            if (lifeLeft <= 0f)
            {
                Despawn();
                return;
            }
        }

        if (destroyOffScreen && !Enemy.IsOnScreen(transform.position, Enemy.MainCamera, -0.02f))
        {
            Despawn();
        }
    }

    void TryHitEnemy(Enemy e)
    {
        if (consumed || e == null || e.IsDead || hitEnemies.Contains(e)) return;

        hitEnemies.Add(e);
        SimpleVFX.SpawnHit(transform.position, boltColor);
        e.ApplyKnockback(velocity, knockbackForce);
        e.TakeDamage(damage, spellId);
        hitsLeft--;

        if (hitsLeft <= 0)
        {
            Despawn();
        }
    }

    void Despawn()
    {
        if (consumed) return;
        consumed = true;
        if (ownerSpell != null)
        {
            ownerSpell.ReturnProjectile(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}