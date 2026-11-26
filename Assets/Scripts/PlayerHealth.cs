using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    const float HitFlashDuration = 0.1f;
    static readonly Color HitFlashTint = new Color(1f, 0.3f, 0.3f, 1f);

    public float invincibleTime = 0.4f;

    public float Current { get; private set; }
    public float MaxHealth => stats.maxHealth;
    public event Action<float, float> onHealthChanged;

    PlayerStats stats;
    SpriteRenderer sr;
    Transform visualTransform;
    Vector3 baseVisualScale = Vector3.one;
    Color baseColor = Color.white;
    float lastHitTime = -999f;
    float flashTimer;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        Current = stats.maxHealth;

        Transform visual = transform.Find("VisualRoot");
        sr = visual != null ? visual.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();
        visualTransform = sr != null ? sr.transform : transform;
        baseVisualScale = visualTransform.localScale;
        if (sr != null) baseColor = sr.color;
    }

    void Update()
    {
        if (flashTimer <= 0f) return;

        flashTimer = Mathf.Max(0f, flashTimer - Time.deltaTime);
        float t = flashTimer / HitFlashDuration;
        if (sr != null)
        {
            sr.color = Color.Lerp(baseColor, HitFlashTint, t);
        }
        if (visualTransform != null && visualTransform != transform)
        {
            visualTransform.localScale = baseVisualScale * Mathf.Lerp(1f, 1.12f, t);
        }
    }

    public void TakeDamage(float amount)
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.IsGameOver) return;
        if (Time.time - lastHitTime < invincibleTime) return;
        lastHitTime = Time.time;

        Current = Mathf.Max(0f, Current - amount * (1f - stats.damageReduction));
        onHealthChanged?.Invoke(Current, MaxHealth);

        if (Current <= 0f)
        {
            gm?.GameOver();
        }
        else
        {
            flashTimer = HitFlashDuration;
            if (sr != null) sr.color = HitFlashTint;
            if (visualTransform != null && visualTransform != transform)
            {
                visualTransform.localScale = baseVisualScale * 1.12f;
            }
        }
    }

    public void AddMaxHealth(float amount)
    {
        if (amount <= 0f) return;
        Current = Mathf.Min(Current + amount, MaxHealth);
        onHealthChanged?.Invoke(Current, MaxHealth);
    }

    public void Heal(float amount)
    {
        if (amount <= 0f) return;
        Current = Mathf.Min(Current + amount, MaxHealth);
        onHealthChanged?.Invoke(Current, MaxHealth);
    }
}