using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PulsarSpell : Spell
{
    [Header("Beam Settings")]
    public float beamLength = 15f;
    public float baseBeamWidth = 0.78f;
    public float telegraphDuration = 0.04f;
    public float defaultBeamDuration = 0.16f;
    public float volleyStagger = 0.022f;
    public float minAngleGap = 6f;
    public float beamKnockback = 1.2f;

    [Header("Visuals")]
    public Color beamColor = new Color(0.62f, 0.48f, 1f, 0.95f);
    public Color evoBeamColor = new Color(0.95f, 0.45f, 1f, 0.98f);

    [Header("Optional Custom Assets")]
    public GameObject beamPrefab;
    public Sprite customBeamSprite;

    struct BeamInstance
    {
        public GameObject go;
        public SpriteRenderer sr;
        public bool isCustomPrefab;
    }

    readonly List<BeamInstance> activeBeams = new List<BeamInstance>(16);
    readonly Stack<BeamInstance> beamPool = new Stack<BeamInstance>(16);
    readonly List<float> pickedAngles = new List<float>(16);
    readonly List<Enemy> hitBuffer = new List<Enemy>(64);
    readonly Stack<HashSet<Enemy>> hitSetPool = new Stack<HashSet<Enemy>>(16);
    readonly List<HashSet<Enemy>> activeHitSets = new List<HashSet<Enemy>>(16);

    WaitForSeconds cachedWaitStagger;
    float cachedStaggerVal = -1f;
    WaitForSeconds cachedWaitVolleyEnd;
    float cachedVolleyEndVal = -1f;

    bool isFiring;
    bool skippedFirstCast;

    void OnDisable()
    {
        StopAllCoroutines();
        isFiring = false;
        skippedFirstCast = false;

        for (int i = 0; i < activeBeams.Count; i++)
        {
            ReturnBeamToPool(activeBeams[i]);
        }
        activeBeams.Clear();

        for (int i = 0; i < activeHitSets.Count; i++)
        {
            activeHitSets[i].Clear();
            hitSetPool.Push(activeHitSets[i]);
        }
        activeHitSets.Clear();
        hitBuffer.Clear();
    }

    void OnDestroy()
    {
        for (int i = 0; i < activeBeams.Count; i++)
        {
            if (activeBeams[i].go != null) Destroy(activeBeams[i].go);
        }
        activeBeams.Clear();

        while (beamPool.Count > 0)
        {
            var b = beamPool.Pop();
            if (b.go != null) Destroy(b.go);
        }
    }

    protected override bool TryFire()
    {
        if (isFiring || Time.timeScale <= 0f) return false;

        if (!skippedFirstCast)
        {
            skippedFirstCast = true;
            return true; // İlk dəfə cast etmir, normal cooldown-u başladır
        }

        StartCoroutine(FireBeamsRoutine(Current, Damage));
        return true;
    }

    IEnumerator FireBeamsRoutine(SpellLevelStats s, float dmg)
    {
        isFiring = true;

        int beamCount = Mathf.Max(1, s.count);
        float width = baseBeamWidth * Mathf.Max(0.4f, s.size);
        float duration = s.duration > 0.04f ? s.duration : defaultBeamDuration;
        bool evolved = IsEvolved;
        Color color = evolved ? evoBeamColor : beamColor;

        WaitForSeconds waitStagger = GetWaitStagger(volleyStagger);
        WaitForSeconds waitEnd = GetWaitVolleyEnd(telegraphDuration + duration);

        pickedAngles.Clear();
        for (int i = 0; i < beamCount; i++)
        {
            float angle = PickRandomAngle(pickedAngles, evolved);
            pickedAngles.Add(angle);

            StartCoroutine(RunBeamRoutine(angle, width, duration, dmg, color));

            if (evolved)
            {
                StartCoroutine(RunBeamRoutine(angle + 180f, width, duration, dmg, color));
            }

            if (waitStagger != null && i < beamCount - 1)
            {
                yield return waitStagger;
            }
        }

        yield return waitEnd;
        isFiring = false;
    }

    WaitForSeconds GetWaitStagger(float seconds)
    {
        if (seconds <= 0f) return null;
        if (cachedWaitStagger == null || !Mathf.Approximately(cachedStaggerVal, seconds))
        {
            cachedStaggerVal = seconds;
            cachedWaitStagger = new WaitForSeconds(seconds);
        }
        return cachedWaitStagger;
    }

    WaitForSeconds GetWaitVolleyEnd(float seconds)
    {
        if (cachedWaitVolleyEnd == null || !Mathf.Approximately(cachedVolleyEndVal, seconds))
        {
            cachedVolleyEndVal = seconds;
            cachedWaitVolleyEnd = new WaitForSeconds(seconds);
        }
        return cachedWaitVolleyEnd;
    }

    BeamInstance RentBeam()
    {
        while (beamPool.Count > 0)
        {
            BeamInstance pooled = beamPool.Pop();
            if (pooled.go == null) continue;
            pooled.go.SetActive(true);
            return pooled;
        }

        if (beamPrefab != null)
        {
            var customGo = Instantiate(beamPrefab);
            var customSr = customGo.GetComponentInChildren<SpriteRenderer>();
            if (customSr != null && customBeamSprite != null) customSr.sprite = customBeamSprite;
            return new BeamInstance { go = customGo, sr = customSr, isCustomPrefab = true };
        }

        var go = new GameObject("Pulsar_Beam");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = customBeamSprite != null ? customBeamSprite : SimpleVFX.GetBeamSprite();
        sr.sortingOrder = 12;
        Material mat = SimpleVFX.GetDefaultSpriteMaterial();
        if (mat != null) sr.sharedMaterial = mat;

        return new BeamInstance { go = go, sr = sr, isCustomPrefab = false };
    }

    void ReturnBeamToPool(BeamInstance beam)
    {
        if (beam.go == null) return;
        beam.go.SetActive(false);
        beamPool.Push(beam);
    }

    void RemoveActiveBeam(GameObject beamGo)
    {
        for (int i = 0; i < activeBeams.Count; i++)
        {
            if (activeBeams[i].go == beamGo)
            {
                int last = activeBeams.Count - 1;
                activeBeams[i] = activeBeams[last];
                activeBeams.RemoveAt(last);
                return;
            }
        }
    }

    HashSet<Enemy> RentHitSet()
    {
        HashSet<Enemy> set = hitSetPool.Count > 0 ? hitSetPool.Pop() : new HashSet<Enemy>();
        set.Clear();
        activeHitSets.Add(set);
        return set;
    }

    void ReturnHitSet(HashSet<Enemy> set)
    {
        if (set == null) return;
        activeHitSets.Remove(set);
        set.Clear();
        hitSetPool.Push(set);
    }

    float PickRandomAngle(List<float> existing, bool checkOpposite)
    {
        float chosen = Random.Range(0f, 360f);

        for (int attempt = 0; attempt < 10; attempt++)
        {
            float candidate = Random.Range(0f, 360f);
            bool tooClose = false;

            for (int i = 0; i < existing.Count; i++)
            {
                float diff = Mathf.Abs(Mathf.DeltaAngle(candidate, existing[i]));
                if (diff < minAngleGap)
                {
                    tooClose = true;
                    break;
                }
                if (checkOpposite && Mathf.Abs(Mathf.DeltaAngle(candidate, existing[i] + 180f)) < minAngleGap)
                {
                    tooClose = true;
                    break;
                }
            }

            chosen = candidate;
            if (!tooClose) break;
        }

        return chosen;
    }

    IEnumerator RunBeamRoutine(float angleDeg, float width, float duration, float dmg, Color color)
    {
        BeamInstance beam = RentBeam();
        activeBeams.Add(beam);
        GameObject beamGo = beam.go;
        SpriteRenderer sr = beam.sr;

        float rad = angleDeg * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        beamGo.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);

        float prepElapsed = 0f;
        while (prepElapsed < telegraphDuration)
        {
            prepElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(prepElapsed / telegraphDuration);

            beamGo.transform.position = transform.position;
            beamGo.transform.localScale = new Vector3(beamLength, width * 0.14f, 1f);
            if (sr != null && !beam.isCustomPrefab)
                sr.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0.2f, 0.65f, t));
            yield return null;
        }

        HashSet<Enemy> hitEnemies = RentHitSet();
        float elapsed = 0f;
        float damageWindow = duration * 0.75f;

        SimpleVFX.SpawnMuzzleFlash(transform.position, color, width * 0.75f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float widthFactor = t < 0.22f
                ? Mathf.Lerp(0.5f, 1.12f, t / 0.22f)
                : Mathf.Lerp(1.12f, 0.18f, (t - 0.22f) / 0.78f);

            float alpha = t < 0.55f ? color.a : Mathf.Lerp(color.a, 0f, (t - 0.55f) / 0.45f);

            Vector2 origin = transform.position;
            beamGo.transform.position = origin;
            beamGo.transform.localScale = new Vector3(beamLength, width * widthFactor, 1f);
            if (sr != null && !beam.isCustomPrefab)
                sr.color = new Color(color.r, color.g, color.b, alpha);

            if (elapsed <= damageWindow)
            {
                Enemy.QueryOrientedBox(origin, dir, beamLength, width * 0.92f, hitBuffer);
                for (int i = 0; i < hitBuffer.Count; i++)
                {
                    Enemy e = hitBuffer[i];
                    if (e == null || e.IsDead || hitEnemies.Contains(e)) continue;

                    hitEnemies.Add(e);
                    e.ApplyKnockback(dir, beamKnockback);
                    e.TakeDamage(dmg, SpellId);
                    SimpleVFX.SpawnHit(e.Position, color);
                }
                hitBuffer.Clear();
            }

            yield return null;
        }

        ReturnHitSet(hitEnemies);
        RemoveActiveBeam(beamGo);
        ReturnBeamToPool(beam);
    }
}