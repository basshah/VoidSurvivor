using System.Collections.Generic;
using UnityEngine;

public class MoonletsSpell : Spell
{
    [Header("Orbit Settings")]
    public float baseOrbitRadius = 2.2f;
    public float baseOrbRadius = 0.38f;
    public float baseAngularSpeed = 155f;
    public float enemyHitCooldown = 0.32f;
    public float hitKnockback = 0.85f;

    [Header("Evolution (2 Counter-Rotating Orbits)")]
    public float evoInnerRadius = 1.65f;
    public float evoOuterRadius = 3.05f;
    public float evoOrbSizeMultiplier = 0.65f;
    public float evoInnerSpeedMultiplier = 1.2f;
    public float evoOuterSpeedMultiplier = 0.95f;

    [Header("Visuals")]
    public Color orbColor = new Color(0.55f, 0.90f, 1f, 1f);
    public Color evoInnerColor = new Color(1f, 0.85f, 0.35f, 1f);
    public Color evoOuterColor = new Color(0.65f, 0.95f, 1f, 1f);
    public float trailInterval = 0.06f;

    [Header("Optional Custom Assets")]
    public GameObject orbPrefab;
    public GameObject evoOrbPrefab;
    public Sprite customOrbSprite;

    readonly List<Transform> orbs = new List<Transform>();
    readonly List<Vector3> orbBaseScales = new List<Vector3>();
    readonly List<SpriteRenderer> orbRenderers = new List<SpriteRenderer>();
    readonly Dictionary<Enemy, float> lastHitTimes = new Dictionary<Enemy, float>();
    readonly List<Enemy> staleHitKeys = new List<Enemy>();
    readonly List<Enemy> hitBuffer = new List<Enemy>(64);

    float outerAngle;
    float innerAngle;
    float trailTimer;
    float spawnScale;
    bool builtForEvolved;
    bool usingCustomPrefab;

    void OnDisable()
    {
        spawnScale = 0f;
        ClearOrbs();
        lastHitTimes.Clear();
        staleHitKeys.Clear();
        hitBuffer.Clear();
    }

    protected override bool TryFire()
    {
        if (Time.timeScale <= 0f) return false;
        int targetCount = GetTargetOrbCount();
        if (orbs.Count != targetCount || builtForEvolved != IsEvolved)
        {
            EnsureOrbCount(targetCount);
        }
        return true;
    }

    int GetTargetOrbCount()
    {
        int count = Mathf.Max(1, Current.count);
        if (IsEvolved) count = Mathf.Max(2, count);
        return count;
    }

    void LateUpdate()
    {
        if (Level <= 0)
        {
            spawnScale = 0f;
            if (orbs.Count > 0) ClearOrbs();
            return;
        }

        // Donma / Level-Up ekranında kürələr yaranmasın və iz buraxmasın
        if (Time.timeScale <= 0f) return;

        SpellLevelStats s = Current;
        int total = GetTargetOrbCount();
        EnsureOrbCount(total);

        spawnScale = Mathf.MoveTowards(spawnScale, 1f, Time.deltaTime * 5f);

        float speedMult = s.speed > 0.1f ? s.speed : 1f;
        float sizeMult = Mathf.Max(0.4f, s.size) * spawnScale;

        trailTimer -= Time.deltaTime;
        bool emitTrail = trailTimer <= 0f && spawnScale > 0.5f;
        if (emitTrail) trailTimer = trailInterval;

        Vector3 center = transform.position;

        if (!IsEvolved)
        {
            outerAngle = (outerAngle + baseAngularSpeed * speedMult * Time.deltaTime) % 360f;
            float orbitRadius = baseOrbitRadius;
            float orbDiameter = baseOrbRadius * 2f * sizeMult;

            for (int i = 0; i < total; i++)
            {
                float rad = (outerAngle + i * (360f / total)) * Mathf.Deg2Rad;
                Vector3 pos = center + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * orbitRadius;

                orbs[i].position = pos;
                orbs[i].localScale = usingCustomPrefab ? orbBaseScales[i] * sizeMult : Vector3.one * orbDiameter;
                if (!usingCustomPrefab && orbRenderers[i] != null) orbRenderers[i].color = orbColor;

                if (emitTrail)
                {
                    SimpleVFX.SpawnBoltDot(pos, orbColor, orbDiameter * 0.36f, 0.14f);
                }
            }
        }
        else
        {
            innerAngle = (innerAngle - baseAngularSpeed * speedMult * evoInnerSpeedMultiplier * Time.deltaTime) % 360f;
            outerAngle = (outerAngle + baseAngularSpeed * speedMult * evoOuterSpeedMultiplier * Time.deltaTime) % 360f;

            int innerCount = total / 2;
            int outerCount = total - innerCount;
            float customScale = sizeMult * evoOrbSizeMultiplier;
            float orbDiameter = baseOrbRadius * 2f * customScale;

            for (int i = 0; i < innerCount; i++)
            {
                float rad = (innerAngle + i * (360f / innerCount)) * Mathf.Deg2Rad;
                Vector3 pos = center + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * evoInnerRadius;

                orbs[i].position = pos;
                orbs[i].localScale = usingCustomPrefab ? orbBaseScales[i] * customScale : Vector3.one * orbDiameter;
                if (!usingCustomPrefab && orbRenderers[i] != null) orbRenderers[i].color = evoInnerColor;

                if (emitTrail)
                {
                    SimpleVFX.SpawnBoltDot(pos, evoInnerColor, orbDiameter * 0.42f, 0.16f);
                }
            }

            for (int i = 0; i < outerCount; i++)
            {
                int idx = innerCount + i;
                float rad = (outerAngle + i * (360f / outerCount)) * Mathf.Deg2Rad;
                Vector3 pos = center + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * evoOuterRadius;

                orbs[idx].position = pos;
                orbs[idx].localScale = usingCustomPrefab ? orbBaseScales[idx] * customScale : Vector3.one * orbDiameter;
                if (!usingCustomPrefab && orbRenderers[idx] != null) orbRenderers[idx].color = evoOuterColor;

                if (emitTrail)
                {
                    SimpleVFX.SpawnBoltDot(pos, evoOuterColor, orbDiameter * 0.42f, 0.16f);
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (Level <= 0 || orbs.Count == 0 || spawnScale < 0.4f) return;

        SpellLevelStats s = Current;
        float sizeMult = Mathf.Max(0.4f, s.size);
        float radiusScale = IsEvolved ? sizeMult * evoOrbSizeMultiplier : sizeMult;
        float hitRadius = baseOrbRadius * radiusScale * 1.15f;
        float hitCd = IsEvolved ? enemyHitCooldown * 0.75f : enemyHitCooldown;
        float dmg = Damage;
        float now = Time.time;

        for (int i = 0; i < orbs.Count; i++)
        {
            Enemy.QueryCircle(orbs[i].position, hitRadius, hitBuffer);
            Color hitColor = orbRenderers[i] != null ? orbRenderers[i].color : (IsEvolved ? evoOuterColor : orbColor);

            for (int j = 0; j < hitBuffer.Count; j++)
            {
                Enemy e = hitBuffer[j];
                if (e == null || e.IsDead) continue;

                if (lastHitTimes.TryGetValue(e, out float lastTime) && now - lastTime < hitCd)
                    continue;

                lastHitTimes[e] = now;
                e.ApplyKnockback(e.Position - (Vector2)transform.position, hitKnockback);
                e.TakeDamage(dmg, SpellId);
                SimpleVFX.SpawnHit(e.Position, hitColor);
            }
        }
        hitBuffer.Clear();

        if (lastHitTimes.Count > 64)
        {
            PruneHitTimes(now, hitCd);
        }
    }

    void PruneHitTimes(float now, float hitCd)
    {
        staleHitKeys.Clear();
        foreach (var kvp in lastHitTimes)
        {
            if (kvp.Key == null || kvp.Key.IsDead || now - kvp.Value >= hitCd)
                staleHitKeys.Add(kvp.Key);
        }
        for (int i = 0; i < staleHitKeys.Count; i++)
            lastHitTimes.Remove(staleHitKeys[i]);
        staleHitKeys.Clear();
    }

    void EnsureOrbCount(int targetCount)
    {
        bool evolved = IsEvolved;
        if (orbs.Count == targetCount && builtForEvolved == evolved) return;

        ClearOrbs();
        builtForEvolved = evolved;

        GameObject prefabToUse = (evolved && evoOrbPrefab != null) ? evoOrbPrefab : orbPrefab;
        usingCustomPrefab = prefabToUse != null;

        if (usingCustomPrefab)
        {
            for (int i = 0; i < targetCount; i++)
            {
                var go = Instantiate(prefabToUse);
                orbs.Add(go.transform);
                orbBaseScales.Add(go.transform.localScale);
                orbRenderers.Add(go.GetComponentInChildren<SpriteRenderer>());
            }
            return;
        }

        Sprite orbSprite = customOrbSprite != null ? customOrbSprite : SimpleVFX.GetOrbSprite();
        Material defaultMat = SimpleVFX.GetDefaultSpriteMaterial();

        for (int i = 0; i < targetCount; i++)
        {
            var go = new GameObject($"Moonlet_{i}");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = orbSprite;
            sr.sortingOrder = 11;
            if (defaultMat != null) sr.sharedMaterial = defaultMat;

            orbs.Add(go.transform);
            orbBaseScales.Add(Vector3.one);
            orbRenderers.Add(sr);
        }
    }

    void ClearOrbs()
    {
        for (int i = 0; i < orbs.Count; i++)
        {
            if (orbs[i] != null) Destroy(orbs[i].gameObject);
        }
        orbs.Clear();
        orbBaseScales.Clear();
        orbRenderers.Clear();
    }
}