using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AndroidBuildSetup
{
    public const string MainScenePath = "Assets/Scenes/SampleScene.unity";
    public const string CompanyName = "basshah";
    public const string ProductName = "VoidSurvivor";
    public const string AndroidAppId = "com.basshah.voidsurvivor";
    public const string ApkOutputPath = "Builds/VoidSurvivor.apk";

    [MenuItem("VoidSurvivor/Configure Android Settings")]
    public static void ConfigureAndroidSettings()
    {
        EnsureSampleSceneInBuildSettings();

        PlayerSettings.companyName = CompanyName;
        PlayerSettings.productName = ProductName;

        NamedBuildTarget androidTarget = NamedBuildTarget.Android;
        PlayerSettings.SetApplicationIdentifier(androidTarget, AndroidAppId);

        // Lock orientation cleanly to Portrait
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;

        // Optimal mobile IL2CPP ARM64 release/test settings
        PlayerSettings.SetScriptingBackend(androidTarget, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
#pragma warning disable CS0618
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
#pragma warning restore CS0618
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

        // Reduce runtime overhead while keeping multi-threaded rendering enabled
#pragma warning disable CS0618
        PlayerSettings.MTRendering = true;
#pragma warning restore CS0618
        PlayerSettings.SetMobileMTRendering(androidTarget, true);
        PlayerSettings.bakeCollisionMeshes = false;

        AssetDatabase.SaveAssets();
        Debug.Log($"[AndroidBuildSetup] Configured Android settings: {CompanyName}/{ProductName} ({AndroidAppId}), Portrait, IL2CPP ARM64, API 24 -> Auto, MTRendering=true.");
    }

    [MenuItem("VoidSurvivor/Build Android APK")]
    public static void BuildAndroidApk()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
        {
            Debug.LogError("[AndroidBuildSetup] Android Build Support module is not installed in Unity Hub for this Editor version. Install 'Android Build Support' (SDK, NDK & OpenJDK) via Unity Hub and retry.");
            return;
        }

        ConfigureAndroidSettings();

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            Debug.Log("[AndroidBuildSetup] Switching active build target to Android...");
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("[AndroidBuildSetup] Failed to switch active build target to Android. Verify Android SDK/NDK/OpenJDK paths in Edit -> Preferences -> External Tools.");
                return;
            }
        }

        string buildsDir = Path.GetDirectoryName(ApkOutputPath);
        if (!string.IsNullOrEmpty(buildsDir) && !Directory.Exists(buildsDir))
        {
            Directory.CreateDirectory(buildsDir);
        }

        var enabledScenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled && !string.IsNullOrEmpty(scene.path))
            {
                enabledScenes.Add(scene.path);
            }
        }

        if (enabledScenes.Count == 0)
        {
            enabledScenes.Add(MainScenePath);
        }

        var options = new BuildPlayerOptions
        {
            scenes = enabledScenes.ToArray(),
            locationPathName = ApkOutputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            string fullPath = Path.GetFullPath(ApkOutputPath);
            long bytes = File.Exists(fullPath) ? new FileInfo(fullPath).Length : (long)summary.totalSize;
            double megabytes = bytes / (1024.0 * 1024.0);
            Debug.Log($"[AndroidBuildSetup] Android APK built successfully: {fullPath} ({megabytes:F2} MB, {summary.totalTime.TotalSeconds:F1}s)");
        }
        else
        {
            Debug.LogError($"[AndroidBuildSetup] Android APK build failed ({summary.result}) with {summary.totalErrors} error(s). Check Android SDK/NDK/JDK installation in Unity Hub if toolchain errors appear.");
        }
    }

    static void EnsureSampleSceneInBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>();
        scenes.Add(new EditorBuildSettingsScene(MainScenePath, true));

        EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
        if (existing != null)
        {
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i] != null && existing[i].path != MainScenePath)
                {
                    scenes.Add(existing[i]);
                }
            }
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
