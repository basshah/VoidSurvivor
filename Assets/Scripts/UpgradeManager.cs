using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum PassiveType
{
    Attack,
    MaxHP,
    DamageReduction,
    Cooldown
}

public enum UpgradeKind
{
    Spell,
    Passive
}

public class UpgradeOption
{
    public UpgradeKind kind;
    public string title;
    public string levelTag;
    public string description;
    public string iconKey;
    public bool isEvolution;

    public Spell spell;
    public PassiveType passiveType;
}

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private XPManager xpManager;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private LevelUpUI levelUpUI;

    [Header("Passive Settings")]
    [SerializeField] private int maxPassiveLevel = 5;
    [SerializeField] private float attackPerLevel = 0.15f;     // +15% her level (max +75%)
    [SerializeField] private float maxHpPerLevel = 20f;        // +20 HP her level (max +100)
    [SerializeField] private float reductionPerLevel = 0.10f;  // 10, 20, 30, 40, 50%
    [SerializeField] private float cooldownPerLevel = 0.10f;   // 10, 20, 30, 40, 50%
    [SerializeField] private float maxPoolHealAmount = 25f;    // Her sey dolanda kompensasiya

    [Header("Resume Transition")]
    [SerializeField] private float resumeFreezeDuration = 1.0f;
    [SerializeField] private Color freezeEdgeColor = new Color(0.55f, 0.88f, 1f, 0.75f);

    private readonly Dictionary<PassiveType, int> passiveLevels = new Dictionary<PassiveType, int>
    {
        { PassiveType.Attack, 0 },
        { PassiveType.MaxHP, 0 },
        { PassiveType.DamageReduction, 0 },
        { PassiveType.Cooldown, 0 }
    };

    private float baseAttackMultiplier = 1f;
    private int pendingLevelUps;
    private bool isChoosing;
    private Coroutine resumeCoroutine;

    private RawImage freezeOverlay;
    private Texture2D freezeTexture;

    private void Awake()
    {
        Instance = this;

        if (xpManager == null) xpManager = FindFirstObjectByType<XPManager>();
        if (playerStats == null) playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerHealth == null) playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (levelUpUI == null) levelUpUI = FindFirstObjectByType<LevelUpUI>(FindObjectsInactive.Include);

        if (playerStats != null)
        {
            baseAttackMultiplier = playerStats.attackMultiplier;
        }
    }

    private void OnEnable()
    {
        if (xpManager != null)
        {
            xpManager.onLevelUp += HandleLevelUp;
        }
    }

    private void OnDisable()
    {
        if (xpManager != null)
        {
            xpManager.onLevelUp -= HandleLevelUp;
        }

        if (resumeCoroutine != null)
        {
            StopCoroutine(resumeCoroutine);
            resumeCoroutine = null;
        }

        HideFreezeOverlay();
    }

    private void OnDestroy()
    {
        if (freezeTexture != null)
        {
            Destroy(freezeTexture);
            freezeTexture = null;
        }
    }

    private void HandleLevelUp(int newLevel) => EnqueueLevelUp();

    private void EnqueueLevelUp()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        pendingLevelUps++;
        if (!isChoosing)
        {
            ShowNextLevelUp();
        }
    }

    private void ShowNextLevelUp()
    {
        if (resumeCoroutine != null)
        {
            StopCoroutine(resumeCoroutine);
            resumeCoroutine = null;
        }
        HideFreezeOverlay();

        List<UpgradeOption> options = RollOptions(3);

        if (options.Count == 0)
        {
            pendingLevelUps = Mathf.Max(0, pendingLevelUps - 1);
            if (playerHealth != null)
            {
                playerHealth.Heal(maxPoolHealAmount);
            }
            if (pendingLevelUps > 0)
            {
                ShowNextLevelUp();
            }
            return;
        }

        if (levelUpUI == null)
        {
            Debug.LogWarning("[UpgradeManager] LevelUpUI tapilmadi!");
            pendingLevelUps = 0;
            return;
        }

        isChoosing = true;
        levelUpUI.Show(options, ApplyUpgrade);
    }

    private List<UpgradeOption> RollOptions(int count)
    {
        List<UpgradeOption> pool = new List<UpgradeOption>();

        if (playerStats != null)
        {
            Spell[] spells = playerStats.GetComponentsInChildren<Spell>(true);
            foreach (Spell spell in spells)
            {
                if (spell == null || spell.Level >= SpellData.MaxLevel)
                    continue;

                bool nextIsEvolution = spell.Level == SpellData.MaxLevel - 1;
                int nextLevel = spell.Level + 1;

                pool.Add(new UpgradeOption
                {
                    kind = UpgradeKind.Spell,
                    spell = spell,
                    title = spell.SpellName,
                    levelTag = nextIsEvolution
                        ? "EVOLUTION"
                        : (spell.Level == 0 ? "NEW" : $"LV {spell.Level} -> {nextLevel}"),
                    description = spell.GetUpgradeDescription(nextLevel),
                    iconKey = "spell_" + spell.SpellId,
                    isEvolution = nextIsEvolution
                });
            }
        }

        AddPassiveIfAvailable(pool, PassiveType.Attack, "Attack", "attack");
        AddPassiveIfAvailable(pool, PassiveType.MaxHP, "Max HP", "heart");
        AddPassiveIfAvailable(pool, PassiveType.DamageReduction, "Armor", "shield");
        AddPassiveIfAvailable(pool, PassiveType.Cooldown, "Cooldown", "cooldown");

        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        if (pool.Count > count)
        {
            pool.RemoveRange(count, pool.Count - count);
        }

        return pool;
    }

    private void AddPassiveIfAvailable(List<UpgradeOption> pool, PassiveType type, string title, string iconKey)
    {
        int current = passiveLevels[type];
        if (current >= maxPassiveLevel)
            return;

        int next = current + 1;
        pool.Add(new UpgradeOption
        {
            kind = UpgradeKind.Passive,
            passiveType = type,
            title = title,
            levelTag = $"LV {current} -> {next}",
            description = GetPassiveDescription(type, next),
            iconKey = iconKey,
            isEvolution = false
        });
    }

    private string GetPassiveDescription(PassiveType type, int nextLevel)
    {
        switch (type)
        {
            case PassiveType.Attack:
                return $"+{Mathf.RoundToInt(attackPerLevel * 100)}% Attack (Total +{Mathf.RoundToInt(nextLevel * attackPerLevel * 100)}%)";
            case PassiveType.MaxHP:
                return $"+{Mathf.RoundToInt(maxHpPerLevel)} Max HP & Heal {Mathf.RoundToInt(maxHpPerLevel)}";
            case PassiveType.DamageReduction:
                return $"+{Mathf.RoundToInt(reductionPerLevel * 100)}% Damage Reduction (Total {Mathf.RoundToInt(nextLevel * reductionPerLevel * 100)}%)";
            case PassiveType.Cooldown:
                return $"-{Mathf.RoundToInt(cooldownPerLevel * 100)}% Cooldown (Total -{Mathf.RoundToInt(nextLevel * cooldownPerLevel * 100)}%)";
            default:
                return "";
        }
    }

    private void ApplyUpgrade(UpgradeOption option)
    {
        if (option.kind == UpgradeKind.Spell && option.spell != null)
        {
            option.spell.LevelUp();
        }
        else if (option.kind == UpgradeKind.Passive)
        {
            ApplyPassive(option.passiveType);
        }

        pendingLevelUps = Mathf.Max(0, pendingLevelUps - 1);
        if (pendingLevelUps > 0)
        {
            ShowNextLevelUp();
        }
        else
        {
            isChoosing = false;
            levelUpUI.Hide(false);
            resumeCoroutine = StartCoroutine(ResumeTimeSmoothly());
        }
    }

    private IEnumerator ResumeTimeSmoothly()
    {
        Time.timeScale = 0f;
        EnsureFreezeOverlay();

        float duration = Mathf.Max(0.05f, resumeFreezeDuration);
        float elapsed = 0f;

        if (freezeOverlay != null)
        {
            freezeOverlay.gameObject.SetActive(true);
            if (levelUpUI != null && levelUpUI.transform.parent == freezeOverlay.transform.parent)
            {
                freezeOverlay.transform.SetAsLastSibling();
                int levelUpIdx = levelUpUI.transform.GetSiblingIndex();
                freezeOverlay.transform.SetSiblingIndex(levelUpIdx);
            }
            else
            {
                freezeOverlay.transform.SetAsLastSibling();
            }
        }

        while (elapsed < duration)
        {
            if (LevelUpUI.IsOpen ||
                (GameManager.Instance != null && (GameManager.Instance.IsPaused || GameManager.Instance.IsGameOver)))
            {
                HideFreezeOverlay();
                resumeCoroutine = null;
                yield break;
            }

            float t = Mathf.Clamp01(elapsed / duration);
            UpdateFreezeOverlay(t);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        HideFreezeOverlay();

        if (!LevelUpUI.IsOpen &&
            (GameManager.Instance == null || (!GameManager.Instance.IsPaused && !GameManager.Instance.IsGameOver)))
        {
            Time.timeScale = 1f;
        }

        resumeCoroutine = null;
    }

    private void EnsureFreezeOverlay()
    {
        if (freezeOverlay != null) return;

        Canvas canvas = levelUpUI != null
            ? levelUpUI.GetComponentInParent<Canvas>()
            : null;
        if (canvas == null)
        {
            GameUI gameUI = FindFirstObjectByType<GameUI>(FindObjectsInactive.Include);
            canvas = gameUI != null ? gameUI.GetComponentInParent<Canvas>() : FindFirstObjectByType<Canvas>();
        }
        if (canvas == null) return;

        GameObject go = new GameObject("FreezeEdgeOverlay", typeof(RectTransform), typeof(RawImage));
        go.layer = canvas.gameObject.layer;
        go.transform.SetParent(canvas.transform, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        freezeTexture = CreateEdgeGlowTexture(256);

        freezeOverlay = go.GetComponent<RawImage>();
        freezeOverlay.texture = freezeTexture;
        freezeOverlay.color = freezeEdgeColor;
        freezeOverlay.raycastTarget = false;
        go.SetActive(false);
    }

    private void UpdateFreezeOverlay(float t)
    {
        if (freezeOverlay == null) return;

        // İlk andan son ana qədər tam sabit (xətti) sürətlə sönmə
        Color c = freezeEdgeColor;
        c.a = freezeEdgeColor.a * (1f - t);
        freezeOverlay.color = c;
    }

    private void HideFreezeOverlay()
    {
        if (freezeOverlay != null)
        {
            freezeOverlay.rectTransform.localScale = Vector3.one;
            freezeOverlay.gameObject.SetActive(false);
        }
    }

    private static Texture2D CreateEdgeGlowTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        float inv = 1f / (size - 1);

        for (int y = 0; y < size; y++)
        {
            float ny = y * inv;
            float vy = ny * 2f - 1f; // -1 .. 1

            for (int x = 0; x < size; x++)
            {
                float nx = x * inv;
                float vx = nx * 2f - 1f; // -1 .. 1

                // Oval/radial məsafə (künclər daha dolğun, ortası tam təmiz)
                float radial = Mathf.Sqrt(vx * vx * 0.92f + vy * vy * 1.06f);

                // İki qatlı yumşaq təbii toxuma (portret ekranda dartılmaması üçün y tezliyi yüksək)
                float n1 = Mathf.PerlinNoise(nx * 3.8f + 1.3f, ny * 6.8f + 2.7f);
                float n2 = Mathf.PerlinNoise(nx * 8.5f + 5.1f, ny * 15.0f + 8.3f);
                float organic = (n1 * 0.65f + n2 * 0.35f) - 0.5f;

                // Daxili sərhəd təbii dalğalanır, düz xətt yaranmır
                float inner = 0.58f + organic * 0.24f;
                float outer = 1.32f;

                float t = Mathf.InverseLerp(inner, outer, radial);
                float smooth = t * t * (3f - 2f * t); // Smoothstep

                float alpha = Mathf.Pow(smooth, 1.35f);
                alpha *= Mathf.Lerp(0.80f, 1.15f, n1);
                alpha = Mathf.Clamp01(alpha);

                // Kənarlara doğru azca daha parlaq ton keçidi
                float shade = Mathf.Lerp(0.75f, 1f, smooth);
                pixels[y * size + x] = new Color(shade, shade, shade, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);
        return tex;
    }

    private void ApplyPassive(PassiveType type)
    {
        int nextLevel = Mathf.Min(passiveLevels[type] + 1, maxPassiveLevel);
        passiveLevels[type] = nextLevel;

        if (playerStats == null) return;

        switch (type)
        {
            case PassiveType.Attack:
                playerStats.attackMultiplier = baseAttackMultiplier + nextLevel * attackPerLevel;
                break;

            case PassiveType.MaxHP:
                playerStats.maxHealth += maxHpPerLevel;
                if (playerHealth != null)
                {
                    playerHealth.AddMaxHealth(maxHpPerLevel);
                }
                break;

            case PassiveType.DamageReduction:
                playerStats.damageReduction = Mathf.Clamp(nextLevel * reductionPerLevel, 0f, 0.50f);
                break;

            case PassiveType.Cooldown:
                playerStats.cooldownReduction = Mathf.Clamp(nextLevel * cooldownPerLevel, 0f, 0.50f);
                break;
        }
    }

    public int GetPassiveLevel(PassiveType type) => passiveLevels[type];
}