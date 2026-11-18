using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    Rect last;

    void OnEnable() { Apply(); }
    void Update() { if (Screen.safeArea != last) Apply(); }

    void Apply()
    {
        Rect sa = Screen.safeArea;
        last = sa;
        if (Screen.width <= 0 || Screen.height <= 0) return;
        var rt = (RectTransform)transform;
        rt.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
        rt.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}