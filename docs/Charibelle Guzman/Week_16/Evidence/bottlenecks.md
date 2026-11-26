# Week 16 - bottlenecks found in the baseline

**Student:** Charibelle Guzman (BSIT-4)
**Theme:** Philippine Games and Sports
**Game:** Dama
**Source run:** `Evidence/profile_baseline/summary.txt` + `frames.csv` (`2026-11-26 14:20:15`)
**Written:** 2026-11-26, after the baseline, before any optimization

Every number below is copied from the baseline run, not estimated. Where a metric could not be
taken it says `NOT MEASURABLE` instead of 0.

## Note on the tool used to read the run

`get_worst_cpu_frames` and `get_worst_gc_frames` were both queried against this run and both
answered `No valid frame data found in profiler` - the Editor's in-memory profiler buffer is not
retained after the run finishes, so there are no captured frames to read back afterwards. The
harness therefore recorded the same information itself, per frame, into
`Evidence/profile_baseline/frames.csv` (34,550 rows, one row per frame, 9 columns) and printed the
fits into `summary.txt`. The findings below come from that CSV. This is a limitation of the
evidence, and it is stated rather than papered over: the frames were recorded but cannot be
re-opened as profiler frames.

## Finding 1 - the per-turn cost of deciding who may move (target PT-2, FAILED)

| measurement | value | source |
|---|---|---|
| one `GetSelectableMoves` call | **556.7 B** +/- 3.3 B (R2 1.00) | summary, fixture A |
| one legality scan over all 12 pieces | **6,975.9 B** +/- 0.1 B (R2 1.00) | summary, fixture B |
| target | **0 B** of game allocation per turn | `targets.md` PT-2 |

`DamaBoard.GetSelectableMoves` asks `PlayerHasCapture` which asks `GetCaptureMoves`, and
`GetCaptureMoves` / `GetSimpleMoves` each built a **fresh `List<Vector2Int>`** for every piece
scanned. `CheckForWin` runs `HasAnyLegalMove` - the same scan over all 12 pieces - at the end of
*every* turn. That is why the whole-board scan is 12x the single call: it is the same allocation,
repeated per piece. 6,975.9 B per turn against a budget of 0 B is not a tight margin, it is an
order-of-magnitude miss on the one rule the target set.

This is the game's real hot path, not a guess: it is on the tap handler (`HandleTap`), on every
turn end (`EndTurn` -> `CheckForWin` -> `HasAnyLegalMove`) and again on
`RehighlightChain` while a capture chain is being played out.

## Finding 2 - restart churn (target PT-3 met, allocation not)

| measurement | value | source |
|---|---|---|
| one `RestartGame`, profile write included | **5,701.2 B** +/- 145.5 B (R2 0.99) | summary, fixture C |
| its fitted frame cost | 0.716 ms +/- 0.290 ms (**R2 0.43** - weak) | summary, fixture C |
| worst restart frame in the scenario | **5.296 ms** | summary, RESTART block |
| mean restart frame | 3.103 ms (idle mean 2.059 ms) | summary, RESTART block |
| target | worst frame **under 16.7 ms** | `targets.md` PT-3 |

**PT-3 is already met** - 5.296 ms against a 16.7 ms budget, a 1.044 ms hitch over the idle frame.
Saying so is the honest finding; a target that is already satisfied does not need an optimization
invented for it.

What the restart *does* cost is churn: `RestartGame` destroys 24 piece GameObjects and creates 24
new ones, and `BindLayout` resolves 64 squares with a per-cell `Transform.Find` plus a
`name.Split('_')` parse for each piece. 5.7 KB of allocation per restart is small next to a
match, but it is repeated object destruction, which is the part that will cost time on a phone's
CPU rather than on this machine's. The **R2 0.43** fit is the reason the frame-cost figure is
quoted with its weakness attached instead of being presented as a clean 0.716 ms: the amplitude
fit is reliable for bytes (R2 0.99/1.00) and unreliable for milliseconds, because the Editor's
own frame time moves by more than the restart costs. The in-scenario worst frame (5.296 ms) is
the better evidence and it is what the comparison will use.

## Finding 3 - what the frame time actually says (target PT-1, MET)

| measurement | value |
|---|---|
| mean frame | **2.062 ms** |
| p95 | **2.841 ms** |
| max | 13.397 ms |
| frames over 33.3 ms | **0 of 34,550** |
| target | 33.3 ms mean, p95 under 33.3 ms (PT-1) |

**PT-1 is met with a wide margin**, so frame time is not the week's bottleneck and does not get an
optimization. Measuring it anyway is the point: it rules the whole rendering side out of the
work, which is what allows the two optimizations to be about allocations and churn rather than
guesses at draw calls.

## Rejected candidate - sprite atlas (evaluated, not done)

The obvious rendering optimization for Unity is to put the board's sprites in a `SpriteAtlas` and
cut draw calls. It is **rejected for this week**, for three recorded reasons:

1. **There is no atlas and nothing to merge.** The project has exactly two piece/square sprite
   textures, both serialized directly on `DamaBoard`. There is no sprite-sheet to slice and no
   candidate set of atlases to master/variant.
2. **The evidence does not support it.** Draw calls and batches both read
   `NOT MEASURABLE (recorder valid but every sample read 0 while the scene was drawing)` in every
   run, and the frame is 2.062 ms mean against a 33.3 ms budget with **0** frames over budget. An
   optimization with no measured cost to remove would be a change made on faith, and PT-4 says
   *record, do not reduce this week*.
3. **The cost would be real and the gain unproven.** 64 squares + up to 25 pieces are separate
   `SpriteRenderer`s; atlasing them changes material state and import settings, which is a
   regression risk for Weeks 14-15's accessibility and defect evidence to carry for a number
   nobody measured.

It is recorded as `ACCEPTED as a candidate, DEFERRED` in the report, with the measurement that
would justify it (a device-side draw-call capture) named as what is missing.

## Optimization order, and why this order

1. **Optimization 1 - allocation-free move generation** (`DamaBoard.GetCaptureMoves`,
   `GetSimpleMoves`, `GetSelectableMoves`, `PlayerHasCapture`, `HasAnyLegalMove`). First because it
   is the only finding with a **failed target** (PT-2, 6,975.9 B against 0 B) and the only one that
   repeats on every single turn, so it is the largest total cost across a match.
2. **Optimization 2 - restart churn** (piece pool, cached square/renderer binding). Second because
   PT-3 is already met: this is churn reduction and a phone-facing improvement, not a rescue of a
   failing budget. Doing it second keeps the two commits separable, so if the second one regresses
   anything the first one's win is already banked.

Nothing else is scheduled. `RefreshUI`'s two interpolated HUD strings are a known, expected
allocation; they are measured separately and are explicitly out of scope, so that the PT-2 rule
does not quietly absorb them.
