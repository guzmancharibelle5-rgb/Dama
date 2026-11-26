using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Week 15 test harness for Dama.
///
/// Menus:
///   Tools > Dama > Week 15 > 1. Baseline Test Run
///   Tools > Dama > Week 15 > 2. Final Test Run
///
/// Both menus run the SAME case list against whatever code is compiled at that moment, and write the
/// result to docs/Charibelle Guzman/Week_15/Evidence/test_logs_baseline/ or test_logs_final/. That is
/// what makes the baseline-to-final comparison a comparison: same harness, same thresholds, same
/// order, two dates.
///
/// The harness deliberately talks to the game the way a player does - DamaBoard's own public entry
/// points for flow, and its own rules methods for the board - so a case cannot pass by bypassing the
/// rule it is supposed to test. Members that do not exist yet (the Week 15 fixes) are reached by
/// reflection and reported as FAIL when absent: that is how the baseline run reproduces M2 and M3
/// before they are fixed, instead of the harness failing to compile.
///
/// Every case writes one row: id, requirement, status, and the measured value that decided it.
/// </summary>
public static class Week15Checks
{
    private const string BaselineMenu = "Tools/Dama/Week 15/1. Baseline Test Run";
    private const string FinalMenu = "Tools/Dama/Week 15/2. Final Test Run";

    // Week 16 seam: the same 28 cases, run again to prove the two optimizations changed no
    // behaviour. It writes under Week_16 so Week 15's committed logs cannot be overwritten.
    private const string RegressionMenu = "Tools/Dama/Week 16/4. Regression Run";

    // Week 17 seam: the same 28 cases once more, this time against the release candidate's title
    // row, HOW TO PLAY and CREDITS. Same harness and thresholds; the output lives under Week_17, so
    // neither Week 15's nor Week 16's committed logs can be rewritten by it.
    private const string ReleaseMenu = "Tools/Dama/Week 17/3. Regression Run";

    private const float TouchFloorCanvasPx = 96f;
    private const float CaptionFloorPx = 20f;
    private const float LabelFloorPx = 18f;

    private sealed class CaseResult
    {
        public string Id;
        public string Reqs;
        public string Title;
        public bool Passed;
        public string Detail;
        public string Blocked;
    }

    private static readonly List<CaseResult> results = new List<CaseResult>();
    private static readonly List<string> console = new List<string>();
    private static readonly List<Action> steps = new List<Action>();

    private static bool running;
    private static int index;
    private static double nextActionAt;
    private static double deadlineAt;
    private static string runLabel = "baseline";
    private static string runWeek = "Week_15";
    private static DamaBoard board;
    private static bool logHooked;
    private static int errorCount;

    private static FieldInfo gridField;
    private static FieldInfo kingField;
    private static FieldInfo playerField;
    private static FieldInfo chainField;

    public static string RunDir =>
        Path.Combine(Directory.GetParent(Application.dataPath).FullName,
            "docs", "Charibelle Guzman", runWeek, "Evidence", "test_logs_" + runLabel);

    [MenuItem(BaselineMenu, false, 30)]
    public static void BeginBaseline() => Begin("baseline", "Week_15");

    [MenuItem(FinalMenu, false, 31)]
    public static void BeginFinal() => Begin("final", "Week_15");

    [MenuItem(RegressionMenu, false, 41)]
    public static void BeginRegression() => Begin("regression", "Week_16");

    [MenuItem(ReleaseMenu, false, 42)]
    public static void BeginRelease() => Begin("regression", "Week_17");

    private static void Begin(string label, string week = "Week_15")
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[Week15] Stop play mode first; this harness starts it itself.");
            return;
        }

        runLabel = label;
        runWeek = week;
        results.Clear();
        console.Clear();
        steps.Clear();
        index = 0;
        errorCount = 0;
        board = null;
        running = true;

        Directory.CreateDirectory(RunDir);

        deadlineAt = EditorApplication.timeSinceStartup + 300.0;

        if (!logHooked)
        {
            Application.logMessageReceived += OnLog;
            logHooked = true;
        }

        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();

        Debug.Log($"[Week15] Entering play mode for the {label.ToUpper()} test run.");
    }

    // =====================================================================
    // Stage machine
    // =====================================================================

    private static void Tick()
    {
        if (!running) return;
        if (!EditorApplication.isPlaying) return;

        if (EditorApplication.timeSinceStartup > deadlineAt)
        {
            Abort("the run exceeded its 300 s budget");
            return;
        }

        if (Time.frameCount < 5) return;
        if (EditorApplication.timeSinceStartup < nextActionAt) return;

        if (board == null)
        {
            board = UnityEngine.Object.FindFirstObjectByType<DamaBoard>();
            if (board == null) { Abort("no DamaBoard in the open scene"); return; }

            gridField = typeof(DamaBoard).GetField("grid", BindingFlags.Instance | BindingFlags.NonPublic);
            kingField = typeof(DamaBoard).GetField("isKing", BindingFlags.Instance | BindingFlags.NonPublic);
            playerField = typeof(DamaBoard).GetField("currentPlayer", BindingFlags.Instance | BindingFlags.NonPublic);
            chainField = typeof(DamaBoard).GetField("chainPiece", BindingFlags.Instance | BindingFlags.NonPublic);

            if (gridField == null || kingField == null || playerField == null || chainField == null)
            {
                Abort("DamaBoard's private state (grid / isKing / currentPlayer / chainPiece) was not found");
                return;
            }

            BuildSteps();
        }

        if (index >= steps.Count) { Finish(); return; }

        try
        {
            steps[index]();
        }
        catch (Exception exception)
        {
            Record("TC-INTERNAL", "harness", "harness step " + index, false,
                "threw " + exception.GetType().Name + ": " + exception.Message);
            Debug.LogError($"[Week15] Step {index} threw: {exception}");
        }

        index++;
        nextActionAt = EditorApplication.timeSinceStartup + 0.05;
    }

    private static void BuildSteps()
    {
        steps.Add(CaseFlow01_TitleScreen);
        steps.Add(CaseFlow02_StartMatch);
        steps.Add(CaseFlow03_PauseResume);
        steps.Add(CaseFlow04_SoundToggle);
        steps.Add(CaseFlow05_ReturnToMenu);

        steps.Add(CaseRule01_MandatoryCapture);
        steps.Add(CaseRule02_CaptureRemovesPiece);
        steps.Add(CaseRule03_ChainContinues);
        steps.Add(CaseRule04_RedPromotes);
        steps.Add(CaseRule05_BluePromotes);
        steps.Add(CaseRule06_FlyingDama);
        steps.Add(CaseRule07_WinByElimination);
        steps.Add(CaseRule08_WinByImmobilisation);
        steps.Add(CaseRule09_EndScreenControls);

        steps.Add(CaseData01_NameAndSide);
        steps.Add(CaseData02_OneBankPerMatch);
        steps.Add(CaseData03_Reset);
        steps.Add(CaseData04_CorruptSave);
        steps.Add(CaseData05_LocalOnly);

        steps.Add(CaseA11y01_TouchFloor);
        steps.Add(CaseA11y02_NoOverlap);
        steps.Add(CaseA11y03_TypeSizes);

        steps.Add(CaseBound01_RapidInput);
        steps.Add(CaseBound02_RapidPauseResume);
        steps.Add(CaseBlockedCompatibility);
    }

    // =====================================================================
    // Flow
    // =====================================================================

    private static void CaseFlow01_TitleScreen()
    {
        Time.timeScale = 1f;

        bool startOn = Active("StartPanel");
        bool hudOff = !Active("HUDPanel");
        bool pauseOff = !Active("PausePanel");
        bool winOff = !Active("WinPanel");

        Record("TC-FLOW-01", "REQ-01", "Launched scene shows the title screen", startOn && hudOff && pauseOff && winOff,
            $"StartPanel={startOn} HUDPanel={hudOff} PausePanel={pauseOff} WinPanel={winOff}");

        Text instructions = FindText("StartPanel/InstructionsText");
        Record("TC-FLOW-02", "REQ-01", "Title screen states the rules", instructions != null && instructions.text.Length > 40,
            instructions == null ? "InstructionsText missing" : $"chars={instructions.text.Length}");
    }

    private static void CaseFlow02_StartMatch()
    {
        board.StartGame();

        int pieces = TotalPieces();
        Text turn = FindText("HUDPanel/TurnText");
        Text counts = FindText("HUDPanel/CountsText");

        string turnText = turn != null ? turn.text : "MISSING";
        string countsText = counts != null ? counts.text : "MISSING";

        bool pass = Active("HUDPanel") && !Active("StartPanel") && pieces == 24 &&
                    turnText.Contains("RED") && countsText.Contains("12");

        Record("TC-FLOW-03", "REQ-02", "START begins a match with 24 pieces, RED to move, counts shown", pass,
            $"pieces={pieces} turn='{turnText}' counts='{countsText}'");
    }

    private static void CaseFlow03_PauseResume()
    {
        board.PauseGame();
        bool paused = Active("PausePanel") && Mathf.Approximately(Time.timeScale, 0f);
        int turnAtPause = CurrentPlayer();
        string labelAtPause = SoundLabel();

        board.ResumeGame();
        bool resumed = !Active("PausePanel") && Mathf.Approximately(Time.timeScale, 1f) && CurrentPlayer() == turnAtPause;

        Record("TC-FLOW-04", "REQ-10", "PAUSE freezes the board; RESUME returns to the same side to move", paused && resumed,
            $"paused={paused} pausedTurn={turnAtPause} resumed={resumed} resumedTurn={CurrentPlayer()} soundLabel='{labelAtPause}'");
    }

    private static void CaseFlow04_SoundToggle()
    {
        board.PauseGame();

        bool before = GameFeedback.Instance != null ? GameFeedback.Instance.SoundEnabled : true;
        board.ToggleSound();
        bool afterOff = GameFeedback.Instance != null ? GameFeedback.Instance.SoundEnabled : true;
        string labelOff = SoundLabel();
        int prefOff = PlayerPrefs.GetInt(GameFeedback.SoundPrefsKey, 1);

        board.ToggleSound();
        bool afterOn = GameFeedback.Instance != null ? GameFeedback.Instance.SoundEnabled : true;
        string labelOn = SoundLabel();
        int prefOn = PlayerPrefs.GetInt(GameFeedback.SoundPrefsKey, 1);

        bool pass = before && !afterOff && afterOn &&
                    labelOff == "SOUND: OFF" && labelOn == "SOUND: ON" &&
                    prefOff == 0 && prefOn == 1;

        Record("TC-FLOW-05", "REQ-11", "SOUND toggle silences feedback, spells its state and survives in PlayerPrefs", pass,
            $"enabled {before}->{afterOff}->{afterOn} label '{labelOff}' then '{labelOn}' prefs {prefOff}/{prefOn}");

        board.ResumeGame();
    }

    private static void CaseFlow05_ReturnToMenu()
    {
        board.PauseGame();

        Transform button = Find("PausePanel/MenuButton");
        MethodInfo returnMethod = typeof(DamaBoard).GetMethod("ReturnToMenu", BindingFlags.Instance | BindingFlags.Public);

        if (button == null || returnMethod == null)
        {
            Record("TC-FLOW-06", "REQ-17", "A match can be left for the title screen without closing the app", false,
                $"PausePanel/MenuButton present={button != null}, DamaBoard.ReturnToMenu present={returnMethod != null}");
            board.ResumeGame();
            return;
        }

        returnMethod.Invoke(board, null);

        bool pass = Active("StartPanel") && !Active("PausePanel") && !Active("HUDPanel") &&
                    Mathf.Approximately(Time.timeScale, 1f);

        Record("TC-FLOW-06", "REQ-17", "A match can be left for the title screen without closing the app", pass,
            $"StartPanel={Active("StartPanel")} PausePanel={Active("PausePanel")} HUDPanel={Active("HUDPanel")} timeScale={Time.timeScale:0.00}");
    }

    // =====================================================================
    // The three rules of dama
    // =====================================================================

    private static void CaseRule01_MandatoryCapture()
    {
        FreshBoard();
        SetCell(1, 2, 1);
        SetCell(2, 3, 2);

        List<Vector2Int> moves = Moves("GetSelectableMoves", 1, 2);
        bool onlyJump = moves.Count == 1 && moves[0] == new Vector2Int(3, 4);

        Record("TC-RULE-01", "REQ-04", "A mandatory capture removes the quiet move from the offered list", onlyJump,
            $"offered=[{Describe(moves)}] expected only [(3,4)]");
    }

    private static void CaseRule02_CaptureRemovesPiece()
    {
        FreshBoard();
        SetCell(1, 2, 1);
        SetCell(2, 3, 2);

        Call("ExecuteMove", new Vector2Int(1, 2), new Vector2Int(3, 4));

        bool pass = CellAt(2, 3) == 0 && CellAt(3, 4) == 1;

        Record("TC-RULE-02", "REQ-04", "A jump lands beyond the enemy and removes it", pass,
            $"landing={(CellAt(3, 4) == 1 ? "RED" : "empty")} jumpedCell={(CellAt(2, 3) == 0 ? "empty" : "still occupied")}");
    }

    private static void CaseRule03_ChainContinues()
    {
        FreshBoard();
        SetCell(1, 2, 1);
        SetCell(2, 3, 2);
        SetCell(4, 5, 2);

        Call("ExecuteMove", new Vector2Int(1, 2), new Vector2Int(3, 4));

        Vector2Int chain = ChainPiece();
        List<Vector2Int> next = Moves("GetSelectableMoves", 3, 4);
        bool keepsTurn = CurrentPlayer() == 1;
        bool hasContinuation = next.Contains(new Vector2Int(5, 6));

        Record("TC-RULE-03", "REQ-05", "A capture that lands on another jump keeps the turn and the chain", keepsTurn && hasContinuation && chain == new Vector2Int(3, 4),
            $"chain=({chain.x},{chain.y}) turn=RED:{keepsTurn} continuationOffered={hasContinuation}");
    }

    private static void CaseRule04_RedPromotes()
    {
        FreshBoard();
        SetCell(3, 6, 1);
        SetCell(7, 7, 2);          // a piece for BLUE, so the match does not end on the next turn

        Call("ExecuteMove", new Vector2Int(3, 6), new Vector2Int(2, 7));

        bool pass = IsKing(2, 7);

        Record("TC-RULE-04", "REQ-06", "RED is crowned on its far row, y == 7", pass,
            $"landing=(2,7) isKing={pass}");
    }

    private static void CaseRule05_BluePromotes()
    {
        FreshBoard();
        SetCurrentPlayer(2);
        SetCell(3, 1, 2);
        SetCell(6, 6, 1);          // a RED piece that still has a move, so the turn does not end the match

        Call("ExecuteMove", new Vector2Int(3, 1), new Vector2Int(4, 0));

        bool pass = IsKing(4, 0);

        Record("TC-RULE-05", "REQ-06", "BLUE is crowned on its far row, y == 0 (not y == 7)", pass,
            $"landing=(4,0) isKing={pass}");
    }

    private static void CaseRule06_FlyingDama()
    {
        FreshBoard();
        SetCell(0, 0, 1);
        SetKing(0, 0, true);
        SetCell(7, 7, 2);

        List<Vector2Int> moves = Moves("GetSimpleMoves", 0, 0);
        bool pass = moves.Count == 6 && moves.Contains(new Vector2Int(6, 6)) && !moves.Contains(new Vector2Int(7, 7));

        Record("TC-RULE-06", "REQ-06", "A Dama flies any distance along a diagonal and stops before a piece", pass,
            $"offered=[{Describe(moves)}] expected 6 squares ending at (6,6)");
    }

    private static void CaseRule07_WinByElimination()
    {
        // The side TO MOVE is the side that gets checked: BLUE to move with no pieces is a RED win.
        board.RestartGame();
        FreshBoard();
        SetCell(7, 7, 1);
        SetCurrentPlayer(2);

        Call("CheckForWin");

        Text win = FindText("WinPanel/WinText");
        string winString = win != null ? win.text : "MISSING";
        bool declared = Active("WinPanel") && winString.Contains("RED WIN");

        string summary = SummaryText();
        bool reason = summary.Contains("no pieces");

        Record("TC-RULE-07", "REQ-07", "A side with no pieces left loses, and the winner is declared", declared,
            $"WinPanel={Active("WinPanel")} WinText='{winString}'");

        Record("TC-RULE-08", "REQ-09", "The end screen says the match was won by removing every piece", reason,
            summary.Length == 0 ? "WinPanel/SummaryText missing" : $"SummaryText='{summary}'");
    }

    private static void CaseRule08_WinByImmobilisation()
    {
        board.RestartGame();
        FreshBoard();
        SetCell(0, 0, 2);            // BLUE man on its own back row: no forward step, nothing to jump
        SetCell(7, 7, 1);
        SetCurrentPlayer(2);

        Call("CheckForWin");

        Text win = FindText("WinPanel/WinText");
        string winString = win != null ? win.text : "MISSING";
        bool declared = Active("WinPanel") && winString.Contains("RED WIN");

        string summary = SummaryText();
        int red = CountPieces(1);
        int blue = CountPieces(2);
        bool reason = summary.Contains("no legal move");
        bool counts = summary.Contains($"RED {red}-{blue}");

        Record("TC-RULE-09", "REQ-08", "A side that still has pieces but no legal move loses", declared,
            $"WinPanel={Active("WinPanel")} WinText='{winString}'");

        Record("TC-RULE-10", "REQ-09", "The end screen names both counts and says why the match ended", reason && counts,
            summary.Length == 0 ? "WinPanel/SummaryText missing" : $"SummaryText='{summary}' expected reason 'no legal move' and 'RED {red}-{blue}'");
    }

    private static void CaseRule09_EndScreenControls()
    {
        bool restart = Find("WinPanel/RestartButtonWin") != null;
        bool menu = Find("WinPanel/MenuButtonWin") != null;

        Record("TC-RULE-11", "REQ-09", "The end screen offers RESTART and a way back to the title screen", restart && menu,
            $"RestartButtonWin={restart} MenuButtonWin={menu}");
    }

    // =====================================================================
    // Player data
    // =====================================================================

    private static void CaseData01_NameAndSide()
    {
        PlayerProfile.UseDefaultDataDirectory();
        PlayerProfile.Load();
        PlayerProfile.ResetProfile();

        bool named = PlayerProfile.SetName("QA-CHARIBELLE");
        bool sided = PlayerProfile.SetSide(2);

        PlayerProfile.ResetCacheForTests();
        PlayerProfile.Load();

        bool pass = named && sided && PlayerProfile.PlayerName == "QA-CHARIBELLE" && PlayerProfile.PlayerSide == 2;

        Record("TC-DATA-01", "REQ-13", "Name and side survive a reload from disk", pass,
            $"name='{PlayerProfile.PlayerName}' side={PlayerProfile.PlayerSide} savePath={PlayerProfile.SavePath}");
    }

    private static void CaseData02_OneBankPerMatch()
    {
        PlayerProfile.ResetProfile();

        int id = PlayerProfile.LastMatchId + 1;
        PlayerProfile.RecordMatch(id, 1, 30.0f, 12, 4);
        int afterFirst = PlayerProfile.MatchesPlayed;

        bool repeatRefused = !PlayerProfile.RecordMatch(id, 1, 30.0f, 12, 4);
        int afterRepeat = PlayerProfile.MatchesPlayed;

        PlayerProfile.ResetCacheForTests();
        PlayerProfile.Load();
        int afterReload = PlayerProfile.MatchesPlayed;

        bool pass = afterFirst == 1 && afterRepeat == 1 && repeatRefused && afterReload == 1;

        Record("TC-DATA-02", "REQ-14", "A finished match is banked once and survives a reload", pass,
            $"afterFirst={afterFirst} afterRepeat={afterRepeat} repeatRefused={repeatRefused} afterReload={afterReload} error='{PlayerProfile.LastError}'");
    }

    private static void CaseData03_Reset()
    {
        PlayerProfile.RecordMatch(PlayerProfile.LastMatchId + 1, 2, 20.0f, 3, 12);
        bool reset = PlayerProfile.ResetProfile();

        PlayerProfile.ResetCacheForTests();
        PlayerProfile.Load();

        bool pass = reset && PlayerProfile.Wins == 0 && PlayerProfile.Losses == 0 &&
                    PlayerProfile.BestWins().Count == 0 && PlayerProfile.PlayerName == PlayerProfile.DefaultName;

        Record("TC-DATA-03", "REQ-15", "Reset clears the record and leaves a usable profile", pass,
            $"reset={reset} name='{PlayerProfile.PlayerName}' wins={PlayerProfile.Wins} losses={PlayerProfile.Losses} best={PlayerProfile.BestWins().Count}");
    }

    private static void CaseData04_CorruptSave()
    {
        // PlayerProfile.Load() is designed never to throw and never to report failure through its return
        // value: a bad file is quarantined and an empty profile starts. The observable signals of that
        // path are LastError, the quarantined copy on disk, and whether the profile still works
        // afterwards - so those are what this case asserts, not a return value the class never promised.
        PlayerProfile.WriteGarbageForTests();
        PlayerProfile.ResetCacheForTests();
        PlayerProfile.Load();

        string error = PlayerProfile.LastError;
        bool quarantined = File.Exists(PlayerProfile.RejectedPath);
        bool emptyDefaults = PlayerProfile.PlayerName == PlayerProfile.DefaultName && PlayerProfile.Wins == 0;

        bool recovered = PlayerProfile.SetName("QA-RECOVERED");
        PlayerProfile.ResetCacheForTests();
        PlayerProfile.Load();
        bool reloaded = PlayerProfile.PlayerName == "QA-RECOVERED";

        bool pass = error.Length > 0 && quarantined && emptyDefaults && recovered && reloaded;

        Record("TC-DATA-04", "REQ-16", "A corrupt save is set aside, reported, and the game recovers", pass,
            $"error='{error}' quarantined={quarantined} emptyDefaults={emptyDefaults} recovered={recovered} reloaded={reloaded}");
    }

    private static void CaseData05_LocalOnly()
    {
        // ResetProfile deletes the file rather than writing one, so the file is asserted after a save.
        PlayerProfile.SetName("QA-LOCAL");

        string savePath = PlayerProfile.SavePath;
        bool inside = savePath.StartsWith(Application.persistentDataPath, StringComparison.OrdinalIgnoreCase);
        bool exists = File.Exists(savePath);
        bool rejectedInside = PlayerProfile.RejectedPath.StartsWith(Application.persistentDataPath, StringComparison.OrdinalIgnoreCase);

        Record("TC-DATA-05", "REQ-22", "The player record is one local file inside the app sandbox", inside && exists && rejectedInside,
            $"path={savePath} insideSandbox={inside} exists={exists} rejectedCopyInsideSandbox={rejectedInside}");
    }

    // =====================================================================
    // Accessibility, measured in the running game
    // =====================================================================

    private static void CaseA11y01_TouchFloor()
    {
        GameObject canvas = GameObject.Find("GameCanvas");
        Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
        InputField[] fields = canvas.GetComponentsInChildren<InputField>(true);

        int tooSmall = 0;
        string worst = "";

        foreach (RectTransform rect in Controls(canvas))
        {
            if (rect.rect.width + 0.5f < TouchFloorCanvasPx || rect.rect.height + 0.5f < TouchFloorCanvasPx)
            {
                tooSmall++;
                worst += $"{rect.name}={rect.rect.width:0}x{rect.rect.height:0}; ";
            }
        }

        Record("TC-A11Y-01", "REQ-19", "Every button and input field is at least 96x96 canvas px",
            tooSmall == 0, $"{buttons.Length} buttons, {fields.Length} fields, {tooSmall} below floor {(tooSmall > 0 ? worst : "")}");
    }

    private static void CaseA11y02_NoOverlap()
    {
        string[] panels = { "StartPanel", "PausePanel", "WinPanel", "ProfilePanel", "HUDPanel" };
        int overlaps = 0;
        string found = "";

        foreach (string panel in panels)
        {
            Transform panelTransform = Find(panel);
            if (panelTransform == null) continue;

            List<RectTransform> rects = new List<RectTransform>();
            foreach (Button button in panelTransform.GetComponentsInChildren<Button>(true))
                rects.Add(button.transform as RectTransform);
            foreach (InputField field in panelTransform.GetComponentsInChildren<InputField>(true))
                rects.Add(field.transform as RectTransform);

            for (int i = 0; i < rects.Count; i++)
            {
                for (int j = i + 1; j < rects.Count; j++)
                {
                    if (rects[i] == null || rects[j] == null) continue;
                    if (Intersects(rects[i], rects[j]))
                    {
                        overlaps++;
                        found += $"{panel}:{rects[i].name}/{rects[j].name}; ";
                    }
                }
            }
        }

        Record("TC-A11Y-02", "REQ-19", "No two controls on the same panel overlap", overlaps == 0,
            overlaps == 0 ? "0 overlapping pairs" : $"{overlaps} pairs: {found}");
    }

    private static void CaseA11y03_TypeSizes()
    {
        GameObject canvas = GameObject.Find("GameCanvas");
        Text[] texts = canvas.GetComponentsInChildren<Text>(true);

        int captionsBad = 0;
        int labelsBad = 0;
        string detail = "";

        foreach (Text text in texts)
        {
            if (text == null) continue;

            bool isCaption = text.transform.parent != null && text.transform.parent.GetComponent<Button>() != null;
            float floor = isCaption ? CaptionFloorPx : LabelFloorPx;

            if (text.fontSize < floor - 0.01f)
            {
                if (isCaption) captionsBad++; else labelsBad++;
                detail += $"{text.name}={text.fontSize:0}px(need {floor:0}); ";
            }
        }

        Record("TC-A11Y-03", "REQ-20", "Captions are at least 20 px and labels at least 18 px",
            captionsBad == 0 && labelsBad == 0,
            $"{captionsBad} caption(s), {labelsBad} label(s) below floor {(detail.Length > 0 ? detail : "")}");
    }

    // =====================================================================
    // Boundary
    // =====================================================================

    private static void CaseBound01_RapidInput()
    {
        Time.timeScale = 1f;
        board.RestartGame();

        int before = TotalPieces();
        int matchIdBefore = PlayerProfile.LastMatchId;
        string failure = "";

        try
        {
            for (int i = 0; i < 100; i++)
            {
                if (i % 3 == 0) Call("HandleTap", new Vector2Int(1, 2));
                else if (i % 3 == 1) Call("HandleTap", new Vector2Int(1, 2));
                else Call("HandleTap", new Vector2Int(7, 0));
            }
        }
        catch (Exception exception)
        {
            failure = exception.GetType().Name + ": " + exception.Message;
        }

        int after = TotalPieces();
        List<Vector2Int> stillAnswers = Moves("GetSelectableMoves", 1, 2);

        bool pass = failure.Length == 0 && after == before && PlayerProfile.LastMatchId == matchIdBefore;

        Record("TC-BOUND-01", "REQ-21", "100 rapid taps do not crash, change the board or bank a match",
            pass, $"pieces {before}->{after} matchId {matchIdBefore}->{PlayerProfile.LastMatchId} throws='{failure}' stillAnswers={stillAnswers != null}");

        Call("ClearSelection");
    }

    private static void CaseBound02_RapidPauseResume()
    {
        Time.timeScale = 1f;
        board.StartGame();

        string failure = "";

        try
        {
            for (int i = 0; i < 50; i++)
            {
                board.PauseGame();
                board.ResumeGame();
            }
        }
        catch (Exception exception)
        {
            failure = exception.GetType().Name + ": " + exception.Message;
        }

        bool pass = failure.Length == 0 && Mathf.Approximately(Time.timeScale, 1f) && !Active("PausePanel");

        Record("TC-BOUND-02", "REQ-10", "50 pause/resume cycles leave the game running and unpaused", pass,
            $"timeScale={Time.timeScale:0.00} pausePanel={Active("PausePanel")} throws='{failure}'");
    }

    private static void CaseBlockedCompatibility()
    {
        // Recorded rather than skipped. The module asks for two approved configurations "when resources
        // permit". This lab had neither a package nor a handset: no APK was built (Editor-only evidence
        // this week) and `adb devices` reported no attached device. A resized Game view is not a second
        // configuration - it would prove the wrong thing - so the case is BLOCKED with the reason.
        results.Add(new CaseResult
        {
            Id = "TC-COMPAT-01",
            Reqs = "compatibility",
            Title = "Second device or configuration profile",
            Passed = false,
            Blocked = "BLOCKED: no APK was built this week (Editor-only evidence) and no Android handset was attached",
            Detail = "adb devices -> 'List of devices attached' with no serial; see Evidence/environment.md"
        });

        Debug.Log("[Week15] TC-COMPAT-01 BLOCKED - no APK and no handset in this lab.");
    }

    // =====================================================================
    // Recording and output
    // =====================================================================

    private static void Record(string id, string reqs, string title, bool passed, string detail)
    {
        results.Add(new CaseResult { Id = id, Reqs = reqs, Title = title, Passed = passed, Detail = detail });
        Debug.Log($"[Week15] {id} {(passed ? "PASS" : "FAIL")} ({reqs}) {title} :: {detail}");
    }

    private static void Finish()
    {
        Time.timeScale = 1f;

        if (Active("PausePanel")) board.ResumeGame();

        File.WriteAllText(Path.Combine(RunDir, "case_log.tsv"), BuildCaseLog(), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(RunDir, "summary.txt"), BuildSummary(), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(RunDir, "console.log"), string.Join("\n", console) + "\n", new UTF8Encoding(false));

        Debug.Log($"[Week15] {runLabel.ToUpper()} run finished: {SummaryLine()} Written to {RunDir}");

        Stop("finished");
    }

    private static void Abort(string reason)
    {
        Debug.LogError($"[Week15] {runLabel.ToUpper()} run aborted at step {index}: {reason}");

        try
        {
            Directory.CreateDirectory(RunDir);
            File.WriteAllText(Path.Combine(RunDir, "case_log.tsv"), BuildCaseLog(), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(RunDir, "summary.txt"),
                $"ABORTED: {reason}\n\n" + BuildSummary(), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(RunDir, "console.log"), string.Join("\n", console) + "\n", new UTF8Encoding(false));
        }
        catch (Exception exception)
        {
            Debug.LogError($"[Week15] Could not write the partial log: {exception.Message}");
        }

        Stop("aborted");
    }

    private static void Stop(string how)
    {
        running = false;
        EditorApplication.update -= Tick;

        if (logHooked)
        {
            Application.logMessageReceived -= OnLog;
            logHooked = false;
        }

        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();

        Debug.Log($"[Week15] Harness {how}.");
    }

    private static string SummaryLine()
    {
        int pass = 0, fail = 0, blocked = 0;
        foreach (CaseResult result in results)
        {
            if (result.Blocked != null) blocked++;
            else if (result.Passed) pass++;
            else fail++;
        }

        return $"{pass} passed, {fail} failed, {blocked} blocked, {errorCount} console error(s).";
    }

    private static string BuildCaseLog()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("id\treq\tstatus\ttitle\tmeasured");

        foreach (CaseResult result in results)
        {
            string status = result.Blocked != null ? "BLOCKED" : (result.Passed ? "PASS" : "FAIL");
            builder.AppendLine($"{result.Id}\t{result.Reqs}\t{status}\t{result.Title}\t{result.Detail}");
        }

        return builder.ToString();
    }

    private static string BuildSummary()
    {
        int pass = 0, fail = 0, blocked = 0;
        foreach (CaseResult result in results)
        {
            if (result.Blocked != null) blocked++;
            else if (result.Passed) pass++;
            else fail++;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("DAMA - " + runWeek.Replace('_', ' ').ToUpperInvariant() +
                           " TEST RUN (" + runLabel.ToUpper() + ")");
        builder.AppendLine("Game: Dama  |  Theme: Philippine Games and Sports  |  Student: Charibelle Guzman (BSIT-4)");
        builder.AppendLine("Harness: Assets/Scripts/Editor/Week15Checks.cs");
        builder.AppendLine($"Scene: DamaGame.unity   Unity: {Application.unityVersion}   Editor: {Application.platform}");
        builder.AppendLine($"Run at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC   Game view: {Screen.width}x{Screen.height}");
        builder.AppendLine($"Thresholds: touch {TouchFloorCanvasPx:0} canvas px, captions {CaptionFloorPx:0} px, labels {LabelFloorPx:0} px");
        builder.AppendLine($"TOTAL: {pass} passed, {fail} failed, {blocked} blocked, {errorCount} console error(s)");
        builder.AppendLine(new string('-', 100));

        foreach (CaseResult result in results)
        {
            string status = result.Blocked != null ? "BLOCKED" : (result.Passed ? "PASS" : "FAIL");
            builder.AppendLine($"{result.Id,-13} {status,-7} {result.Reqs,-11} {result.Title}");
            if (result.Blocked != null) builder.AppendLine($"                {result.Blocked}");
            else builder.AppendLine($"                {result.Detail}");
        }

        builder.AppendLine(new string('-', 100));
        builder.AppendLine("ACCEPTANCE ROLL-UP (criteria written before the run, see Evidence/acceptance_criteria.md)");
        builder.AppendLine($"  CC-1 data loss            : {Rollup("TC-DATA-02", "TC-DATA-03", "TC-DATA-04")}");
        builder.AppendLine($"  CC-2 core loop completes  : {Rollup("TC-FLOW-03", "TC-FLOW-04", "TC-RULE-01", "TC-RULE-02", "TC-RULE-07", "TC-RULE-09")}");
        builder.AppendLine($"  CC-3 no shared data       : {Rollup("TC-DATA-05")} (the game has no network path; see environment.md)");
        builder.AppendLine($"  CC-4 no repeated crashes  : {(errorCount == 0 ? "PASS" : "CHECK")} ({errorCount} console error(s) in console.log)");
        builder.AppendLine($"  AC-5 every control usable : {Rollup("TC-A11Y-01", "TC-A11Y-02")}");
        builder.AppendLine($"  AC-6 every label readable : {Rollup("TC-A11Y-03")} (contrast is measured by A11yAudit, not here)");

        return builder.ToString();
    }

    private static string Rollup(params string[] ids)
    {
        foreach (CaseResult result in results)
        {
            if (Array.IndexOf(ids, result.Id) < 0) continue;
            if (result.Blocked != null) return "BLOCKED";
        }

        bool allMet = true;
        int counted = 0;

        foreach (string id in ids)
        {
            foreach (CaseResult result in results)
            {
                if (result.Id != id) continue;
                counted++;
                if (!result.Passed) allMet = false;
            }
        }

        if (counted == 0) return "NOT RUN";
        return allMet ? "PASS" : "FAIL";
    }

    // =====================================================================
    // Scene helpers
    // =====================================================================

    private static void FreshBoard()
    {
        Time.timeScale = 1f;
        board.RestartGame();
        SetCurrentPlayer(1);

        Array grid = Grid();
        for (int x = 0; x < DamaBoard.Size; x++)
            for (int y = 0; y < DamaBoard.Size; y++)
                grid.SetValue(Enum.ToObject(grid.GetType().GetElementType(), 0), x, y);

        Array kings = Kings();
        for (int x = 0; x < DamaBoard.Size; x++)
            for (int y = 0; y < DamaBoard.Size; y++)
                kings.SetValue(false, x, y);

        SetChain(-1, -1);
    }

    private static Array Grid() => (Array)gridField.GetValue(board);
    private static Array Kings() => (Array)kingField.GetValue(board);

    private static void SetCell(int x, int y, int piece)
    {
        Array grid = Grid();
        grid.SetValue(Enum.ToObject(grid.GetType().GetElementType(), piece), x, y);
    }

    private static int CellAt(int x, int y) => Convert.ToInt32(Grid().GetValue(x, y));

    private static void SetKing(int x, int y, bool value) => Kings().SetValue(value, x, y);
    private static bool IsKing(int x, int y) => (bool)Kings().GetValue(x, y);

    private static void SetCurrentPlayer(int player) => playerField.SetValue(board, player);
    private static int CurrentPlayer() => (int)playerField.GetValue(board);

    private static Vector2Int ChainPiece() => (Vector2Int)chainField.GetValue(board);
    private static void SetChain(int x, int y) => chainField.SetValue(board, new Vector2Int(x, y));

    private static int TotalPieces() => CountPieces(1) + CountPieces(2);

    private static int CountPieces(int player)
    {
        MethodInfo info = typeof(DamaBoard).GetMethod("CountPieces", BindingFlags.Instance | BindingFlags.NonPublic);
        return info == null ? 0 : (int)info.Invoke(board, new object[] { player });
    }

    private static List<Vector2Int> Moves(string method, int x, int y)
    {
        MethodInfo info = typeof(DamaBoard).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
        if (info == null)
        {
            Debug.LogError($"[Week15] DamaBoard.{method} not found.");
            return new List<Vector2Int>();
        }

        object value = info.Invoke(board, new object[] { x, y });
        return value as List<Vector2Int> ?? new List<Vector2Int>();
    }

    private static void Call(string method, params object[] args)
    {
        MethodInfo info = typeof(DamaBoard).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (info == null)
        {
            Debug.LogError($"[Week15] DamaBoard.{method} not found.");
            return;
        }

        info.Invoke(board, args);
    }

    private static IEnumerable<RectTransform> Controls(GameObject canvas)
    {
        foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
            yield return button.transform as RectTransform;

        foreach (InputField field in canvas.GetComponentsInChildren<InputField>(true))
            yield return field.transform as RectTransform;
    }

    private static bool Intersects(RectTransform a, RectTransform b)
    {
        Vector3[] ca = new Vector3[4];
        Vector3[] cb = new Vector3[4];
        a.GetWorldCorners(ca);
        b.GetWorldCorners(cb);

        float minAx = Mathf.Min(ca[0].x, ca[2].x), maxAx = Mathf.Max(ca[0].x, ca[2].x);
        float minAy = Mathf.Min(ca[0].y, ca[2].y), maxAy = Mathf.Max(ca[0].y, ca[2].y);
        float minBx = Mathf.Min(cb[0].x, cb[2].x), maxBx = Mathf.Max(cb[0].x, cb[2].x);
        float minBy = Mathf.Min(cb[0].y, cb[2].y), maxBy = Mathf.Max(cb[0].y, cb[2].y);

        float overlapX = Mathf.Min(maxAx, maxBx) - Mathf.Max(minAx, minBx);
        float overlapY = Mathf.Min(maxAy, maxBy) - Mathf.Max(minAy, minBy);

        return overlapX > 0.5f && overlapY > 0.5f;
    }

    private static Transform Find(string path)
    {
        GameObject canvas = GameObject.Find("GameCanvas");
        return canvas != null ? canvas.transform.Find(path) : null;
    }

    private static bool Active(string panel) => Find(panel) != null && Find(panel).gameObject.activeSelf;

    private static Text FindText(string path)
    {
        Transform transform = Find(path);
        return transform != null ? transform.GetComponent<Text>() : null;
    }

    private static string SoundLabel()
    {
        Text text = FindText("PausePanel/SoundButton/Text");
        return text != null ? text.text : "MISSING";
    }

    private static string SummaryText()
    {
        Text text = FindText("WinPanel/SummaryText");
        return text != null ? text.text : "";
    }

    private static string Describe(List<Vector2Int> moves)
    {
        if (moves == null || moves.Count == 0) return "";
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < moves.Count; i++)
        {
            if (i > 0) builder.Append(", ");
            builder.Append($"({moves[i].x},{moves[i].y})");
        }

        return builder.ToString();
    }

    private static void OnLog(string condition, string stackTrace, LogType type)
    {
        console.Add($"{DateTime.UtcNow:HH:mm:ss} {type,-11} {condition}");
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errorCount++;
    }
}
