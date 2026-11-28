using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Week 19: the evidence for the mode work, run from a menu item in one pass.
///
/// Menu: Tools > Dama > Week 19 > 2. Run AI Checks
///
/// Four cases, each printing [Week19Checks] PASS or FAIL with the number behind it:
///   A  rules parity  - 200 seeded random positions; DamaBoard's own move generator and
///                      DamaRules must agree on how many moves each side has.
///   B  mandatory capture - on a position where a jump exists, EASY, NORMAL and HARD must all
///                      choose a jump (the rule that makes Dama Dama).
///   C  strength      - ten headless games, HARD against EASY, at a reduced think budget so the
///                      run stays quick; HARD must win at least seven.
///   D  AI turn       - on the live board, a chosen move played through ExecuteMove must move the
///                      piece and hand the turn back to the other side.
///
/// Everything here runs in EDIT mode: no coroutines, no play mode, no scene save. The AI turn's
/// timing and pause behaviour are exercised in play mode instead, which is what the plan's
/// verification step does.
/// </summary>
public static class Week19Checks
{
    private const string MenuPath = "Tools/Dama/Week 19/2. Run AI Checks";
    private const int ParityPositions = 200;
    private const int TournamentGames = 10;
    private const int TournamentWinsNeeded = 7;
    private const int TournamentBudgetMs = 20;

    private static int passed;
    private static int failed;

    [MenuItem(MenuPath, false, 42)]
    public static void RunAiChecks()
    {
        passed = 0;
        failed = 0;

        Stopwatch watch = Stopwatch.StartNew();
        DamaAI.Seed(190019);
        bool verboseWas = DamaAI.Verbose;

        Debug.Log("[Week19Checks] === Week 19 AI checks ===");

        try
        {
            DamaAI.Verbose = false;
            CaseA_RulesParity();
            CaseB_MandatoryCapture();

            DamaAI.TimeBudgetMs = TournamentBudgetMs;
            CaseC_Tournament();
            DamaAI.ResetBudgets();

            CaseD_AiTurnMovesAndPasses();
        }
        catch (Exception error)
        {
            failed++;
            Debug.LogError($"[Week19Checks] FAIL threw {error.GetType().Name}: {error.Message}\n{error.StackTrace}");
        }
        finally
        {
            DamaAI.Verbose = verboseWas;
            DamaAI.ResetBudgets();
        }

        watch.Stop();
        string summary = $"[Week19Checks] {(failed == 0 ? "ALL PASS" : $"{failed} FAILED")} " +
                         $"({passed} passed, {failed} failed) in {watch.ElapsedMilliseconds} ms";

        if (failed == 0) Debug.Log(summary);
        else Debug.LogError(summary);
    }

    // ---- A: the two rule engines must agree ---------------------------------

    private static void CaseA_RulesParity()
    {
        DamaBoard board = UnityEngine.Object.FindAnyObjectByType<DamaBoard>();
        if (board == null)
        {
            Fail("A rules parity", "no DamaBoard in the open scene (open Assets/Scenes/DamaGame.unity)");
            return;
        }

        System.Random rng = new System.Random(20261127);
        int mismatches = 0;
        int positions = 0;
        string firstMismatch = "";

        for (int i = 0; i < ParityPositions; i++)
        {
            DamaRules.Position position = RandomPosition(rng);
            WriteBoard(board, position);
            positions++;

            for (int side = 1; side <= 2; side++)
            {
                int boardTotal = BoardMoveCount(board, side);
                int rulesTotal = RulesMoveCount(position, side);

                if (boardTotal == rulesTotal) continue;

                mismatches++;
                if (firstMismatch.Length == 0)
                    firstMismatch = $"position {i} side {(side == 1 ? "RED" : "BLUE")}: board={boardTotal} rules={rulesTotal}";
            }
        }

        // Leave the board as the scene had it, so a later case starts clean.
        CleanUp(board);

        string detail = $"{positions} positions x 2 sides, {mismatches} disagreements" +
                        (firstMismatch.Length > 0 ? $" (first: {firstMismatch})" : "");

        if (mismatches == 0) Pass("A rules parity", detail);
        else Fail("A rules parity", detail);
    }

    // ---- B: a jump that exists must be taken -------------------------------

    private static void CaseB_MandatoryCapture()
    {
        // BLUE (men move DOWN the board) at (3,5) can jump the RED man at (2,4) and land on (1,3).
        // A BLUE man at (7,7) and a RED man at (0,0) give both sides a piece that is not capturing,
        // so a level that ignored the rule would have something else to choose.
        DamaRules.Position position = new DamaRules.Position();
        position.Set(3, 5, 2, false);
        position.Set(2, 4, 1, false);
        position.Set(7, 7, 2, false);
        position.Set(0, 0, 1, false);

        List<string> verdicts = new List<string>();
        int bad = 0;

        foreach (AiLevel level in new[] { AiLevel.Easy, AiLevel.Normal, AiLevel.Hard })
        {
            DamaAI.Choice choice = DamaAI.Choose(position, 2, level, new Vector2Int(-1, -1));

            if (!choice.valid)
            {
                verdicts.Add($"{level}=no move");
                bad++;
                continue;
            }

            DamaRules.Position copy = position.Clone();
            DamaRules.Step step = DamaRules.Apply(copy, choice.from, choice.to);
            bool captured = step.jumped.x >= 0;

            verdicts.Add($"{level}={choice}{(captured ? "" : " (NOT A JUMP)")}");
            if (!captured) bad++;
        }

        string detail = string.Join(", ", verdicts);
        if (bad == 0) Pass("B mandatory capture", detail);
        else Fail("B mandatory capture", detail);
    }

    // ---- C: HARD must beat EASY --------------------------------------------

    private static void CaseC_Tournament()
    {
        int hardWins = 0;
        int draws = 0;
        List<string> lines = new List<string>();

        for (int game = 0; game < TournamentGames; game++)
        {
            int winner = PlayGame(1900 + game, AiLevel.Hard, AiLevel.Easy, out int plies);

            if (winner == 1) hardWins++;
            else if (winner == 0) draws++;

            lines.Add($"g{game + 1}:{plies}ply->{(winner == 1 ? "HARD" : winner == 2 ? "EASY" : "draw")}");
        }

        string detail = $"HARD won {hardWins}/{TournamentGames} (needed {TournamentWinsNeeded}), {draws} draws at " +
                        $"{TournamentBudgetMs} ms/move [{string.Join(" ", lines)}]";

        if (hardWins >= TournamentWinsNeeded) Pass("C HARD beats EASY", detail);
        else Fail("C HARD beats EASY", detail);
    }

    /// <summary>One headless game: RED plays <paramref name="red"/>, BLUE plays <paramref name="blue"/>.</summary>
    private static int PlayGame(int seed, AiLevel red, AiLevel blue, out int plies)
    {
        DamaAI.Seed(seed);

        DamaRules.Position position = DamaRules.Position.Starting();
        int side = 1;
        Vector2Int forced = new Vector2Int(-1, -1);

        for (plies = 0; plies < 400; plies++)
        {
            DamaAI.Choice choice = DamaAI.Choose(position, side, side == 1 ? red : blue, forced);

            if (!choice.valid)
            {
                plies++;
                return DamaRules.Opponent(side);   // the side with no move loses
            }

            DamaRules.Step step = DamaRules.Apply(position, choice.from, choice.to);

            if (DamaRules.ChainContinues(position, step))
            {
                forced = choice.to;
                continue;
            }

            forced = new Vector2Int(-1, -1);
            side = DamaRules.Opponent(side);
        }

        return 0;   // 400 plies without a result is a draw for the purposes of this check
    }

    // ---- D: the AI's move goes through the board and passes the turn --------

    private static void CaseD_AiTurnMovesAndPasses()
    {
        DamaBoard board = UnityEngine.Object.FindAnyObjectByType<DamaBoard>();
        if (board == null)
        {
            Fail("D AI turn", "no DamaBoard in the open scene");
            return;
        }

        // Two-player first, so RestartGame does not try to hand the turn to a coroutine that cannot
        // run in edit mode; the mode is then set the way StartVsAi sets it.
        board.RestartGame();
        SetField(board, "playMode", PlayMode.VsAi);
        SetField(board, "aiSide", 2);
        SetField(board, "currentPlayer", 2);
        Call(board, "RefreshUI");

        DamaRules.Position snapshot = (DamaRules.Position)Call(board, "SnapshotPosition");
        DamaAI.Choice choice = DamaAI.Choose(snapshot, 2, AiLevel.Normal, new Vector2Int(-1, -1));

        if (!choice.valid)
        {
            Fail("D AI turn", "the AI found no legal move from the opening position");
            CleanUp(board);
            return;
        }

        bool legal = (bool)Call(board, "IsLegalBoardMove", choice.from, choice.to);
        if (!legal)
        {
            Fail("D AI turn", $"the board rejected the AI's opening move {choice}");
            CleanUp(board);
            return;
        }

        Call(board, "ExecuteMove", choice.from, choice.to);

        int player = (int)GetField(board, "currentPlayer");
        int atDestination = CellSideAt(board, choice.to.x, choice.to.y);
        int atOrigin = CellSideAt(board, choice.from.x, choice.from.y);

        bool moved = atDestination == 2 && atOrigin == 0;
        bool turned = player == 1;

        string detail = $"move={choice}, destination=BLUE:{moved}, currentPlayer={(player == 1 ? "RED" : "BLUE")}";

        CleanUp(board);

        if (moved && turned) Pass("D AI turn", detail);
        else Fail("D AI turn", detail);
    }

    /// <summary>Puts the board back to the title screen, two-player, so the scene is not left mid-match.</summary>
    private static void CleanUp(DamaBoard board)
    {
        SetField(board, "playMode", PlayMode.TwoPlayer);
        board.ReturnToMenu();
    }

    // ---- Position helpers ---------------------------------------------------

    private static DamaRules.Position RandomPosition(System.Random rng)
    {
        DamaRules.Position position = new DamaRules.Position();

        for (int x = 0; x < DamaRules.Size; x++)
        {
            for (int y = 0; y < DamaRules.Size; y++)
            {
                if (!DamaRules.IsDark(x, y)) continue;

                int roll = rng.Next(100);
                if (roll < 20) position.Set(x, y, 1, rng.Next(100) < 20);
                else if (roll < 40) position.Set(x, y, 2, rng.Next(100) < 20);
            }
        }

        if (DamaRules.CountPieces(position, 1) == 0) position.Set(0, 1, 1, false);
        if (DamaRules.CountPieces(position, 2) == 0) position.Set(7, 6, 2, false);

        return position;
    }

    private static int RulesMoveCount(DamaRules.Position position, int side)
    {
        List<DamaRules.Candidate> moves = new List<DamaRules.Candidate>(64);
        DamaRules.CollectMoves(position, side, new Vector2Int(-1, -1), moves);
        return moves.Count;
    }

    /// <summary>DamaBoard's own answer, summed piece by piece through its private generator.</summary>
    private static int BoardMoveCount(DamaBoard board, int side)
    {
        MethodInfo selectable = typeof(DamaBoard).GetMethod("GetSelectableMoves", BindingFlags.Instance | BindingFlags.NonPublic);
        Array grid = (Array)GetField(board, "grid");

        int total = 0;
        for (int x = 0; x < DamaRules.Size; x++)
        {
            for (int y = 0; y < DamaRules.Size; y++)
            {
                if (Convert.ToInt32(grid.GetValue(x, y)) != side) continue;

                IList moves = (IList)selectable.Invoke(board, new object[] { x, y });
                total += moves.Count;
            }
        }

        return total;
    }

    private static void WriteBoard(DamaBoard board, DamaRules.Position position)
    {
        Array grid = (Array)GetField(board, "grid");
        Array kings = (Array)GetField(board, "isKing");
        Type cellType = grid.GetType().GetElementType();

        for (int x = 0; x < DamaRules.Size; x++)
        {
            for (int y = 0; y < DamaRules.Size; y++)
            {
                int side = DamaRules.CellToSide(position.cells[x, y]);
                grid.SetValue(Enum.ToObject(cellType, side), x, y);
                kings.SetValue(position.kings[x, y], x, y);
            }
        }
    }

    private static int CellSideAt(DamaBoard board, int x, int y)
    {
        Array grid = (Array)GetField(board, "grid");
        return Convert.ToInt32(grid.GetValue(x, y));
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
        Debug.Log($"[Week19Checks] PASS {name} - {detail}");
    }

    private static void Fail(string name, string detail)
    {
        failed++;
        Debug.LogError($"[Week19Checks] FAIL {name} - {detail}");
    }
}
