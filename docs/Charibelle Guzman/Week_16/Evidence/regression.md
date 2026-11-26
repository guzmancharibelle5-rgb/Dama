# Week 16 - regression: did the two optimizations change anything?

**Student:** Charibelle Guzman (BSIT-4)
**Theme:** Philippine Games and Sports
**Game:** Dama
**Run:** 2026-11-26 06:39:37 UTC, `Evidence/test_logs_regression/summary.txt`

An optimization that changes behaviour is not an optimization. Two independent checks were run
after both optimizations landed, and both compare against the numbers Week 15 already committed.

## 1. The 28 functional cases - 27 passed, 0 failed, 1 blocked

`Tools > Dama > Week 16 > 4. Regression Run` re-runs the Week 15 suite unchanged, through
`Assets/Scripts/Editor/Week15Checks.cs`.

| | Week 15 final (committed) | Week 16 regression |
|---|---|---|
| passed | 27 | **27** |
| failed | 0 | **0** |
| blocked | 1 | **1** |
| console errors | 0 | **0** |
| cases | 28 | 28 |

The blocked case is `TC-COMPAT-01` (second device or configuration profile), blocked for the same
recorded reason as in Week 15: no APK was built this week and no Android handset is attached. It is
**blocked, not passed** - the same discipline as every week since 14.

Every case that touches the code the two optimizations changed passes:

| case | what it proves about this week's changes |
|---|---|
| TC-RULE-01 | the mandatory-capture rule still removes the quiet move from the offered list - `GetSelectableMoves` returning the shared empty list is read as "locked out", not as "no moves at all" |
| TC-RULE-02 | a jump lands beyond the enemy and removes it - `ReleasePiece` replaces `Destroy` with identical board state |
| TC-RULE-03 | a capture that lands on another jump keeps the turn and the chain - the chain path still re-reads `GetCaptureMoves` after the buffer was reused |
| TC-RULE-04 / 05 | RED crowns at `y == 7`, BLUE at `y == 0` - `PromoteToDama` now reuses a pooled piece's crown object instead of creating one |
| TC-RULE-06 | a Dama flies any distance and stops before a piece - the flying-king path in `GetSimpleMoves` is intact |
| TC-RULE-07 / 08 / 09 | wins by losing all pieces and by having no legal move - `CheckForWin` -> `HasAnyLegalMove`, the hottest consumer of the optimized buffers, still decides correctly |
| TC-FLOW-06 | a match can be left for the title screen - `ReturnToMenu` -> `RestartGame` runs the pool path |
| TC-BOUND-01 | 100 rapid taps do not crash, change the board or bank a match - the buffer reuse survives hammering |

The detailed evidence is `test_logs_regression/case_log.tsv` (28 rows, one per case) and
`test_logs_regression/console.log`.

**The seam that made this safe:** `Week15Checks` used to write into fixed
`Week_15/Evidence/test_logs_baseline/` and `test_logs_final/` folders. Those are committed historic
records; a Week 16 run writing into them would have destroyed Week 15's evidence. The harness now
carries a `runWeek` field and the run goes to
`Week_16/Evidence/test_logs_regression/`. Verified after the run:
`git status -- docs/Charibelle Guzman/Week_14 docs/Charibelle Guzman/Week_15` reports **no change**.

## 2. The accessibility audit - 105 passed, 0 failed, 4 not measurable

`Tools > Dama > Week 16 > 0. Accessibility Audit` (`A11yAudit.RunWeek16()`) re-runs the five check
kinds (contrast, touch targets, type sizes, non-text contrast, naming) and writes
`Evidence/a11y_week16_after.log`.

| | Week 15 final | Week 16 after both optimizations |
|---|---|---|
| passed | 105 | **105** |
| failed | 0 | **0** |
| warnings | 0 | **0** |
| not measurable | 4 | **4** |

The four not-measurable items are the same four as Week 15 (they concern what a screen reader
announces, which cannot be measured from the Editor). The audit runs in edit mode against the same
scene, so it captures the UI as built, not as a run leaves it.

The file is labelled **AFTER** deliberately: both optimizations were already committed when the
audit ran, so there is no pre-state left to call a baseline. The harness takes an explicit label for
this week instead of applying its file-existence rule, which would have named the first file
"baseline" and misdescribed it.

## 3. Gameplay equivalence inside the profile runs themselves

The regression suite plays scripted positions; the profile runs play a whole 32-turn match. Both
optimized profile runs replayed the baseline's recorded turns and ended on the **same move hash
`0xE432E83D`** with **no replay rejection**, so the same 32 turns produced the same board states on
all three builds. See `metrics.md`.

## What this regression does not prove

- Nothing about a phone or about performance on one - see `method.md`.
- The four not-measurable accessibility checks remain not measured; they are not passes.
- `TC-COMPAT-01` remains **BLOCKED**: no APK, no handset.
