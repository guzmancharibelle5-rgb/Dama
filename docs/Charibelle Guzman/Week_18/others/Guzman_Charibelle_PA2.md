# PA2 — Final Project, Demonstration and Defense

**Student:** Charibelle Guzman — BSIT-4
**Course:** MOBILE DEV 002 Advanced Mobile Application Development, Module 5
**Week:** 18 · **Assessment:** PA2
**Game:** Dama — the Philippine form of draughts
**Theme:** Philippine Games and Sports

---

## 1. Submission at a glance

| Item | Value |
|---|---|
| Submitted tree | `main` at the PA2 commit, tagged `v1.0.0-pa2` |
| Submitted binary | **none — BLOCKED**, see §3 |
| Version the app reports | `1.0.0-rc1` (Android version code `2`) |
| Application id | `com.defaultcompany.dama` |
| Engine | Unity 6000.6.0f1, Universal Render Pipeline 17.6.0 (2D Renderer) |
| Build settings as configured | Android, IL2CPP, ARM64 only, minimum API 26, target API automatic, debug signing only |
| Scene | `Assets/Scenes/DamaGame.unity`, build index 0 |
| Demonstration | nine captures D1–D9, `Evidence/demo_rehearsal.txt` |
| Final checklist | `Evidence/final_pre_submission_checklist.md` |
| Gate results | `Evidence/final_results.md` — 5/0 release, 27/0/1 regression, 148/0/0/4 audit |

**The submission is the source tree, not a package.** No Android build was produced in this project in
any week, and this document says so in three places rather than implying otherwise: here, in §3, and in
`Evidence/release_record.md`. Everything that is claimed below was measured on the tree that is being
submitted, in the Editor, at a 1151x584 game view.

## 2. The game

*Dama* is draughts as it is played in the Philippines: an 8x8 board, twelve men a side, two people
taking turns on one phone. Three rules make it Dama rather than checkers:

- **A capture must be taken.** If a jump exists for the player to move, the non-capturing moves are not
  offered at all — they are not legal moves that are discouraged, they are absent.
- **A capture chains.** A man that lands on a square from which another jump is available keeps going
  until the chain ends on its own.
- **The king flies.** A man that reaches the far row is crowned a *Dama* and may move any distance
  along any diagonal, in any direction.

A match ends when a side has no men left, or has men but no legal move.

**Target player.** Students and young adults who played *agawan base*, *patintero* or *dama* as
children, on a mid-range Android phone, one device shared by two players. That decided three things
that are visible in the build: every control clears a 96 canvas-pixel touch floor because two people
are reaching for the same glass; the whole board is drawn in 2D from the top so a player can read it
across a table; and the profile system records a local record because the two players are not on
separate devices and have no accounts.

## 3. The submitted build — and why there is none

The module asks for an installable build. This submission does not contain one. Three independent
reasons, each sufficient:

1. **No package was ever produced.** Weeks 13–18 were scoped to Editor evidence by decision, and no
   Android build exists for this project.
2. **No signing key exists.** `androidUseCustomKeystore: 0`, and the keystore, alias and password fields
   are empty. Anything this machine built would be debug-signed.
3. **No device answers `adb`.** Unity's bundled platform-tools return `List of devices attached` with no
   serial, so a package could not have been installed or launched anyway.

This is recorded as **BLOCKED**, not as a pass, in `Evidence/final_pre_submission_checklist.md` and in
`Evidence/release_record.md` §3, and `TC-COMPAT-01` remains BLOCKED in the regression suite. The full
record of the attempt, including what the Editor evidence is and is not worth, is
`Week_17/Evidence/clean_install.md`.

## 4. The demonstration sequence

Nine screens, one run, nine distinct SHA-256 values. Full transcript with offsets in
`Evidence/demo_rehearsal.txt`.

| Step | What it demonstrates | Capture |
|---|---|---|
| Title | onboarding, version caption derived from the build | `D1_title.png` |
| How to play | the rules in 1,122 characters fitted to 19 px | `D2_how_to_play.png` |
| Credits | author, course, engine, licence, AI-assistance statement, build line | `D3_credits.png` |
| Profile | local record, best times, side toggle, name field | `D4_profile_record.png` |
| Profile after reset | the two-press confirmation erasing the record | `D5_profile_after_reset.png` |
| Fresh match | the core loop at 12 v 12, HUD naming the turn | `D6_match_fresh.png` |
| After a capture | theme mechanic: a mandatory jump, counts following the board | `D7_match_after_capture.png` |
| Pause | settings: RESUME, RESTART, SOUND, MENU | `D8_pause.png` |
| End screen | completion: RED wins because BLUE has no legal move | `D9_end_screen.png` |

The demonstration covers the module's list in order — onboarding, core loop, theme mechanics, settings,
data features, error handling (the reset confirmation), completion and progression. It does **not**
cover a handset, because there is none.

## 5. Architecture at a glance

| Piece | File | What it owns |
|---|---|---|
| Board and rules | `Assets/Scripts/DamaBoard.cs` | the 8x8 grid, move generation, chains, crowning, `GameState`, the seven panels |
| Player data | `Assets/Scripts/Profile/PlayerProfile.cs` | the signed save file and every read and write of it |
| Profile screen | `Assets/Scripts/Profile/ProfilePanel.cs` | the name field, record, best times, side toggle, two-press reset |
| Release identity | `Assets/Scripts/Core/ReleaseInfo.cs` | `Version`, `BundleId`, `Engine`, `Platform`, `Caption()`, `BuildLine()` — all from `Application.*` |
| Feedback | `Assets/Scripts/Core/GameFeedback.cs` | the move and end-of-match audio |
| Camera | `Assets/Scripts/BoardCameraFitter.cs` | frames the board for the current aspect ratio |

`DamaBoard` holds rules and UI wiring; it does not know how a save is serialised. `PlayerProfile` is the
only class that touches the save file. `ProfilePanel` never parses JSON — it asks `PlayerProfile`.

**State machine.** `GameState` is `Ready`, `Playing`, `Paused`, `GameOver`, `Profile`, `HowTo`,
`Credits`. `ShowState()` is the only place a panel is turned on or off; every panel is therefore
reachable and every panel is hideable, which is what `TC-REL-05` checks.

## 6. Security and player data

The save is a JSON envelope with a schema number, a payload and an HMAC signature over the payload. The
key is a per-install salt stored beside the file. The store is written atomically — temp file, then
move — and a file that fails its signature is set aside as a rejected copy instead of being trusted. Every
numeric field is clamped on load, and the match id gate refuses a repeated end-of-match bank.

**What that is worth, stated plainly:** the save is **tamper-evident, not encrypted**. The key and the
file live on the same device and there is no server to check a record against, so a determined player
with a debugger can rewrite their own stats. The design goal was to make an accidental corrupt file
recoverable and a casual edit detectable, not to defend against the device's owner. That limit is
recorded in `Week_13/Evidence/pt5_checks.log` as `L15 KNOWN LIMIT` and is not counted as a pass.

## 7. Testing and evidence

| Gate | Result | Where |
|---|---|---|
| Week 17 release checks re-run on this tree | **5 passed, 0 failed, 0 console errors** | `Evidence/test_logs_release/` |
| Full regression re-run on this tree | **27 passed, 0 failed, 1 blocked** (`TC-COMPAT-01`) | `Evidence/test_logs_regression/` |
| Accessibility audit | **148 passed, 0 failed, 0 warnings, 4 not measurable** | `Evidence/a11y_week18_after.log` |
| Demonstration | nine distinct captures | `Evidence/D1…D9.png`, `demo_rehearsal.txt` |
| Historical weeks | Weeks 13–17 evidence, untouched | `Week_13` … `Week_17` |

The regression suite is the same 27 cases Weeks 15–17 ran, including the ten rule cases, the five data
cases (one of which recovers a deliberately corrupted save), the two boundary cases and the corrected
accessibility cases.

## 8. Optimisation

Week 16 measured first, then changed two things: move generation was made allocation-free and pieces
were pooled instead of destroyed. On the same scene and the same 32-turn replay:

| Measure | Before | After |
|---|---|---|
| Allocation per legality scan | 6,975.9 B | −0.1 B ± 0.1 (below the method's resolution) |
| Allocation per restart | 5,596 B | ~1.7 KB or less |
| Frame time (mean) | 2.062 ms | unchanged within run noise |

The frame claim is deliberately not made: the frame was already far inside a 33.3 ms budget before the
change, and the mean moved with the Editor session rather than with the code. What is proven is that the
allocation went away. Every performance number in this project is an **Editor** number; nothing is known
about a phone's frame rate, battery or thermals.

## 9. Limitations and honest gaps

| Limitation | Standing |
|---|---|
| **No APK, no handset** | `TC-COMPAT-01` BLOCKED; §3 of this document; nothing claims a device |
| The save is tamper-evident, not encrypted | recorded as a known limit since Week 13 |
| **No human playtest has been run** | the Week 14 kit exists (consent, script, task cards, observation sheet); zero volunteers have played |
| Text does not follow the OS font scale | WCAG 2.2 SC 1.4.4 is not met; the audit measures a size floor, not a dynamic-type path |
| No screen-reader path | uGUI exposes no accessibility tree; a native bridge would be needed |
| Contrast margins are thin | three fills pass the 3:1 line at 3.16–3.19:1; the next edit to those fills can undo it |
| Haptics unproven | `Handheld.Vibrate()` is a no-op in the Editor, and every capture is an Editor capture |
| `EXIT` bound, never fired on Android | `TC-REL-04` proves the wire to `DamaBoard.QuitGame`; `Application.Quit()` has never run, because no build has |
| The version caption reads `· Editor` | a build would read `· Android`; nobody has seen that string |
| An audit is a floor | it cannot show that a player understood the game |

## 10. What is in this folder

```
Week_18/
  Guzman_Charibelle_PA2.pdf                          this document
  Guzman_Charibelle_PA2_Technical_Report.pdf         architecture, data flow, defects, harnesses
  Guzman_Charibelle_PA2_User_Guide.pdf               how to play, the screens, troubleshooting
  Guzman_Charibelle_PA2_Evidence_Portfolio.pdf       every claim mapped to the file that proves it
  Guzman_Charibelle_PA2_Originality_Declaration.pdf  the declaration, unsigned in this copy
  Guzman_Charibelle_PA2_Presentation.pdf             the defense deck
  Guzman_Charibelle_PA2_Presentation_Speaker_Notes.pdf
  Guzman_Charibelle_Week18_Reflection.pdf
  others/                                            the markdown sources and their HTML
  Evidence/                                          checklist, release record, gates, captures, defence pointers
```

## 11. Responsible AI use

Code, document structure and phrasing were produced with AI assistance and then read, run and corrected
against the actual build. Every number in this package is copied from a harness log or a command output:
the gate counts from `Evidence/test_logs_*` and `a11y_week18_after.log`, the capture hashes from
`demo_rehearsal.txt`, the optimisation numbers from the Week 16 `summary.txt`/`metrics.md`. Where the
evidence cannot support a claim — device behaviour, frame time, a playtest, the defense itself — the
documents say so instead of quoting a number. The full disclosure table is section 6 of
`Guzman_Charibelle_PA2_Originality_Declaration`.
