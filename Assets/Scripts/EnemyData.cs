using UnityEngine;

[CreateAssetMenu(menuName = "VoidSurvivor/Enemy Data", fileName = "NewEnemy")]
public class EnemyData : ScriptableObject
{
    public string displayName;
    public float maxHealth = 10f;
    public float moveSpeed = 2f;
    public float contactDamage = 8f;
    public int xpValue = 1;

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