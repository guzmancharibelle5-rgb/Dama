# Charibelle Guzman - MOBILE DEV 002 Individual Thematic Mobile Game

**Student:** Charibelle Guzman
**Section:** BSIT-4
**Course:** MOBILE DEV 002 Advanced Mobile Application Development, Module 5
**Theme:** Philippine Games and Sports
**Game:** Dama
**Engine:** Unity 6000.6.0f1, Universal Render Pipeline 17.6.0 (2D Renderer)

This folder holds one week per sub-folder. Each week contains the week's written work in
`others/`, the raw proof in `Evidence/`, and the printable documents at the week's root.

---

## Google Drive folder

The submission folder is named after the module's convention:

```
GUZMAN_CHARIBELLE_[ STUDENT NUMBER ]_Dama
```

The student number is deliberately left as a marked blank in this file rather than guessed. It
appears nowhere else in the submission, because no other document needs it.

---

## What the game is

*Dama* is the Philippine form of draughts, played on an 8x8 board by two people who take turns
on the same phone. Three rules carry the game:

- **A capture must be taken.** If a jump exists, non-capture moves are not offered at all.
- **A capture chains.** A piece that lands on another jump keeps going until the chain ends on
  its own.
- **The king flies.** A piece reaching the far row is crowned a *Dama* and may move any
  distance along any diagonal, in any direction.

A match ends when a side has no pieces left, or has pieces but no legal move.

---

## Weeks

| Week | Topic | Deliverables | State |
|---|---|---|---|
| 13 | Secure Player Data and Responsible Development | SA5, PT5, reflection, evidence | complete - see `Week_13/` |
| 14 | Game Usability, Accessibility and Playtesting | SA6, PT6, reflection, playtest kit, evidence | complete - see `Week_14/` |
| 15 | Game Testing, Defect Correction and Milestones | AS3 milestone, reflection, test logs, defect log, evidence | complete - see `Week_15/` |
| 16 | Mobile Game Profiling and Performance Optimization | SA6, AS3, reflection, three profile runs, regression logs, evidence | complete - see `Week_16/` |
| 17 | Release Preparation and Documentation | - | not started |
| 18 | Final Project, Demonstration and Defense | - | not started |

---

## Week 16 at a glance

The game was measured before it was touched. A single profiling harness runs one fixed ~75 s scenario - 32 turns driven
through the game's own `GetSelectableMoves` and `ExecuteMove`, three scripted restarts, vSync forced off - and records
every frame to a CSV plus a summary. The first thing it proved was that the byte counter it was using was blind: a
deliberate 64 KB allocation moved `GC.GetAllocatedBytesForCurrentThread()` by **0 B**, so per-call allocation is
measured instead by **amplitude scaling** (run the call n and 2n times, fit the line, report the slope with its
standard error). The baseline then failed one target and met two: the whole-board legality scan that runs at the end of
every turn allocated **6,975.9 B** against a budget of 0 B, the frame was 2.062 ms mean with **0 of 34,550 frames** over
the 33.3 ms budget, and the worst restart frame was 5.296 ms against 16.7 ms. Two optimizations followed, each measured
separately: the per-turn allocation is now **0.1 B +/- 0.1** (target met) and a restart allocates at most ~1.7 KB
against 5,596 B (churn removed). All three runs replayed the same 32 turns to the same move hash `0xE432E83D`, and the
Week 15 suite still reports 27 passed / 0 failed / 1 blocked with the accessibility audit at 105 / 0.

| Item | Value |
|---|---|
| Features | `Week16Profile` (one scenario, three runs), allocation-free move generation, piece pooling, `Week15Checks` regression seam, `A11yAudit.RunWeek16()` |
| Code commits | `f9503e8` (harness + baseline), `d6c4ba7` (optimization 1), `3b396b9` (optimization 2 + harness seams) |
| Documentation commit | recorded below once the documentation is committed |
| Targets | `Week_16/Evidence/targets.md` - PT-1 frame 33.3 ms, PT-2 0 B per turn, PT-3 restart under 16.7 ms, PT-4 record draw calls. Written **before** the baseline |
| Baseline | `Week_16/Evidence/profile_baseline/` - 2.062 ms mean, legality scan **6,975.9 B**, restart 5,701.2 B, worst restart frame 5.296 ms, hash `0xE432E83D` |
| After optimization 1 | `Week_16/Evidence/profile_after_alloc/` - legality scan **below the resolution of the method** (-0.1 B +/- 0.1), restart still ~5.6 KB |
| After optimization 2 | `Week_16/Evidence/profile_after_pool/` - restart **-1,665.5 B +/- 1,704.1** (consistent with 0 B), worst restart frame 5.154 ms |
| Bottlenecks | `Week_16/Evidence/bottlenecks.md` - two findings with raw values, the sprite-atlas candidate rejected with reasons, the optimization order |
| Regression | `Week_16/Evidence/test_logs_regression/` - 27 passed, 0 failed, 1 blocked (Week 15 logs untouched) |
| Audit | `Week_16/Evidence/a11y_week16_after.log` - 105 passed, 0 failed, 4 not measurable |
| Draw calls | `NOT MEASURABLE` in all three runs - the recorder was valid but every sample read 0 while the scene was drawing |
| Re-running everything | `Week_16/Evidence/HOW_TO_RERUN.txt` |

### Folder map

```
  Week_16/
    Guzman_Charibelle_SA6.pdf                the analysis: targets, instrument, baseline, bottlenecks, priorities
    Guzman_Charibelle_AS3.pdf                the change log, before/after metrics, trade-offs and regression record
    Guzman_Charibelle_Week16_Reflection.pdf  the weekly self-assessment
    others/                                  the same three as .md and print-ready .html
    Evidence/
      targets.md                             PT-1..PT-4, written before the baseline was taken
      method.md                              the scenario, the instrument, and what it cannot see
      metrics.md                             the three runs side by side, raw values, no percentage without them
      bottlenecks.md                         the findings, the rejected candidate, the order
      regression.md                          the 28 cases and the audit, re-run after both changes
      moves.txt                              the 32 turns every run replays
      profile_baseline/                      summary.txt, frames.csv - before any change
      profile_after_alloc/                   the same two files, after optimization 1
      profile_after_pool/                    the same two files, after optimization 2
      test_logs_regression/                  case_log.tsv, summary.txt, console.log - the Week 16 regression
      a11y_week16_after.log                  the audit after both changes, plus a timestamped copy
      build_and_commit.txt                   the three commits, what each contains, and the commands used
      HOW_TO_RERUN.txt                       the run order, the expected numbers and the known snags
```

---

## Week 15 at a glance

The game is now tested rather than trusted. A 28-case harness writes a machine-generated log, and it was run twice:
against the Week 14 code, where it reported 22 passed, **5 failed** and 1 blocked, and against the corrected build,
where the same 28 cases report **27 passed, 0 failed** and the same 1 blocked. The five failures name four defects,
all closed with a re-run of the same case. The accessibility audit grew three checks - caption size, label size and
non-text contrast - and immediately found three fills that had no visible edge; after the repair pass all 109 scored
rows are re-measured, **0 have regressed**, and 0 baseline rows were dropped. Two requirements are still covered only
indirectly, two cases cannot be run without a handset, and both facts are printed in the matrix rather than covered
by a passing neighbour.

| Item | Value |
|---|---|
| Features | `Week15Checks` (the 28 cases), `Week15Fixes` (the idempotent repair), `Week15Captures` (E1-E4), `A11yAudit` (extended to five check kinds) |
| Code commit | `207376b` - "Week 15 AS3: test harness, defect fixes and play-mode captures" |
| Documentation commit | `3f4bb95` - "Week 15 AS3 milestone, reflection, test logs, defect log and printed PDFs" |
| Baseline | `Week_15/Evidence/test_logs_baseline/` - 22 passed, **5 failed**, 1 blocked, 0 console errors |
| Final | `Week_15/Evidence/test_logs_final/` - **27 passed, 0 failed**, 1 blocked, 0 console errors |
| Defects | `Week_15/Evidence/defect_log.md` - DEF-15-01 (S2), DEF-15-02/03/04 (S3), **all closed**, 0 open |
| Audit before | `Week_15/Evidence/a11y_week15_baseline.log` - 83 passed, **9 failed**, 4 not measurable |
| Audit after | `Week_15/Evidence/a11y_week15_after.log` - 105 passed, **0 failed**, 4 not measurable |
| Delta | `Week_15/Evidence/a11y_delta_week15.txt` - 109 rows: 72 unchanged, 15 moved, 9 FIXED, **0 REGRESSED**, 0 dropped, 13 added |
| Captures | `Week_15/Evidence/E1..E4` - pause panel with MENU, end screen with the summary and MENU, profile at 20 px, mid-match board |
| Re-running everything | `Week_15/Evidence/HOW_TO_RERUN.txt` |

### Folder map

```
  Week_15/
    Guzman_Charibelle_AS3_Milestone.pdf      the test plan, executed tests, defect log and acceptance summary
    Guzman_Charibelle_Week15_Reflection.pdf  the weekly self-assessment
    others/                                  the same two as .md and print-ready .html
    Evidence/
      requirements.md                        REQ-01..REQ-22, written before any test ran
      acceptance_criteria.md                 CC-1..CC-4 and AC-5..AC-6, also written before the run
      traceability_matrix.md                 every requirement to its cases, or to a stated gap
      defect_log.md                          the four defects: reproduce, root cause, fix, retest
      test_logs_baseline/                    case_log.tsv, summary.txt, console.log - before the fixes
      test_logs_final/                       the same three files, after them
      a11y_week15_baseline.log               the audit before the repairs
      a11y_week15_after.log                  the audit after them
      a11y_delta_week15.txt                  every baseline row paired with its after value
      E1_pause_menu.png                      RESUME, RESTART, SOUND and MENU on the restart line
      E2_end_screen.png                      the end screen, naming both counts and the reason
      E3_profile_status.png                  the profile panel, status line at 20 px
      E4_mid_match.png                       the board after a jump, with the HUD counts
      week15_shots.txt                       the capture run in order, and the seeding caveat
      environment.md                         machine, scene, the two adb attempts, four stated limits
      build_and_commit.txt                   build id, platform settings, scene order, commit id
      HOW_TO_RERUN.txt                       how to regenerate every artefact above
```

---

## Week 14 at a glance

The game is now measured rather than assumed. An editor audit walks every label and control on the
canvas - including the panels that are switched off - and scores three things against published
thresholds: contrast (WCAG 2.2 SC 1.4.3), label fit, and a 96 canvas-pixel touch floor. Ten controls
failed. A repair pass fixed them by rule rather than by hand-picked colours, and re-running the audit
reports zero failures with every baseline failure paired to a measured after value. The game also has
sound for the first time: five clips, a haptic on a capture, and a `SOUND: ON/OFF` toggle.

| Item | Value |
|---|---|
| Features | `A11yAudit` (the measurement), `AccessibilityBuild` (the repair, idempotent), `GameFeedback` (five clips, one haptic, one toggle), `Week14Checks` (the captures) |
| Code commit | `1998b0c` - "Week 14 PT6: accessibility audit, repair pass, audio feedback and play-mode evidence" |
| Documentation commit | `58976b7` - "Week 14 SA6, PT6, reflection, playtest kit and the printed PDFs" |
| Audit before | `Week_14/Evidence/a11y_baseline.log` - 45 passed, **10 failed**, 2 not measurable |
| Audit after | `Week_14/Evidence/a11y_after.log` - 58 passed, **0 failed**, 2 not measurable |
| Delta | `Week_14/Evidence/a11y_delta.txt` - 10 of 10 failures paired, 0 unmatched |
| Feedback | `Week_14/Evidence/feedback_lines.txt` - move, capture, crown, win, lose: **5 of 5** kinds fired |
| Captures | `Week_14/Evidence/E1..E3` - pause panel with SOUND, the in-match HUD, the profile panel |
| Playtest | `Week_14/Playtest/01`-`05` - consent, script, task cards, observation sheet, checklist. **No human session has been run yet**, and `PT6` section 7 says so in every cell |
| Re-running everything | `Week_14/Evidence/HOW_TO_RERUN.txt` |

### Folder map

```
  Week_14/
    Guzman_Charibelle_SA6.pdf                usability and accessibility analysis
    Guzman_Charibelle_PT6.pdf                the audit, the delta, the feedback, the playtest kit
    Guzman_Charibelle_Week14_Reflection.pdf  the weekly self-assessment
    others/                                  the same three as .md and print-ready .html,
                                             plus SA6_audit_loop.svg
    Playtest/
      01_consent_form.md                     what a participant is told and signs
      02_session_script.md                   the five tasks and the moderator's wording
      03_task_cards.md                       the participant's cards
      04_observation_sheet.md                one per participant, filled the same day
      05_facilitator_checklist.md            setup, rules, and what to do when a session goes wrong
    Evidence/
      a11y_baseline.log                      the audit before the fixes
      a11y_after.log                         the audit after the fixes
      a11y_delta.txt                         every failure paired with its after value
      E1_pause_sound.png                     the pause panel, reading SOUND: ON
      E2_hud_in_match.png                    the HUD over the board
      E3_profile_panel.png                   the profile panel
      feedback_lines.txt                     the [Feedback] transcript, 5 of 5 kinds
      autopilot_session.md                   the machine pre-test - not a human session
      week14_shots.txt                       the Console transcript of the capture run
      build_and_commit.txt                   platform settings, scene order, commit id
      HOW_TO_RERUN.txt                       how to regenerate every artefact above
```

---

## Week 13 at a glance

The game now remembers the player: a name, the side they play, a win/loss/draw record with a
win streak, and a local best-times board of the five fastest victories. The record lives in a
signed JSON file inside the app's own sandbox and survives closing and reopening the game.

| Item | Value |
|---|---|
| Feature | `PlayerProfile` (signed save), `ProfilePanel` (the screen), `DamaBoard` (banks a finished match) |
| Code commit | `b6b477b` - "Week 13 PT5: signed player profile, PT5 data checks and play-mode evidence" |
| Documentation commit | `a215f34` - SA5, PT5, the reflection, their HTML and PDFs |
| Checks | `Week_13/Evidence/pt5_checks.log` - 14 assertions passed, 0 failed, 1 documented known limit |
| Captures | `Week_13/Evidence/E1..E4` - empty profile, record, best-times board, after reset |
| Re-running everything | `Week_13/Evidence/HOW_TO_RERUN.txt` |

### Folder map

```
Charibelle Guzman/
  README.md                                  this file
  Week_13/
    Guzman_Charibelle_SA5.pdf                theme, player and integration analysis
    Guzman_Charibelle_PT5.pdf                the feature, its integrity design and its proof
    Guzman_Charibelle_Week13_Reflection.pdf  the weekly self-assessment
    others/                                  the same three documents as .md and print-ready .html,
                                             plus the data-flow diagram as editable .svg
    Evidence/
      pt5_checks.log                         the fifteen rows with measured values
      E1_profile_empty.png                   fresh install
      E2_profile_stats.png                   after three matches
      E3_leaderboard.png                     the best-times board, five entries
      E4_after_reset.png                     after the two-press reset
      week13_shots.log                       timestamped record of the capture run
      build_and_commit.txt                   Android settings, scene order, commit id
      secrets_and_gitignore.txt              the credential scan and the ignore rules
      HOW_TO_RERUN.txt                       how to regenerate every artefact
```

---

## Known limitations

Listed here on purpose, in the week they were found, rather than left for someone else to
discover.

| Limitation | Why it stands |
|---|---|
| The save is **tamper-evident, not encrypted** | The HMAC key is a per-install salt on the same device as the file, and there is no server to check a record against. Recorded as `L15 KNOWN LIMIT` in the check log and in PT5 section 5, never counted as a pass |
| **No human playtest session has been run** | Week 14 shipped the kit - consent, script, five task cards, observation sheet, checklist - but zero volunteers have played. `PT6` section 7 states this in a sentence and in a table whose every cell reads `not run`. No time, count or quote was estimated to fill it |
| The **haptic is unproven** | `Handheld.Vibrate()` is a no-op in the Editor, and every Week 14 capture was taken in the Editor. The call is in the build; the buzz needs a handset, and neither PT6 nor the transcript claims it |
| **Text size does not scale** with the OS setting (WCAG 2.2 SC 1.4.4) | Week 15 raised the floor - five captions from 14 px to 22 px bold and `ProfilePanel/StatusText` from 17 to 20 - and made the audit measure it (`S` check, 20 px caption and 18 px label floors). What is still absent is a **dynamic type** path: the game picks its own sizes and does not follow a system font scale, so a player who enlarges text on the phone gets nothing. Recorded as DEF-15-02, closed for the size floor only |
| There is **no screen-reader path** | uGUI exposes no accessibility tree. A voice-over user cannot play this game. Fixing it needs a native Android bridge, which is not in this submission - stated in SA6 section 6 rather than implied |
| Contrast margins are **thin, not comfortable** | After the Week 15 repairs, three control fills pass the 3:1 non-text line at `3.16:1`, `3.17:1` and `3.19:1`, `ProfilePanel/SideButton` sits at `3.03:1`, and two labels pass at 4.7:1 against 4.5. Lightening a fill to give a control a visible edge **spends** the contrast of the white label on it - the pass is bounded on both sides - so the next edit to any of those fills or sizes puts them back under the line, and nothing will warn anyone. The audit is to be re-run before every submission |
| **No route back to the start screen** once a match begins - **fixed in Week 15** | Found by the Week 14 machine pre-test as M2 (S2), the highest-severity finding of that week, and carried into Week 15 as DEF-15-01. `Week15Fixes` builds `PausePanel/MenuButton` and `WinPanel/MenuButtonWin`; `DamaBoard.ReturnToMenu` restores `Time.timeScale = 1`, rebuilds the board and shows the title screen. Closed by `TC-FLOW-06` and `TC-RULE-11` passing in `Week_15/Evidence/test_logs_final/` |
| **Compatibility is untested** - no device, no APK | `TC-COMPAT-01` is **BLOCKED**, not passed, in both Week 15 logs. `adb devices` returns `List of devices attached` with no serial, and no APK was built this week by decision, so there is no second profile to install on an emulator either. The module asks for two profiles where resources permit; neither was available, and the command and its output are quoted in `Week_15/Evidence/environment.md` |
| Two requirements have **indirect coverage only** | `REQ-03` (tap to select, tap to move, turn passes) has no case driving `HandleTap` through a board change - the rules are exercised through `ExecuteMove` instead. `REQ-12` (the `lose` clip) is loaded and wired but **never fired**, because RED won every match the harness seeded. Both are marked as gaps in `Week_15/Evidence/traceability_matrix.md` rather than counted as passes |
| The capture harness **stalls if a script recompiles mid-run** | Domain reload on play-mode entry is disabled, so a recompile that lands while play mode is running leaves the harness with play mode on and no transcript - it happened three times while producing the Week 15 captures. `HOW_TO_RERUN.txt` says to edit nothing and wait for the compile before running it, and to check the transcript's "Run at" line against the PNG timestamps before trusting a set |
| **No APK was produced this week** either | Week 13, 14 and 15 all rest on the Editor in play mode, captured from the real scene and the real save file. The Android build settings are applied and recorded in each week's `build_and_commit.txt`, but "the game runs in the Editor" and "the game was installed on a phone" are different claims, and only the first is made here |
| The profile panel has **never run on a physical phone** | All captures are at the Editor's 1151x584 game view. A phone's aspect ratio and touch precision are untested |
| The UI's earlier scaler note was wrong | Week 13's README said the UI had **no** `CanvasScaler`. It has one: the Week 14 audit header reads `Scale With Screen Size, reference 1280x720, match 0.50`. That scaler is what makes the touch floor definable at all - 48 dp at xxhdpi is 144 physical px, and 144 / 1.50 = 96 canvas px - and it is why the audit measures canvas pixels against a stated reference instead of raw pixels. When it was added is not recorded; this row replaces the old claim rather than dating it |
| Commits are still **per week, not per step** | Week 13 said splitting commits per working step would start in Week 14, and it did not: Weeks 14 and 15 both landed as one code commit and one documentation commit. In Week 15 the cost is visible - the harness that produced the baseline and the harness that produced the final log are the same file, but the fixes it measured landed in the same commit as the fixes' own tests, so the history cannot show the baseline run happening first. The test logs can, and they do. Admitted rather than quietly dropped, in the Week 15 reflection section 4. **Week 16 fixed this for itself**: the harness and its baseline, the first optimization and the second optimization each landed as its own code commit (`f9503e8`, `d6c4ba7`, `3b396b9`), so the history can show the baseline run happening before either change |
| The project has **domain reload on play-mode entry disabled** | Fast iteration, but Editor-time statics survive into play mode. Every harness run resets the store to the default data directory first |
| Every Week 16 performance number is an **Editor number** | There is still no APK and no handset, so nothing is known about a phone's frame time, battery, thermals or GPU cost. The Editor also adds roughly 2 ms of its own work to every frame, and its draw-call counters read 0 for every sample while the scene was drawing - recorded as `NOT MEASURABLE`, never as 0. `Week_16/Evidence/method.md` names all of this as a limit of the evidence |
| The optimizations **cannot be shown to improve frame time** | The frame was 2.062 ms mean with 0 of 34,550 frames over a 33.3 ms budget **before** any change, and the idle mean moved with the frame mean between runs, so frame differences come from the Editor session, not the code. What is proven is that the per-turn allocation fell from 6,975.9 B to 0.1 B +/- 0.1 and that a restart's allocation fell from 5,596 B to at most ~1.7 KB. The frame claim is not made because it cannot be supported |
| A move generator now returns a **shared buffer** | `GetCaptureMoves`, `GetSimpleMoves` and `GetSelectableMoves` return a list that is valid only until that generator is next called. Every current caller consumes it immediately and stores nothing - audited call by call - but a future caller that keeps the list will break the board silently, with no error. The contract is written into the field comment in `DamaBoard.cs` because a comment is the only thing standing between the next reader and that bug |
| The piece pool **keeps 24 objects alive** for the session | A piece that leaves the board is parked under an inactive `PiecePool` root instead of being destroyed, which is what removed the restart churn - and what makes the memory cost of a long session slightly higher than before. The pool's cost is bounded and named in `Week_16/Evidence/metrics.md`, but a 20-minute match was never profiled, so the session-long effect is reasoned about rather than measured |

---

## Responsible AI use

Code, document structure and phrasing were produced with AI assistance and then read, run and
corrected against the actual build. Every measured value in PT5 section 6 is copied from
`pt5_checks.log`, which the harness generated; none is typed from memory. The same rule holds in Week
14: every ratio, size and paired value in PT6 section 3 comes from `a11y_delta.txt`, generated by
pairing the two logs mechanically. Week 15 adds a third: every status, ratio and size in `AS3_Milestone.pdf` is
copied from `case_log.tsv`, the two audit logs or the delta, and the milestone's section 14 lists what was assisted
and how each item was checked. The full disclosure tables are PT5 section 9, PT6 section 10 and AS3 section 14, Week 16's `AS3.pdf` section 10, and
all three reflections carry the required declaration.

Week 16 adds a fourth, and a sharper one: every number in `SA6.pdf` and `AS3.pdf` is copied from a
`summary.txt` the harness wrote, and the harness itself was **disproven before it was trusted** - a
byte counter was tested against a deliberate 64 KB allocation, failed, and was replaced rather than
reported. Where the evidence cannot support a claim (frame time, draw calls, anything about a phone)
the documents say so instead of quoting a number.

One use worth naming separately: the Week 14 machine pre-test was **run by an automation agent**, not
by a person. It is reported as such in `Week_14/Evidence/autopilot_session.md`, it is labelled a
machine pre-test everywhere it is mentioned, and it is never referred to as a playtest.

---

## Repository state

| Item | Value |
|---|---|
| Branch | `main` |
| Code commit (Week 13) | `b6b477b` |
| Documentation commit (Week 13) | `a215f34` |
| Code commit (Week 14) | `1998b0c` |
| Documentation commit (Week 14) | `58976b7` |
| Code commit (Week 15) | `207376b` |
| Documentation commit (Week 15) | `3f4bb95` |
| Build target | Android, ARM64 only, IL2CPP, minimum API 26, `com.defaultcompany.dama` |
| Scenes in build | `DamaGame.unity` (index 0), `SampleScene.unity` (disabled) |

`.gitignore` excludes `*.apk`, `*.aab`, `*.unitypackage`, `*.keystore`, `*.jks` and the
assistant's own working folders, so no build output or signing key can be committed by
accident. See `Week_13/Evidence/secrets_and_gitignore.txt`.
