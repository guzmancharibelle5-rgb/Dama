using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

/// <summary>
/// Week 16 profiling harness for Dama.
///
/// Menu: Tools > Dama > Week 16 > 1. Baseline Profile
///       Tools > Dama > Week 16 > 2. After Optimization 1
///       Tools > Dama > Week 16 > 3. After Optimization 2
///
/// ONE FIXED SCENARIO, RUN THREE TIMES, so the numbers are comparable: the default data
/// directory, a profile reset, START, a 3 s warm-up that is thrown away, an allocation fixture,
/// then 32 turns - one every ~1.85 s - each chosen by the game's own GetSelectableMoves and
/// executed through the game's own ExecuteMove, three scripted RESTARTs, and a 3 s dwell on the
/// end screen. The baseline RECORDS the turn list to moves.txt; the later runs REPLAY it, so the
/// input sequence is identical and a change in move generation shows up as a different move hash
/// or a replay warning instead of being averaged away.
///
/// WHY THERE IS A FIXTURE (measured, not assumed):
///   - System.GC.GetAllocatedBytesForCurrentThread() returns a constant 0 on this runtime; it
///     cannot see even a deliberate 64 KB allocation.
///   - System.GC.GetTotalMemory(false) and Profiler.GetMonoUsedSizeLong() move in 4 KB blocks and
///     read 0 across a run of 200-byte allocations.
///   - AppDomain.MonitoringIsEnabled throws NotImplementedException.
///   - The Editor-wide "GC Allocated In Frame" counter carries ~210 KB per frame of the Editor's
///     own work, so a ~1 KB turn is under the noise floor.
///   So per-call allocation is measured by AMPLITUDE SCALING: the same call is run n and 2n times
///   in separate frames and the per-frame GC.Alloc figures are fitted with a straight line. The
///   slope is the bytes per call, and the intercept absorbs the Editor's per-frame cost exactly.
///   That is reported with its standard error and R-squared, or as NOT MEASURABLE.
///
/// FRAME TIME is a Stopwatch delta between frame boundaries and never depends on a counter, and
/// the run is executed with vSync off so that a hitch cannot hide inside a 16.7 ms vsync lock.
///
/// Sampling rules: nothing in the per-frame path formats a string or boxes a value; values land
/// in preallocated arrays and are formatted once, at the end. Frames are tagged, and the tag says
/// what the frame was: warm-up and probe and fixture frames are excluded from every turn metric.
/// </summary>
public static class Week16Profile
{
    private const string MenuBaseline = "Tools/Dama/Week 16/1. Baseline Profile";
    private const string MenuAfterOne = "Tools/Dama/Week 16/2. After Optimization 1";
    private const string MenuAfterTwo = "Tools/Dama/Week 16/3. After Optimization 2";

    private const double WarmupSeconds = 3.0;
    private const double MoveIntervalSeconds = 1.85;
    private const double ProbeGapSeconds = 0.10;
    private const double RestartSettleSeconds = 1.0;
    private const double EndDwellSeconds = 3.0;
    private const double HardCapSeconds = 240.0;
    private const int TargetTurns = 32;
    private const int RestartCycles = 3;
    private const int RestartWatchFrames = 3;
    private const int Capacity = 120000;

    private const byte EvWarmup = 1;
    private const byte EvGame = 2;
    private const byte EvProbe = 3;
    private const byte EvMove = 4;
    private const byte EvRestart = 5;
    private const byte EvEnd = 6;
    private const byte EvFixture = 7;

    private const int FixtureKinds = 3;
    private const int FixtureRepeats = 5;

    private static readonly string ProjectRoot = Directory.GetParent(Application.dataPath).FullName;
    private static readonly string EvidenceRoot =
        Path.Combine(ProjectRoot, "docs", "Charibelle Guzman", "Week_16", "Evidence");

    // ---- run state ----
    private static bool running;
    private static string runLabel = "baseline";
    private static string runFolder = "profile_baseline";
    private static bool recordMoves = true;
    private static int stage;
    private static double nextActionAt;
    private static DamaBoard board;
    private static int turnsDone;
    private static int restarts;
    private static int matchEndRestarts;
    private static int restartCyclesLeft;
    private static int restartWatch;
    private static bool hasPendingMove;
    private static int pendingFromX, pendingFromY, pendingToX, pendingToY;
    private static int pendingRed, pendingBlue;
    private static byte pendingTag = EvGame;
    private static int savedVSync;
    private static int savedTargetFrameRate;

    // ---- reflection surface of DamaBoard ----
    private static MethodInfo miSelectable;
    private static MethodInfo miExecute;
    private static FieldInfo fiGrid;
    private static FieldInfo fiCurrentPlayer;
    private static Func<int, int, List<Vector2Int>> selectableFn;
    private static Action<Vector2Int, Vector2Int> executeFn;
    private static bool allocatingCallsAreStronglyTyped;

    // ---- allocation fixture ----
    private static readonly List<int> fixtureN = new List<int>(64);
    private static readonly List<long> fixtureGc = new List<long>(64);
    private static readonly List<double> fixtureMs = new List<double>(64);
    private static readonly List<int> fixtureKind = new List<int>(64);
    private static readonly List<Vector2Int> fixturePieces = new List<Vector2Int>(24);
    private static int fixturePlayer;
    private static int fixtureStep;
    private static int fixtureTotalSteps;
    private static bool fixtureWorkloadPending;
    private static int pendingFixtureN;
    private static int pendingFixtureKind;

    // ---- recording buffers (no per-frame allocation) ----
    private static readonly double[] rowMs = new double[Capacity];
    private static readonly long[] rowGc = new long[Capacity];
    private static readonly long[] rowMem = new long[Capacity];
    private static readonly int[] rowDraws = new int[Capacity];
    private static readonly int[] rowBatches = new int[Capacity];
    private static readonly int[] rowSetPass = new int[Capacity];
    private static readonly int[] rowTris = new int[Capacity];
    private static readonly double[] rowSeconds = new double[Capacity];
    private static readonly byte[] rowTag = new byte[Capacity];
    private static int rowCount;

    private static long lastTimestamp;
    private static int lastFrame = -1;
    private static long startTimestamp;
    private static double warmupEndSeconds;

    // ---- profiler counters ----
    private static ProfilerRecorder recGc;
    private static ProfilerRecorder recDraws;
    private static ProfilerRecorder recBatches;
    private static ProfilerRecorder recSetPass;
    private static ProfilerRecorder recTris;
    private static string gcSource = "NOT MEASURABLE";
    private static string byteProbeNote = "not evaluated";
    private static bool profilerWasEnabled;

    // ---- turn list, so the input sequence is identical across runs ----
    private static readonly List<int> fromX = new List<int>();
    private static readonly List<int> fromY = new List<int>();
    private static readonly List<int> toX = new List<int>();
    private static readonly List<int> toY = new List<int>();
    private static int replayIndex;
    private static uint moveHash = 2166136261u;
    private static int replayMisses;

    private static double ElapsedSeconds =>
        (double)(System.Diagnostics.Stopwatch.GetTimestamp() - startTimestamp) /
        System.Diagnostics.Stopwatch.Frequency;

    // =====================================================================
    // Menus
    // =====================================================================

    [MenuItem(MenuBaseline, false, 10)]
    public static void RunBaseline() => Begin("baseline", "profile_baseline", true);

    [MenuItem(MenuAfterOne, false, 11)]
    public static void RunAfterOptimizationOne() => Begin("after-optimization-1", "profile_after_alloc", false);

    [MenuItem(MenuAfterTwo, false, 12)]
    public static void RunAfterOptimizationTwo() => Begin("after-optimization-2", "profile_after_pool", false);

    // =====================================================================
    // Run control
    // =====================================================================

    private static void Begin(string label, string folder, bool record)
    {
        if (running)
        {
            Debug.LogWarning("[Week16] A profile run is already in progress; menu item ignored.");
            return;
        }

        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[Week16] Stop play mode before starting a profile run - the harness enters play mode itself.");
            return;
        }

        ResetState();
        runLabel = label;
        runFolder = folder;
        recordMoves = record;

        Directory.CreateDirectory(Path.Combine(EvidenceRoot, runFolder));

        if (!record)
        {
            string movesPath = Path.Combine(EvidenceRoot, "moves.txt");
            if (File.Exists(movesPath)) LoadMoves(movesPath);
            else Debug.LogWarning("[Week16] moves.txt is missing; this run chooses its own moves and is NOT strictly comparable.");
        }

        running = true;
        EditorApplication.update += Tick;
        Debug.Log($"[Week16] {runLabel}: entering play mode. The scenario runs about 75 s; do not edit any script until it finishes.");
        EditorApplication.EnterPlaymode();
    }

    private static void ResetState()
    {
        stage = 0;
        turnsDone = 0;
        restarts = 0;
        matchEndRestarts = 0;
        restartCyclesLeft = 0;
        restartWatch = 0;
        hasPendingMove = false;
        pendingTag = EvGame;
        rowCount = 0;
        lastFrame = -1;
        lastTimestamp = 0;
        replayIndex = 0;
        replayMisses = 0;
        moveHash = 2166136261u;
        board = null;
        fromX.Clear(); fromY.Clear(); toX.Clear(); toY.Clear();
        fixtureN.Clear(); fixtureGc.Clear(); fixtureMs.Clear(); fixtureKind.Clear(); fixturePieces.Clear();
        fixtureStep = 0; fixtureTotalSteps = 0; fixtureWorkloadPending = false;
        pendingFixtureN = 0; pendingFixtureKind = 0;
        selectableFn = null; executeFn = null;
        allocatingCallsAreStronglyTyped = false;
        byteProbeNote = "not evaluated";

        DisposeRecorders();
        gcSource = "NOT MEASURABLE";
    }

    private static void Tick()
    {
        if (!running)
        {
            EditorApplication.update -= Tick;
            return;
        }

        if (!EditorApplication.isPlaying)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Debug.LogError("[Week16] Play mode ended before the run finished; no files were written.");
            running = false;
            EditorApplication.update -= Tick;
            return;
        }

        if (board == null)
        {
            board = UnityEngine.Object.FindAnyObjectByType<DamaBoard>();
            if (board == null) return;
            if (!PrepareScenario()) return;
        }

        SampleFrame();

        double now = ElapsedSeconds;

        if (now > HardCapSeconds)
        {
            Finish($"hard cap of {HardCapSeconds:0} s reached at stage {stage}");
            return;
        }

        switch (stage)
        {
            case 0:
                if (now >= warmupEndSeconds)
                {
                    stage = 4;
                    fixtureStep = 0;
                    fixtureWorkloadPending = false;
                    Debug.Log($"[Week16] Warm-up discarded; measuring the allocation fixture next ({fixtureTotalSteps} samples).");
                }
                break;

            case 4:
                if (fixtureWorkloadPending)
                {
                    RecordFixtureSample();
                    fixtureWorkloadPending = false;
                    break;
                }

                if (fixtureStep >= fixtureTotalSteps)
                {
                    stage = 1;
                    nextActionAt = now + MoveIntervalSeconds;
                    Debug.Log("[Week16] Fixture complete; measurement window open.");
                    break;
                }

                RunFixtureWorkload();
                fixtureWorkloadPending = true;
                break;

            case 1:
                if (now < nextActionAt) break;
                AdvanceTurn(now);
                break;

            case 3:
                if (now < nextActionAt) break;
                if (restartCyclesLeft > 0)
                {
                    RestartNow(false);
                    restartCyclesLeft--;
                    nextActionAt = now + RestartSettleSeconds;
                    break;
                }

                stage = 2;
                nextActionAt = now + EndDwellSeconds;
                Debug.Log($"[Week16] Restart cycles complete ({restarts} restart(s) measured); dwelling {EndDwellSeconds:0} s on the end screen.");
                break;

            case 2:
                if (now >= nextActionAt) Finish(null);
                break;
        }
    }

    private static bool PrepareScenario()
    {
        miSelectable = typeof(DamaBoard).GetMethod("GetSelectableMoves", BindingFlags.Instance | BindingFlags.NonPublic);
        miExecute = typeof(DamaBoard).GetMethod("ExecuteMove", BindingFlags.Instance | BindingFlags.NonPublic);
        fiGrid = typeof(DamaBoard).GetField("grid", BindingFlags.Instance | BindingFlags.NonPublic);
        fiCurrentPlayer = typeof(DamaBoard).GetField("currentPlayer", BindingFlags.Instance | BindingFlags.NonPublic);

        if (miSelectable == null || miExecute == null || fiGrid == null || fiCurrentPlayer == null)
        {
            Debug.LogError("[Week16] DamaBoard's private surface changed (GetSelectableMoves / ExecuteMove / grid / currentPlayer). " +
                           "The harness cannot drive the game; no files were written.");
            running = false;
            EditorApplication.update -= Tick;
            EditorApplication.ExitPlaymode();
            return false;
        }

        // Strongly typed delegates instead of MethodInfo.Invoke: an Invoke of a bool-returning
        // method boxes its result, and that ~24 B per call would be inside every measurement.
        try
        {
            selectableFn = (Func<int, int, List<Vector2Int>>)Delegate.CreateDelegate(
                typeof(Func<int, int, List<Vector2Int>>), board, miSelectable);
            executeFn = (Action<Vector2Int, Vector2Int>)Delegate.CreateDelegate(
                typeof(Action<Vector2Int, Vector2Int>), board, miExecute);
            allocatingCallsAreStronglyTyped = selectableFn != null && executeFn != null;
        }
        catch (Exception error)
        {
            Debug.LogWarning($"[Week16] Strongly typed delegates unavailable ({error.GetType().Name}); falling back to Invoke, " +
                             "which adds the same boxing to every run.");
        }

        EvaluateByteProbes();
        StartRecorders();

        profilerWasEnabled = Profiler.enabled;
        Profiler.enabled = true;

        savedVSync = QualitySettings.vSyncCount;
        savedTargetFrameRate = Application.targetFrameRate;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;

        PlayerProfile.UseDefaultDataDirectory();
        PlayerProfile.ResetProfile();

        startTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
        lastTimestamp = startTimestamp;
        warmupEndSeconds = WarmupSeconds;

        board.StartGame();
        pendingTag = EvWarmup;

        PrepareFixture();

        Debug.Log($"[Week16] {runLabel}: scenario started; vSync {savedVSync}->0, targetFrameRate {savedTargetFrameRate}->-1. " +
                  $"Save file under test: {PlayerProfile.SavePath}");
        return true;
    }

    /// <summary>
    /// Records, once, whether this runtime offers any byte-level allocation counter at all. It is
    /// checked rather than assumed, because a counter that cannot see a 64 KB allocation would
    /// otherwise be believed when it reports 0 B forever.
    /// </summary>
    private static void EvaluateByteProbes()
    {
        try
        {
            long before = System.GC.GetAllocatedBytesForCurrentThread();
            byte[] ballast = new byte[64 * 1024];
            ballast[0] = 1;
            ballast[ballast.Length - 1] = 1;
            long after = System.GC.GetAllocatedBytesForCurrentThread();

            if (after - before >= 32 * 1024)
            {
                byteProbeNote = "GC.GetAllocatedBytesForCurrentThread saw the 64 KB calibration allocation";
                return;
            }

            byteProbeNote = "GC.GetAllocatedBytesForCurrentThread read 0 B across a deliberate 64 KB allocation " +
                            "(a stub on this runtime); GC.GetTotalMemory(false) and Profiler.GetMonoUsedSizeLong() " +
                            "move in 4 KB blocks and read 0 B across a run of 200 B allocations; " +
                            "AppDomain.MonitoringIsEnabled throws NotImplementedException. " +
                            "Byte-level per-call allocation is therefore measured by amplitude scaling below.";
        }
        catch (Exception error)
        {
            byteProbeNote = $"the allocation counter threw {error.GetType().Name}; byte-level allocation is measured by amplitude scaling";
        }

        Debug.LogWarning("[Week16] " + byteProbeNote);
    }

    private static void StartRecorders()
    {
        recGc = StartFirst("GC Allocated In Frame", ProfilerCategory.Memory, ProfilerCategory.Scripts, ProfilerCategory.Internal);
        gcSource = recGc.Valid
            ? "ProfilerRecorder GC Allocated In Frame (Editor-wide: includes the Editor's own per-frame work)"
            : "NOT MEASURABLE";

        recDraws = StartFirst("Draw Calls Count", ProfilerCategory.Render);
        recBatches = StartFirst("Batches Count", ProfilerCategory.Render);
        recSetPass = StartFirst("Set Pass Calls Count", ProfilerCategory.Render);
        recTris = StartFirst("Triangles Count", ProfilerCategory.Render);

        Debug.Log($"[Week16] Counters: gc={(recGc.Valid ? "on" : "NOT MEASURABLE")} draws={(recDraws.Valid ? "on" : "NOT MEASURABLE")} " +
                  $"batches={(recBatches.Valid ? "on" : "NOT MEASURABLE")} setPass={(recSetPass.Valid ? "on" : "NOT MEASURABLE")} " +
                  $"triangles={(recTris.Valid ? "on" : "NOT MEASURABLE")}");
    }

    private static ProfilerRecorder StartFirst(string markerName, params ProfilerCategory[] categories)
    {
        foreach (ProfilerCategory category in categories)
        {
            try
            {
                ProfilerRecorder recorder = ProfilerRecorder.StartNew(category, markerName);
                if (recorder.Valid) return recorder;
                recorder.Dispose();
            }
            catch (Exception)
            {
                // A category that cannot host this marker is simply the wrong candidate.
            }
        }

        return default;
    }

    private static void DisposeRecorders()
    {
        if (recGc.Valid) recGc.Dispose();
        if (recDraws.Valid) recDraws.Dispose();
        if (recBatches.Valid) recBatches.Dispose();
        if (recSetPass.Valid) recSetPass.Dispose();
        if (recTris.Valid) recTris.Dispose();
        recGc = default; recDraws = default; recBatches = default; recSetPass = default; recTris = default;
    }

    // =====================================================================
    // Per-frame sampling
    // =====================================================================

    private static void SampleFrame()
    {
        int frame = Time.frameCount;
        if (frame == lastFrame) return;
        lastFrame = frame;

        long nowTicks = System.Diagnostics.Stopwatch.GetTimestamp();
        double ms = (nowTicks - lastTimestamp) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        lastTimestamp = nowTicks;

        byte tag = pendingTag;
        pendingTag = (stage == 0) ? EvWarmup : (stage == 2 ? EvEnd : (stage == 4 ? EvFixture : EvGame));
        if (restartWatch > 0)
        {
            tag = EvRestart;
            restartWatch--;
        }

        if (rowCount >= Capacity) return;

        rowMs[rowCount] = ms;
        rowSeconds[rowCount] = ElapsedSeconds;
        rowTag[rowCount] = tag;
        rowGc[rowCount] = recGc.Valid ? recGc.LastValue : -1;
        rowMem[rowCount] = Profiler.GetTotalAllocatedMemoryLong();
        rowDraws[rowCount] = recDraws.Valid ? (int)recDraws.LastValue : -1;
        rowBatches[rowCount] = recBatches.Valid ? (int)recBatches.LastValue : -1;
        rowSetPass[rowCount] = recSetPass.Valid ? (int)recSetPass.LastValue : -1;
        rowTris[rowCount] = recTris.Valid ? (int)recTris.LastValue : -1;
        rowCount++;
    }

    // =====================================================================
    // Allocation fixture - amplitude scaling
    // =====================================================================

    /// <summary>
    /// Two workloads, each run at n and 2n calls in separate frames. Kind 0 is one
    /// GetSelectableMoves call; kind 1 is one full legality scan over every piece of the side to
    /// move, which is the work CheckForWin does at the end of a turn.
    /// </summary>
    private static void PrepareFixture()
    {
        fixturePieces.Clear();
        fixturePlayer = (int)fiCurrentPlayer.GetValue(board);

        Array grid = (Array)fiGrid.GetValue(board);
        for (int x = 0; x < DamaBoard.Size; x++)
        {
            for (int y = 0; y < DamaBoard.Size; y++)
            {
                if (Convert.ToInt32(grid.GetValue(x, y), CultureInfo.InvariantCulture) == fixturePlayer)
                    fixturePieces.Add(new Vector2Int(x, y));
            }
        }

        fixtureTotalSteps = FixtureKinds * 2 * FixtureRepeats;
        Debug.Log($"[Week16] Fixture: {fixturePieces.Count} piece(s) for side {fixturePlayer}, " +
                  $"{fixtureTotalSteps} samples, levels kind0 {{1000,2000}} kind1 {{60,120}}.");
    }

    private static void RunFixtureWorkload()
    {
        int kind = fixtureStep / (2 * FixtureRepeats);
        int n = FixtureLevel(kind, fixtureStep);

        if (kind == 0)
        {
            Vector2Int piece = fixturePieces.Count > 0 ? fixturePieces[0] : new Vector2Int(0, 1);
            for (int i = 0; i < n; i++) CallSelectable(piece.x, piece.y);
        }
        else if (kind == 1)
        {
            for (int i = 0; i < n; i++)
                for (int p = 0; p < fixturePieces.Count; p++)
                    CallSelectable(fixturePieces[p].x, fixturePieces[p].y);
        }
        else
        {
            // RESTART, including the profile write, because that is what one tap really costs.
            for (int i = 0; i < n; i++) board.RestartGame();
        }

        pendingFixtureN = n;
        pendingFixtureKind = kind;
        fixtureStep++;
        pendingTag = EvFixture;
    }

    private static int FixtureLevel(int kind, int step)
    {
        bool secondLevel = ((step % (2 * FixtureRepeats)) % 2) == 1;
        switch (kind)
        {
            case 0: return secondLevel ? 2000 : 1000;
            case 1: return secondLevel ? 120 : 60;
            default: return secondLevel ? 4 : 2;
        }
    }

    private static void RecordFixtureSample()
    {
        if (rowCount == 0) return;
        if (!recGc.Valid) return;

        fixtureN.Add(pendingFixtureN);
        fixtureKind.Add(pendingFixtureKind);
        fixtureGc.Add(rowGc[rowCount - 1]);
        fixtureMs.Add(rowMs[rowCount - 1]);
    }

    // =====================================================================
    // Scenario
    // =====================================================================

    private static void AdvanceTurn(double now)
    {
        if (Time.timeScale <= 0f)
        {
            // DeclareWinner stops the clock, which is how the harness knows the match ended.
            RestartNow(true);
            nextActionAt = now + RestartSettleSeconds;
            return;
        }

        if (turnsDone >= TargetTurns && !hasPendingMove)
        {
            stage = 3;
            restartCyclesLeft = RestartCycles;
            nextActionAt = now + RestartSettleSeconds;
            Debug.Log($"[Week16] {turnsDone} turns complete; measuring {RestartCycles} scripted RESTART cycles next.");
            return;
        }

        if (!hasPendingMove)
        {
            if (!ChooseMove())
            {
                Finish("no legal move available to the side to move - the scenario cannot continue");
                return;
            }

            pendingTag = EvProbe;
            nextActionAt = now + ProbeGapSeconds;
            return;
        }

        ExecutePendingMove();
        turnsDone++;
        nextActionAt = now + MoveIntervalSeconds;
    }

    private static bool ChooseMove()
    {
        if (recordMoves || replayIndex >= toX.Count) return ChooseMoveInner();

        int fx = fromX[replayIndex], fy = fromY[replayIndex];
        int tx = toX[replayIndex], ty = toY[replayIndex];

        if (IsReplayLegal(fx, fy, tx, ty))
        {
            pendingFromX = fx; pendingFromY = fy; pendingToX = tx; pendingToY = ty;
            CountPieces(out pendingRed, out pendingBlue);
            hasPendingMove = true;
            replayIndex++;
            return true;
        }

        replayMisses++;
        Debug.LogWarning($"[Week16] Recorded turn {replayIndex} ({fx},{fy})->({tx},{ty}) is not legal on this board; " +
                         "re-choosing from the live position. A run with replay misses is not strictly comparable and the report says so.");
        replayIndex++;
        return ChooseMoveInner();
    }

    /// <summary>
    /// Replays are only taken when the live game still offers that destination, so a changed move
    /// generator shows up as a warning instead of as a silent divergence between runs.
    /// </summary>
    private static bool IsReplayLegal(int fx, int fy, int tx, int ty)
    {
        Array grid = (Array)fiGrid.GetValue(board);
        int player = (int)fiCurrentPlayer.GetValue(board);
        if (Convert.ToInt32(grid.GetValue(fx, fy), CultureInfo.InvariantCulture) != player) return false;

        List<Vector2Int> moves = CallSelectable(fx, fy);
        if (moves == null) return false;

        for (int i = 0; i < moves.Count; i++)
            if (moves[i].x == tx && moves[i].y == ty) return true;

        return false;
    }

    /// <summary>Asks the game which move the side to move would offer, and takes the first one.</summary>
    private static bool ChooseMoveInner()
    {
        Array grid = (Array)fiGrid.GetValue(board);
        int player = (int)fiCurrentPlayer.GetValue(board);

        for (int x = 0; x < DamaBoard.Size; x++)
        {
            for (int y = 0; y < DamaBoard.Size; y++)
            {
                if (Convert.ToInt32(grid.GetValue(x, y), CultureInfo.InvariantCulture) != player) continue;

                List<Vector2Int> moves = CallSelectable(x, y);
                if (moves == null || moves.Count == 0) continue;

                pendingFromX = x;
                pendingFromY = y;
                pendingToX = moves[0].x;
                pendingToY = moves[0].y;
                CountPieces(out pendingRed, out pendingBlue);
                hasPendingMove = true;

                if (recordMoves)
                {
                    fromX.Add(pendingFromX);
                    fromY.Add(pendingFromY);
                    toX.Add(pendingToX);
                    toY.Add(pendingToY);
                }

                return true;
            }
        }

        return false;
    }

    private static void ExecutePendingMove()
    {
        hasPendingMove = false;
        pendingTag = EvMove;

        Vector2Int from = new Vector2Int(pendingFromX, pendingFromY);
        Vector2Int to = new Vector2Int(pendingToX, pendingToY);

        if (executeFn != null) executeFn(from, to);
        else miExecute.Invoke(board, new object[] { from, to });

        unchecked
        {
            moveHash = (moveHash ^ (uint)(pendingFromX * 64 + pendingFromY * 8 + pendingToX)) * 16777619u;
            moveHash = (moveHash ^ (uint)(pendingToY + pendingRed * 16 + pendingBlue * 256)) * 16777619u;
        }
    }

    private static List<Vector2Int> CallSelectable(int x, int y)
    {
        if (selectableFn != null) return selectableFn(x, y);
        return (List<Vector2Int>)miSelectable.Invoke(board, new object[] { x, y });
    }

    private static void RestartNow(bool matchEnd)
    {
        board.RestartGame();
        restarts++;
        if (matchEnd) matchEndRestarts++;

        restartWatch = RestartWatchFrames;
        pendingTag = EvRestart;

        Debug.Log(matchEnd
            ? $"[Week16] Match ended at turn {turnsDone}; RESTART called (#{restarts})."
            : $"[Week16] Scripted RESTART #{restarts} for the restart measurement.");
    }

    private static void CountPieces(out int red, out int blue)
    {
        red = 0;
        blue = 0;
        Array grid = (Array)fiGrid.GetValue(board);

        for (int x = 0; x < DamaBoard.Size; x++)
        {
            for (int y = 0; y < DamaBoard.Size; y++)
            {
                int cell = Convert.ToInt32(grid.GetValue(x, y), CultureInfo.InvariantCulture);
                if (cell == 1) red++;
                else if (cell == 2) blue++;
            }
        }
    }

    // =====================================================================
    // Output
    // =====================================================================

    private static void Finish(string failureNote)
    {
        running = false;
        EditorApplication.update -= Tick;

        string folder = Path.Combine(EvidenceRoot, runFolder);
        Directory.CreateDirectory(folder);

        WriteCsv(Path.Combine(folder, "frames.csv"));
        WriteSummary(Path.Combine(folder, "summary.txt"), failureNote);

        if (recordMoves && failureNote == null) WriteMoves(Path.Combine(EvidenceRoot, "moves.txt"));

        QualitySettings.vSyncCount = savedVSync;
        Application.targetFrameRate = savedTargetFrameRate;
        Profiler.enabled = profilerWasEnabled;
        DisposeRecorders();

        Debug.Log($"[Week16] {runLabel}: run complete, {rowCount} frame(s) recorded into {folder}.");
        EditorApplication.ExitPlaymode();
    }

    private static void WriteCsv(string path)
    {
        StringBuilder builder = new StringBuilder(rowCount * 48 + 128);
        builder.AppendLine("frame_index,seconds,tag,ms,gc_alloc_editor_wide,draw_calls,batches,set_pass,triangles,total_mem");

        for (int i = 0; i < rowCount; i++)
        {
            builder.Append(i).Append(',')
                   .Append(rowSeconds[i].ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                   .Append(TagName(rowTag[i])).Append(',')
                   .Append(rowMs[i].ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                   .Append(rowGc[i]).Append(',')
                   .Append(rowDraws[i]).Append(',')
                   .Append(rowBatches[i]).Append(',')
                   .Append(rowSetPass[i]).Append(',')
                   .Append(rowTris[i]).Append(',')
                   .Append(rowMem[i]).AppendLine();
        }

        File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
    }

    private static string TagName(byte tag)
    {
        switch (tag)
        {
            case EvWarmup: return "warmup";
            case EvProbe: return "probe";
            case EvMove: return "move";
            case EvRestart: return "restart";
            case EvEnd: return "end";
            case EvFixture: return "fixture";
            default: return "game";
        }
    }

    private static void WriteSummary(string path, string failureNote)
    {
        List<double> measuredMs = new List<double>(rowCount);
        List<double> restartMs = new List<double>(8);
        long gcWarmup = 0, gcProbe = 0, gcFixture = 0, gcTurn = 0;
        int turnFrames = 0;
        long drawSum = 0, batchSum = 0, setPassSum = 0, triSum = 0;
        int drawCount = 0, batchCount = 0, setPassCount = 0, triCount = 0;
        int drawNonZero = 0, batchNonZero = 0;
        long peakMem = 0, endMem = 0;
        double measureStartSeconds = -1, measureEndSeconds = -1;

        for (int i = 0; i < rowCount; i++)
        {
            if (rowTag[i] == EvWarmup) { if (rowGc[i] > 0) gcWarmup += rowGc[i]; continue; }
            if (rowTag[i] == EvProbe) { if (rowGc[i] > 0) gcProbe += rowGc[i]; continue; }
            if (rowTag[i] == EvFixture) { if (rowGc[i] > 0) gcFixture += rowGc[i]; continue; }

            if (measureStartSeconds < 0) measureStartSeconds = rowSeconds[i];
            measureEndSeconds = rowSeconds[i];

            measuredMs.Add(rowMs[i]);
            if (rowTag[i] == EvMove || rowTag[i] == EvRestart)
            {
                turnFrames++;
                if (rowGc[i] > 0) gcTurn += rowGc[i];
            }
            if (rowTag[i] == EvRestart) restartMs.Add(rowMs[i]);

            if (rowMem[i] > peakMem) peakMem = rowMem[i];
            endMem = rowMem[i];

            if (rowDraws[i] >= 0) { drawSum += rowDraws[i]; drawCount++; if (rowDraws[i] > 0) drawNonZero++; }
            if (rowBatches[i] >= 0) { batchSum += rowBatches[i]; batchCount++; if (rowBatches[i] > 0) batchNonZero++; }
            if (rowSetPass[i] >= 0) { setPassSum += rowSetPass[i]; setPassCount++; }
            if (rowTris[i] >= 0) { triSum += rowTris[i]; triCount++; }
        }

        double mean = Mean(measuredMs);
        double p95 = Percentile(measuredMs, 0.95);
        double max = 0;
        foreach (double value in measuredMs) if (value > max) max = value;
        int over33 = 0;
        foreach (double value in measuredMs) if (value > 33.3) over33++;

        double idleMean = MeanOfTag(EvGame);
        double restartWorst = 0, restartMean = 0;
        foreach (double value in restartMs) if (value > restartWorst) restartWorst = value;
        if (restartMs.Count > 0) restartMean = Mean(restartMs);

        // Amplitude scaling: fit bytes = intercept + slope * calls over the two levels.
        double singleSlope, singleSe, singleR2;
        FitKind(0, false, out singleSlope, out singleSe, out singleR2);
        double scanSlope, scanSe, scanR2;
        FitKind(1, false, out scanSlope, out scanSe, out scanR2);
        double restartBytesSlope, restartBytesSe, restartBytesR2;
        FitKind(2, false, out restartBytesSlope, out restartBytesSe, out restartBytesR2);
        double restartMsSlope, restartMsSe, restartMsR2;
        FitKind(2, true, out restartMsSlope, out restartMsSe, out restartMsR2);

        StringBuilder builder = new StringBuilder(6144);
        builder.AppendLine("DAMA - WEEK 16 PROFILE RUN");
        builder.AppendLine($"Run label      : {runLabel}");
        builder.AppendLine("Student        : Charibelle Guzman (BSIT-4)   Theme: Philippine Games and Sports");
        builder.AppendLine($"Written        : {DateTime.Now:yyyy-MM-dd HH:mm:ss} (local)");
        builder.AppendLine($"Harness        : Assets/Scripts/Editor/Week16Profile.cs   menu {MenuFor(runLabel)}");
        builder.AppendLine("--------------------------------------------------------------------------------------------------");
        builder.AppendLine("CONDITIONS (identical in every run, or the runs are not comparable)");
        builder.AppendLine($"  Unity            : {Application.unityVersion}   Editor: {Application.platform}");
        builder.AppendLine($"  Scene            : {SceneManager.GetActiveScene().path}");
        builder.AppendLine($"  Game view        : {Screen.width}x{Screen.height}");
        builder.AppendLine($"  Quality level    : {QualitySettings.GetQualityLevel()} ({QualitySettings.names[QualitySettings.GetQualityLevel()]})");
        builder.AppendLine($"  Frame pacing     : free-run for the run - vSync {savedVSync} -> 0, targetFrameRate {savedTargetFrameRate} -> -1,");
        builder.AppendLine("                     restored afterwards. A 16.7 ms vsync lock would hide every hitch.");
        builder.AppendLine($"  Render pipeline  : {PipelineName()}");
        builder.AppendLine($"  Graphics device  : {SystemInfo.graphicsDeviceName}");
        builder.AppendLine($"  Machine          : {SystemInfo.processorType}, {SystemInfo.systemMemorySize} MB");
        builder.AppendLine($"  Profiler enabled : {profilerWasEnabled} before the run; forced on for the run, restored after");
        builder.AppendLine($"  Save file        : {PlayerProfile.SavePath}");
        builder.AppendLine($"  Calls            : {(allocatingCallsAreStronglyTyped ? "strongly typed delegates for ExecuteMove and GetSelectableMoves (no per-call boxing)" : "MethodInfo.Invoke fallback - the same boxing in every run")}");
        builder.AppendLine("  Domain reload    : disabled on play-mode entry - no script was edited during the run");
        builder.AppendLine("--------------------------------------------------------------------------------------------------");
        builder.AppendLine("COUNTER SOURCES");
        builder.AppendLine($"  Editor-wide frame allocation : {gcSource}");
        builder.AppendLine($"  Draw calls       : {(recDraws.Valid ? "ProfilerRecorder 'Draw Calls Count'" : "NOT MEASURABLE")}");
        builder.AppendLine($"  Batches          : {(recBatches.Valid ? "ProfilerRecorder 'Batches Count'" : "NOT MEASURABLE")}");
        builder.AppendLine($"  Set pass calls   : {(recSetPass.Valid ? "ProfilerRecorder 'Set Pass Calls Count'" : "NOT MEASURABLE")}");
        builder.AppendLine($"  Triangles        : {(recTris.Valid ? "ProfilerRecorder 'Triangles Count'" : "NOT MEASURABLE")}");
        builder.AppendLine($"  Byte-level probe : {byteProbeNote}");
        builder.AppendLine("  A counter that is unavailable, or that reads 0 for every frame while the scene is drawing,");
        builder.AppendLine("  is reported as NOT MEASURABLE, never as 0 - a zero would read as a perfect score.");
        builder.AppendLine("--------------------------------------------------------------------------------------------------");
        builder.AppendLine("SCENARIO");
        builder.AppendLine($"  Warm-up discarded : {WarmupSeconds:0.0} s ({CountTag(EvWarmup)} frames)");
        builder.AppendLine($"  Fixture           : {fixtureGc.Count} sample frame(s)");
        builder.AppendLine($"  Measured window   : {(measureStartSeconds < 0 ? 0 : measureEndSeconds - measureStartSeconds):0.00} s " +
                           $"({measuredMs.Count} frames; probe, warm-up and fixture frames excluded)");
        builder.AppendLine($"  Turns played      : {turnsDone}   Restarts: {restarts} (forced by a finished match: {matchEndRestarts})");
        builder.AppendLine($"  Replay            : {(recordMoves ? "this run recorded moves.txt" : $"{toX.Count} recorded turn(s) replayed")}");
        builder.AppendLine($"  Move hash         : 0x{moveHash:X8}   (equal hashes across runs = the same game happened)");
        if (replayMisses > 0) builder.AppendLine($"  Replay misses     : {replayMisses} - the game would not accept a recorded turn; this run is not strictly comparable");
        builder.AppendLine($"  Failure note      : {(failureNote == null ? "none - the scenario completed" : failureNote)}");
        builder.AppendLine("--------------------------------------------------------------------------------------------------");
        builder.AppendLine("FRAME TIME (measured window, free-run)");
        builder.AppendLine($"  mean   : {mean:0.000} ms");
        builder.AppendLine($"  p95    : {(p95 < 0 ? double.NaN : p95):0.000} ms");
        builder.AppendLine($"  max    : {max:0.000} ms");
        builder.AppendLine($"  idle mean (non-action frames) : {idleMean:0.000} ms");
        builder.AppendLine($"  frames over 33.3 ms : {over33} of {measuredMs.Count}");
        builder.AppendLine("--------------------------------------------------------------------------------------------------");
        builder.AppendLine("ALLOCATION - THE DECISION METRIC (target PT-2: 0 B per turn beyond the HUD strings)");
        builder.AppendLine("  Method: amplitude scaling. The same call is run n and 2n times in separate frames and the");
        builder.AppendLine("  per-frame GC.Alloc is fitted; the slope is bytes per call and the intercept absorbs the");
        builder.AppendLine("  Editor's own per-frame cost, which cancels out of the comparison between runs.");
        builder.AppendLine($"  A. one GetSelectableMoves call            : {FormatSlope(singleSlope, singleSe, singleR2)}");
        builder.AppendLine($"  B. one legality scan over all {fixturePieces.Count,2} piece(s)    : {FormatSlope(scanSlope, scanSe, scanR2)}");
        builder.AppendLine("     (B is the work CheckForWin does at the end of every turn, so B is the per-turn figure)");
        string restartBytesText = FormatSlope(restartBytesSlope, restartBytesSe, restartBytesR2);
        string restartMsText = double.IsNaN(restartMsSlope)
            ? "NOT MEASURABLE (the fixture recorded no restart timings)"
            : restartMsSlope.ToString("0.000", CultureInfo.InvariantCulture) + " ms per restart, +- " +
              restartMsSe.ToString("0.000", CultureInfo.InvariantCulture) + " ms (R2 " + restartMsR2.ToString("0.00", CultureInfo.InvariantCulture) + ")";
        builder.AppendLine($"  C. one RESTART, profile write included   : {restartBytesText}");
        builder.AppendLine($"     its frame cost                       : {restartMsText}");
        builder.AppendLine($"  Editor-wide frame allocation, reference only: warm-up {gcWarmup} B   probe {gcProbe} B   " +
                           $"fixture {gcFixture} B   action frames {FormatBytesPerFrame(gcTurn, turnFrames)}");
        builder.AppendLine("  Those Editor-wide numbers are ~200 KB per frame of the Editor's own work and are recorded for");
        builder.AppendLine("  context only; they are not attributable to the game and are never used as the decision metric.");
        builder.AppendLine("--------------------------------------------------------------------------------------------------");
        builder.AppendLine("RESTART (target PT-3: worst frame under 16.7 ms)");
        builder.AppendLine($"  restarts measured: {restarts}   forced by a finished match: {matchEndRestarts}");
        builder.AppendLine($"  worst frame      : {(restartMs.Count == 0 ? "none recorded" : restartWorst.ToString("0.000", CultureInfo.InvariantCulture) + " ms")}");
        builder.AppendLine($"  mean frame       : {(restartMs.Count == 0 ? "none recorded" : restartMean.ToString("0.000", CultureInfo.InvariantCulture) + " ms")}");
        builder.AppendLine($"  idle mean        : {idleMean:0.000} ms   hitch over idle: {(restartMs.Count == 0 ? "n/a" : (restartMean - idleMean).ToString("0.000", CultureInfo.InvariantCulture) + " ms")}");
        builder.AppendLine($"  frame samples    : {restartMs.Count} (the {RestartWatchFrames} frames after each RESTART call)");
        builder.AppendLine("  RESTART destroys every piece, creates 24 new GameObjects and re-binds 64 squares, and it is");
        builder.AppendLine("  one tap away from a lost match, which is why it gets its own budget.");
        builder.AppendLine("--------------------------------------------------------------------------------------------------");
        builder.AppendLine("RENDERING AND MEMORY");
        builder.AppendLine($"  draw calls   mean: {(drawCount == 0 || drawNonZero == 0 ? "NOT MEASURABLE (recorder valid but every sample read 0 while the scene was drawing)" : ((double)drawSum / drawCount).ToString("0.0", CultureInfo.InvariantCulture))}");
        builder.AppendLine($"  batches      mean: {(batchCount == 0 || batchNonZero == 0 ? "NOT MEASURABLE (recorder valid but every sample read 0 while the scene was drawing)" : ((double)batchSum / batchCount).ToString("0.0", CultureInfo.InvariantCulture))}");
        builder.AppendLine($"  set pass     mean: {(setPassCount == 0 ? "NOT MEASURABLE" : ((double)setPassSum / setPassCount).ToString("0.0", CultureInfo.InvariantCulture))}");
        builder.AppendLine($"  triangles    mean: {(triCount == 0 ? "NOT MEASURABLE" : ((double)triSum / triCount).ToString("0.0", CultureInfo.InvariantCulture))}");
        builder.AppendLine($"  memory peak      : {peakMem / (1024.0 * 1024.0):0.0} MB   end of run: {endMem / (1024.0 * 1024.0):0.0} MB");
        builder.AppendLine("--------------------------------------------------------------------------------------------------");
        builder.AppendLine("WHAT THIS RUN DOES NOT PROVE");
        builder.AppendLine("  Anything about a phone. This is the Editor in play mode at one resolution on one machine;");
        builder.AppendLine("  there is no APK and adb has no device. Battery, thermals and GPU time are not measured.");

        File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
    }

    private static string FormatBytesPerFrame(long total, int frames)
    {
        if (frames == 0) return "NOT MEASURABLE";
        return (total / (double)frames).ToString("0.0", CultureInfo.InvariantCulture) + " B per frame";
    }

    private static string FormatSlope(double slope, double standardError, double r2)
    {
        if (double.IsNaN(slope))
            return "NOT MEASURABLE (the Editor-wide counter was unavailable, so the fit has no data)";

        string value = slope.ToString("0.0", CultureInfo.InvariantCulture);
        string error = standardError.ToString("0.0", CultureInfo.InvariantCulture);
        string fit = r2.ToString("0.00", CultureInfo.InvariantCulture);

        if (slope <= 2 * standardError)
            return $"below the resolution of this method (slope {value} B +- {error} B, R2 {fit}) - consistent with 0 B";

        return $"{value} B per call, +- {error} B (R2 {fit})";
    }

    /// <summary>Least squares for one fixture kind, with the slope's standard error and R-squared.</summary>
    private static void FitKind(int kind, bool milliseconds, out double slope, out double standardError, out double r2)
    {
        slope = double.NaN;
        standardError = double.NaN;
        r2 = double.NaN;

        List<double> xs = new List<double>();
        List<double> ys = new List<double>();
        for (int i = 0; i < fixtureN.Count && i < fixtureGc.Count && i < fixtureKind.Count && i < fixtureMs.Count; i++)
        {
            if (fixtureKind[i] != kind) continue;
            xs.Add(fixtureN[i]);
            ys.Add(milliseconds ? fixtureMs[i] : fixtureGc[i]);
        }

        if (xs.Count < 3) return;

        double meanX = Mean(xs);
        double meanY = Mean(ys);
        double sxx = 0, sxy = 0;
        for (int i = 0; i < xs.Count; i++)
        {
            double dx = xs[i] - meanX;
            sxx += dx * dx;
            sxy += dx * (ys[i] - meanY);
        }

        if (sxx <= 0) return;

        slope = sxy / sxx;
        double intercept = meanY - slope * meanX;

        double sse = 0, sst = 0;
        for (int i = 0; i < xs.Count; i++)
        {
            double predicted = intercept + slope * xs[i];
            double residual = ys[i] - predicted;
            sse += residual * residual;
            sst += (ys[i] - meanY) * (ys[i] - meanY);
        }

        if (xs.Count > 2) standardError = Math.Sqrt((sse / (xs.Count - 2)) / sxx);
        r2 = sst > 0 ? 1.0 - sse / sst : double.NaN;
    }

    private static double MeanOfTag(byte tag)
    {
        double sum = 0;
        int count = 0;
        for (int i = 0; i < rowCount; i++)
        {
            if (rowTag[i] != tag) continue;
            sum += rowMs[i];
            count++;
        }

        return count == 0 ? 0 : sum / count;
    }

    private static int CountTag(byte tag)
    {
        int count = 0;
        for (int i = 0; i < rowCount; i++) if (rowTag[i] == tag) count++;
        return count;
    }

    private static double Mean(List<double> values)
    {
        if (values.Count == 0) return 0;
        double sum = 0;
        foreach (double value in values) sum += value;
        return sum / values.Count;
    }

    private static double Percentile(List<double> values, double fraction)
    {
        if (values.Count == 0) return -1;
        List<double> sorted = new List<double>(values);
        sorted.Sort();
        int index = Mathf.Clamp((int)Math.Round(fraction * (sorted.Count - 1)), 0, sorted.Count - 1);
        return sorted[index];
    }

    private static string PipelineName()
    {
        UnityEngine.Rendering.RenderPipelineAsset asset = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        return asset != null ? asset.name : "Built-in";
    }

    private static string MenuFor(string label)
    {
        switch (label)
        {
            case "after-optimization-1": return MenuAfterOne;
            case "after-optimization-2": return MenuAfterTwo;
            default: return MenuBaseline;
        }
    }

    // =====================================================================
    // moves.txt - the recorded input sequence
    // =====================================================================

    private static void WriteMoves(string path)
    {
        StringBuilder builder = new StringBuilder(toX.Count * 12 + 96);
        builder.AppendLine("# Dama Week 16 - recorded turn list from the baseline run.");
        builder.AppendLine("# from_x\tfrom_y\tto_x\tto_y");
        for (int i = 0; i < toX.Count; i++)
            builder.AppendLine($"{fromX[i]}\t{fromY[i]}\t{toX[i]}\t{toY[i]}");

        File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
        Debug.Log($"[Week16] Recorded {toX.Count} turn(s) to {path}");
    }

    private static void LoadMoves(string path)
    {
        foreach (string line in File.ReadAllLines(path))
        {
            if (line.Length == 0 || line[0] == '#') continue;
            string[] parts = line.Split('\t');
            if (parts.Length != 4) continue;
            if (!int.TryParse(parts[0], out int fx)) continue;
            if (!int.TryParse(parts[1], out int fy)) continue;
            if (!int.TryParse(parts[2], out int tx)) continue;
            if (!int.TryParse(parts[3], out int ty)) continue;

            fromX.Add(fx); fromY.Add(fy); toX.Add(tx); toY.Add(ty);
        }

        Debug.Log($"[Week16] Replaying {toX.Count} recorded turn(s) from {path}");
    }
}
