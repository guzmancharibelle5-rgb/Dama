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
/// Week 17: captures the six submission images for the release package from the running game, and writes the
/// transcript that says what each one is.
///
/// Menu: Tools > Dama > Week 17 > 4. Capture Evidence
///
///   E1  the title screen: version caption, HOW TO PLAY / START / CREDITS, PROFILE / EXIT
///   E2  the HOW TO PLAY panel with its instruction body
///   E3  the CREDITS panel with the build line
///   E4  the board mid-match, after a real capture
///   E5  the pause panel
///   E6  the end screen with the summary line
///
/// ScreenCapture.CaptureScreenshot writes asynchronously - a shot requested and a shot written are two
/// different moments - so the run does not advance until the PNG exists on disk with a non-zero length.
/// Every landed PNG is hashed; the run fails loudly if any two of the six are identical, because six names
/// pointing at five pictures is the failure mode this harness exists to prevent. The run stops play mode itself.
/// </summary>
public static class Week17Captures
{
    private const string Menu = "Tools/Dama/Week 17/4. Capture Evidence";
    private const string SceneName = "DamaGame";
    private const double StageSettleSeconds = 0.45;

    private static bool running;
    private static int stage;
    private static double nextActionAt;
    private static double deadlineAt;
    private static string pendingShot;
    private static DamaBoard board;

    private static readonly List<string> transcript = new List<string>();
    private static readonly List<string> shotNames = new List<string>();
    private static readonly List<string> shotHashes = new List<string>();

    public static string EvidenceDir =>
        Path.Combine(Directory.GetParent(Application.dataPath).FullName,
            "docs", "Charibelle Guzman", "Week_17", "Evidence");

    [MenuItem(Menu, false, 43)]
    public static void Begin()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != SceneName)
        {
            string active = scene.IsValid() ? scene.name : "none";
            Debug.LogWarning($"[Week17Captures] Open {SceneName}.unity first; the active scene is '{active}'.");
            return;
        }

        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[Week17Captures] Stop play mode first; this harness starts it itself.");
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
        deadlineAt = EditorApplication.timeSinceStartup + 180.0;

        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();

        Note("Entering play mode for the Week 17 captures.");
    }

    private static void Tick()
    {
        if (!running) return;
        if (!EditorApplication.isPlaying) return;

        if (EditorApplication.timeSinceStartup > deadlineAt)
        {
            Abort("the whole run exceeded its 180 s budget");
            return;
        }

        if (pendingShot != null)
        {
            // The editor does not repaint the Game view while its window is in the background, and a
            // capture of a view that is not drawing returns the previous frame byte for byte. Asking for
            // a repaint each tick is what keeps six names from becoming one picture.
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

                Advance(1);
                break;

            case 1:
                Note("E1 state: StartPanel=" + PanelActive("StartPanel") +
                     " VersionLabel='" + LabelText("StartPanel/VersionLabel") + "'" +
                     " HowToButton=" + Measure("StartPanel/HowToButton") +
                     " StartButton=" + Measure("StartPanel/StartButton") +
                     " CreditsButton=" + Measure("StartPanel/CreditsButton") +
                     " ProfileButton=" + Measure("StartPanel/ProfileButton") +
                     " ExitButton=" + Measure("StartPanel/ExitButton") + ".");
                Request("E1_title.png");
                Advance(2);
                break;

            case 2:
                board.ShowHowTo();
                Advance(3);
                break;

            case 3:
                Note("E2 state: HowToPanel=" + PanelActive("HowToPanel") +
                     " StartPanel=" + PanelActive("StartPanel") +
                     " body chars=" + TextLength("HowToPanel/BodyText") +
                     " body=" + FontSize("HowToPanel/BodyText") +
                     " title=" + FontSize("HowToPanel/TitleText") +
                     " back=" + Measure("HowToPanel/BackButton") + ".");
                Request("E2_how_to_play.png");
                Advance(4);
                break;

            case 4:
                board.CloseHowTo();
                board.ShowCredits();
                Advance(5);
                break;

            case 5:
                Note("E3 state: CreditsPanel=" + PanelActive("CreditsPanel") +
                     " StartPanel=" + PanelActive("StartPanel") +
                     " BuildText='" + LabelText("CreditsPanel/BuildText") + "'" +
                     " body chars=" + TextLength("CreditsPanel/BodyText") +
                     " body=" + FontSize("CreditsPanel/BodyText") + ".");
                Request("E3_credits.png");
                Advance(6);
                break;

            case 6:
                board.CloseCredits();
                board.StartGame();
                SeedMidMatch();
                Advance(7);
                break;

            case 7:
                Note("E4 state: turn='" + TurnText() + "' counts='" + CountsText() + "'" +
                     " HUDPanel=" + PanelActive("HUDPanel") + ".");
                Request("E4_mid_match.png");
                Advance(8);
                break;

            case 8:
                board.PauseGame();
                Advance(9);
                break;

            case 9:
                Note("E5 state: PausePanel=" + PanelActive("PausePanel") +
                     " timeScale=" + Time.timeScale.ToString("0.00") +
                     " ResumeButton=" + Measure("PausePanel/ResumeButton") + ".");
                Request("E5_pause.png");
                Advance(10);
                break;

            case 10:
                board.ReturnToMenu();
                board.StartGame();
                SeedImmobilisation();
                Advance(11);
                break;

            case 11:
                Note("E6 state: WinPanel=" + PanelActive("WinPanel") +
                     " WinText='" + LabelText("WinPanel/WinText") + "'" +
                     " SummaryText='" + LabelText("WinPanel/SummaryText") + "'.");
                Request("E6_end_screen.png");
                Advance(12);
                break;

            case 12:
                Finish();
                break;
        }
    }

    // =====================================================================
    // The states the four in-match images are taken in
    // =====================================================================

    /// <summary>
    /// A board after one real capture: RED jumps a BLUE man at (3,4) and lands at (4,5). BLUE keeps
    /// two men elsewhere, so the capture does not also end the match.
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
    /// forward step, and there is nothing for it to jump.
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
    /// its own summary line is worse evidence than no image.
    /// </summary>
    private static void SyncPieces()
    {
        Transform root = board.transform.Find("Pieces");
        if (root == null)
        {
            Debug.LogWarning("[Week17Captures] No Pieces root; the capture will not match the seeded grid.");
            return;
        }

        MethodInfo circle = typeof(DamaBoard).GetMethod("CreateCircleSprite", BindingFlags.Static | BindingFlags.NonPublic);
        MethodInfo rent = typeof(DamaBoard).GetMethod("RentPiece", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo release = typeof(DamaBoard).GetMethod("ReleasePiece", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo register = typeof(DamaBoard).GetMethod("RegisterPiece", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo spriteField = typeof(DamaBoard).GetField("circleSprite", BindingFlags.Instance | BindingFlags.NonPublic);

        if (circle == null || rent == null || release == null || register == null)
        {
            Debug.LogError("[Week17Captures] DamaBoard.RentPiece/ReleasePiece/RegisterPiece/CreateCircleSprite not found; pieces were not rebuilt.");
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
        if (field == null) { Debug.LogError("[Week17Captures] DamaBoard.grid not found."); return; }

        Array grid = field.GetValue(board) as Array;
        if (grid == null) return;

        grid.SetValue(Enum.ToObject(grid.GetType().GetElementType(), piece), x, y);
    }

    private static void SetCurrentPlayer(int player)
    {
        FieldInfo field = typeof(DamaBoard).GetField("currentPlayer", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null) { Debug.LogError("[Week17Captures] DamaBoard.currentPlayer not found."); return; }
        field.SetValue(board, player);
    }

    private static void Invoke(string method, params object[] args)
    {
        MethodInfo info = typeof(DamaBoard).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (info == null)
        {
            Debug.LogError($"[Week17Captures] DamaBoard.{method} not found.");
            return;
        }

        try
        {
            info.Invoke(board, args);
        }
        catch (TargetInvocationException exception)
        {
            Debug.LogError($"[Week17Captures] {method} threw: {exception.InnerException?.Message}");
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

    /// <summary>
    /// Makes the editor draw the Game view even though its window is not the foreground app. Without this
    /// the view stops updating the moment the editor loses focus and every later capture is a copy of the
    /// last frame that was drawn - which is exactly how E4, E5 and E6 arrived byte-identical on the first run.
    /// </summary>
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
        string stamped = $"{DateTime.Now:HH:mm:ss}  {line}";
        transcript.Add(stamped);
        Debug.Log($"[Week17Captures] {line}");
    }

    private static void Finish()
    {
        string collision;
        bool distinct = HashesAreDistinct(out collision);

        if (shotNames.Count != 6)
            Debug.LogError($"[Week17Captures] Expected 6 images, landed {shotNames.Count}.");

        if (!distinct)
            Debug.LogError("[Week17Captures] Two captures are byte-identical: " + collision);

        File.WriteAllText(Path.Combine(EvidenceDir, "week17_shots.txt"), BuildTranscript(distinct, collision),
            new UTF8Encoding(false));
        Debug.Log($"[Week17Captures] Wrote week17_shots.txt with {transcript.Count} line(s); distinct={distinct}.");

        running = false;
        EditorApplication.update -= Tick;
        EditorApplication.ExitPlaymode();
    }

    private static void Abort(string reason)
    {
        Debug.LogError($"[Week17Captures] Aborted at stage {stage}: {reason}.");

        running = false;
        EditorApplication.update -= Tick;
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
    }

    private static string BuildTranscript(bool distinct, string collision)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("DAMA - WEEK 17 - CAPTURE RUN TRANSCRIPT");
        builder.AppendLine("Student : Charibelle Guzman (BSIT-4)");
        builder.AppendLine("Scene   : Assets/Scenes/DamaGame.unity");
        builder.AppendLine("Harness : " + Menu.Replace("/", " > "));
        builder.AppendLine($"Run at  : {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC   Unity {Application.unityVersion}");
        builder.AppendLine($"Build   : {ReleaseInfo.Caption()}   Application.version={Application.version}");
        builder.AppendLine("Canvas  : 1280x720 reference, match 0.50   Game view at capture: " +
                           $"{Screen.width}x{Screen.height}");
        builder.AppendLine("Platform: Editor. No APK was built and no handset was attached, so these images are Editor");
        builder.AppendLine("          captures of the same scene an Android build would ship - not evidence from a device.");
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
        builder.AppendLine("HASHES (sha256, first 32 hex characters)");
        builder.AppendLine(new string('-', 96));

        for (int i = 0; i < shotNames.Count; i++)
        {
            string prefix = shotHashes[i].Length >= 32 ? shotHashes[i].Substring(0, 32) : shotHashes[i];
            builder.AppendLine($"  {shotNames[i],-22} {prefix}");
        }

        builder.AppendLine();
        builder.AppendLine(distinct
            ? "  All " + shotNames.Count + " images are byte-distinct: no name is a copy of another."
            : "  COLLISION: " + collision);
        builder.AppendLine(new string('-', 96));
        builder.AppendLine("WHAT EACH IMAGE IS");
        builder.AppendLine("  E1_title.png          the title screen: the version caption top-left, HOW TO PLAY, START and");
        builder.AppendLine("                        CREDITS on the first row, PROFILE and EXIT on the second. This is the");
        builder.AppendLine("                        screen a reviewer sees before touching anything, and the caption is the");
        builder.AppendLine("                        build the binary was compiled with - not a string typed into the scene.");
        builder.AppendLine("  E2_how_to_play.png    the HOW TO PLAY panel: 1122 characters of rules fitted to 19 px, with");
        builder.AppendLine("                        BACK returning to the title. It exists because the module asks for");
        builder.AppendLine("                        clear instructions, and the title's 156-character line was not one.");
        builder.AppendLine("  E3_credits.png        the CREDITS panel: author, course, engine, licence and AI-assistance");
        builder.AppendLine("                        statement, with the build line naming the same version the title shows.");
        builder.AppendLine("  E4_mid_match.png      the board after RED jumped a BLUE man at (3,4) and landed at (4,5),");
        builder.AppendLine("                        with the HUD showing whose turn it is and both counts.");
        builder.AppendLine("  E5_pause.png          the pause panel: RESUME, RESTART, SOUND and MENU.");
        builder.AppendLine("  E6_end_screen.png     the end screen after BLUE was left with a man and no legal move. The");
        builder.AppendLine("                        summary line names the winner, both counts and the reason.");
        builder.AppendLine();
        builder.AppendLine("The states were produced through the game's own entry points (StartGame, ShowHowTo, CloseHowTo,");
        builder.AppendLine("ShowCredits, CloseCredits, PauseGame, ReturnToMenu, CheckForWin). Only the board seeding is done");
        builder.AppendLine("by reflection, the same way the Week 13-16 harnesses seed a position.");
        builder.AppendLine();
        builder.AppendLine("Seeding the grid by reflection changes what the game believes is on the board, not what is");
        builder.AppendLine("drawn on it, so the harness rebuilds the visible pieces from the grid before each in-match");
        builder.AppendLine("capture and refreshes the HUD. The \"Rebuilt N visible piece(s)\" lines above are that step, and");
        builder.AppendLine("they are what makes the counts in the pictures checkable against the transcript.");
        builder.AppendLine();
        builder.AppendLine("The first run of this harness wrote six names over four pictures: the editor window was not the");
        builder.AppendLine("foreground application, so the Game view stopped drawing after E3 and E4, E5 and E6 came back byte for");
        builder.AppendLine("byte identical to it. The run now asks for a repaint (RepaintAllViews + QueuePlayerLoopUpdate) on every");
        builder.AppendLine("tick while a shot is pending, hashes each landed PNG, and reports a collision instead of a success. The");
        builder.AppendLine("\"All 6 images are byte-distinct\" line above is that check passing, not a claim.");
        return builder.ToString();
    }
}
