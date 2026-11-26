using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Week 18: the nine-image demonstration sequence for PA2, captured from the running game, plus the
/// transcript that says what each image is and when it was taken.
///
/// Menu: Tools > Dama > Week 18 > 1. Capture Demonstration
///
///   D1  title screen with the version caption
///   D2  HOW TO PLAY
///   D3  CREDITS with the build line
///   D4  PROFILE with a banked record and best times
///   D5  PROFILE after the two-press RESET
///   D6  a fresh match
///   D7  the same match after a real capture
///   D8  the pause panel
///   D9  the end screen
///
/// The profile images are produced against a scratch data directory, because the demonstration must
/// show a record and then erase it - and the record on this machine belongs to the person who played
/// the game, not to a harness. The real directory is restored before the match images, and the
/// transcript says so.
///
/// Same plumbing as the Week 17 harness: a shot is not advanced past until the PNG exists on disk with
/// a non-zero length, every landed PNG is hashed, and the run fails loudly if any two of the nine are
/// identical. The run stops play mode itself.
/// </summary>
public static class Week18Captures
{
    private const string Menu = "Tools/Dama/Week 18/1. Capture Demonstration";
    private const string SceneName = "DamaGame";
    private const double StageSettleSeconds = 0.45;
    private const int ShotCount = 9;

    private static bool running;
    private static int stage;
    private static double nextActionAt;
    private static double deadlineAt;
    private static double startedAt;
    private static string pendingShot;
    private static DamaBoard board;
    private static ProfilePanel profile;
    private static string realProfileDir;
    private static string scratchProfileDir;
    private static int banksDone;

    private static readonly List<string> transcript = new List<string>();
    private static readonly List<string> shotNames = new List<string>();
    private static readonly List<string> shotHashes = new List<string>();

    public static string EvidenceDir =>
        Path.Combine(Directory.GetParent(Application.dataPath).FullName,
            "docs", "Charibelle Guzman", "Week_18", "Evidence");

    [MenuItem(Menu, false, 50)]
    public static void Begin()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != SceneName)
        {
            string active = scene.IsValid() ? scene.name : "none";
            Debug.LogWarning($"[Week18Captures] Open {SceneName}.unity first; the active scene is '{active}'.");
            return;
        }

        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[Week18Captures] Stop play mode first; this harness starts it itself.");
            return;
        }

        Directory.CreateDirectory(EvidenceDir);

        transcript.Clear();
        shotNames.Clear();
        shotHashes.Clear();
        stage = 0;
        running = true;
        pendingShot = null;
        board = null;
        profile = null;
        realProfileDir = null;
        scratchProfileDir = null;
        deadlineAt = EditorApplication.timeSinceStartup + 240.0;
        startedAt = EditorApplication.timeSinceStartup;

        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();

        Note("Entering play mode for the Week 18 demonstration sequence.");
    }

    private static void Tick()
    {
        if (!running) return;
        if (!EditorApplication.isPlaying) return;

        if (EditorApplication.timeSinceStartup > deadlineAt)
        {
            Abort("the whole run exceeded its 240 s budget");
            return;
        }

        if (pendingShot != null)
        {
            // The editor does not repaint the Game view while its window is in the background, and a
            // capture of a view that is not drawing returns the previous frame byte for byte.
            ForceRepaint();

            if (ShotLanded(pendingShot))
            {
                string name = Path.GetFileName(pendingShot);
                long bytes = new FileInfo(pendingShot).Length;
                string hash = Hash(pendingShot);

                shotNames.Add(name);
                shotHashes.Add(hash);
                Note($"Wrote {name} ({bytes} bytes, sha256 {hash.Substring(0, 16)}...).");

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

                profile = UnityEngine.Object.FindAnyObjectByType<ProfilePanel>(FindObjectsInactive.Include);
                if (profile == null) Debug.LogWarning("[Week18Captures] No ProfilePanel found; D4 and D5 will fail.");

                realProfileDir = PlayerProfile.DataDirectory;
                Note($"Real profile directory: {realProfileDir}");
                Advance(1);
                break;

            case 1:
                Note("D1 state: StartPanel=" + PanelActive("StartPanel") +
                     " VersionLabel='" + LabelText("StartPanel/VersionLabel") + "'" +
                     $" Application.version='{Application.version}'" +
                     " HowToButton=" + Measure("StartPanel/HowToButton") +
                     " StartButton=" + Measure("StartPanel/StartButton") +
                     " CreditsButton=" + Measure("StartPanel/CreditsButton") +
                     " ProfileButton=" + Measure("StartPanel/ProfileButton") +
                     " ExitButton=" + Measure("StartPanel/ExitButton") + ".");
                Request("D1_title.png");
                Advance(2);
                break;

            case 2:
                board.ShowHowTo();
                Advance(3);
                break;

            case 3:
                Note("D2 state: HowToPanel=" + PanelActive("HowToPanel") +
                     " StartPanel=" + PanelActive("StartPanel") +
                     " body chars=" + TextLength("HowToPanel/BodyText") +
                     " body=" + FontSize("HowToPanel/BodyText") +
                     " back=" + Measure("HowToPanel/BackButton") + ".");
                Request("D2_how_to_play.png");
                Advance(4);
                break;

            case 4:
                board.CloseHowTo();
                board.ShowCredits();
                Advance(5);
                break;

            case 5:
                Note("D3 state: CreditsPanel=" + PanelActive("CreditsPanel") +
                     " BuildText='" + LabelText("CreditsPanel/BuildText") + "'" +
                     " body chars=" + TextLength("CreditsPanel/BodyText") + ".");
                Request("D3_credits.png");
                Advance(6);
                break;

            case 6:
                board.CloseCredits();
                BeginScratchProfile();
                board.StartGame();
                banksDone = 0;
                Advance(7);
                break;

            case 7:
                // One bank per pass through this stage, so each match's duration is the real elapsed
                // time between the start and the bank instead of the microseconds of a single tick.
                if (banksDone < 3)
                {
                    BankScratchMatch(banksDone == 2 ? 2 : 1);
                    banksDone++;
                    Advance(7);
                    break;
                }

                // ShowProfile only answers from the title screen, and banking left the board mid-match.
                board.ReturnToMenu();
                board.ShowProfile();

                Note("D4 state: ProfilePanel=" + PanelActive("ProfilePanel") +
                     " RecordText='" + OneLine(LabelText("ProfilePanel/RecordText")) + "'" +
                     " BestText='" + OneLine(LabelText("ProfilePanel/BestText")) + "'" +
                     " StatusText='" + LabelText("ProfilePanel/StatusText") + "'" +
                     " dir='" + PlayerProfile.DataDirectory + "'.");
                Request("D4_profile_record.png");
                Advance(8);
                break;

            case 8:
                if (profile == null) { Abort("no ProfilePanel to press RESET on"); return; }

                profile.ResetPressed();
                Note("D5 step 1: RESET pressed once - ResetLabel='" +
                     LabelText("ProfilePanel/ResetButton/ResetLabel") + "'" +
                     " StatusText='" + LabelText("ProfilePanel/StatusText") + "'.");
                Advance(9);
                break;

            case 9:
                profile.ResetPressed();
                Note("D5 step 2: RESET pressed again - ResetLabel='" +
                     LabelText("ProfilePanel/ResetButton/ResetLabel") + "'" +
                     " RecordText='" + OneLine(LabelText("ProfilePanel/RecordText")) + "'" +
                     " BestText='" + OneLine(LabelText("ProfilePanel/BestText")) + "'" +
                     " StatusText='" + LabelText("ProfilePanel/StatusText") + "'.");
                Request("D5_profile_after_reset.png");
                Advance(10);
                break;

            case 10:
                board.CloseProfile();
                board.StartGame();
                Advance(11);
                break;

            case 11:
                Note("D6 state: HUDPanel=" + PanelActive("HUDPanel") +
                     " turn='" + LabelText("HUDPanel/TurnText") + "'" +
                     " counts='" + LabelText("HUDPanel/CountsText") + "'" +
                     " StartPanel=" + PanelActive("StartPanel") + ".");
                Request("D6_match_fresh.png");
                Advance(12);
                break;

            case 12:
                SeedMidMatch();
                Advance(13);
                break;

            case 13:
                Note("D7 state: a RED man jumped a BLUE man (3,4) -> (4,5); turn='" + LabelText("HUDPanel/TurnText") +
                     "' counts='" + LabelText("HUDPanel/CountsText") + "'.");
                Request("D7_match_after_capture.png");
                Advance(14);
                break;

            case 14:
                board.PauseGame();
                Advance(15);
                break;

            case 15:
                Note("D8 state: PausePanel=" + PanelActive("PausePanel") +
                     " timeScale=" + Time.timeScale.ToString("0.00") +
                     " ResumeButton=" + Measure("PausePanel/ResumeButton") + ".");
                Request("D8_pause.png");
                Advance(16);
                break;

            case 16:
                board.ResumeGame();
                board.ReturnToMenu();
                board.StartGame();
                SeedImmobilisation();
                Advance(17);
                break;

            case 17:
                Note("D9 state: WinPanel=" + PanelActive("WinPanel") +
                     " WinText='" + LabelText("WinPanel/WinText") + "'" +
                     " SummaryText='" + LabelText("WinPanel/SummaryText") + "'.");
                Request("D9_end_screen.png");
                Advance(18);
                break;

            case 18:
                // The D9 match ended through the game's own win check, which banks it - so the real
                // store is only pointed back at its own directory once every match image is written.
                RestoreRealProfileDir();
                Finish();
                break;
        }
    }

    // =====================================================================
    // The states the images are taken in
    // =====================================================================

    /// <summary>
    /// Points the profile store at a scratch directory, so the demonstration can show a record and
    /// then erase it without touching the record of whoever actually played on this machine.
    /// </summary>
    private static void BeginScratchProfile()
    {
        try
        {
            scratchProfileDir = Path.Combine(Path.GetTempPath(), "DamaWeek18Demo");
            if (Directory.Exists(scratchProfileDir)) Directory.Delete(scratchProfileDir, true);
            Directory.CreateDirectory(scratchProfileDir);

            PlayerProfile.SetDataDirectory(scratchProfileDir);
            PlayerProfile.ResetCacheForTests();
            PlayerProfile.Load();

            Note($"Scratch profile opened at {scratchProfileDir}: matches={PlayerProfile.MatchesPlayed}.");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[Week18Captures] Opening the scratch profile failed: {exception.Message}");
        }
    }

    /// <summary>Banks one match through the game's own path and starts the next, timed by the game clock.</summary>
    private static void BankScratchMatch(int winner)
    {
        board.BankMatchForTests(winner);

        Note($"Scratch match {PlayerProfile.LastMatchId} banked: matches={PlayerProfile.MatchesPlayed}" +
             $" wins={PlayerProfile.Wins} losses={PlayerProfile.Losses} bestStreak={PlayerProfile.BestStreak}" +
             $" bestWins={PlayerProfile.BestWins().Count}.");

        board.RestartGame();
    }

    private static void RestoreRealProfileDir()
    {
        if (string.IsNullOrEmpty(realProfileDir)) return;

        PlayerProfile.SetDataDirectory(realProfileDir);
        PlayerProfile.ResetCacheForTests();
        PlayerProfile.Load();

        Note($"Profile directory restored to {PlayerProfile.DataDirectory}: matches={PlayerProfile.MatchesPlayed}.");
    }

    /// <summary>A board after one real capture: RED jumps a BLUE man at (3,4) and lands at (4,5).</summary>
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

    /// <summary>BLUE still holds a man but has no legal move, so the match ends on immobilisation.</summary>
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

    /// <summary>Rebuilds the visible pieces from the grid so the image matches the state it claims.</summary>
    private static void SyncPieces()
    {
        Transform root = board.transform.Find("Pieces");
        if (root == null)
        {
            Debug.LogWarning("[Week18Captures] No Pieces root; the capture will not match the seeded grid.");
            return;
        }

        MethodInfo circle = typeof(DamaBoard).GetMethod("CreateCircleSprite", BindingFlags.Static | BindingFlags.NonPublic);
        MethodInfo rent = typeof(DamaBoard).GetMethod("RentPiece", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo release = typeof(DamaBoard).GetMethod("ReleasePiece", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo register = typeof(DamaBoard).GetMethod("RegisterPiece", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo spriteField = typeof(DamaBoard).GetField("circleSprite", BindingFlags.Instance | BindingFlags.NonPublic);

        if (circle == null || rent == null || release == null || register == null)
        {
            Debug.LogError("[Week18Captures] DamaBoard.RentPiece/ReleasePiece/RegisterPiece/CreateCircleSprite not found; pieces were not rebuilt.");
            return;
        }

        Sprite sprite = spriteField != null ? spriteField.GetValue(board) as Sprite : null;
        if (sprite == null) sprite = circle.Invoke(null, null) as Sprite;

        // Park whatever is drawn first: the pool must keep its pieces, so they are released, not destroyed.
        List<GameObject> drawn = new List<GameObject>();
        for (int i = 0; i < root.childCount; i++) drawn.Add(root.GetChild(i).gameObject);
        foreach (GameObject piece in drawn) release.Invoke(board, new object[] { piece });

        int made = 0;

        for (int x = 0; x < DamaBoard.Size; x++)
        {
            for (int y = 0; y < DamaBoard.Size; y++)
            {
                int piece = CellAt(x, y);
                if (piece == 0) continue;

                GameObject rented = rent.Invoke(board, new object[] { root, sprite, piece, x, y }) as GameObject;
                register.Invoke(board, new object[] { rented, piece, x, y });
                made++;
            }
        }

        Note($"Rebuilt {made} visible piece(s) from the grid so the image matches the state.");
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

    private static string OneLine(string value) => value == null ? "MISSING" : value.Replace("\n", " | ");

    private static string TextLength(string path)
    {
        string value = LabelText(path);
        return value == "MISSING" ? value : value.Length.ToString();
    }

    private static string FontSize(string path)
    {
        Transform found = Find(path);
        Text text = found != null ? found.GetComponent<Text>() : null;
        return text != null ? $"{text.fontSize:0} px" : "MISSING";
    }

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
        if (field == null) { Debug.LogError("[Week18Captures] DamaBoard.grid not found."); return; }

        Array grid = field.GetValue(board) as Array;
        if (grid == null) return;

        grid.SetValue(Enum.ToObject(grid.GetType().GetElementType(), piece), x, y);
    }

    private static void SetCurrentPlayer(int player)
    {
        FieldInfo field = typeof(DamaBoard).GetField("currentPlayer", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null) { Debug.LogError("[Week18Captures] DamaBoard.currentPlayer not found."); return; }
        field.SetValue(board, player);
    }

    private static void Invoke(string method, params object[] args)
    {
        MethodInfo info = typeof(DamaBoard).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (info == null)
        {
            Debug.LogError($"[Week18Captures] DamaBoard.{method} not found.");
            return;
        }

        try
        {
            info.Invoke(board, args);
        }
        catch (TargetInvocationException exception)
        {
            Debug.LogError($"[Week18Captures] {method} threw: {exception.InnerException?.Message}");
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

        ForceRepaint();
        Canvas.ForceUpdateCanvases();
        ScreenCapture.CaptureScreenshot(path);
        pendingShot = path;

        Note($"Requested {fileName} -> {path}");
    }

    /// <summary>Makes the editor draw the Game view even though its window is not the foreground app.</summary>
    private static void ForceRepaint()
    {
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        EditorApplication.QueuePlayerLoopUpdate();
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

    private static string Hash(string path)
    {
        using (FileStream stream = File.OpenRead(path))
        using (SHA256 sha = SHA256.Create())
        {
            byte[] digest = sha.ComputeHash(stream);
            StringBuilder hex = new StringBuilder(digest.Length * 2);
            foreach (byte value in digest) hex.Append(value.ToString("x2"));
            return hex.ToString();
        }
    }

    private static bool HashesAreDistinct(out string collision)
    {
        collision = "";

        for (int i = 0; i < shotHashes.Count; i++)
        {
            for (int j = i + 1; j < shotHashes.Count; j++)
            {
                if (shotHashes[i] != shotHashes[j]) continue;

                collision = shotNames[i] + " and " + shotNames[j] + " share sha256 " + shotHashes[i];
                return false;
            }
        }

        return true;
    }

    private static void Note(string line)
    {
        double offset = EditorApplication.timeSinceStartup - startedAt;
        string stamped = $"+{offset,6:0.0} s  {DateTime.Now:HH:mm:ss}  {line}";
        transcript.Add(stamped);
        Debug.Log($"[Week18Captures] {line}");
    }

    private static void Finish()
    {
        string collision;
        bool distinct = HashesAreDistinct(out collision);

        if (shotNames.Count != ShotCount)
            Debug.LogError($"[Week18Captures] Expected {ShotCount} images, landed {shotNames.Count}.");

        if (!distinct)
            Debug.LogError("[Week18Captures] Two captures are byte-identical: " + collision);

        File.WriteAllText(Path.Combine(EvidenceDir, "demo_rehearsal.txt"), BuildTranscript(distinct, collision),
            new UTF8Encoding(false));
        Debug.Log($"[Week18Captures] Wrote demo_rehearsal.txt with {transcript.Count} line(s); distinct={distinct}.");

        running = false;
        EditorApplication.update -= Tick;
        EditorApplication.ExitPlaymode();
    }

    private static void Abort(string reason)
    {
        Debug.LogError($"[Week18Captures] Aborted at stage {stage}: {reason}.");

        running = false;
        EditorApplication.update -= Tick;
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
    }

    private static string BuildTranscript(bool distinct, string collision)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("DAMA - WEEK 18 - DEMONSTRATION SEQUENCE TRANSCRIPT");
        builder.AppendLine("Student : Charibelle Guzman (BSIT-4)");
        builder.AppendLine("Scene   : Assets/Scenes/DamaGame.unity");
        builder.AppendLine("Harness : " + Menu.Replace("/", " > "));
        builder.AppendLine($"Run at  : {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC   Unity {Application.unityVersion}");
        builder.AppendLine($"Build   : {ReleaseInfo.Caption()}   Application.version={Application.version}");
        builder.AppendLine("Canvas  : 1280x720 reference, match 0.50   Game view at capture: " +
                           $"{Screen.width}x{Screen.height}");
        builder.AppendLine("Platform: Editor. No APK was built and no handset was attached, so these are Editor");
        builder.AppendLine("          captures of the scene an Android build would ship - not evidence from a device.");
        builder.AppendLine();
        builder.AppendLine("Each line carries the seconds since the run began, so the sequence can be checked against");
        builder.AppendLine("the PNG timestamps. A \"Requested\" line is the screenshot call; the \"Wrote\" line for the");
        builder.AppendLine("same file is the moment the PNG existed on disk with a non-zero length.");
        builder.AppendLine(new string('-', 96));
        builder.AppendLine("RUN LOG");
        builder.AppendLine(new string('-', 96));
        foreach (string line in transcript) builder.AppendLine(line);
        builder.AppendLine(new string('-', 96));
        builder.AppendLine("HASHES (sha256, first 32 hex characters)");
        builder.AppendLine(new string('-', 96));

        for (int i = 0; i < shotNames.Count; i++)
        {
            string prefix = shotHashes[i].Length >= 32 ? shotHashes[i].Substring(0, 32) : shotHashes[i];
            builder.AppendLine($"  {shotNames[i],-28} {prefix}");
        }

        builder.AppendLine();
        builder.AppendLine(distinct
            ? "  All " + shotNames.Count + " images are byte-distinct: no name is a copy of another."
            : "  COLLISION: " + collision);
        builder.AppendLine(new string('-', 96));
        builder.AppendLine("WHAT EACH IMAGE IS");
        builder.AppendLine("  D1_title.png                 the title screen: the version caption top-left, HOW TO PLAY, START");
        builder.AppendLine("                               and CREDITS on the first row, PROFILE and EXIT on the second.");
        builder.AppendLine("                               The caption is derived from Application.version by ReleaseInfo -");
        builder.AppendLine("                               not a string typed into the scene.");
        builder.AppendLine("  D2_how_to_play.png           the HOW TO PLAY panel: the rules, the mandatory capture, the chain,");
        builder.AppendLine("                               the flying Dama, and BACK returning to the title.");
        builder.AppendLine("  D3_credits.png               the CREDITS panel: author, course, engine, licence and the");
        builder.AppendLine("                               AI-assistance statement, with the build line naming the same");
        builder.AppendLine("                               version the title shows and the application id.");
        builder.AppendLine("  D4_profile_record.png        the PROFILE panel with a banked record and the fastest-winning");
        builder.AppendLine("                               times, produced by three real matches banked through the game's");
        builder.AppendLine("                               own RecordMatch path.");
        builder.AppendLine("  D5_profile_after_reset.png   the same panel after the two-press RESET: the record cleared and");
        builder.AppendLine("                               the status line saying so. One tap wiping a record is the defect");
        builder.AppendLine("                               the second press exists to prevent.");
        builder.AppendLine("  D6_match_fresh.png           a fresh match: twelve men a side and the HUD naming the turn.");
        builder.AppendLine("  D7_match_after_capture.png   the same match after RED jumped a BLUE man at (3,4) and landed at");
        builder.AppendLine("                               (4,5), with the HUD counts following the board.");
        builder.AppendLine("  D8_pause.png                 the pause panel: RESUME, RESTART, SOUND and MENU, with the clock");
        builder.AppendLine("                               stopped (timeScale 0).");
        builder.AppendLine("  D9_end_screen.png            the end screen after BLUE was left with a man and no legal move.");
        builder.AppendLine();
        builder.AppendLine("The profile images are taken against a scratch data directory, because the demonstration has");
        builder.AppendLine("to show a record and then erase it, and the record on this machine belongs to whoever played.");
        builder.AppendLine("PlayerProfile.SetDataDirectory points the store at a temporary folder, three matches are banked");
        builder.AppendLine("there, and the real directory is pointed back at its own folder only after D9 - because the last");
        builder.AppendLine("match ends through the game's own win check, and that check banks the match. The \"Real profile");
        builder.AppendLine("directory\" and \"Profile directory restored\" lines above are that round trip; the match");
        builder.AppendLine("images therefore bank into the temporary folder, not into the player's own record.");
        builder.AppendLine();
        builder.AppendLine("Every other state is produced through the game's own entry points (ShowHowTo, ShowCredits,");
        builder.AppendLine("ShowProfile, ResetPressed, StartGame, RestartGame, PauseGame, ResumeGame, ReturnToMenu,");
        builder.AppendLine("CheckForWin). Only the board seeding is done by reflection, and the visible pieces are then");
        builder.AppendLine("rebuilt from the grid so the picture matches the state the transcript describes.");
        return builder.ToString();
    }
}
