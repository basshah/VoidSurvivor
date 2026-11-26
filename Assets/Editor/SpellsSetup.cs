using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SpellsSetup
{
    [MenuItem("VoidSurvivor/Setup Spells")]
    static void Setup()
    {
        var stats = Object.FindFirstObjectByType<PlayerStats>();
        if (stats == null)
        {
            EditorUtility.DisplayDialog("Setup Spells", "Səhnədə Player (PlayerStats) tapılmadı.", "OK");
            return;
        }

        EnsureFolder("Assets/ScriptableObjects");
        EnsureFolder("Assets/ScriptableObjects/Spells");

        // ---- Moonlets ----
        var moonletsData = CreateOrUpdateSpell(
            "Assets/ScriptableObjects/Spells/Spell_Moonlets.asset",
            id: "moonlets",
            displayName: "Moonlets",
            description: "Orbiting spheres that damage nearby enemies.",
            minCooldown: 0.1f,
            levels: new[]
            {
                L(12f, 1f, 3, 1.00f, 1.00f, 0, 0f),
                L(15f, 1f, 4, 1.05f, 1.00f, 0, 0f),
                L(19f, 1f, 5, 1.10f, 1.00f, 0, 0f),
                L(24f, 1f, 6, 1.15f, 1.00f, 0, 0f),
                L(30f, 1f, 7, 1.20f, 1.05f, 0, 0f),
                L(38f, 1f, 8, 1.30f, 1.05f, 0, 0f),
            },
            evolution: L(42f, 1f, 10, 1.40f, 1.00f, 0, 0f)
        );

        // ---- Pulsar (2x say, sürətli, tam random, Evo: 8+8 əks şüa) ----
        var pulsarData = CreateOrUpdateSpell(
            "Assets/ScriptableObjects/Spells/Spell_Pulsar.asset",
            id: "pulsar",
            displayName: "Pulsar",
            description: "Fires rapid wide energy beams in random directions.",
            minCooldown: 0.3f,
            levels: new[]
            {
                L(18f, 3.2f, 2, 1f, 0.80f, 0, 0.16f),
                L(23f, 3.0f, 2, 1f, 0.95f, 0, 0.16f),
                L(28f, 2.8f, 4, 1f, 1.05f, 0, 0.16f),
                L(35f, 2.6f, 4, 1f, 1.15f, 0, 0.17f),
                L(43f, 2.4f, 6, 1f, 1.25f, 0, 0.17f),
                L(52f, 2.2f, 6, 1f, 1.35f, 0, 0.18f),
            },
            evolution: L(64f, 2.0f, 8, 1f, 1.55f, 0, 0.19f)
        );

        // ---- Supernova (ən güclü, uzun cooldown, oyunçudan ekranı süpürən partlayış) ----
        var supernovaData = CreateOrUpdateSpell(
            "Assets/ScriptableObjects/Spells/Spell_Supernova.asset",
            id: "supernova",
            displayName: "Supernova",
            description: "Unleashes a massive screen-wide cosmic blast from your core.",
            minCooldown: 3.0f,
            levels: new[]
            {
                L(120f, 22.0f, 1, 1f, 1.00f, 0, 0f),
                L(165f, 21.0f, 1, 1f, 1.06f, 0, 0f),
                L(220f, 20.0f, 1, 1f, 1.12f, 0, 0f),
                L(285f, 19.0f, 1, 1f, 1.18f, 0, 0f),
                L(360f, 18.0f, 1, 1f, 1.24f, 0, 0f),
                L(450f, 17.0f, 1, 1f, 1.32f, 0, 0f),
            },
            evolution: L(580f, 15.5f, 1, 1f, 1.45f, 0, 6.5f)
        );

        // ---- Səhnə (Player) ----
        AttachSpell<MoonletsSpell>(stats.gameObject, moonletsData, 0);
        AttachSpell<PulsarSpell>(stats.gameObject, pulsarData, 0);
        AttachSpell<SupernovaSpell>(stats.gameObject, supernovaData, 0);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("VoidSurvivor: Moonlets, Pulsar və Supernova quruldu.");
    }

    static SpellData CreateOrUpdateSpell(
        string assetPath,
        string id,
        string displayName,
        string description,
        float minCooldown,
        SpellLevelStats[] levels,
        SpellLevelStats evolution)
    {
        var data = AssetDatabase.LoadAssetAtPath<SpellData>(assetPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<SpellData>();
            AssetDatabase.CreateAsset(data, assetPath);
        }

        data.id = id;
        data.displayName = displayName;
        data.description = description;
        data.minCooldown = minCooldown;
        data.levels = levels;
        data.evolution = evolution;
        EditorUtility.SetDirty(data);
        return data;
    }

    static void AttachSpell<T>(GameObject player, SpellData data, int startLevel) where T : Spell
    {
        var spell = player.GetComponent<T>();
        if (spell == null) spell = Undo.AddComponent<T>(player);
        spell.data = data;
        spell.level = startLevel;
        EditorUtility.SetDirty(spell);
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