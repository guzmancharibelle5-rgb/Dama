using UnityEngine;

/// <summary>
/// Mobile runtime defaults applied once when the player starts.
/// Mobile platforms default to a 30 FPS cap; raise it to 60 for smooth input.
/// </summary>
public static class MobileRuntimeConfig
{
    private const int TargetFrameRate = 60;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
#if UNITY_ANDROID || UNITY_IOS
        Application.targetFrameRate = TargetFrameRate;
#endif
    }
}
