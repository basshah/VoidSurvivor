using UnityEngine;

/// <summary>
/// Generates and caches crisp pixel-art shadow-silhouette fallback sprites for the 4 enemy archetypes.
/// </summary>
public static class EnemyVisuals
{
    static readonly Sprite[] cachedSprites = new Sprite[4];
    static Material cachedUnlitMaterial;

    public static Material GetUnlitMaterial()
    {
        if (cachedUnlitMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader != null) cachedUnlitMaterial = new Material(shader);
        }
        return cachedUnlitMaterial;
    }

    public static Sprite GetSprite(EnemyVisualType type)
    {
        int idx = (int)type;
        if (idx < 0 || idx >= cachedSprites.Length) idx = 0;
        if (cachedSprites[idx] != null) return cachedSprites[idx];

        Sprite sprite = type switch
        {
            EnemyVisualType.Swarmer => BuildSwarmerSprite(),
            EnemyVisualType.Stalker => BuildStalkerSprite(),
            EnemyVisualType.Charger => BuildChargerSprite(),
            EnemyVisualType.Juggernaut => BuildJuggernautSprite(),
            _ => BuildStalkerSprite()
        };

        cachedSprites[idx] = sprite;
        return sprite;
    }

    static Sprite CreatePointSprite(int size, Color32[] pixels)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    static void SetPixel(Color32[] pixels, int size, int x, int y, Color32 c)
    {
        if (x < 0 || x >= size || y < 0 || y >= size) return;
        pixels[y * size + x] = c;
    }

    static void FillRect(Color32[] pixels, int size, int x0, int y0, int x1, int y1, Color32 c)
    {
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                SetPixel(pixels, size, x, y, c);
    }

    static Sprite BuildSwarmerSprite()
    {
        // 16x16 spiky winged shadow silhouette with bright crimson eye
        const int s = 16;
        var p = new Color32[s * s];
        Color32 body = new Color32(18, 14, 24, 255);
        Color32 rim = new Color32(54, 22, 38, 255);
        Color32 eyeGlow = new Color32(255, 36, 66, 255);
        Color32 eyeCore = new Color32(255, 225, 230, 255);

        // Central spiky body
        FillRect(p, s, 6, 4, 9, 11, body);
        FillRect(p, s, 5, 5, 10, 10, body);
        // Sharp bat wings
        FillRect(p, s, 1, 8, 5, 10, body);
        FillRect(p, s, 10, 8, 14, 10, body);
        FillRect(p, s, 2, 11, 4, 12, rim);
        FillRect(p, s, 11, 11, 13, 12, rim);
        SetPixel(p, s, 1, 12, rim);
        SetPixel(p, s, 14, 12, rim);
        // Bottom talons / tail spikes
        SetPixel(p, s, 5, 3, rim);
        SetPixel(p, s, 7, 2, body);
        SetPixel(p, s, 8, 2, body);
        SetPixel(p, s, 10, 3, rim);
        // Top horns
        SetPixel(p, s, 6, 12, rim);
        SetPixel(p, s, 9, 12, rim);
        // Bright crimson eye
        FillRect(p, s, 7, 7, 8, 8, eyeGlow);
        SetPixel(p, s, 8, 8, eyeCore);

        return CreatePointSprite(s, p);
    }

    static Sprite BuildStalkerSprite()
    {
        // 24x24 tall hooded/void silhouette with 2 glowing crimson-white eyes
        const int s = 24;
        var p = new Color32[s * s];
        Color32 body = new Color32(16, 14, 22, 255);
        Color32 cloak = new Color32(34, 26, 48, 255);
        Color32 eyeOuter = new Color32(255, 48, 78, 255);
        Color32 eyeInner = new Color32(255, 235, 240, 255);

        // Hooded head
        FillRect(p, s, 8, 14, 15, 20, body);
        FillRect(p, s, 9, 21, 14, 21, cloak);
        FillRect(p, s, 10, 22, 13, 22, cloak);
        // Cloaked torso & shoulders
        FillRect(p, s, 6, 6, 17, 14, body);
        FillRect(p, s, 5, 8, 6, 13, cloak);
        FillRect(p, s, 17, 8, 18, 13, cloak);
        // Wispy tattered bottom tendrils
        FillRect(p, s, 7, 3, 8, 5, body);
        FillRect(p, s, 11, 2, 12, 5, body);
        FillRect(p, s, 15, 3, 16, 5, body);
        SetPixel(p, s, 6, 4, cloak);
        SetPixel(p, s, 17, 4, cloak);
        // Two glowing crimson-white eyes
        FillRect(p, s, 9, 16, 10, 17, eyeOuter);
        FillRect(p, s, 13, 16, 14, 17, eyeOuter);
        SetPixel(p, s, 10, 17, eyeInner);
        SetPixel(p, s, 14, 17, eyeInner);

        return CreatePointSprite(s, p);
    }

    static Sprite BuildChargerSprite()
    {
        // 24x24 sleek spear/horned silhouette with bright orange-amber glowing core
        const int s = 24;
        var p = new Color32[s * s];
        Color32 body = new Color32(20, 14, 20, 255);
        Color32 horn = new Color32(68, 32, 24, 255);
        Color32 coreOuter = new Color32(255, 138, 28, 255);
        Color32 coreInner = new Color32(255, 238, 150, 255);

        // Sleek diamond/spear body
        for (int y = 4; y <= 19; y++)
        {
            int dy = Mathf.Abs(y - 11);
            int halfW = Mathf.Max(1, 7 - dy / 2);
            FillRect(p, s, 12 - halfW, y, 11 + halfW, y, body);
        }
        // Swept horns
        FillRect(p, s, 5, 16, 7, 19, horn);
        FillRect(p, s, 16, 16, 18, 19, horn);
        SetPixel(p, s, 4, 20, horn);
        SetPixel(p, s, 19, 20, horn);
        // Tail fin
        FillRect(p, s, 11, 2, 12, 4, horn);
        // Bright orange-amber core
        FillRect(p, s, 10, 10, 13, 13, coreOuter);
        FillRect(p, s, 11, 11, 12, 12, coreInner);

        return CreatePointSprite(s, p);
    }

    static Sprite BuildJuggernautSprite()
    {
        // 32x32 massive broad heavy void-golem silhouette with glowing violet/magenta core ring
        const int s = 32;
        var p = new Color32[s * s];
        Color32 body = new Color32(18, 14, 28, 255);
        Color32 armor = new Color32(46, 30, 68, 255);
        Color32 ring = new Color32(215, 68, 245, 255);
        Color32 core = new Color32(250, 205, 255, 255);

        // Heavy torso
        FillRect(p, s, 7, 6, 24, 23, body);
        FillRect(p, s, 9, 24, 22, 26, body);
        // Broad armored pauldrons
        FillRect(p, s, 3, 12, 7, 22, armor);
        FillRect(p, s, 24, 12, 28, 22, armor);
        FillRect(p, s, 4, 8, 6, 12, body);
        FillRect(p, s, 25, 8, 27, 12, body);
        // Heavy legs
        FillRect(p, s, 9, 2, 13, 6, body);
        FillRect(p, s, 18, 2, 22, 6, body);
        // Crown horns
        FillRect(p, s, 7, 25, 9, 29, armor);
        FillRect(p, s, 22, 25, 24, 29, armor);
        // Glowing violet/magenta core ring
        FillRect(p, s, 12, 12, 19, 19, ring);
        FillRect(p, s, 14, 14, 17, 17, body);
        FillRect(p, s, 15, 15, 16, 16, core);
        // Glowing slit visor above core
        FillRect(p, s, 12, 22, 14, 23, core);
        FillRect(p, s, 17, 22, 19, 23, core);

        return CreatePointSprite(s, p);
    }
}
