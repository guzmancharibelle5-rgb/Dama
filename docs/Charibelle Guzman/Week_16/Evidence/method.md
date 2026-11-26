# Week 16 - method: how the numbers were produced

**Student:** Charibelle Guzman (BSIT-4)
**Theme:** Philippine Games and Sports
**Game:** Dama
**Harness:** `Assets/Scripts/Editor/Week16Profile.cs`

A number nobody can re-derive is not evidence. This file states exactly what was run, what was
measured, how, and what could not be measured - so that the numbers in `metrics.md` can be checked
or challenged on their method rather than taken on trust.

## The scenario is the experiment

Every run executes the same script, in the same order, in `Assets/Scenes/DamaGame.unity`:

1. `PlayerProfile.UseDefaultDataDirectory()`, the profile reset, `StartGame()` - the real save file
   at `C:/Users/Zedrich/AppData/LocalLow/DefaultCompany/dama/Dama/profile.json` is used and reset,
   so the run does touch the local record. That is disclosed in the summary header of every run.
2. A **3 s warm-up, discarded** (1367 / 1380 / 1333 frames across the three runs) - the first frames
   of a scene load are not the game.
3. The **allocation fixture**: 30 sample frames (see below).
4. **32 turns**, one every ~1.85 s, each chosen by the game's own `GetSelectableMoves` and executed
   through the game's own `ExecuteMove`, followed by the game's own `EndTurn`. Nothing is poked into
   the board directly: the harness drives the same entry points a finger does (through
   `ExecuteMove`, not through a simulated tap, because the tap path needs a real screen point).
5. **3 scripted RESTARTs**, with the three frames after each one tagged and recorded separately.
6. A **3 s dwell** on the end screen, so a run that ends in a win still produces a complete frame
   history.

Total measured window: 71.31 / 71.33 / 71.34 s, 34,550 / 32,516 / 32,681 frames. The scenario is
hard-capped at 240 s so a broken run cannot hang the Editor.

**The baseline records its moves to `Evidence/moves.txt`** (32 turns); the two later runs **replay**
that file instead of choosing their own moves. Each replayed turn is re-checked against the live
board with the game's own legality rules; a turn that is no longer legal logs a warning and counts a
replay miss, and the run continues by choosing a fresh move. Neither optimized run logged a warning,
and all three runs ended with the same move hash, so the three runs played the same game.

## Conditions, recorded in every summary header

Unity 6000.6.0f1 (WindowsEditor), URP, NVIDIA GTX 1070, AMD Ryzen 5 5600G, 16276 MB, game view
1151x584, quality level 5 (Ultra). **vSync is forced to 0 and `Application.targetFrameRate` to -1
for the duration of the run and restored afterwards** - a 16.7 ms vsync lock hides exactly the
hitches this week is looking for. The Profiler is forced on for the run and restored afterwards.

A run under different conditions is not comparable, and the summary says so rather than averaging it
in.

## Why allocation needed a fixture

The obvious instrument - `System.GC.GetAllocatedBytesForCurrentThread()` - **does not work on this
runtime**. It was tested before being trusted: a deliberate 64 KB allocation moved it by **0 B**
(`raw 0 -> 0`). The alternatives were tested too and all failed the same way:

| candidate | measured behaviour |
|---|---|
| `GC.GetAllocatedBytesForCurrentThread()` | constant 0 B, blind to a 64 KB allocation |
| `GC.GetTotalMemory(false)` | moves in 4 KB blocks; 0 B across twelve 200 B allocations |
| `Profiler.GetMonoUsedSizeLong()` | same 4 KB granularity, 0 B across the same run |
| `AppDomain.MonitoringIsEnabled = true` | `NotImplementedException` |
| Editor-wide `GC Allocated In Frame` counter | ~210 KB per frame of the *Editor's* own work (tag means: game 211,031 B, probe 216,599 B, move 237,157 B) - a ~1 KB turn is under the noise floor |

So the harness measures allocation by **amplitude scaling**. The same call is executed n times in
one frame and 2n times in a later frame, the per-frame `GC.Alloc` reading is fitted with a straight
line, and:

- the **slope** is the bytes per call - the number that matters;
- the **intercept** absorbs the Editor's own per-frame allocation, which is large, constant, and
  therefore cancels out of every comparison between runs.

Each fit is reported with its **standard error and R2**, and a slope not separated from zero by two
standard errors is printed as *below the resolution of this method ... consistent with 0 B* rather
than as a suspiciously perfect 0. Three fixtures are run: **A** one `GetSelectableMoves`, **B** one
legality scan over all 12 pieces (the work `CheckForWin` does at the end of every turn), **C** one
`RestartGame` including the profile write, at levels n and 2n, five repeats per level.

This is the single most important methodological point of the week: the instrument was **disproven
first and replaced**, not assumed. An earlier attempt reported "0.0 B per turn" from a counter that
could not see a 64 KB allocation - a false pass - and the numbers in this report exist because that
was caught.

## Sampling discipline

- Frame time is a `Stopwatch` delta between frame boundaries and never depends on the profiler, so
  it survives every counter being unavailable.
- Nothing in the per-frame path formats a string or boxes a value. Values land in preallocated
  arrays (`rowMs`, `rowGc`, `rowMem`, `rowDraws`, `rowBatches`, `rowSetPass`, `rowTris`,
  `rowSeconds`, `rowTag`, capacity 120,000) and the CSV is formatted once, at the end.
- The game's `GetSelectableMoves` and `ExecuteMove` are bound once as **strongly typed delegates**
  (`Func<int,int,List<Vector2Int>>`, `Action<Vector2Int,Vector2Int>`) rather than invoked through
  `MethodInfo.Invoke`, so the harness's own reflection does not allocate on the path being measured.
- Frames are **tagged** (`warm-up 1`, `game 2`, `probe 3`, `move 4`, `restart 5`, `end 6`, `fixture
  7`) and warm-up, probe and fixture frames are excluded from every turn metric.
- A counter that is unavailable, or that reads 0 for every frame while the scene is visibly drawing,
  is reported as **NOT MEASURABLE**, never as 0. A zero would read as a perfect score.

## What is measured, and what is not

| measured | source |
|---|---|
| frame time (mean, p95, max, idle mean, frames over budget) | `Stopwatch` |
| bytes per call for the three fixtures | amplitude fit on `ProfilerRecorder GC Allocated In Frame` |
| restart frame cost | the three frames after each RESTART call |
| triangles | `ProfilerRecorder 'Triangles Count'` |
| memory | `Profiler.GetTotalAllocatedMemoryLong()` |
| gameplay equivalence | move hash + replay legality check |

| NOT measured, therefore not claimed | why |
|---|---|
| draw calls, batches, set pass calls | the recorders were valid but every sample read 0 while the scene was drawing |
| anything about a phone | Editor play mode only; no APK, `adb` has no device (device/compatibility stays BLOCKED) |
| battery, thermals, GPU time | not observable from the Editor |
| GPU-bound frame cost | the frame is CPU/Editor-bound at ~2 ms, far under the 33.3 ms budget |

## Known limits of this evidence

- The Editor adds roughly 2 ms of its own work to every frame. That floor is why frame *mean*
  differences between runs are not used as a performance claim (see `metrics.md`, PT-1), and why the
  byte fixture - which cancels the Editor's per-frame allocation out - is the decision metric.
- The Editor's in-memory profiler buffer is not retained after a run, so `get_worst_cpu_frames` and
  `get_worst_gc_frames` answer `No valid frame data found in profiler` for these runs. The per-frame
  history is in `frames.csv` instead, which is why the harness writes it.
- Nine restart frame samples per run is a small sample; the run-to-run spread of the worst restart
  frame (5.296 / 8.099 / 5.154 ms) is larger than any effect claimed from it.

## How to reproduce

1. Open `Assets/Scenes/DamaGame.unity`.
2. Make sure play mode is **off** - the harness enters it itself and refuses otherwise.
3. Run `Tools > Dama > Week 16 > 1. Baseline Profile` (re-records `moves.txt`), or
   `2. After Optimization 1` / `3. After Optimization 2` (replays the recorded turns).
4. Wait ~80 s. The Editor log prints `run complete`, and
   `docs/Charibelle Guzman/Week_16/Evidence/profile_*/` receives a fresh `summary.txt` and
   `frames.csv`.
5. Do not edit any script while a run is in progress: a recompile mid-run stalls the harness.

The regression and accessibility runs are separate and much faster:

- `Tools > Dama > Week 16 > 4. Regression Run` - the same 28 Week 15 cases, written to
  `Evidence/test_logs_regression/` (`~40 s`).
- `Tools > Dama > Week 16 > 0. Accessibility Audit` - writes `Evidence/a11y_week16_after.log`
  (instant, edit mode).
