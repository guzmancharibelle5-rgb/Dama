using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Week 19: the rules the AI thinks with.
///
/// DamaBoard stays the only thing that changes the live board. This class works on its own
/// copy of a position (<see cref="Position"/>), so the AI can try a move without touching the
/// board, the piece pool, the sounds or the shared move buffers in DamaBoard
/// (<c>captureBuffer</c> / <c>simpleBuffer</c> are reused lists, and a search that called them
/// while iterating would read its own output).
///
/// The rules here mirror DamaBoard line for line:
///   man       - steps and jumps one square diagonally FORWARD only (RED up, BLUE down)
///   Dama      - slides and jumps along a diagonal at any distance, landing past the first piece
///   capture   - mandatory for the whole side; a piece may only step when no piece can jump
///   chain     - a jump that can jump again keeps the turn with the same piece
///   promote   - reaching the far row turns a man into a Dama, including mid-chain
///
/// Week 19's check harness compares this generator against DamaBoard's on seeded positions, so a
/// drift between the two is a failed case rather than a quiet difference in play strength.
/// </summary>
public static class DamaRules
{
    public const int Size = 8;

    /// <summary>Same numbering as DamaBoard's private Cell: 0 empty, 1 RED, 2 BLUE.</summary>
    public enum Cell { Empty = 0, Player1 = 1, Player2 = 2 }

    private static bool reported;

    /// <summary>A candidate destination for one piece, as the AI enumerates them.</summary>
    public struct Candidate
    {
        public Vector2Int from;
        public Vector2Int to;
        public bool capture;

        public override string ToString() => $"({from.x},{from.y})->({to.x},{to.y}){(capture ? "x" : "")}";
    }

    /// <summary>What <see cref="Apply"/> changed, so <see cref="Undo"/> can put it all back.</summary>
    public struct Step
    {
        public Vector2Int from;
        public Vector2Int to;
        public Vector2Int jumped;
        public Cell moving;
        public bool wasKing;
        public bool jumpedWasKing;
        public bool wasPromotion;
    }

    /// <summary>A board the AI can copy and edit freely. Indexing is [x, y], as in DamaBoard.</summary>
    public sealed class Position
    {
        public readonly Cell[,] cells = new Cell[Size, Size];
        public readonly bool[,] kings = new bool[Size, Size];

        public Position Clone()
        {
            Position copy = new Position();
            System.Array.Copy(cells, copy.cells, cells.Length);
            System.Array.Copy(kings, copy.kings, kings.Length);
            return copy;
        }

        public void Clear()
        {
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    cells[x, y] = Cell.Empty;
                    kings[x, y] = false;
                }
            }
        }

        /// <summary>Sets one square. <paramref name="side"/> is 1 or 2; 0 empties the square.</summary>
        public void Set(int x, int y, int side, bool king)
        {
            if (!InBounds(x, y)) return;

            cells[x, y] = side == 1 ? Cell.Player1 : side == 2 ? Cell.Player2 : Cell.Empty;
            kings[x, y] = side != 0 && king;
        }

        /// <summary>The opening position: three back rows per side, dark squares only.</summary>
        public static Position Starting()
        {
            Position position = new Position();

            for (int y = 0; y <= 2; y++)
                for (int x = 0; x < Size; x++)
                    if (IsDark(x, y)) position.cells[x, y] = Cell.Player1;

            for (int y = 5; y <= 7; y++)
                for (int x = 0; x < Size; x++)
                    if (IsDark(x, y)) position.cells[x, y] = Cell.Player2;

            return position;
        }
    }

    /// <summary>Called once from the AI driver so the console shows which rules build loaded.</summary>
    public static void Report()
    {
        if (reported) return;
        reported = true;
        Debug.Log($"[Week19] DamaRules ready - {Size}x{Size}, mirrored man/Dama/mandatory-capture/chain rules, undoable Apply for search");
    }

    // ---- Queries ------------------------------------------------------------

    public static bool InBounds(int x, int y) => x >= 0 && x < Size && y >= 0 && y < Size;

    public static bool IsDark(int x, int y) => (x + y) % 2 == 1;

    public static int Opponent(int side) => side == 1 ? 2 : 1;

    public static bool IsPromotionRow(int side, int y) => side == 1 ? y == Size - 1 : y == 0;

    public static Cell SideToCell(int side) => side == 1 ? Cell.Player1 : Cell.Player2;

    public static int CellToSide(Cell cell) => cell == Cell.Player1 ? 1 : cell == Cell.Player2 ? 2 : 0;

    public static int CountPieces(Position position, int side)
    {
        Cell me = SideToCell(side);
        int count = 0;

        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
                if (position.cells[x, y] == me) count++;

        return count;
    }

    // ---- Move generation ----------------------------------------------------

    /// <summary>Non-capture destinations: one step for a man, any distance for a Dama.</summary>
    public static void GenerateSimpleMoves(Position position, int x, int y, List<Vector2Int> into)
    {
        into.Clear();

        Cell me = position.cells[x, y];
        if (me == Cell.Empty) return;

        if (position.kings[x, y])
        {
            for (int dx = -1; dx <= 1; dx += 2)
            {
                for (int dy = -1; dy <= 1; dy += 2)
                {
                    int cx = x + dx;
                    int cy = y + dy;

                    while (InBounds(cx, cy) && position.cells[cx, cy] == Cell.Empty)
                    {
                        into.Add(new Vector2Int(cx, cy));
                        cx += dx;
                        cy += dy;
                    }
                }
            }

            return;
        }

        int direction = me == Cell.Player1 ? 1 : -1;
        for (int dx = -1; dx <= 1; dx += 2)
        {
            int stepX = x + dx;
            int stepY = y + direction;
            if (InBounds(stepX, stepY) && position.cells[stepX, stepY] == Cell.Empty)
                into.Add(new Vector2Int(stepX, stepY));
        }
    }

    /// <summary>Capture destinations: men jump one adjacent enemy forward, a Dama jumps at any distance.</summary>
    public static void GenerateCaptureMoves(Position position, int x, int y, List<Vector2Int> into)
    {
        into.Clear();

        Cell me = position.cells[x, y];
        if (me == Cell.Empty) return;

        if (position.kings[x, y])
        {
            for (int dx = -1; dx <= 1; dx += 2)
            {
                for (int dy = -1; dy <= 1; dy += 2)
                {
                    int cx = x + dx;
                    int cy = y + dy;

                    while (InBounds(cx, cy) && position.cells[cx, cy] == Cell.Empty)
                    {
                        cx += dx;
                        cy += dy;
                    }

                    if (!InBounds(cx, cy) || position.cells[cx, cy] == me) continue;

                    int landX = cx + dx;
                    int landY = cy + dy;
                    while (InBounds(landX, landY) && position.cells[landX, landY] == Cell.Empty)
                    {
                        into.Add(new Vector2Int(landX, landY));
                        landX += dx;
                        landY += dy;
                    }
                }
            }

            return;
        }

        int direction = me == Cell.Player1 ? 1 : -1;
        for (int dx = -1; dx <= 1; dx += 2)
        {
            int stepX = x + dx;
            int stepY = y + direction;
            int landX = x + 2 * dx;
            int landY = y + 2 * direction;

            if (!InBounds(landX, landY)) continue;
            if (position.cells[stepX, stepY] == Cell.Empty) continue;
            if (position.cells[stepX, stepY] == me) continue;
            if (position.cells[landX, landY] != Cell.Empty) continue;

            into.Add(new Vector2Int(landX, landY));
        }
    }

    public static bool HasCaptureAt(Position position, int x, int y)
    {
        if (!InBounds(x, y) || position.cells[x, y] == Cell.Empty) return false;

        List<Vector2Int> buffer = Scratch();
        GenerateCaptureMoves(position, x, y, buffer);
        return buffer.Count > 0;
    }

    /// <summary>True when any piece of the side can capture right now: the mandatory-capture test.</summary>
    public static bool PlayerHasCapture(Position position, int side)
    {
        Cell me = SideToCell(side);

        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
                if (position.cells[x, y] == me && HasCaptureAt(position, x, y)) return true;

        return false;
    }

    public static bool HasAnyLegalMove(Position position, int side)
    {
        List<Candidate> moves = ScratchCandidates();
        CollectMoves(position, side, new Vector2Int(-1, -1), moves);
        return moves.Count > 0;
    }

    /// <summary>
    /// Every legal destination for the side, under the mandatory-capture rule. When
    /// <paramref name="forcedFrom"/> names a square, only that piece's captures are returned - that
    /// is how a jump chain keeps the turn on the piece that jumped.
    /// </summary>
    public static void CollectMoves(Position position, int side, Vector2Int forcedFrom, List<Candidate> into)
    {
        into.Clear();

        if (forcedFrom.x >= 0)
        {
            List<Vector2Int> chain = Scratch();
            GenerateCaptureMoves(position, forcedFrom.x, forcedFrom.y, chain);

            for (int i = 0; i < chain.Count; i++)
                into.Add(new Candidate { from = forcedFrom, to = chain[i], capture = true });

            return;
        }

        Cell me = SideToCell(side);
        bool mustCapture = PlayerHasCapture(position, side);
        List<Vector2Int> buffer = Scratch();

        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                if (position.cells[x, y] != me) continue;

                if (mustCapture)
                {
                    GenerateCaptureMoves(position, x, y, buffer);
                    for (int i = 0; i < buffer.Count; i++)
                        into.Add(new Candidate { from = new Vector2Int(x, y), to = buffer[i], capture = true });
                }
                else
                {
                    GenerateSimpleMoves(position, x, y, buffer);
                    for (int i = 0; i < buffer.Count; i++)
                        into.Add(new Candidate { from = new Vector2Int(x, y), to = buffer[i], capture = false });
                }
            }
        }
    }

    // ---- Simulation ---------------------------------------------------------

    /// <summary>
    /// Plays one step on a copied position: removes the jumped piece, moves the piece and promotes
    /// it on the far row. Returns what changed so the search can undo it instead of cloning.
    /// </summary>
    public static Step Apply(Position position, Vector2Int from, Vector2Int to)
    {
        Step step = new Step { from = from, to = to, jumped = new Vector2Int(-1, -1) };

        step.moving = position.cells[from.x, from.y];
        if (step.moving == Cell.Empty) return step;

        step.wasKing = position.kings[from.x, from.y];

        Vector2Int jumped = FindJumpedCell(position, from, to);
        if (jumped.x >= 0)
        {
            step.jumped = jumped;
            step.jumpedWasKing = position.kings[jumped.x, jumped.y];

            position.cells[jumped.x, jumped.y] = Cell.Empty;
            position.kings[jumped.x, jumped.y] = false;
        }

        position.cells[to.x, to.y] = step.moving;
        position.kings[to.x, to.y] = step.wasKing;
        position.cells[from.x, from.y] = Cell.Empty;
        position.kings[from.x, from.y] = false;

        int side = CellToSide(step.moving);
        if (!position.kings[to.x, to.y] && IsPromotionRow(side, to.y))
        {
            position.kings[to.x, to.y] = true;
            step.wasPromotion = true;
        }

        return step;
    }

    /// <summary>Puts a position back exactly as it was before <see cref="Apply"/>.</summary>
    public static void Undo(Position position, Step step)
    {
        position.cells[step.to.x, step.to.y] = Cell.Empty;
        position.kings[step.to.x, step.to.y] = false;

        position.cells[step.from.x, step.from.y] = step.moving;
        position.kings[step.from.x, step.from.y] = step.wasKing;

        if (step.jumped.x < 0) return;

        position.cells[step.jumped.x, step.jumped.y] = SideToCell(Opponent(CellToSide(step.moving)));
        position.kings[step.jumped.x, step.jumped.y] = step.jumpedWasKing;
    }

    /// <summary>The enemy piece between two squares, or (-1,-1) for a plain step or an illegal path.</summary>
    public static Vector2Int FindJumpedCell(Position position, Vector2Int from, Vector2Int to)
    {
        int stepX = to.x > from.x ? 1 : (to.x < from.x ? -1 : 0);
        int stepY = to.y > from.y ? 1 : (to.y < from.y ? -1 : 0);

        if (stepX == 0 || stepY == 0) return new Vector2Int(-1, -1);
        if (Mathf.Abs(to.x - from.x) != Mathf.Abs(to.y - from.y)) return new Vector2Int(-1, -1);

        Vector2Int jumped = new Vector2Int(-1, -1);
        int cx = from.x + stepX;
        int cy = from.y + stepY;

        while (cx != to.x || cy != to.y)
        {
            if (position.cells[cx, cy] != Cell.Empty)
            {
                if (jumped.x >= 0) return new Vector2Int(-1, -1);
                jumped = new Vector2Int(cx, cy);
            }

            cx += stepX;
            cy += stepY;
        }

        return jumped;
    }

    /// <summary>True when the piece that just landed must jump again, which keeps the turn.</summary>
    public static bool ChainContinues(Position position, Step step)
    {
        if (step.jumped.x < 0) return false;
        return HasCaptureAt(position, step.to.x, step.to.y);
    }

    // ---- Scratch buffers ----------------------------------------------------
    // The AI is single-threaded and asks one question at a time, so two buffers, one per kind of
    // question, are enough - and a search that runs thousands of nodes allocates nothing.

    private static readonly List<Vector2Int> scratchCells = new List<Vector2Int>(32);
    private static readonly List<Candidate> scratchMoves = new List<Candidate>(64);

    private static List<Vector2Int> Scratch() => scratchCells;

    private static List<Candidate> ScratchCandidates() => scratchMoves;
}
