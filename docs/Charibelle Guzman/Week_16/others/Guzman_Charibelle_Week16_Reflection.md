# Week 16 Reflection - What Profiling Actually Taught Me

**Game:** Dama
**Student:** Charibelle Guzman
**Section:** BSIT-4
**Course:** MOBILE DEV 002 Advanced Mobile Application Development, Module 5
**Week:** 16 - Mobile Game Profiling and Performance Optimization
**Produced:** 2026-11-26

I expected this week to be about making the game faster. It turned out to be about finding out
whether I could tell whether the game was faster - and that was the harder, more useful lesson.

## The number I was most proud of was wrong

I built the profiling harness, ran it, and it printed `0.0 B allocated per turn`. That is exactly
what my own target asked for, so my first reaction was satisfaction. My second reaction was doubt,
because `0.0` is a suspicious answer: the game allocates a `List` per piece scanned, so a true zero
was not credible.

So I tested the counter itself. I allocated 64 KB on purpose and asked the counter how much it had
seen. It reported **0 B**. The instrument was blind, and if I had not checked, my entire week's
evidence - a whole document, "target met" - would have been a lie with a clean table around it.
Four other byte counters failed the same way: one is a stub, two move in 4 KB blocks, one throws
`NotImplementedException`. The only counter that does work is the Editor-wide per-frame one, and it
carries about 210 KB of the Editor's own work per frame, which is roughly 200 times the size of the
thing I was trying to measure.

The fix was to stop trying to read the signal and start **amplifying** it: run the same call *n*
times and *2n* times, and fit a line. The slope is the cost of one call; the intercept eats the
Editor's own allocation. The first time I saw a slope of 6,975.9 B with R² 1.00 come out of a counter
I had just upgraded from useless, I trusted it - because I could see it move when I asked the same
call to run twice as often.

The lesson generalises beyond Unity: **before you measure something, measure your ruler**. A broken
measurement does not announce itself; it reports a perfect score.

## Profiling told me what *not* to fix

My frame mean is 2.062 ms against a 33.3 ms budget, with **zero** frames over budget out of 34,550.
My instinct was that this much headroom meant something was being wasted and I should go find it. The
better reading is the opposite: frame time is simply not this game's problem, and any optimization I
justified with frame time would have been a story I told myself.

Instead the data pointed somewhere I would not have guessed: the thing that costs the most is
**deciding who is allowed to move**. `CheckForWin` asks `HasAnyLegalMove` at the end of every turn,
which asks every piece whether it can capture, and each of those questions was building a brand-new
list. 6,975.9 bytes per turn, on a turn-based board game, forever. Removing it was a twenty-line
change, and the whole-board figure went from 6,975.9 B to 0.1 B.

That is the difference between optimizing what looks expensive and optimizing what repeats.

## Being honest about the numbers that did not move

The restart work is where I had to be most careful with myself. I expected the restart frame to drop
and it did not, not really: 5.296 ms before, 5.154 ms after, 8.099 ms in between. Nine samples per
run cannot separate that from noise, so I did not claim it. What I can prove is that the *allocation*
on that path collapsed, because the byte fit can see it and the error bar, while wide, sits far from
the old value.

The same discipline applies to frame time: mean went from 2.062 ms to 2.181 ms. That looks like a
5.8% regression caused by my optimizations, and it would have been easy to hide it by quoting only
the p95. The honest test was the idle frames - 2.059 ms to 2.172 ms - which execute no move
generation at all and moved by the same amount. If idle moves, it is the machine and the Editor
session, not my code. Saying that out loud is worth more to me than a nicer table.

I also had to write down, before measuring, that the restart target was **already met**. It would
have been convenient to pretend it was failing so that I had "two bottlenecks, both fixed". Instead
the second change is reported as what it is: churn removed, on a path whose budget was never in
danger.

## What I could not do, and what I would do next

There is no phone in this lab and no APK was built, so every number this week is an Editor number.
The Editor adds about 2 ms of its own work to every frame and cannot see draw calls at all - both of
which I have written into the evidence rather than around. What that means practically is that I
have proved the game allocates nothing per turn, and I have **not** proved anything about how that
feels on an Adreno or Mali phone.

If I had one more week, the honest order would be:

1. build the APK and capture draw calls and frame time on the one handset I can find, so the
   `NOT MEASURABLE` cells become measurements;
2. profile a long match, not a 71-second window, to see whether the pool's remembered pieces and
   any slow growth matter as the session wears on;
3. profile in a Development build rather than in the Editor, because the Editor's own 2 ms hides
   exactly the costs I am now small enough to care about.

## What I am taking out of this week

Two things. The first is that **a measurement you have not tried to break is not a measurement** -
the counter that read 0 B through a 64 KB allocation taught me that in one afternoon. The second is
that the most valuable thing profiling produced was not an optimization at all: it was the sentence
"frame time is fine, stop looking there". Knowing where *not* to spend a week is a result, and I only
know it because I wrote the targets down before I looked at the numbers.

---

I confirm that this reflection describes my own development experience and that all assistance,
including AI assistance, has been disclosed. The documents for this week were drafted with AI
assistance and then checked against the build: every number quoted in this reflection and in
`Guzman_Charibelle_SA6.pdf` / `Guzman_Charibelle_AS3.pdf` is copied from a `summary.txt`, a
`case_log.tsv` or an audit log that the harness generated, and each figure was re-read against its
source file before the PDFs were printed. The frame-time and draw-call claims that the evidence could
not support were removed from the documents rather than softened.
