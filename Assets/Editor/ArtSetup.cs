using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ArtSetup
{
    public const string IconsFolder = "Assets/Art/Icons";
    public const string EnvironmentTilePath = "Assets/Art/Environment/space_tile.png";
    const string SpriteUnlitDefaultGuid = "9dfc825aed78fcd4ba02077103263b40";
    const float PlayerVisualSize = 0.95f;
    const float SpaceTilePpu = 128f;

    static readonly string[] LevelUpIconKeys =
    {
        "attack", "heart", "shield", "cooldown",
        "spell_starshot", "spell_moonlets", "spell_pulsar", "spell_supernova"
    };

    struct CharacterArtSpec
    {
        public string name;
        public string artFolder;
        public float ppu;
        public string dataPath;
        public string prefabPath;
    }

    static readonly CharacterArtSpec[] EnemySpecs =
    {
        new CharacterArtSpec
        {
            name = "Swarmer",
            artFolder = "Assets/Art/Enemies/Swarmer",
            ppu = 48f,
            dataPath = "Assets/ScriptableObjects/Enemies/Enemy_Swarmer.asset",
            prefabPath = "Assets/Prefabs/Enemy_Swarmer.prefab"
        },
        new CharacterArtSpec
        {
            name = "Stalker",
            artFolder = "Assets/Art/Enemies/Stalker",
            ppu = 64f,
            dataPath = "Assets/ScriptableObjects/Enemies/Enemy_Stalker.asset",
            prefabPath = "Assets/Prefabs/Enemy_Stalker.prefab"
        },
        new CharacterArtSpec
        {
            name = "Charger",
            artFolder = "Assets/Art/Enemies/Charger",
            ppu = 64f,
            dataPath = "Assets/ScriptableObjects/Enemies/Enemy_Charger.asset",
            prefabPath = "Assets/Prefabs/Enemy_Charger.prefab"
        },
        new CharacterArtSpec
        {
            name = "Juggernaut",
            artFolder = "Assets/Art/Enemies/Juggernaut",
            ppu = 96f,
            dataPath = "Assets/ScriptableObjects/Enemies/Enemy_Juggernaut.asset",
            prefabPath = "Assets/Prefabs/Enemy_Juggernaut.prefab"
        }
    };

    [MenuItem("VoidSurvivor/Refresh All Art & Icons")]
    public static void RefreshAll()
    {
        if (Application.isBatchMode && string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        }

        ExtractCharacterAndEnemyZips();
        ConfigureCharacterAndEnemyImporters();
        ConfigureAndWireSpaceBackground();
        WirePlayerAndEnemies();
        WireAllIcons();

        AssetDatabase.SaveAssets();
        Scene activeScene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(activeScene);
        if (Application.isBatchMode)
        {
            EditorSceneManager.SaveScene(activeScene);
        }

        Debug.Log("[ArtSetup] Art extraction, pixel-perfect imports, 8-dir wiring, space_tile & icons complete.");
    }

    static void ExtractCharacterAndEnemyZips()
    {
        var folders = new List<string> { "Assets/Art/Characters/Player" };
        for (int i = 0; i < EnemySpecs.Length; i++) folders.Add(EnemySpecs[i].artFolder);

        bool extractedAny = false;
        for (int f = 0; f < folders.Count; f++)
        {
            string folder = folders[f];
            if (!Directory.Exists(folder)) continue;

            string[] zips = Directory.GetFiles(folder, "*.zip", SearchOption.TopDirectoryOnly);
            for (int z = 0; z < zips.Length; z++)
            {
                string zipPath = zips[z];
                using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name)) continue;
                        string norm = entry.FullName.Replace('\\', '/').ToLowerInvariant();
                        if (!norm.EndsWith(".png")) continue;
                        if (!norm.Contains("rotations/") && norm.Contains("/")) continue;

                        string destPath = Path.Combine(folder, entry.Name);
                        entry.ExtractToFile(destPath, overwrite: true);
                        extractedAny = true;
                    }
                }

                File.Delete(zipPath);
                string meta = zipPath + ".meta";
                if (File.Exists(meta)) File.Delete(meta);
            }
        }

        if (extractedAny)
        {
            AssetDatabase.Refresh();
        }
    }

    static void ConfigureCharacterAndEnemyImporters()
    {
        ConfigureFolderSprites("Assets/Art/Characters/Player", 48f);
        for (int i = 0; i < EnemySpecs.Length; i++)
        {
            ConfigureFolderSprites(EnemySpecs[i].artFolder, EnemySpecs[i].ppu);
        }
    }

    static void ConfigureFolderSprites(string folder, float ppu)
    {
        if (!AssetDatabase.IsValidFolder(folder)) return;

        string[] pngFiles = Directory.GetFiles(folder, "*.png", SearchOption.TopDirectoryOnly);
        for (int i = 0; i < pngFiles.Length; i++)
        {
            string assetPath = pngFiles[i].Replace('\\', '/');
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) continue;

            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; dirty = true; }
            if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; dirty = true; }
            if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; dirty = true; }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; dirty = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }
            if (importer.isReadable) { importer.isReadable = false; dirty = true; }
            if (!Mathf.Approximately(importer.spritePixelsPerUnit, ppu)) { importer.spritePixelsPerUnit = ppu; dirty = true; }

            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }
    }

    public static void ConfigureAndWireSpaceBackground()
    {
        if (!File.Exists(EnvironmentTilePath))
        {
            Debug.LogWarning($"[ArtSetup] Missing space tile texture at {EnvironmentTilePath}.");
            return;
        }

        AssetDatabase.ImportAsset(EnvironmentTilePath, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(EnvironmentTilePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spritePixelsPerUnit = SpaceTilePpu; // 1024px / 128 = 8.0 world units per tile
            settings.filterMode = FilterMode.Point;
            settings.wrapMode = TextureWrapMode.Repeat;
            settings.mipmapEnabled = false;
            settings.readable = false;
            importer.SetTextureSettings(settings);
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        Sprite spaceSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvironmentTilePath);
        Texture2D spaceTex = spaceSprite != null
            ? spaceSprite.texture
            : AssetDatabase.LoadAssetAtPath<Texture2D>(EnvironmentTilePath);
        Material unlitMat = LoadUnlitSpriteMaterial();

        float tileSize = spaceSprite != null && spaceSprite.bounds.size.x > 0.001f
            ? spaceSprite.bounds.size.x
            : (spaceTex != null ? spaceTex.width / SpaceTilePpu : 8f);

        var bgFollow = UnityEngine.Object.FindFirstObjectByType<BackgroundFollow>(FindObjectsInactive.Include);
        if (bgFollow != null)
        {
            bgFollow.tileSize = tileSize;
            var sr = bgFollow.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (spaceSprite != null) sr.sprite = spaceSprite;
                sr.color = Color.white;
                sr.drawMode = SpriteDrawMode.Tiled;
                float tiledExtent = Mathf.Max(32f, tileSize * 4f);
                sr.size = new Vector2(tiledExtent, tiledExtent);
                sr.sortingOrder = -100;
                if (unlitMat != null) sr.sharedMaterial = unlitMat;
                EditorUtility.SetDirty(sr);
            }
            EditorUtility.SetDirty(bgFollow);
        }
    }

    static void WirePlayerAndEnemies()
    {
        Material unlitMat = LoadUnlitSpriteMaterial();

        // 1. Player VisualRoot
        var playerStats = UnityEngine.Object.FindFirstObjectByType<PlayerStats>(FindObjectsInactive.Include);
        if (playerStats != null)
        {
            Transform visual = playerStats.transform.Find("VisualRoot");
            if (visual != null)
            {
                var dv = visual.GetComponent<DirectionalVisual>();
                if (dv == null) dv = Undo.AddComponent<DirectionalVisual>(visual.gameObject);

                Sprite[] playerSprites = Load8DirectionSprites("Assets/Art/Characters/Player");
                if (HasAll8(playerSprites))
                {
                    dv.sprites = playerSprites;
                    dv.RefreshSpriteCompleteness();

                    var sr = visual.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sprite = playerSprites[6];
                        sr.color = Color.white;
                        if (unlitMat != null) sr.sharedMaterial = unlitMat;
                        EditorUtility.SetDirty(sr);
                    }

                    if (playerSprites[6] != null && playerSprites[6].bounds.size.x > 0.001f)
                    {
                        float calibratedScale = PlayerVisualSize / playerSprites[6].bounds.size.x;
                        visual.localScale = Vector3.one * calibratedScale;
                        EditorUtility.SetDirty(visual);
                    }
                }
                EditorUtility.SetDirty(dv);
            }
        }

        // 2. Four Enemy archetypes
        for (int i = 0; i < EnemySpecs.Length; i++)
        {
            CharacterArtSpec spec = EnemySpecs[i];
            Sprite[] sprites = Load8DirectionSprites(spec.artFolder);
            if (!HasAll8(sprites)) continue;

            EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(spec.dataPath);
            if (data != null)
            {
                data.directionalSprites = sprites;
                data.customSprite = sprites[6];
                EditorUtility.SetDirty(data);
            }

            if (File.Exists(spec.prefabPath))
            {
                using (var editScope = new PrefabUtility.EditPrefabContentsScope(spec.prefabPath))
                {
                    GameObject root = editScope.prefabContentsRoot;
                    Transform visual = root.transform.Find("VisualRoot");
                    if (visual != null)
                    {
                        var sr = visual.GetComponent<SpriteRenderer>();
                        if (sr != null)
                        {
                            sr.sprite = sprites[6];
                            sr.color = Color.white;
                            if (unlitMat != null) sr.sharedMaterial = unlitMat;
                        }

                        var dv = visual.GetComponent<DirectionalVisual>();
                        if (dv == null) dv = visual.gameObject.AddComponent<DirectionalVisual>();
                        dv.sprites = sprites;
                        dv.RefreshSpriteCompleteness();

                        if (data != null && sprites[6] != null && sprites[6].bounds.size.x > 0.001f)
                        {
                            float calibratedScale = data.visualSize / sprites[6].bounds.size.x;
                            visual.localScale = Vector3.one * calibratedScale;
                        }
                    }
                }
            }
        }
    }

    static void WireAllIcons()
    {
        Dictionary<string, Sprite> iconMap = LoadIconMap();

        int levelUpWired = 0;
        int gameUiWired = 0;
        int spellDataWired = 0;

        LevelUpUI levelUpUI = UnityEngine.Object.FindFirstObjectByType<LevelUpUI>(FindObjectsInactive.Include);
        if (levelUpUI != null)
        {
            SerializedObject uiSO = new SerializedObject(levelUpUI);
            SerializedProperty iconsProp = uiSO.FindProperty("icons");
            iconsProp.arraySize = LevelUpIconKeys.Length;

            for (int i = 0; i < LevelUpIconKeys.Length; i++)
            {
                string key = LevelUpIconKeys[i];
                SerializedProperty entry = iconsProp.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("key").stringValue = key;

                if (iconMap.TryGetValue(key, out Sprite sp) && sp != null)
                {
                    entry.FindPropertyRelative("sprite").objectReferenceValue = sp;
                    levelUpWired++;
                }
            }

            uiSO.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(levelUpUI);
        }

        GameUI gameUI = UnityEngine.Object.FindFirstObjectByType<GameUI>(FindObjectsInactive.Include);
        if (gameUI != null)
        {
            foreach (Image img in gameUI.GetComponentsInChildren<Image>(true))
            {
                if (!img.name.StartsWith("Icon_")) continue;
                string key = img.name.Substring(5);
                if (iconMap.TryGetValue(key, out Sprite sp) && sp != null)
                {
                    Undo.RecordObject(img, "Refresh All Art & Icons");
                    img.sprite = sp;
                    img.color = Color.white;
                    img.preserveAspect = true;
                    EditorUtility.SetDirty(img);
                    gameUiWired++;
                }
            }
        }

        string[] spellGuids = AssetDatabase.FindAssets("t:SpellData", new[] { "Assets/ScriptableObjects" });
        foreach (string guid in spellGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SpellData spellData = AssetDatabase.LoadAssetAtPath<SpellData>(path);
            if (spellData == null || string.IsNullOrEmpty(spellData.id)) continue;

            if (iconMap.TryGetValue("spell_" + spellData.id, out Sprite sp) && sp != null)
            {
                Undo.RecordObject(spellData, "Refresh All Art & Icons");
                spellData.icon = sp;
                EditorUtility.SetDirty(spellData);
                spellDataWired++;
            }
        }

        Debug.Log($"[ArtSetup] Icons found={iconMap.Count} | LevelUp={levelUpWired}/{LevelUpIconKeys.Length}, GameUI={gameUiWired}, SpellData={spellDataWired}.");
    }

    static Sprite[] Load8DirectionSprites(string folder)
    {
        var result = new Sprite[8];
        if (!AssetDatabase.IsValidFolder(folder)) return result;

        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path);
            int octant = DirectionalVisual.GetOctantFromFileName(name);
            if (octant >= 0 && octant < 8)
            {
                result[octant] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
        }
        return result;
    }

    static bool HasAll8(Sprite[] sprites)
    {
        if (sprites == null || sprites.Length < 8) return false;
        for (int i = 0; i < 8; i++)
        {
            if (sprites[i] == null) return false;
        }
        return true;
    }

    static Material LoadUnlitSpriteMaterial()
    {
        string matPath = AssetDatabase.GUIDToAssetPath(SpriteUnlitDefaultGuid);
        return !string.IsNullOrEmpty(matPath) ? AssetDatabase.LoadAssetAtPath<Material>(matPath) : null;
    }

    public static Sprite FindIconSprite(string exactName)
    {
        if (string.IsNullOrEmpty(exactName)) return null;

        string[] searchFolders = AssetDatabase.IsValidFolder(IconsFolder)
            ? new[] { IconsFolder, "Assets" }
            : new[] { "Assets" };

        foreach (string folder in searchFolders)
        {
            string[] guids = AssetDatabase.FindAssets(exactName + " t:Sprite", new[] { folder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path).Equals(exactName, StringComparison.OrdinalIgnoreCase))
                {
                    return AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
            }
        }
        return null;
    }

    static Dictionary<string, Sprite> LoadIconMap()
    {
        var map = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        if (!AssetDatabase.IsValidFolder(IconsFolder)) return map;

        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { IconsFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string key = Path.GetFileNameWithoutExtension(path);
            Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (!string.IsNullOrEmpty(key) && sp != null)
            {
                map[key] = sp;
            }
        }
        return map;
    }
}
