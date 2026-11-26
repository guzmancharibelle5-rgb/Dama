using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Week 14: the sound and haptic layer.
///
/// It listens and it never decides. The rules live in DamaBoard, which calls one of the Play*
/// methods at the point where something has already happened; this component only turns that into
/// a clip and, on a capture, a buzz. Keeping the decision out of here is what makes the feedback
/// safe to add: no branch in this file can change the outcome of a match.
///
/// Why this is an accessibility fix and not polish: before this component existed the project held
/// zero AudioClips. A capture, a promotion and a lost match all produced the same feedback - a
/// number on the HUD changing. On a phone held at arm's length, for a player who struggles to read
/// a fast-changing HUD, that is not enough, and for a player looking at their own piece it is
/// nothing at all.
///
/// The five clips are generated assets (Assets/Audio/Generated). The preference is one
/// PlayerPrefs int, defaulting to ON, and the pause panel's SOUND button spells its state in words
/// rather than relying on a tint a player cannot see at arm's length.
/// </summary>
[DisallowMultipleComponent]
public class GameFeedback : MonoBehaviour
{
    public const string SoundPrefsKey = "audio.feedback";

    [Header("Clips (Assets/Audio/Generated)")]
    [SerializeField] private AudioClip moveClip;
    [SerializeField] private AudioClip captureClip;
    [SerializeField] private AudioClip crownClip;
    [SerializeField] private AudioClip winClip;
    [SerializeField] private AudioClip loseClip;

    [Header("Haptics")]
    [Tooltip("One short buzz when a piece is captured. Android only; a no-op on desktop.")]
    [SerializeField] private bool vibrateOnCapture = true;

    private AudioSource source;

    /// <summary>True when the player has not switched feedback off.</summary>
    public bool SoundEnabled { get; private set; } = true;

    /// <summary>Last instance to wake up. A fallback for callers that do not hold a reference.</summary>
    public static GameFeedback Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;   // 2D: the board is a UI-like object, not a thing in the world

        SoundEnabled = PlayerPrefs.GetInt(SoundPrefsKey, 1) == 1;

        Debug.Log($"[Feedback] ready - sound {(SoundEnabled ? "ON" : "OFF")}, " +
                  $"clips move={Present(moveClip)} capture={Present(captureClip)} crown={Present(crownClip)} " +
                  $"win={Present(winClip)} lose={Present(loseClip)}, haptics={(vibrateOnCapture ? "on" : "off")} " +
                  $"on {Application.platform}");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ---- Events, called by DamaBoard after the rule has already been applied ----

    public void PlayMove() => Play("move", moveClip);

    public void PlayCapture()
    {
        Play("capture", captureClip);

        if (!vibrateOnCapture) return;

        if (Application.isMobilePlatform)
        {
            Handheld.Vibrate();
        }
        else
        {
            // Said out loud rather than skipped silently: a transcript from the Editor cannot
            // claim a buzz it was never able to feel.
            Debug.Log("[Feedback] haptic skipped - Handheld.Vibrate is a no-op on this platform.");
        }
    }

    public void PlayCrown() => Play("crown", crownClip);

    /// <summary>Win or lose relative to the side stored in the player profile.</summary>
    public void PlayResult(bool playerWon) => Play(playerWon ? "win" : "lose", playerWon ? winClip : loseClip);

    // ---- The one setting ----

    /// <summary>Flips feedback on or off, remembers it, and returns the new state.</summary>
    public bool ToggleSound()
    {
        SetSoundEnabled(!SoundEnabled);
        return SoundEnabled;
    }

    public void SetSoundEnabled(bool enabled)
    {
        SoundEnabled = enabled;
        PlayerPrefs.SetInt(SoundPrefsKey, enabled ? 1 : 0);
        PlayerPrefs.Save();

        if (!enabled && source != null) source.Stop();

        Debug.Log($"[Feedback] sound {(enabled ? "ON" : "OFF")} - remembered in PlayerPrefs[\"{SoundPrefsKey}\"]");
    }

    /// <summary>The label the SOUND button shows. Words, not a colour, not an icon.</summary>
    public static string SoundLabel(bool enabled) => enabled ? "SOUND: ON" : "SOUND: OFF";

    // ---- Internals ----

    private void Play(string kind, AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning($"[Feedback] {kind} fired but its clip is not assigned.");
            return;
        }

        Debug.Log($"[Feedback] {kind} -> {clip.name}");

        if (!SoundEnabled) return;

        if (source == null) source = GetComponent<AudioSource>();
        if (source != null) source.PlayOneShot(clip);
    }

    private static string Present(AudioClip clip) => clip == null ? "MISSING" : "ok";
}
