# Defect log - Dama AS3 Milestone (Week 15)

**Student:** Charibelle Guzman (BSIT-4)
**Baseline:** `test_logs_baseline/case_log.tsv` — 22 passed, 5 failed, 1 blocked, 0 console errors
**Baseline audit:** `a11y_week15_baseline.log` — 83 passed, 9 failed, 0 warnings, 4 not measurable
**Final:** `test_logs_final/case_log.tsv` — 27 passed, 0 failed, 1 blocked, 0 console errors
**Final audit:** `a11y_week15_after.log` — 105 passed, 0 failed, 0 warnings, 4 not measurable

Every defect below was **raised by the baseline run before any code was changed**, and is
**closed by a re-run of the same case with the same harness**. No row in this log is closed on the
strength of reading the source.

Severity uses the PT6 scale: **S1** blocker, **S2** major, **S3** minor, **S4** cosmetic.

## Summary

| ID | Severity | Requirement | Symptom | Baseline case(s) | State |
|---|---|---|---|---|---|
| DEF-15-01 | S2 | REQ-17, REQ-09 | A match cannot be left for the title screen; the pause menu and the end screen have no route out | `TC-FLOW-06`, `TC-RULE-11` | Closed |
| DEF-15-02 | S3 | REQ-20 | Five button captions are 14 px and `ProfilePanel/StatusText` is 17 px, so the smallest text on screen is the caption of the control it sits on | `TC-A11Y-03`, audit `S` rows | Closed |
| DEF-15-03 | S3 | REQ-09 | The end screen states only the winner, never the score or why the match ended | `TC-RULE-08`, `TC-RULE-10` | Closed |
| DEF-15-04 | S3 | REQ-20 | A control whose fill cannot be told from the panel behind it had no visible edge, and the audit could not see it | audit `N` rows (new check) | Closed |

Open defects after the fix pass: **none**. Open S1/S2: **none** — see `acceptance_criteria.md`
for what that does and does not licence.

---

## DEF-15-01 - No route back to the title screen from inside a match

| | |
|---|---|
| Severity | **S2** - a player who has started a match cannot reach the profile screen or start over without closing the app |
| Requirement | REQ-17 (route back to the title screen), part of REQ-09 (end screen offers a way back) |
| Found by | Week 14 machine pre-test (finding M2), reproduced at baseline as `TC-FLOW-06` and `TC-RULE-11` |

**Reproduction.** Press START, then PAUSE. The panel offers RESUME, RESTART and SOUND, and nothing else. The end screen offers RESTART only. Closing the pause menu's only exit is RESTART, which immediately begins a new match; there is no way to reach the title screen where PROFILE lives.

**Baseline evidence.** `test_logs_baseline/case_log.tsv`:

- `TC-FLOW-06  FAIL  PausePanel/MenuButton present=False, DamaBoard.ReturnToMenu present=False`
- `TC-RULE-11  FAIL  RestartButtonWin=True MenuButtonWin=False`

**Root cause.** `DamaBoard.CacheUI` wires a fixed list of controls and none of them leads back to `GameState.Ready`, so no such control was ever built by the scene builder either. The game state machine had five states and no transition from `Paused` or `GameOver` back to `Ready`.

**Fix.**

- `Assets/Scripts/Editor/Week15Fixes.cs` - creates `PausePanel/MenuButton` beside `RestartButtonPause` and `WinPanel/MenuButtonWin` beside `RestartButtonWin`: same anchor line, offset 340 px to the right, 320 x 96, so the row stays straight and no two controls overlap. Four 96 px controls cannot stack vertically in the pause panel (a 357 px band against the 420 px four controls plus gaps need), which is why MENU goes beside RESTART rather than under it.
- `Assets/Scripts/DamaBoard.cs:240` - `ReturnToMenu()` restores the clock to 1, rebuilds the board and shows `GameState.Ready`. The clock matters: both the pause menu and the end screen are reached with `Time.timeScale = 0`, so a title screen shown without restoring it would be frozen.
- `Assets/Scripts/DamaBoard.cs:151` and `:153` - `CacheUI` wires both new buttons to `ReturnToMenu`.

**Retest.** `test_logs_final/case_log.tsv`:

- `TC-FLOW-06  PASS  StartPanel=True PausePanel=False HUDPanel=False timeScale=1.00`
- `TC-RULE-11  PASS  RestartButtonWin=True MenuButtonWin=True`

Also covered by the audit: `T PausePanel/MenuButton 320x96 px` and `T WinPanel/MenuButtonWin 320x96 px`,
both at the touch floor, and `TC-A11Y-02` reports `0 overlapping pairs` with the two new buttons in place.

---

## DEF-15-02 - Five captions at 14 px and one label at 17 px

| | |
|---|---|
| Severity | **S3** - readable on a desktop monitor, small on the screen this game is aimed at |
| Requirement | REQ-20 (no caption below 20 px, no other label below 18 px) |
| Found by | Week 14 finding M1, reproduced at baseline by `TC-A11Y-03` and the audit's `S` check |

**Reproduction.** Open any screen and read the captions. START, PAUSE, RESUME, RESTART and the end
screen's RESTART are all 14 px, smaller than every other string in the game, and the profile screen's
status line is 17 px.

**Baseline evidence.** `a11y_week15_baseline.log` - six `S` failures:

```
S  FAIL  StartPanel/StartButton/Text         size=14px need=20px role=caption  short by 6 px
S  FAIL  HUDPanel/PauseButton/Text           size=14px need=20px role=caption  short by 6 px
S  FAIL  PausePanel/ResumeButton/Text        size=14px need=20px role=caption  short by 6 px
S  FAIL  PausePanel/RestartButtonPause/Text  size=14px need=20px role=caption  short by 6 px
S  FAIL  WinPanel/RestartButtonWin/Text      size=14px need=20px role=caption  short by 6 px
S  FAIL  ProfilePanel/StatusText             size=17px need=18px role=label    short by 1 px
```

`TC-A11Y-03` reported the same defect through the game's own harness: `5 caption(s), 1 label(s) below floor`.

**Root cause.** Each screen was built separately and each button's caption kept whatever size its
builder gave it. Nothing in the project owned "the smallest text on screen".

**Fix.** `Week15Fixes.RaiseTypeSizes` - a caption below the 20 px floor becomes **22 px bold**; a label
below the 18 px floor becomes **20 px**. Only the offenders move: a caption already at 20 px or more is
left exactly as designed, so this does not redraw screens the audit already passes. The floors are the
audit's own constants, so the fix and the measurement cannot drift apart.

**Retest.** `a11y_week15_after.log` - all 27 labels pass `S`. `ProfilePanel/StatusText size=20px need=18px`.
The delta file marks all six as `FIXED` and nothing else moved:
`a11y_delta_week15.txt` - `S` rows for the five captions and `StatusText`.

Raising the type did not cost contrast: the same six rows still pass `C` at their old ratios
(`StartButton/Text 4.94:1`, `ResumeButton/Text 4.73:1`, `RestartButtonPause/Text 4.71:1`), and label fit
still passes with room to spare (`height=96.0 need=24.6`).

---

## DEF-15-03 - The end screen does not say how the match ended

| | |
|---|---|
| Severity | **S3** - the player is told who won but not why, or by how much |
| Requirement | REQ-09 (winner, both final counts, the reason, RESTART and MENU) |
| Found by | Week 14 finding M3, reproduced at baseline by `TC-RULE-08` and `TC-RULE-10` |

**Reproduction.** Win a match by taking every enemy piece, then win one by leaving the opponent with
pieces but no legal move. Both end screens read `RED WIN!` and nothing else, so the two outcomes that
the rules distinguish are indistinguishable on screen.

**Baseline evidence.** `test_logs_baseline/case_log.tsv`:

- `TC-RULE-08  FAIL  WinPanel/SummaryText missing`
- `TC-RULE-10  FAIL  WinPanel/SummaryText missing`

**Root cause.** `DeclareWinner(int)` was given the winner and nothing else. `CheckForWin` knew the
reason - zero pieces against no legal move - and discarded it, and there was no text object for it to
write into even if it had not.

**Fix.**

- `Week15Fixes.EnsureSummaryText` creates `WinPanel/SummaryText`: anchors x 0.10-0.90, y 0.43-0.54, 22 px, centred. That band is the only one available - `WinText` occupies 0.56-0.76 and the button row's top edge is at 0.41 - and it leaves a 13 px gap above the buttons and 13 px below the title.
- `Assets/Scripts/DamaBoard.cs:380` - `DeclareWinner` derives the loser, both counts and the reason from the board it is looking at, and writes `RED WINS.  RED 6-1, BLUE has no legal move.` **The method keeps a single parameter.** Adding a second would have made `Week14Checks`'s reflection lookup (`GetMethod("DeclareWinner", ...)` with no argument types) throw `AmbiguousMatchException`, and it would have created a second place for the winner and the reason to disagree.

**Retest.** `test_logs_final/case_log.tsv`:

- `TC-RULE-08  PASS  SummaryText='RED WINS.  RED 1-0, BLUE has no pieces left.'`
- `TC-RULE-10  PASS  SummaryText='RED WINS.  RED 1-1, BLUE has no legal move.' expected reason 'no legal move' and 'RED 1-1'`

The two outcomes now produce two different sentences from the same code path, which is the point of
the case pair.

---

## DEF-15-04 - A control fill with no edge against its panel, and an audit that could not see it

| | |
|---|---|
| Severity | **S3** - the control is there and works; it cannot be seen as a control |
| Requirement | REQ-20 (every control fill at least 3:1 against its panel, WCAG 2.2 SC 1.4.11) |
| Found by | Deferred in the Week 14 SA6 section 8 as "the audit has no non-text contrast check"; the check was written this week and immediately found three failures |

**Reproduction.** The audit's `N` check measures each button's own fill against the panel behind it.
Three fills sit at roughly half the required ratio against the black overlay, so the button reads as
text floating on the panel rather than as a button.

**Baseline evidence.** `a11y_week15_baseline.log`:

```
N  FAIL  PausePanel/SoundButton      ratio=2.26:1 need=3.0 fill=#404759 panel=#000000
N  FAIL  ProfilePanel/ResetButton    ratio=2.55:1 need=3.0 fill=#8C2E2E panel=#000000
N  FAIL  ProfilePanel/BackButton     ratio=2.27:1 need=3.0 fill=#404759 panel=#000000
```

**Root cause.** Two causes, one row each. The Week 14 contrast fix darkened any fill whose **white
label** was under 4.5:1, which is the right thing for the label and the wrong thing for the control's
edge: `#404759` is what the SOUND and BACK buttons were darkened to. `ResetButton` was simply built
dark. And the audit had no check that would have reported either, so both survived Week 14's pass.

**Fix.**

- `A11yAudit.AuditNonTextContrast` - the missing check, with its derivation written into the log header rather than assumed.
- `Week15Fixes.LiftFills` - a fill the audit reports below 3.0:1 is lightened in its own hue, 1% per step, until it clears a 3.15:1 target, and never so far that its white label drops below 4.6:1. The two constraints are in tension and the label wins: an unreadable caption is worse than a soft edge. Fills that already pass are not touched.

**Retest.** `a11y_week15_after.log`:

```
N  PASS  PausePanel/SoundButton    ratio=3.19:1 need=3.0 fill=#545D74 panel=#000000
N  PASS  ProfilePanel/ResetButton  ratio=3.17:1 need=3.0 fill=#A43636 panel=#000000
N  PASS  ProfilePanel/BackButton   ratio=3.16:1 need=3.0 fill=#535C74 panel=#000000
```

The white labels on those three fills still pass their own threshold (`6.59:1`, `6.63:1`, `6.64:1`),
so the fix bought the edge without spending the text. The two MENU buttons built by the same pass
carry the same slate and the same `3.19:1`.

---

## What is not in this log, and why

- **The blocked case.** `TC-COMPAT-01` (second device or configuration) is **BLOCKED**, not passed. `adb devices` returns `List of devices attached` with no serial, and there is no APK this week. The command and its output are recorded in `environment.md`.
- **The unrun human sessions.** Week 14's five playtest sessions remain unrun. This log records what the machine can prove; it does not claim a first-time player was observed.
- **The Week 14 findings that are not defects.** The Week 14 deferred table also carried items that were work items rather than failures at the time. They are not listed here as closed, because nothing was re-measured for them this week.
