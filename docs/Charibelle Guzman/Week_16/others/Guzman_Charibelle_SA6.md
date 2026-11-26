# SA6 - Systems Analysis: Profiling the Game and Choosing What to Optimise

**Game:** Dama
**Student:** Charibelle Guzman
**Section:** BSIT-4
**Course:** MOBILE DEV 002 Advanced Mobile Application Development, Module 5
**Theme:** Philippine Games and Sports
**Week:** 16 - Mobile Game Profiling and Performance Optimization
**Engine:** Unity 6000.6.0f1, Universal Render Pipeline 17.6.0 (2D Renderer), Input System 1.20.0
**Platform range:** Android 8.0 (API 26) and above, ARM64, IL2CPP, `com.defaultcompany.dama`
**Repository:** local Git repository, branch `main`
**Produced:** 2026-11-26

---

## 1. The question this analysis has to answer

The week asks for a performance profile of a mobile game, at least two bottlenecks proven with data,
and optimizations that remove them without changing behaviour. The trap in that instruction is the
word *proven*. It is easy to open the Unity Profiler, look at the tallest bar, and "optimize" it. It
is much harder to show that the bar you chose is the one that costs something on every turn, that
your measurement could actually see the cost, and that the number moved afterwards.

This document is therefore written in the order the work was done, and the order is not negotiable:

1. decide what "fast enough" means for **this** game, and write it down **before** measuring;
2. prove the measuring instrument works before trusting a single number from it;
3. take the baseline;
4. name the bottlenecks from that baseline, with the call graph that makes each one repeat;
5. rank them and choose what to fix, including what to **refuse** to fix;
6. only then write code.

Step 2 is the step most reports skip, and in this project it was the step that mattered most.

## 2. What performance means for Dama

Dama is a two-player, turn-based board game on one device. There is no animation loop, no physics,
no scrolling and no continuous input. The player taps a square; the board updates; the player taps
again. Its cost profile is therefore nothing like an action game's:

| property | consequence for performance work |
|---|---|
| The board is static between taps | frame time is dominated by idle frames, so a 60 fps target buys nothing visible |
| Every turn runs the same three functions | a per-turn allocation is not a spike, it is a leak that runs forever |
| 64 squares and at most 24 pieces, all `SpriteRenderer`s | rendering load is fixed and tiny; draw calls are not the interesting number |
| A restart rebuilds the whole board from scratch | a deliberate user action can afford one hitch - but only one frame's worth |

That reasoning produced four targets, and they were written into
`Evidence/targets.md` **before** the baseline run:

| Id | Target | Value | Why this value |
|---|---|---|---|
| PT-1 | Frame budget | **33.3 ms mean**, p95 under 33.3 ms | a turn-based board game does not need 60 fps; 30 fps is the floor a touch game reads as smooth |
| PT-2 | Managed allocation per turn | **0 B** beyond the HUD strings | a turn changes two pieces; it must not allocate a collection per piece scanned |
| PT-3 | Restart cost | **worst frame under 16.7 ms** | restart is one tap away from a lost match; one 60 fps frame is the acceptable hitch |
| PT-4 | Draw calls | **record, do not reduce** | no `SpriteAtlas` exists and nothing measured a rendering cost; inventing a pass line would be guesswork |

PT-1's 33.3 ms is a **project decision**, not a WCAG or platform number, and it is stated as one.
Its purpose is to make "is the frame fast enough" answerable at all - a target that is never written
down cannot be failed, so it also cannot be met.

## 3. The measurement environment, and what it cannot decide

| Item | Value |
|---|---|
| Machine | AMD Ryzen 5 5600G, 16,276 MB, NVIDIA GeForce GTX 1070 |
| Editor | Unity 6000.6.0f1, Windows Editor target |
| Render pipeline | URP 17.6.0, 2D Renderer |
| Quality level | 5 (Ultra) |
| Game view | 1151 x 584 |
| Scene | `Assets/Scenes/DamaGame.unity`, build index 0 |
| Frame pacing during a run | **vSync 0, `Application.targetFrameRate` -1** (restored afterwards) |
| Domain reload on play-mode entry | **disabled** |
| Save file under test | `C:/Users/Zedrich/AppData/LocalLow/DefaultCompany/dama/Dama/profile.json` |

Free-running the frame is a deliberate choice: a 16.7 ms vsync lock would round every measurement to
the display rate and hide exactly the hitches PT-3 is about. It is also why the frame numbers cannot
be read as "the game runs at 480 fps" - see section 6.

What this environment cannot decide, stated here rather than in a footnote:

- **No handset and no APK.** `adb devices` has no serial, so device performance stays **BLOCKED**,
  exactly as Week 15 left compatibility. Every number in this week's evidence is an Editor number.
- **One renderer, one resolution, one machine.** Nothing here generalises to a Mali or Adreno GPU.
- **No battery, thermal or GPU-time measurement** - none is observable from the Editor.
- **The Editor's own work is inside every frame.** Play mode is the Editor, and the Editor costs
  roughly 2 ms of every frame before the game does anything.

That last point is not a footnote. It is the reason the next section exists.

## 4. Proving the instrument before trusting it

The natural instrument for PT-2 is a byte counter. Five candidates were tested directly in this
project's Editor, each with a deliberate allocation large enough that a working counter could not
miss it:

| candidate | measured behaviour |
|---|---|
| `System.GC.GetAllocatedBytesForCurrentThread()` | returned **0 B** across a deliberate 64 KB allocation (`raw 0 -> 0`). It is a stub on this runtime |
| `System.GC.GetTotalMemory(false)` | moves in 4 KB blocks; read 0 across twelve 200 B allocations |
| `Profiler.GetMonoUsedSizeLong()` | same 4 KB granularity, same 0 |
| `AppDomain.MonitoringIsEnabled = true` | `NotImplementedException` |
| Editor-wide `GC Allocated In Frame` counter | ~210 KB of the **Editor's own** work per frame (tag means: game 211,031 B, probe 216,599 B, move 237,157 B), so a ~1 KB turn is below the noise floor |

An earlier run of the harness used the first candidate and reported "0.0 B per turn" - a **false
pass** against PT-2, produced by an instrument that could not see a 64 KB allocation. That result was
caught before it reached a document, and it is the reason the measurement method is documented as
carefully as the measurement.

The replacement is **amplitude scaling**. The same call is executed *n* times in one frame and *2n*
times in a later frame, the per-frame allocation reading is fitted with a straight line, and:

- the **slope** is bytes per call - the number that matters;
- the **intercept** absorbs the Editor's constant per-frame allocation, so it cancels out of every
  comparison between runs.

Each fit is reported with a standard error and R². A slope that is not separated from zero by two
standard errors is printed as *below the resolution of this method*, never as `0.0`. Three fixtures
are fitted: **A** one `GetSelectableMoves`, **B** one legality scan over all 12 pieces (what
`CheckForWin` does at the end of every turn), and **C** one `RestartGame` including the profile write.

## 5. The baseline

`Tools > Dama > Week 16 > 1. Baseline Profile`, `Evidence/profile_baseline/`, written
**2026-11-26 14:20:15** at commit `f9503e8`. Scenario: profile reset, 3 s warm-up discarded, 32 turns
through the game's own `GetSelectableMoves`/`ExecuteMove`, 3 scripted restarts, 3 s end dwell. The
measured window is 71.31 s / 34,550 frames.

| metric | baseline | target | verdict |
|---|---|---|---|
| frame mean | **2.062 ms** | 33.3 ms | met |
| frame p95 | **2.841 ms** | under 33.3 ms | met |
| frames over 33.3 ms | **0 of 34,550** | 0 | met |
| one `GetSelectableMoves` | **556.7 B** +/- 3.3 (R² 1.00) | - | - |
| one legality scan, 12 pieces (per turn) | **6,975.9 B** +/- 0.1 (R² 1.00) | **0 B** | **FAILED** |
| one restart incl. profile write | 5,701.2 B +/- 145.5 (R² 0.99) | - | - |
| worst restart frame | **5.296 ms** | under 16.7 ms | met |
| draw calls / batches | **NOT MEASURABLE** | record only | recorded as unmeasurable |
| triangles | 306.0 | record only | recorded |
| memory peak | 1674.6 MB | - | recorded |

The baseline was run three times before it was accepted. The first run had no restart cycles at all
(so PT-3 was unmeasurable), the second exposed the false `0.0 B per turn` and triggered the
instrument audit, and the third - the one above - is the run that everything else is compared
against. Iterations that failed to answer the question are part of the method, not hidden from it.

## 6. Reading the baseline: what it does and does not say

**PT-1 is met with an enormous margin** (2.062 ms against 33.3 ms) and **0 frames** over budget. The
honest conclusion is not "we have 16x headroom to spend" - it is that **frame time is not this
game's problem in the Editor at all**, so no optimization this week should be justified by frame
time. Two things make that reading safe:

1. The frame is **free-running** and **vsync-free**, so 2.062 ms is the Editor's real per-frame cost,
   not a rounded 16.7 ms.
2. The idle mean (frames with no move in them) is **2.059 ms** - statistically the same. The game's
   turn work is not visible in the frame at all.

**PT-2 is failed, and by an order of magnitude.** 6,975.9 B per turn against a budget of 0 B. That
number is not a spike: it is the whole-board legality scan that `CheckForWin` runs at the end of
every single turn, and a per-turn allocation is paid again on the next turn, for as long as the
player plays.

**PT-3 is already met** in the Editor: worst restart frame 5.296 ms against 16.7 ms. Saying so is
part of the analysis - a target that is already satisfied does not need an optimization invented for
it, and pretending otherwise would have made the second optimization dishonest.

**PT-4 is unmeasurable, and is recorded as unmeasurable.** The draw-call and batch recorders were
valid (`draws=on batches=on` in the run's log) but every sample read 0 while the scene was visibly
drawing. A zero would have read as a perfect score; it is written as `NOT MEASURABLE` instead.

## 7. Finding 1 - the per-turn cost of deciding who may move

**Evidence:** 556.7 B (one call) and 6,975.9 B (12 pieces), both fitted with R² 1.00, so these are
real costs and not noise.

**Where it comes from.** `GetCaptureMoves` and `GetSimpleMoves` built a **fresh
`List<Vector2Int>`** for every piece they examined. One tap calls `GetSelectableMoves`, which asks
`PlayerHasCapture`, which asks `GetCaptureMoves` - once per piece considered. 556.7 B is what one
piece costs.

**Why it repeats.** The same scan is on four paths:

```
tap ──────────► HandleTap ──► GetSelectableMoves ──► PlayerHasCapture ──► GetCaptureMoves  (per piece)
turn end ─────► EndTurn ──► CheckForWin ──► HasAnyLegalMove ──► PlayerHasCapture (again)
                                                             └──► GetCaptureMoves / GetSimpleMoves (per piece)
capture chain ► ExecuteMove ──► GetCaptureMoves
re-highlight ► RehighlightChain ──► GetCaptureMoves
```

`HasAnyLegalMove` scans all 12 pieces at the end of **every** turn, which is why the whole-board
figure is 12x the single call: it is the same allocation, repeated. That is the 6,975.9 B, and it is
the number PT-2 is measured against.

**Rank: first priority** - the only finding with a failed target, and the only one that repeats on
every turn of every match.

## 8. Finding 2 - restart churn

**Evidence:** 5,701.2 B per restart (R² 0.99) and, in the scenario, worst restart frame 5.296 ms,
mean 3.103 ms against an idle mean of 2.059 ms.

**Where it comes from.** `RestartGame` destroyed 24 piece GameObjects and created 24 new ones, each
with a name formatted from a string interpolation, and `BindLayout` (startup only) resolved 64
squares with a per-cell `Transform.Find` and parsed each piece's name with `name.Split('_')`.

**Why it does not matter to the frame, and still matters.** 5.296 ms is comfortably inside the
16.7 ms budget, so PT-3 is met and this is **not** a frame-time rescue. What remains is churn:
destroying and recreating 24 objects is precisely the kind of work that costs a phone CPU - object
creation, name strings, component registration - while costing almost nothing on a desktop Editor
where the frame is already dominated by Editor overhead.

**Rank: second priority** - real churn on a real user action, but a target already met, so it is
never allowed to claim a frame-time win it cannot demonstrate.

## 9. Finding 3 - what was considered and refused

The obvious rendering optimization for a Unity game is a `SpriteAtlas`. It is **rejected for this
week**, for three reasons that are evidence rather than taste:

1. **There is nothing to merge.** The project has exactly two piece/square sprite textures, both
   serialized directly on `DamaBoard`. There is no sprite sheet and no candidate atlas set.
2. **No measured cost to remove.** Draw calls and batches are `NOT MEASURABLE`, and the frame is
   2.062 ms with 0 frames over budget. An optimization is a response to a measured cost; here there
   is neither a number nor a timeline pressure.
3. **A real, unproven risk.** Atlasing changes material state and import settings across the board
   object, which is a regression risk for Weeks 14 and 15's accessibility and defect evidence, for a
   gain nobody measured.

It is recorded as **accepted as a candidate, deferred**, with the missing measurement named: a
device-side draw-call capture.

## 10. The plan that follows from the analysis

| Order | Change | Target it addresses | Risk and its mitigation |
|---|---|---|---|
| 1 | Reusable buffers in `GetCaptureMoves` / `GetSimpleMoves` / `GetSelectableMoves`; allocation-free `HasCaptureAt` / `HasSimpleMoveAt`; the side-wide capture question asked once in `HasAnyLegalMove` | PT-2 (failed -> met) | the returned list becomes a **shared buffer**. Every current caller consumes it immediately with `Count` or `AddRange` and stores nothing - audited call by call before the change - and the contract is written into the code comment for the next reader |
| 2 | Piece pool in `RestartGame` + captured pieces released to it; prebuilt piece names; crown found by child index; squares bound by child index with a positional check | PT-3 allocation (churn) | 24 objects stay alive for the session instead of being destroyed (memory up, churn down); crowns are kept and toggled instead of recreated; `Transform.Find` stays as the fallback for a reordered hierarchy |
| - | `SpriteAtlas` | none | deferred, reasons in section 9 |
| - | `RefreshUI`'s two HUD strings | none | **explicitly out of scope**, and measured separately, so PT-2's 0 B rule cannot quietly absorb them |

Two optimizations, both separately committed, both re-measured, both followed by a full regression
run. The measurement of what they actually achieved is in `Guzman_Charibelle_AS3.pdf`, and the raw
runs are in `Evidence/metrics.md`.

## 11. What this analysis does not cover

- **Device behaviour.** No APK, no handset, no adb. All numbers are Editor numbers.
- **Rendering cost.** Draw calls, batches and set-pass calls are `NOT MEASURABLE` in this
  environment and no rendering claim is made in either direction.
- **Battery, thermals, GPU time.** Not observable here.
- **Long sessions.** The scenario is ~71 s of measured window; a 20-minute match was not profiled,
  so the pool's session-long memory cost is reasoned about, not measured.
