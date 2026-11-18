using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Base")]
    public float moveSpeed = 3f;
    public float maxHealth = 100f;
    public float pickupRange = 1.5f;

    [Header("Passives (Day 4)")]
    [Range(0f, 0.5f)] public float damageReduction = 0f;
    [Range(0f, 0.5f)] public float cooldownReduction = 0f;
    public float attackMultiplier = 1f;
}