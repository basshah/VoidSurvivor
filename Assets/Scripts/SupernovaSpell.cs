using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SupernovaSpell : Spell
{
    [Header("Blast Settings")]
    public bool fitToScreen = true;
    public float screenCoverage = 0.88f;
    public float fallbackRadius = 6.5f;
    public bool requireEnemyInRange = true;
    public bool followPlayerWhileExpanding = true;

    [Header("Wave Timing")]
    public float expandDuration = 1.30f;
    public float fadeDuration = 0.55f;

    [Header("Sorting Orders")]
    public int fillSortingOrder = 1;
    public int rimSortingOrder = 11;

    [Header("Evolution (Full-Area Damage Zone + Tiny Flames)")]
    public float evoZoneDuration = 6.5f;
    public float evoTickInterval = 0.35f;
    public float evoTickDamageRatio = 0.22f;
    public float evoFlameSpawnInterval = 0.022f; // Çoxlu minik alov üçün sıx interval
    public int evoFlamesPerBurst = 3;

    [Header("Visuals (White Theme)")]
    public Color fillColor = new Color(0.95f, 0.98f, 1f, 0.58f);
    public Color rimColor = new Color(1f, 1f, 1f, 0.98f);
    public Color coreFlashColor = new Color(1f, 1f, 1f, 0.92f);
    public Color sparkColor = new Color(0.92f, 0.96f, 1f, 1f);
    public Color tinyFlameColor = new Color(1f, 0.98f, 0.92f, 0.95f);
    public float blastKnockback = 2.4f;

    [Header("Optional Custom Assets")]
    public Sprite customFillSprite;
    public Sprite customRimSprite;
    public GameObject customBlastPrefab;

    readonly List<Enemy> hitBuffer = new List<Enemy>(128);
    readonly HashSet<Enemy> waveHitEnemies = new HashSet<Enemy>();
    readonly HashSet<Enemy> tickHitEnemies = new HashSet<Enemy>();

    GameObject fillGo;
    GameObject rimGo;
    GameObject coreFlashGo;
    GameObject customBlastInstance;
    SpriteRenderer fillSr;
    SpriteRenderer rimSr;
    SpriteRenderer coreSr;

    bool isFiring;
    bool skippedFirstCast;

    void OnDisable()
    {
        StopAllCoroutines();
        isFiring = false;
        skippedFirstCast = false;
        HideAllVisuals();
        waveHitEnemies.Clear();
        tickHitEnemies.Clear();
        hitBuffer.Clear();
    }

    void OnDestroy()
    {
        if (fillGo != null) Destroy(fillGo);
        if (rimGo != null) Destroy(rimGo);
        if (coreFlashGo != null) Destroy(coreFlashGo);
        if (customBlastInstance != null) Destroy(customBlastInstance);
    }

    float ComputeBlastRadius(float sizeMult)
    {
        float baseRadius = fallbackRadius;
        Camera cam = Enemy.MainCamera;

        if (fitToScreen && cam != null && cam.orthographic)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            float halfDiag = Mathf.Sqrt(halfH * halfH + halfW * halfW);
            baseRadius = halfDiag * screenCoverage;
        }

        return baseRadius * Mathf.Max(0.5f, sizeMult);
    }

    protected override bool TryFire()
    {
        if (isFiring || Time.timeScale <= 0f) return false;

        if (!skippedFirstCast)
        {
            skippedFirstCast = true;
            return true; // İlk dəfə cast etmir, normal cooldown-u başladır
        }

        float radius = ComputeBlastRadius(Current.size);
        if (requireEnemyInRange && Enemy.Nearest(transform.position, radius * 1.15f, false) == null)
            return false;

        StartCoroutine(SupernovaWaveRoutine(Current, Damage, IsEvolved));
        return true;
    }

    void EnsureWaveVisuals()
    {
        if (customBlastPrefab != null)
        {
            if (customBlastInstance == null)
            {
                customBlastInstance = Instantiate(customBlastPrefab);
            }
            customBlastInstance.SetActive(true);
            return;
        }

        Material defaultMat = SimpleVFX.GetDefaultSpriteMaterial();

        if (fillGo == null)
        {
            Sprite fillSp = customFillSprite != null ? customFillSprite : SimpleVFX.GetSupernovaFillSprite();
            CreateVisual("Supernova_Fill", fillSp, fillColor, fillSortingOrder, defaultMat, out fillGo, out fillSr);
        }
        if (rimGo == null)
        {
            Sprite rimSp = customRimSprite != null ? customRimSprite : SimpleVFX.GetSupernovaRimSprite();
            CreateVisual("Supernova_Rim", rimSp, rimColor, rimSortingOrder, defaultMat, out rimGo, out rimSr);
        }
        if (coreFlashGo == null)
        {
            CreateVisual("Supernova_CoreFlash", SimpleVFX.GetOrbSprite(), coreFlashColor, rimSortingOrder + 1, defaultMat, out coreFlashGo, out coreSr);
        }

        fillSr.color = fillColor;
        rimSr.color = rimColor;
        coreSr.color = coreFlashColor;

        fillGo.SetActive(true);
        rimGo.SetActive(true);
        coreFlashGo.SetActive(true);
    }

    void HideAllVisuals()
    {
        if (fillGo != null) fillGo.SetActive(false);
        if (rimGo != null) rimGo.SetActive(false);
        if (coreFlashGo != null) coreFlashGo.SetActive(false);
        if (customBlastInstance != null) customBlastInstance.SetActive(false);
    }

    IEnumerator SupernovaWaveRoutine(SpellLevelStats s, float dmg, bool evolved)
    {
        isFiring = true;

        float maxRadius = ComputeBlastRadius(s.size);
        EnsureWaveVisuals();

        Vector2 origin = transform.position;
        SimpleVFX.SpawnExplosion(origin, sparkColor, Mathf.Min(maxRadius * 0.4f, 2.2f));

        // 1. Oyunçunun içindən genişlənən ağ supernova dalğası
        waveHitEnemies.Clear();
        float elapsed = 0f;
        float sparkTimer = 0f;

        while (elapsed < expandDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / expandDuration);
            float eased = 1f - Mathf.Pow(1f - t, 1.8f);
            float currentRadius = Mathf.Lerp(0.35f, maxRadius, eased);

            if (followPlayerWhileExpanding) origin = transform.position;

            if (customBlastInstance != null)
            {
                customBlastInstance.transform.position = origin;
                customBlastInstance.transform.localScale = Vector3.one * (currentRadius * 2f);
            }
            else
            {
                fillGo.transform.position = origin;
                fillGo.transform.localScale = Vector3.one * (currentRadius * 2f);

                rimGo.transform.position = origin;
                rimGo.transform.localScale = Vector3.one * (currentRadius * 2.01f);

                if (coreFlashGo != null)
                {
                    float flashT = Mathf.Clamp01(t / 0.30f);
                    coreFlashGo.transform.position = origin;
                    coreFlashGo.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, maxRadius * 0.55f, flashT);
                    coreSr.color = new Color(coreFlashColor.r, coreFlashColor.g, coreFlashColor.b, Mathf.Lerp(coreFlashColor.a, 0f, flashT * flashT));
                }

                float rimAlpha = t < 0.78f ? rimColor.a : Mathf.Lerp(rimColor.a, 0.25f, (t - 0.78f) / 0.22f);
                rimSr.color = new Color(rimColor.r, rimColor.g, rimColor.b, rimAlpha);
            }

            Enemy.QueryCircle(origin, currentRadius, hitBuffer);
            for (int i = 0; i < hitBuffer.Count; i++)
            {
                Enemy e = hitBuffer[i];
                if (e == null || e.IsDead || waveHitEnemies.Contains(e)) continue;

                waveHitEnemies.Add(e);
                e.ApplyKnockback(e.Position - origin, blastKnockback);
                e.TakeDamage(dmg, SpellId);
                SimpleVFX.SpawnHit(e.Position, sparkColor);
            }
            hitBuffer.Clear();

            sparkTimer -= Time.deltaTime;
            if (sparkTimer <= 0f && Time.deltaTime > 0f)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                Vector2 edgePos = origin + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * currentRadius;
                SimpleVFX.SpawnBoltDot(edgePos, sparkColor, Random.Range(0.20f, 0.36f), 0.22f);
                sparkTimer = 0.035f;
            }

            yield return null;
        }

        waveHitEnemies.Clear();
        if (coreFlashGo != null) coreFlashGo.SetActive(false);

        // Evo: bütün sahə (maxRadius) zərər verən zonaya çevrilir, minik alovlar vizual göstəricidir
        if (evolved)
        {
            float zoneDuration = s.duration > 0.5f ? s.duration : evoZoneDuration;
            StartCoroutine(FullZoneDamageWithTinyFlamesRoutine(origin, maxRadius, zoneDuration, dmg * evoTickDamageRatio));
        }

        // Əsas dalğa sönüb arxa fonu təmizləyir
        float fadeElapsed = 0f;
        float startRimA = rimSr != null ? rimSr.color.a : rimColor.a;
        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.deltaTime;
            float ft = Mathf.Clamp01(fadeElapsed / fadeDuration);

            if (customBlastInstance == null)
            {
                if (rimSr != null) rimSr.color = new Color(rimColor.r, rimColor.g, rimColor.b, Mathf.Lerp(startRimA, 0f, ft));
                if (fillSr != null) fillSr.color = new Color(fillColor.r, fillColor.g, fillColor.b, Mathf.Lerp(fillColor.a, 0f, ft));
            }
            yield return null;
        }

        if (rimGo != null) rimGo.SetActive(false);
        if (fillGo != null) fillGo.SetActive(false);
        if (customBlastInstance != null) customBlastInstance.SetActive(false);
        isFiring = false;
    }

    // Bütün sahə zərər vurur; çoxlu minik alovlar yalnız zərər zonasını vizual bildirmək üçündür
    IEnumerator FullZoneDamageWithTinyFlamesRoutine(Vector2 center, float radius, float duration, float tickDamage)
    {
        float elapsed = 0f;
        float tickTimer = evoTickInterval;
        float flameTimer = 0f;
        int burstCount = Mathf.Max(1, evoFlamesPerBurst);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            tickTimer -= Time.deltaTime;
            flameTimer -= Time.deltaTime;

            float fadeOut = elapsed > duration - 0.75f
                ? Mathf.Clamp01((duration - elapsed) / 0.75f)
                : 1f;

            // Vizual: bütün sahə boyunca çoxlu minik alov/qığılcım zərrəcikləri
            if (flameTimer <= 0f && Time.deltaTime > 0f && fadeOut > 0.05f)
            {
                for (int i = 0; i < burstCount; i++)
                {
                    // Sahənin hər yerində bərabər paylansın deyə sqrt(Random.value)
                    float rDist = Mathf.Sqrt(Random.value) * (radius * 0.96f);
                    float ang = Random.Range(0f, Mathf.PI * 2f);
                    Vector2 flamePos = center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rDist;

                    float flameSize = Random.Range(0.09f, 0.19f) * Mathf.Lerp(0.5f, 1f, fadeOut);
                    float flameLife = Random.Range(0.16f, 0.28f);
                    SimpleVFX.SpawnBoltDot(flamePos, tinyFlameColor, flameSize, flameLife);
                }

                flameTimer = evoFlameSpawnInterval;
            }

            // Mexanika: bütün sahə (radius) zərər vurur
            if (tickTimer <= 0f)
            {
                tickTimer = evoTickInterval;
                DealAreaDamage(center, radius, tickDamage, sparkColor);
            }

            yield return null;
        }
    }

    void DealAreaDamage(Vector2 pos, float radius, float dmg, Color hitColor)
    {
        Enemy.QueryCircle(pos, radius, hitBuffer);
        tickHitEnemies.Clear();

        for (int i = 0; i < hitBuffer.Count; i++)
        {
            Enemy e = hitBuffer[i];
            if (e == null || e.IsDead || tickHitEnemies.Contains(e)) continue;

            tickHitEnemies.Add(e);
            e.ApplyKnockback(e.Position - pos, blastKnockback * 0.2f);
            e.TakeDamage(dmg, SpellId);
            SimpleVFX.SpawnHit(e.Position, hitColor);
        }

        tickHitEnemies.Clear();
        hitBuffer.Clear();
    }

    void CreateVisual(string objName, Sprite sprite, Color color, int sortingOrder, Material mat, out GameObject go, out SpriteRenderer sr)
    {
        go = new GameObject(objName);
        sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        if (mat != null) sr.sharedMaterial = mat;
    }
}