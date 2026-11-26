using UnityEngine;

public abstract class Spell : MonoBehaviour
{
    public SpellData data;
    [Range(0, 7)] public int level = 1;

    protected PlayerStats stats;
    float timer;

    public int Level => level;
    public bool IsEvolved => level >= SpellData.MaxLevel;

    // QEYD: SpellData.cs-də sahə adları fərqlidirsə (məs: displayName / spellName), bu 2 sətri uyğunlaşdır
    public string SpellId => data != null ? data.id : name.ToLower();
    public string SpellName => data != null ? data.displayName : name;

    public SpellLevelStats Current
    {
        get
        {
            if (IsEvolved && data.evolution != null) return data.evolution;
            int i = Mathf.Clamp(level, 1, data.levels.Length) - 1;
            return data.levels[i];
        }
    }

    protected float Damage => Current.damage * stats.attackMultiplier;
    protected float Cooldown =>
        Mathf.Max(data.minCooldown, Current.cooldown * (1f - stats.cooldownReduction));

    protected virtual void Awake() { stats = GetComponentInParent<PlayerStats>(); }

    void Update()
    {
        if (Time.timeScale <= 0f) return;
        if (level <= 0 || data == null || stats == null) return;
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = TryFire() ? Cooldown : 0.1f;   // hədəf yoxdursa tez-tez yenidən yoxla
    }

    public void LevelUp() { if (level < SpellData.MaxLevel) level++; }

    public virtual string GetUpgradeDescription(int nextLevel)
    {
        if (data == null) return $"Upgrade to Level {nextLevel}";

        if (nextLevel >= SpellData.MaxLevel && data.evolution != null)
        {
            var evo = data.evolution;
            return $"EVOLUTION | DMG {evo.damage:0} | CD {evo.cooldown:0.0}s";
        }

        int i = Mathf.Clamp(nextLevel, 1, data.levels.Length) - 1;
        var s = data.levels[i];
        return $"DMG {s.damage:0} | CD {s.cooldown:0.0}s";
    }

    protected abstract bool TryFire();
}