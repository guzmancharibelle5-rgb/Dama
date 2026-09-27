using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// One-shot editor utilities for mobile build configuration.
/// Covers settings that aren't reachable through the project settings API surface.
/// </summary>
public static class AndroidBuildSetup
{
    private const string MenuPath = "Tools/Android/Apply Mobile Build Settings";

    [MenuItem(MenuPath)]
    private static void ApplyMobileBuildSettings()
    {
        // 64-bit only — required by Google Play and the fastest path for modern devices.
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        // IL2CPP is required for ARM64.
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);

        ApplyPerPlatformDefaultQuality();
        ApplyBuildScenes();

        AssetDatabase.SaveAssets();
        Debug.Log("[AndroidBuildSetup] Applied: ARM64 architecture, IL2CPP scripting backend, mobile quality preset.");
    }

    /// <summary>
    /// Points Android and iOS at the reduced "Medium" quality preset while
    /// leaving the desktop default untouched. Quality's per-platform defaults
    /// are only reachable through the serialized QualitySettings asset.
    /// </summary>
    private static void ApplyPerPlatformDefaultQuality()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
        if (assets == null || assets.Length == 0)
        {
            Debug.LogWarning("[AndroidBuildSetup] QualitySettings asset not found; skipping quality preset.");
            return;
        }

        var serialized = new SerializedObject(assets[0]);
        SerializedProperty map = serialized.FindProperty("m_PerPlatformDefaultQuality");
        if (map == null || !map.isArray)
        {
            Debug.LogWarning("[AndroidBuildSetup] m_PerPlatformDefaultQuality not found; skipping quality preset.");
            return;
        }

        int mediumLevel = FindLevelIndex(serialized, "Medium");
        if (mediumLevel < 0)
        {
            Debug.LogWarning("[AndroidBuildSetup] No 'Medium' quality level found; skipping quality preset.");
            return;
        }

        for (int i = 0; i < map.arraySize; i++)
        {
            SerializedProperty entry = map.GetArrayElementAtIndex(i);
            SerializedProperty key = entry.FindPropertyRelative("first");
            SerializedProperty value = entry.FindPropertyRelative("second");
            if (key == null || value == null) continue;

            if (key.stringValue == "Android" || key.stringValue == "iPhone")
            {
                value.intValue = mediumLevel;
            }
        }

        serialized.ApplyModifiedProperties();
    }

    /// <summary>
    /// Makes DamaGame the launch scene (index 0) and demotes any other
    /// scenes (e.g. the empty SampleScene template scene) to disabled so
    /// they never load in a build.
    /// </summary>
    private static void ApplyBuildScenes()
    {
        const string gameScenePath = "Assets/Scenes/DamaGame.unity";

        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(gameScenePath, true)
        };

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.path != gameScenePath)
            {
                scenes.Add(new EditorBuildSettingsScene(scene.path, false));
            }
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }
    private static int FindLevelIndex(SerializedObject qualitySettings, string levelName)
    {
        SerializedProperty names = qualitySettings.FindProperty("m_QualitySettings");
        if (names == null || !names.isArray) return -1;

        for (int i = 0; i < names.arraySize; i++)
        {
            SerializedProperty name = names.GetArrayElementAtIndex(i).FindPropertyRelative("name");
            if (name != null && name.stringValue == levelName) return i;
        }

        return -1;
    }
}
