using UnityEngine;

public class XPGem : MonoBehaviour
{
    public int value = 1;
    public float collectDistance = 0.4f;

    float speed = 4f;
    bool attracted;

    void Update()
    {
        var m = XPManager.Instance;
        if (m == null || m.player == null) return;

        Vector2 to = (Vector2)m.player.position - (Vector2)transform.position;
        float sqr = to.sqrMagnitude;

        if (!attracted)
        {
            float r = m.PickupRange;
            if (sqr > r * r) return;
            attracted = true;
        }

        if (sqr <= collectDistance * collectDistance)
        {
            m.AddXP(value);
            Destroy(gameObject);
            return;
        }

        speed = Mathf.Min(speed + 40f * Time.deltaTime, 20f);
        transform.position += (Vector3)(to.normalized * speed * Time.deltaTime);
    }
}