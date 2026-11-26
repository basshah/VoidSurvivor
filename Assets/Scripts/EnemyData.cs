using UnityEngine;

public enum EnemyVisualType
{
    Swarmer,
    Stalker,
    Charger,
    Juggernaut
}

[CreateAssetMenu(menuName = "VoidSurvivor/Enemy Data", fileName = "NewEnemy")]
public class EnemyData : ScriptableObject
{
    public string displayName;
    public float maxHealth = 10f;
    public float moveSpeed = 2f;
    public float contactDamage = 8f;
    public int xpValue = 1;

    [Header("Visuals & Hitbox")]
    public EnemyVisualType visualType = EnemyVisualType.Stalker;
    public float visualSize = 0.9f;
    public float hitRadiusRatio = 0.42f;
    [Range(0f, 1f)] public float knockbackResistance = 0f;
    public Color deathVfxColor = new Color(1f, 0.25f, 0.3f, 1f);

    [Header("Optional Custom Art (Future Swap)")]
    public Sprite customSprite;
    public RuntimeAnimatorController animatorController;

    private void OnEnable()
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = name;
        }
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = name;
        }
    }
}