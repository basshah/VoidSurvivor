using System;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public static readonly List<Enemy> Active = new List<Enemy>(256);
    public static int ActiveCount => Active.Count;

    static Camera cachedMainCam;
    public static Camera MainCamera
    {
        get
        {
            if (cachedMainCam == null) cachedMainCam = Camera.main;
            return cachedMainCam;
        }
    }

    // Spatial hash grid for zero-GC O(N) soft separation (sized for 1.45–2.85 unit archetypes)
    const int GridHashSize = 512;
    const float GridCellSize = 3.2f;
    const float DespawnWrapDistance = 24f;
    const float WrapRingRadius = 13.8f;
    const int MaxSeparationNeighbors = 12;
    const float HitFeedbackDuration = 0.08f;
    static readonly Color HitFlashTint = new Color(1f, 0.35f, 0.35f, 1f);

    static readonly int[] cellHeads = new int[GridHashSize];
    static int[] nextInCell = new int[256];

    // Reusable query/sort buffers
    static readonly List<(Enemy enemy, float t)> segmentHitBuffer = new List<(Enemy enemy, float t)>(32);
    static readonly Comparison<(Enemy enemy, float t)> segmentHitComparison = (a, b) => a.t.CompareTo(b.t);

    static Transform trackedPlayer;
    static PlayerHealth trackedPlayerHealth;
    static float trackedPlayerRadius = 0.1f;

    public static void ClearAll()
    {
        for (int i = 0; i < Active.Count; i++)
        {
            if (Active[i] != null) Active[i].activeIndex = -1;
        }
        Active.Clear();
        cachedMainCam = null;
        trackedPlayer = null;
        trackedPlayerHealth = null;
        trackedPlayerRadius = 0.1f;
    }

    public static bool IsOnScreen(Vector3 worldPos, Camera cam, float margin = 0.02f)
    {
        if (cam == null) return true;
        Vector3 vp = cam.WorldToViewportPoint(worldPos);
        return vp.z > 0f && vp.x >= margin && vp.x <= 1f - margin && vp.y >= margin && vp.y <= 1f - margin;
    }

    public static Enemy Nearest(Vector2 from, float maxRange, bool onlyOnScreen = false)
    {
        Camera cam = onlyOnScreen ? MainCamera : null;
        Enemy best = null;
        float bestSqr = maxRange * maxRange;

        for (int i = 0; i < Active.Count; i++)
        {
            Enemy e = Active[i];
            if (e == null || e.dead) continue;
            if (onlyOnScreen && !IsOnScreen(e.Position, cam)) continue;

            float d = (e.Position - from).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = e; }
        }
        return best;
    }

    public static Enemy NearestUnvisited(Vector2 from, float maxRange, HashSet<Enemy> exclude, bool onlyOnScreen = true)
    {
        Camera cam = onlyOnScreen ? MainCamera : null;
        Enemy best = null;
        float bestSqr = maxRange * maxRange;

        for (int i = 0; i < Active.Count; i++)
        {
            Enemy e = Active[i];
            if (e == null || e.dead) continue;
            if (exclude != null && exclude.Contains(e)) continue;
            if (onlyOnScreen && !IsOnScreen(e.Position, cam)) continue;

            float d = (e.Position - from).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = e; }
        }
        return best;
    }

    public static List<Enemy> NearestMany(Vector2 from, float maxRange, int count, bool onlyOnScreen = true)
    {
        Camera cam = onlyOnScreen ? MainCamera : null;
        float maxSqr = maxRange * maxRange;
        var candidates = new List<(Enemy enemy, float sqrDist)>();

        for (int i = 0; i < Active.Count; i++)
        {
            Enemy e = Active[i];
            if (e == null || e.dead) continue;
            if (onlyOnScreen && !IsOnScreen(e.Position, cam)) continue;

            float d = (e.Position - from).sqrMagnitude;
            if (d <= maxSqr) candidates.Add((e, d));
        }

        candidates.Sort((a, b) => a.sqrDist.CompareTo(b.sqrDist));

        int take = Mathf.Min(count, candidates.Count);
        var result = new List<Enemy>(take);
        for (int i = 0; i < take; i++) result.Add(candidates[i].enemy);
        return result;
    }

    public static void QueryCircle(Vector2 center, float radius, List<Enemy> results)
    {
        results.Clear();
        for (int i = 0; i < Active.Count; i++)
        {
            Enemy e = Active[i];
            if (e == null || e.dead) continue;

            float r = radius + e.HitRadius;
            if ((e.Position - center).sqrMagnitude <= r * r)
            {
                results.Add(e);
            }
        }
    }

    public static void QueryOrientedBox(Vector2 origin, Vector2 dirNormalized, float length, float width, List<Enemy> results)
    {
        results.Clear();
        Vector2 perp = new Vector2(-dirNormalized.y, dirNormalized.x);
        float halfW = width * 0.5f;

        for (int i = 0; i < Active.Count; i++)
        {
            Enemy e = Active[i];
            if (e == null || e.dead) continue;

            Vector2 rel = e.Position - origin;
            float hr = e.HitRadius;
            float along = Vector2.Dot(rel, dirNormalized);
            if (along < -hr || along > length + hr) continue;

            float side = Mathf.Abs(Vector2.Dot(rel, perp));
            if (side <= halfW + hr)
            {
                results.Add(e);
            }
        }
    }

    public static void QuerySegmentFirstOrAll(Vector2 p0, Vector2 p1, float projectileRadius, List<Enemy> results)
    {
        results.Clear();
        Vector2 seg = p1 - p0;
        float segLenSqr = seg.sqrMagnitude;
        if (segLenSqr <= 0.000001f)
        {
            QueryCircle(p0, projectileRadius, results);
            return;
        }

        segmentHitBuffer.Clear();
        float invLenSqr = 1f / segLenSqr;

        for (int i = 0; i < Active.Count; i++)
        {
            Enemy e = Active[i];
            if (e == null || e.dead) continue;

            float t = Mathf.Clamp01(Vector2.Dot(e.Position - p0, seg) * invLenSqr);
            Vector2 closest = p0 + seg * t;
            float r = projectileRadius + e.HitRadius;
            if ((e.Position - closest).sqrMagnitude <= r * r)
            {
                segmentHitBuffer.Add((e, t));
            }
        }

        if (segmentHitBuffer.Count > 1)
        {
            segmentHitBuffer.Sort(segmentHitComparison);
        }

        for (int i = 0; i < segmentHitBuffer.Count; i++)
        {
            results.Add(segmentHitBuffer[i].enemy);
        }
        segmentHitBuffer.Clear();
    }

    public static void TickAll(float dt, Transform player)
    {
        int count = Active.Count;
        if (dt <= 0f || count == 0) return;

        float stepDt = Mathf.Min(dt, 0.05f);

        if (player != trackedPlayer)
        {
            trackedPlayer = player;
            trackedPlayerHealth = player != null ? player.GetComponent<PlayerHealth>() : null;
            var pCol = player != null ? player.GetComponent<CircleCollider2D>() : null;
            if (pCol != null)
            {
                float pScale = Mathf.Max(Mathf.Abs(player.lossyScale.x), Mathf.Abs(player.lossyScale.y));
                trackedPlayerRadius = pCol.radius * pScale;
            }
            else
            {
                trackedPlayerRadius = 0.1f;
            }
        }

        if (nextInCell.Length < count)
        {
            nextInCell = new int[Mathf.NextPowerOfTwo(count)];
        }

        for (int h = 0; h < GridHashSize; h++)
        {
            cellHeads[h] = -1;
        }

        const float invCell = 1f / GridCellSize;
        Vector2 playerPos = player != null ? (Vector2)player.position : Vector2.zero;
        const float wrapDistSqr = DespawnWrapDistance * DespawnWrapDistance;

        for (int i = 0; i < count; i++)
        {
            Enemy e = Active[i];
            if (e == null)
            {
                nextInCell[i] = -1;
                continue;
            }

            Vector2 ePos = e.Position;
            bool invalidPos = float.IsNaN(ePos.x) || float.IsNaN(ePos.y) || float.IsInfinity(ePos.x) || float.IsInfinity(ePos.y);
            if (invalidPos || (player != null && !e.dead && (ePos - playerPos).sqrMagnitude > wrapDistSqr))
            {
                float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                Vector2 wrapped = playerPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * WrapRingRadius;
                e.Position = wrapped;
                e.knockbackVel = Vector2.zero;
                e.transform.position = new Vector3(wrapped.x, wrapped.y, 0f);
            }

            if (e.flashTimer > 0f)
            {
                e.flashTimer = Mathf.Max(0f, e.flashTimer - stepDt);
                float t = e.flashTimer / HitFeedbackDuration;
                if (e.sr != null)
                {
                    e.sr.color = Color.Lerp(e.baseColor, HitFlashTint, t);
                }
                if (e.visualRoot != null && e.visualRoot != e.transform)
                {
                    float punch = Mathf.Lerp(1f, 1.12f, t);
                    e.visualRoot.localScale = e.baseVisualScale * punch;
                }
            }

            int cx = Mathf.FloorToInt(e.Position.x * invCell);
            int cy = Mathf.FloorToInt(e.Position.y * invCell);
            int hash = ((cx * 73856093) ^ (cy * 19349663)) & (GridHashSize - 1);
            nextInCell[i] = cellHeads[hash];
            cellHeads[hash] = i;
        }

        for (int i = 0; i < count; i++)
        {
            Enemy e = Active[i];
            if (e == null || e.dead || e.data == null) continue;

            Vector2 pos = e.Position;
            Vector2 moveVel = Vector2.zero;
            if (player != null)
            {
                Vector2 toPlayer = playerPos - pos;
                float distSqr = toPlayer.sqrMagnitude;
                if (distSqr > 0.000001f)
                {
                    moveVel = (toPlayer / Mathf.Sqrt(distSqr)) * e.data.moveSpeed;
                }
            }

            if (e.directionalVisual != null)
            {
                e.directionalVisual.TickVisual(moveVel, stepDt);
            }
            else if (e.sr != null && Mathf.Abs(moveVel.x) > 0.05f)
            {
                e.sr.flipX = moveVel.x < 0f;
            }

            int cx = Mathf.FloorToInt(pos.x * invCell);
            int cy = Mathf.FloorToInt(pos.y * invCell);
            Vector2 sep = Vector2.zero;
            int checkedNeighbors = 0;
            bool neighborCapReached = false;

            for (int dy = -1; dy <= 1 && !neighborCapReached; dy++)
            {
                for (int dx = -1; dx <= 1 && !neighborCapReached; dx++)
                {
                    int hash = (((cx + dx) * 73856093) ^ ((cy + dy) * 19349663)) & (GridHashSize - 1);
                    int j = cellHeads[hash];
                    while (j != -1)
                    {
                        if (j != i)
                        {
                            Enemy other = Active[j];
                            if (other != null && !other.dead)
                            {
                                checkedNeighbors++;
                                Vector2 diff = pos - other.Position;
                                float minDist = e.HitRadius + other.HitRadius;
                                float d2 = diff.sqrMagnitude;
                                if (d2 < minDist * minDist)
                                {
                                    if (d2 > 0.000001f)
                                    {
                                        float d = Mathf.Sqrt(d2);
                                        sep += (diff / d) * ((minDist - d) / minDist);
                                    }
                                    else
                                    {
                                        sep += (i > j) ? new Vector2(0.5f, 0.25f) : new Vector2(-0.5f, -0.25f);
                                    }
                                }

                                if (checkedNeighbors >= MaxSeparationNeighbors)
                                {
                                    neighborCapReached = true;
                                    break;
                                }
                            }
                        }
                        j = nextInCell[j];
                    }
                }
            }

            float speed = e.data.moveSpeed;
            Vector2 sepVel = sep * (speed * 1.4f);
            float maxSepSpeed = speed * 1.6f;
            if (sepVel.sqrMagnitude > maxSepSpeed * maxSepSpeed)
            {
                sepVel = sepVel.normalized * maxSepSpeed;
            }

            if (e.knockbackVel.sqrMagnitude > 0.0001f)
            {
                e.knockbackVel = Vector2.MoveTowards(e.knockbackVel, Vector2.zero, 28f * stepDt);
            }
            else
            {
                e.knockbackVel = Vector2.zero;
            }

            Vector2 finalVel = moveVel + sepVel + e.knockbackVel;
            if (finalVel.sqrMagnitude > 0.000001f)
            {
                pos += finalVel * stepDt;
                e.Position = pos;
                e.transform.position = new Vector3(pos.x, pos.y, 0f);
            }
            e.Velocity = finalVel;

            if (trackedPlayerHealth != null)
            {
                float contactDist = e.HitRadius + trackedPlayerRadius;
                if ((pos - playerPos).sqrMagnitude <= contactDist * contactDist)
                {
                    trackedPlayerHealth.TakeDamage(e.damage);
                }
            }
        }
    }

    public EnemyData data;
    public Color deathEffectColor = new Color(1f, 0.45f, 0.2f, 1f);
    [SerializeField] float baseHitRadius = 0.1f;

    public bool IsDead => dead;
    public Vector2 Position { get; private set; }
    public Vector2 Velocity { get; private set; }
    public float HitRadius { get; private set; }
    public GameObject SourcePrefab { get; set; }

    Transform visualRoot;
    SpriteRenderer sr;
    Animator anim;
    DirectionalVisual directionalVisual;
    Vector3 prefabVisualScale = Vector3.one;
    Vector3 baseVisualScale = Vector3.one;
    Color prefabBaseColor = Color.white;
    Color baseColor = Color.white;
    EnemySpawner ownerSpawner;
    Vector2 knockbackVel;
    float health;
    float damage;
    float flashTimer;
    bool dead;
    int activeIndex = -1;

    void Awake()
    {
        Transform foundVisual = transform.Find("VisualRoot");
        sr = foundVisual != null ? foundVisual.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();
        visualRoot = foundVisual != null ? foundVisual : (sr != null ? sr.transform : transform);
        anim = visualRoot != null ? visualRoot.GetComponent<Animator>() : null;
        directionalVisual = visualRoot != null ? visualRoot.GetComponent<DirectionalVisual>() : null;
        if (directionalVisual != null) directionalVisual.EnsureInitialized();

        if (visualRoot != null)
        {
            prefabVisualScale = visualRoot.localScale;
            baseVisualScale = prefabVisualScale;
        }
        if (sr != null)
        {
            prefabBaseColor = sr.color;
            baseColor = prefabBaseColor;
        }

        var col = GetComponent<CircleCollider2D>();
        if (col != null) baseHitRadius = col.radius;

        ApplyVisuals();
        RecalculateHitRadius();
        Position = transform.position;
    }

    void OnEnable()
    {
        if (activeIndex < 0)
        {
            activeIndex = Active.Count;
            Active.Add(this);
        }
    }

    void OnDisable()
    {
        if (activeIndex >= 0 && activeIndex < Active.Count && Active[activeIndex] == this)
        {
            int lastIdx = Active.Count - 1;
            Enemy last = Active[lastIdx];
            Active[activeIndex] = last;
            last.activeIndex = activeIndex;
            Active.RemoveAt(lastIdx);
        }
        activeIndex = -1;
    }

    void ApplyVisuals()
    {
        if (sr == null || data == null) return;

        bool hasDirectional = directionalVisual != null && directionalVisual.HasAllDirections;
        if (!hasDirectional && directionalVisual != null && data.directionalSprites != null && data.directionalSprites.Length >= 8)
        {
            directionalVisual.SetSprites(data.directionalSprites);
            hasDirectional = directionalVisual.HasAllDirections;
        }

        if (hasDirectional)
        {
            baseColor = prefabBaseColor;
            baseVisualScale = prefabVisualScale;
            EnsureUnlitSpriteMaterial();
            if (anim != null) anim.enabled = data.animatorController != null;
            directionalVisual.ResetVisual();
        }
        else if (data.customSprite != null)
        {
            sr.sprite = data.customSprite;
            baseColor = prefabBaseColor;
            baseVisualScale = prefabVisualScale;
            EnsureUnlitSpriteMaterial();

            if (data.animatorController != null)
            {
                if (anim == null && visualRoot != null)
                {
                    anim = visualRoot.gameObject.AddComponent<Animator>();
                }
                if (anim != null)
                {
                    anim.runtimeAnimatorController = data.animatorController;
                    anim.enabled = true;
                }
            }
            else if (anim != null)
            {
                anim.enabled = anim.runtimeAnimatorController != null;
            }
        }
        else
        {
            sr.sprite = EnemyVisuals.GetSprite(data.visualType);
            EnsureUnlitSpriteMaterial();

            baseColor = Color.white;
            baseVisualScale = Vector3.one * Mathf.Max(0.1f, data.visualSize);
            if (anim != null) anim.enabled = false;
        }

        sr.color = baseColor;
        if (!hasDirectional) sr.flipX = false;
        if (visualRoot != null && visualRoot != transform)
        {
            visualRoot.localScale = baseVisualScale;
        }
    }

    void EnsureUnlitSpriteMaterial()
    {
        if (sr.sharedMaterial == null || sr.sharedMaterial.shader == null ||
            sr.sharedMaterial.shader.name != "Universal Render Pipeline/2D/Sprite-Unlit-Default")
        {
            Material unlit = EnemyVisuals.GetUnlitMaterial();
            if (unlit != null) sr.sharedMaterial = unlit;
        }
    }

    void RecalculateHitRadius()
    {
        if (data != null)
        {
            HitRadius = Mathf.Max(0.05f, data.visualSize * data.hitRadiusRatio);
            return;
        }

        float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        HitRadius = Mathf.Max(0.02f, baseHitRadius * scale);
    }

    public void Init(Transform target, EnemyData enemyData, float hpMultiplier = 1f)
    {
        if (enemyData != null) data = enemyData;
        Init(target, hpMultiplier, 1f, null);
    }

    public void Init(Transform player, float hpMultiplier, float damageMultiplier, EnemySpawner spawner = null)
    {
        ownerSpawner = spawner;
        if (player != trackedPlayer)
        {
            trackedPlayer = player;
            trackedPlayerHealth = player != null ? player.GetComponent<PlayerHealth>() : null;
            var pCol = player != null ? player.GetComponent<CircleCollider2D>() : null;
            if (pCol != null)
            {
                float pScale = Mathf.Max(Mathf.Abs(player.lossyScale.x), Mathf.Abs(player.lossyScale.y));
                trackedPlayerRadius = pCol.radius * pScale;
            }
            else
            {
                trackedPlayerRadius = 0.1f;
            }
        }

        health = data != null ? data.maxHealth * hpMultiplier : 10f;
        damage = data != null ? data.contactDamage * damageMultiplier : 5f;
        dead = false;
        flashTimer = 0f;
        Velocity = Vector2.zero;
        knockbackVel = Vector2.zero;
        Position = transform.position;

        ApplyVisuals();
        RecalculateHitRadius();
    }

    public void ResetForPool()
    {
        dead = false;
        flashTimer = 0f;
        Velocity = Vector2.zero;
        knockbackVel = Vector2.zero;
        if (sr != null)
        {
            sr.color = baseColor;
            sr.flipX = false;
        }
        if (directionalVisual != null)
        {
            directionalVisual.ResetVisual();
        }
        if (visualRoot != null && visualRoot != transform)
        {
            visualRoot.localScale = baseVisualScale;
        }
        gameObject.SetActive(false);
    }

    public void ApplyKnockback(Vector2 dir, float force)
    {
        if (dead || force <= 0f) return;
        float res = data != null ? Mathf.Clamp01(data.knockbackResistance) : 0f;
        float effective = force * (1f - res);
        if (effective <= 0.01f) return;

        if (dir.sqrMagnitude > 0.0001f)
        {
            knockbackVel += dir.normalized * effective;
            if (knockbackVel.sqrMagnitude > 144f)
            {
                knockbackVel = knockbackVel.normalized * 12f;
            }
        }
    }

    public void TakeDamage(float amount, string spellId = null)
    {
        if (dead) return;

        float dealt = Mathf.Min(amount, health);
        health -= amount;
        if (!string.IsNullOrEmpty(spellId)) DamageTracker.Add(spellId, dealt);

        flashTimer = HitFeedbackDuration;
        if (sr != null) sr.color = HitFlashTint;
        if (visualRoot != null && visualRoot != transform)
        {
            visualRoot.localScale = baseVisualScale * 1.12f;
        }

        if (health <= 0f) Die();
    }

    void Die()
    {
        dead = true;
        Color vfxColor = data != null
            ? data.deathVfxColor
            : ((sr != null && baseColor != Color.white) ? baseColor : deathEffectColor);
        SimpleVFX.SpawnDeath(Position, vfxColor);

        if (GameManager.Instance != null)
            GameManager.Instance.AddKill();
        if (XPManager.Instance != null)
            XPManager.Instance.SpawnGem(Position, data != null ? data.xpValue : 1);

        if (ownerSpawner != null)
        {
            ownerSpawner.ReturnToPool(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}