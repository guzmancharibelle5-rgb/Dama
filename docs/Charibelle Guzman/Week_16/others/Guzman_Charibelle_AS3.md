# AS3 - Performance Change Log, Measured Results and Regression Record

**Game:** Dama
**Student:** Charibelle Guzman
**Section:** BSIT-4
**Course:** MOBILE DEV 002 Advanced Mobile Application Development, Module 5
**Theme:** Philippine Games and Sports
**Week:** 16 - Mobile Game Profiling and Performance Optimization
**Build:** `dama-editor-w16-opt2` - scene `DamaGame.unity` at commit `3b396b9`, executed in the Unity Editor in play mode
**Platform range:** Android 8.0 (API 26) and above, ARM64, IL2CPP, `com.defaultcompany.dama`
**Engine:** Unity 6000.6.0f1, Universal Render Pipeline 17.6.0 (2D Renderer), C#
**Repository:** local Git repository, branch `main`
**Components under test:** `Assets/Scripts/DamaBoard.cs`, `Assets/Scripts/Editor/Week16Profile.cs`,
`Assets/Scripts/Editor/Week15Checks.cs`, `Assets/Scripts/Editor/A11yAudit.cs`, `Assets/Scenes/DamaGame.unity`
**Produced:** 2026-11-26

---

## 1. The build, and the four artefacts that judge it

This document records two code changes and what they measurably did. Every claim here is a cell
copied from a generated file, and the files are named so the claim can be checked.

| | |
|---|---|
| Build identifier | `dama-editor-w16-opt2` |
| Source revision | `3b396b9` ("Week 16 optimization 2: pooled pieces, cached square binding, regression seam") |
| Baseline revision | `f9503e8` ("Week 16: profiling harness and measured baseline") |
| Optimization 1 revision | `d6c4ba7` ("Week 16 optimization 1: allocation-free move generation") |
| Scene | `Assets/Scenes/DamaGame.unity`, build index 0 |
| Execution mode | Unity Editor 6000.6.0f1, play mode, game view 1151 x 584, quality 5 (Ultra) |
| Frame pacing during a run | vSync forced to 0, `Application.targetFrameRate` to -1, both restored afterwards |
| Save location | `C:/Users/Zedrich/AppData/LocalLow/DefaultCompany/dama/Dama/profile.json` |

| Artefact | Written | Result |
|---|---|---|
| `Evidence/profile_baseline/` | 2026-11-26 14:20:15 | frame mean 2.062 ms; legality scan **6,975.9 B**; restart 5,701.2 B |
| `Evidence/profile_after_alloc/` | 2026-11-26 14:27:06 | frame mean 2.191 ms; legality scan **below resolution**; restart 5,596.2 B |
| `Evidence/profile_after_pool/` | 2026-11-26 14:36:21 | frame mean 2.181 ms; legality scan **0.1 B +/- 0.1**; restart **below resolution** |
| `Evidence/test_logs_regression/` | 2026-11-26 06:39 UTC | **27 passed, 0 failed, 1 blocked**, 0 console errors |
| `Evidence/a11y_week16_after.log` | 2026-11-26 14:38 | **105 passed, 0 failed**, 0 warnings, 4 not measurable |

All three profile runs used the same scenario, the same scene and the same 32 turns replayed from
`Evidence/moves.txt`, and all three ended on move hash **`0xE432E83D`** with no replay rejection.
That is the condition that makes the comparison legitimate: the same game happened three times.

## 2. Change 1 - allocation-free move generation

**Revision:** `d6c4ba7`. **File:** `Assets/Scripts/DamaBoard.cs`.

### What was wrong

`GetCaptureMoves` and `GetSimpleMoves` each built a fresh `List<Vector2Int>` on every call, and
`GetSelectableMoves` built another for the mandatory-capture refusal. Because `CheckForWin` calls
`HasAnyLegalMove` at the end of **every** turn, and `HasAnyLegalMove` asks those generators about all
12 pieces, the game allocated a collection per piece per turn, forever.

Baseline measurement: **556.7 B** for one `GetSelectableMoves`, **6,975.9 B** for one whole-board
legality scan (R² 1.00 on both), against a project target of **0 B** per turn.

### What changed

- **One reusable buffer per generator.** `captureBuffer` and `simpleBuffer` are instance
  `List<Vector2Int>(32)` fields; the generators write into them and return them.
- **A shared empty result** for the mandatory-capture refusal, so refusing costs nothing.
- **`HasCaptureAt(x, y)` / `HasSimpleMoveAt(x, y)`** - the probe form of the same question, answered
  with `.Count > 0` instead of a list built for a caller that only wanted a yes or a no. These back
  `PlayerHasCapture` and `HasAnyLegalMove`.
- **`HasAnyLegalMove` asks the side-wide question once.** `bool mustCapture = PlayerHasCapture(me)`
  is hoisted out of the loop instead of being re-derived per piece.
- A one-time `[Perf]` log states the new contract in the console, so the behaviour is visible in a
  build log and not only in a source comment.

### The contract this creates, and how it was made safe

A generator now returns a **shared buffer, valid only until that generator is next called**. A caller
that stored the list would silently see it change. Every call site was audited before the change:

| caller | what it does with the list |
|---|---|
| `HandleTap` | `selectedMoves.AddRange(moves)` immediately |
| `ExecuteMove` (capture chain) | reads `continuations.Count`, then `AddRange` into `selectedMoves` |
| `RehighlightChain` | `selectedMoves.AddRange(...)` |
| `HasAnyLegalMove` / `PlayerHasCapture` | reads `.Count` through the probe helpers |

None stores it, and the nesting is strictly sequential (`GetSelectableMoves` -> `GetCaptureMoves` ->
`PlayerHasCapture` -> `GetCaptureMoves`), so no call can invalidate a list another call is reading.
The contract is written into the field comment for whoever touches this next.

### Measured result

| fixture | before | after | reading |
|---|---|---|---|
| one `GetSelectableMoves` | 556.7 B +/- 3.3 (R² 1.00) | **-6.8 B +/- 4.2** (R² 0.25) | below the resolution of the method - and 556.7 B removed |
| one whole-board legality scan (per turn) | 6,975.9 B +/- 0.1 (R² 1.00) | **-0.1 B +/- 0.1** (R² 0.25) | **PT-2 met** |

The R² falling to 0.25 is the signature of success, not of failure: a fit through a real per-call
cost has R² near 1; a fit through noise has R² near 0. The instrument still works - the intercept and
the Editor-wide column still move - there is simply nothing left for it to find.

## 3. Change 2 - piece pooling, cached binding, and no strings on the hot path

**Revision:** `3b396b9`. **File:** `Assets/Scripts/DamaBoard.cs` (plus harness seams, section 4).

### What was wrong

`RestartGame` destroyed 24 piece GameObjects and created 24 new ones, each with an interpolated name
and a freshly added `SpriteRenderer`. `ExecuteMove` renamed a piece with another interpolation on
every move. `PromoteToDama` located a crown with `Transform.Find`, and `BindLayout` resolved 64
squares with a per-cell `Transform.Find` plus a `name.Split('_')` per piece.

Baseline measurement: **5,701.2 B** per restart (R² 0.99); worst restart frame 5.296 ms.

### What changed

- **A piece pool.** A piece that leaves the board - captured, or cleared by a restart - is parked
  under an inactive `PiecePool` root and **rented again**. A restart releases what is on the board
  and rents 24 objects instead of destroying and creating them. Because the pool root is inactive
  and is not under `Pieces`, nothing that iterates the board's pieces ever sees a parked one.
- **Prebuilt piece names.** `pieceNames[side, cell]` is built once at type initialisation, so neither
  a rent nor a move formats a string.
- **Crowns found by child index** (`CrownChild`) instead of `Transform.Find`, and kept on the pooled
  object: promoting a rented piece only re-activates its crown instead of creating one.
- **`BindLayout` reads its squares by child index**, `x * Size + y`, with a **positional check**
  (`IsSquareAt`); `Transform.Find` remains as the fallback for a hierarchy that was hand-reordered.
  `isKing` now comes from a crown's active state instead of a name lookup.
- A one-time `[Perf]` log records how many pieces were parked the first time the pool path ran.

### Measured result

| metric | before | after | reading |
|---|---|---|---|
| one restart incl. profile write | 5,596.2 B +/- 109.4 (R² 1.00) | **-1,665.5 B +/- 1,704.1** (R² 0.11) | indistinguishable from 0 B; **at most ~1.7 KB at two standard errors**, at least a **69%** reduction |
| worst restart frame | 5.296 ms (baseline) | **5.154 ms** | under the 16.7 ms budget; **not** claimed as an improvement |
| mean restart frame | 3.103 ms | 3.668 ms | within the noise of nine samples per run |

The frame figure is deliberately **not** claimed as a win. Nine samples (3 restarts x 3 frames) per
run, with worst-frame values of 5.296 / 8.099 / 5.154 ms across the three runs, cannot separate the
effect from the noise. What the bytes can show, they show; what they cannot, this document says.

## 4. The harness seams this week needed

Two Week 15 harnesses had to be made safe to re-run without damaging committed evidence:

| file | seam | why it was mandatory |
|---|---|---|
| `Week15Checks.cs` | new `runWeek` field; `Tools/Dama/Week 16/4. Regression Run` writes `Week_16/Evidence/test_logs_regression/`; the summary title derives from the week | the harness wrote into fixed `Week_15/Evidence/test_logs_baseline|final/`. A Week 16 run there would have **overwritten Week 15's committed evidence** |
| `A11yAudit.cs` | `RunWeek16()` writes `a11y_week16_after.log`, with the **after** label passed explicitly | both optimizations were already committed, so there is no pre-state. The file-existence rule would have called the first file "baseline" and misdescribed it |

Verified after the runs: `git status -- docs/Charibelle Guzman/Week_14 docs/Charibelle Guzman/Week_15`
reports **no change**.

## 5. Trade-offs, stated

| trade-off | cost | benefit | decision |
|---|---|---|---|
| Shared move buffers | a caller must not retain a generator's list | 6,975.9 B per turn -> 0 B | accepted; every caller audited, contract documented in code |
| Piece pool | 24 piece GameObjects stay alive for the session instead of being destroyed; captured pieces are retained too | restart allocation 5,596 B -> at most ~1.7 KB; no Destroy/new-GameObject churn on a user action | accepted; memory cost is bounded and known |
| Crowns kept, not recreated | a pooled piece carries an inactive child | promotion stops creating an object mid-match | accepted; the crown is toggled and found by index |
| `BindLayout` by child index | the fast path depends on `BuildLayout`'s creation order | 64 `Transform.Find` calls removed from a bind | accepted with a positional check and a `Find` fallback |
| `RefreshUI` strings untouched | HUD still allocates two small strings per refresh | scope stays honest - PT-2's 0 B rule is about move generation | **out of scope, measured separately** |
| `SpriteAtlas` | - | - | **deferred**, reasons in SA6 section 9 |

## 6. Target scorecard

| Id | Target | Baseline | Final | Status |
|---|---|---|---|---|
| PT-1 | 33.3 ms mean, p95 under 33.3 ms | 2.062 ms mean, p95 2.841, 0/34,550 over | 2.181 ms mean, p95 3.097, 0/32,681 over | **MET** in all runs |
| PT-2 | 0 B per turn beyond the HUD strings | **6,975.9 B** | **0.1 B +/- 0.1** | **FAILED -> MET** |
| PT-3 | worst restart frame under 16.7 ms | 5.296 ms | 5.154 ms | **MET** in all runs |
| PT-4 | record draw calls, do not reduce | NOT MEASURABLE | NOT MEASURABLE | **RECORDED as unmeasurable** |
| - | restart allocation (churn, not a target) | 5,701.2 B | at most ~1.7 KB at 2 sigma | **improved** |

Against the delivery target stated in `Evidence/targets.md`, which is the module's own completion
checkpoint: one target - the allocation budget - went from failed to met, and the other two were
already met and stayed met.

An honest note on frame time: mean frame is 2.062 ms at baseline and 2.181 ms at the end, which is
**+0.119 ms (+5.8%)**. That is not a regression from this work. The **idle** mean moved by almost the
same amount (2.059 -> 2.172 ms), and idle frames execute no move generation at all; a change to move
generation cannot move the idle frame, while a different Editor session can. That is why the
allocation fixture, not the frame mean, is the decision metric for this week (see `Evidence/method.md`).

## 7. Regression - behaviour did not change

| check | Week 15 (committed) | Week 16 after both changes |
|---|---|---|
| 28 functional cases | 27 passed, 0 failed, 1 blocked | **27 passed, 0 failed, 1 blocked** |
| console errors | 0 | **0** |
| accessibility audit | 105 passed, 0 failed, 4 not measurable | **105 passed, 0 failed, 4 not measurable** |
| move hash over 32 replayed turns | `0xE432E83D` | **`0xE432E83D`** |
| replay rejections | - | **0** |
| `Week_14` / `Week_15` evidence | - | **unchanged** (`git status` clean) |

The blocked case is `TC-COMPAT-01` (second device or configuration profile) - **blocked, not
passed**: no APK was built this week and no Android handset is attached. The four not-measurable
accessibility items are the same four as Week 15 and are not passes either.

Full detail: `Evidence/regression.md`, `Evidence/test_logs_regression/case_log.tsv`,
`Evidence/a11y_week16_after.log`.

## 8. What this milestone does not prove

- **Nothing about a phone.** Editor play mode, one machine, one resolution, no APK, `adb` has no
  device. Device performance and compatibility stay **BLOCKED**.
- **No rendering claim.** Draw calls and batches read 0 in every run while the scene was drawing, so
  they are recorded as `NOT MEASURABLE` - not as an improvement and not as a problem.
- **No battery, thermal or GPU-time claim.**
- **The restart frame time is not claimed as improved**, only as still passing: nine samples per run
  cannot support more.
- **Long sessions were not profiled**; the pool's session-long memory cost is reasoned about, not
  measured.

## 9. Where everything is

| Question | File |
|---|---|
| What was measured, how, and what the instrument can see | `Evidence/method.md` |
| The three runs, side by side, with raw values | `Evidence/metrics.md` |
| The analysis behind the choices, and what was refused | `Guzman_Charibelle_SA6.pdf` |
| Targets written before measuring | `Evidence/targets.md` |
| Bottleneck findings from the baseline | `Evidence/bottlenecks.md` |
| Regression detail | `Evidence/regression.md` |
| Raw baseline run | `Evidence/profile_baseline/summary.txt`, `frames.csv` |
| Raw after-opt-1 run | `Evidence/profile_after_alloc/summary.txt`, `frames.csv` |
| Raw after-opt-2 run | `Evidence/profile_after_pool/summary.txt`, `frames.csv` |
| The turn list every run replays | `Evidence/moves.txt` |
| Build and commit commands | `Evidence/build_and_commit.txt` |
| How to reproduce all of it | `Evidence/HOW_TO_RERUN.txt` |

## 10. Assistance, and how each item was checked

Code, document structure and phrasing were produced with AI assistance and then read, run and
corrected against the actual build. The rule for this week is that no number in this document was
typed from memory or from a conversation:

| Item | How it was checked |
|---|---|
| every baseline, after-opt-1 and after-opt-2 figure | copied from `Evidence/profile_*/summary.txt`, which the harness wrote; each one re-read against the file before printing |
| the allocation method (amplitude scaling) | the counters that failed were tested directly in the Editor and their failures are recorded with the readings, not summarised |
| the move hash and replay result | read from the `SCENARIO` block of each run's `summary.txt`; the console was checked for replay warnings |
| the 28 regression cases | `Evidence/test_logs_regression/case_log.tsv`, generated by the harness |
| the accessibility counts | `Evidence/a11y_week16_after.log`, generated by `A11yAudit` |
| the commit ids | `git log` in this repository |
| the claims that were **removed** | frame-time improvement and any rendering claim were dropped when the evidence could not support them; they are absent on purpose, and section 8 names why |
