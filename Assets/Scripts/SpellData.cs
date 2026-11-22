using UnityEngine;

[System.Serializable]
public class SpellLevelStats
{
    public float damage = 10f;
    public float cooldown = 1f;
    public int count = 1;        // mərmi / şüa / kürə sayı
    public float speed = 12f;
    public float size = 1f;
    public int pierce = 0;       // neçə düşməndən keçir
    public float duration = 1.5f;
}

[CreateAssetMenu(menuName = "VoidSurvivor/Spell Data", fileName = "NewSpell")]
public class SpellData : ScriptableObject
{
    public const int MaxLevel = 7;   // 7-ci level = evolution

    public string id = "starshot";   // DamageTracker-dəki ad ilə eyni olmalıdır
    public string displayName = "Starshot";
    public Sprite icon;
    [TextArea] public string description;
    public float minCooldown = 0.05f;

    public SpellLevelStats[] levels = new SpellLevelStats[6];  // level 1..6
    public SpellLevelStats evolution;                          // level 7
}