# Week 17 — readiness results

**Student:** Charibelle Guzman · BSIT-4 · MOBILE DEV 002, Module 5, Week 17
**Build under test:** `1.0.0-rc1`, version code `2`, application id `com.defaultcompany.dama`
**Tree under test:** `week16-final` (`4cce8c0`) plus the Week 17 working tree
**Environment:** Unity 6000.6.0f1, URP 17.6.0 (2D), WindowsEditor, game view 1151x584 — **Editor only. No APK was built and no handset was attached** (`ENV-17-01`, `TC-COMPAT-01`: BLOCKED).

All three runs below were taken against the same final scene, `Assets/Scenes/DamaGame.unity`, after the
defect in section 2 was corrected. Each number is the one printed by the harness, not an estimate.

## 1. The three gates

| Gate | Harness | Result | Target | Evidence |
|---|---|---|---|---|
| Week 17 release checks | `Assets/Scripts/Editor/Week17Checks.cs` → `Begin()` | **5 passed, 0 failed, 0 console errors** | 5 / 5 | `test_logs_release/summary.txt`, `case_log.tsv`, `console.log` — run 2026-11-26 07:28:46 UTC |
| Week 15 regression at `runWeek = "Week_17"` | `Assets/Scripts/Editor/Week15Checks.cs` → `BeginRelease()` | **27 passed, 0 failed, 1 blocked, 0 console errors** | 27 / 0 / 1 | `test_logs_regression/summary.txt`, `case_log.tsv`, `console.log` — run 2026-11-26 07:27:12 UTC |
| Accessibility audit (post-fix) | `Assets/Scripts/Editor/A11yAudit.cs` → `RunWeek17()` | **148 passed, 0 failed, 0 warnings, 4 not measurable** | strictly greater than Week 16's 105 | `a11y_week17_after.log` — run 2026-11-26 07:28:14 UTC |

The single blocked case is `TC-COMPAT-01`, and it is blocked on hardware, exactly as in Weeks 15 and 16: the run
records `BLOCKED: no APK was built this week (Editor-only evidence) and no Android handset was attached`. It is
reported as blocked rather than passed, which is the only honest option available.

**Week 17 release cases.** `TC-REL-01` reads `1.0.0-rc1 · Editor` off the title caption and matches it against
`Application.version`, so the string is derived and not typed into the scene. `TC-REL-02` reads the credits build
line `Build 1.0.0-rc1 · Editor · com.defaultcompany.dama` and confirms it agrees with the title and names the
application id. `TC-REL-03` opens HOW TO PLAY (1122 characters, 19 px) and closes it back to the title.
`TC-REL-04` confirms all five new listeners resolve to the method they claim —
`DamaBoard.ShowHowTo`, `ShowCredits`, `QuitGame`, `CloseHowTo`, `CloseCredits`, one call each.
`TC-REL-05` confirms HowTo and Credits are both off at the title and stay off after START, so neither panel can
leak into a running match.

**Regression.** All 27 previously passing cases pass again. That includes the ten rule cases, the five data cases
(including the corrupt-save recovery), the two boundary cases, and the accessibility cases in their corrected
form — `TC-A11Y-01` now measures **18 buttons** and 1 input field, 0 below the 96 px floor, and `TC-A11Y-02`
reports **0 overlapping pairs**.

**Accessibility audit.** 148 passes over 18 buttons and 1 input field, with 0 failures and 0 warnings. The audit
runs in this scene and reports the same four rows as not measurable, kept as such rather than given an invented
number:

| Row | Why it cannot be measured |
|---|---|
| `HUDPanel/TurnText` | white text drawn over the 3D board, no opaque backdrop |
| `HUDPanel/CountsText` | white text drawn over the 3D board, no opaque backdrop |
| `HUDPanel/PauseButton` | the control has no opaque panel behind it |
| `ProfilePanel/NameInput` | the input has no opaque fill of its own |

The count is 148 rather than the 163 an earlier pre-fix audit printed. That is not a regression: the three
duplicated title controls described in section 2 contributed 15 checks of their own (each duplicate has a touch
target, a fill and a label), and those checks left with them. 148 is still 43 more than Week 16's 105, because
Week 17 adds six controls and two panels.

## 2. What the run found — DEF-17-05

The first regression run at `Week_17` returned **26 passed, 1 failed, 1 blocked**, with

```
TC-A11Y-02 FAIL :: 3 pairs: StartPanel:HowToButton/HowToButton;
                  StartPanel:CreditsButton/CreditsButton; StartPanel:ExitButton/ExitButton
```

Each control was reported as overlapping *itself*, which is only possible if the same control appears twice in the
panel. It did. `Week17ReleasePanels.BuildReleasePanels()` called `DestroyIfExists` before rebuilding
`VersionLabel`, `HowToPanel` and `CreditsPanel`, but **not** before creating the three new title buttons. The
builder had been run twice, so the second run appended a second `HowToButton`, `CreditsButton` and `ExitButton` at
the same anchored position: `StartPanel` held 11 children, indices 4–6 and 8–10, each pair exactly coincident.

| | Before | After |
|---|---|---|
| `StartPanel` children | 11 (three names twice) | 8 (one of each) |
| `TC-A11Y-02` | 3 overlapping pairs — FAIL | 0 overlapping pairs — PASS |
| Regression total | 26 / 1 / 1 | 27 / 0 / 1 |
| Audit buttons counted | 21 | 18 |
| Audit total | 163 / 0 / 0 | 148 / 0 / 0 |

**Fix.** `DestroyIfExists` now keeps destroying until no child of that name is left, so a half-finished earlier
run cannot survive a rebuild, and the builder calls it for `HowToButton`, `CreditsButton` and `ExitButton` before
creating them. The builder was re-run, the scene re-saved, and all three gates re-run. The fix is in
`Assets/Scripts/Editor/Week17ReleasePanels.cs`; it is release tooling, so no gameplay code changed for it, and the
regression run above is what confirms that.

Row recorded in `issue_disposition.md`.

## 3. Harness corrections made while running

Two failures during this week were in the check, not in the game, and both are disclosed rather than quietly
fixed:

1. **`TC-REL-04` read `1 call(s): null.?`** on its first run, which made the first Week 17 release run
   **4 passed, 1 failed**. The case inspected `InvokableCall.Target` and `.Method`; uGUI's serialized persistent
   listeners do not populate those members, they hold the handler in the private `m_Delegate` field. Reading the
   delegate's `Target` and `Method.Name` gives `DamaBoard.ShowHowTo` and the rest. The run in section 1 supersedes
   the 4/1 run — the listeners were always correct.
2. **`FindFirstObjectByType`** was replaced with `FindAnyObjectByType` in the same pass; the scene has one
   `DamaBoard`, so the distinction does not change what was measured.

## 4. Evidence map

```
Week_17/Evidence/
  readiness_results.md          this file
  test_logs_release/            Week 17 release checks  (5 / 0 / 0)
    summary.txt  case_log.tsv  console.log
  test_logs_regression/         Week 15 regression at Week_17  (27 / 0 / 1)
    summary.txt  case_log.tsv  console.log
  a11y_week17_after.log         accessibility audit, post-fix  (148 / 0 / 0, 4 not measurable)
  a11y_week17_audit_*.log       the timestamped copy the audit writes alongside the stable log
  feature_freeze_checklist.md   section 1 of the week: what was frozen
  issue_disposition.md          every row opened this week and where it closed
```

## 5. What these results do not prove

- **No APK, no device.** No release candidate was built and no handset answered `adb`, so nothing here is
  evidence about an installed build, a real touchscreen, frame rate, memory, battery or thermal behaviour on a
  phone. `TC-COMPAT-01` stays BLOCKED and `clean_install.md` records what stands in its place.
- **Touch and contrast are canvas-pixel measurements.** 96 px and the contrast ratios come from the canvas with
  the 1280x720 reference scaler, converted with the derivation written in the header of `a11y_week17_after.log`.
  They are not ruler-and-colorimeter readings from a handset.
- **An audit is a floor.** It can show that every measurable control clears five thresholds. It cannot show that
  a player understood the game; that needs a participant, and none was available this week.
- **The four not-measurable rows stay open by name.** They are listed above so that a reviewer reads them as
  unmeasured, not as passes.
