using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smooth = 10f;

    void LateUpdate()
    {
        if (target == null) return;
        float t = 1f - Mathf.Exp(-smooth * Time.deltaTime);
        Vector3 p = Vector3.Lerp(transform.position, target.position, t);
        p.z = -10f;
        transform.position = p;
    }
}