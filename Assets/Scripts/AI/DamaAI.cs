using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>The three opponents the title screen offers.</summary>
public enum AiLevel { Easy = 0, Normal = 1, Hard = 2 }

/// <summary>
/// Week 19: the opponent.
///
/// It chooses; it never acts. <see cref="Choose"/> is handed a copy of the board and returns the
/// one step to play, which DamaBoard then plays through its own ExecuteMove - so the capture sound,
/// the crown, the forced chain and the end screen all happen exactly once, in the place that
/// already owns them.
///
///   EASY    a uniformly random legal move. It still obeys the mandatory-capture rule, so it is a
///           weak player rather than an illegal one.
///   NORMAL  greedy with one reply of look-ahead: take the most material, crown a man when it can,
///           and refuse a landing square the opponent can jump straight back off.
///   HARD    alpha-beta with iterative deepening to depth 4, material 100/300, a mobility term and
///           an advancement term. Bounded by a node count and a time budget so a phone frame is
///           never held hostage by one think.
///
/// Scoring is from the moving side's point of view (negamax): a position that is good for RED is
/// bad for BLUE, and the search simply flips the sign on every turn.
/// </summary>
public static class DamaAI
{
    private const int ManValue = 100;
    private const int DamaValue = 300;
    private const int WinScore = 100000;
    private const int MaxDepth = 4;

    /// <summary>The shipped think budgets. A phone frame must never wait on one move for long.</summary>
    public const int DefaultNodeBudget = 30000;
    public const long DefaultTimeBudgetMs = 150;

    /// <summary>
    /// Live budgets, settable so Week 19's ten-game tournament can run at a fraction of the cost of a
    /// real match. <see cref="ResetBudgets"/> puts the shipped values back.
    /// </summary>
    public static int NodeBudget = DefaultNodeBudget;
    public static long TimeBudgetMs = DefaultTimeBudgetMs;

    /// <summary>When false, Choose stops writing a console line per move (the tournament reports for itself).</summary>
    public static bool Verbose = true;

    public static void ResetBudgets()
    {
        NodeBudget = DefaultNodeBudget;
        TimeBudgetMs = DefaultTimeBudgetMs;
    }

    private static readonly Vector2Int NoChain = new Vector2Int(-1, -1);
    private static System.Random random = new System.Random(20260101);

    /// <summary>Reused by the one-reply look-ahead, so a greedy turn allocates nothing per move.</summary>
    private static readonly List<DamaRules.Candidate> replyBuffer = new List<DamaRules.Candidate>(64);

    /// <summary>What the AI decided. <c>valid</c> is false when the side has no legal move at all.</summary>
    public struct Choice
    {
        public Vector2Int from;
        public Vector2Int to;
        public bool valid;
        public int score;
        public int nodes;

        public override string ToString() =>
            valid ? $"({from.x},{from.y})->({to.x},{to.y})" : "(no move)";
    }

    /// <summary>Marks the random seed. Called by the check harness so its games are repeatable.</summary>
    public static void Seed(int value) => random = new System.Random(value);

    private static void Trace(string message)
    {
        if (Verbose) Debug.Log(message);
    }

    /// <summary>
    /// Picks one step for <paramref name="side"/>. Pass the chain piece as
    /// <paramref name="forcedFrom"/> when a jump chain is in progress; it is then the only piece
    /// allowed to move, which is exactly what DamaBoard's chainPiece does.
    /// </summary>
    public static Choice Choose(DamaRules.Position position, int side, AiLevel level, Vector2Int forcedFrom)
    {
        Choice choice = new Choice { from = new Vector2Int(-1, -1), to = new Vector2Int(-1, -1), valid = false };

        List<DamaRules.Candidate> moves = new List<DamaRules.Candidate>(64);
        DamaRules.CollectMoves(position, side, forcedFrom, moves);

        if (moves.Count == 0)
        {
            Trace($"[DamaAI] {(side == 1 ? "RED" : "BLUE")} has no legal move - the board decides this one.");
            return choice;
        }

        switch (level)
        {
            case AiLevel.Easy:
                choice = PickEasy(moves);
                break;
            case AiLevel.Normal:
                choice = PickNormal(position, side, moves);
                break;
            default:
                choice = PickHard(position, side, moves);
                break;
        }

        return choice;
    }

    // ---- Easy ---------------------------------------------------------------

    private static Choice PickEasy(List<DamaRules.Candidate> moves)
    {
        DamaRules.Candidate pick = moves[random.Next(moves.Count)];

        Trace($"[DamaAI] EASY chose {pick} (random legal move of {moves.Count}).");
        return new Choice { from = pick.from, to = pick.to, valid = true, score = 0, nodes = 0 };
    }

    // ---- Normal -------------------------------------------------------------

    private static Choice PickNormal(DamaRules.Position position, int side, List<DamaRules.Candidate> moves)
    {
        Choice best = new Choice { valid = false };
        int bestScore = int.MinValue;

        foreach (DamaRules.Candidate move in moves)
        {
            DamaRules.Step step = DamaRules.Apply(position, move.from, move.to);

            // What the opponent can take straight back is the whole point of this level.
            int reply = BestOpponentCapture(position, DamaRules.Opponent(side));
            int score = ScoreFor(position, side, move, step) - reply;

            DamaRules.Undo(position, step);

            if (score > bestScore)
            {
                bestScore = score;
                best = new Choice { from = move.from, to = move.to, valid = true, score = score, nodes = 1 };
            }
        }

        if (best.valid)
            Trace($"[DamaAI] NORMAL chose {best} score={bestScore} of {moves.Count} legal moves.");

        return best;
    }

    /// <summary>How many points of material the opponent could take in one step of their own.</summary>
    private static int BestOpponentCapture(DamaRules.Position position, int opponent)
    {
        List<DamaRules.Candidate> replies = replyBuffer;
        DamaRules.CollectMoves(position, opponent, NoChain, replies);

        int worst = 0;
        foreach (DamaRules.Candidate reply in replies)
        {
            if (!reply.capture) continue;

            // The jumped piece is what is actually removed; it is the square between the two.
            Vector2Int jumped = DamaRules.FindJumpedCell(position, reply.from, reply.to);
            if (jumped.x < 0) continue;

            int taken = position.kings[jumped.x, jumped.y] ? DamaValue : ManValue;
            if (taken > worst) worst = taken;
        }

        return worst;
    }

    /// <summary>Immediate value of a step for the mover: material, promotion, advancement.</summary>
    private static int ScoreFor(DamaRules.Position position, int side, DamaRules.Candidate move, DamaRules.Step step)
    {
        int score = 0;

        if (step.jumped.x >= 0) score += step.jumpedWasKing ? DamaValue : ManValue;
        if (step.wasPromotion) score += DamaValue - ManValue;

        int forward = side == 1 ? step.to.y : DamaRules.Size - 1 - step.to.y;
        score += forward * 2;

        return score;
    }

    // ---- Hard ---------------------------------------------------------------

    private sealed class SearchState
    {
        public Stopwatch watch;
        public int nodes;
        public bool aborted;
    }

    private static Choice PickHard(DamaRules.Position position, int side, List<DamaRules.Candidate> moves)
    {
        SearchState state = new SearchState { watch = Stopwatch.StartNew() };
        Choice best = new Choice { from = moves[0].from, to = moves[0].to, valid = true };

        int completedDepth = 0;

        for (int depth = 1; depth <= MaxDepth && !state.aborted; depth++)
        {
            Choice depthBest = best;
            int depthScore = int.MinValue;
            int alpha = -WinScore * 2;
            foreach (DamaRules.Candidate move in moves)
            {
                DamaRules.Step step = DamaRules.Apply(position, move.from, move.to);

                int score;
                if (DamaRules.ChainContinues(position, step))
                    score = Search(position, side, move.to, depth - 1, alpha, WinScore * 2, state);
                else
                    score = -Search(position, DamaRules.Opponent(side), NoChain, depth - 1, -WinScore * 2, -alpha, state);

                DamaRules.Undo(position, step);

                if (state.aborted) break;

                if (score > depthScore)
                {
                    depthScore = score;
                    depthBest = new Choice { from = move.from, to = move.to, valid = true };
                }

                if (depthScore > alpha) alpha = depthScore;
            }

            if (state.aborted) break;

            best = depthBest;
            best.score = depthScore;
            completedDepth = depth;
        }

        best.nodes = state.nodes;
        Trace($"[DamaAI] HARD chose {best} score={best.score} depth={completedDepth} nodes={state.nodes} " +
              $"time={state.watch.ElapsedMilliseconds}ms{(state.aborted ? " (budget reached)" : "")} of {moves.Count} legal moves.");

        return best;
    }

    private static int Search(DamaRules.Position position, int side, Vector2Int forcedFrom, int depth,
        int alpha, int beta, SearchState state)
    {
        state.nodes++;

        if (state.nodes > NodeBudget || state.watch.ElapsedMilliseconds > TimeBudgetMs)
        {
            state.aborted = true;
            return Evaluate(position, side);
        }

        if (depth <= 0) return Evaluate(position, side);

        List<DamaRules.Candidate> moves = new List<DamaRules.Candidate>(64);
        DamaRules.CollectMoves(position, side, forcedFrom, moves);

        if (moves.Count == 0) return -WinScore + depth;   // no move: this side has lost

        int best = -WinScore * 2;

        foreach (DamaRules.Candidate move in moves)
        {
            DamaRules.Step step = DamaRules.Apply(position, move.from, move.to);

            int score;
            if (DamaRules.ChainContinues(position, step))
                score = Search(position, side, move.to, depth - 1, alpha, beta, state);
            else
                score = -Search(position, DamaRules.Opponent(side), NoChain, depth - 1, -beta, -alpha, state);

            DamaRules.Undo(position, step);

            if (state.aborted) return best > -WinScore * 2 ? best : score;

            if (score > best) best = score;
            if (best > alpha) alpha = best;
            if (alpha >= beta) break;
        }

        return best;
    }

    /// <summary>Static value of a position for <paramref name="side"/>: material, mobility, advancement.</summary>
    private static int Evaluate(DamaRules.Position position, int side)
    {
        int score = 0;

        for (int x = 0; x < DamaRules.Size; x++)
        {
            for (int y = 0; y < DamaRules.Size; y++)
            {
                DamaRules.Cell cell = position.cells[x, y];
                if (cell == DamaRules.Cell.Empty) continue;

                bool mine = DamaRules.CellToSide(cell) == side;
                bool king = position.kings[x, y];

                int value = king ? DamaValue : ManValue;
                int forward = DamaRules.CellToSide(cell) == 1 ? y : DamaRules.Size - 1 - y;

                score += mine ? value + forward * 2 : -(value + forward * 2);
            }
        }

        // Side to move with a capture available is a tempo the material count does not show.
        if (DamaRules.PlayerHasCapture(position, side)) score += 8;
        if (DamaRules.PlayerHasCapture(position, DamaRules.Opponent(side))) score -= 8;

        return score;
    }
}
