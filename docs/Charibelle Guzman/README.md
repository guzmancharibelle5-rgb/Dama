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
| 15 | Game Testing, Defect Correction and Milestones | - | not started |
| 16 | Game Performance and Optimization | - | not started |
| 17 | Release Preparation and Documentation | - | not started |
| 18 | Final Project, Demonstration and Defense | - | not started |

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
| **Text size does not scale** (WCAG 2.2 SC 1.4.4) | The five button captions are 14 px, the smallest strings in the game. The audit measures contrast and box height, not type size, so it cannot see them. Deferred as finding M1 rather than changed late, because changing them would have invalidated every capture and the audit baseline |
| There is **no screen-reader path** | uGUI exposes no accessibility tree. A voice-over user cannot play this game. Fixing it needs a native Android bridge, which is not in this submission - stated in SA6 section 6 rather than implied |
| Contrast margins are **thin, not comfortable** | Two controls pass at 4.7:1 against 4.5, and `ProfilePanel/NameInput/Placeholder` passes at 3.01:1 against 3.0. The next edit to any of those fills or sizes puts them back under the line, and nothing will warn anyone. The audit is to be re-run before every submission |
| **No route back to the start screen** once a match begins | The pause panel offers RESUME, RESTART and SOUND; PROFILE exists only on the start screen, so a name or side cannot be changed mid-match. Found by the machine pre-test as M2 (S2), the highest-severity finding of Week 14, and deferred to Week 15's defect log |
| **No APK was produced this week** | Week 13's runtime evidence is the Editor in play mode, captured from the real scene and the real save file. The Android build settings are applied and recorded, but "the game runs in the Editor" and "the game was installed on a phone" are different claims, and only the first is made here |
| The profile panel has **never run on a physical phone** | All captures are at the Editor's 1151x584 game view. A phone's aspect ratio and touch precision are untested |
| The UI's earlier scaler note was wrong | Week 13's README said the UI had **no** `CanvasScaler`. It has one: the Week 14 audit header reads `Scale With Screen Size, reference 1280x720, match 0.50`. That scaler is what makes the touch floor definable at all - 48 dp at xxhdpi is 144 physical px, and 144 / 1.50 = 96 canvas px - and it is why the audit measures canvas pixels against a stated reference instead of raw pixels. When it was added is not recorded; this row replaces the old claim rather than dating it |
| Commits are still **per week, not per step** | Week 13 said splitting commits per working step would start in Week 14, and it did not: Week 14 landed as one code commit and one documentation commit. The cost is that the three repair-script bugs are invisible in the history. Admitted rather than quietly dropped, in the Week 14 reflection section 4 |
| The project has **domain reload on play-mode entry disabled** | Fast iteration, but Editor-time statics survive into play mode. Every harness run resets the store to the default data directory first |

---

## Responsible AI use

Code, document structure and phrasing were produced with AI assistance and then read, run and
corrected against the actual build. Every measured value in PT5 section 6 is copied from
`pt5_checks.log`, which the harness generated; none is typed from memory. The same rule holds in Week
14: every ratio, size and paired value in PT6 section 3 comes from `a11y_delta.txt`, generated by
pairing the two logs mechanically. The full disclosure tables are PT5 section 9 and PT6 section 10,
and both reflections carry the required declaration.

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
| Build target | Android, ARM64 only, IL2CPP, minimum API 26, `com.defaultcompany.dama` |
| Scenes in build | `DamaGame.unity` (index 0), `SampleScene.unity` (disabled) |

`.gitignore` excludes `*.apk`, `*.aab`, `*.unitypackage`, `*.keystore`, `*.jks` and the
assistant's own working folders, so no build output or signing key can be committed by
accident. See `Week_13/Evidence/secrets_and_gitignore.txt`.
