using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives <c>GameCanvas/ProfilePanel</c>: the name field, the record, the local
/// best-times board, the side toggle and the two-press RESET.
///
/// This class never touches the save file and never parses JSON - every read and
/// every write goes through <see cref="PlayerProfile"/>. Children are found by
/// name, the same way <c>DamaBoard</c> finds its panels.
/// </summary>
public class ProfilePanel : MonoBehaviour
{
    private const float ResetWindowSeconds = 4f;

    private InputField nameInput;
    private Text recordText;
    private Text bestText;
    private Text statusText;
    private Text sideLabel;
    private Text resetLabel;

    private float resetArmedAt = -1f;
    private bool bound;

    private void Awake()
    {
        Bind();
    }

    private void OnEnable()
    {
        Bind();

        PlayerProfile.Load();
        resetArmedAt = -1f;

        if (resetLabel != null) resetLabel.text = "RESET PROFILE";
        if (nameInput != null) nameInput.text = PlayerProfile.PlayerName;

        Refresh();
        SetStatus("Type a name, then SAVE. Nothing leaves this device.");

        Debug.Log($"[ProfilePanel] Opened. name='{PlayerProfile.PlayerName}' matches={PlayerProfile.MatchesPlayed}");
    }

    private void Update()
    {
        // Unscaled: the board sets Time.timeScale = 0 on pause and on game over.
        if (resetArmedAt < 0f) return;
        if (Time.unscaledTime - resetArmedAt <= ResetWindowSeconds) return;

        resetArmedAt = -1f;
        if (resetLabel != null) resetLabel.text = "RESET PROFILE";
        SetStatus("Reset cancelled.");
    }

    private void Bind()
    {
        if (bound) return;
        bound = true;

        nameInput = Find<InputField>("NameInput");
        recordText = Find<Text>("RecordText");
        bestText = Find<Text>("BestText");
        statusText = Find<Text>("StatusText");

        if (nameInput != null) nameInput.characterLimit = PlayerProfile.MaxNameLength;

        Button sideButton = Find<Button>("SideButton");
        if (sideButton != null)
        {
            sideLabel = sideButton.transform.Find("SideLabel")?.GetComponent<Text>();
            sideButton.onClick.RemoveAllListeners();
            sideButton.onClick.AddListener(ToggleSide);
        }

        Button saveButton = Find<Button>("SaveButton");
        if (saveButton != null)
        {
            saveButton.onClick.RemoveAllListeners();
            saveButton.onClick.AddListener(SaveName);
        }

        Button resetButton = Find<Button>("ResetButton");
        if (resetButton != null)
        {
            resetLabel = resetButton.transform.Find("ResetLabel")?.GetComponent<Text>();
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(ResetPressed);
        }

        if (nameInput == null || recordText == null || bestText == null || statusText == null)
        {
            Debug.LogWarning("[ProfilePanel] One or more children were not found; run Tools/Dama/Build Profile UI.");
        }
    }

    private T Find<T>(string childName) where T : Component
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    /// <summary>Redraws every label from the stored profile.</summary>
    public void Refresh()
    {
        PlayerProfile.Load();

        if (sideLabel != null)
            sideLabel.text = PlayerProfile.PlayerSide == 1 ? "SIDE: RED" : "SIDE: BLUE";

        if (recordText != null)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append($"Matches: {PlayerProfile.MatchesPlayed}   Wins: {PlayerProfile.Wins}   Losses: {PlayerProfile.Losses}");
            builder.Append('\n');
            builder.Append($"Current streak: {PlayerProfile.CurrentStreak}   Best streak: {PlayerProfile.BestStreak}");
            recordText.text = builder.ToString();
        }

        if (bestText != null)
        {
            var wins = PlayerProfile.BestWins();

            if (wins.Count == 0)
            {
                bestText.text = "Fastest victories\n- none yet -";
                return;
            }

            StringBuilder builder = new StringBuilder("Fastest victories");
            for (int i = 0; i < wins.Count; i++)
            {
                builder.Append('\n');
                builder.Append($"{i + 1}. {wins[i].seconds:0.0} s   match {wins[i].matchId}");
            }
            bestText.text = builder.ToString();
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    /// <summary>Pushes the stored name back into the field after an outside change (check harness).</summary>
    public void SyncNameField()
    {
        if (nameInput != null) nameInput.text = PlayerProfile.PlayerName;
    }

    /// <summary>SAVE: stores the typed name. A blank field is refused and the old name survives.</summary>
    public void SaveName()
    {
        if (nameInput == null) return;

        if (!PlayerProfile.SetName(nameInput.text))
        {
            SetStatus("Enter a name first (up to 16 characters).");
            Debug.Log("[ProfilePanel] Save refused: the name was blank.");
            return;
        }

        if (!string.IsNullOrEmpty(PlayerProfile.LastError))
        {
            SetStatus($"Could not save. {PlayerProfile.LastError}");
            Debug.LogError($"[ProfilePanel] Save failed: {PlayerProfile.LastError}");
            return;
        }

        nameInput.text = PlayerProfile.PlayerName;
        SetStatus("Saved. Your record is stored on this device only.");
        Debug.Log($"[ProfilePanel] Saved name='{PlayerProfile.PlayerName}'");
        Refresh();
    }

    /// <summary>Toggles which side the profile owner plays, so wins and losses mean something.</summary>
    public void ToggleSide()
    {
        int next = PlayerProfile.PlayerSide == 1 ? 2 : 1;

        if (!PlayerProfile.SetSide(next))
        {
            SetStatus("Could not change side.");
            return;
        }

        SetStatus($"You play {(next == 1 ? "RED" : "BLUE")}.");
        Debug.Log($"[ProfilePanel] Side set to {next}");
        Refresh();
    }

    /// <summary>
    /// Two presses within four seconds. One tap that wipes an evening's record is a
    /// defect waiting to happen on a phone, and a confirmation dialog gets dismissed
    /// by habit.
    /// </summary>
    public void ResetPressed()
    {
        if (resetArmedAt < 0f)
        {
            resetArmedAt = Time.unscaledTime;
            if (resetLabel != null) resetLabel.text = "PRESS AGAIN";
            SetStatus("This erases your name, record and best times. Press again within 4 s.");
            Debug.Log("[ProfilePanel] Reset armed.");
            return;
        }

        resetArmedAt = -1f;
        if (resetLabel != null) resetLabel.text = "RESET PROFILE";

        if (!PlayerProfile.ResetProfile())
        {
            SetStatus($"Could not reset. {PlayerProfile.LastError}");
            Debug.LogError($"[ProfilePanel] Reset failed: {PlayerProfile.LastError}");
            return;
        }

        if (nameInput != null) nameInput.text = PlayerProfile.DefaultName;
        SetStatus("Profile erased.");
        Debug.Log("[ProfilePanel] Profile reset.");
        Refresh();
    }
}
