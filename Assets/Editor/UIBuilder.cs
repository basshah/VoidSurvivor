using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class UIBuilder
{
    static readonly Color Clear = new Color(0f, 0f, 0f, 0f);
    static readonly Color Yellow = new Color(1f, 0.85f, 0.25f);
    static readonly Color Orange = new Color(1f, 0.5f, 0.15f);
    static readonly Color Green = new Color(0.55f, 1f, 0.3f);
    static readonly Color Gray = new Color(0.6f, 0.6f, 0.6f);

    const string StageName = "Deep Space";
    const float RowH = 150f;

    static TMP_FontAsset font;

    // ================= MENYU =================
    [MenuItem("VoidSurvivor/Build UI")]
    static void Build()
    {
        var health = Object.FindFirstObjectByType<PlayerHealth>();
        var stats = Object.FindFirstObjectByType<PlayerStats>();
        if (health == null || stats == null)
        {
            EditorUtility.DisplayDialog("Build UI",
                "Səhnədə Player tapılmadı (PlayerHealth və PlayerStats lazımdır).", "OK");
            return;
        }

        var old = Object.FindObjectsByType<GameUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (old.Length > 0)
        {
            if (!EditorUtility.DisplayDialog("Build UI",
                "Səhnədə artıq GameUI var. O silinib yenidən qurulacaq və əl ilə etdiyin dəyişikliklər itəcək. Davam edək?",
                "Davam et", "Ləğv et")) return;
            foreach (var g in old) Undo.DestroyObjectImmediate(g.gameObject);
        }

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Resources/UIFont.asset");
        DamageTracker.Reset();

        var canvasGo = new GameObject("GameUI_Canvas", typeof(RectTransform), typeof(Canvas),
                                      typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.layer = LayerMask.NameToLayer("UI");
        Undo.RegisterCreatedObjectUndo(canvasGo, "Build UI");

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        var ui = canvasGo.AddComponent<GameUI>();

        BuildHud(ui, canvas.transform);
        BuildPause(ui, canvas.transform, stats, health);
        BuildRecordScreen(ui, canvas.transform);
        BuildGameOver(ui, canvas.transform);
        BuildInfo(ui, canvas.transform, stats, health);

        ui.pausePanel.SetActive(false);
        ui.recordPanel.SetActive(false);
        ui.gameOverPanel.SetActive(false);
        ui.infoPanel.SetActive(false);

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem),
                typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(es, "Build UI");
        }

        Selection.activeGameObject = canvasGo;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("VoidSurvivor: UI quruldu. Panellər gizlidir, redaktə üçün Inspector-da aktiv et.");
    }

    [MenuItem("VoidSurvivor/Refresh UI Icons")]
    static void RefreshIcons()
    {
        var ui = Object.FindFirstObjectByType<GameUI>(FindObjectsInactive.Include);
        if (ui == null) { Debug.LogWarning("GameUI tapılmadı."); return; }

        int n = 0;
        foreach (var img in ui.GetComponentsInChildren<Image>(true))
        {
            if (!img.name.StartsWith("Icon_")) continue;
            var sp = FindSprite(img.name.Substring(5));
            if (sp == null) continue;
            Undo.RecordObject(img, "Refresh UI Icons");
            img.sprite = sp;
            img.color = Color.white;
            img.preserveAspect = true;
            n++;
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("VoidSurvivor: " + n + " ikon yeniləndi.");
    }

    // ================= HUD =================
    static void BuildHud(GameUI ui, Transform canvas)
    {
        var safe = SafeArea(canvas);

        var xpBg = Img("XPBar", safe, new Color(0.05f, 0.1f, 0.2f, 0.9f));
        TopBar(xpBg.rectTransform, 18f);
        ui.xpFill = FillRect(xpBg.transform, new Color(0.2f, 0.6f, 1f));
        SetFill(ui.xpFill, 0.35f); // dizayn zamanı görünsün, oyun başlayanda 0 olur

        ui.levelText = Txt("Level", safe, "Lv.1", 40f, Color.white, TextAlignmentOptions.Left);
        Place(ui.levelText.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -28f), new Vector2(300f, 56f));

        var skull = Icon("skull", safe, Orange);
        Place(skull.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -90f), new Vector2(44f, 44f));
        ui.killText = Txt("Kills", safe, "0", 40f, Orange, TextAlignmentOptions.Left);
        Place(ui.killText.rectTransform, new Vector2(0f, 1f), new Vector2(80f, -84f), new Vector2(300f, 56f));

        ui.timerText = Txt("Timer", safe, "00:00", 72f, Color.white);
        Place(ui.timerText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(400f, 96f));

        var pause = Btn("PauseButton", safe, "", 10f, new Color(0f, 0f, 0f, 0.45f));
        Place((RectTransform)pause.transform, new Vector2(1f, 1f), new Vector2(-24f, -36f), new Vector2(100f, 100f));
        AddBar(pause.transform, -16f, 14f, 48f);
        AddBar(pause.transform, 16f, 14f, 48f);
        UnityEventTools.AddPersistentListener(pause.onClick, ui.Pause);
    }

    // ================= PAUZA =================
    static void BuildPause(GameUI ui, Transform canvas, PlayerStats stats, PlayerHealth health)
    {
        var panel = Overlay(canvas, "PausePanel", 0.93f);
        ui.pausePanel = panel.gameObject;
        var c = SafeArea(panel.transform);

        Label(c, "PAUSE", 96f, Color.white, -300f, 130f);
        ui.pauseLevel = Label(c, "Current Level : 1", 46f, Yellow, -470f, 64f);
        ui.pauseTime = Label(c, "Time Survived 00:00", 46f, Orange, -540f, 64f);
        ui.pauseKills = Label(c, "Enemies killed 0", 46f, Orange, -610f, 64f);

        ui.pauseStats = BuildStats(c, stats, health, new Vector2(0f, -780f));

        var resume = Btn("ResumeButton", c, "Resume Game", 76f, Clear);
        Place((RectTransform)resume.transform, new Vector2(0.5f, 0f), new Vector2(0f, 330f), new Vector2(800f, 130f));
        UnityEventTools.AddPersistentListener(resume.onClick, ui.Resume);

        var restart = Btn("RestartButton", c, "Restart", 50f, Clear);
        restart.GetComponentInChildren<TextMeshProUGUI>().color = Gray;
        Place((RectTransform)restart.transform, new Vector2(0.5f, 0f), new Vector2(0f, 190f), new Vector2(500f, 100f));
        UnityEventTools.AddPersistentListener(restart.onClick, ui.Restart);

        var rec = Btn("RecordButton", c, "", 10f, Clear);
        Place((RectTransform)rec.transform, new Vector2(1f, 1f), new Vector2(-30f, -120f), new Vector2(120f, 120f));
        AddBar(rec.transform, -30f, 22f, 30f, -25f);
        AddBar(rec.transform, 0f, 22f, 55f, -12.5f);
        AddBar(rec.transform, 30f, 22f, 80f, 0f);
        UnityEventTools.AddPersistentListener(rec.onClick, ui.OpenRecord);
    }

    // ================= COMBAT RECORD =================
    static void BuildRecordScreen(GameUI ui, Transform canvas)
    {
        var panel = Overlay(canvas, "RecordPanel", 0.97f);
        ui.recordPanel = panel.gameObject;
        var c = SafeArea(panel.transform);

        Label(c, "Combat Record", 84f, Color.white, -260f, 120f);
        Label(c, StageName, 40f, Gray, -400f, 60f);
        ui.pauseRecord = BuildRecord(c, new Vector2(0f, -520f));
        CloseButton(c, ui.CloseRecord);
    }

    // ================= GAME OVER =================
    static void BuildGameOver(GameUI ui, Transform canvas)
    {
        var panel = Overlay(canvas, "GameOverPanel", 1f);
        ui.gameOverPanel = panel.gameObject;
        var c = SafeArea(panel.transform);

        Label(c, "MISSION FAILED", 90f, Color.white, -330f, 130f);
        var line = Img("Line", c, new Color(1f, 1f, 1f, 0.25f));
        Place(line.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -480f), new Vector2(900f, 3f));
        Label(c, StageName + " (Normal)", 40f, new Color(0.95f, 0.8f, 0.8f), -510f, 60f);

        ui.overTime = ResultRow(c, "clock", Green, "00:00", -620f);
        ui.overKills = ResultRow(c, "skull", Orange, "0", -720f);

        ui.overRecord = BuildRecord(c, new Vector2(0f, -850f));

        var restart = Btn("RestartButton", c, "Restart", 76f, Clear);
        Place((RectTransform)restart.transform, new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(700f, 130f));
        UnityEventTools.AddPersistentListener(restart.onClick, ui.Restart);

        var who = Btn("CharacterButton", c, "", 10f, Clear);
        Place((RectTransform)who.transform, new Vector2(1f, 1f), new Vector2(-30f, -120f), new Vector2(120f, 120f));
        AddBar(who.transform, 0f, 34f, 34f, 28f);
        AddBar(who.transform, 0f, 70f, 44f, -18f);
        UnityEventTools.AddPersistentListener(who.onClick, ui.OpenInfo);
    }

    // ================= CHARACTER INFO =================
    static void BuildInfo(GameUI ui, Transform canvas, PlayerStats stats, PlayerHealth health)
    {
        var panel = Overlay(canvas, "InfoPanel", 1f);
        ui.infoPanel = panel.gameObject;
        var c = SafeArea(panel.transform);

        Label(c, "Character Info", 84f, Color.white, -300f, 120f);
        ui.infoLevel = Label(c, "Character Level : 1", 46f, Yellow, -450f, 64f);
        ui.infoStats = BuildStats(c, stats, health, new Vector2(0f, -640f));
        CloseButton(c, ui.CloseInfo);
    }

    // ================= STAT CƏDVƏLİ =================
    static StatsPanel BuildStats(Transform parent, PlayerStats stats, PlayerHealth health, Vector2 pos)
    {
        var root = Node("Stats", parent);
        Place(root, new Vector2(0.5f, 1f), pos, new Vector2(1060f, 240f));

        var sp = root.gameObject.AddComponent<StatsPanel>();
        sp.stats = stats;
        sp.health = health;

        sp.hpValue = StatRow(root, 0, 0, "HP", "heart", new Color(1f, 0.25f, 0.3f), "100 / 100");
        sp.damageTakenValue = StatRow(root, 0, 1, "Damage Taken", "shield", new Color(0.6f, 0.8f, 1f), "0%");
        sp.moveSpeedValue = StatRow(root, 0, 2, "Move Speed", "speed", Color.white, "3.0");
        sp.spellDamageValue = StatRow(root, 1, 0, "Spell Damage", "attack", new Color(0.3f, 0.9f, 1f), "0%");
        sp.cooldownValue = StatRow(root, 1, 1, "Spell Cooldown", "cooldown", new Color(0.8f, 0.7f, 1f), "0%");
        sp.pickupValue = StatRow(root, 1, 2, "Pickup Range", "magnet", new Color(1f, 0.8f, 0.3f), "1.5");
        return sp;
    }

    static TMP_Text StatRow(RectTransform root, int col, int r, string label, string icon, Color iconColor, string sample)
    {
        var row = Node("Row_" + label, root);
        Place(row, new Vector2(0f, 1f), new Vector2(col * 530f, -r * 80f), new Vector2(500f, 70f));

        var ic = Icon(icon, row, iconColor);
        Place(ic.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(48f, 48f));

        var lb = Txt("Label", row, label, 32f, Color.white, TextAlignmentOptions.Left);
        Place(lb.rectTransform, new Vector2(0f, 0.5f), new Vector2(64f, 0f), new Vector2(300f, 56f));

        var v = Txt("Value", row, sample, 32f, Green, TextAlignmentOptions.Right);
        Place(v.rectTransform, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(220f, 56f));
        return v;
    }

    // ================= ZƏRƏR CƏDVƏLİ =================
    static RecordPanel BuildRecord(Transform parent, Vector2 pos)
    {
        int n = DamageTracker.All.Count;

        var root = Node("Record", parent);
        Place(root, new Vector2(0.5f, 1f), pos, new Vector2(900f, n * RowH + 40f));

        var card = Img("Card", root, new Color(0.06f, 0.06f, 0.07f, 1f));
        Stretch(card.rectTransform);

        var rowsRt = Node("Rows", root);
        Stretch(rowsRt);
        var vlg = rowsRt.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(30, 30, 20, 20);
        vlg.spacing = 10f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;

        var rp = root.gameObject.AddComponent<RecordPanel>();
        rp.container = rowsRt;

        for (int i = 0; i < n; i++)
        {
            var e = DamageTracker.All[i];

            var row = Node("Row_" + e.name, rowsRt);
            row.sizeDelta = new Vector2(840f, RowH - 10f);

            var icon = Icon("spell_" + e.id, row, e.color);
            Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 8f), new Vector2(90f, 90f));

            var label = Txt("Name", row, e.name, 40f, e.color, TextAlignmentOptions.Left);
            Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(110f, -6f), new Vector2(400f, 52f));

            var value = Txt("Value", row, "0", 40f, Yellow, TextAlignmentOptions.Right);
            Place(value.rectTransform, new Vector2(1f, 1f), new Vector2(0f, -6f), new Vector2(400f, 52f));

            var barBg = Img("BarBG", row, new Color(1f, 1f, 1f, 0.12f));
            Place(barBg.rectTransform, new Vector2(0f, 0f), new Vector2(110f, 14f), new Vector2(730f, 18f));
            var fill = FillRect(barBg.transform, e.color);
            SetFill(fill, 1f - i * 0.25f); // dizayn zamanı görünsün

            rp.rows.Add(new RecordPanel.Row { id = e.id, rect = row, value = value, fill = fill });
        }
        return rp;
    }

    // ================= KÖMƏKÇİLƏR =================
    static RectTransform Node(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // Anchor və pivot eynidir: pos = elementin həmin küncünün anchor-dan məsafəsi
    static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void TopBar(RectTransform rt, float height)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(0f, -height);
        rt.offsetMax = Vector2.zero;
    }

    static RectTransform SafeArea(Transform parent)
    {
        var rt = Node("SafeArea", parent);
        Stretch(rt);
        rt.gameObject.AddComponent<SafeAreaFitter>();
        return rt;
    }

    static Image Img(string name, Transform parent, Color color, bool raycast = false)
    {
        var rt = Node(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = raycast;
        return img;
    }

    static Image Overlay(Transform canvas, string name, float alpha)
    {
        var bg = Img(name, canvas, new Color(0f, 0f, 0f, alpha), true);
        Stretch(bg.rectTransform);
        return bg;
    }

    static TextMeshProUGUI Txt(string name, Transform parent, string text, float size, Color color,
                               TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var rt = Node(name, parent);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        return t;
    }

    static TextMeshProUGUI Label(RectTransform parent, string text, float size, Color color, float y, float h)
    {
        var t = Txt("Label", parent, text, size, color);
        Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(1000f, h));
        return t;
    }

    static TextMeshProUGUI ResultRow(RectTransform parent, string icon, Color color, string sample, float y)
    {
        var ic = Icon(icon, parent, color);
        Place(ic.rectTransform, new Vector2(0.5f, 1f), new Vector2(-130f, y), new Vector2(64f, 64f));
        var t = Txt("Value", parent, sample, 64f, color, TextAlignmentOptions.Left);
        Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(130f, y), new Vector2(400f, 64f));
        return t;
    }

    static Button Btn(string name, Transform parent, string label, float fontSize, Color bg)
    {
        var img = Img(name, parent, bg, true);
        var b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        if (!string.IsNullOrEmpty(label))
        {
            var t = Txt("Label", img.transform, label, fontSize, Color.white);
            Stretch(t.rectTransform);
        }
        return b;
    }

    static void CloseButton(RectTransform parent, UnityAction onClick)
    {
        var b = Btn("CloseButton", parent, "X", 90f, Clear);
        Place((RectTransform)b.transform, new Vector2(1f, 1f), new Vector2(-30f, -120f), new Vector2(130f, 130f));
        UnityEventTools.AddPersistentListener(b.onClick, onClick);
    }

    static void AddBar(Transform parent, float x, float w, float h, float y = 0f)
    {
        var b = Img("Shape", parent, Color.white);
        Place(b.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(w, h));
    }

    static RectTransform FillRect(Transform parent, Color color)
    {
        var img = Img("Fill", parent, color);
        Stretch(img.rectTransform);
        return img.rectTransform;
    }

    static void SetFill(RectTransform rt, float v)
    {
        rt.anchorMax = new Vector2(Mathf.Clamp01(v), 1f);
    }

    // Assets içində hər yerdə <ad>.png (Sprite tipli) axtarır
    static Sprite FindSprite(string name)
    {
        foreach (var guid in AssetDatabase.FindAssets(name + " t:Sprite", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == name)
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return null;
    }

    static Image Icon(string name, Transform parent, Color fallback)
    {
        var img = Img("Icon_" + name, parent, fallback);
        var sp = FindSprite(name);
        if (sp != null)
        {
            img.sprite = sp;
            img.color = Color.white;
            img.preserveAspect = true;
        }
        return img;
    }
}