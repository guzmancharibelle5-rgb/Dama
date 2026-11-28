# Week 15 Reflection - Game Testing, Defect Correction and Quality Evidence

**Game:** Dama (Philippine Games and Sports theme)
**Student:** Charibelle Guzman
**Section:** BSIT-4
**Course:** MOBILE DEV 002 Advanced Mobile Application Development, Module 5
**Week:** 15
**Build:** `dama-editor-w15-rc1` - scene `DamaGame.unity` at commit `fdb3cc1`, branch `main`
**Date:** November 26, 2026

---

## 1. The defect that taught me the most

`TC-FLOW-06`, the route back to the title screen. I carried that finding forward from Week 14 as M2 and thought
I already understood it. I understood the symptom. What the test taught me was the size of the cause. The case
does not ask whether a MENU button exists; it asks whether the game can be left. The baseline measured value was
`PausePanel/MenuButton present=False, DamaBoard.ReturnToMenu present=False`, and the second half is the part that
mattered: there was no method to call. `DamaBoard` had no transition from `Paused` or `GameOver` back to `Ready`.
So this was not a missing button, it was a missing direction in the game's own model, and the pause panel had been
offering RESTART as its only exit because there was nothing else to offer.

That reframed the other three findings. Each was the game keeping a fact and never carrying it to the screen: the
end screen said only `RED WIN!` because `DeclareWinner` received the winner and the reason derived in
`CheckForWin` was thrown away; the smallest caption anywhere was 14 px because each panel builder picked its own
number and nothing owned "the smallest text". Three defects, one habit.

## 2. Reproducing and correcting it

Reproducing took two taps, written down: START, then PAUSE, and the panel offers RESUME, RESTART and SOUND and
nothing else. Making it reproducible for someone else became `TC-FLOW-06` in
`Assets/Scripts/Editor/Week15Checks.cs` - the same file produced the failing line above and the passing line after
the fix. The correction had two halves that had to agree:

- `Week15Fixes.EnsureButton` builds `PausePanel/MenuButton` beside `RestartButtonPause` and
  `WinPanel/MenuButtonWin` beside `RestartButtonWin`: same anchor line, 340 px right, 320 x 96. It went beside
  RESTART and not under it because the arithmetic refused it - four 96 px controls plus three 12 px gaps need
  420 px and the pause title leaves a 357 px band. Stacked, I would have shipped a second defect while fixing the
  first.
- `DamaBoard.ReturnToMenu` restores `Time.timeScale = 1` **before** it shows the title screen. Both screens are
  reached with the clock stopped, so without that line the title screen is drawn and dead to the touch, and a
  screenshot would not have told me. The case asserts it: `StartPanel=True PausePanel=False HUDPanel=False
  timeScale=1.00`.

The retest is the same case, the same assertion, against the corrected build. Nothing about the test moved - only
the code under it - which is the only reason I trust the result.

## 3. The regression risk I discovered

Two, and I found neither by reasoning.

**The contrast margins got thinner because of a fix.** Raising five captions from 14 px to 22 px touches no
colour, so I assumed contrast was untouched. The delta file says otherwise: `PausePanel/SoundButton/Text` fell
from `9.29:1` to `6.59:1`, because lightening a fill to give the control a visible edge spends the contrast of the
white label on it. The edge needs 3.0:1 against the panel and the label needs 4.5:1 against the fill, and those
pull against each other. The pass is bounded on both sides and the label wins, but three controls now sit at
3.16:1, 3.17:1 and 3.19:1 against a 3.0 floor and `ProfilePanel/SideButton` sits at 3.03:1. Nothing warns me if
the next edit pushes them under. The audit runs before every submission from now on.

**The screenshots were lying, and only the numbers caught it.** My first capture run drew the board from the 24
opening pieces while the grid held a seeded end position, so `E2_end_screen.png` showed a HUD reading
`Red: 12   Blue: 12` beside a summary reading `RED 1-1`. Both were rendered correctly from two different sources
of truth: seeding by reflection changes what the game believes, not what is drawn. The harness now rebuilds the
visible pieces before every capture, and the `Rebuilt N visible piece(s)` lines in `week15_shots.txt` are what let
a reviewer check the counts in a picture against the state it claims. I left that disclosure in the transcript
rather than re-taking the images quietly, because a screenshot that agrees with the log by accident is worse than
one that documents why it once did not.

## 4. My confidence, and its limits

Confident about what was measured, deliberately unconfident about the rest.

What supports it: 27 of 28 cases pass against a baseline of 22 passed and **5 failed** - the same five cases, now
passing, none removed or renamed. The audit passes every scored row, 105 of 105, with 0 regressed and 0 dropped
when all 109 rows are paired across two runs. The repair script proved idempotence instead of asserting it:
`5BB40B04 -> 8C0D040E` on the first run, `8C0D040E -> 8C0D040E (UNCHANGED - this run was a no-op)` on the second.
The console is clean at the end of both runs, which answers the module's "repeatedly crashes" condition with a
number.

What limits it, written down rather than rounded up: one case is **BLOCKED** - `adb devices` returns no serial and
no APK exists, so nothing here ran outside the Editor, at one resolution, on one machine. `REQ-03` and `REQ-12`
are covered only indirectly; the `lose` clip is loaded and wired but RED won every match I seeded, so it never
fired. Both are marked as gaps rather than counted as passes through neighbouring cases.

The excerpt I leaned on hardest, the same case before and after, from `Evidence/test_logs_baseline/case_log.tsv`
and `Evidence/test_logs_final/case_log.tsv`:

```
TC-RULE-10  FAIL  ...  WinPanel/SummaryText missing
TC-RULE-10  PASS  ...  SummaryText='RED WINS.  RED 1-1, BLUE has no legal move.'
```

Two sentences from one method, and the second exists because the first did not. My verdict: the Editor build at
commit `fdb3cc1` passes the acceptance criteria I wrote before running it, and no evidence this week claims it ran
anywhere but in the Editor.

---

I confirm that this reflection describes my own development experience and that all assistance, including AI
assistance, has been disclosed.
