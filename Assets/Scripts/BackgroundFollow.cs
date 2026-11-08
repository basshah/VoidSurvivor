using UnityEngine;

public class BackgroundFollow : MonoBehaviour
{
    public Transform target;
    public float tileSize = 5.12f; // 512 piksel / 100 PPU

    void LateUpdate()
    {
        if (target == null) return;
        float x = Mathf.Round(target.position.x / tileSize) * tileSize;
        float y = Mathf.Round(target.position.y / tileSize) * tileSize;
        transform.position = new Vector3(x, y, 0f);
    }
}