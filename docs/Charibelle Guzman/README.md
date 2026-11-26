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
| 14 | Game Usability, Accessibility and Playtesting | - | not started |
| 15 | Game Testing, Defect Correction and Milestones | - | not started |
| 16 | Game Performance and Optimization | - | not started |
| 17 | Release Preparation and Documentation | - | not started |
| 18 | Final Project, Demonstration and Defense | - | not started |

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
| **No APK was produced this week** | Week 13's runtime evidence is the Editor in play mode, captured from the real scene and the real save file. The Android build settings are applied and recorded, but "the game runs in the Editor" and "the game was installed on a phone" are different claims, and only the first is made here |
| The profile panel has **never run on a physical phone** | All captures are at the Editor's 1151x584 game view. A phone's aspect ratio and touch precision are untested |
| The UI has **no `CanvasScaler`** | The panel is authored with fractional anchors so it scales, but absolute pixel offsets in a panel without a scaler overflow a short canvas - this actually happened once and the panel was rebuilt |
| Week 13 is **one large commit** | The work is committed and the hash is in the documents, but the history does not show each fix. Splitting commits per working step starts in Week 14 |
| The project has **domain reload on play-mode entry disabled** | Fast iteration, but Editor-time statics survive into play mode. Every harness run resets the store to the default data directory first |

---

## Responsible AI use

Code, document structure and phrasing were produced with AI assistance and then read, run and
corrected against the actual build. Every measured value in PT5 section 6 is copied from
`pt5_checks.log`, which the harness generated; none is typed from memory. The full disclosure
table is PT5 section 9, and the reflection carries the required declaration.

---

## Repository state

| Item | Value |
|---|---|
| Branch | `main` |
| Code commit (Week 13) | `b6b477b` |
| Documentation commit (Week 13) | `a215f34` |
| Tracked files after the commit | 190 |
| Build target | Android, ARM64 only, IL2CPP, minimum API 26, `com.defaultcompany.dama` |
| Scenes in build | `DamaGame.unity` (index 0), `SampleScene.unity` (disabled) |

`.gitignore` excludes `*.apk`, `*.aab`, `*.unitypackage`, `*.keystore`, `*.jks` and the
assistant's own working folders, so no build output or signing key can be committed by
accident. See `Week_13/Evidence/secrets_and_gitignore.txt`.
