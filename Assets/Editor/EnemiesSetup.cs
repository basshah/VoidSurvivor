using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EnemiesSetup
{
    const string SpriteUnlitDefaultGuid = "9dfc825aed78fcd4ba02077103263b40";

    [MenuItem("VoidSurvivor/Setup Enemies & Waves")]
    public static void Setup()
    {
        if (Application.isBatchMode && string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        }

        EnsureFolder("Assets/ScriptableObjects");
        EnsureFolder("Assets/ScriptableObjects/Enemies");
        EnsureFolder("Assets/Prefabs");

        Material unlitMat = null;
        string matPath = AssetDatabase.GUIDToAssetPath(SpriteUnlitDefaultGuid);
        if (!string.IsNullOrEmpty(matPath))
        {
            unlitMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        }

        var swarmerData = CreateOrUpdateEnemyData(
            "Assets/ScriptableObjects/Enemies/Enemy_Swarmer.asset",
            displayName: "Swarmer",
            visualType: EnemyVisualType.Swarmer,
            visualSize: 1.45f,
            hitRadiusRatio: 0.39f,
            maxHealth: 10f,
            moveSpeed: 2.25f,
            contactDamage: 5f,
            xpValue: 1,
            knockbackResistance: 0.05f,
            deathVfxColor: new Color(1f, 0.22f, 0.32f, 1f));

        var stalkerData = CreateOrUpdateEnemyData(
            "Assets/ScriptableObjects/Enemies/Enemy_Stalker.asset",
            displayName: "Stalker",
            visualType: EnemyVisualType.Stalker,
            visualSize: 1.85f,
            hitRadiusRatio: 0.39f,
            maxHealth: 20f,
            moveSpeed: 1.80f,
            contactDamage: 8f,
            xpValue: 2,
            knockbackResistance: 0.25f,
            deathVfxColor: new Color(0.95f, 0.25f, 0.38f, 1f));

        var chargerData = CreateOrUpdateEnemyData(
            "Assets/ScriptableObjects/Enemies/Enemy_Charger.asset",
            displayName: "Charger",
            visualType: EnemyVisualType.Charger,
            visualSize: 1.70f,
            hitRadiusRatio: 0.39f,
            maxHealth: 22f,
            moveSpeed: 3.25f,
            contactDamage: 11f,
            xpValue: 3,
            knockbackResistance: 0.35f,
            deathVfxColor: new Color(1f, 0.58f, 0.18f, 1f));

        var juggernautData = CreateOrUpdateEnemyData(
            "Assets/ScriptableObjects/Enemies/Enemy_Juggernaut.asset",
            displayName: "Juggernaut",
            visualType: EnemyVisualType.Juggernaut,
            visualSize: 2.85f,
            hitRadiusRatio: 0.39f,
            maxHealth: 260f,
            moveSpeed: 1.25f,
            contactDamage: 20f,
            xpValue: 14,
            knockbackResistance: 0.85f,
            deathVfxColor: new Color(0.85f, 0.30f, 1f, 1f));

        var swarmerPrefab = CreateOrUpdateEnemyPrefab("Assets/Prefabs/Enemy_Swarmer.prefab", swarmerData, unlitMat);
        var stalkerPrefab = CreateOrUpdateEnemyPrefab("Assets/Prefabs/Enemy_Stalker.prefab", stalkerData, unlitMat);
        var chargerPrefab = CreateOrUpdateEnemyPrefab("Assets/Prefabs/Enemy_Charger.prefab", chargerData, unlitMat);
        var juggernautPrefab = CreateOrUpdateEnemyPrefab("Assets/Prefabs/Enemy_Juggernaut.prefab", juggernautData, unlitMat);

        var spawner = Object.FindFirstObjectByType<EnemySpawner>(FindObjectsInactive.Include);
        if (spawner != null)
        {
            Undo.RecordObject(spawner, "Setup Enemies & Waves");
            spawner.spawnDistance = 13.8f;
            spawner.startInterval = 1.00f;
            spawner.minInterval = 0.22f;
            spawner.intervalDecayPerMinute = 0.085f;
            spawner.maxAlive = 110;
            spawner.hpGraceSeconds = 45f;
            spawner.hpGrowthPerMinute = 0.22f;
            spawner.hpQuadraticGrowth = 0.11f;
            spawner.damageGrowthPerMinute = 0.12f;

            spawner.swarmRushInterval = 120f;
            spawner.swarmRushDuration = 9f;
            spawner.swarmRushBurstInterval = 0.35f;
            spawner.swarmRushCountPerBurst = 2;

            spawner.entries = new[]
            {
                new SpawnEntry
                {
                    prefab = swarmerPrefab,
                    unlockTime = 0f,
                    maxTime = -1f,
                    weight = 58f,
                    lateWeight = -1f,
                    lateWeightTime = 360f,
                    includeInSwarmRush = true
                },
                new SpawnEntry
                {
                    prefab = stalkerPrefab,
                    unlockTime = 0f,
                    maxTime = -1f,
                    weight = 42f,
                    lateWeight = -1f,
                    lateWeightTime = 360f,
                    includeInSwarmRush = false
                },
                new SpawnEntry
                {
                    prefab = chargerPrefab,
                    unlockTime = 75f,
                    maxTime = -1f,
                    weight = 22f,
                    lateWeight = 30f,
                    lateWeightTime = 240f,
                    includeInSwarmRush = true
                },
                new SpawnEntry
                {
                    prefab = juggernautPrefab,
                    unlockTime = 180f,
                    maxTime = -1f,
                    weight = 8f,
                    lateWeight = 16f,
                    lateWeightTime = 360f,
                    includeInSwarmRush = false
                }
            };

            EditorUtility.SetDirty(spawner);
        }
        else
        {
            Debug.LogWarning("[EnemiesSetup] EnemySpawner not found in active scene.");
        }

        // Balance PlayerStats & switch Player sprite to unlit material for clean SRP batching
        var playerStats = Object.FindFirstObjectByType<PlayerStats>(FindObjectsInactive.Include);
        if (playerStats != null)
        {
            Undo.RecordObject(playerStats, "Balance PlayerStats");
            playerStats.moveSpeed = 4.4f;
            playerStats.maxHealth = 100f;
            playerStats.pickupRange = 2.6f;
            EditorUtility.SetDirty(playerStats);

            var playerSr = playerStats.GetComponent<SpriteRenderer>();
            if (playerSr != null && unlitMat != null)
            {
                Undo.RecordObject(playerSr, "Player Unlit Material");
                playerSr.sharedMaterial = unlitMat;
                EditorUtility.SetDirty(playerSr);
            }

            var moonlets = playerStats.GetComponent<MoonletsSpell>();
            if (moonlets != null) { moonlets.hitKnockback = 0.85f; EditorUtility.SetDirty(moonlets); }
            var pulsar = playerStats.GetComponent<PulsarSpell>();
            if (pulsar != null) { pulsar.beamKnockback = 1.2f; EditorUtility.SetDirty(pulsar); }
            var supernova = playerStats.GetComponent<SupernovaSpell>();
            if (supernova != null) { supernova.blastKnockback = 2.4f; EditorUtility.SetDirty(supernova); }
            var starshot = playerStats.GetComponent<StarshotSpell>();
            if (starshot != null && starshot.projectilePrefab != null)
            {
                var proj = starshot.projectilePrefab.GetComponent<Projectile>();
                if (proj != null) { proj.knockbackForce = 1.1f; EditorUtility.SetDirty(proj); }
            }
        }

        // Balance XPManager progression
        var xpManager = Object.FindFirstObjectByType<XPManager>(FindObjectsInactive.Include);
        if (xpManager != null)
        {
            Undo.RecordObject(xpManager, "Balance XPManager");
            xpManager.baseXP = 6;
            xpManager.xpPerLevel = 6;
            xpManager.maxActiveGems = 220;
            EditorUtility.SetDirty(xpManager);
        }

        // Ensure zero-GC FpsCounter exists in GameUI_Canvas/SafeArea (bottom-left, no HUD overlap)
        EnsureFpsCounter();

        AssetDatabase.SaveAssets();
        Scene activeScene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(activeScene);
        if (Application.isBatchMode)
        {
            EditorSceneManager.SaveScene(activeScene);
        }

        Debug.Log("VoidSurvivor: 4 enemy archetypes, Magic-Survival scale, balance & FpsCounter configured.");
    }

    static void EnsureFpsCounter()
    {
        var gameUI = Object.FindFirstObjectByType<GameUI>(FindObjectsInactive.Include);
        if (gameUI == null) return;

        Transform safeArea = gameUI.transform.Find("SafeArea");
        Transform parent = safeArea != null ? safeArea : gameUI.transform;

        var existing = Object.FindFirstObjectByType<FpsCounter>(FindObjectsInactive.Include);
        GameObject fpsGo;
        if (existing != null)
        {
            fpsGo = existing.gameObject;
            if (fpsGo.transform.parent != parent)
            {
                fpsGo.transform.SetParent(parent, false);
            }
        }
        else
        {
            fpsGo = new GameObject("FpsText", typeof(RectTransform));
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0) fpsGo.layer = uiLayer;
            fpsGo.transform.SetParent(parent, false);
            fpsGo.AddComponent<TextMeshProUGUI>();
            fpsGo.AddComponent<FpsCounter>();
            Undo.RegisterCreatedObjectUndo(fpsGo, "Add FpsCounter");
        }

        var rt = fpsGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.anchoredPosition = new Vector2(24f, 24f);
        rt.sizeDelta = new Vector2(220f, 44f);

        var tmp = fpsGo.GetComponent<TextMeshProUGUI>();
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("UIFont");
        if (font != null) tmp.font = font;
        tmp.text = "FPS 60";
        tmp.fontSize = 30f;
        tmp.color = new Color(0.75f, 1f, 0.75f, 0.85f);
        tmp.alignment = TextAlignmentOptions.BottomLeft;
        tmp.raycastTarget = false;
        EditorUtility.SetDirty(fpsGo);
    }

    static EnemyData CreateOrUpdateEnemyData(
        string assetPath,
        string displayName,
        EnemyVisualType visualType,
        float visualSize,
        float hitRadiusRatio,
        float maxHealth,
        float moveSpeed,
        float contactDamage,
        int xpValue,
        float knockbackResistance,
        Color deathVfxColor)
    {
        var data = AssetDatabase.LoadAssetAtPath<EnemyData>(assetPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<EnemyData>();
            AssetDatabase.CreateAsset(data, assetPath);
        }

        data.displayName = displayName;
        data.visualType = visualType;
        data.visualSize = visualSize;
        data.hitRadiusRatio = hitRadiusRatio;
        data.maxHealth = maxHealth;
        data.moveSpeed = moveSpeed;
        data.contactDamage = contactDamage;
        data.xpValue = xpValue;
        data.knockbackResistance = knockbackResistance;
        data.deathVfxColor = deathVfxColor;
        EditorUtility.SetDirty(data);
        return data;
    }

    static GameObject CreateOrUpdateEnemyPrefab(string prefabPath, EnemyData data, Material unlitMat)
    {
        string goName = Path.GetFileNameWithoutExtension(prefabPath);
        var root = new GameObject(goName);
        var enemy = root.AddComponent<Enemy>();
        enemy.data = data;
        enemy.deathEffectColor = data.deathVfxColor;

        var visualRoot = new GameObject("VisualRoot");
        visualRoot.transform.SetParent(root.transform, false);
        visualRoot.transform.localScale = Vector3.one * data.visualSize;

        var sr = visualRoot.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;
        if (unlitMat != null) sr.sharedMaterial = unlitMat;
        if (data.customSprite != null) sr.sprite = data.customSprite;

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
