# Defense Preparation — pointer sheet, no answers

**Student:** Charibelle Guzman · BSIT-4 · MOBILE DEV 002, Module 5, Week 18
**For:** module item 7, "complete the individual technical defense, naming specific files, classes, commits,
test records and profiling evidence"
**Status:** the defense has **not been sat**. Nothing on this page is an answer.

---

## Why there are no answers here

The module's own list of common mistakes names two that this sheet exists to avoid: *being unable to identify
important files and commits*, and *reading an AI-generated defense answer*. A scripted answer read aloud is
indistinguishable from one that was never understood, and it is graded as such.

So this file holds **questions** and, for each one, **where the true answer already lives in this repository** —
a path, a class, a commit, a log, a row. Read the pointer, look at the artefact, and be able to say what is in it.
If the pointer leads somewhere you cannot explain in your own words before the viva, that is the thing to study,
not this page.

Every path below is relative to `docs/Charibelle Guzman/` unless it starts with `Assets/` or is a git command.

---

## 1. The twelve things to be able to name cold

If you can only prepare twelve facts, prepare these. Each is a file you should be able to open without searching.

| # | Must be able to name | Where it is |
|---|---|---|
| 1 | The scene the game ships | `Assets/Scenes/DamaGame.unity` — build index 0; `SampleScene.unity` disabled |
| 2 | The class that holds the rules and the screen state machine | `Assets/Scripts/DamaBoard.cs` |
| 3 | The class that owns the save file | `Assets/Scripts/Profile/PlayerProfile.cs` — the **only** class permitted to read, sign, write or delete `profile.json` |
| 4 | The class that renders the derived version strings | `Assets/Scripts/Core/ReleaseInfo.cs` — `Caption()` and `BuildLine()` |
| 5 | Where the save actually is on this machine | `C:/Users/Zedrich/AppData/LocalLow/DefaultCompany/dama/Dama/profile.json` — path built from `companyName` + `productName` |
| 6 | The version being submitted | `1.0.0-rc1`, Android version code `2` — `ProjectSettings/ProjectSettings.asset`; the tag is `v1.0.0-pa2` |
| 7 | The three gate commands and their counts | §2 below — release **5/0**, regression **27/0/1 blocked**, audit **148/0/0/4 not measurable** |
| 8 | The one case that is blocked, and why | `TC-COMPAT-01` — no APK and no handset; `Week_18/Evidence/test_logs_regression/summary.txt` |
| 9 | The nine demonstration screens | `Week_18/Evidence/D1…D9.png` + `demo_rehearsal.txt` |
| 10 | The defect that was most severe | `DEF-15-01`, "no route back to the start screen", S2 — `Week_15/Evidence/defect_log.md` |
| 11 | The optimisation with a number attached to it | per-turn allocation `6,975.9 B` → `0.1 B ± 0.1` — `Week_16/Evidence/metrics.md` |
| 12 | What is explicitly **not** proven | §6 below, and `Week_18/Evidence/final_results.md` §4 |

---

## 2. The three gates, and the exact command that re-runs each

All three were re-run on the submission tree on 2026-11-26. The counts are copied from the logs, not remembered.

| Gate | Entry point | Counts | Log |
|---|---|---|---|
| Release checks | `Week17Checks.Begin()` with `Week17Checks.RunDirOverride` set | **5 passed, 0 failed, 0 console errors** | `Week_18/Evidence/test_logs_release/summary.txt` |
| Full regression | `Week15Checks.BeginWeek18()` | **27 passed, 0 failed, 1 blocked, 0 console errors** | `Week_18/Evidence/test_logs_regression/summary.txt` |
| Accessibility audit | `A11yAudit.RunWeek18()` | **148 passed, 0 failed, 0 warnings, 4 not measurable** | `Week_18/Evidence/a11y_week18_after.log` |

To read them without opening Unity:

```
type "docs\Charibelle Guzman\Week_18\Evidence\test_logs_release\summary.txt"
type "docs\Charibelle Guzman\Week_18\Evidence\test_logs_regression\summary.txt"
type "docs\Charibelle Guzman\Week_18\Evidence\final_results.md"
```

Be ready for the follow-up *"why did these have to be re-run at all if Week 17 already passed them?"* — the pointer
is `Week_18/Evidence/final_results.md` §1, paragraph *"Why the logs did not overwrite Week 17"*.

---

## 3. Questions by area

Each row is a question the panel may ask, and where the answer is. **No row contains the answer.**

### 3.1 Theme, target player and scope

| # | Question | Where the answer lives |
|---|---|---|
| Q01 | What is the theme, and what makes this game belong to it rather than merely be labelled with it? | `Week_18/others/Guzman_Charibelle_PA2.md`; `Week_13/others/Guzman_Charibelle_SA5.md` |
| Q02 | What are the three rules that make the game *Dama* and not checkers? | `Assets/Scripts/DamaBoard.cs` (move generation); demonstrated by `TC-RULE-01`, `TC-RULE-03`, `TC-RULE-06` in `Week_18/Evidence/test_logs_regression/case_log.tsv` |
| Q03 | Who is the target player, and what in the build is there because of them? | `Week_13/others/Guzman_Charibelle_SA5.md`; the 96 canvas-px touch floor in `Week_18/Evidence/a11y_week18_after.log` |
| Q04 | Which requirements did you decline to implement, and why? | `Week_15/Evidence/requirements.md` (`REQ-01…REQ-22`), `Week_15/Evidence/traceability_matrix.md` |
| Q05 | What in this build is a stub or a simulation rather than a feature? | `Week_16/Evidence/method.md`; `Week_18/Evidence/final_results.md` §3 |

### 3.2 Core mechanics

| # | Question | Where the answer lives |
|---|---|---|
| Q06 | How is a capture enforced, and where in code does the quiet move disappear? | `DamaBoard.GetSelectableMoves` in `Assets/Scripts/DamaBoard.cs`; `TC-RULE-01` |
| Q07 | How does a chain capture keep the turn? | `DamaBoard.cs`; `TC-RULE-03` (`continuationOffered=True`) |
| Q08 | How is the crowned piece different, and how is the far row defined for each colour? | `DamaBoard.cs`; `TC-RULE-04` (RED, `y == 7`) and `TC-RULE-05` (BLUE, `y == 0`) |
| Q09 | How can a side with pieces still on the board lose? | `DamaBoard.CheckForWin` / `DeclareWinner`; `TC-RULE-09`, `TC-RULE-10` |
| Q10 | The move generators return a shared buffer. What breaks if a caller keeps the list? | the field comment in `Assets/Scripts/DamaBoard.cs`; `README.md`, *Known limitations* |

### 3.3 Architecture and state

| # | Question | Where the answer lives |
|---|---|---|
| Q11 | Name every screen state, and the one method that switches between them. | `DamaBoard.GameState` / `ShowState()` in `Assets/Scripts/DamaBoard.cs`; `TC-REL-05` proves the panels hide and cannot leak into a match |
| Q12 | Where is the board stored, and why is it not a GameObject tree? | `grid` and `isKing` arrays in `DamaBoard.cs` |
| Q13 | How does the camera keep an 8x8 board on a phone? | `Assets/Scripts/BoardCameraFitter.cs` |
| Q14 | What is `MobileRuntimeConfig` for? | `Assets/Scripts/MobileRuntimeConfig.cs` |
| Q15 | Which files are the scene builders, and what is the failure mode of running one twice? | `Assets/Scripts/Editor/DamaBoardBuilder.cs`, `Week17ReleasePanels.cs`, `ProfileSceneBuilder.cs`; `DEF-17-05` in `Week_17/Evidence/issue_disposition.md` |

### 3.4 Data, security and services

| # | Question | Where the answer lives |
|---|---|---|
| Q16 | What is in the save file, and who is allowed to write it? | `Assets/Scripts/Profile/PlayerProfile.cs`; `TC-DATA-01…TC-DATA-05` |
| Q17 | "Tamper-evident, not encrypted" — what exactly does that mean, and what can an attacker still do? | `Week_13/Evidence/pt5_checks.log` (`L15 KNOWN LIMIT`); `README.md`, *Known limitations* |
| Q18 | What happens when the save file is corrupt? | `TC-DATA-04` — quarantined, reported, defaults restored |
| Q19 | Why is a two-press reset used instead of one? | `Assets/Scripts/Profile/ProfilePanel.cs` (`ResetWindowSeconds = 4f`, `ResetPressed`); captures `D4` and `D5` in `Week_18/Evidence/` |
| Q20 | Does the game talk to a server or a third-party service? What does that mean for the "no shared data" criterion? | `TC-DATA-05`; `CC-3` in `Week_18/Evidence/test_logs_regression/summary.txt` |
| Q21 | Is there a credential, key or keystore in the repository? | `Week_18/Evidence/secret_scan_week18.md`; `Week_17/Evidence/secret_scan.md` + `secret_scan_raw.txt` |

### 3.5 Usability and accessibility

| # | Question | Where the answer lives |
|---|---|---|
| Q22 | What exactly does the accessibility audit measure, and against which published thresholds? | header and row kinds in `Week_18/Evidence/a11y_week18_after.log`; method in `Week_14/others/Guzman_Charibelle_PT6.md` |
| Q23 | Week 14 started at 45 passed / 10 failed and ended at 58 / 0. Where is every failure paired with its after value? | `Week_14/Evidence/a11y_delta.txt` |
| Q24 | Week 15 found nine failures the Week 14 pass had already "fixed". How is that possible, and what changed? | `Week_15/Evidence/a11y_delta_week15.txt`; the three new check kinds (caption size, label size, non-text contrast) |
| Q25 | The controls pass at 3.16:1, 3.17:1, 3.19:1 and 3.03:1 against a 3:1 line. Is that a pass you are comfortable defending? | `Week_15/Evidence/a11y_week15_after.log`; `README.md`, *Known limitations*, "Contrast margins are thin" |
| Q26 | Four rows are `NOT MEASURABLE` rather than failing. Name them and say why. | `Week_18/Evidence/final_results.md` §1, table of four |
| Q27 | What did the game not have for the first four weeks, and what has it still not got? | sound and haptics (`Assets/Scripts/Core/GameFeedback.cs`, `Week_14/Evidence/feedback_lines.txt`); a screen-reader path and dynamic type (`README.md`, *Known limitations*) |
| Q28 | Was a human playtest ever run? | `Week_14/Playtest/`; `PT6` §7; `Week_18/Evidence/final_results.md` §4 |

### 3.6 Testing and defects

| # | Question | Where the answer lives |
|---|---|---|
| Q29 | How many cases does the suite have, and what does it look like before and after the Week 15 fixes? | `Week_15/Evidence/test_logs_baseline/` (**22 / 5 / 1**) and `test_logs_final/` (**27 / 0 / 1**) |
| Q30 | Which case in the final suite is not a pass, and why is it allowed to stay that way? | `TC-COMPAT-01` — `Week_18/Evidence/test_logs_regression/summary.txt`; `Week_17/Evidence/clean_install.md` |
| Q31 | Name the four Week 15 defects, their severity, and the case that closed each. | `Week_15/Evidence/defect_log.md` (`DEF-15-01…04`) |
| Q32 | Which two requirements have only indirect coverage, and why is that written down instead of hidden? | `Week_15/Evidence/traceability_matrix.md` (`REQ-03`, `REQ-12`) |
| Q33 | Which two defects were found in Week 17, and by what? | `Week_17/Evidence/issue_disposition.md` (`DEF-17-05` found by `TC-A11Y-02`; `DEF-17-06`) |
| Q34 | Which three faults did the Week 18 demonstration harness have, and which one touched the player's own save? | `Week_18/Evidence/final_results.md` §3, items 1–3 |

### 3.7 Performance

| # | Question | Where the answer lives |
|---|---|---|
| Q35 | State the four performance targets and the order they were written in. | `Week_16/Evidence/targets.md` (`PT-1…PT-4`), written **before** the baseline |
| Q36 | Why was the first allocation instrument thrown away? | `Week_16/Evidence/method.md` — a deliberate 64 KB allocation moved the counter by **0 B** |
| Q37 | What replaced it, and what is the uncertainty on the result? | amplitude scaling in `Week_16/Evidence/method.md`; result `0.1 B ± 0.1` in `metrics.md` |
| Q38 | What is the before/after number for a restart, and why is it not "zero"? | `Week_16/Evidence/metrics.md` — `5,596 B` → at most ~`1.7 KB`; the `PiecePool` ceiling |
| Q39 | Can you prove the game runs faster? | `Week_16/Evidence/metrics.md` and `bottlenecks.md` — the frame was already inside budget (`0 of 34,550` over 33.3 ms) before any change; the claim made is about allocation, not frame time |
| Q40 | Why are all of these Editor numbers? | `Week_16/Evidence/method.md`, limits section |

### 3.8 Release, identity and evidence integrity

| # | Question | Where the answer lives |
|---|---|---|
| Q41 | Why does the app still report `1.0.0-rc1` when the submission tag is `v1.0.0-pa2`? | `Week_18/Evidence/release_record.md` §2 |
| Q42 | What breaks if `productName` or `companyName` is changed? | same section — the save path is built from them |
| Q43 | How do you know the on-screen version string is not typed into the scene? | `Assets/Scripts/Core/ReleaseInfo.cs`; `TC-REL-01`, `TC-REL-02` |
| Q44 | Why is there no APK in this submission? | `Week_18/Evidence/release_record.md` §3 — three independent reasons; `final_pre_submission_checklist.md` §3.1 |
| Q45 | What would it take to close that row? | `Week_18/Evidence/release_record.md` §5, four steps |
| Q46 | Is the originality declaration signed? | `Week_18/others/Guzman_Charibelle_PA2_Originality_Declaration.md` — the signature line is deliberately blank |
| Q47 | How do I know the nine demonstration images are nine different screens and not one image copied? | `Week_18/Evidence/demo_rehearsal.txt`, hash table — nine distinct SHA-256 values, and the harness fails the run if any two match |
| Q48 | Which commit is the submission, and how do I reproduce the tree? | `Week_18/Evidence/repo_state.txt`; `git show --stat <tag>` |
| Q49 | Which files in this repository are historic evidence that must not be regenerated? | `Week_13…Week_17/Evidence/`; `Week_18/Evidence/final_results.md` §1 |

---

## 4. Questions to expect that this package cannot answer

Say the second half out loud. The value of the first half is that the limit was recorded before it was asked about.

| Question | The honest shape of the answer |
|---|---|
| "How does it run on a phone?" | It has never run on a phone. `TC-COMPAT-01` is **BLOCKED**, `Week_17/Evidence/clean_install.md` quotes `adb devices` returning no serial, and `Week_18/Evidence/release_record.md` §3 says so in the same sentence as the version number. |
| "What frame rate does a player get?" | Unknown. Every Week 16 number is an Editor number on a desktop GPU; `Week_16/Evidence/method.md` names that as a limit rather than a caveat. |
| "What did real players think?" | Nothing — no human playtest session has been run in any week. `Week_14/Playtest/` is a kit, and `PT6` §7 marks every cell `not run`. |
| "Is the save secure?" | Tamper-**evident**, not encrypted, and the key lives on the same device as the file. `Week_13/Evidence/pt5_checks.log`, `L15 KNOWN LIMIT`. |
| "How did the defense go?" | It has not happened. `e3_record.md` is blank by design. |

---

## 5. A ten-minute walkthrough to run the night before

Print nothing you have not opened. Run these in order and look at the output — each one is under a minute.

```
1  git log --oneline -20                       seventeen commits, one week per line
2  git tag -l                                  week16-final and v1.0.0-pa2
3  type "docs\Charibelle Guzman\Week_18\Evidence\final_results.md"
4  type "docs\Charibelle Guzman\Week_18\Evidence\test_logs_regression\summary.txt"
5  type "docs\Charibelle Guzman\Week_18\Evidence\demo_rehearsal.txt"
6  dir  "docs\Charibelle Guzman\Week_18\Evidence\D*.png"      nine images, nine sizes
7  type "docs\Charibelle Guzman\Week_16\Evidence\metrics.md"  the three profile runs
8  type "docs\Charibelle Guzman\Week_15\Evidence\defect_log.md"
9  type "docs\Charibelle Guzman\Week_18\Evidence\release_record.md"
10 type "docs\Charibelle Guzman\README.md"     the whole project in one page per week
```

Then open the game in the Editor and walk the nine screens once, in the order `demo_rehearsal.txt` lists them, so
that the demonstration and the transcript are the same walkthrough.

---

## 6. Rules for the room

1. **Name the artefact, not the intention.** "`TC-RULE-06` in the Week 18 regression log" beats "we tested that".
2. **Never read an answer.** If you do not know, say which file would contain it and what you would look for.
3. **When a number is asked for, give the number and its source**, and say whether it is an Editor number.
4. **Do not upgrade a BLOCKED row under pressure.** `TC-COMPAT-01` is blocked; calling it passed would
   contradict a file in the repository that the panel can open.
5. **Do not defend the shared-buffer or the thin contrast margins as fine.** Both are named in the README as
   limitations; defending them as complete is a weaker position than naming them first.
6. **Leave `e3_record.md` blank until it is filled in front of the instructor.** Filling it in beforehand would
   make it a fabrication, which is the failure mode the whole evidence package is built to avoid.

---

*Prepared 2026-11-26. Contains no answers by design; `final_pre_submission_checklist.md` §4 records why.*
