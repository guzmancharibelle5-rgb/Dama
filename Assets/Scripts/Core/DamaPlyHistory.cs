using System.Collections.Generic;
using UnityEngine;

/// <summary>Why a match ended in a draw, or None while it is still being played.</summary>
public enum DamaDrawReason { None = 0, Repetition = 1, NoProgress = 2 }

/// <summary>
/// One completed ply, kept so UNDO can put the board back exactly as it was: the rule arrays, the
/// piece renderers, the pieces that were jumped, whose turn it was, the forced chain and the two
/// draw counters.
///
/// The renderers are stored as references on purpose. A captured piece is not destroyed - it is
/// parked in the board's piece pool - so undoing a capture only has to take that same object back
/// out of the pool, which keeps the board's own object count stable and makes undo exact rather
/// than a rebuild that could drift.
/// </summary>
public sealed class PlySnapshot
{
    public const int BoardCells = 64;

    public readonly int[] cells = new int[BoardCells];          // 0 empty, 1 RED, 2 BLUE
    public readonly bool[] kings = new bool[BoardCells];
    public readonly SpriteRenderer[] renderers = new SpriteRenderer[BoardCells];

    // A forced chain can take more than one man in a single turn, and a turn is one undo, so the
    // captured pieces are lists rather than a single slot.
    public readonly List<GameObject> captured = new List<GameObject>(4);
    public readonly List<Vector2Int> capturedCells = new List<Vector2Int>(4);

    public bool promoted;                                       // the turn crowned a man

    public int player = 1;                                      // the side that made this ply
    public int chainX = -1;                                     // forced chain this ply ended on, if any
    public int chainY = -1;

    public int noCapturePlies;                                  // counter before this ply
    public Vector2Int selected = new Vector2Int(-1, -1);
}

/// <summary>
/// Week 20: the ply stack, the position keys and the two draw counters.
///
/// Two rules, both standard dama:
///   * threefold repetition - the same position with the same side to move three times;
///   * no progress - 50 plies in a row with neither a capture nor a promotion.
///
/// The key is an FNV-1a hash of the 64 squares, their crown flags and the side to move. It is a
/// hash, not a replay of the moves: a collision would need two different positions to hash alike,
/// and the cost of checking is a 64-step loop per ply instead of storing a move list.
///
/// This class only counts. It never decides that a match is over - DamaBoard asks Verdict and acts.
/// </summary>
public sealed class DamaPlyHistory
{
    /// <summary>Plies without a capture or a promotion before the match is declared no-progress.</summary>
    public const int NoProgressLimit = 50;

    /// <summary>How many times the same position must appear before it is a repetition draw.</summary>
    public const int RepetitionLimit = 3;

    private const ulong FnvOffset = 14695981039346656037UL;
    private const ulong FnvPrime = 1099511628211UL;

    private readonly List<PlySnapshot> plies = new List<PlySnapshot>(64);

    // One key per position the board has stood on, oldest first. It holds one more entry than
    // plies, because the position a match starts from is recorded as well: a shuffle that walks
    // back into the opening is otherwise counted one appearance short.
    private readonly List<ulong> positions = new List<ulong>(65);

    private int noCapturePlies;

    /// <summary>How many plies can still be undone.</summary>
    public int Count => plies.Count;

    /// <summary>Plies since the last capture or promotion.</summary>
    public int NoCapturePlies => noCapturePlies;

    /// <summary>The key of the position the board is standing on.</summary>
    public ulong LastKey => positions.Count > 0 ? positions[positions.Count - 1] : 0UL;

    public void Clear()
    {
        plies.Clear();
        positions.Clear();
        noCapturePlies = 0;

        Debug.Log("[Week20] Ply history cleared; draw counters at 0.");
    }

    /// <summary>
    /// Empties the history and records the position the new match starts from, so a repetition that
    /// returns to the opening counts that first appearance too.
    /// </summary>
    public void Reset(ulong startKey)
    {
        Clear();
        positions.Add(startKey);

        Debug.Log($"[Week20] Draw watch started on position {startKey:X16}.");
    }

    /// <summary>
    /// Records a completed turn: the snapshot that can undo it, the key of the position the move
    /// left the board in, and whether that turn captured or crowned anything.
    /// </summary>
    public void Push(PlySnapshot snapshot, ulong positionKey, bool captured, bool promoted)
    {
        snapshot.noCapturePlies = noCapturePlies;

        plies.Add(snapshot);
        positions.Add(positionKey);

        noCapturePlies = (captured || promoted) ? 0 : noCapturePlies + 1;

        int nextSide = snapshot.player == 1 ? 2 : 1;
        int repeats = Repeats(positionKey);
        if (repeats >= RepetitionLimit)
        {
            Debug.Log($"[Week20] Position {positionKey:X16} has now appeared {repeats} times with " +
                      $"{(nextSide == 1 ? "RED" : "BLUE")} to move.");
        }
        else if (noCapturePlies == NoProgressLimit)
        {
            Debug.Log($"[Week20] {noCapturePlies} plies without a capture or a promotion.");
        }
    }

    /// <summary>Takes back the newest ply and restores the counters it changed.</summary>
    public bool TryPop(out PlySnapshot snapshot)
    {
        if (plies.Count == 0)
        {
            snapshot = null;
            return false;
        }

        int last = plies.Count - 1;
        snapshot = plies[last];
        plies.RemoveAt(last);

        // One key per ply plus the opening one: dropping the plies must drop their keys with them.
        int keep = plies.Count + 1;
        if (positions.Count > keep) positions.RemoveRange(keep, positions.Count - keep);

        noCapturePlies = snapshot.noCapturePlies;

        return true;
    }

    /// <summary>The draw this position has earned, with the number behind it for the summary line.</summary>
    public DamaDrawReason Verdict(out int evidence)
    {
        evidence = 0;
        if (positions.Count == 0) return DamaDrawReason.None;

        int repeats = Repeats(LastKey);
        if (repeats >= RepetitionLimit)
        {
            evidence = repeats;
            return DamaDrawReason.Repetition;
        }

        if (noCapturePlies >= NoProgressLimit)
        {
            evidence = noCapturePlies;
            return DamaDrawReason.NoProgress;
        }

        return DamaDrawReason.None;
    }

    /// <summary>How many times a key appears in the recorded history.</summary>
    public int Repeats(ulong key)
    {
        int count = 0;
        for (int i = 0; i < positions.Count; i++)
            if (positions[i] == key) count++;

        return count;
    }

    /// <summary>The FNV-1a key of a position and the side to move.</summary>
    public static ulong Key(int[] cells, bool[] kings, int sideToMove)
    {
        ulong hash = FnvOffset;

        for (int i = 0; i < PlySnapshot.BoardCells; i++)
        {
            // 1..6: empty, RED, BLUE, then the same three crowned. The +1 keeps an empty square
            // from contributing nothing, which would make two boards with different piece counts
            // hash alike.
            int code = cells[i] * 2 + (kings[i] ? 1 : 0) + 1;
            hash ^= (ulong)code;
            hash *= FnvPrime;
        }

        hash ^= (ulong)(sideToMove + 1);
        hash *= FnvPrime;

        return hash;
    }
}
