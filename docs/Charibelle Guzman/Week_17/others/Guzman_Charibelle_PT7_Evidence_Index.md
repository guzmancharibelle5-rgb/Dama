# PT7 - Evidence Index

**Game:** Dama
**Student:** Charibelle Guzman · BSIT-4
**Week:** 17 - Release Preparation and Documentation
**Release under test:** `1.0.0-rc1`, version code `2`, `com.defaultcompany.dama`
**Tree:** tag `week16-final` (`337b436`) plus the Week 17 change set
**Produced:** 2026-11-26

---

## 1. How to read this index

Every claim in the Week 17 package is listed once, with the file that proves it and - where a harness produced
it - the case id inside that file. A claim with no row here does not belong in the documents, and a row whose
evidence file is missing means the claim has to be withdrawn, not softened.

Two rules were applied while building the table:

- **A pass is a measurement, not an opinion.** "The caption is correct" is only a claim because
  `TC-REL-01` compared the on-screen string to `Application.version`; the comparison is in the log.
- **Something that could not be measured is listed as not measured.** The rows in section 5 are in this index
  precisely so that a reader can see the holes without hunting for them.

## 2. Release identity

| Claim | Evidence | Case / line |
|---|---|---|
| The build is `1.0.0-rc1` with version code 2 | `build_record.txt` | release identity block |
| `productName` and `companyName` are unchanged, so the save store did not move | `build_record.txt` | `unchanged=True` lines |
| The title caption names the build it was compiled as | `test_logs_release/summary.txt` | `TC-REL-01` |
| The credits build line agrees with the title and names the application id | `test_logs_release/summary.txt` | `TC-REL-02` |
| The version string is derived, not typed into the scene | `others/Guzman_Charibelle_PT7_Technical_Documentation.md` §2, plus `TC-REL-01` comparing against `Application.version` | `TC-REL-01` |

## 3. The three new screens and the one new control

| Claim | Evidence | Case |
|---|---|---|
| HOW TO PLAY opens, contains 1,122 characters at 19 px, and closes back to the title | `test_logs_release/summary.txt`, `E2_how_to_play.png` | `TC-REL-03` |
| CREDITS opens and closes and shows the build line | `test_logs_release/summary.txt`, `E3_credits.png` | `TC-REL-02`, `TC-REL-04` |
| EXIT is bound to `DamaBoard.QuitGame`, and the two BACK buttons to their close methods | `test_logs_release/summary.txt` | `TC-REL-04` |
| Neither new panel is visible at the title, and neither survives into a match | `test_logs_release/summary.txt` | `TC-REL-05` |
| The title row is HOW TO PLAY / START / CREDITS with PROFILE and EXIT beneath | `E1_title.png`, `week17_shots.txt` (measured rects and anchored positions) | capture transcript |

## 4. Whole-game gates

| Claim | Evidence | Result |
|---|---|---|
| The release checks pass | `test_logs_release/summary.txt` + `case_log.tsv` + `console.log` | **5 passed, 0 failed, 0 console errors** |
| Everything from Weeks 13-16 still works on the frozen tree | `test_logs_regression/summary.txt` + `case_log.tsv` + `console.log` | **27 passed, 0 failed, 1 blocked, 0 console errors** |
| No control is smaller than 96x96 canvas px, and no two controls overlap | `test_logs_regression/summary.txt` | `TC-A11Y-01` 18 buttons / 0 below floor; `TC-A11Y-02` 0 overlapping pairs |
| The interface clears its contrast and type-size floors | `a11y_week17_after.log` | **148 passed, 0 failed, 0 warnings, 4 not measurable** |
| The audit ran against the same scene that was captured, after the duplicate-control fix | `a11y_week17_after.log` header (`Run at 2026-11-26 07:28:14 UTC`) versus `week17_shots.txt` (`Run at 2026-11-26 07:33:42 UTC`) | both post-fix, same scene, same scene file |
| The six submission images are six different pictures | `week17_shots.txt` | six SHA-256 prefixes, `All 6 images are byte-distinct` |

`a11y_week17_audit_20261126_151804.log` is the audit taken **before** the duplicate-control fix, when
`StartPanel` still held two copies of three buttons; it is retained because it is the run that exposed the
defect. `a11y_week17_after.log` and `a11y_week17_audit_20261126_152814.log` are the authoritative post-fix runs.
The difference (163 versus 148 passing checks) is exactly the 15 checks the duplicated controls contributed.

## 5. What is not measured, and where that is recorded

| Claim **not** made | Why | Where it is recorded |
|---|---|---|
| A clean install on an Android device | no APK was built and `adb` has no attached device | `clean_install.md`, `ENV-17-01`, `TC-COMPAT-01` BLOCKED |
| Device frame rate, memory, battery or thermal behaviour | no device run exists; Editor numbers are not device numbers | front page §6, technical documentation §6 |
| Compatibility across a second device or configuration | `TC-COMPAT-01` needs hardware | `test_logs_regression/summary.txt` |
| A participant usability study | no participant was available in Week 14 and none was added by assertion | Week 14 evidence, restated in the reflection |
| Contrast for `HUDPanel/TurnText`, `HUDPanel/CountsText`, `HUDPanel/PauseButton`, `ProfilePanel/NameInput` | no opaque backdrop / no own fill to measure against | `a11y_week17_after.log` (four `NOT MEASURABLE` rows) |
| That the game is understandable to a new player | an audit cannot see that; only the HOW TO PLAY text and its readability are evidenced | `E2_how_to_play.png`, `TC-REL-03` |

## 6. Repository hygiene

| Claim | Evidence |
|---|---|
| No credential, key or token is committed in the tree | `secret_scan.md` + `secret_scan_raw.txt` (the raw tool output, unedited) |
| The scene and assets are the ones described here | `architecture.svg` (scene and script map), `build_record.txt` (paths) |
| Nothing under `docs/Charibelle Guzman/Week_14|Week_15|Week_16/Evidence/` was rewritten | `git status` output in `build_record.txt` |

## 7. File inventory

```
Evidence/
  readiness_results.md                        the three gate counts and what they do not prove
  feature_freeze_checklist.md                 what was frozen, and the change sets accepted so far
  issue_disposition.md                        DEF-17-01…06 and ENV-17-01/02 with outcomes
  build_record.txt                            release identity, machine, scene, commit, BLOCKED APK step
  clean_install.md                            the install step that could not run, and its reason
  secret_scan.md, secret_scan_raw.txt         repository hygiene scan and its raw output
  architecture.svg                            scene, script and data map for this release
  a11y_week17_after.log                       the authoritative accessibility audit (148 / 0 / 0)
  a11y_week17_audit_*.log                     timestamped audit copies, one pre-fix and one post-fix
  test_logs_release/                          Week 17 release checks (5 / 0 / 0)
  test_logs_regression/                       Week 15 regression at Week_17 (27 / 0 / 1)
  E1_title.png … E6_end_screen.png            the six submission images
  week17_shots.txt                            capture transcript, sizes, SHA-256 of all six
```

## 8. How to re-run everything

| Gate | How |
|---|---|
| Release checks | `Tools/Dama/Week 17/2. Release Checks` |
| Regression at Week 17 | `Tools/Dama/Week 17/3. Regression Run` |
| Accessibility audit | `Tools/Dama/Week 17/0. Accessibility Audit` |
| Captures | `Tools/Dama/Week 17/4. Capture Evidence` (starts and stops play mode itself) |
| Release panels (rebuild) | `Tools/Dama/Week 17/1. Build Release Panels` |
| HTML from the markdown | `Tools/Dama/Submission/Build Week 17 HTML` |

Each harness writes into `Evidence/` and overwrites only its own outputs. None of them writes into the Week 13-16
folders, which are committed records.
