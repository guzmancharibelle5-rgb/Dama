# PT6 - Accessibility Audit, Audio Feedback, and the Playtest Package

**Game:** Dama
**Student:** Charibelle Guzman
**Section:** BSIT-4
**Course:** MOBILE DEV 002 Advanced Mobile Application Development, Module 5
**Theme:** Philippine Games and Sports
**Week:** 14 - Game Usability, Accessibility and Playtesting
**Build:** Dama MVP (Android target: ARM64 / IL2CPP, `com.defaultcompany.dama`, minimum API 26)
**Engine:** Unity 6000.6.0f1, Universal Render Pipeline 17.6.0 (2D Renderer), C#
**Repository:** local Git repository, branch `main`, code commit `1998b0c`
**Components under test:** `Assets/Scripts/Editor/A11yAudit.cs`, `Assets/Scripts/Core/GameFeedback.cs`,
`Assets/Scripts/Editor/AccessibilityBuild.cs`, `Assets/Scripts/Editor/Week14Checks.cs`, `Assets/Scripts/DamaBoard.cs`

---

## 1. What was built

Three things, and a fourth that is a document rather than code.

| Piece | File | Responsibility |
|---|---|---|
| Accessibility audit | `Assets/Scripts/Editor/A11yAudit.cs` | Menu `Tools/Dama/Week 14/1. Run Accessibility Audit`. Walks every `Text`, `Button` and `InputField` under `GameCanvas` **including inactive panels**; three check codes; writes `a11y_baseline.log` on the first run and `a11y_after.log` on later runs, plus a timestamped copy |
| Repair pass | `Assets/Scripts/Editor/AccessibilityBuild.cs` | Menu `Tools/Dama/Week 14/2. Apply Accessibility Fixes`. Darkens a failing fill in its own hue until the label clears its threshold, raises sub-floor controls from their top edge, re-stacks only the columns that changed, de-overlaps, adds `PausePanel/SoundButton`. Idempotent, proved by a layout fingerprint |
| Feedback layer | `Assets/Scripts/Core/GameFeedback.cs` | One `AudioSource`, five clips, `Handheld.Vibrate()` on a capture, a persisted mute. Listens, never decides |
| Evidence harness | `Assets/Scripts/Editor/Week14Checks.cs` | Menu `Tools/Dama/Week 14/3. Capture Week 14 Evidence`. Enters play mode once, captures E3 / E2 / E1 gated on each PNG landing on disk, then drives the rules through a move, a capture, a promotion and both end screens and writes every `[Feedback]` line to `feedback_lines.txt` |
| Playtest kit | `Week_14/Playtest/01` - `05` | Consent form, session script, task cards, observation sheet, facilitator checklist - written now, run later |

### Public surface of `GameFeedback`

| Member | Purpose |
|---|---|
| `SoundPrefsKey` = `"audio.feedback"` | The one `PlayerPrefs` key the layer owns |
| `SoundEnabled` | The current state, read and written by the pause panel |
| `PlayMove()`, `PlayCapture()`, `PlayCrown()`, `PlayResult(bool playerWon)` | The four call sites. `PlayResult` picks win or lose from a bool, so the rules never choose a clip |
| `ToggleSound()`, `SetSoundEnabled(bool)`, `static SoundLabel(bool)` | The toggle, and the words `SOUND: ON` / `SOUND: OFF` it produces. The label is a static pure function so it can be asserted without a scene |

`DamaBoard` gained the calls and nothing else structural: `feedback` and `soundButtonLabel` fields,
`ToggleSound()` and `RefreshSoundLabel()` for the button, and one `Debug.Log("[Feedback] ...")` per
event so the run produces a transcript instead of a claim.

---

## 2. Four pieces of evidence, and what each one proves

| Evidence | Proves | Does **not** prove |
|---|---|---|
| `a11y_baseline.log` / `a11y_after.log` + `a11y_delta.txt` | That ten measurable controls failed three published thresholds, and that the same ten now pass those same thresholds | That the game is usable or accessible |
| `E1`, `E2`, `E3` (play mode) | That the layout the audit measured is the layout the game draws, on the pause panel, the HUD and the profile panel | How it looks on a phone |
| `feedback_lines.txt` | That the five clips are wired to the rules, and fired from a real move, capture, promotion and both end screens | That a player hears them, or that the phone buzzes |
| `autopilot_session.md` | That a machine can complete the five tasks, and four specific defects it found on the way | Anything about a person |

---

## 3. The accessibility check results

Both runs were executed by `A11yAudit.cs` in the Unity editor against `DamaGame.unity`, at the
thresholds stated in the log headers. Every number below is copied from
`Evidence/a11y_delta.txt`, which pairs the two logs mechanically - none is typed from memory.

```
before   TOTAL: 45 passed, 10 failed, 0 warnings, 2 not measurable
after    TOTAL: 58 passed, 0 failed, 0 warnings, 2 not measurable
Baseline failures: 10   paired with a measured after value: 10   unmatched: 0
```

### The ten failures, paired

| # | Check | Control | Before | After | Requirement |
|---|---|---|---|---|---|
| 1 | C | `PausePanel/ResumeButton/Text` | 4.27:1, `bg=#268C40` | **4.73:1**, `bg=#24843C` | 4.5:1 |
| 2 | C | `PausePanel/RestartButtonPause/Text` | 2.72:1, `bg=#D98C1A` | **4.71:1**, `bg=#A06713` | 4.5:1 |
| 3 | C | `WinPanel/RestartButtonWin/Text` | 2.72:1, `bg=#D98C1A` | **4.71:1**, `bg=#A06713` | 4.5:1 |
| 4 | T | `StartPanel/ProfileButton` | 320 x 84, short by 12 | **320 x 96** | 96 x 96 |
| 5 | T | `HUDPanel/PauseButton` | 170 x 64, short by 32 | **170 x 96** | 96 x 96 |
| 6 | T | `ProfilePanel/SideButton` | 260 x 52, short by 44 | **260 x 96** | 96 x 96 |
| 7 | T | `ProfilePanel/SaveButton` | 200 x 54, short by 42 | **200 x 96** | 96 x 96 |
| 8 | T | `ProfilePanel/ResetButton` | 200 x 54, short by 42 | **200 x 96** | 96 x 96 |
| 9 | T | `ProfilePanel/BackButton` | 200 x 46, short by 50 | **200 x 96** | 96 x 96 |
| 10 | T | `ProfilePanel/NameInput` | 420 x 56, short by 40 | **420 x 96** | 96 x 96 |

### The three checks the new control adds

| Check | Control | Measured | Requirement |
|---|---|---|---|
| C | `PausePanel/SoundButton/Text` | 9.29:1, `bg=#404759`, 22 px bold, white | 4.5:1 |
| L | `PausePanel/SoundButton/Text` | height 96, need 24.6 | 1.12 x font size |
| T | `PausePanel/SoundButton` | 320 x 96 | 96 x 96 |

That is why the passed total rises by 13 and not 10: 57 checks became 60.

### What is still not measured, and why that is not a pass

`HUDPanel/TurnText` (34 px) and `HUDPanel/CountsText` (28 px) have **no opaque backdrop** - they are
drawn straight over the 3D board, so their contrast changes as pieces move. Both logs report them
`NOT MEASURABLE`, both logs say so in the total line, and neither is counted as a pass in either
run. The audit's closing line says what the whole exercise is worth:

> *"Every measurable control meets the three thresholds. That is a floor, not a ceiling: an audit
> cannot see an accessible game, and it cannot read a label that is the right size and still says
> nothing."*

### One near-miss, stated rather than rounded up

`ProfilePanel/NameInput/Placeholder` passes at **3.01:1 against a 3.0 requirement**. It is 28 px, so
it is judged as large text and needs only 3.0. A hundredth of a ratio point is not a pass worth
being pleased about, and the repair pass deliberately does not touch a passing control, so it is
left as it is and listed here and in the README.

---

## 4. The three bugs the repair pass had, and how they were found

The repair script is itself code, and it broke things before it fixed them. All three failures were
caught by running it, looking at the result, and measuring - not by reading it.

**4.1 It pushed controls off the bottom of their panels.**
The de-overlap pass sorted siblings by `rect.yMax` to find what sat where. `RectTransform.rect` is
**pivot-local**: `yMax` is a distance from that control's own pivot, not a position in the panel, so
two controls in different rows compared as though they were in the same place and the pass shoved
them 96 to 137 px down. Some left the canvas entirely. The fix was a `PanelRect()` helper that takes
real world corners (`GetWorldCorners`) and converts them into the panel's local space with
`InverseTransformPoint`, so every comparison is in one coordinate system. The damaged positions were
then restored with a one-off scripted pass before running again.

**4.2 It clamped to zero instead of the panel's floor.**
The follow-up clamp compared a control's bottom against `0` - the panel's pivot - rather than
`panel.rect.yMin`. On a panel whose pivot sits at its middle, "0" is the middle of the screen, and
the clamp happily left controls parked below a boundary it thought was a wall.

**4.3 A row nudge moved one button of three.**
Three buttons sit on one row of the profile panel - BACK, SAVE, RESET - at three different anchors.
The nudge looked for a control at a given position, found the first, and left the other two behind:
a row that visibly broke apart. The fix is `ShiftRow()`, which selects siblings by **`anchorMin.y`
being equal** rather than by where they happen to be, and moves all of them by the same amount.

After the fix, the final geometry, printed by the script and visible in `E3`:

| Control | Size | Anchor | Note |
|---|---|---|---|
| `NameInput` | 420 x 96 | `(0.395, 0.72)` | Raised; nudged down 4.6 px to keep a 12 px gap under the label |
| `SideButton` | 260 x 96 | `(0.665, 0.72)` | Raised; sits beside the name field, not under it |
| `BackButton` | 200 x 96 | `(0.220, 0.19)` | Bottom row, nudged down 25.3 px as one row |
| `SaveButton` | 200 x 96 | `(0.500, 0.19)` | Same row |
| `ResetButton` | 200 x 96 | `(0.780, 0.19)` | Same row |

**Idempotence, proved by hash.** Running the pass a second time prints

```
[AccessibilityBuild] fingerprint 4C3E2E0C -> 4C3E2E0C (UNCHANGED - this run was a no-op)
```

The fingerprint covers every control's anchor, size and position. A build script that nudges the UI
by a pixel on every run would make each new screenshot incomparable with the evidence already
submitted, so "it is idempotent" is a measured claim here, not a design intention.

---

## 5. Sound and haptics - what fired, and what cannot be claimed

`feedback_lines.txt`, captured in play mode at 2026-11-26 04:10:11 UTC by menu 3:

| Event kind | Fires from | Line in the transcript |
|---|---|---|
| move | `DamaBoard` after a legal move resolves | `[Feedback] move -> Dama_Move` |
| capture | the capture resolution, with the haptic call | `[Feedback] capture -> Dama_Capture` |
| crown | `PromoteToDama` | `[Feedback] crown - a man reached the back row at (2,7) -> Crown` |
| win | `DeclareWinner`, winner is the profile's side | `[Feedback] win - winner RED, your side RED -> Win` |
| lose | `DeclareWinner`, winner is the other side | `[Feedback] lose - winner BLUE, your side RED -> Lose` |

```
Kinds fired: move, capture, crown, win, lose (5 of 5)
```

**The haptic is not proven and is not claimed.** `Handheld.Vibrate()` is a no-op in the Editor, and
this transcript was captured in the Editor. The call is in the build; the buzz needs a handset. The
transcript says so on its own last two lines, so a reader of the file cannot come away believing a
phone was involved.

**One bug worth recording, because it was silent.** The first version of the harness put a RED man
on `(3, 6)` and moved it to `(2, 7)` expecting a crown - and no crown fired, because `(3, 6)` was
occupied as a BLUE piece, so the move was a BLUE move and BLUE promotes at `y == 0`. The move
played, the sound played, and only the transcript showed that one of the five kinds was missing.
`Kinds fired: 4 of 5` is what caught it; a run that had only listened for "some audio played" would
have passed.

---

## 6. Machine pre-test results

`Evidence/autopilot_session.md` is the full record. It is a **machine pre-test**: an automated agent
played the same five tasks a volunteer will be given, to find mechanical defects before a person's
twenty minutes are spent on them.

| Task | What the run did | Time |
|---|---|---|
| T1 - profile name and save | `PLAYTEST` -> `PLAYTEST2`, status line confirmed, `profile.json` written | 1.6 s |
| T2 - start a match | Start panel hidden, HUD `Turn: RED`, `Red: 12  Blue: 12` | 2.8 s |
| T3 - catch a piece | BLUE count 12 -> 11 on a real click pair | 12.9 s |
| T4 - crown a Dama | RED chain `(4,3) x(5,4) -> (6,5)`, then `x(5,6) -> (4,7)`; ring visible, BLUE 10 | 13.3 s |
| T5 - finish the match | `RED WIN!`, profile matches 5 -> 6, wins 3 -> 4 | 53.2 s |

The match ended 6-1 with BLUE still holding a piece but having no legal move - the immobilisation
rule, exercised for real.

### The four findings

| # | Finding | Severity |
|---|---|---|
| M1 | **The five button captions are 14 px while every other label is 17-110 px.** START, `II PAUSE`, RESUME, RESTART (pause) and RESTART (end screen) are the smallest strings in the game, on the largest controls. The audit cannot see this - it measures contrast and box height, not type size | S3 |
| M2 | **There is no way back to the start screen once a match has begun.** The pause panel offers RESUME, RESTART, SOUND; the end screen offers RESTART. PROFILE exists only on the start screen, so a name or side cannot be changed without restarting | S2 |
| M3 | **The end screen states only the winner.** `RED WIN!` and RESTART: no final counts, and nothing that explains a win by immobilisation, in a match that ended 6-1 | S3 |
| M4 | **`ProfilePanel/StatusText` is the faintest text on the profile panel** at 17 px, beside a 20 px best-times line. Legible in the capture, marginal at arm's length | S4 |

M2 is the one to act on first: it is the only finding that stops a player doing something they would
reasonably want to, and it lengthens every future session for no reason.

### The harness's own contradiction, disclosed

The run raised an alert at the start - *"the game didn't answer session within 60 s; the run played
the editor's own level"* - and then played Dama anyway. Every observation in its trace names a
`DamaGame` object (pointer targets on `DamaGame:/DamaBoard/Squares/...`, the HUD text, the profile
store, the win panel). The alert is recorded in `autopilot_session.md` section 5 rather than dropped,
because a harness that contradicts itself should be described, not smoothed over. The findings above
are drawn only from observations that name a DamaGame object or a Dama script.

**A side effect to disclose:** the run banked a sixth match into the local profile on this machine
(matches 5 -> 6, wins 4). The profile is a local file, and the Week 13 two-press RESET erases it.

---

## 7. Human playtest results - **not yet run**

**Sessions run to date: 0.** No volunteer has played this build. This section is empty because the
sessions have not happened yet, not because the results were thin.

| Participant | Build | T1 | T2 | T3 | T4 | T5 | Total time | Notes |
|---|---|---|---|---|---|---|---|---|
| `P1` | - | not run | not run | not run | not run | not run | - | - |
| `P2` | - | not run | not run | not run | not run | not run | - | - |
| `P3` | - | not run | not run | not run | not run | not run | - | - |
| `P4` | - | not run | not run | not run | not run | not run | - | - |
| `P5` | - | not run | not run | not run | not run | not run | - | - |

**What is deliberately absent from this table:** no participant codes are filled in, no times, no
success counts, no quotes, no ratings. An estimated row would read exactly like a measured one and
would be the worst thing in this submission, so the table says `not run` in every cell it cannot
honestly fill.

The kit that will fill it is complete and in `../Playtest/`:

| File | What it is |
|---|---|
| `01_consent_form.md` | What the participant is told and what they sign. Name-bearing sheets stay out of the submission |
| `02_session_script.md` | The moderator's exact wording, the five tasks, the no-help rule, the 3-minute stall rule, the debrief |
| `03_task_cards.md` | The participant's cards, one task per card, large type |
| `04_observation_sheet.md` | One sheet per participant: result, time, help, wrong taps, verbatim quotes, severity, ratings |
| `05_facilitator_checklist.md` | Setup, reset, environment, the four rules that keep notes worth having, and what to do when a session goes wrong |

**When the sessions are run**, this section gains one row per participant and the summary sentence
above changes to state how many were run, on which commit, and on what device. Until then the honest
statement is the one at the top of this section: zero sessions, zero results.

### Why the machine pre-test is not a substitute

It completed the five tasks in 53.2 seconds because it was *told what to do* - catch a piece, crown a
man. A volunteer will not be told, and the gap between those two situations is the entire reason to
run a session. The pre-test found four mechanical defects; it cannot say whether a rule is
understandable, how long understanding takes, or at what moment a player gives up.

---

## 8. Reproducing every check

`Evidence/HOW_TO_RERUN.txt` holds the exact procedure. Three editor menu items produce everything:

```
Tools > Dama > Week 14 > 1. Run Accessibility Audit     -> a11y_baseline.log, then a11y_after.log
Tools > Dama > Week 14 > 2. Apply Accessibility Fixes   -> the repairs, and a fingerprint to prove no-op
Tools > Dama > Week 14 > 3. Capture Week 14 Evidence    -> E1-E3 + feedback_lines.txt, in play mode
```

Two project quirks that will break a naive re-run, both recorded there: **domain reload on entering
play mode is disabled**, so Editor-time statics survive into play mode and every harness resets the
profile first; and the game sets **`Time.timeScale = 0`** when it pauses or ends, so any new timing
code must use unscaled time.

One trap specific to this week: the audit decides its own label with
`File.Exists(BaselinePath) ? "after" : "baseline"`. **Deleting `a11y_baseline.log` silently
relabels the next run as a baseline** - which would quietly destroy the comparison this whole
document rests on.

---

## 9. Evidence

| File | What it shows |
|---|---|
| `Evidence/a11y_baseline.log` | 57 checks before the fixes: 45 passed, 10 failed, 2 not measurable, with the 96 px derivation in the header |
| `Evidence/a11y_after.log` | 60 checks after: 58 passed, 0 failed, 2 not measurable, same thresholds and same control set plus the SOUND button |
| `Evidence/a11y_delta.txt` | The ten failures paired with their measured after values; 10 of 10 paired, 0 unmatched, generated mechanically |
| `Evidence/a11y_audit_20261126_115656.log` | The timestamped copy of the baseline run |
| `Evidence/a11y_audit_20261126_120637.log` | The timestamped copy of the after run |
| `Evidence/E1_pause_sound.png` | The pause panel showing `SOUND: ON`, nothing overlapping its neighbour |
| `Evidence/E2_hud_in_match.png` | The in-match HUD over the board: whose turn, and the counts |
| `Evidence/E3_profile_panel.png` | The profile panel with all five raised controls inside the canvas and none clipped |
| `Evidence/feedback_lines.txt` | 14 `[Feedback]` lines from one scripted match; 5 of 5 event kinds |
| `Evidence/autopilot_session.md` | The machine pre-test: five tasks, four findings, the harness alert, the banked match |
| `Evidence/week14_shots.txt` | The timestamped console transcript of the capture run |
| `Evidence/build_and_commit.txt` | The platform settings, the scene order, the commit id, and what was not built |
| `Evidence/HOW_TO_RERUN.txt` | How to regenerate every artefact above |
| `Playtest/01` - `05` | The human-session kit |

The three screenshots were taken in play mode from the real game canvas - not a mock-up - and each
capture is gated on its PNG existing with a non-zero length, because
`ScreenCapture.CaptureScreenshot` writes asynchronously. A shot requested and a shot written are not
the same moment, which is how the Week 13 evidence once ended up showing the wrong panel.

---

## 10. Disclosure record

| Activity | How it was done |
|---|---|
| SA6 analysis, PT6 write-up, reflection drafting | Written by me with AI assistance for structure and phrasing, then reviewed and corrected against the code and the logs |
| `A11yAudit.cs`, `AccessibilityBuild.cs`, `GameFeedback.cs`, `Week14Checks.cs` | Written with AI assistance, then read line by line; every threshold, hex value and measured ratio in this document is copied from a generated log, not from the plan |
| The two audit runs | Executed by the editor script; the tables in section 3 are `a11y_delta.txt` |
| The three screenshots | Captured in play mode by menu 3; they are the real canvas |
| The five audio clips | Generated from text descriptions by an audio tool, then assigned to five fields on `GameFeedback` and verified firing from the rules in `feedback_lines.txt`. No clip is a recording of anything, and none is claimed to be |
| The machine pre-test | Run by an automation agent in play mode; reported as a machine result, with its own contradiction disclosed in section 6 |
| The five human sessions | **Not run.** Section 7 says so in every cell |
| The decision to leave the 14 px captions alone | Mine: changing them would have re-authorised every capture and invalidated the audit baseline this document is built on. Deferred as M1 rather than done badly and late |

Nothing in the project was fabricated for this write-up. The build is short of five human sessions,
short of a device run and short of a screen-reader path, and each of those is stated where it belongs
rather than left out.

---

## 11. Build and repository state

| Item | Value |
|---|---|
| APK | none attached this week - the runtime evidence is the play-mode captures |
| Build target | Android, ARM64 only, IL2CPP, minimum API 26, package `com.defaultcompany.dama` |
| Scenes in build | `DamaGame.unity` (enabled, index 0), `SampleScene.unity` (disabled) |
| Signing | not applicable - no build produced |
| Repository branch | `main` |
| Code commit | `1998b0c` - "Week 14 PT6: accessibility audit, repair pass, audio feedback and play-mode evidence" |
| Documentation commit | recorded in `README.md`, which is the file that can hold a hash of the commit that contains these documents |

`Evidence/build_and_commit.txt` records the same settings and the same commit id.

---

## 12. How this meets the Week 14 laboratory requirements

| Module requirement | Where it is satisfied |
|---|---|
| A usability evaluation of the game | The five-task script, the observation sheet, and the machine pre-test that ran the same five tasks (sections 6, 7) |
| An accessibility assessment with a standard behind it | The WCAG 2.2 subset in `A11yAudit.cs`, the mapping table in SA6 section 6, and the two runs plus delta in section 3 |
| Evidence of improvement, not an assertion of it | `a11y_baseline.log` -> `a11y_after.log` -> `a11y_delta.txt`: 10 failures, 10 paired after values, 0 unmatched |
| Feedback so that events are not silent | Five clips, a haptic call on the capture, a worded `SOUND: ON/OFF` toggle persisted in `PlayerPrefs`, and a transcript proving 5 of 5 kinds fire (section 5) |
| A playtest kit a real session can run | `Playtest/01` - `05`, including consent, the no-help rule, the severity scale and the four rules that keep notes worth having |
| Honest reporting of what was not done | Section 7 (zero human sessions), section 5 (haptics unproven), section 3 (one near-miss at 3.01:1), section 6 (the harness alert and the banked match) |
| A reflective account of the work | `Guzman_Charibelle_Week14_Reflection.md` |
