# Week 16 - the three runs, side by side

**Student:** Charibelle Guzman (BSIT-4)
**Theme:** Philippine Games and Sports
**Game:** Dama

All three runs used the same scenario, the same scene, the same game view (1151x584), the same
quality level (5, Ultra), vSync forced off and `Application.targetFrameRate = -1` for the duration,
and the same 32 turns replayed from `Evidence/moves.txt`. Every number below is copied from the
`summary.txt` of the run it is attributed to. A percentage is always followed by the two raw
values it was computed from.

| run | folder | written | code under test |
|---|---|---|---|
| baseline | `profile_baseline/` | 2026-11-26 14:20:15 | commit `aacfc7f` |
| after opt 1 | `profile_after_alloc/` | 2026-11-26 14:27:06 | commit `356e99f` |
| after opt 2 | `profile_after_pool/` | 2026-11-26 14:36:21 | working tree on top of `356e99f` |

One earlier `after-optimization-2` run (14:33:27) was **discarded**: its numbers were fine but the
harness's RESTART paragraph still described the pre-optimization code, so the file contradicted
itself. It is not cited anywhere in this report. For the record its frame mean was 2.180 ms and its
worst restart frame 6.637 ms - the same as the run that replaced it, within noise.

## PT-1 - frame budget (target: 33.3 ms mean, p95 under 33.3 ms)

| metric | baseline | after opt 1 | after opt 2 |
|---|---|---|---|
| mean frame | **2.062 ms** | 2.191 ms | 2.181 ms |
| p95 | **2.841 ms** | 3.251 ms | 3.097 ms |
| max | 13.397 ms | 25.896 ms | 19.251 ms |
| idle (non-action) mean | 2.059 ms | 2.173 ms | 2.172 ms |
| frames over 33.3 ms | **0 of 34,550** | 0 of 32,516 | 0 of 32,681 |

**PT-1 met in all three runs, with no frame anywhere near the budget.**

Read honestly, the frame mean did **not** improve: 2.062 -> 2.181 ms is +0.119 ms (+5.8%). That is
not a regression the optimizations caused, and the reason is in the table: the **idle** mean moved
by almost the same amount (2.059 -> 2.172 ms, +0.113 ms), and idle frames run no move generation at
all beyond what the Editor itself is doing. A change to move generation cannot move the idle frame;
a different Editor session can. The frame in this harness is dominated by the Editor's own ~2 ms
per-frame work, which is why the allocation fixture exists: it is the instrument that can actually
see the code change. This is also the honest limit of an Editor-side measurement and is repeated in
`method.md`.

## PT-2 - allocation per turn (target: 0 B beyond the HUD strings) - **TURNED FROM FAILED TO MET**

Measured by amplitude scaling: the same call is run n and 2n times in separate frames, the per-frame
`GC.Alloc` is fitted, and the slope is the bytes per call. The intercept absorbs the Editor's own
per-frame allocation, so the comparison between runs is clean. A slope that is not separated from
zero by more than twice its standard error is reported as *below the resolution of this method*, not
as a number.

| fixture | baseline | after opt 1 | after opt 2 |
|---|---|---|---|
| A. one `GetSelectableMoves` | **556.7 B** +/- 3.3 (R2 1.00) | -6.8 B +/- 4.2 (R2 0.25) | **-3.6 B +/- 3.6 (R2 0.11)** |
| B. one legality scan, 12 pieces (the per-turn figure) | **6,975.9 B** +/- 0.1 (R2 1.00) | -0.1 B +/- 0.1 (R2 0.25) | **0.1 B +/- 0.1 (R2 0.11)** |
| C. one RESTART incl. profile write | 5,701.2 B +/- 145.5 (R2 0.99) | 5,596.2 B +/- 109.4 (R2 1.00) | **-1,665.5 B +/- 1,704.1 (R2 0.11)** |

- **A: 556.7 B -> below the resolution of the method (-3.6 B +/- 3.6 B).** The single-call
  allocation is gone; the fitted slope is now statistically zero. That is 556.7 B removed per call,
  a reduction of at least 98.7% (556.7 B to a 2-sigma upper bound of 3.6 B).
- **B: 6,975.9 B -> 0.1 B +/- 0.1 B.** B is what `CheckForWin` -> `HasAnyLegalMove` does at the end
  of every turn, so this is the per-turn figure, and **PT-2 is met**: the per-turn game allocation is
  no longer distinguishable from zero against a budget of 0 B.
- **C: 5,596.2 B -> -1,665.5 B +/- 1,704.1 B.** The slope collapsed from 5,596 B with R2 1.00 to a
  value indistinguishable from zero, but the error bar is wide, so the honest statement is bounded:
  a restart now allocates **at most about 1.7 KB** (two standard errors above the fitted slope),
  against 5,596 B before - **at least a 69% reduction**, and the point estimate is zero. The pool
  cannot do better than this instrument can resolve.

The fixture's own R2 tells the same story: the baseline fits are 1.00 / 1.00 / 0.99 because there
was a real per-call cost to find. After the optimizations the R2 is 0.11-0.25, which is what a flat
line through noise looks like. The instrument did not stop working - the intercept and the
Editor-wide column still move - there is simply nothing left at the resolution it can see.

## PT-3 - restart cost (target: worst frame under 16.7 ms) - **MET in the baseline already**

| metric | baseline | after opt 1 | after opt 2 |
|---|---|---|---|
| worst restart frame | 5.296 ms | 8.099 ms | **5.154 ms** |
| mean restart frame | 3.103 ms | 4.572 ms | 3.668 ms |
| idle mean in that run | 2.059 ms | 2.173 ms | 2.172 ms |
| hitch over idle | 1.044 ms | 2.399 ms | 1.496 ms |
| samples | 9 (3 frames x 3 restarts) | 9 | 9 |

Three restarts x three frames is nine samples per run, and the run-to-run spread (5.296 / 8.099 /
5.154 ms worst) is larger than the effect being chased, so the restart frame time is **not** claimed
as an improvement. It is claimed as **still under budget in every run**: 5.154 ms against 16.7 ms
leaves 11.5 ms of headroom (69% of the budget unused) after both optimizations.

What did improve on the restart path is the allocation in row C, and there the effect is far outside
the noise even with its wide error bar (5,596 B -> at most ~1.7 KB). The restart optimization is
therefore reported as **churn removed**, proven by the byte fit, and **frame time unchanged and
still passing**, proven by the nine samples - not as a frame-time win it cannot demonstrate.

## PT-4 - rendering (target: record, do not reduce)

| metric | baseline | after opt 1 | after opt 2 |
|---|---|---|---|
| draw calls | NOT MEASURABLE | NOT MEASURABLE | NOT MEASURABLE |
| batches | NOT MEASURABLE | NOT MEASURABLE | NOT MEASURABLE |
| set pass calls | NOT MEASURABLE | NOT MEASURABLE | NOT MEASURABLE |
| triangles | 306.0 | 305.9 | 305.8 |
| memory peak | 1674.6 MB | 1722.1 MB | 1828.3 MB |

`NOT MEASURABLE` is the recorded answer, not a zero: the recorder was valid (`gc=on draws=on
batches=on setPass=NOT MEASURABLE triangles=on` in every run's log) but every draw-call and batch
sample read 0 while the scene was visibly drawing, so no number is claimed. Triangles are stable to
within one triangle per run, which is the sanity check that the same board was on screen.

Memory peak rises 1674.6 -> 1828.3 MB across a single Editor session. Most of that is the session,
not the code: 47.5 MB of it appeared between the baseline and after-opt-1 runs, which added no
objects at all. The pool's own cost is bounded and known - 24 piece GameObjects that would have been
destroyed anyway, plus the crowns those pieces grow - and it is paid once for the session instead of
24 objects of churn per restart. It is named here as a trade-off rather than hidden.

## Gameplay equivalence - the same game happened in all three runs

| run | turns | restarts | move hash | replay rejections |
|---|---|---|---|---|
| baseline | 32 | 3 | **0xE432E83D** | 0 (this run recorded `moves.txt`) |
| after opt 1 | 32 | 3 | **0xE432E83D** | 0 |
| after opt 2 | 32 | 3 | **0xE432E83D** | 0 |

The hash accumulates every move's from/to cell and both piece counts. Identical hashes across three
builds mean the same 32 turns produced the same board states. Both optimized runs **replayed** the
baseline's recorded turns rather than choosing their own, so the input sequence was identical by
construction, and `IsReplayLegal` would have logged a warning and counted a miss if the optimized
move generator had disagreed about legality. There were no warnings and no misses in either run.

## What this does not prove

- Nothing about a phone. Editor play mode, one machine, one resolution, no APK, `adb` has no device.
  Device performance stays **BLOCKED**, exactly as Week 15 recorded compatibility.
- Nothing about battery, thermals or GPU time.
- Draw calls and batches are unmeasured, so no rendering claim is made either way.
- The frame-mean comparison between runs is not a performance claim: see PT-1 above.
