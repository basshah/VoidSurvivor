using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class LevelUpSetup
{
    private static readonly string[] IconKeys =
    {
        "attack", "heart", "shield", "cooldown",
        "spell_starshot", "spell_moonlets", "spell_pulsar", "spell_supernova"
    };

    [MenuItem("VoidSurvivor/Setup Level-Up + Passives")]
    public static void SetupAll()
    {
        GameUI gameUI = Object.FindFirstObjectByType<GameUI>(FindObjectsInactive.Include);
        Canvas canvas = gameUI != null
            ? gameUI.GetComponentInParent<Canvas>()
            : Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[LevelUpSetup] Səhnədə GameUI_Canvas (və ya Canvas) tapılmadı! Əvvəl UIBuilder işlət.");
            return;
        }

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("UIFont");
        if (font == null)
        {
            Debug.LogWarning("[LevelUpSetup] Resources/UIFont.asset tapılmadı, standart şrift işlənəcək.");
        }

        // Köhnə LevelUpPanel varsa təmizlə (hər hansı Canvas altında)
        LevelUpUI oldPanel = Object.FindFirstObjectByType<LevelUpUI>(FindObjectsInactive.Include);
        if (oldPanel != null)
        {
            Object.DestroyImmediate(oldPanel.gameObject);
        }

        // 1. LevelUpPanel (tam ekran tünd fon)
        GameObject panelGO = CreateUIObject("LevelUpPanel", canvas.transform);
        StretchFull(panelGO.GetComponent<RectTransform>());
        Image backdrop = panelGO.AddComponent<Image>();
        backdrop.color = new Color(0.02f, 0.03f, 0.06f, 0.90f);
        backdrop.raycastTarget = true;

        LevelUpUI levelUpUI = panelGO.AddComponent<LevelUpUI>();

        // 2. Konteyner (mərkəzdə portret sütun)
        GameObject containerGO = CreateUIObject("Container", panelGO.transform);
        RectTransform containerRT = containerGO.GetComponent<RectTransform>();
        containerRT.anchorMin = new Vector2(0.06f, 0.12f);
        containerRT.anchorMax = new Vector2(0.94f, 0.88f);
        containerRT.offsetMin = Vector2.zero;
        containerRT.offsetMax = Vector2.zero;

        VerticalLayoutGroup vlg = containerGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 28f;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // 3. Başlıq
        TextMeshProUGUI header = CreateText("HeaderText", containerGO.transform, "LEVEL UP", 70, font, Color.white, TextAlignmentOptions.Center);
        header.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 90);

        TextMeshProUGUI subHeader = CreateText("SubHeaderText", containerGO.transform, "Choose an upgrade", 40, font, new Color(0.65f, 0.72f, 0.85f, 1f), TextAlignmentOptions.Center);
        subHeader.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 50);

        // 4. 3 Kart
        SerializedObject uiSO = new SerializedObject(levelUpUI);
        uiSO.FindProperty("panelRoot").objectReferenceValue = panelGO;
        SerializedProperty cardsProp = uiSO.FindProperty("cards");
        cardsProp.arraySize = 3;

        for (int i = 0; i < 3; i++)
        {
            BuildCard(containerGO.transform, i, font, cardsProp.GetArrayElementAtIndex(i));
        }

        PopulateIcons(uiSO);
        uiSO.ApplyModifiedPropertiesWithoutUndo();

        panelGO.SetActive(false);

        // 5. UpgradeManager qur və bağla
        UpgradeManager manager = Object.FindFirstObjectByType<UpgradeManager>();
        if (manager == null)
        {
            GameObject mgrHost = GameObject.Find("GameManager");
            if (mgrHost == null) mgrHost = new GameObject("UpgradeManager");
            manager = mgrHost.AddComponent<UpgradeManager>();
        }

        SerializedObject mgrSO = new SerializedObject(manager);
        mgrSO.FindProperty("xpManager").objectReferenceValue = Object.FindFirstObjectByType<XPManager>();
        mgrSO.FindProperty("playerStats").objectReferenceValue = Object.FindFirstObjectByType<PlayerStats>();
        mgrSO.FindProperty("playerHealth").objectReferenceValue = Object.FindFirstObjectByType<PlayerHealth>();
        mgrSO.FindProperty("levelUpUI").objectReferenceValue = levelUpUI;
        mgrSO.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[LevelUpSetup] Level-Up UI və UpgradeManager uğurla quruldu!");
    }

    [MenuItem("VoidSurvivor/Refresh Level-Up Icons")]
    public static void RefreshIconsOnly()
    {
        LevelUpUI levelUpUI = Object.FindFirstObjectByType<LevelUpUI>(FindObjectsInactive.Include);
        if (levelUpUI == null)
        {
            Debug.LogError("[LevelUpSetup] LevelUpUI tapılmadı. Əvvəl Setup Level-Up + Passives işlət.");
            return;
        }

        SerializedObject uiSO = new SerializedObject(levelUpUI);
        PopulateIcons(uiSO);
        uiSO.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[LevelUpSetup] Level-Up ikonları yeniləndi.");
    }

    private static void BuildCard(Transform parent, int index, TMP_FontAsset font, SerializedProperty cardProp)
    {
        // Xarici çərçivə (Border)
        GameObject cardGO = CreateUIObject($"Card_{index + 1}", parent);
        RectTransform cardRT = cardGO.GetComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(0, 250);

        Image borderImg = cardGO.AddComponent<Image>();
        borderImg.color = new Color(0.28f, 0.38f, 0.56f, 1f);

        Button btn = cardGO.AddComponent<Button>();
        btn.targetGraphic = borderImg;

        // Daxili fon (6px çərçivə qalınlığı)
        GameObject bgGO = CreateUIObject("Background", cardGO.transform);
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        StretchFull(bgRT);
        bgRT.offsetMin = new Vector2(6, 6);
        bgRT.offsetMax = new Vector2(-6, -6);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.10f, 0.16f, 1f);
        bgImg.raycastTarget = false;

        // İkon qutusu (sol tərəf)
        GameObject iconFrameGO = CreateUIObject("IconFrame", bgGO.transform);
        RectTransform iconFrameRT = iconFrameGO.GetComponent<RectTransform>();
        iconFrameRT.anchorMin = new Vector2(0, 0.5f);
        iconFrameRT.anchorMax = new Vector2(0, 0.5f);
        iconFrameRT.pivot = new Vector2(0, 0.5f);
        iconFrameRT.anchoredPosition = new Vector2(28, 0);
        iconFrameRT.sizeDelta = new Vector2(140, 140);
        Image iconFrameImg = iconFrameGO.AddComponent<Image>();
        iconFrameImg.color = new Color(0.04f, 0.05f, 0.09f, 1f);
        iconFrameImg.raycastTarget = false;

        GameObject iconGO = CreateUIObject("Icon", iconFrameGO.transform);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        StretchFull(iconRT);
        iconRT.offsetMin = new Vector2(10, 10);
        iconRT.offsetMax = new Vector2(-10, -10);
        Image iconImg = iconGO.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        // Sağ mətn sahəsi
        GameObject textAreaGO = CreateUIObject("TextArea", bgGO.transform);
        RectTransform textRT = textAreaGO.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0, 0);
        textRT.anchorMax = new Vector2(1, 1);
        textRT.offsetMin = new Vector2(196, 20);
        textRT.offsetMax = new Vector2(-24, -20);

        TextMeshProUGUI titleTMP = CreateText("Title", textAreaGO.transform, "Upgrade Name", 50, font, Color.white, TextAlignmentOptions.TopLeft);
        RectTransform titleRT = titleTMP.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0.55f);
        titleRT.anchorMax = new Vector2(0.68f, 1f);
        titleRT.offsetMin = Vector2.zero;
        titleRT.offsetMax = Vector2.zero;

        TextMeshProUGUI levelTMP = CreateText("LevelTag", textAreaGO.transform, "LV 1 -> 2", 40, font, new Color(0.55f, 0.85f, 1f, 1f), TextAlignmentOptions.TopRight);
        RectTransform levelRT = levelTMP.GetComponent<RectTransform>();
        levelRT.anchorMin = new Vector2(0.65f, 0.55f);
        levelRT.anchorMax = new Vector2(1f, 1f);
        levelRT.offsetMin = Vector2.zero;
        levelRT.offsetMax = Vector2.zero;

        TextMeshProUGUI descTMP = CreateText("Description", textAreaGO.transform, "Upgrade description text", 40, font, new Color(0.80f, 0.84f, 0.90f, 1f), TextAlignmentOptions.BottomLeft);
        descTMP.textWrappingMode = TextWrappingModes.Normal;
        RectTransform descRT = descTMP.GetComponent<RectTransform>();
        descRT.anchorMin = new Vector2(0, 0f);
        descRT.anchorMax = new Vector2(1f, 0.56f);
        descRT.offsetMin = Vector2.zero;
        descRT.offsetMax = Vector2.zero;

        // Evolution etiketi (yuxarı sol künçdə qızılı lent)
        GameObject evoBadgeGO = CreateUIObject("EvolutionBadge", cardGO.transform);
        RectTransform evoRT = evoBadgeGO.GetComponent<RectTransform>();
        evoRT.anchorMin = new Vector2(0, 1);
        evoRT.anchorMax = new Vector2(0, 1);
        evoRT.pivot = new Vector2(0, 0.5f);
        evoRT.anchoredPosition = new Vector2(24, 0);
        evoRT.sizeDelta = new Vector2(260, 44);
        Image evoImg = evoBadgeGO.AddComponent<Image>();
        evoImg.color = new Color(1f, 0.82f, 0.24f, 1f);
        evoImg.raycastTarget = false;

        TextMeshProUGUI evoText = CreateText("EvoText", evoBadgeGO.transform, "EVOLUTION", 30, font, new Color(0.08f, 0.06f, 0.02f, 1f), TextAlignmentOptions.Center);
        StretchFull(evoText.GetComponent<RectTransform>());
        evoBadgeGO.SetActive(false);

        cardProp.FindPropertyRelative("root").objectReferenceValue = cardGO;
        cardProp.FindPropertyRelative("button").objectReferenceValue = btn;
        cardProp.FindPropertyRelative("borderImage").objectReferenceValue = borderImg;
        cardProp.FindPropertyRelative("backgroundImage").objectReferenceValue = bgImg;
        cardProp.FindPropertyRelative("iconImage").objectReferenceValue = iconImg;
        cardProp.FindPropertyRelative("titleText").objectReferenceValue = titleTMP;
        cardProp.FindPropertyRelative("levelText").objectReferenceValue = levelTMP;
        cardProp.FindPropertyRelative("descText").objectReferenceValue = descTMP;
        cardProp.FindPropertyRelative("evolutionBadge").objectReferenceValue = evoBadgeGO;
    }

    private static void PopulateIcons(SerializedObject uiSO)
    {
        SerializedProperty iconsProp = uiSO.FindProperty("icons");
        iconsProp.arraySize = IconKeys.Length;

        for (int i = 0; i < IconKeys.Length; i++)
        {
            SerializedProperty entry = iconsProp.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("key").stringValue = IconKeys[i];
            entry.FindPropertyRelative("sprite").objectReferenceValue = FindExactSprite(IconKeys[i]);
        }
    }

    private static Sprite FindExactSprite(string exactName)
    {
        string[] guids = AssetDatabase.FindAssets(exactName + " t:Sprite", new[] { "Assets" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path).Equals(exactName, System.StringComparison.OrdinalIgnoreCase))
            {
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
        }
        return null;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer >= 0) go.layer = uiLayer;
        go.transform.SetParent(parent, false);
        return go;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, TMP_FontAsset font, Color color, TextAlignmentOptions align)
    {
        GameObject go = CreateUIObject(name, parent);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        if (font != null) tmp.font = font;
        return tmp;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}