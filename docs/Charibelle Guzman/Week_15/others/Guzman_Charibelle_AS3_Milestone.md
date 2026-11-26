# AS3 Milestone - Test Plan, Executed Tests, Defect Log and Acceptance Evidence

**Game:** Dama
**Student:** Charibelle Guzman
**Section:** BSIT-4
**Course:** MOBILE DEV 002 Advanced Mobile Application Development, Module 5
**Theme:** Philippine Games and Sports
**Week:** 15 - Game Testing, Defect Correction and Quality Evidence
**Build:** `dama-editor-w15-rc1` - scene `DamaGame.unity` at commit `207376b`, executed in the Unity Editor in play mode
**Platform range:** Android 8.0 (API 26) and above, ARM64, IL2CPP, `com.defaultcompany.dama`
**Engine:** Unity 6000.6.0f1, Universal Render Pipeline 17.6.0 (2D Renderer), C#
**Repository:** local Git repository, branch `main`, code commit `207376b`
**Components under test:** `Assets/Scripts/Editor/Week15Checks.cs`, `Assets/Scripts/Editor/A11yAudit.cs`,
`Assets/Scripts/Editor/Week15Fixes.cs`, `Assets/Scripts/Editor/Week15Captures.cs`, `Assets/Scripts/DamaBoard.cs`,
`Assets/Scripts/Core/PlayerProfile.cs`, `Assets/Scenes/DamaGame.unity`

---

## 1. What this milestone is, and the build it describes

This document is the acceptance record for one specific build. The build is identified by the scene, the
commit and the way it was executed, because a test result that does not name its subject proves nothing:

| | |
|---|---|
| Build identifier | `dama-editor-w15-rc1` |
| Source revision | `207376b` ("Week 15 AS3: test harness, defect fixes and play-mode captures") |
| Scene | `Assets/Scenes/DamaGame.unity`, build index 0 |
| Execution mode | Unity Editor 6000.6.0f1, play mode, game view 1151 x 584 |
| Save location | `C:/Users/Zedrich/AppData/LocalLow/DefaultCompany/dama/Dama/profile.json` |
| Produced | 2026-11-26 |

Two runs of the **same harness file** are the spine of the record. The baseline run was executed against the
code as Week 14 left it, before a single line was changed. The final run was executed against the code at the
commit above. Both write a machine-generated case log, and the accessibility audit writes a second pair of
logs that are paired row by row. Every status in this document is a cell copied from one of those files.

| Artefact | Run at (UTC) | Result |
|---|---|---|
| `Evidence/test_logs_baseline/` | 2026-11-26 05:13 | 22 passed, **5 failed**, 1 blocked, 0 console errors |
| `Evidence/a11y_week15_baseline.log` | 2026-11-26 05:10 | 83 passed, **9 failed**, 0 warnings, 4 not measurable |
| `Evidence/test_logs_final/` | 2026-11-26 05:15 | **27 passed**, 0 failed, 1 blocked, 0 console errors |
| `Evidence/a11y_week15_after.log` | 2026-11-26 05:16 | **105 passed**, 0 failed, 0 warnings, 4 not measurable |
| `Evidence/a11y_delta_week15.txt` | generated | 109 rows re-measured: 72 unchanged, 15 moved, **9 FIXED**, **0 REGRESSED**, 0 dropped, 13 added |

---

## 2. The test environment, and what it cannot decide

| Item | Value |
|---|---|
| Machine | Windows 10 Home 10.0.19045, x64, 16,277 MB RAM |
| Editor | Unity 6000.6.0f1, Windows Editor target |
| Render pipeline | URP 17.6.0, 2D Renderer |
| Input | Input System 1.20.0, new input path only |
| UI | uGUI 2.6.0; `CanvasScaler` Scale With Screen Size, reference 1280 x 720, match 0.50 |
| Canvas rect | 1347.73 x 683.82 |
| Game view | 1151 x 584 |
| Domain reload | **disabled** on play-mode entry |

The canvas scaler is what makes a touch floor definable at all: 48 dp at xxhdpi is 144 physical pixels, and
144 / 1.50 = **96 canvas pixels**. That derivation is written into the header of both audit logs so the number
is not an opinion. The two type floors - 20 px for a caption, 18 px for any other label - are a **project
decision**, not a WCAG number, and both audit headers say so: 20 px is roughly 13.3 dp at this scale.

What the environment cannot decide is stated in `Evidence/environment.md` and repeated here, because an
acceptance record that hides its blind spots is worth less than one that names them:

- **No handset.** `adb devices` returns `List of devices attached` with no serial. No device, no logcat.
- **No APK.** Editor-only evidence by decision; the Android settings are applied and recorded but nothing was
  installed.
- **One renderer, one resolution.** Every measurement is against the 1280 x 720 reference on one machine.
- **Domain reload disabled** means Editor-time statics survive into play mode, so both harnesses reset the
  profile store to the default data directory before seeding. That reset wipes the real local profile; the
  consequence is recorded in `environment.md` section 3 rather than fixed by deleting the code.

---

## 3. Acceptance criteria, written before the run

The module's completion checkpoint is the frame: no unresolved defect may cause data loss, prevent completion
of the core gameplay loop, expose another player's information, or repeatedly crash the application. Those four
conditions were turned into four criteria, and two more were added from the Week 14 audit, **before** any test
was executed. The order matters: a criterion written after seeing the result is a rationalisation.

| Id | Criterion | Final |
|---|---|---|
| CC-1 | A finished match, a saved name and a saved side survive a reload; a corrupt save does not lose the profile silently | **PASS** (`TC-DATA-01`, `TC-DATA-04`) |
| CC-2 | A match can be started, played, ended and restarted without leaving the app | **PASS** (`TC-FLOW-03`, `TC-RULE-07`, `TC-RULE-09`, `TC-RULE-11`) |
| CC-3 | No other player's information is exposed, because there is no other player and no network path | **PASS** (`TC-DATA-05`, network absence stated in `environment.md`) |
| CC-4 | No repeated crash: the console must be clean at the end of a full run | **PASS** (0 console errors in both runs) |
| AC-5 | Every control is at least 96 x 96 canvas px and nothing overlaps | **PASS** (13 buttons, 1 input field, 0 below floor, 0 overlapping pairs) |
| AC-6 | Every caption is at least 20 px, every other label at least 18 px, every control fill at least 3:1 | **PASS** (0 `S` failures, 0 `N` failures after the fixes) |

---

## 4. The test suite

One harness, `Assets/Scripts/Editor/Week15Checks.cs`, 1021 lines, two menu items:

- `Tools > Dama > Week 15 > 1. Baseline Test Run`
- `Tools > Dama > Week 15 > 2. Final Test Run`

Both write `case_log.tsv`, `summary.txt` and `console.log` into `Evidence/test_logs_baseline/` or
`Evidence/test_logs_final/`. There are no two scripts to drift apart: the case list, the seeding helpers and the
assertions are the same code, and only the output folder differs.

| Group | Cases | What it asks |
|---|---|---|
| `TC-FLOW` | 6 | the title screen, starting a match, pause and resume, the sound toggle, leaving a match |
| `TC-RULE` | 11 | mandatory capture, the jump, the chain, both promotion rows, the flying Dama, both win conditions, the end screen's three obligations |
| `TC-DATA` | 5 | reload of name and side, banking once, reset, a corrupt save, the sandbox |
| `TC-A11Y` | 3 | the 96 px floor, overlap, the type floor |
| `TC-BOUND` | 2 | 100 rapid taps, 50 pause/resume cycles |
| `TC-COMPAT` | 1 | a second device or configuration profile |

Twenty-eight cases. Every one of them records a **measured value**, not a sentence: `pieces=24`, `turn='Turn: RED'`,
`offered=[(1,1), (2,2), ... (6,6)]`, `repeatRefused=True`, `timeScale=1.00`. A case that can only print `PASS` is a
case that cannot fail.

The rules are seeded by reflection into the board's grid and then driven through the game's own entry points
(`ExecuteMove`, `EndTurn`, `CheckForWin`), so a case exercises the same code a tap does, not a parallel copy of it.

---

## 5. Traceability, at a glance

The full requirements-side matrix is `Evidence/traceability_matrix.md`: 22 requirements, each with its cases,
its baseline status, its final status and its evidence. The case-side view is below, and it is the honest one,
because it shows the five failures in the column where they were found.

| Case | REQ | Baseline | Final |
|---|---|---|---|
| `TC-FLOW-01` | REQ-01 | PASS | PASS |
| `TC-FLOW-02` | REQ-01 | PASS | PASS |
| `TC-FLOW-03` | REQ-02 | PASS | PASS |
| `TC-FLOW-04` | REQ-10 | PASS | PASS |
| `TC-FLOW-05` | REQ-11 | PASS | PASS |
| `TC-FLOW-06` | REQ-17 | **FAIL** | PASS |
| `TC-RULE-01` | REQ-04 | PASS | PASS |
| `TC-RULE-02` | REQ-04 | PASS | PASS |
| `TC-RULE-03` | REQ-05 | PASS | PASS |
| `TC-RULE-04` | REQ-06 | PASS | PASS |
| `TC-RULE-05` | REQ-06 | PASS | PASS |
| `TC-RULE-06` | REQ-06 | PASS | PASS |
| `TC-RULE-07` | REQ-07 | PASS | PASS |
| `TC-RULE-08` | REQ-09 | **FAIL** | PASS |
| `TC-RULE-09` | REQ-08 | PASS | PASS |
| `TC-RULE-10` | REQ-09 | **FAIL** | PASS |
| `TC-RULE-11` | REQ-09 | **FAIL** | PASS |
| `TC-DATA-01` | REQ-13 | PASS | PASS |
| `TC-DATA-02` | REQ-14 | PASS | PASS |
| `TC-DATA-03` | REQ-15 | PASS | PASS |
| `TC-DATA-04` | REQ-16 | PASS | PASS |
| `TC-DATA-05` | REQ-22 | PASS | PASS |
| `TC-A11Y-01` | REQ-19 | PASS | PASS |
| `TC-A11Y-02` | REQ-19 | PASS | PASS |
| `TC-A11Y-03` | REQ-20 | **FAIL** | PASS |
| `TC-BOUND-01` | REQ-21 | PASS | PASS |
| `TC-BOUND-02` | REQ-10 | PASS | PASS |
| `TC-COMPAT-01` | - | **BLOCKED** | **BLOCKED** |

Two requirements are covered **indirectly** and are marked as gaps rather than passes, in the matrix and here:

- **REQ-03** (tap a piece, tap a highlighted square, the turn passes) has no case that drives `HandleTap` through a
  board change. `TC-RULE-01..06` exercise the same move engine by reflection and `TC-BOUND-01` taps a real piece
  100 times without changing the board, but neither is the same claim.
- **REQ-12** (move, capture, crown, win **and lose** each play their own clip) has no numbered case. Four of the
  five clips are observed in `test_logs_final/console.log`; the `lose` clip is loaded and wired but **never fired**,
  because RED won every seeded match. It is written as a partial, not a pass.

---

## 6. The baseline run: five failures, in the harness's own words

The five failures were not predicted. They were what the first execution produced:

```
TC-FLOW-06    FAIL    PausePanel/MenuButton present=False, DamaBoard.ReturnToMenu present=False
TC-RULE-08    FAIL    WinPanel/SummaryText missing
TC-RULE-10    FAIL    WinPanel/SummaryText missing
TC-RULE-11    FAIL    RestartButtonWin=True MenuButtonWin=False
TC-A11Y-03    FAIL    5 caption(s), 1 label(s) below floor
                      Text=14px(need 20); Text=14px(need 20); Text=14px(need 20);
                      Text=14px(need 20); Text=14px(need 20); StatusText=17px(need 18);
```

The new accessibility checks found three more failures the Week 14 audit had no way to see, all of them a
control whose own fill could not be told from the panel behind it:

```
N  FAIL  PausePanel/SoundButton      ratio=2.26:1 need=3.0 fill=#404759 panel=#000000
N  FAIL  ProfilePanel/ResetButton    ratio=2.55:1 need=3.0 fill=#8C2E2E panel=#000000
N  FAIL  ProfilePanel/BackButton     ratio=2.27:1 need=3.0 fill=#404759 panel=#000000
```

Nine audit failures and five case failures describe **four** defects: two cases can fail for one defect, and one
check kind can report the same defect on three controls.

---

## 7. The defects, and how each was corrected

Full reproduction steps, expected and actual results, and the paired retest quotes are in
`Evidence/defect_log.md`. This is the summary an assessor needs:

| ID | Sev | What it was | Requirement | Correction | Retest |
|---|---|---|---|---|---|
| DEF-15-01 | **S2** | A match could not be left for the title screen; the pause menu and the end screen had no route out | REQ-17, REQ-09 | `Week15Fixes.EnsureButton` builds `PausePanel/MenuButton` and `WinPanel/MenuButtonWin` beside RESTART; `DamaBoard.ReturnToMenu` restores `Time.timeScale = 1`, rebuilds the board, shows `GameState.Ready` | `TC-FLOW-06` PASS `timeScale=1.00`; `TC-RULE-11` PASS `MenuButtonWin=True` |
| DEF-15-02 | S3 | Five button captions at 14 px, `ProfilePanel/StatusText` at 17 px - the smallest text on screen was the caption of the control it sat on | REQ-20 | `Week15Fixes.RaiseTypeSizes`: a caption below 20 px becomes 22 px bold, a label below 18 px becomes 20 px; nothing already passing is touched | `TC-A11Y-03` PASS `0 caption(s), 0 label(s) below floor`; six `S` rows `FIXED` |
| DEF-15-03 | S3 | The end screen named the winner and nothing else - the two ways a match can end were indistinguishable | REQ-09 | `Week15Fixes.EnsureSummaryText` builds `WinPanel/SummaryText` in the only free band; `DamaBoard.DeclareWinner` derives the loser, both counts and the reason from the board and writes them | `TC-RULE-08` PASS `RED WINS.  RED 1-0, BLUE has no pieces left.`; `TC-RULE-10` PASS `RED WINS.  RED 1-1, BLUE has no legal move.` |
| DEF-15-04 | S3 | Three control fills at roughly half the required ratio against the black overlay, and no check that could report them | REQ-20 (WCAG 2.2 SC 1.4.11, 3:1) | `A11yAudit.AuditNonTextContrast` is the missing check; `Week15Fixes.LiftFills` lightens a failing fill in its own hue until it clears 3.15:1, but never so far that its white label drops below 4.6:1 | three `N` rows `FIXED` at 3.19:1, 3.17:1, 3.16:1 |

Open defects after the pass: **none**. Open S1/S2: **none**.

Two decisions inside those corrections are worth naming, because they are the ones a reviewer would otherwise
have to guess at:

- **`DeclareWinner` keeps a single parameter.** The plan said it would take `(winner, reason)`. It does not. A
  second parameter would have made `Week14Checks`'s reflection lookup throw `AmbiguousMatchException`, and it
  would have created a second place for the winner and the reason to disagree. The method derives both from the
  board it is already looking at.
- **The label wins the tension in DEF-15-04.** Lightening a fill to buy a visible edge spends the contrast of the
  white text on it. The pass is bounded on both sides - 3.15:1 for the edge, 4.6:1 for the text - and the text
  bound is the harder one. All three controls still pass their own label contrast at 6.59:1, 6.63:1 and 6.64:1.

---

## 8. Regression: what the fixes cost, and what moved

A fix pass that is only checked against the failing cases is not a regression test. Three independent checks were
made.

**1. The repair script is idempotent, by fingerprint.** `Week15Fixes.Fingerprint()` hashes the layout it is about
to change. The first run reports `3 control(s) created, 6 label(s) resized, 5 fill(s) lightened` and a hash
change `5BB40B04 -> 8C0D040E`. The second run reports `0 / 0 / 0` and `8C0D040E -> 8C0D040E (UNCHANGED - this run
was a no-op)`. Its `Verify()` step then prints `13 caption(s) and 14 label(s), 0 below its size floor. 0 button
fill(s) below 3.0:1.`

**2. The audit is re-run in full, and every baseline row is paired.** `a11y_delta_week15.txt` re-measures all 109
scored rows and reports **0 REGRESSED** and **0 DROPPED** - no baseline row was quietly lost between the two runs.
Fifteen rows keep their verdict while their measurement moved, and each is paired rather than ignored. Two
examples, both copied from the delta: `StartPanel/StartButton/Text L` still passes with `height=100.0`, but the
requirement it must clear rose from `need=15.7` to `need=24.6` as the caption went from 14 to 22 px; and
`PausePanel/SoundButton/Text C` dropped from `9.29:1` to `6.59:1` when the fill behind it was lightened from
`#404759` to `#545D74`. Both still pass. Both are closer to their line than they were.

**3. The full case suite is re-run, not just the failures.** The final run is 27 passed, 0 failed, 1 blocked - the
same 28 cases, with `TC-A11Y-01` now measuring **13** buttons where the baseline measured 11. The two arrivals are
the two MENU buttons, and `TC-A11Y-02` still reports `0 overlapping pairs` with them in place.

The residual risk this section names rather than hides: three controls now sit between 3.16:1 and 3.19:1 against a
3.0 floor, and `ProfilePanel/SideButton` sits at 3.03:1. The next edit to any of those fills puts them back under
the line, and nothing will warn anyone. The audit is to be re-run before every submission.

---

## 9. Boundary and negative tests

| Case | What it does | Result |
|---|---|---|
| `TC-BOUND-01` | 100 taps in one frame on a real piece | `pieces 24->24 matchId 0->0 throws='' stillAnswers=True` - no crash, no board change, no match banked |
| `TC-BOUND-02` | 50 pause/resume cycles | `timeScale=1.00 pausePanel=False` - the clock is restored every time |
| `TC-DATA-04` | A corrupt save file, written on purpose, then a load | `error='JSON parse error: Invalid value.' quarantined=True emptyDefaults=True recovered=True reloaded=True` - the bad file is set aside, not overwritten in place, and the game starts |
| `TC-DATA-02` | The same finished match banked twice | `afterFirst=1 afterRepeat=1 repeatRefused=True afterReload=1` - a repeat cannot inflate the record |
| `TC-DATA-05` | A save path outside the sandbox, refused | `insideSandbox=True rejectedCopyInsideSandbox=True` |

The invalid-value and interrupted-network cases from the module's list are answered where they exist in this game:
the corrupt save is the invalid value, and there is no network path to interrupt. `REQ-22` is that absence,
stated and checked - the project contains no networking code, as recorded in `environment.md` - rather
than silently omitted.

---

## 10. Compatibility: blocked, with the command that failed

The module asks for at least two approved device or configuration profiles where resources permit, otherwise one
physical device and one approved emulator or configuration. Neither was available.

```
> adb devices
List of devices attached
```

No serial, so no device. There is no APK this week by decision, so no emulator profile could be installed either.
`TC-COMPAT-01` is recorded **BLOCKED** in both logs. It is not counted as a pass, and no reviewer should read the
22 or 27 pass counts as covering it. What would clear it: an Android build, one handset on `adb`, and this same
harness run against it - the harness is Editor-only today, so the honest next step is a build with on-device
`logcat` capture rather than pretending the Editor is a phone.

---

## 11. Acceptance summary

| | Count | Items |
|---|---|---|
| Cases | 28 | 6 flow, 11 rules, 5 data, 3 accessibility, 2 boundary, 1 compatibility |
| Passed (final) | **27** | every case except the blocked one |
| Failed (final) | **0** | the five baseline failures are the same five cases, now passing |
| Blocked | **1** | `TC-COMPAT-01`, reason quoted above |
| Deferred | 2 | `REQ-03` and `REQ-12`, covered indirectly and labelled as gaps, not passes |
| Open defects | 0 | 0 at S1, 0 at S2, 0 at S3 |
| Audit rows (final) | 109 scored | 105 passed, 0 failed, 4 not measurable in both runs |

**Is the build ready for optimization (Week 16)?** Yes, with conditions, and the conditions are the point:

- **Ready:** the four conditions of the module's completion checkpoint each have a case and all four pass. The
  core loop completes and can be left and re-entered. Data loss is survivable - the corrupt-save case recovers.
  Nothing crashes twice, and nothing crashed once: 0 console errors in both runs. There is no shared data to
  expose. The accessibility floors are met and mechanised, so Week 16's profiling runs cannot quietly break them
  without the audit saying so.
- **Not ready to call finished:** one case is blocked (compatibility), two requirements are covered indirectly,
  and every measurement was taken in the Editor at one resolution. Week 16 measures performance; it must not be
  read as the week compatibility or on-device behaviour was proven.

The honest one-line verdict: **the Editor build of commit `207376b` passes its own written acceptance criteria, and
nothing in this record claims it was tested on a phone.**

---

## 12. Evidence index

| File | Proves | Does **not** prove |
|---|---|---|
| `Evidence/requirements.md` | 22 requirements, each testable and naming the script that implements it, written before any test | That they are the right requirements |
| `Evidence/acceptance_criteria.md` | CC-1..CC-4 and AC-5..AC-6, written before the run | Anything about a device |
| `Evidence/traceability_matrix.md` | Every requirement linked to its cases or to a stated gap | That indirect coverage is coverage |
| `Evidence/test_logs_baseline/` | 22 passed, 5 failed, 1 blocked, with the measured value of every case | That the failures were known in advance |
| `Evidence/test_logs_final/` | 27 passed, 0 failed, 1 blocked, same cases | On-device behaviour |
| `Evidence/a11y_week15_baseline.log` | 83 passed, 9 failed, 4 not measurable, thresholds derived in the header | That a passing ratio is comfortable |
| `Evidence/a11y_week15_after.log` | 105 passed, 0 failed, 4 not measurable | That the layout is pleasant |
| `Evidence/a11y_delta_week15.txt` | All 109 rows paired, 0 regressed, 0 dropped | That nothing outside the audit changed |
| `Evidence/defect_log.md` | Four defects with reproduction, root cause, fix, commit reference and retest quote | That no other defect exists unrecorded |
| `Evidence/environment.md` | The machine, the scene, the disabled domain reload, the two `adb` attempts, four stated limits | Anything about performance |
| `Evidence/E1..E4` (play mode) | The pause panel with MENU, the end screen with the summary and MENU, the profile at 20 px, a mid-match board | How it looks on a phone |
| `Evidence/week15_shots.txt` | The capture run in order, with each screenshot's write confirmed on disk | That the images are the only states the game can reach |

The four captures exist because a log can be right while the screen is wrong. They were taken through the game's
own entry points, and the harness rebuilds the drawn pieces from the seeded grid before each one - the first
attempt did not, and E2 showed `Red: 12 Blue: 12` in the HUD beside a `1-1` summary. That discrepancy is disclosed
inside `week15_shots.txt`, and the rebuilt-piece lines in the same file are what make the counts in the pictures
checkable.

---

## 13. Reproducing the runs

```
1. Tools > Dama > Week 15 > 1. Baseline Test Run     (on the Week 14 code, before any fix)
      -> Evidence/test_logs_baseline/
2. A11yAudit.RunWeek15() from the Week 15 baseline audit path
      -> Evidence/a11y_week15_baseline.log
3. Tools > Dama > Week 15 > 3. Apply Defect Fixes    (run twice: the second run must report 0/0/0)
4. Tools > Dama > Week 15 > 2. Final Test Run
      -> Evidence/test_logs_final/
5. A11yAudit.RunWeek15() again
      -> Evidence/a11y_week15_after.log
6. Tools > Dama > Week 15 > 4. Capture Evidence
      -> Evidence/E1..E4, Evidence/week15_shots.txt
```

`Evidence/HOW_TO_RERUN.txt` carries the same sequence with the two known interruptions written down: a script
recompile that lands while play mode is running stalls the capture harness (domain reload is disabled), and the
accessibility audit's menu item can return success without emitting a log line, so the timestamped file is the
proof it ran.

---

## 14. Disclosure record

AI assistance was used for the structure of this document, for drafting, and for writing the harness, the fix pass
and the audit extension. Every number in this document is copied from a generated artefact - `case_log.tsv`,
`summary.txt`, the two audit logs, the delta file or the fix pass's own console output - and none is typed from
memory. Every claim was re-read against those files before submission.

| Item | Assisted | By whom | How it was checked |
|---|---|---|---|
| Test plan and case list | Structure and wording drafted with AI | Charibelle Guzman | Every case was executed; the five failures were not predicted and were not removed |
| `Week15Checks.cs` (1021 lines) | Written with AI | Charibelle Guzman | Run twice against two code revisions; the logs are the output of the file, not a description of it |
| `Week15Fixes.cs` | Written with AI | Charibelle Guzman | Run twice to prove idempotence; the fingerprint values are printed by the script |
| `DamaBoard.cs` changes | Written with AI | Charibelle Guzman | Driven by the harness through the game's own entry points, and captured in play mode |
| `A11yAudit.cs` extension | Written with AI | Charibelle Guzman | The thresholds are derived in the log header; Week 14's three logs are unchanged and still committed |
| This document | Drafted with AI, corrected against the logs | Charibelle Guzman | Cell-by-cell against the files named in every table |

What was **not** done by AI: the decision to fix all four findings rather than defer them, the decision to record
the missing handset as BLOCKED instead of estimating a result, and the decision to mark `REQ-03` and `REQ-12` as
gaps. Those are the three places where the record could have been made to look better, and was not.

---

## 15. How this meets the Week 15 laboratory requirements

| Required | Where |
|---|---|
| Build identifier, supported platform range, test environment, acceptance criteria | Sections 1, 2 and 3; `Evidence/environment.md` |
| Requirements-to-test traceability matrix | Section 5 and `Evidence/traceability_matrix.md` (22 requirements, 28 cases) |
| Functional tests for navigation, input, scoring, win and loss, saving, audio, pause and reset | Section 4: `TC-FLOW`, `TC-RULE`, `TC-DATA`, `TC-A11Y` |
| Negative and boundary tests: rapid repeated input, missing or invalid data, interrupted network, low resources, backgrounding | Section 9: `TC-BOUND-01`, `TC-BOUND-02`, `TC-DATA-04`; interrupted network is not applicable and `REQ-22` is that absence, stated |
| Compatibility on two profiles, or one device and one approved configuration | Section 10: **BLOCKED**, with the command output and what would clear it |
| Defect record with id, severity, reproduction, expected, actual, evidence, status, commit | `Evidence/defect_log.md` (DEF-15-01 to DEF-15-04) |
| Correct high-priority defects, rerun the failed cases, regress the affected features | Sections 7 and 8: baseline to final on the same cases, plus the idempotence proof and the paired audit |
| Acceptance summary: passed, failed, blocked, deferred, and whether the build is ready for optimization | Section 11 |
| The tested build and commit identified | Section 1: `dama-editor-w15-rc1`, commit `207376b` |
| Completion checkpoint: no unresolved defect causing data loss, blocking the loop, exposing another player's data, or repeatedly crashing | Section 3, CC-1 to CC-4, all PASS, with the cases named |
