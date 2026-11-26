using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameplaySetup
{
    [MenuItem("VoidSurvivor/Setup Starshot + XP")]
    static void Setup()
    {
        var stats = Object.FindFirstObjectByType<PlayerStats>();
        if (stats == null)
        {
            EditorUtility.DisplayDialog("Setup", "Səhnədə Player (PlayerStats) tapılmadı.", "OK");
            return;
        }

        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/ScriptableObjects");
        EnsureFolder("Assets/ScriptableObjects/Spells");

        var circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        var square = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // ---- Starshot data ----
        string dataPath = "Assets/ScriptableObjects/Spells/Spell_Starshot.asset";
        var data = AssetDatabase.LoadAssetAtPath<SpellData>(dataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<SpellData>();
            data.id = "starshot";
            data.displayName = "Starshot";
            data.minCooldown = 0.12f;
            data.levels = new[]
            {
                L(10f, 1.0f, 1, 14f, 1f, 0, 1.5f),
                L(13f, 0.9f, 1, 14f, 1f, 0, 1.5f),
                L(16f, 0.9f, 2, 14f, 1f, 0, 1.5f),
                L(20f, 0.8f, 2, 14f, 1f, 1, 1.5f),
                L(24f, 0.7f, 3, 14f, 1f, 1, 1.5f),
                L(30f, 0.6f, 3, 14f, 1f, 2, 1.5f),
            };
            data.evolution = L(28f, 0.18f, 2, 18f, 1.2f, 3, 1.5f);
            AssetDatabase.CreateAsset(data, dataPath);
        }

        // ---- Mərmi prefab ----
        string projPath = "Assets/Prefabs/Projectile_Starshot.prefab";
        var projPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(projPath);
        if (projPrefab == null)
        {
            var go = new GameObject("Projectile_Starshot");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = circle;
            sr.color = new Color(0.3f, 0.9f, 1f);
            sr.sortingOrder = 8;
            go.transform.localScale = Vector3.one * 0.6f;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = circle != null ? circle.bounds.extents.x : 0.5f;
            go.AddComponent<Projectile>();
            projPrefab = PrefabUtility.SaveAsPrefabAsset(go, projPath);
            Object.DestroyImmediate(go);
        }

        // ---- XP kristalı prefab ----
        string gemPath = "Assets/Prefabs/XPGem.prefab";
        var gemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(gemPath);
        if (gemPrefab == null)
        {
            var go = new GameObject("XPGem");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = new Color(0.3f, 1f, 0.8f);
            sr.sortingOrder = 4;
            go.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
            go.transform.localScale = Vector3.one * 0.8f;
            go.AddComponent<XPGem>();
            gemPrefab = PrefabUtility.SaveAsPrefabAsset(go, gemPath);
            Object.DestroyImmediate(go);
        }

        // ---- Səhnə ----
        var xp = Object.FindFirstObjectByType<XPManager>();
        if (xp == null)
        {
            var go = new GameObject("XPManager");
            xp = go.AddComponent<XPManager>();
            Undo.RegisterCreatedObjectUndo(go, "Setup Starshot + XP");
        }
        xp.player = stats.transform;
        xp.gemPrefab = gemPrefab;
        EditorUtility.SetDirty(xp);

        var spell = stats.GetComponent<StarshotSpell>();
        if (spell == null) spell = Undo.AddComponent<StarshotSpell>(stats.gameObject);
        spell.data = data;
        spell.projectilePrefab = projPrefab;
        spell.level = 1;
        EditorUtility.SetDirty(spell);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("VoidSurvivor: Starshot və XP quruldu.");
    }

    static SpellLevelStats L(float dmg, float cd, int count, float speed, float size, int pierce, float dur)
    {
        return new SpellLevelStats
        {
            damage = dmg,
            cooldown = cd,
            count = count,
            speed = speed,
            size = size,
            pierce = pierce,
            duration = dur
        };
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}