using UnityEngine;

public class XPGem : MonoBehaviour
{
    public int value = 1;
    public float collectDistance = 0.4f;
    public float globalPullSpeed = 16f;

    [HideInInspector] public float speed = 4f;
    [HideInInspector] public bool attracted;
    [HideInInspector] public bool globalAttracted;
    [HideInInspector] public Vector2 position;
    [HideInInspector] public int activeIndex = -1;

    public void ResetForPool(Vector2 pos, int val)
    {
        position = pos;
        transform.position = new Vector3(pos.x, pos.y, 0f);
        value = val;
        speed = 4f;
        attracted = false;
        globalAttracted = false;
    }
}