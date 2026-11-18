using UnityEngine;

public class PlayerHealthBar : MonoBehaviour
{
    public Vector2 offset = new Vector2(0f, -0.9f);
    public float width = 1.4f;
    public float height = 0.16f;

    PlayerHealth health;
    Transform root, fill;
    SpriteRenderer fillSr;

    void Start()
    {
        health = GetComponent<PlayerHealth>();

        var tex = Texture2D.whiteTexture;
        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                   new Vector2(0f, 0.5f), tex.width);

        root = new GameObject("HealthBar").transform;

        var bg = new GameObject("BG").AddComponent<SpriteRenderer>();
        bg.sprite = sprite;
        bg.color = new Color(0f, 0f, 0f, 0.7f);
        bg.sortingOrder = 20;
        bg.transform.SetParent(root, false);
        bg.transform.localPosition = new Vector3(-width / 2f - 0.03f, 0f, 0f);
        bg.transform.localScale = new Vector3(width + 0.06f, height + 0.06f, 1f);

        fillSr = new GameObject("Fill").AddComponent<SpriteRenderer>();
        fillSr.sprite = sprite;
        fillSr.sortingOrder = 21;
        fill = fillSr.transform;
        fill.SetParent(root, false);
        fill.localPosition = new Vector3(-width / 2f, 0f, 0f);

        health.onHealthChanged += OnChanged;
        OnChanged(health.Current, health.MaxHealth);
    }

    void OnChanged(float current, float max)
    {
        float f = Mathf.Clamp01(current / max);
        fill.localScale = new Vector3(width * f, height, 1f);
        fillSr.color = Color.Lerp(new Color(1f, 0.25f, 0.25f), new Color(0.35f, 1f, 0.4f), f);
    }

    void LateUpdate()
    {
        if (root != null) root.position = transform.position + (Vector3)offset;
    }

    void OnDestroy()
    {
        if (health != null) health.onHealthChanged -= OnChanged;
        if (root != null) Destroy(root.gameObject);
    }
}