using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Week 13 evidence harness for PT5.
///
/// Menu 1 - Tools > Dama > Week 13 > 1. Run PT5 Data Checks
///   Runs fifteen data and security checks against a scratch folder in %TEMP%, so the
///   player's real save is never touched, and writes
///   docs/Charibelle Guzman/Week_13/Evidence/pt5_checks.log.
///
/// Menu 2 - Tools > Dama > Week 13 > 2. Capture Profile Screenshots
///   Enters play mode, drives the real profile panel and writes E1 to E4 into the same
///   Evidence folder, plus week13_shots.log.
/// </summary>
public static class Week13Checks
{
    private const string MenuRun = "Tools/Dama/Week 13/1. Run PT5 Data Checks";
    private const string MenuShots = "Tools/Dama/Week 13/2. Capture Profile Screenshots";

    private const string PendingKey = "Week13Shots.Pending";
    private const string FrameKey = "Week13Shots.Frame";
    private const string StageKey = "Week13Shots.Stage";
    private const string StageStartKey = "Week13Shots.StageStart";

    private static readonly List<string> log = new List<string>();
    private static int passed;
    private static int failed;

    private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
    public static string EvidenceDir => Path.Combine(ProjectRoot, "docs", "Charibelle Guzman", "Week_13", "Evidence");
    private static string ChecksLogPath => Path.Combine(EvidenceDir, "pt5_checks.log");
    private static string ShotsLogPath => Path.Combine(EvidenceDir, "week13_shots.log");
    private static string ScratchRoot => Path.Combine(Path.GetTempPath(), "Dama_week13_scratch");

    // =====================================================================
    // 1. Data and security checks
    // =====================================================================

    [MenuItem(MenuRun)]
    public static void RunDataChecks()
    {
        Directory.CreateDirectory(EvidenceDir);

        log.Clear();
        passed = 0;
        failed = 0;

        if (Directory.Exists(ScratchRoot)) Directory.Delete(ScratchRoot, true);
        Directory.CreateDirectory(ScratchRoot);

        log.Add("DAMA - WEEK 13 PT5 DATA AND SECURITY CHECKS");
        log.Add("Game: Dama  |  Theme: Philippine Games and Sports");
        log.Add("Student: Charibelle Guzman (BSIT-4)");
        log.Add("Harness: Assets/Scripts/Editor/Week13Checks.cs");
        log.Add($"Unity: {Application.unityVersion}   Run at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        log.Add($"Scratch folder: {ScratchRoot}  (the player's real save is never written to)");
        log.Add(new string('-', 100));

        Check("L1", "No save file at all is not an error", () =>
        {
            PlayerProfile.SetDataDirectory(Fresh("l1"));
            bool loaded = PlayerProfile.Load();
            string measured = $"loaded={loaded} name={PlayerProfile.PlayerName} matches={PlayerProfile.MatchesPlayed} error='{PlayerProfile.LastError}'";
            return Expect(loaded && PlayerProfile.PlayerName == "PLAYER" && PlayerProfile.MatchesPlayed == 0, measured);
        });

        Check("L2", "A valid name is trimmed and inner spaces collapsed", () =>
        {
            PlayerProfile.SetDataDirectory(Fresh("l2"));
            bool saved = PlayerProfile.SetName("   Dama    Player   ");
            string measured = $"saved={saved} name='{PlayerProfile.PlayerName}'";
            return Expect(saved && PlayerProfile.PlayerName == "Dama Player", measured);
        });

        Check("L3", "The profile survives a reload from disk and the file carries a signature", () =>
        {
            PlayerProfile.SetDataDirectory(Fresh("l3"));
            PlayerProfile.SetName("Charibelle G.");

            bool exists = File.Exists(PlayerProfile.SavePath);
            string raw = exists ? File.ReadAllText(PlayerProfile.SavePath) : "";
            bool signed = raw.Contains("\"sig\":\"");

            PlayerProfile.ResetCacheForTests();
            PlayerProfile.Load();

            string measured = $"name='{PlayerProfile.PlayerName}' file={exists} sig={signed}";
            return Expect(exists && signed && PlayerProfile.PlayerName == "Charibelle G.", measured);
        });

        Check("L4", "A blank name is refused and the stored name is kept", () =>
        {
            PlayerProfile.SetDataDirectory(Fresh("l4"));
            PlayerProfile.SetName("Charibelle G.");
            bool refused = !PlayerProfile.SetName("     ");
            string measured = $"refused={refused} name='{PlayerProfile.PlayerName}'";
            return Expect(refused && PlayerProfile.PlayerName == "Charibelle G.", measured);
        });

        Check("L5", "A name longer than 16 characters is capped", () =>
        {
            PlayerProfile.SetDataDirectory(Fresh("l5"));
            PlayerProfile.SetName("ABCDEFGHIJKLMNOPQRST");
            string name = PlayerProfile.PlayerName;
            string measured = $"length={name.Length} name='{name}'";
            return Expect(name.Length == PlayerProfile.MaxNameLength && name == "ABCDEFGHIJKLMNOP", measured);
        });

        Check("L6", "Control characters are stripped out of a name", () =>
        {
            PlayerProfile.SetDataDirectory(Fresh("l6"));
            PlayerProfile.SetName("Dama\u0001\u0002Player");
            string measured = $"name='{PlayerProfile.PlayerName}'";
            return Expect(PlayerProfile.PlayerName == "DamaPlayer", measured);
        });

        Check("L7", "A finished match is recorded and written to disk", () =>
        {
            PlayerProfile.SetDataDirectory(Fresh("l7"));
            bool banked = PlayerProfile.RecordMatch(1, 1, 47.3f, 6, 0);
            string measured = $"returned={banked} wins={PlayerProfile.Wins} recent={PlayerProfile.Recent.Count} file={File.Exists(PlayerProfile.SavePath)}";
            return Expect(banked && PlayerProfile.Wins == 1 && PlayerProfile.Recent.Count == 1, measured);
        });

        Check("L8", "The best-times board keeps the 5 fastest victories, quickest first", () =>
        {
            PlayerProfile.SetDataDirectory(Fresh("l8"));
            PlayerProfile.RecordMatch(1, 1, 61.2f, 6, 0);
            PlayerProfile.RecordMatch(2, 1, 42.5f, 6, 0);
            PlayerProfile.RecordMatch(3, 1, 55.0f, 6, 0);
            PlayerProfile.RecordMatch(4, 1, 38.9f, 6, 0);
            PlayerProfile.RecordMatch(5, 1, 70.0f, 6, 0);
            PlayerProfile.RecordMatch(6, 1, 50.0f, 6, 0);

            List<PlayerProfile.MatchRecord> best = PlayerProfile.BestWins();
            bool sorted = true;
            for (int i = 1; i < best.Count; i++)
                if (best[i - 1].seconds > best[i].seconds) sorted = false;

            string measured = $"count={best.Count} first={best[0].seconds:0.0} last={best[best.Count - 1].seconds:0.0} sorted={sorted}";
            return Expect(best.Count == PlayerProfile.MaxBestWins && best[0].seconds == 38.9f && sorted, measured);
        });

        Check("L9", "The same match id cannot be banked twice", () =>
        {
            PlayerProfile.SetDataDirectory(Fresh("l9"));
            PlayerProfile.RecordMatch(5, 1, 40f, 6, 0);
            int before = PlayerProfile.Wins;
            bool refused = !PlayerProfile.RecordMatch(5, 1, 40f, 6, 0);
            string measured = $"refused={refused} wins={PlayerProfile.Wins} (before={before})";
            return Expect(refused && PlayerProfile.Wins == before, measured);
        });

        Check("L10", "An unreadable save starts empty without a crash, and is set aside", () =>
        {
            PlayerProfile.SetDataDirectory(Fresh("l10"));
            PlayerProfile.WriteGarbageForTests();
            PlayerProfile.Load();

            string measured = $"name={PlayerProfile.PlayerName} matches={PlayerProfile.MatchesPlayed} rejected_kept={File.Exists(PlayerProfile.RejectedPath)}";
            return Expect(PlayerProfile.PlayerName == "PLAYER" && PlayerProfile.MatchesPlayed == 0 && File.Exists(PlayerProfile.RejectedPath), measured);
        });

        Check("L11", "A hand-edited save whose signature does not match is rejected", () =>
        {
            PlayerProfile.SetDataDirectory(Fresh("l11"));
            PlayerProfile.SetName("Charibelle G.");

            string raw = File.ReadAllText(PlayerProfile.SavePath);
            string tampered = raw.Replace("\"wins\":0", "\"wins\":99");
            if (tampered == raw) tampered = raw.Replace("\"wins\": 0", "\"wins\": 99");

            PlayerProfile.WriteRawForTests(tampered);
            PlayerProfile.Load();

            string measured = $"wins={PlayerProfile.Wins} name={PlayerProfile.PlayerName} error='{PlayerProfile.LastError}'";
            return Expect(PlayerProfile.Wins == 0 && PlayerProfile.PlayerName == "PLAYER" && PlayerProfile.LastError.Contains("signature"), measured);
        });

        Check("L12", "Impossible numbers in a correctly signed save are clamped, not trusted", () =>
        {
            PlayerProfile.SetDataDirectory(Fresh("l12"));

            PlayerProfile.Payload planted = new PlayerProfile.Payload
            {
                playerName = "Charibelle G.",
                playerSide = 1,
                wins = -5,
                losses = -3,
                draws = -1,
                currentStreak = -2,
                bestStreak = 9,
                lastMatchId = -7,
                recent = null
            };

            PlayerProfile.WriteSignedForTests(planted);
            PlayerProfile.Load();

            string measured = $"wins={PlayerProfile.Wins} losses={PlayerProfile.Losses} bestStreak={PlayerProfile.BestStreak} name='{PlayerProfile.PlayerName}'";
            return Expect(PlayerProfile.Wins == 0 && PlayerProfile.Losses == 0 && PlayerProfile.LastMatchId == 0, measured);
        });

        Check("L13", "Reset erases the record on disk and returns the default name", () =>
        {
            PlayerProfile.SetDataDirectory(Fresh("l13"));
            PlayerProfile.SetName("Charibelle G.");
            PlayerProfile.RecordMatch(1, 1, 44f, 6, 0);

            bool reset = PlayerProfile.ResetProfile();
            string measured = $"returned={reset} file={File.Exists(PlayerProfile.SavePath)} name={PlayerProfile.PlayerName} matches={PlayerProfile.MatchesPlayed}";
            return Expect(reset && !File.Exists(PlayerProfile.SavePath) && PlayerProfile.PlayerName == "PLAYER", measured);
        });

        Check("L14", "An unwritable save location reports an error instead of crashing", () =>
        {
            string root = Fresh("l14");
            string blocker = Path.Combine(root, "blocked");
            File.WriteAllText(blocker, "this is a file, not a folder");

            PlayerProfile.SetDataDirectory(Path.Combine(blocker, "profile"));
            bool saved = PlayerProfile.SetName("Charibelle G.");

            string measured = $"saved={saved} error='{PlayerProfile.LastError}'";
            return Expect(!saved && !string.IsNullOrEmpty(PlayerProfile.LastError), measured);
        });

        // L15 is a limit, not a pass, so it is printed rather than asserted.
        log.Add($"{Stamp()} L15 KNOWN LIMIT: the save is tamper-evident, not encrypted.");
        log.Add($"{Stamp()}    The HMAC key is a per-install salt stored in PlayerPrefs on the same device as the file.");
        log.Add($"{Stamp()}    A determined local attacker can extract it and re-sign an edited save. There is no server to check against.");
        log.Add($"{Stamp()}    Recorded as a limitation in PT5 section 5 and in README.md, not claimed as protection.");

        log.Add(new string('-', 100));
        log.Add($"TOTAL: {passed} passed, {failed} failed, 1 documented known limit (L15)");

        File.WriteAllText(ChecksLogPath, string.Join("\n", log) + "\n", new UTF8Encoding(false));
        Debug.Log($"[Week13Checks] {passed} passed, {failed} failed. Log written to {ChecksLogPath}");
    }

    private static void Check(string id, string what, Func<(bool ok, string measured)> body)
    {
        string measured;
        bool ok;

        try
        {
            (ok, measured) = body();
        }
        catch (Exception error)
        {
            ok = false;
            measured = $"threw {error.GetType().Name}: {error.Message}";
        }

        string state = ok ? "PASS" : "FAIL";
        if (ok) passed++; else failed++;

        string line = $"{Stamp()} {id} {(what + " " + new string('.', Math.Max(1, 78 - what.Length)))} {state} [{measured}]";
        log.Add(line);
        Debug.Log($"[Week13Checks] {id} {state} [{measured}]");
    }

    private static (bool, string) Expect(bool condition, string measured)
    {
        return (condition, measured);
    }

    private static string Stamp()
    {
        return DateTime.UtcNow.ToString("HH:mm:ss.fff");
    }

    /// <summary>A clean scratch folder per check, so one check can never leak into the next.</summary>
    private static string Fresh(string tag)
    {
        string dir = Path.Combine(ScratchRoot, tag);
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
        Directory.CreateDirectory(dir);
        return dir;
    }

    // =====================================================================
    // 2. Screenshot capture (play mode)
    // =====================================================================

    [MenuItem(MenuShots)]
    public static void BeginShots()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[Week13Shots] Stop play mode before starting the capture run.");
            return;
        }

        Directory.CreateDirectory(EvidenceDir);

        // The checks above may have left the store pointed at a scratch folder: point it
        // back at the real save and start from a fresh install so E1 is genuinely empty.
        PlayerProfile.UseDefaultDataDirectory();
        PlayerProfile.ResetProfile();
        File.WriteAllText(ShotsLogPath,
            "WEEK 13 - PROFILE SCREENSHOT RUN\n" +
            $"Unity {Application.unityVersion}  |  {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n" +
            "Captured in play mode from GameCanvas/ProfilePanel driving the real save file.\n" +
            new string('-', 90) + "\n",
            new UTF8Encoding(false));

        SessionState.SetInt(FrameKey, 0);
        SessionState.SetInt(StageKey, 0);
        SessionState.SetInt(StageStartKey, 0);
        SessionState.SetBool(PendingKey, true);

        EditorApplication.isPlaying = true;
        Debug.Log($"[Week13Shots] Entering play mode. Screenshots will land in {EvidenceDir}");
    }

    /// <summary>
    /// Re-subscribed after every domain reload, because entering play mode reloads the
    /// domain and would otherwise drop a plain static subscription.
    /// </summary>
    [InitializeOnLoadMethod]
    private static void Hook()
    {
        EditorApplication.update -= ShotTick;
        EditorApplication.update += ShotTick;
    }

    private static void ShotTick()
    {
        if (!SessionState.GetBool(PendingKey, false)) return;
        if (!EditorApplication.isPlaying) return;

        int frame = SessionState.GetInt(FrameKey, 0) + 1;
        SessionState.SetInt(FrameKey, frame);

        int stageStart = SessionState.GetInt(StageStartKey, 0);
        int elapsed = frame - stageStart;
        int stage = SessionState.GetInt(StageKey, 0);

        // Every stage waits before acting, and every capture stage waits for its file to
        // exist, so a deferred screen readback can never pick up the next state.
        switch (stage)
        {
            case 0 when elapsed >= 40:
                OpenPanel();
                AdvanceStage(frame);
                break;

            case 1 when elapsed >= 40:
                RequestCapture("E1_profile_empty.png");
                AdvanceStage(frame);
                break;

            case 2 when elapsed >= 40 && CaptureLanded("E1_profile_empty.png"):
                SeedFirstMatches();
                AdvanceStage(frame);
                break;

            case 3 when elapsed >= 40:
                RequestCapture("E2_profile_stats.png");
                AdvanceStage(frame);
                break;

            case 4 when elapsed >= 40 && CaptureLanded("E2_profile_stats.png"):
                SeedMoreWins();
                AdvanceStage(frame);
                break;

            case 5 when elapsed >= 40:
                RequestCapture("E3_leaderboard.png");
                AdvanceStage(frame);
                break;

            case 6 when elapsed >= 40 && CaptureLanded("E3_leaderboard.png"):
                ResetThroughPanel();
                AdvanceStage(frame);
                break;

            case 7 when elapsed >= 40:
                RequestCapture("E4_after_reset.png");
                AdvanceStage(frame);
                break;

            case 8 when elapsed >= 40 && CaptureLanded("E4_after_reset.png"):
                BankOnceTest();
                AdvanceStage(frame);
                break;

            case 9 when elapsed >= 40:
                Finish();
                break;
        }
    }

    private static void AdvanceStage(int frame)
    {
        SessionState.SetInt(StageKey, SessionState.GetInt(StageKey, 0) + 1);
        SessionState.SetInt(StageStartKey, frame);
    }

    private static void OpenPanel()
    {
        PlayerProfile.UseDefaultDataDirectory();

        DamaBoard board = UnityEngine.Object.FindAnyObjectByType<DamaBoard>();
        if (board == null)
        {
            ShotLog("FAILED: no DamaBoard in the open scene.");
            Finish();
            return;
        }

        board.ShowProfile();
        ShotLog($"Opened the profile panel. name='{PlayerProfile.PlayerName}' matches={PlayerProfile.MatchesPlayed}");
    }

    private static void SeedFirstMatches()
    {
        PlayerProfile.Load();
        PlayerProfile.SetName("Charibelle G.");
        PlayerProfile.RecordMatch(1, 1, 47.3f, 6, 0);   // RED wins
        PlayerProfile.RecordMatch(2, 1, 55.0f, 5, 0);   // RED wins, best streak 2
        PlayerProfile.RecordMatch(3, 2, 62.0f, 1, 4);   // BLUE wins

        RefreshPanel();
        ShotLog($"Seeded three matches: matches={PlayerProfile.MatchesPlayed} wins={PlayerProfile.Wins} losses={PlayerProfile.Losses} streak={PlayerProfile.CurrentStreak} best={PlayerProfile.BestStreak}");
    }

    private static void SeedMoreWins()
    {
        PlayerProfile.RecordMatch(4, 1, 61.2f, 6, 0);
        PlayerProfile.RecordMatch(5, 1, 42.5f, 6, 0);
        PlayerProfile.RecordMatch(6, 1, 38.9f, 6, 0);
        PlayerProfile.RecordMatch(7, 1, 50.0f, 6, 0);

        RefreshPanel();

        List<PlayerProfile.MatchRecord> best = PlayerProfile.BestWins();
        string entries = "";
        foreach (PlayerProfile.MatchRecord record in best) entries += $" {record.seconds:0.0}s(m{record.matchId})";
        ShotLog($"Seeded four more victories. Board count={best.Count}:{entries}");
    }

    private static void ResetThroughPanel()
    {
        ProfilePanel panel = FindPanel();
        if (panel == null)
        {
            ShotLog("FAILED: ProfilePanel was not found.");
            return;
        }

        // Two presses in one frame: arms, then erases. The same path a player uses.
        panel.ResetPressed();
        panel.ResetPressed();

        ShotLog($"Two-press reset executed. name={PlayerProfile.PlayerName} matches={PlayerProfile.MatchesPlayed} file={File.Exists(PlayerProfile.SavePath)}");
    }

    private static void BankOnceTest()
    {
        DamaBoard board = UnityEngine.Object.FindAnyObjectByType<DamaBoard>();
        if (board == null) return;

        int before = PlayerProfile.Wins;

        // Two calls in the same frame: the second must be refused by recordedMatchId
        // and by the store's match-id gate.
        board.BankMatchForTests(1);
        board.BankMatchForTests(1);

        int delta = PlayerProfile.Wins - before;
        ShotLog($"Bank-once test after reset: wins before={before} after={PlayerProfile.Wins} delta={delta} lastMatchId={PlayerProfile.LastMatchId}");
    }

    private static void RefreshPanel()
    {
        ProfilePanel panel = FindPanel();
        if (panel == null) return;

        panel.SyncNameField();
        panel.Refresh();
    }

    private static ProfilePanel FindPanel()
    {
        return UnityEngine.Object.FindAnyObjectByType<ProfilePanel>(FindObjectsInactive.Include);
    }

    private static string CapturePath(string fileName)
    {
        return Path.Combine(EvidenceDir, fileName);
    }

    /// <summary>Deletes any stale file first, so CaptureLanded can only see the new one.</summary>
    private static void RequestCapture(string fileName)
    {
        string path = CapturePath(fileName);
        if (File.Exists(path)) File.Delete(path);

        ScreenCapture.CaptureScreenshot(path);
        ShotLog($"Requested {fileName}  (name='{PlayerProfile.PlayerName}' matches={PlayerProfile.MatchesPlayed})");
    }

    private static bool CaptureLanded(string fileName)
    {
        string path = CapturePath(fileName);
        if (!File.Exists(path)) return false;

        var info = new FileInfo(path);
        if (info.Length <= 0) return false;

        ShotLog($"Wrote {fileName} ({info.Length} bytes)");
        return true;
    }

    private static void ShotLog(string message)
    {
        File.AppendAllText(ShotsLogPath, $"{DateTime.UtcNow:HH:mm:ss} {message}\n", new UTF8Encoding(false));
        Debug.Log($"[Week13Shots] {message}");
    }

    private static void Finish()
    {
        ShotLog("Capture run finished. Play mode stopping.");
        SessionState.SetBool(PendingKey, false);
        EditorApplication.isPlaying = false;
    }
}
