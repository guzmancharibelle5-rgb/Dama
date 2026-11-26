using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Week 14 evidence capture.
///
/// Menu: Tools > Dama > Week 14 > 3. Capture Week 14 Evidence
///
/// Enters play mode once and walks a small stage machine, so the three screenshots are of the game
/// actually running, not of an edit-mode mock-up:
///   E3  profile panel, reached through DamaBoard.ShowProfile() with a seeded record
///   E2  the in-match HUD over the board
///   E1  the pause panel, showing the new SOUND control
///
/// It then drives one move, one capture, one promotion and both end screens through the game's own
/// private rules methods, and writes every [Feedback] line the run produced to feedback_lines.txt.
/// That file is the evidence that the sound is wired to the rules rather than to a button; the
/// screenshots are the evidence that the layout is what the audit measured.
///
/// Screenshots are gated on the PNG existing with a non-zero length, because
/// ScreenCapture.CaptureScreenshot writes asynchronously - a shot requested and a shot written are
/// not the same moment, and treating them as one is how evidence ends up showing the previous panel.
/// </summary>
public static class Week14Checks
{
    private const string Menu = "Tools/Dama/Week 14/3. Capture Week 14 Evidence";
    private const double StageTimeoutSeconds = 25.0;
    private const double StageSettleSeconds = 0.45;

    private static bool running;
    private static int stage;
    private static double nextActionAt;
    private static double deadlineAt;
    private static string pendingShot;
    private static DamaBoard board;

    private static readonly List<string> feedbackLines = new List<string>();
    private static bool logHooked;

    public static string EvidenceDir =>
        Path.Combine(Directory.GetParent(Application.dataPath).FullName,
            "docs", "Charibelle Guzman", "Week_14", "Evidence");

    [MenuItem(Menu)]
    public static void Begin()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[Week14Checks] Stop play mode first; this harness starts it itself.");
            return;
        }

        Directory.CreateDirectory(EvidenceDir);

        feedbackLines.Clear();
        stage = 0;
        running = true;
        pendingShot = null;
        board = null;
        deadlineAt = EditorApplication.timeSinceStartup + 120.0;

        if (!logHooked)
        {
            Application.logMessageReceived += OnLog;
            logHooked = true;
        }

        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();

        Debug.Log("[Week14Checks] Entering play mode for the Week 14 captures.");
    }

    private static void Tick()
    {
        if (!running) return;

        if (!EditorApplication.isPlaying) return;

        if (EditorApplication.timeSinceStartup > deadlineAt)
        {
            Abort("the whole run exceeded its 120 s budget");
            return;
        }

        // A capture that was requested is finished only when the file is on disk.
        if (pendingShot != null)
        {
            if (ShotLanded(pendingShot))
            {
                Debug.Log($"[Week14Checks] Wrote {Path.GetFileName(pendingShot)} ({new FileInfo(pendingShot).Length} bytes).");
                pendingShot = null;
                nextActionAt = EditorApplication.timeSinceStartup + StageSettleSeconds;
            }
            else if (EditorApplication.timeSinceStartup > deadlineAt - 100.0 && Time.frameCount % 60 == 0)
            {
                Debug.Log($"... still waiting for {Path.GetFileName(pendingShot)}");
            }

            return;
        }

        if (EditorApplication.timeSinceStartup < nextActionAt) return;
        if (Time.frameCount < 5) return;

        switch (stage)
        {
            case 0:
                board = UnityEngine.Object.FindFirstObjectByType<DamaBoard>();
                if (board == null) { Abort("no DamaBoard in the scene"); return; }

                SeedProfile();
                board.ShowProfile();
                Advance(1);
                break;

            case 1:
                Request("E3_profile_panel.png");
                Advance(2);
                break;

            case 2:
                board.CloseProfile();
                board.StartGame();
                Advance(3);
                break;

            case 3:
                Request("E2_hud_in_match.png");
                Advance(4);
                break;

            case 4:
                board.PauseGame();
                Advance(5);
                break;

            case 5:
                Debug.Log($"[Week14Checks] Pause panel active={PausePanelActive()}, " +
                          $"SOUND label='{SoundLabel()}'.");
                Request("E1_pause_sound.png");
                Advance(6);
                break;

            case 6:
                DriveFeedback();
                Advance(7);
                break;

            case 7:
                Finish();
                break;
        }
    }

    // =====================================================================
    // The run
    // =====================================================================

    private static void SeedProfile()
    {
        PlayerProfile.UseDefaultDataDirectory();
        PlayerProfile.Load();
        PlayerProfile.ResetProfile();

        int side = PlayerProfile.PlayerSide;

        PlayerProfile.RecordMatch(1, side, 38.9f, 12, 6);          // win
        PlayerProfile.RecordMatch(2, side == 1 ? 2 : 1, 42.5f, 9, 12); // loss
        PlayerProfile.RecordMatch(3, side, 50.0f, 11, 8);          // win

        Debug.Log($"[Week14Checks] Seeded the profile: wins={PlayerProfile.Wins} losses={PlayerProfile.Losses} " +
                  $"best={PlayerProfile.BestWins().Count} entries.");
    }

    /// <summary>
    /// Drives the game's own rules methods so the feedback fires from the real code path. The
    /// end-of-match calls run twice on purpose: win and lose are different clips, and a run that
    /// only ever saw one of them would leave the other unobserved.
    /// </summary>
    private static void DriveFeedback()
    {
        board.ResumeGame();

        SetCell(1, 2, 1);
        Invoke("ExecuteMove", new Vector2Int(1, 2), new Vector2Int(0, 3));

        SetCell(3, 3, 1);
        SetCell(2, 2, 2);
        Invoke("ExecuteMove", new Vector2Int(3, 3), new Vector2Int(1, 1));

        // A man only ranks up on the far row, and which row that is depends on the side: RED reaches
        // y = 7, BLUE reaches y = 0. Getting this wrong is silent - the move plays, and no crown fires.
        SetCell(3, 6, 1);
        Invoke("ExecuteMove", new Vector2Int(3, 6), new Vector2Int(2, 7));

        int side = PlayerProfile.PlayerSide;
        Invoke("DeclareWinner", side);
        board.RestartGame();
        Invoke("DeclareWinner", side == 1 ? 2 : 1);
    }

    private static void Finish()
    {
        File.WriteAllText(Path.Combine(EvidenceDir, "feedback_lines.txt"), Transcript(), new UTF8Encoding(false));
        Debug.Log($"[Week14Checks] Wrote feedback_lines.txt with {feedbackLines.Count} line(s).");

        running = false;
        EditorApplication.update -= Tick;

        if (logHooked)
        {
            Application.logMessageReceived -= OnLog;
            logHooked = false;
        }

        EditorApplication.ExitPlaymode();
    }

    private static void Abort(string reason)
    {
        Debug.LogError($"[Week14Checks] Aborted at stage {stage}: {reason}.");

        running = false;
        EditorApplication.update -= Tick;

        if (logHooked)
        {
            Application.logMessageReceived -= OnLog;
            logHooked = false;
        }

        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
    }

    // =====================================================================
    // Plumbing
    // =====================================================================

    private static void Advance(int nextStage)
    {
        stage = nextStage;
        nextActionAt = EditorApplication.timeSinceStartup + StageSettleSeconds;
        deadlineAt = EditorApplication.timeSinceStartup + 120.0;
    }

    private static void Request(string fileName)
    {
        string path = Path.Combine(EvidenceDir, fileName);

        if (File.Exists(path)) File.Delete(path);
        ScreenCapture.CaptureScreenshot(path);
        pendingShot = path;

        Debug.Log($"[Week14Checks] Requested {fileName} -> {path}");
    }

    private static bool ShotLanded(string path)
    {
        if (!File.Exists(path)) return false;

        try
        {
            return new FileInfo(path).Length > 0;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static void SetCell(int x, int y, int piece)
    {
        FieldInfo field = typeof(DamaBoard).GetField("grid", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null) { Debug.LogError("[Week14Checks] DamaBoard.grid not found."); return; }

        Array grid = field.GetValue(board) as Array;
        if (grid == null) return;

        Type element = grid.GetType().GetElementType();
        grid.SetValue(Enum.ToObject(element, piece), x, y);
    }

    private static void Invoke(string method, params object[] args)
    {
        MethodInfo info = typeof(DamaBoard).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (info == null)
        {
            Debug.LogError($"[Week14Checks] DamaBoard.{method} not found.");
            return;
        }

        try
        {
            info.Invoke(board, args);
        }
        catch (TargetInvocationException exception)
        {
            Debug.LogError($"[Week14Checks] {method} threw: {exception.InnerException?.Message}");
        }
    }

    private static bool PausePanelActive()
    {
        GameObject canvas = GameObject.Find("GameCanvas");
        Transform pause = canvas != null ? canvas.transform.Find("PausePanel") : null;
        return pause != null && pause.gameObject.activeSelf;
    }

    private static string SoundLabel()
    {
        GameObject canvas = GameObject.Find("GameCanvas");
        Transform label = canvas != null ? canvas.transform.Find("PausePanel/SoundButton/Text") : null;
        Text text = label != null ? label.GetComponent<Text>() : null;
        return text != null ? text.text : "MISSING";
    }

    private static void OnLog(string condition, string stackTrace, LogType type)
    {
        if (condition != null && condition.StartsWith("[Feedback]")) feedbackLines.Add(condition);
    }

    private static string Transcript()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("DAMA - WEEK 14 FEEDBACK TRANSCRIPT");
        builder.AppendLine("Captured in play mode by Tools > Dama > Week 14 > 3. Capture Week 14 Evidence.");
        builder.AppendLine("These lines are the [Feedback] messages the running game produced, in order, with no editing.");
        builder.AppendLine($"Run at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC   Scene: DamaGame.unity");
        builder.AppendLine(new string('-', 96));

        foreach (string line in feedbackLines) builder.AppendLine(line);

        builder.AppendLine(new string('-', 96));

        string[] kinds = { "move", "capture", "crown", "win", "lose" };
        List<string> fired = new List<string>();

        foreach (string kind in kinds)
            foreach (string line in feedbackLines)
                if (line.Contains($" {kind} ") || line.Contains($"] {kind} ") || line.Contains($" {kind} -"))
                {
                    fired.Add(kind);
                    break;
                }

        builder.AppendLine($"Kinds fired: {string.Join(", ", fired)} ({fired.Count} of {kinds.Length})");

        if (fired.Count < kinds.Length)
        {
            List<string> missing = new List<string>();
            foreach (string kind in kinds) if (!fired.Contains(kind)) missing.Add(kind);
            builder.AppendLine($"Not observed in this run: {string.Join(", ", missing)}. It is not claimed as working.");
        }

        builder.AppendLine("Haptics: Handheld.Vibrate is a no-op in the Editor, and this transcript was captured in the Editor.");
        builder.AppendLine("A buzz therefore cannot be claimed from this file, only from a build on a handset.");

        return builder.ToString();
    }
}
