# PA2 — Evidence Portfolio

**Student:** Charibelle Guzman — BSIT-4
**Course:** MOBILE DEV 002 Advanced Mobile Application Development, Module 5
**Week:** 18 · **Assessment:** PA2
**Rule of this document:** every row names the file that proves the claim, and no row claims more than
its file proves. Where a row cannot be proved in this environment it says **BLOCKED** and why.

---

## 1. What is submitted

| Claim | Proof | Standing |
|---|---|---|
| The submitted tree is `main` at the PA2 commit, tagged `v1.0.0-pa2` | `Evidence/repo_state.txt` | PASS |
| The app reports `1.0.0-rc1`, version code 2, `com.defaultcompany.dama` | `Week_17/Evidence/build_record.txt`; `Evidence/test_logs_release/summary.txt` (`TC-REL-01`, `TC-REL-02`) | PASS |
| No signing key, credential or build artifact is tracked | `Evidence/secret_scan_week18.md` | PASS |
| An installable build is submitted | — | **BLOCKED**: no APK exists, no keystore, no device (`Evidence/release_record.md` §3) |
| The build was installed or launched from the submitted package | — | **BLOCKED**: nothing to install, no device answering `adb` |

## 2. The final pre-submission checklist (module, Week 18)

| # | Requirement | Status | Proof |
|---|---|---|---|
| 1 | Repository, build, documentation, evidence, credits, licences and disclosure records checked | PASS | `Evidence/final_pre_submission_checklist.md` §1–§4 |
| 2 | Final tagged repository version, tag and commit recorded | PASS | `Evidence/repo_state.txt` |
| 3 | Installable build, repository access, technical report, user guide, evidence portfolio, presentation, signed declaration | **PARTIAL** | documents PASS; repository access in `repo_state.txt` §4; **build BLOCKED**; declaration unsigned in this copy |
| 4 | Install or launch the submitted build from the same package given to the instructor | **BLOCKED** | no package, no device |
| 5 | Concise presentation of theme, player, mechanics, architecture, services, security, usability, testing, optimisation, limitations | PASS | `Guzman_Charibelle_PA2_Presentation.pdf` + speaker notes |
| 6 | Demonstrate onboarding, core loop, theme mechanics, settings, data, error handling, completion | PASS | `Evidence/D1…D9.png`, `Evidence/demo_rehearsal.txt` |
| 7 | Individual technical defense naming files, classes, commits, test records, profiling evidence | **NOT SAT** | `Evidence/defense_prep.md` (pointers only); `Evidence/e3_record.md` (blank) |
| 8 | Reflection: strongest result, most important limitation, next improvement | PASS | `Guzman_Charibelle_Week18_Reflection.pdf` |

## 3. The demonstration, claim by claim

| Claim | Proof | Standing |
|---|---|---|
| Nine screens exist and are legible at 1151x584 | `Evidence/D1…D9.png` | PASS |
| The nine images are nine different pictures | `Evidence/demo_rehearsal.txt` — nine distinct SHA-256 values; the harness fails on a collision | PASS |
| The title shows the version the code was built with | `D1_title.png` reads `1.0.0-rc1 · Editor`; `TC-REL-01` compares it to `Application.version` | PASS |
| The credits line names the same version and the application id | `D3_credits.png`; `TC-REL-02` | PASS |
| The profile shows a real record and real best times | `Evidence/demo_rehearsal.txt` — `Matches: 3 Wins: 2 Losses: 1`, best times 0.5 s and 0.5 s, banked through `PlayerProfile.RecordMatch` | PASS |
| The two-press reset erases the record and says so | `D5_profile_after_reset.png`; transcript lines for press 1 (`PRESS AGAIN`) and press 2 (`Profile erased.`) | PASS |
| A mandatory jump is enforced and counted | `D7_match_after_capture.png`; transcript `counts Red: 1 Blue: 2` after `(3,4) → (4,5)` | PASS |
| The end state is reachable and explained | `D9_end_screen.png` — `RED WINS. RED 1-1, BLUE has no legal move.` | PASS |
| The demonstration was run on a phone | — | **BLOCKED**: it was run in the Editor |

## 4. The gates, re-run on the submitted tree

| Gate | Result | File |
|---|---|---|
| Release checks (`TC-REL-01…05`) | **5 passed, 0 failed, 0 console errors** | `Evidence/test_logs_release/summary.txt`, `case_log.tsv`, `console.log` |
| Full regression (27 cases) | **27 passed, 0 failed, 1 blocked** | `Evidence/test_logs_regression/` |
| Accessibility audit | **203 passed, 0 failed, 0 warnings, 6 not measurable** | `Evidence/a11y_week18_after.log` (148 / 0 / 0 / 4 at the PA2 submission; the tree has since gained UNDO and HINT) |
| Second device / profile (`TC-COMPAT-01`) | **BLOCKED** | `Evidence/test_logs_regression/case_log.tsv` |

The blocked case is blocked on hardware and is not counted anywhere as a pass.

## 4a. Requirements added after the Week 18 submission

These eight rows are not graded work. They are here because the tree the repository now holds
contains them, and each row names the file that proves it - or says plainly that no file does.
The full statements are `Guzman_Charibelle_PA2_Technical_Report.pdf` §12.

| Claim | Proof | Standing |
|---|---|---|
| RED or BLUE is chosen on CHOOSE MODE or PROFILE and kept in the signed record without a new field | `Guzman_Charibelle_PA2_User_Guide.pdf` §2.1, §5; `Evidence/test_logs_regression/` data cases still cover the store | implemented; no log of its own |
| A BLUE player's board is turned, and a tap still lands on the tapped cell | `Guzman_Charibelle_PA2_User_Guide.pdf` §2.1 | implemented; confirmed by playing, no log |
| The AI obeys the same rules, takes mandatory captures at all three levels and HARD beats EASY | `Evidence/ai_checks_postweek18.log` - parity 200 x 2 with 0 disagreements, **HARD 10/10** | PASS |
| A third appearance of the same position ends the match level | `Evidence/postweek18_features.log` case A - `DRAW.  RED 1-1, the same position 3 times.` | PASS |
| 50 plies with no capture or promotion end the match level | `Evidence/postweek18_features.log` case B - `DRAW.  RED 1-1, 50 plies without a capture.` | PASS |
| UNDO returns the board, both counts, the side to move and the captured piece | `Evidence/postweek18_features.log` case C; audit row `UNDO` 272x96 at 6.24:1 | PASS |
| HINT paints a legal move and moves nothing | `Evidence/postweek18_features.log` case D; audit row `HINT` 160x96 at 12.44:1 | PASS |
| A move travels 120 ms and a capture pops | — | **NOT MEASURED**: the edit-mode cases snap on purpose; no log records the travel |

## 5. Claims carried from earlier weeks, and where each one lives

| Claim | Proof | Note |
|---|---|---|
| The save is signed, clamped, atomically written, and recovers a corrupted file | `Week_13/Evidence/pt5_checks.log` (15 rows) | tamper-evident, **not encrypted** — recorded as a known limit |
| The save survives a rejected signature without losing the file | `Week_13/Evidence/pt5_checks.log`; `Week_15/Evidence/test_logs_final/` data cases | PASS |
| Every measurable control clears the touch, contrast and size floors | `Week_14/Evidence/a11y_after.log`, `Week_15/Evidence/a11y_week15_after.log`, `Week_17/Evidence/a11y_week17_after.log`, `Evidence/a11y_week18_after.log` | 6 rows are NOT MEASURABLE and stay so |
| The rules hold across 27 cases | `Week_15/Evidence/test_logs_final/`, re-run in `Evidence/test_logs_regression/` | PASS |
| Per-turn allocation fell from 6,975.9 B to below the method's resolution | `Week_16/Evidence/profile_after_alloc/summary.txt`, `metrics.md` | Editor measurement |
| Restart allocation fell from 5,596 B to ~1.7 KB or less | `Week_16/Evidence/profile_after_pool/summary.txt` | Editor measurement |
| Frame time did **not** measurably change, and is not claimed | `Week_16/Evidence/metrics.md`, `method.md` | stated as a limit, not a win |
| A human playtest was run | — | **NOT RUN**: the Week 14 kit exists, no volunteer played |

## 6. What the evidence does not cover

- **Anything about a phone**: install, launch, touch on glass, frame rate, battery, thermals, memory
  headroom. No build and no device, in any week.
- **A real player**: every session was driven by a harness or by the developer. Usability findings are
  machine and heuristic findings, labelled as such since Week 14.
- **The defense**: the viva is not in this folder and is not described as passed.

## 7. Known defects in the evidence itself, disclosed

| Event | What happened | Where recorded |
|---|---|---|
| Week 17 capture run | six names over four pictures, because the Editor window was not foreground | `Week_17/Evidence/week17_shots.txt` |
| Week 17 builder | a second run duplicated three title controls (`DEF-17-05`) | `Week_17/Evidence/issue_disposition.md` |
| Week 18 capture run 1 | `ShowProfile` refused mid-match, so D4/D5/D6 were one picture; fixed, re-run | `Evidence/final_results.md` §3 |
| Week 18 capture run 2 | best times read `0.0 s` because three matches were banked in one tick; fixed, re-run | `Evidence/final_results.md` §3 |
| Week 18 runs 1–3 | the end screen banked a match into the real profile store (the game's own behaviour); the harness now banks into a temporary store | `Evidence/final_results.md` §3 |
| Post-submission audit, run 1 (2026-11-28 12:25) | the working tree had one failing row: `StartPanel/StartButton/Text` measured **1.89:1** because the button's fill had drifted to `#37D926` from the committed `#D92626`. The fill was restored to the committed red and the audit re-run: **203 / 0 / 0 / 6**. Both runs are kept | `Evidence/a11y_week18_audit_20261128_122518.log` (failing), `Evidence/a11y_week18_audit_20261128_122607.log` and `Evidence/a11y_week18_after.log` (passing) |

Each of these is kept in the record rather than tidied away, because a portfolio that only shows the
runs that worked is not evidence.

## 8. How to regenerate everything

| Artefact | How |
|---|---|
| The nine captures | `Tools > Dama > Week 18 > 1. Capture Demonstration` |
| Release checks | `Tools > Dama > Week 17 > 2. Release Checks` (set `Week17Checks.RunDirOverride` to keep Week 17's logs intact) |
| Regression | `Tools > Dama > Week 18 > 3. Regression Run` |
| Accessibility audit | `Tools > Dama > Week 18 > 2. Accessibility Audit` |
| The draw rule, UNDO and HINT | `Tools > Dama > Week 20 > 2. Run Feature Checks` (four cases, writes `Evidence/postweek18_features.log`) |
| The AI | `Tools > Dama > Week 19 > 2. Run AI Checks` (prints to the Console; `Evidence/ai_checks_postweek18.log` is that transcript) |
| The documents | `Tools > Dama > Submission > Build Week 18 HTML`, then print the HTML to PDF |
| The tags and commits | `Evidence/repo_state.txt` lists the commands and the output |
