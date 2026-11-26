using UnityEngine;

/// <summary>
/// Week 17: the single place the running game asks which build it is.
///
/// Every value here is read from the binary at runtime - <see cref="Application.version"/> is the
/// Player Settings <c>bundleVersion</c>, <see cref="Application.identifier"/> is the application id,
/// <see cref="Application.unityVersion"/> is the engine that compiled it. Nothing is baked into a
/// scene or a prefab, so a caption cannot survive into a binary it no longer describes.
///
/// The version deliberately does NOT live in <c>PlayerSettings.productName</c>: Unity derives both
/// <c>Application.persistentDataPath</c> and the <c>PlayerPrefs</c> namespace from the product name,
/// so putting the version there would point the release candidate at an empty store and quietly
/// erase a player's saved record. See <c>Week_17/Evidence/issue_disposition.md</c>.
/// </summary>
public static class ReleaseInfo
{
    /// <summary>`bundleVersion`, e.g. "1.0.0-rc1".</summary>
    public static string Version => Application.version;

    /// <summary>The application id the binary was built with, e.g. "com.defaultcompany.dama".</summary>
    public static string BundleId => Application.identifier;

    /// <summary>The engine version that compiled the binary, e.g. "6000.6.0f1".</summary>
    public static string Engine => Application.unityVersion;

    /// <summary>"Editor" in the editor, otherwise the runtime platform - never a hand-written date.</summary>
    public static string Platform
    {
        get
        {
            if (Application.isEditor) return "Editor";
            return Application.platform.ToString();
        }
    }

    /// <summary>The title caption: version and the platform the running binary is executing on.</summary>
    public static string Caption() => $"{Version} · {Platform}";

    /// <summary>The credits build line: version, platform and application id, all derived.</summary>
    public static string BuildLine() => $"Build {Version} · {Platform} · {BundleId}";
}
