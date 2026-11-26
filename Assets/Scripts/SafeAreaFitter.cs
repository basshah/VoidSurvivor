using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    Rect last;
    int lastWidth;
    int lastHeight;

    void OnEnable() { Apply(); }
    void Update()
    {
        if (Screen.safeArea != last || Screen.width != lastWidth || Screen.height != lastHeight)
            Apply();
    }

    void Apply()
    {
        if (Screen.width <= 0 || Screen.height <= 0) return;
        Rect sa = Screen.safeArea;
        if (sa == last && Screen.width == lastWidth && Screen.height == lastHeight) return;
        last = sa;
        lastWidth = Screen.width;
        lastHeight = Screen.height;
        var rt = (RectTransform)transform;
        rt.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
        rt.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}