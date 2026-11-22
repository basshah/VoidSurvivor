using UnityEngine;

public abstract class Spell : MonoBehaviour
{
    public SpellData data;
    [Range(1, 7)] public int level = 1;

    protected PlayerStats stats;
    float timer;

    public bool IsEvolved => level >= SpellData.MaxLevel;

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
        if (data == null || stats == null) return;
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = TryFire() ? Cooldown : 0.1f;   // hədəf yoxdursa tez-tez yenidən yoxla
    }

    public void LevelUp() { if (level < SpellData.MaxLevel) level++; }

    protected abstract bool TryFire();
}