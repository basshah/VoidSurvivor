using System.Collections.Generic;
using UnityEngine;

public class SimpleVFX : MonoBehaviour
{
    struct RingHandle
    {
        public GameObject go;
        public SpriteRenderer sr;
    }

    struct ActiveRing
    {
        public GameObject go;
        public SpriteRenderer sr;
        public float startRadius;
        public float endRadius;
        public float duration;
        public float elapsed;
        public Color color;
    }

    static SimpleVFX instance;
    static Sprite cachedBoltSprite;
    static Sprite cachedOrbSprite;
    static Sprite cachedRingSprite;
    static Sprite cachedBeamSprite;
    static Sprite cachedZoneSprite;
    static Sprite cachedSupernovaFillSprite;
    static Sprite cachedSupernovaRimSprite;
    static Texture2D cachedSoftCircleTex;
    static Material cachedSpriteMat;

    ParticleSystem sparkPs;
    ParticleSystem orbPs;
    Material sparkMat;
    Material orbMat;
    readonly List<ActiveRing> activeRings = new List<ActiveRing>();
    readonly Stack<RingHandle> ringPool = new Stack<RingHandle>(32);

    static Shader FindUnlitSpriteShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        return shader;
    }

    static SimpleVFX GetOrCreate()
    {
        if (instance != null) return instance;

        var go = new GameObject("SimpleVFX");
        instance = go.AddComponent<SimpleVFX>();
        instance.Setup();
        return instance;
    }

    public static Material GetDefaultSpriteMaterial()
    {
        if (cachedSpriteMat == null)
        {
            Shader shader = FindUnlitSpriteShader();
            if (shader != null) cachedSpriteMat = new Material(shader);
        }
        return cachedSpriteMat;
    }

    public static Sprite GetBoltSprite()
    {
        if (cachedBoltSprite != null) return cachedBoltSprite;

        const int w = 64;
        const int h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float ny = ((y + 0.5f) / h) * 2f - 1f;
            for (int x = 0; x < w; x++)
            {
                float nx = (((x + 0.5f) / w) * 2f - 1f) * 2f;
                float dx = Mathf.Max(0f, Mathf.Abs(nx) - 1.05f);
                float d = Mathf.Sqrt(dx * dx + ny * ny);

                float core = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.18f, 0.42f, d));
                float glow = Mathf.Pow(Mathf.Clamp01(1f - d / 0.95f), 2.4f) * 0.65f;
                pixels[y * w + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(core + glow));
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);

        cachedBoltSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 90f);
        return cachedBoltSprite;
    }

    public static Sprite GetOrbSprite()
    {
        if (cachedOrbSprite != null) return cachedOrbSprite;

        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            float ny = ((y + 0.5f) / size) * 2f - 1f;
            for (int x = 0; x < size; x++)
            {
                float nx = ((x + 0.5f) / size) * 2f - 1f;
                float d = Mathf.Sqrt(nx * nx + ny * ny);

                float core = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.32f, 0.52f, d));
                float glow = Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f) * 0.7f;
                pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(core + glow));
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);

        cachedOrbSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return cachedOrbSprite;
    }

    public static Sprite GetRingSprite()
    {
        if (cachedRingSprite != null) return cachedRingSprite;

        const int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            float ny = ((y + 0.5f) / size) * 2f - 1f;
            for (int x = 0; x < size; x++)
            {
                float nx = ((x + 0.5f) / size) * 2f - 1f;
                float d = Mathf.Sqrt(nx * nx + ny * ny);

                float ringDist = Mathf.Abs(d - 0.88f);
                float ring = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.02f, 0.10f, ringDist));
                float innerFill = d < 0.88f ? Mathf.Pow(Mathf.Clamp01(d / 0.88f), 4f) * 0.22f : 0f;
                pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(ring + innerFill));
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);

        cachedRingSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return cachedRingSprite;
    }

    public static Sprite GetBeamSprite()
    {
        if (cachedBeamSprite != null) return cachedBeamSprite;

        const int w = 64;
        const int h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float ay = Mathf.Abs(((y + 0.5f) / h) * 2f - 1f);
            float core = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.16f, 0.42f, ay));
            float outer = Mathf.Pow(Mathf.Clamp01(1f - ay), 1.8f) * 0.72f;
            float yAlpha = Mathf.Clamp01(core + outer);
            float rgb = Mathf.Lerp(0.82f, 1f, core);

            for (int x = 0; x < w; x++)
            {
                float nx = (x + 0.5f) / w;
                float startFade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.04f, nx));
                float endFade = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.88f, 1f, nx));
                float a = yAlpha * startFade * endFade;
                pixels[y * w + x] = new Color(rgb, rgb, rgb, a);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);

        cachedBeamSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0f, 0.5f), w);
        return cachedBeamSprite;
    }

    public static Sprite GetZoneSprite()
    {
        if (cachedZoneSprite != null) return cachedZoneSprite;

        const int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            float ny = ((y + 0.5f) / size) * 2f - 1f;
            for (int x = 0; x < size; x++)
            {
                float nx = ((x + 0.5f) / size) * 2f - 1f;
                float d = Mathf.Sqrt(nx * nx + ny * ny);

                float fill = d < 0.96f ? Mathf.Lerp(0.45f, 0.18f, d / 0.96f) : 0f;
                float rimDist = Mathf.Abs(d - 0.90f);
                float rim = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.01f, 0.09f, rimDist)) * 0.7f;
                float edgeMask = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.92f, 1f, d));

                pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01((fill + rim) * edgeMask));
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);

        cachedZoneSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return cachedZoneSprite;
    }

    // Supernova: təmiz, xətsiz, hamar ağ enerji sahəsi (həm partlayış, həm geridə qalan iz üçün)
    public static Sprite GetSupernovaFillSprite()
    {
        if (cachedSupernovaFillSprite != null) return cachedSupernovaFillSprite;

        const int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        const float edge = 0.90f;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            float ny = ((y + 0.5f) / size) * 2f - 1f;
            for (int x = 0; x < size; x++)
            {
                float nx = ((x + 0.5f) / size) * 2f - 1f;
                float d = Mathf.Sqrt(nx * nx + ny * ny);

                float norm = Mathf.Clamp01(d / edge);
                float insideMask = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(edge - 0.04f, edge + 0.03f, d));

                // Xətsiz, hamar radial parıltı + kənarda incə kontur
                float body = Mathf.Lerp(0.22f, 0.52f, Mathf.Pow(norm, 2.0f));
                float softRim = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.0f, 0.055f, Mathf.Abs(d - edge))) * 0.45f;
                float alpha = Mathf.Clamp01((body + softRim) * insideMask);

                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);

        cachedSupernovaFillSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size * edge);
        return cachedSupernovaFillSprite;
    }

    // Supernova: saf ağ, kəskin nüvəli və yumşaq parıltılı şok dalğası halqası
    public static Sprite GetSupernovaRimSprite()
    {
        if (cachedSupernovaRimSprite != null) return cachedSupernovaRimSprite;

        const int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        const float edge = 0.90f;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            float ny = ((y + 0.5f) / size) * 2f - 1f;
            for (int x = 0; x < size; x++)
            {
                float nx = ((x + 0.5f) / size) * 2f - 1f;
                float d = Mathf.Sqrt(nx * nx + ny * ny);
                float dist = Mathf.Abs(d - edge);

                float core = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.004f, 0.024f, dist));
                float glow = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.010f, 0.090f, dist)) * 0.65f;
                float alpha = Mathf.Clamp01(core + glow);

                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);

        cachedSupernovaRimSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size * edge);
        return cachedSupernovaRimSprite;
    }

    static Texture2D GetSoftCircleTexture()
    {
        if (cachedSoftCircleTex != null) return cachedSoftCircleTex;

        const int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            float ny = ((y + 0.5f) / size) * 2f - 1f;
            for (int x = 0; x < size; x++)
            {
                float nx = ((x + 0.5f) / size) * 2f - 1f;
                float d = Mathf.Sqrt(nx * nx + ny * ny);

                float core = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.25f, 0.55f, d));
                float glow = Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f) * 0.55f;
                pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(core + glow));
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);
        cachedSoftCircleTex = tex;
        return cachedSoftCircleTex;
    }

    void Setup()
    {
        sparkPs = CreateSystem("Sparks", Texture2D.whiteTexture, 15, 250, out sparkMat);
        orbPs = CreateSystem("Orbs", GetSoftCircleTexture(), 14, 400, out orbMat);
    }

    ParticleSystem CreateSystem(string childName, Texture2D tex, int sortingOrder, int maxParticles, out Material createdMat)
    {
        createdMat = null;
        var child = new GameObject(childName);
        child.transform.SetParent(transform, false);

        var ps = child.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = maxParticles;

        var emission = ps.emission;
        emission.enabled = false;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = grad;

        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sortingOrder = sortingOrder;

        Shader shader = FindUnlitSpriteShader();
        if (shader != null)
        {
            createdMat = new Material(shader);
            createdMat.mainTexture = tex;
            r.sharedMaterial = createdMat;
        }

        ps.Play();
        return ps;
    }

    void Update()
    {
        for (int i = activeRings.Count - 1; i >= 0; i--)
        {
            ActiveRing ring = activeRings[i];
            if (ring.go == null)
            {
                activeRings.RemoveAt(i);
                continue;
            }

            ring.elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(ring.elapsed / ring.duration);
            float eased = 1f - (1f - t) * (1f - t);

            float radius = Mathf.Lerp(ring.startRadius, ring.endRadius, eased);
            ring.go.transform.localScale = Vector3.one * (radius * 2f);

            Color c = ring.color;
            c.a = ring.color.a * (1f - t);
            ring.sr.color = c;

            if (t >= 1f)
            {
                ring.go.SetActive(false);
                ringPool.Push(new RingHandle { go = ring.go, sr = ring.sr });
                activeRings.RemoveAt(i);
            }
            else
            {
                activeRings[i] = ring;
            }
        }
    }

    void OnDestroy()
    {
        activeRings.Clear();
        ringPool.Clear();

        if (sparkMat != null)
        {
            Destroy(sparkMat);
            sparkMat = null;
        }
        if (orbMat != null)
        {
            Destroy(orbMat);
            orbMat = null;
        }
        if (instance == this)
        {
            if (cachedSpriteMat != null)
            {
                Destroy(cachedSpriteMat);
                cachedSpriteMat = null;
            }
            instance = null;
        }
    }

    public static void SpawnBoltDot(Vector3 pos, Color color, float size = 0.24f, float life = 0.24f)
    {
        var vfx = GetOrCreate();
        if (vfx.orbPs == null) return;

        var ep = new ParticleSystem.EmitParams
        {
            position = pos,
            velocity = Vector3.zero,
            startSize = size,
            startLifetime = life,
            startColor = color
        };
        vfx.orbPs.Emit(ep, 1);
    }

    public static void SpawnMuzzleFlash(Vector3 pos, Color color, float size = 0.55f)
    {
        var vfx = GetOrCreate();
        if (vfx.orbPs == null) return;

        var ep = new ParticleSystem.EmitParams
        {
            position = pos,
            velocity = Vector3.zero,
            startSize = size,
            startLifetime = 0.09f,
            startColor = color
        };
        vfx.orbPs.Emit(ep, 1);
    }

    public static void SpawnRing(Vector3 pos, float startRadius, float endRadius, float duration, Color color, int sortingOrder = 12)
    {
        var vfx = GetOrCreate();

        RingHandle handle = vfx.RentRing();
        handle.go.transform.position = pos;
        handle.go.transform.localScale = Vector3.one * (startRadius * 2f);

        handle.sr.color = color;
        handle.sr.sortingOrder = sortingOrder;

        vfx.activeRings.Add(new ActiveRing
        {
            go = handle.go,
            sr = handle.sr,
            startRadius = startRadius,
            endRadius = endRadius,
            duration = Mathf.Max(0.05f, duration),
            elapsed = 0f,
            color = color
        });
    }

    RingHandle RentRing()
    {
        while (ringPool.Count > 0)
        {
            RingHandle pooled = ringPool.Pop();
            if (pooled.go == null) continue;
            pooled.go.SetActive(true);
            return pooled;
        }

        var go = new GameObject("VFX_Ring");
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetRingSprite();
        Material mat = GetDefaultSpriteMaterial();
        if (mat != null) sr.sharedMaterial = mat;
        return new RingHandle { go = go, sr = sr };
    }

    public static void SpawnExplosion(Vector3 pos, Color color, float radius)
    {
        var vfx = GetOrCreate();
        float speedScale = Mathf.Max(0.9f, radius * 1.35f);

        vfx.EmitBurst(vfx.sparkPs, pos, color, count: 22, minSpeed: 2.0f * speedScale, maxSpeed: 4.5f * speedScale, minSize: 0.14f, maxSize: 0.30f, life: 0.38f);
        vfx.EmitBurst(vfx.orbPs, pos, Color.white, count: 1, minSpeed: 0f, maxSpeed: 0.1f, minSize: radius * 1.45f, maxSize: radius * 1.55f, life: 0.18f);
        vfx.EmitBurst(vfx.orbPs, pos, color, count: 8, minSpeed: 0.8f * speedScale, maxSpeed: 2.4f * speedScale, minSize: 0.30f, maxSize: 0.60f, life: 0.34f);
    }

    public static void SpawnHit(Vector3 pos, Color color)
    {
        var vfx = GetOrCreate();
        vfx.EmitBurst(vfx.sparkPs, pos, color, count: 4, minSpeed: 1.8f, maxSpeed: 3.6f, minSize: 0.08f, maxSize: 0.14f, life: 0.16f);
    }

    public static void SpawnDeath(Vector3 pos, Color color)
    {
        var vfx = GetOrCreate();
        vfx.EmitBurst(vfx.sparkPs, pos, color, count: 8, minSpeed: 2.2f, maxSpeed: 5.0f, minSize: 0.10f, maxSize: 0.22f, life: 0.24f);
        vfx.EmitBurst(vfx.orbPs, pos, Color.white, count: 1, minSpeed: 0f, maxSpeed: 0.2f, minSize: 0.35f, maxSize: 0.45f, life: 0.12f);
    }

    void EmitBurst(ParticleSystem targetPs, Vector3 pos, Color color, int count, float minSpeed, float maxSpeed, float minSize, float maxSize, float life)
    {
        if (targetPs == null) return;

        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float speed = Random.Range(minSpeed, maxSpeed);
            Vector3 vel = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * speed;

            var ep = new ParticleSystem.EmitParams
            {
                position = pos,
                velocity = vel,
                startSize = Random.Range(minSize, maxSize),
                startLifetime = life * Random.Range(0.75f, 1.15f),
                startColor = color,
                rotation = Random.Range(0f, 360f)
            };
            targetPs.Emit(ep, 1);
        }
    }
}