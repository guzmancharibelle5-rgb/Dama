using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

/// <summary>
/// Week 20: the evidence for the draw rule, UNDO, HINT and the move animation, run from one menu item.
///
/// Menu: Tools > Dama > Week 20 > 2. Run Feature Checks
///
/// Four cases, each printing [Week20Checks] PASS or FAIL with the numbers behind it:
///   A  threefold repetition - two kings shuttle twice through the same four plies; the position the
///                        match started from appears a third time and the match must end in DRAW.
///   B  no progress     - 50 plies with neither a capture nor a promotion must end in DRAW, and the
///                        positions are built so no repetition can fire first.
///   C  undo            - a capture is played, then taken back: the grid, the crowns, the side to
///                        move, both piece counts and the captured piece itself must all return.
///   D  hint            - on three different positions, HINT must paint a move the board's own move
///                        generator calls legal, and it must not move anything.
///
/// Everything runs in EDIT mode: no coroutines, no play mode. The move animation snaps in edit mode
/// by design (see AnimatePieceTo), so the 120 ms travel is verified in play mode instead - that is
/// the plan's third verification step. A and B bank a draw through BankMatch, so the whole run points
/// PlayerProfile at a scratch folder first and restores the real save afterwards.
/// </summary>
public static class Week20Checks
{
    private const string MenuPath = "Tools/Dama/Week 20/2. Run Feature Checks";
    private const string EvidenceRelative = "docs/Charibelle Guzman/Week_18/Evidence/postweek18_features.log";

    private const int StatePlaying = 1;
    private const int StateGameOver = 3;

    private static int passed;
    private static int failed;
    private static readonly StringBuilder transcript = new StringBuilder();

    [MenuItem(MenuPath, false, 52)]
    public static void RunFeatureChecks()
    {
        passed = 0;
        failed = 0;
        transcript.Clear();

        Stopwatch watch = Stopwatch.StartNew();
        string sandbox = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Library", "Week20Checks");

        transcript.AppendLine("Week 20 feature checks - Dama");
        transcript.AppendLine($"run at {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ} (UTC)");
        transcript.AppendLine($"profile sandbox: {sandbox}");
        transcript.AppendLine();

        Debug.Log("[Week20Checks] === Week 20 feature checks ===");

        // A and B finish a match, which banks it. Point the store at a scratch folder so a check can
        // never touch the player's real record, and put it back in the finally below.
        PlayerProfile.SetDataDirectory(sandbox);

        try
        {
            // The board binds its squares and labels from the scene in Start, which never runs in edit
            // mode. Bind here so HINT has squares to paint and a draw has a summary line to write to.
            DamaBoard board = Board();
            if (board != null)
            {
                if (!(bool)Call(board, "BindLayout"))
                    throw new InvalidOperationException("DamaBoard.BindLayout found no built board: run Tools > Dama > Build Board first");

                BindResultLabels(board);
            }

            CaseA_ThreefoldRepetition();
            CaseB_NoProgress();
            CaseC_UndoRestoresCapture();
            CaseD_HintIsLegal();
        }
        catch (Exception error)
        {
            failed++;
            string line = $"FAIL harness threw {error.GetType().Name}: {error.Message}";
            transcript.AppendLine(line);
            transcript.AppendLine(error.StackTrace);
            Debug.LogError($"[Week20Checks] {line}\n{error.StackTrace}");
        }
        finally
        {
            DamaBoard board = UnityEngine.Object.FindAnyObjectByType<DamaBoard>();
            if (board != null) CleanUp(board);

            PlayerProfile.UseDefaultDataDirectory();
            Time.timeScale = 1f;
        }

        watch.Stop();

        string summary = $"[Week20Checks] {(failed == 0 ? "ALL PASS" : $"{failed} FAILED")} " +
                         $"({passed} passed, {failed} failed) in {watch.ElapsedMilliseconds} ms";

        transcript.AppendLine();
        transcript.AppendLine(summary);

        if (failed == 0) Debug.Log(summary);
        else Debug.LogError(summary);

        WriteTranscript();
    }

    // ---- A: the same position three times is a draw -------------------------

    private static void CaseA_ThreefoldRepetition()
    {
        const string name = "A threefold repetition";

        DamaBoard board = Board();
        if (board == null)
        {
            Fail(name, "no DamaBoard in the open scene (open Assets/Scenes/DamaGame.unity)");
            return;
        }

        int drawsBefore = PlayerProfile.Draws;

        // A sparse board with one king each, so the shuffle is exact: kings move any distance along a
        // diagonal, so two plies out and two plies back return to the very same position.
        PrepareAbstractBoard(board, 1);
        Place(board, 0, 0, 1, true);
        Place(board, 7, 0, 2, true);
        SetField(board, "currentPlayer", 1);
        ResetHistory(board, 1);

        Vector2Int[] froms =
        {
            new Vector2Int(0, 0), new Vector2Int(7, 0), new Vector2Int(1, 1), new Vector2Int(6, 1),
            new Vector2Int(0, 0), new Vector2Int(7, 0), new Vector2Int(1, 1), new Vector2Int(6, 1)
        };

        Vector2Int[] tos =
        {
            new Vector2Int(1, 1), new Vector2Int(6, 1), new Vector2Int(0, 0), new Vector2Int(7, 0),
            new Vector2Int(1, 1), new Vector2Int(6, 1), new Vector2Int(0, 0), new Vector2Int(7, 0)
        };

        int plies = 0;
        for (int i = 0; i < froms.Length; i++)
        {
            PlayBoardMove(board, froms[i], tos[i]);
            plies = i + 1;
            if (GameStateOf(board) != StatePlaying) break;
        }

        bool ended = GameStateOf(board) == StateGameOver;
        string summary = SummaryTextOf(board);
        int drawsAfter = PlayerProfile.Draws;

        string detail = $"{plies} plies played, state={(ended ? "GameOver" : "still " + GameStateOf(board))}, " +
                        $"summary='{summary}', draws {drawsBefore}->{drawsAfter}";

        if (ended && plies == 8 && summary.Contains("the same position 3 times") && drawsAfter == drawsBefore + 1)
            Pass(name, detail);
        else
            Fail(name, detail);
    }

    // ---- B: 50 plies without a capture or a promotion is a draw -------------

    private static void CaseB_NoProgress()
    {
        const string name = "B no progress after 50 plies";

        DamaBoard board = Board();
        if (board == null)
        {
            Fail(name, "no DamaBoard in the open scene");
            return;
        }

        int drawsBefore = PlayerProfile.Draws;

        // RED shuttles between two squares, BLUE walks to a square it has never stood on, so the two
        // of them together never repeat a position three times and only the no-progress rule can end
        // this. Every BLUE step is a quiet diagonal move that does not pass over the RED king.
        PrepareAbstractBoard(board, 1);
        Place(board, 0, 0, 1, true);
        Place(board, 7, 0, 2, true);
        SetField(board, "currentPlayer", 1);
        ResetHistory(board, 1);

        Vector2Int red = new Vector2Int(0, 0);
        Vector2Int blue = new Vector2Int(7, 0);
        List<Vector2Int> visited = new List<Vector2Int>(DamaPlyHistory.NoProgressLimit) { blue };

        int plies = 0;
        for (int ply = 0; ply < DamaPlyHistory.NoProgressLimit; ply++)
        {
            bool redToMove = Convert.ToInt32(GetField(board, "currentPlayer")) == 1;

            if (redToMove)
            {
                Vector2Int next = red == new Vector2Int(0, 0) ? new Vector2Int(1, 1) : new Vector2Int(0, 0);
                PlayBoardMove(board, red, next);
                red = next;
            }
            else
            {
                Vector2Int next = NextBlueStep(blue, red, visited);
                if (next.x < 0)
                {
                    Fail(name, $"no quiet diagonal step left for BLUE after {plies} plies; the walk needs widening");
                    return;
                }

                PlayBoardMove(board, blue, next);
                blue = next;
                visited.Add(next);
            }

            plies = ply + 1;
            if (GameStateOf(board) != StatePlaying) break;
        }

        DamaPlyHistory history = (DamaPlyHistory)GetField(board, "history");
        bool ended = GameStateOf(board) == StateGameOver;
        string summary = SummaryTextOf(board);
        int drawsAfter = PlayerProfile.Draws;

        string detail = $"{plies} plies, counter={history.NoCapturePlies}, state={(ended ? "GameOver" : "still playing")}, " +
                        $"summary='{summary}', draws {drawsBefore}->{drawsAfter}";

        if (ended && plies == DamaPlyHistory.NoProgressLimit &&
            summary.Contains($"{DamaPlyHistory.NoProgressLimit} plies without a capture") &&
            drawsAfter == drawsBefore + 1)
            Pass(name, detail);
        else
            Fail(name, detail);
    }

    /// <summary>The first dark square BLUE has never stood on that is a quiet diagonal move away.</summary>
    private static Vector2Int NextBlueStep(Vector2Int from, Vector2Int red, List<Vector2Int> visited)
    {
        for (int x = 0; x < DamaRules.Size; x++)
        {
            for (int y = 0; y < DamaRules.Size; y++)
            {
                Vector2Int candidate = new Vector2Int(x, y);

                if (!DamaRules.IsDark(x, y)) continue;
                if (candidate == red) continue;
                if (visited.Contains(candidate)) continue;
                if (!IsOnDiagonal(from, candidate)) continue;
                if (PassesOver(from, candidate, red)) continue;

                return candidate;
            }
        }

        return new Vector2Int(-1, -1);
    }

    // ---- C: undo puts a captured piece back ---------------------------------

    private static void CaseC_UndoRestoresCapture()
    {
        const string name = "C undo restores a capture";

        DamaBoard board = Board();
        if (board == null)
        {
            Fail(name, "no DamaBoard in the open scene");
            return;
        }

        // The real opening board, with all 24 pieces, because this case is about the scene coming
        // back - a captured piece must leave the pool and stand on its square again.
        SetField(board, "playMode", PlayMode.TwoPlayer);
        board.RestartGame();

        int redBefore = PieceCount(board, 1);
        int blueBefore = PieceCount(board, 2);
        int poolBefore = PoolCount(board);

        // RED (1,2) forward, BLUE (0,5) forward, then RED jumps that same BLUE man from (2,3) over
        // (1,4) to (0,5) - one quiet turn each, then one capture.
        PlayBoardMove(board, new Vector2Int(1, 2), new Vector2Int(2, 3));
        PlayBoardMove(board, new Vector2Int(0, 5), new Vector2Int(1, 4));
        PlayBoardMove(board, new Vector2Int(2, 3), new Vector2Int(0, 5));

        int redAfterCapture = PieceCount(board, 1);
        int blueAfterCapture = PieceCount(board, 2);
        int poolAfterCapture = PoolCount(board);

        board.UndoPressed();

        int redAfterUndo = PieceCount(board, 1);
        int blueAfterUndo = PieceCount(board, 2);
        int poolAfterUndo = PoolCount(board);
        int player = Convert.ToInt32(GetField(board, "currentPlayer"));

        bool capturedBack = CellAt(board, 1, 4) == 2;
        bool originBack = CellAt(board, 2, 3) == 1;
        bool landingCleared = CellAt(board, 0, 5) == 0;
        bool noCrowns = KingsAt(board) == 0;

        SpriteRenderer restored = RendererAt(board, 1, 4);
        bool active = restored != null && restored.gameObject.activeInHierarchy;
        bool onSquare = restored != null &&
                        Vector3.Distance(restored.transform.localPosition, new Vector3(1 - 3.5f, 4 - 3.5f, 0f)) < 0.001f;
        bool underPieces = restored != null && restored.transform.parent != null && restored.transform.parent.name == "Pieces";

        string detail = $"capture RED {redAfterCapture} BLUE {blueAfterCapture} pool {poolBefore}->{poolAfterCapture}, " +
                        $"after undo RED {redAfterUndo} BLUE {blueAfterUndo} pool {poolAfterUndo}, turn={(player == 1 ? "RED" : "BLUE")}, " +
                        $"captured {(capturedBack ? "back at (1,4)" : "MISSING")}, origin {originBack}, landing cleared {landingCleared}, " +
                        $"active {active}, on square {onSquare}, under Pieces {underPieces}";

        bool ok = redAfterCapture == redBefore && blueAfterCapture == blueBefore - 1 &&
                  poolAfterCapture == poolBefore + 1 &&
                  redAfterUndo == redBefore && blueAfterUndo == blueBefore &&
                  poolAfterUndo == poolBefore &&
                  player == 1 &&
                  capturedBack && originBack && landingCleared && noCrowns &&
                  active && onSquare && underPieces;

        if (ok) Pass(name, detail);
        else Fail(name, detail);
    }

    // ---- D: the hint is a move the board itself calls legal -----------------

    private static void CaseD_HintIsLegal()
    {
        const string name = "D hint paints a legal move";

        DamaBoard board = Board();
        if (board == null)
        {
            Fail(name, "no DamaBoard in the open scene");
            return;
        }

        List<string> verdicts = new List<string>();
        int bad = 0;

        // 1. The real opening board, RED to move.
        SetField(board, "playMode", PlayMode.TwoPlayer);
        board.RestartGame();
        if (!HintOn(board, "opening", verdicts)) bad++;

        // 2. BLUE to move with a jump on the board: with captures mandatory, the hint must find it.
        PrepareAbstractBoard(board, 2);
        Place(board, 3, 5, 2, false);
        Place(board, 2, 4, 1, false);
        Place(board, 7, 7, 2, false);
        Place(board, 0, 0, 1, false);
        if (!HintOn(board, "forced capture", verdicts)) bad++;

        // 3. A flying king and a lone man: the long-range generator has to produce something legal.
        PrepareAbstractBoard(board, 1);
        Place(board, 3, 3, 1, true);
        Place(board, 0, 7, 2, false);
        if (!HintOn(board, "flying king", verdicts)) bad++;

        int totalBefore = PieceCount(board, 1) + PieceCount(board, 2);

        string detail = string.Join("; ", verdicts) + $", pieces on the board after hinting: {totalBefore}";
        if (bad == 0) Pass(name, detail);
        else Fail(name, detail);
    }

    /// <summary>Presses HINT and checks the two squares it painted are a legal move for the side to move.</summary>
    private static bool HintOn(DamaBoard board, string label, List<string> verdicts)
    {
        Call(board, "ClearSelection");
        board.HintPressed();

        List<Vector2Int> painted = HighlightedCells(board);

        if (painted.Count != 2)
        {
            verdicts.Add($"{label}: painted {painted.Count} square(s), expected 2");
            return false;
        }

        Vector2Int from = painted[0];
        Vector2Int to = painted[1];
        bool legal = IsLegalForSideToMove(board, from, to);

        verdicts.Add($"{label}: {from}->{to} {(legal ? "legal" : "NOT LEGAL")}");

        // The hint must never touch the board itself: the piece is still on the square it started on.
        return legal && CellAt(board, from.x, from.y) != 0 && CellAt(board, to.x, to.y) == 0;
    }

    /// <summary>Asks the board's own generator, so the check and the game agree on what legal means.</summary>
    private static bool IsLegalForSideToMove(DamaBoard board, Vector2Int from, Vector2Int to)
    {
        if (from.x < 0 || to.x < 0) return false;

        IList moves = (IList)Call(board, "GetSelectableMoves", from.x, from.y);
        foreach (object move in moves)
            if ((Vector2Int)move == to) return true;

        return false;
    }

    // ---- Board plumbing -----------------------------------------------------

    private static DamaBoard Board() => UnityEngine.Object.FindAnyObjectByType<DamaBoard>();

    /// <summary>
    /// Gives the board its end-screen labels without going through CacheUI, which removes and re-adds
    /// every button's click handler - harmless at runtime, but this run happens in edit mode.
    /// </summary>
    private static void BindResultLabels(DamaBoard board)
    {
        GameObject canvas = GameObject.Find("GameCanvas");
        if (canvas == null) return;

        Transform win = canvas.transform.Find("WinPanel");
        if (win == null) return;

        SetField(board, "winText", win.Find("WinText")?.GetComponent<Text>());
        SetField(board, "winSummaryText", win.Find("SummaryText")?.GetComponent<Text>());
    }

    /// <summary>A fresh, empty board with <paramref name="side"/> to move and no pieces in the scene.</summary>
    private static void PrepareAbstractBoard(DamaBoard board, int side)
    {
        SetField(board, "playMode", PlayMode.TwoPlayer);
        SetField(board, "aiSide", 0);
        board.RestartGame();
        ClearBoard(board);
        SetField(board, "currentPlayer", side);
        Call(board, "RefreshUI");
    }

    private static void ClearBoard(DamaBoard board)
    {
        Transform pieces = (Transform)Call(board, "PiecesRoot");
        if (pieces != null) Call(board, "ReleaseAllPieces", pieces);

        Array grid = (Array)GetField(board, "grid");
        Array kings = (Array)GetField(board, "isKing");
        Array renderers = (Array)GetField(board, "pieceRenderers");
        Type cellType = grid.GetType().GetElementType();

        for (int x = 0; x < DamaRules.Size; x++)
        {
            for (int y = 0; y < DamaRules.Size; y++)
            {
                grid.SetValue(Enum.ToObject(cellType, 0), x, y);
                kings.SetValue(false, x, y);
                renderers.SetValue(null, x, y);
            }
        }
    }

    private static void Place(DamaBoard board, int x, int y, int side, bool king)
    {
        Array grid = (Array)GetField(board, "grid");
        Array kings = (Array)GetField(board, "isKing");
        Type cellType = grid.GetType().GetElementType();

        grid.SetValue(Enum.ToObject(cellType, side), x, y);
        kings.SetValue(king, x, y);
    }

    /// <summary>Points the draw watch at the position that was just built, as a new match would.</summary>
    private static void ResetHistory(DamaBoard board, int side)
    {
        DamaPlyHistory history = (DamaPlyHistory)GetField(board, "history");
        history.Reset(KeyOf(board, side));
    }

    private static ulong KeyOf(DamaBoard board, int side)
    {
        int[] cells = new int[PlySnapshot.BoardCells];
        bool[] kings = new bool[PlySnapshot.BoardCells];

        Array grid = (Array)GetField(board, "grid");
        Array kingGrid = (Array)GetField(board, "isKing");

        for (int x = 0; x < DamaRules.Size; x++)
        {
            for (int y = 0; y < DamaRules.Size; y++)
            {
                int index = x * DamaRules.Size + y;
                cells[index] = Convert.ToInt32(grid.GetValue(x, y));
                kings[index] = (bool)kingGrid.GetValue(x, y);
            }
        }

        return DamaPlyHistory.Key(cells, kings, side);
    }

    private static void PlayBoardMove(DamaBoard board, Vector2Int from, Vector2Int to)
    {
        Call(board, "ExecuteMove", from, to);
    }

    private static int GameStateOf(DamaBoard board) => Convert.ToInt32(GetField(board, "gameState"));

    private static string SummaryTextOf(DamaBoard board)
    {
        Text summary = (Text)GetField(board, "winSummaryText");
        return summary != null ? summary.text : "";
    }

    private static int CellAt(DamaBoard board, int x, int y)
    {
        Array grid = (Array)GetField(board, "grid");
        return Convert.ToInt32(grid.GetValue(x, y));
    }

    private static SpriteRenderer RendererAt(DamaBoard board, int x, int y)
    {
        Array renderers = (Array)GetField(board, "pieceRenderers");
        return renderers.GetValue(x, y) as SpriteRenderer;
    }

    private static int PieceCount(DamaBoard board, int side)
    {
        Array grid = (Array)GetField(board, "grid");
        int count = 0;

        for (int x = 0; x < DamaRules.Size; x++)
            for (int y = 0; y < DamaRules.Size; y++)
                if (Convert.ToInt32(grid.GetValue(x, y)) == side) count++;

        return count;
    }

    private static int KingsAt(DamaBoard board)
    {
        Array kings = (Array)GetField(board, "isKing");
        int count = 0;

        for (int x = 0; x < DamaRules.Size; x++)
            for (int y = 0; y < DamaRules.Size; y++)
                if ((bool)kings.GetValue(x, y)) count++;

        return count;
    }

    private static int PoolCount(DamaBoard board)
    {
        IList pool = (IList)GetField(board, "piecePool");
        return pool.Count;
    }

    private static List<Vector2Int> HighlightedCells(DamaBoard board)
    {
        IList cells = (IList)GetField(board, "highlightedCells");
        List<Vector2Int> copy = new List<Vector2Int>(cells.Count);

        foreach (object cell in cells) copy.Add((Vector2Int)cell);

        return copy;
    }

    private static bool IsOnDiagonal(Vector2Int from, Vector2Int to)
    {
        int dx = Mathf.Abs(to.x - from.x);
        int dy = Mathf.Abs(to.y - from.y);
        return dx != 0 && dx == dy;
    }

    /// <summary>True when <paramref name="blocker"/> stands strictly between the two squares.</summary>
    private static bool PassesOver(Vector2Int from, Vector2Int to, Vector2Int blocker)
    {
        if (!IsOnDiagonal(from, to)) return false;
        if (blocker == from || blocker == to) return false;

        int dx = Math.Sign(to.x - from.x);
        int dy = Math.Sign(to.y - from.y);

        int x = from.x + dx;
        int y = from.y + dy;

        while (x != to.x || y != to.y)
        {
            if (x == blocker.x && y == blocker.y) return true;
            x += dx;
            y += dy;
        }

        return false;
    }

    /// <summary>Leaves the scene on the title screen, two-player, with the clock running.</summary>
    private static void CleanUp(DamaBoard board)
    {
        SetField(board, "playMode", PlayMode.TwoPlayer);
        board.ReturnToMenu();
        Time.timeScale = 1f;
    }

    // ---- Reflection ---------------------------------------------------------

    private static object GetField(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null) throw new MissingFieldException(target.GetType().Name, name);
        return field.GetValue(target);
    }

    private static void SetField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null) throw new MissingFieldException(target.GetType().Name, name);
        field.SetValue(target, value);
    }

    private static object Call(object target, string name, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null) throw new MissingMethodException(target.GetType().Name, name);
        return method.Invoke(target, args);
    }

    // ---- Reporting ----------------------------------------------------------

    private static void Pass(string name, string detail)
    {
        passed++;
        transcript.AppendLine($"PASS {name} - {detail}");
        Debug.Log($"[Week20Checks] PASS {name} - {detail}");
    }

    private static void Fail(string name, string detail)
    {
        failed++;
        transcript.AppendLine($"FAIL {name} - {detail}");
        Debug.LogError($"[Week20Checks] FAIL {name} - {detail}");
    }

    private static void WriteTranscript()
    {
        try
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            string path = Path.Combine(root, EvidenceRelative.Replace('/', Path.DirectorySeparatorChar));

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, transcript.ToString(), new UTF8Encoding(false));

            Debug.Log($"[Week20Checks] Transcript written to {path}");
        }
        catch (Exception error)
        {
            Debug.LogWarning($"[Week20Checks] Could not write the transcript: {error.Message}");
        }
    }
}
