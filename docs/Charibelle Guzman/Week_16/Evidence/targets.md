# Week 16 - targets, written before any measurement

**Student:** Charibelle Guzman (BSIT-4)
**Theme:** Philippine Games and Sports
**Game:** Dama
**Written:** 2026-11-26, before the baseline run

These four numbers are the game's own budget, decided from what Dama is - a two-player,
turn-based board game on a phone with no animation loop and no physics - and written down
*before* the baseline was taken. A threshold chosen after seeing the result would not be a
threshold; it would be a description.

| Id | Target | Value | Why this value |
|---|---|---|---|
| PT-1 | Frame budget in the measured scenario | **33.3 ms mean** (30 fps), **p95 under 33.3 ms** | A turn-based board game that never animates a frame-critical path does not need 60 fps. 30 fps is the floor a touch game reads as smooth, and 33.3 ms is what one frame at that rate costs. 60 fps (16.7 ms) is recorded as a stretch figure, not the pass line |
| PT-2 | Managed allocation per turn | **0 B of game allocation per turn** | The HUD is two short strings; a turn that changes two pieces must not allocate a collection per piece scanned. `RefreshUI`'s interpolated strings are expected and are measured separately so the rule does not hide them |
| PT-3 | Restart cost | **worst frame under 16.7 ms** | RESTART is a tap away from a lost match and rebuilds the whole board. One 60 fps frame is the acceptable hitch for a deliberate user action |
| PT-4 | Steady-state draw calls | **record, do not reduce this week** | 64 squares + up to 25 pieces are separate `SpriteRenderer`s with no `SpriteAtlas` in the project. Making a number up for a pass line before measuring the ceiling would be guesswork; the baseline records it and the report names the ceiling |

## What "the same conditions" means here

Every run of `Tools/Dama/Week 16/*` uses this scenario, in this order, and nothing else:

1. the default data directory, profile reset, `StartGame`;
2. 3 s warm-up, discarded;
3. 32 turns at one per ~1.95 s, each chosen by the game's own `GetSelectableMoves` and executed
   through its own `ExecuteMove` - the same input sequence in all three runs, replayed from
   `moves.txt` after the baseline recorded it;
4. a restart whenever the match ends, then the run continues;
5. a 3 s dwell on the end screen.

Conditions recorded in the summary header of every run: editor and engine version, scene, game
view size, quality level, vSync count, `Application.targetFrameRate`, render pipeline, graphics
device, and whether the Profiler was enabled. A run under different conditions is not comparable
and the report says so rather than quietly averaging it in.

## What is measured, and what is not

| metric | source | note |
|---|---|---|
| frame time | `Stopwatch` between frame boundaries | independent of the profiler; works even if every counter is unavailable |
| `GC.Alloc` | `ProfilerRecorder`, with a `Profiler.GetMonoUsedSizeLong()` delta as the documented fallback | if neither is available the column reads `NOT MEASURABLE`, never 0 |
| draw calls, batches, set-pass, triangles | `ProfilerRecorder` (Render category) | Render category counters are 0 when the profiler cannot see them; a 0 is reported as `NOT MEASURABLE` rather than as a perfect score |
| memory | `Profiler.GetTotalAllocatedMemoryLong()` | peak and end-of-run |

Not measured, and therefore not claimed: battery, thermal behaviour, GPU time on a phone, and
anything about a physical device. There is no APK this week and `adb devices` has no serial, so
device performance stays **BLOCKED** exactly as Week 15 recorded compatibility.
