using UnityEngine;

/// <summary>
/// Zero-GC 8-direction sprite selector and zero-G hover/tilt controller for VisualRoot.
/// Sprite order clockwise from East (0 deg): E, NE, N, NW, W, SW, S, SE.
/// </summary>
public class DirectionalVisual : MonoBehaviour
{
    public static readonly string[] DirectionNames =
    {
        "east", "north-east", "north", "north-west",
        "west", "south-west", "south", "south-east"
    };

    [Header("8-Direction Sprites (E, NE, N, NW, W, SW, S, SE)")]
    public Sprite[] sprites = new Sprite[8];

    [Header("Zero-G Float & Tilt")]
    public bool enableHover = true;
    public float hoverAmplitude = 0.055f;
    public float hoverFrequency = 2.6f;
    public float maxTiltDegrees = 7f;
    public float tiltSpeed = 10f;

    SpriteRenderer sr;
    Vector3 baseLocalPos;
    float phaseOffset;
    float hoverTime;
    float currentTiltZ;
    int currentOctant = -1;
    bool hasAll8;
    bool initialized;

    public bool HasAllDirections => hasAll8;

    void Awake()
    {
        EnsureInitialized();
    }

    public void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;
        sr = GetComponent<SpriteRenderer>();
        baseLocalPos = transform.localPosition;
        phaseOffset = (Mathf.Abs(GetInstanceID()) % 1024) * 0.0123f;
        RefreshSpriteCompleteness();
    }

    public void SetSprites(Sprite[] newSprites)
    {
        EnsureInitialized();
        if (newSprites == null) return;

        if (sprites == null || sprites.Length != 8)
        {
            sprites = new Sprite[8];
        }

        int len = Mathf.Min(8, newSprites.Length);
        for (int i = 0; i < len; i++)
        {
            sprites[i] = newSprites[i];
        }

        RefreshSpriteCompleteness();
        currentOctant = -1;
    }

    public void RefreshSpriteCompleteness()
    {
        hasAll8 = sprites != null && sprites.Length >= 8;
        if (!hasAll8) return;

        for (int i = 0; i < 8; i++)
        {
            if (sprites[i] == null)
            {
                hasAll8 = false;
                break;
            }
        }
    }

    public void ResetVisual()
    {
        EnsureInitialized();
        currentOctant = -1;
        currentTiltZ = 0f;
        hoverTime = 0f;
        transform.localPosition = baseLocalPos;
        transform.localRotation = Quaternion.identity;

        if (hasAll8 && sr != null)
        {
            sr.flipX = false;
            currentOctant = 6; // Default South-facing until movement sets octant
            sr.sprite = sprites[6];
        }
    }

    public void SetDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude <= 0.0025f) return;

        if (hasAll8)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;
            int octant = Mathf.RoundToInt(angle / 45f) & 7;

            if (octant != currentOctant)
            {
                currentOctant = octant;
                if (sr != null)
                {
                    sr.sprite = sprites[octant];
                }
            }
            if (sr != null && sr.flipX) sr.flipX = false;
        }
        else if (sr != null && Mathf.Abs(dir.x) > 0.05f)
        {
            sr.flipX = dir.x < 0f;
        }
    }

    public void TickVisual(Vector2 velocity, float dt)
    {
        EnsureInitialized();

        if (velocity.sqrMagnitude > 0.0025f)
        {
            SetDirection(velocity);
        }

        if (enableHover && dt > 0f)
        {
            hoverTime += dt * hoverFrequency;
            float yOffset = Mathf.Sin(hoverTime + phaseOffset) * hoverAmplitude;
            transform.localPosition = new Vector3(baseLocalPos.x, baseLocalPos.y + yOffset, baseLocalPos.z);

            float targetTilt = Mathf.Clamp(-velocity.x * 1.4f, -maxTiltDegrees, maxTiltDegrees);
            currentTiltZ = Mathf.Lerp(currentTiltZ, targetTilt, Mathf.Clamp01(dt * tiltSpeed));
            transform.localRotation = Quaternion.Euler(0f, 0f, currentTiltZ);
        }
    }

    /// <summary>
    /// Maps a filename to an octant index (0..7) matching compound directions before cardinals.
    /// </summary>
    public static int GetOctantFromFileName(string fileNameWithoutExt)
    {
        if (string.IsNullOrEmpty(fileNameWithoutExt)) return -1;
        string s = fileNameWithoutExt.ToLowerInvariant();

        // Compound directions MUST be checked before cardinal substrings
        if (s.Contains("north-east") || s.Contains("northeast") || s == "ne") return 1;
        if (s.Contains("north-west") || s.Contains("northwest") || s == "nw") return 3;
        if (s.Contains("south-west") || s.Contains("southwest") || s == "sw") return 5;
        if (s.Contains("south-east") || s.Contains("southeast") || s == "se") return 7;

        if (s.Contains("east") || s == "e") return 0;
        if (s.Contains("north") || s == "n") return 2;
        if (s.Contains("west") || s == "w") return 4;
        if (s.Contains("south") || s == "s") return 6;

        return -1;
    }
}
