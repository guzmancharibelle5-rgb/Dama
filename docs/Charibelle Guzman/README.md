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
| 17 | Release Preparation and Documentation | PT7 (front page, technical documentation, user guide, evidence index, reflection), three screens, four new controls, release identity, six captures, evidence | complete - see `Week_17/` |
| 18 | Final Project, Demonstration and Defense | PA2 package, technical report, user guide, evidence portfolio, originality declaration, 14-slide deck + speaker notes, reflection, nine-screen demonstration, defense pointer sheet | complete - see `Week_18/` |

No Week 19 or Week 20 folder exists: the work that came after PA2 was submitted is recorded in
**Post-submission revisions** below, as revisions to the Week 17 and Week 18 documents rather than as a
week that was never assessed.

---

## Post-submission revisions

Five pieces of work were done after PA2 was submitted. They are **not graded**, no Week 19 or Week 20
deliverable exists, and they are recorded here and in the two documents they touch - which is why
`Week_17/` (PT7) and `Week_18/` (PA2) now describe the tree rather than the `v1.0.0-pa2` commit. The
requirement rows are `REQ-23…REQ-30` in `Week_18/others/Guzman_Charibelle_PA2_Technical_Report.md` §12,
mirrored in the evidence portfolio §4a.

| Added | Where it lives | Evidence |
|---|---|---|
| One layout rhythm across the eight panels, and a CHOOSE MODE panel | `Assets/Scripts/Editor/MenuLayout.cs`, `Week19ModeUi.cs` - menu `Tools > Dama > Week 19 > 3. Rebalance Menus` | user guides §2.1 |
| RED or BLUE chosen and kept in the signed profile; a BLUE player's board turned to face them | `DamaBoard.SelectSide`, `ApplyBoardView`, `PlayerProfile.SetSide` - no new payload field, `SchemaVersion` still 1 | guides §2.1 and §5 |
| PLAYER vs AI at EASY, NORMAL and HARD | `DamaBoard.StartVsAi`, `DamaAI.Choose` | `Week_18/Evidence/ai_checks_postweek18.log` - rules parity 200 positions x 2 sides with 0 disagreements, identical mandatory capture at all three levels, HARD won 10/10 against EASY |
| A draw on threefold repetition, and a draw on 50 plies with no capture or promotion | `Assets/Scripts/Core/DamaPlyHistory.cs`, `DamaBoard.DeclareDraw`, `GameFeedback.PlayDraw` with `Assets/Audio/Generated/Dama_Draw.mp3` | `Week_18/Evidence/postweek18_features.log` cases A and B - `draws 2->3` and `draws 3->4` |
| UNDO in the pause panel, and HINT in the HUD | `PausePanel/UndoButton` + `DamaBoard.UndoPressed`, `HUDPanel/HintButton` + `DamaBoard.HintPressed` | same log, cases C and D; audit rows `UNDO` 272x96 at 6.24:1 and `HINT` 160x96 at 12.44:1 |
| A 120 ms move lerp and a capture pop | `DamaBoard.AnimatePieceTo`, `CapturePop` | **not measured** - the edit-mode cases snap the piece on purpose |

The audit over this tree reports **203 passed, 0 failed, 0 warnings, 6 not measurable**
(`Week_18/Evidence/a11y_week18_after.log`), against 148 / 0 / 0 / 4 at the PA2 submission. The first
re-audit of the revision found one failing row - `START` had drifted to a `#37D926` fill, 1.89:1 - and the
committed `#D92626` was restored and re-measured; both runs are kept, and the event is disclosed in the
evidence portfolio §7 rather than tidied away.

---

## Week 18 at a glance

The last week produced no new game code: it produced proof that the game that already exists does what the previous
seventeen weeks said it does. A checklist was written **first**, before any artefact, so the week was measured
against a list rather than a list written to fit the week. Then one harness walked the game through its own entry
points - `ShowHowTo`, `ShowCredits`, `ShowProfile`, `ResetPressed`, `StartGame`, `PauseGame`, `ResumeGame`,
`ReturnToMenu`, `CheckForWin` - and took **nine** screenshots, one per demonstration requirement, each with its own
wall-clock offset. The harness fails the run if any two images are byte-identical, which is not decoration: the
first version of it produced three names over one picture, and the gate caught it (`final_results.md` section 3).

All three gates were re-run on the same tree, and none of them overwrote a committed log - `Week17Checks` gained
`RunDirOverride`, `A11yAudit` gained `RunWeek18()`, `Week15Checks` gained `BeginWeek18()`. Release checks **5 / 0**,
regression **27 / 0 / 1 blocked** (`TC-COMPAT-01`, hardware, exactly as in Weeks 15-17), accessibility **148 passed,
0 failed, 4 not measurable**.

The submission is the tree at tag **`v1.0.0-pa2`** and **no Android package**. The version string did not move to
`1.0.0-pa2` on purpose: on Android the save file lives under a path built from `productName` and `companyName`, and
both on-screen version strings are derived from `Application.version` by `ReleaseInfo`, so re-labelling the build
would present every player with an empty profile or print a string the code did not generate. The tag marks the
submission where marking costs nothing.

| Item | Where |
|---|---|
| Checklist, written before anything else | `Week_18/Evidence/final_pre_submission_checklist.md` - the module's eight Week 18 procedure rows plus every named submission item, each PASS / PENDING / BLOCKED |
| Release record | `Week_18/Evidence/release_record.md` - identity, why the version did not move, the three independent reasons there is no APK, and the four steps that would close the row |
| Demonstration | `Week_18/Evidence/D1_title.png` … `D9_end_screen.png` - nine images, nine distinct SHA-256 values, transcript in `demo_rehearsal.txt` (run 2026-11-26 08:50:43 UTC) |
| Release checks | `Week_18/Evidence/test_logs_release/` - **5 passed, 0 failed, 0 console errors** (`TC-REL-01…05`) |
| Regression at `Week_18` | `Week_18/Evidence/test_logs_regression/` - **27 passed, 0 failed, 1 blocked** (`TC-COMPAT-01`) |
| Accessibility audit | `Week_18/Evidence/a11y_week18_after.log` - **148 passed, 0 failed, 0 warnings, 4 not measurable** |
| Results, faults and their limits | `Week_18/Evidence/final_results.md` - the three counts, the three harness faults with what each one cost, and section 4 naming what none of it proves |
| Repository hygiene | `Week_18/Evidence/secret_scan_week18.md` - no key, token, keystore or `.apk` tracked |
| Defense | `Week_18/Evidence/defense_prep.md` - 49 questions with file, class, commit, case and log pointers and **no answers**; `e3_record.md` blank until the viva |
| Documents | `Week_18/*.pdf` - PA2 (5 pp), technical report (7 pp), user guide (4 pp), evidence portfolio (5 pp), originality declaration (3 pp, signature line blank), deck (**14 slides at 16:9**, 960x540 pt), speaker notes (2 pp), reflection (2 pp). The report, user guide and portfolio each gained a page in the post-submission revision (5 / 3 / 4 pp at PA2) |
| Submission tag | `v1.0.0-pa2` on the PA2 commit; `repo_state.txt` records the log, the tags and the push result |

### Folder map

```
  Week_18/
    Guzman_Charibelle_PA2.pdf                     the submission front page and the project in one document
    Guzman_Charibelle_PA2_Technical_Report.pdf    rules, state machine, persistence, identity, testing, limits
    Guzman_Charibelle_PA2_User_Guide.pdf          the nine screens, the rules, the profile, troubleshooting
    Guzman_Charibelle_PA2_Evidence_Portfolio.pdf  every claim mapped to the file, case or hash that proves it
    Guzman_Charibelle_PA2_Originality_Declaration.pdf  complete except the signature line, which is blank
    Guzman_Charibelle_PA2_Presentation.pdf        14 slides, 16:9 landscape
    Guzman_Charibelle_PA2_Presentation_Speaker_Notes.pdf  the outline that goes with the deck
    Guzman_Charibelle_Week18_Reflection.pdf       the week's self-assessment
    others/
      *.md                                        the eight document bodies
      *.html                                      built with Tools > Dama > Submission > Build Week 18 HTML
      Guzman_Charibelle_PA2_Presentation_16x9.html  the deck's 16:9 page size, kept next to the A4 build
    Evidence/
      final_pre_submission_checklist.md           written first; every row re-read at the end
      release_record.md                           identity, the no-APK reasons, what would close them
      final_results.md                            the three gate counts, the nine captures, the three faults
      secret_scan_week18.md                       the hygiene scan
      demo_rehearsal.txt                          the demonstration run, in order, with offsets and hashes
      defense_prep.md                             questions and pointers; no scripted answers
      e3_record.md                                the viva record, deliberately blank
      D1_title.png … D9_end_screen.png            the nine demonstration images
      a11y_week18_after.log                       the authoritative audit (148 / 0 / 0, 4 not measurable)
      a11y_week18_audit_*.log                     timestamped copies
      test_logs_release/                          case_log.tsv, summary.txt, console.log - 5 / 0 / 0
      test_logs_regression/                       case_log.tsv, summary.txt, console.log - 27 / 0 / 1
```

---

## Week 17 at a glance

The build was frozen first (`git tag -a week16-final` on `4cce8c0`), then given an identity it cannot lie about:
`bundleVersion` went `1.0` → **`1.0.0-rc1`** and the Android version code `1` → **`2`**, while `productName` and
`companyName` were left alone on purpose - on Android the save file lives under a path built from them, so moving
them would look exactly like a wipe. Both on-screen version strings are **derived** from `Application.version` by
`ReleaseInfo`, never typed into a scene label, and `TC-REL-01` compares the title caption to `Application.version`
to keep it that way.

Three screens were missing and are now built and wired: **HOW TO PLAY** (1,122 characters fitted to 19 px, 399.3 px
of a 440 px box), **CREDITS** (903 characters, plus a build line naming the build and the application id) and
**EXIT**, which calls `DamaBoard.QuitGame()`. The title row is HOW TO PLAY / START / CREDITS with PROFILE and EXIT
beneath, and both new panels are state-driven, so they cannot leak into a match.

| Item | Where |
|---|---|
| Code commit | `b8e69b9` - "Week 17: release identity 1.0.0-rc1, HOW TO PLAY / CREDITS / EXIT panels, release checks and captures" |
| Documentation commit | `d2d3ad8` - "Week 17: PT7 package, release evidence, six captures and printed PDFs" |
| Release identity | `Week_17/Evidence/build_record.txt` - `1.0.0-rc1` / code 2, `productName` `dama` and `companyName` `DefaultCompany` unchanged |
| Release checks | `Week_17/Evidence/test_logs_release/` - **5 passed, 0 failed, 0 console errors** (`TC-REL-01…05`) |
| Regression at `Week_17` | `Week_17/Evidence/test_logs_regression/` - **27 passed, 0 failed, 1 blocked** (`TC-COMPAT-01`), 0 console errors |
| Accessibility audit | `Week_17/Evidence/a11y_week17_after.log` - **148 passed, 0 failed, 0 warnings, 4 not measurable** (Week 16 was 105) |
| Captures | `Week_17/Evidence/E1…E6.png` - six images, six distinct SHA-256 values, transcript in `week17_shots.txt` |
| Defects found by the gates | `Week_17/Evidence/issue_disposition.md` - **DEF-17-05** (a second builder run left a duplicate HOW TO PLAY / CREDITS / EXIT under `StartPanel`, which `TC-A11Y-02` reported as three controls overlapping themselves) and **DEF-17-06** (the release check read the wrong field of a uGUI listener and reported `null`). Both fixed, both disclosed |
| Install on a device | **BLOCKED** - no APK was built and `adb devices` (Unity's bundled platform-tools) lists no serial. `Week_17/Evidence/clean_install.md` records the attempt, the exact command output, and what the Editor evidence does not prove |
| Repository hygiene | `Week_17/Evidence/secret_scan.md` + `secret_scan_raw.txt` - no key, token, keystore or `.env` is tracked; the signing settings are empty (debug signing only) |
| Documents | `Week_17/*.pdf` - PT7 (3 pp), technical documentation (6 pp), user guide (5 pp, one more than the released 4 after the post-submission revision), evidence index (4 pp), reflection (3 pp) |
| Re-running everything | `Week_17/others/Guzman_Charibelle_PT7_Evidence_Index.md` section 8 |

### Folder map

```
  Week_17/
    Guzman_Charibelle_PT7.pdf                          the front page of the release package
    Guzman_Charibelle_PT7_Technical_Documentation.pdf  identity mechanism, state machine, builder, defects, gates
    Guzman_Charibelle_PT7_User_Guide.pdf               the six screens, the rules, the profile, troubleshooting
    Guzman_Charibelle_PT7_Evidence_Index.pdf           every claim mapped to the file and case that proves it
    Guzman_Charibelle_Week17_Reflection.pdf            the week's self-assessment
    others/
      *.md                                             the five document bodies
      *.html                                           built with Tools > Dama > Submission > Build Week 17 HTML
    Evidence/
      readiness_results.md                             the three gate counts and what they do not prove
      feature_freeze_checklist.md                      what was frozen and the change sets accepted so far
      issue_disposition.md                             DEF-17-01…06 and ENV-17-01/02 with outcomes
      build_record.txt                                 identity, machine, scene, commit, the BLOCKED APK step
      clean_install.md                                 the install step that could not run, and why
      secret_scan.md, secret_scan_raw.txt              the hygiene scan and its unedited output
      architecture.svg                                 scene, scripts, identity source and harnesses
      a11y_week17_after.log                            the authoritative audit (148 / 0 / 0, 4 not measurable)
      a11y_week17_audit_*.log                          timestamped copies: one pre-fix, one post-fix
      test_logs_release/                               case_log.tsv, summary.txt, console.log - 5 / 0 / 0
      test_logs_regression/                            case_log.tsv, summary.txt, console.log - 27 / 0 / 1
      E1_title.png … E6_end_screen.png                 the six submission images
      week17_shots.txt                                 capture transcript with sizes and SHA-256 of all six
```

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
| Code commits | `aacfc7f` (harness + baseline), `356e99f` (optimization 1), `bb7e7f3` (optimization 2 + harness seams) |
| Documentation commit | `e5b6c1a` - "Week 16 SA6 analysis, AS3 performance record, reflection and printed PDFs" |
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
| Code commit | `fdb3cc1` - "Week 15 AS3: test harness, defect fixes and play-mode captures" |
| Documentation commit | `3761802` - "Week 15 AS3 milestone, reflection, test logs, defect log and printed PDFs" |
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
| Code commit | `1e27cc2` - "Week 14 PT6: accessibility audit, repair pass, audio feedback and play-mode evidence" |
| Documentation commit | `8adbce3` - "Week 14 SA6, PT6, reflection, playtest kit and the printed PDFs" |
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
| Code commit | `b9a612d` - "Week 13 PT5: signed player profile, PT5 data checks and play-mode evidence" |
| Documentation commit | `0f01608` - SA5, PT5, the reflection, their HTML and PDFs |
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
| Commits are still **per week, not per step** | Week 13 said splitting commits per working step would start in Week 14, and it did not: Weeks 14 and 15 both landed as one code commit and one documentation commit. In Week 15 the cost is visible - the harness that produced the baseline and the harness that produced the final log are the same file, but the fixes it measured landed in the same commit as the fixes' own tests, so the history cannot show the baseline run happening first. The test logs can, and they do. Admitted rather than quietly dropped, in the Week 15 reflection section 4. **Week 16 fixed this for itself**: the harness and its baseline, the first optimization and the second optimization each landed as its own code commit (`aacfc7f`, `356e99f`, `bb7e7f3`), so the history can show the baseline run happening before either change |
| The project has **domain reload on play-mode entry disabled** | Fast iteration, but Editor-time statics survive into play mode. Every harness run resets the store to the default data directory first |
| Every Week 16 performance number is an **Editor number** | There is still no APK and no handset, so nothing is known about a phone's frame time, battery, thermals or GPU cost. The Editor also adds roughly 2 ms of its own work to every frame, and its draw-call counters read 0 for every sample while the scene was drawing - recorded as `NOT MEASURABLE`, never as 0. `Week_16/Evidence/method.md` names all of this as a limit of the evidence |
| The optimizations **cannot be shown to improve frame time** | The frame was 2.062 ms mean with 0 of 34,550 frames over a 33.3 ms budget **before** any change, and the idle mean moved with the frame mean between runs, so frame differences come from the Editor session, not the code. What is proven is that the per-turn allocation fell from 6,975.9 B to 0.1 B +/- 0.1 and that a restart's allocation fell from 5,596 B to at most ~1.7 KB. The frame claim is not made because it cannot be supported |
| A move generator now returns a **shared buffer** | `GetCaptureMoves`, `GetSimpleMoves` and `GetSelectableMoves` return a list that is valid only until that generator is next called. Every current caller consumes it immediately and stores nothing - audited call by call - but a future caller that keeps the list will break the board silently, with no error. The contract is written into the field comment in `DamaBoard.cs` because a comment is the only thing standing between the next reader and that bug |
| The piece pool **keeps 24 objects alive** for the session | A piece that leaves the board is parked under an inactive `PiecePool` root instead of being destroyed, which is what removed the restart churn - and what makes the memory cost of a long session slightly higher than before. The pool's cost is bounded and named in `Week_16/Evidence/metrics.md`, but a 20-minute match was never profiled, so the session-long effect is reasoned about rather than measured |
| **No APK and no handset again** - the release candidate was never installed | `TC-COMPAT-01` stays **BLOCKED**, not passed. `adb devices` was run from Unity's own platform-tools (`...\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe`, since `adb` is not on `PATH`) and returned `List of devices attached` with nothing under it; no Android package was produced this week either. `Week_17/Evidence/clean_install.md` quotes the command, the output and the signing settings, and lists what the Editor evidence does and does not prove |
| The version caption reads **`· Editor`** in every Week 17 capture | `ReleaseInfo.Platform` reads the platform it is running on, so an Android build would read `· Android`. Nobody has seen that string, because no build exists. The mechanism is proven (`TC-REL-01` compares the caption to `Application.version`); the Android value is inferred from `Application.platform`, not observed |
| **EXIT is bound, not proven to quit on Android** | `TC-REL-04` proves `StartPanel/ExitButton` holds one listener to `DamaBoard.QuitGame`. `QuitGame` calls `EditorApplication.isPlaying = false` under the editor and `Application.Quit()` in a build - and `Application.Quit()` has never run, because no build has. The check proves the wire, not the behaviour on a phone |
| A scene builder **run twice can corrupt the scene** | It did: the release-panel builder had no cleanup for the three new title buttons, so its second run left a duplicate set and `TC-A11Y-02` reported three controls overlapping themselves (`DEF-17-05`). `DestroyIfExists` now loops until nothing with that name is left, and the regression catches the symptom - but every future builder has the same failure mode until it is written with the same discipline |
| The **PA2 submission carries no binary** | Tag `v1.0.0-pa2` marks a source tree and nothing else. No APK, AAB or Windows package exists, so "install the submitted build" cannot be done with this submission by anyone, including the student: `release_record.md` section 3 gives three independent reasons, and section 5 gives the four steps that would produce one |
| The **defense was not sat** | Item 7 of the module's Week 18 procedure is a viva held under supervision. The package ships the pointer sheet (`defense_prep.md`) and the blank record (`e3_record.md`); a blank record means not run, and it must not be read as a pass |
| The **originality declaration is unsigned in this copy** | The document is complete except for the signature line, which is deliberately empty. Printing a signature from this toolchain would be a fabricated signature, so `final_pre_submission_checklist.md` section 3.3 records the row as unsigned rather than attaching a name to it |
| The **demonstration is a harness, not a person** | All nine images come from one scripted run of the game's own entry points at the Editor's 1151x584 game view, and `demo_rehearsal.txt` says so in its header. It proves the screens exist, are reachable and are legible at that resolution; it proves nothing about a thumb on a phone |
| The Week 18 harness **added three rows to the real profile - and the gate then erased them** | The end screen is produced by the game's own win check, and that check banks the match. During the first three harness runs the real store was restored too early, so each run added one RED win: it read 5 matches before the first run and 8 after the third. The committed harness restores the directory only after D9, so it adds nothing. The three rows are gone anyway, because the regression gate runs its data cases against the default store - `TC-DATA-01` resets, `TC-DATA-02` and `03` bank and reset again, `TC-DATA-04` quarantines a deliberately corrupt file - so the store now holds a test record (257 B, 0 matches, name `QA-LOCAL`, plus `profile.rejected.json`). `final_results.md` section 3 item 4 is the correction, including why the first version of that sentence was checked against the transcript rather than the file |
| The **deck is a PDF, not a `.pptx`** | 14 slides at 960x540 pt (16:9) printed from HTML by headless Edge, because no presentation toolchain is in this project. It presents and it prints; it cannot be edited by the instructor, and the document says so where the format is described |
| The **move animation is not measured** | `DamaBoard.AnimatePieceTo` runs a 120 ms unscaled ease-out and `CapturePop` a 120 ms scale bump, but the edit-mode harnesses snap the piece on purpose and no log records the travel. It was watched in the Editor, which is not the same as measured, and REQ-30 says so |

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
| Code commit (Week 13) | `b9a612d` |
| Documentation commit (Week 13) | `0f01608` |
| Code commit (Week 14) | `1e27cc2` |
| Documentation commit (Week 14) | `8adbce3` |
| Code commit (Week 15) | `fdb3cc1` |
| Documentation commit (Week 15) | `3761802` |
| Code commits (Week 16) | `aacfc7f` (profiling harness + baseline), `356e99f` (optimization 1), `bb7e7f3` (optimization 2) |
| Documentation commit (Week 16) | `e5b6c1a` |
| Code commit (Week 17) | `b8e69b9` |
| Documentation commit (Week 17) | `d2d3ad8` |
| Code commit (Week 18) | `2577a33` - demonstration harness, the Week 18 seams in `A11yAudit` / `Week15Checks` / `Week17Checks`, and the `Week_18` entry in `SubmissionDocBuilder` |
| Documentation commit (Week 18) | `9a28ab9` - the eight documents and their HTML, the eight PDFs, the nine captures, the evidence files and this README |
| Code commit (post-submission revision) | `c4e3f18` - side choice and turned board, PLAYER vs AI, the draw rule, UNDO, HINT, the move animation and `Dama_Draw.mp3` |
| Documentation commit (post-submission revision) | `08a2152` - `REQ-23…REQ-30` in the PA2 report and evidence portfolio, both user guides reworded, HTML and four PDFs rebuilt, the four audit logs and the AI transcript |
| Author of every commit | `guzmancharibelle5-rgb <guzmancharibelle5@gmail.com>`. The machine's default identity was `Charry <zedrichzaivan753@gmail.com>`, which GitHub renders as another account; on 2026-11-28 the whole history was rewritten under the repository's own account - all thirty-two commits at once, with every tree, message and author and committer date preserved - and every hash these documents cite was remapped (table in `Week_18/Evidence/repo_state_postweek18.txt` §3) |
| Submission tag | `v1.0.0-pa2` - annotated, on the Week 18 closing commit, deleted and recreated on the rewritten commit on 2026-11-28 with its original tagger date; `week16-final` likewise. `Week_18/Evidence/repo_state.txt` records the original capture, `repo_state_postweek18.txt` §4 the recreated tags |
| Remote state | `origin/main` carries the whole project, both tags and the Week 18 closing records. The pushes of 2026-11-28 are recorded verbatim in `Week_18/Evidence/repo_state_postweek18.txt`: the first push of the post-submission work, the two forced updates that corrected its identity, and the final force-push that re-authored all thirty-two commits and moved both tags. `git rev-parse HEAD` and `git rev-parse refs/remotes/origin/main` agree; the values are in §5 of that file |
| Build target | Android, ARM64 only, IL2CPP, minimum API 26, `com.defaultcompany.dama` |
| Scenes in build | `DamaGame.unity` (index 0), `SampleScene.unity` (disabled) |

`.gitignore` excludes `*.apk`, `*.aab`, `*.unitypackage`, `*.keystore`, `*.jks` and the
assistant's own working folders, so no build output or signing key can be committed by
accident. See `Week_13/Evidence/secrets_and_gitignore.txt`.
