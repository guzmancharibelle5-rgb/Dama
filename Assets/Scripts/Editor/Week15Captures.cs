using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Week 15: captures the four submission images from the running game and writes the transcript that
/// says what each one is.
///
/// Menu: Tools > Dama > Week 15 > 4. Capture Evidence
///
///   E1  the pause panel with the new MENU control beside RESTART
///   E2  the end screen with the summary line and MENU beside RESTART
///   E3  the profile panel with the status line at its new size
///   E4  the board mid-match, after a real capture
///
/// ScreenCapture.CaptureScreenshot writes asynchronously - a shot requested and a shot written are two
/// different moments - so the run does not advance until the PNG exists on disk with a non-zero length.
/// Nothing is changed until the image of the state before it has landed. The run stops play mode itself.
/// </summary>
public static class Week15Captures
{
    private const string Menu = "Tools/Dama/Week 15/4. Capture Evidence";
    private const string SceneName = "DamaGame";
    private const double StageSettleSeconds = 0.45;

    private static bool running;
    private static int stage;
    private static double nextActionAt;
    private static double deadlineAt;
    private static string pendingShot;
    private static DamaBoard board;

    private static readonly List<string> transcript = new List<string>();

    public static string EvidenceDir =>
        Path.Combine(Directory.GetParent(Application.dataPath).FullName,
            "docs", "Charibelle Guzman", "Week_15", "Evidence");

    [MenuItem(Menu, false, 40)]
    public static void Begin()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != SceneName)
        {
            Debug.LogWarning($"[Week15Captures] Open {SceneName}.unity first; the active scene is '{(scene.IsValid() ? scene.name : "none")}'.");
            return;
        }

        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[Week15Captures] Stop play mode first; this harness starts it itself.");
            return;
        }

        Directory.CreateDirectory(EvidenceDir);

        transcript.Clear();
        stage = 0;
        running = true;
        pendingShot = null;
        board = null;
        deadlineAt = EditorApplication.timeSinceStartup + 120.0;

        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();

        Note("Entering play mode for the Week 15 captures.");
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

        if (pendingShot != null)
        {
            if (ShotLanded(pendingShot))
            {
                Note($"Wrote {Path.GetFileName(pendingShot)} ({new FileInfo(pendingShot).Length} bytes).");
                pendingShot = null;
                nextActionAt = EditorApplication.timeSinceStartup + StageSettleSeconds;
            }

            return;
        }

        if (EditorApplication.timeSinceStartup < nextActionAt) return;
        if (Time.frameCount < 5) return;

        switch (stage)
        {
            case 0:
                board = UnityEngine.Object.FindAnyObjectByType<DamaBoard>();
                if (board == null) { Abort("no DamaBoard in the scene"); return; }

                board.StartGame();
                SeedMidMatch();
                Advance(1);
                break;

            case 1:
                Note($"E4 state: turn={TurnText()} counts={CountsText()}.");
                Request("E4_mid_match.png");
                Advance(2);
                break;

            case 2:
                board.PauseGame();
                Advance(3);
                break;

            case 3:
                Note($"E1 state: PausePanel active={PanelActive("PausePanel")}, " +
                     $"MenuButton={Measure("PausePanel/MenuButton")}, SoundButton={Measure("PausePanel/SoundButton")}.");
                Request("E1_pause_menu.png");
                Advance(4);
                break;

            case 4:
                board.ReturnToMenu();
                Advance(5);
                break;

            case 5:
                Note($"ReturnToMenu left: StartPanel={PanelActive("StartPanel")} HUDPanel={PanelActive("HUDPanel")} " +
                     $"PausePanel={PanelActive("PausePanel")} timeScale={Time.timeScale:0.00}.");
                // Seed first, then show: the panel reads the profile when it is enabled, so seeding
                // afterwards would photograph the previous record.
                SeedProfile();
                board.ShowProfile();
                Advance(6);
                break;

            case 6:
                Note($"E3 state: StatusText {StatusSize()}, RecordText='{LabelText("ProfilePanel/RecordText")}'.");
                Request("E3_profile_status.png");
                Advance(7);
                break;

            case 7:
                board.CloseProfile();
                board.StartGame();
                SeedImmobilisation();
                Advance(8);
                break;

            case 8:
                Note($"E2 state: WinPanel={PanelActive("WinPanel")} summary='{LabelText("WinPanel/SummaryText")}' " +
                     $"MenuButtonWin={Measure("WinPanel/MenuButtonWin")}.");
                Request("E2_end_screen.png");
                Advance(9);
                break;

            case 9:
                Finish();
                break;
        }
    }

    // =====================================================================
    // The states the four images are taken in
    // =====================================================================

    /// <summary>
    /// A board after one real capture: RED jumps a BLUE man at (3,4) and lands at (4,5). BLUE keeps
    /// two men elsewhere, so the capture does not also end the match - the point of this image is a
    /// match in progress, not another end screen.
    /// </summary>
    private static void SeedMidMatch()
    {
        ClearGrid();
        SetCell(2, 3, 1);
        SetCell(3, 4, 2);
        SetCell(0, 7, 2);
        SetCell(6, 0, 2);
        SetCurrentPlayer(1);

        Invoke("ExecuteMove", new Vector2Int(2, 3), new Vector2Int(4, 5));

        SyncPieces();
        Invoke("RefreshUI");
    }

    /// <summary>
    /// BLUE still holds a man but has no legal move: it stands on its own back row, where a man has no
    /// forward step, and there is nothing for it to jump. That is the reason the summary line should
    /// name, and it is the more informative of the two endings to photograph.
    /// </summary>
    private static void SeedImmobilisation()
    {
        ClearGrid();
        SetCell(7, 7, 1);
        SetCell(0, 0, 2);
        SetCurrentPlayer(2);

        SyncPieces();
        Invoke("RefreshUI");
        Invoke("CheckForWin");
    }

    /// <summary>
    /// Rebuilds the visible pieces from the grid. Seeding the grid by reflection changes what the game
    /// thinks is on the board, not what is drawn on it - and an image whose HUD counts disagree with
    /// its own summary line is worse evidence than no image. This makes the picture show the state.
    /// </summary>
    private static void SyncPieces()
    {
        Transform root = board.transform.Find("Pieces");
        if (root == null)
        {
            Debug.LogWarning("[Week15Captures] No Pieces root; the capture will not match the seeded grid.");
            return;
        }

        for (int i = root.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(root.GetChild(i).gameObject);

        MethodInfo circle = typeof(DamaBoard).GetMethod("CreateCircleSprite", BindingFlags.Static | BindingFlags.NonPublic);
        MethodInfo create = typeof(DamaBoard).GetMethod("CreatePiece", BindingFlags.Instance | BindingFlags.NonPublic);

        if (circle == null || create == null)
        {
            Debug.LogError("[Week15Captures] DamaBoard.CreatePiece/CreateCircleSprite not found; pieces were not rebuilt.");
            return;
        }

        Sprite sprite = circle.Invoke(null, null) as Sprite;
        int made = 0;

        for (int x = 0; x < DamaBoard.Size; x++)
        {
            for (int y = 0; y < DamaBoard.Size; y++)
            {
                int piece = CellAt(x, y);
                if (piece == 0) continue;

                create.Invoke(board, new object[] { root, sprite, piece, x, y });
                made++;
            }
        }

        Note($"Rebuilt {made} visible piece(s) from the grid so the image matches the state.");
    }

    private static void SeedProfile()
    {
        PlayerProfile.UseDefaultDataDirectory();
        PlayerProfile.Load();
        PlayerProfile.ResetProfile();

        int side = PlayerProfile.PlayerSide;

        PlayerProfile.RecordMatch(1, side, 38.9f, 12, 6);
        PlayerProfile.RecordMatch(2, side == 1 ? 2 : 1, 42.5f, 9, 12);
        PlayerProfile.RecordMatch(3, side, 50.0f, 11, 8);

        Note($"Seeded the profile: wins={PlayerProfile.Wins} losses={PlayerProfile.Losses} " +
             $"best={PlayerProfile.BestWins().Count} entries.");
    }

    // =====================================================================
    // Reading the screen back
    // =====================================================================

    private static Transform Find(string path)
    {
        GameObject canvas = GameObject.Find("GameCanvas");
        return canvas != null ? canvas.transform.Find(path) : null;
    }

    private static bool PanelActive(string panel)
    {
        Transform found = Find(panel);
        return found != null && found.gameObject.activeSelf;
    }

    private static string LabelText(string path)
    {
        Transform found = Find(path);
        Text text = found != null ? found.GetComponent<Text>() : null;
        return text != null ? text.text : "MISSING";
    }

    private static string StatusSize()
    {
        Transform found = Find("ProfilePanel/StatusText");
        Text text = found != null ? found.GetComponent<Text>() : null;
        return text != null ? $"{text.fontSize:0} px" : "MISSING";
    }

    private static string TurnText() => LabelText("HUDPanel/TurnText");

    private static string CountsText() => LabelText("HUDPanel/CountsText");

    private static string Measure(string path)
    {
        RectTransform rect = Find(path) as RectTransform;
        if (rect == null) return "MISSING";

        return $"{rect.rect.width:0}x{rect.rect.height:0} at ({rect.anchoredPosition.x:0}, {rect.anchoredPosition.y:0})";
    }

    // =====================================================================
    // Board state, by reflection - the harness must not need the game to expose these
    // =====================================================================

    private static void ClearGrid()
    {
        for (int x = 0; x < DamaBoard.Size; x++)
            for (int y = 0; y < DamaBoard.Size; y++)
                SetCell(x, y, 0);
    }

    private static int CellAt(int x, int y)
    {
        FieldInfo field = typeof(DamaBoard).GetField("grid", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null) return 0;

        Array grid = field.GetValue(board) as Array;
        if (grid == null) return 0;

        return Convert.ToInt32(grid.GetValue(x, y));
    }

    private static void SetCell(int x, int y, int piece)
    {
        FieldInfo field = typeof(DamaBoard).GetField("grid", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null) { Debug.LogError("[Week15Captures] DamaBoard.grid not found."); return; }

        Array grid = field.GetValue(board) as Array;
        if (grid == null) return;

        grid.SetValue(Enum.ToObject(grid.GetType().GetElementType(), piece), x, y);
    }

    private static void SetCurrentPlayer(int player)
    {
        FieldInfo field = typeof(DamaBoard).GetField("currentPlayer", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null) { Debug.LogError("[Week15Captures] DamaBoard.currentPlayer not found."); return; }
        field.SetValue(board, player);
    }

    private static void Invoke(string method, params object[] args)
    {
        MethodInfo info = typeof(DamaBoard).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (info == null)
        {
            Debug.LogError($"[Week15Captures] DamaBoard.{method} not found.");
            return;
        }

        try
        {
            info.Invoke(board, args);
        }
        catch (TargetInvocationException exception)
        {
            Debug.LogError($"[Week15Captures] {method} threw: {exception.InnerException?.Message}");
        }
    }

    // =====================================================================
    // Capture plumbing
    // =====================================================================

    private static void Advance(int nextStage)
    {
        stage = nextStage;
        nextActionAt = EditorApplication.timeSinceStartup + StageSettleSeconds;
    }

    private static void Request(string fileName)
    {
        string path = Path.Combine(EvidenceDir, fileName);

        if (File.Exists(path)) File.Delete(path);
        ScreenCapture.CaptureScreenshot(path);
        pendingShot = path;

        Note($"Requested {fileName} -> {path}");
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

    private static void Note(string line)
    {
        string stamped = $"{DateTime.Now:HH:mm:ss}  {line}";
        transcript.Add(stamped);
        Debug.Log($"[Week15Captures] {line}");
    }

    private static void Finish()
    {
        File.WriteAllText(Path.Combine(EvidenceDir, "week15_shots.txt"), BuildTranscript(), new UTF8Encoding(false));
        Debug.Log($"[Week15Captures] Wrote week15_shots.txt with {transcript.Count} line(s).");

        running = false;
        EditorApplication.update -= Tick;
        EditorApplication.ExitPlaymode();
    }

    private static void Abort(string reason)
    {
        Debug.LogError($"[Week15Captures] Aborted at stage {stage}: {reason}.");

        running = false;
        EditorApplication.update -= Tick;
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
    }

    private static string BuildTranscript()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("DAMA - WEEK 15 - CAPTURE RUN TRANSCRIPT");
        builder.AppendLine("Student : Charibelle Guzman (BSIT-4)");
        builder.AppendLine("Scene   : Assets/Scenes/DamaGame.unity");
        builder.AppendLine("Harness : Tools > Dama > Week 15 > 4. Capture Evidence");
        builder.AppendLine($"Run at  : {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC   Unity {Application.unityVersion}");
        builder.AppendLine("Canvas  : 1280x720 reference, match 0.50   Game view at capture: " +
                           $"{Screen.width}x{Screen.height}");
        builder.AppendLine();
        builder.AppendLine("This file is the harness's own record of the run, in order. The \"Requested\" line is the");
        builder.AppendLine("screenshot call; the \"Wrote\" line for the same file is the moment the PNG existed on disk with");
        builder.AppendLine("a non-zero length. Those pairs are the gate doing its work: ScreenCapture completes a frame or");
        builder.AppendLine("two after the call, so a run that does not wait writes empty files.");
        builder.AppendLine(new string('-', 96));
        builder.AppendLine("RUN LOG");
        builder.AppendLine(new string('-', 96));
        foreach (string line in transcript) builder.AppendLine(line);
        builder.AppendLine(new string('-', 96));
        builder.AppendLine("WHAT EACH IMAGE IS");
        builder.AppendLine("  E1_pause_menu.png      the pause panel: RESUME, RESTART, SOUND, and MENU on the restart line.");
        builder.AppendLine("                         MENU sits beside RESTART, not under it - four 96 px controls do not fit");
        builder.AppendLine("                         vertically in the 357 px band the title leaves.");
        builder.AppendLine("  E2_end_screen.png      the end screen after BLUE was left with a man and no legal move. The");
        builder.AppendLine("                         summary line names the winner, both counts and the reason, and MENU");
        builder.AppendLine("                         sits beside RESTART.");
        builder.AppendLine("  E3_profile_status.png  the profile panel, seeded to wins=2 losses=1, with the status line at");
        builder.AppendLine("                         20 px - it was 17 px before this week's fix.");
        builder.AppendLine("  E4_mid_match.png       the board after RED jumped a BLUE man at (3,4) and landed at (4,5),");
        builder.AppendLine("                         with the HUD showing whose turn it is and both counts.");
        builder.AppendLine();
        builder.AppendLine("The states were produced through the game's own entry points (StartGame, PauseGame,");
        builder.AppendLine("ReturnToMenu, ShowProfile, ExecuteMove, CheckForWin). Only the board seeding is done by");
        builder.AppendLine("reflection, the same way the Week 13-15 harnesses seed a position.");
        builder.AppendLine();
        builder.AppendLine("Seeding the grid by reflection changes what the game believes is on the board, not what is");
        builder.AppendLine("drawn on it: on the first attempt E2 showed a HUD reading Red: 12 Blue: 12 beside a summary");
        builder.AppendLine("reading RED 1-1. The two disagreed because the 24 opening pieces were still rendered. The");
        builder.AppendLine("harness now rebuilds the visible pieces from the grid before each capture and refreshes the HUD,");
        builder.AppendLine("so every image shows the state it claims to show. The \"Rebuilt N visible piece(s)\" lines above are");
        builder.AppendLine("that step, and they are what makes the counts in the pictures checkable.");
        return builder.ToString();
    }
}
