using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class UIKit
{
    public static readonly Color Clear = new Color(0f, 0f, 0f, 0f);
    public static readonly Color Yellow = new Color(1f, 0.85f, 0.25f);
    public static readonly Color Orange = new Color(1f, 0.5f, 0.15f);
    public static readonly Color Green = new Color(0.55f, 1f, 0.3f);
    public static readonly Color Gray = new Color(0.6f, 0.6f, 0.6f);

    static TMP_FontAsset font;
    static bool fontLoaded;

    // Öz şriftini istəsən: Assets/Resources/UIFont.asset (TMP Font Asset)
    public static TMP_FontAsset Font
    {
        get
        {
            if (!fontLoaded) { font = Resources.Load<TMP_FontAsset>("UIFont"); fontLoaded = true; }
            return font;
        }
    }

    public static RectTransform Node(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // Anchor və pivot eynidir: pos = elementin həmin küncünün anchor-dan məsafəsi
    public static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    public static void TopBar(RectTransform rt, float height)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(0f, -height);
        rt.offsetMax = Vector2.zero;
    }

    public static Image Img(string name, Transform parent, Color color, bool raycast = false)
    {
        var rt = Node(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = raycast;
        return img;
    }

    public static TextMeshProUGUI Txt(string name, Transform parent, string text, float size,
        Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var rt = Node(name, parent);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (Font != null) t.font = Font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        return t;
    }

    public static Button Btn(string name, Transform parent, string label, float fontSize,
        Color bg, UnityAction onClick)
    {
        var img = Img(name, parent, bg, true);
        var b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        var t = Txt("Label", img.transform, label, fontSize, Color.white);
        Stretch(t.rectTransform);
        b.onClick.AddListener(onClick);
        return b;
    }

    // Assets/Resources/Icons/<ad>.png varsa onu göstərir, yoxdursa rəngli kvadrat
    public static Image Icon(string name, Transform parent, Color fallback)
    {
        var img = Img("Icon_" + name, parent, fallback);
        var sp = Resources.Load<Sprite>("Icons/" + name);
        if (sp != null)
        {
            img.sprite = sp;
            img.color = Color.white;
            img.preserveAspect = true;
        }
        return img;
    }

    public static RectTransform Fill(Transform parent, Color color)
    {
        var img = Img("Fill", parent, color);
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    public static void SetFill(RectTransform rt, float value)
    {
        rt.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
    }

    public static Canvas CreateCanvas(string name, int order)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas),
                                typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.layer = LayerMask.NameToLayer("UI");
        var c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = order;
        var s = go.GetComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1080f, 1920f);
        s.matchWidthOrHeight = 0.5f;
        return c;
    }

    // Çentik (notch) və sistem panellərindən qaçmaq üçün
    public static RectTransform SafeArea(Transform parent)
    {
        var rt = Node("SafeArea", parent);
        Rect sa = Screen.safeArea;
        rt.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
        rt.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    public static string FormatTime(float t)
    {
        int m = (int)(t / 60f);
        int s = (int)(t % 60f);
        return $"{m:00}:{s:00}";
    }
}