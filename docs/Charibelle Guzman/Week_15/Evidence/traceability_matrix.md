# Requirements-to-test traceability matrix - Dama (Week 15, AS3 Milestone)

**Student:** Charibelle Guzman (BSIT-4)
**Requirements:** `requirements.md` (REQ-01 to REQ-22, written before any test ran)
**Case source:** `test_logs_baseline/case_log.tsv` and `test_logs_final/case_log.tsv` (same harness file, same cases, run before and after the fix pass)
**Audit source:** `a11y_week15_baseline.log`, `a11y_week15_after.log`, paired row by row in `a11y_delta_week15.txt`

The two logs are produced by the same script, `Assets/Scripts/Editor/Week15Checks.cs`, so a row in the
baseline and the row of the same id in the final log are the same case measured twice. Nothing in this
matrix is copied from the milestone document: every status cell is the `status` column of one of those
files.

## Coverage

| | Count |
|---|---|
| Requirements stated | 22 |
| Requirements with at least one numbered case | 19 |
| Requirements with a measurement but no numbered case (`REQ-18` - the audit's own `C`/`L` checks) | 1 |
| Requirements covered only indirectly, and labelled as such below (`REQ-03`, `REQ-12`) | 2 |
| Requirements with **no** coverage, stated rather than hidden | 0 |

## Matrix

| REQ | Requirement (short) | Case(s) | Baseline | Final | Evidence |
|---|---|---|---|---|---|
| REQ-01 | Title screen with START, PROFILE and the rules | `TC-FLOW-01`, `TC-FLOW-02` | PASS | PASS | `case_log.tsv` both runs; `C`/`L` rows for `StartPanel/*` in both audit logs |
| REQ-02 | START spawns 24 pieces, RED to move, both counts shown | `TC-FLOW-03` | PASS | PASS | `pieces=24 turn='Turn: RED' counts='Red: 12   Blue: 12'` |
| REQ-03 | Tap selects, tap a highlighted square moves, turn passes | **no numbered case** | - | - | Indirect only: `TC-RULE-01..06` drive the same `ExecuteMove`/`EndTurn` path by reflection, and `TC-BOUND-01` taps a real piece 100 times and asserts the board is unchanged. **No case drives select-then-move through `HandleTap` with a board change.** Recorded as a gap, not a pass |
| REQ-04 | Mandatory capture removes the quiet move | `TC-RULE-01`, `TC-RULE-02` | PASS | PASS | `offered=[(3,4)] expected only [(3,4)]`; `landing=RED jumpedCell=empty` |
| REQ-05 | A capture that lands on another jump keeps the turn and the chain | `TC-RULE-03` | PASS | PASS | `chain=(3,4) turn=RED:True continuationOffered=True` |
| REQ-06 | Crowning: RED at `y == 7`, BLUE at `y == 0`; the Dama flies any distance | `TC-RULE-04`, `TC-RULE-05`, `TC-RULE-06` | PASS | PASS | `landing=(2,7) isKing=True`; `landing=(4,0) isKing=True`; `offered=[(1,1) ... (6,6)]` |
| REQ-07 | A side with no pieces left loses, declared once | `TC-RULE-07` | PASS | PASS | `WinPanel=True WinText='RED WIN!'`; once-only is `TC-DATA-02`'s `repeatRefused=True` |
| REQ-08 | A side with pieces but no legal move loses | `TC-RULE-09` | PASS | PASS | `WinPanel=True WinText='RED WIN!'` |
| REQ-09 | End screen states winner, both counts, the reason; offers RESTART and MENU | `TC-RULE-08`, `TC-RULE-10`, `TC-RULE-11` | **FAIL** (08, 10, 11) | **PASS** | `SummaryText='RED WINS.  RED 1-0, BLUE has no pieces left.'`; `SummaryText='RED WINS.  RED 1-1, BLUE has no legal move.'`; `RestartButtonWin=True MenuButtonWin=True` - see DEF-15-01, DEF-15-03 |
| REQ-10 | PAUSE freezes; RESUME returns to the same position and side | `TC-FLOW-04`, `TC-BOUND-02` | PASS | PASS | `paused=True pausedTurn=1 resumed=True resumedTurn=1`; `50` cycles, `timeScale=1.00` |
| REQ-11 | SOUND silences feedback, spells its state, survives relaunch | `TC-FLOW-05` | PASS | PASS | `label 'SOUND: OFF' then 'SOUND: ON' prefs 0/1`; `[Feedback] sound OFF - remembered in PlayerPrefs` |
| REQ-12 | Move, capture, crown, win and lose each play their own clip | **no numbered case** | - | - | Indirect only: `test_logs_final/console.log` lines 13-44 record `[Feedback] move -> Dama_Move`, `capture -> Dama_Capture`, `crown -> Dama_Crown`, `win -> Dama_Win`, and the ready line `clips move=ok capture=ok crown=ok win=ok lose=ok`. **`lose` did not fire in this run** (RED won every seeded match), so the lose clip is loaded and wired but not observed. Recorded as a partial, not a pass |
| REQ-13 | Name and side survive an app restart | `TC-DATA-01` | PASS | PASS | `name='QA-CHARIBELLE' side=2` after a disk reload |
| REQ-14 | A finished match is banked once; no repeat inflates it | `TC-DATA-02` | PASS | PASS | `afterFirst=1 afterRepeat=1 repeatRefused=True afterReload=1` |
| REQ-15 | Reset clears the record and leaves a usable profile | `TC-DATA-03` | PASS | PASS | `reset=True name='PLAYER' wins=0 losses=0 best=0` |
| REQ-16 | A corrupt save is rejected with a message and the game still starts | `TC-DATA-04` | PASS | PASS | `error='JSON parse error: Invalid value.' quarantined=True emptyDefaults=True recovered=True reloaded=True` |
| REQ-17 | Return to the title screen from inside a match | `TC-FLOW-06` | **FAIL** | **PASS** | `PausePanel/MenuButton present=False, DamaBoard.ReturnToMenu present=False` -> `StartPanel=True PausePanel=False HUDPanel=False timeScale=1.00` - see DEF-15-01 |
| REQ-18 | Labels meet WCAG contrast and fit their rect | audit `C`, `L` (no numbered case) | 0 `C`/`L` failures | 0 `C`/`L` failures | Both audit logs. 4 `NOT MEASURABLE` rows in each, unchanged: two HUD labels over the 3D board and two controls with no opaque fill, reported rather than scored |
| REQ-19 | Every button and input field at least 96 x 96 canvas px; nothing overlapping | `TC-A11Y-01`, `TC-A11Y-02` | PASS | PASS | `0 below floor`; `0 overlapping pairs`. The final run measures **13** buttons against the baseline's 11 |
| REQ-20 | Captions >= 20 px, labels >= 18 px, control fills >= 3:1 | `TC-A11Y-03` + audit `S`, `N` | **FAIL** (`TC-A11Y-03`; 6 `S`, 3 `N`) | **PASS** (0 `S`, 0 `N`) | `0 caption(s), 0 label(s) below floor`; `a11y_delta_week15.txt` marks the 9 rows `FIXED` - see DEF-15-02, DEF-15-04 |
| REQ-21 | 100 rapid taps do not crash, corrupt the board or bank twice | `TC-BOUND-01` | PASS | PASS | `pieces 24->24 matchId 0->0 throws=''` |
| REQ-22 | No network path, so interrupted network access is not applicable | `TC-DATA-05` | PASS | PASS | `insideSandbox=True rejectedCopyInsideSandbox=True`; absence of networking code stated in `environment.md` |

## Cases with no requirement

| Case | Why it exists | Status |
|---|---|---|
| `TC-COMPAT-01` | A second device or configuration profile. It is a **test type**, not a requirement: the module asks for compatibility evidence where resources permit. | **BLOCKED** - `adb devices` returns no serial and there is no APK this week; the attempted command is quoted in `environment.md` |

## How to read the two rows that are not passes

`REQ-03` and `REQ-12` are the two requirements this week's harness does not drive end-to-end. They are
listed with `-` in the status columns and with what does and does not cover them, because a matrix that
marks them PASS on the strength of adjacent cases would be worth less than one that names the hole.
Closing them is a Week 16 task: a case that taps a piece, taps a highlighted square and asserts the
piece moved and the turn changed, and a case that forces a RED loss so the lose clip is observed.
