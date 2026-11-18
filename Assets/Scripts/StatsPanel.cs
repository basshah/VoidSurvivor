using TMPro;
using UnityEngine;

public class StatsPanel : MonoBehaviour
{
    public PlayerStats stats;
    public PlayerHealth health;

    public TMP_Text hpValue;
    public TMP_Text damageTakenValue;
    public TMP_Text moveSpeedValue;
    public TMP_Text spellDamageValue;
    public TMP_Text cooldownValue;
    public TMP_Text pickupValue;

    static readonly Color Lit = new Color(0.55f, 1f, 0.3f);
    static readonly Color Dim = new Color(0.6f, 0.6f, 0.6f);

    void OnEnable() { Refresh(); }

    public void Refresh()
    {
        if (stats == null || health == null) return;
        Set(hpValue, $"{health.Current:0} / {health.MaxHealth:0}", true);
        Set(damageTakenValue, Pct(-stats.damageReduction), stats.damageReduction > 0.0001f);
        Set(moveSpeedValue, stats.moveSpeed.ToString("0.0"), true);
        Set(spellDamageValue, Pct(stats.attackMultiplier - 1f), Mathf.Abs(stats.attackMultiplier - 1f) > 0.0001f);
        Set(cooldownValue, Pct(-stats.cooldownReduction), stats.cooldownReduction > 0.0001f);
        Set(pickupValue, stats.pickupRange.ToString("0.0"), true);
    }

    static void Set(TMP_Text t, string s, bool lit)
    {
        if (t == null) return;
        t.text = s;
        t.color = lit ? Lit : Dim;
    }

    static string Pct(float v)
    {
        int p = Mathf.RoundToInt(v * 100f);
        return (p > 0 ? "+" : "") + p + "%";
    }
}