using System;
using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float invincibleTime = 0.4f;

    public float Current { get; private set; }
    public float MaxHealth => stats.maxHealth;
    public event Action<float, float> onHealthChanged;

    PlayerStats stats;
    SpriteRenderer sr;
    Color baseColor;
    float lastHitTime = -999f;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        Current = stats.maxHealth;
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
    }

    public void TakeDamage(float amount)
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.IsGameOver) return;
        if (Time.time - lastHitTime < invincibleTime) return;
        lastHitTime = Time.time;

        Current = Mathf.Max(0f, Current - amount * (1f - stats.damageReduction));
        onHealthChanged?.Invoke(Current, MaxHealth);

        if (Current <= 0f) gm?.GameOver();
        else if (sr != null) StartCoroutine(Flash());
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

    IEnumerator Flash()
    {
        sr.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        sr.color = baseColor;
    }
}