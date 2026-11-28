# PA2 — Technical Report

**Student:** Charibelle Guzman — BSIT-4
**Course:** MOBILE DEV 002 Advanced Mobile Application Development, Module 5
**Week:** 18 · **Assessment:** PA2
**Engine:** Unity 6000.6.0f1 · URP 17.6.0 (2D Renderer) · Android, IL2CPP, ARM64, minimum API 26
**Version under test:** `1.0.0-rc1` (version code `2`), `com.defaultcompany.dama`

---

## 1. Scope of this report

This report describes the code that is being submitted, names the file each behaviour lives in, and
records the defects found across Weeks 13–18 and how each one closed. It does not describe a phone,
because no build was made and no phone was attached; §9 states what that costs.

## 2. System shape

One scene, `Assets/Scenes/DamaGame.unity`, at build index 0. The scene holds a `GameCanvas`, a board
root, and one `DamaBoard` component that owns both the rules and the UI wiring.

| Script | Kind | Responsibility |
|---|---|---|
| `Assets/Scripts/DamaBoard.cs` | runtime | 8x8 `Cell[,] grid`, parallel `bool[,] isKing`, move generation, chains, crowning, win detection, `GameState`, the seven panels, HUD |
| `Assets/Scripts/Profile/PlayerProfile.cs` | runtime, static | the save file: load, clamp, sign, write, reset; the only class that touches it |
| `Assets/Scripts/Profile/ProfilePanel.cs` | runtime | the profile screen: name field, record, best times, side toggle, two-press reset |
| `Assets/Scripts/Core/ReleaseInfo.cs` | runtime, static | `Version`, `BundleId`, `Engine`, `Platform`, `Caption()`, `BuildLine()` |
| `Assets/Scripts/Core/GameFeedback.cs` | runtime | move and end-of-match audio |
| `Assets/Scripts/BoardCameraFitter.cs` | runtime | frames the board for the current aspect ratio |
| `Assets/Scripts/MobileRuntimeConfig.cs` | runtime | mobile runtime settings applied at startup |

The boundary that matters: `DamaBoard` decides *what happened*, `ProfilePanel` decides *what the player
sees*, and `PlayerProfile` decides *what is stored*. No other class opens the save file, which is what
makes the data rules (clamping, signature, atomic write) enforceable in one place.

## 3. The board

The grid is a `Cell[,]` of 8x8 — `DamaBoard.Size` is the single source for every loop — plus a parallel
`bool[,] isKing`. A square holds either nothing, a man of one side, or a crowned man; the king flag is
kept beside the grid rather than encoded twice, so a crowning changes one boolean and one cell.

Three rules are implemented as constraints on generation rather than as checks after the fact:

1. **Capture is compulsory.** `GetCaptureMoves` is consulted first; if it returns anything, the simple
   moves are never offered. That is why a player who taps a man with no legal move gets nothing, and why
   the UI has no "invalid move" path to explain.
2. **Captures chain.** After a jump lands, the generator is asked again from the landing square; the
   chain ends only when no continuation exists.
3. **The Dama flies.** A king's move walks the diagonal in both directions until the edge or a piece;
   a jump over an enemy it can reach is legal from any distance.

Win detection is one predicate used everywhere: `CountPieces(side) == 0 || !HasAnyLegalMove(side)`.
It runs after a move and when a seeded position is checked, so the end screen and the tests agree.

**A contract worth naming.** `GetCaptureMoves`, `GetSimpleMoves` and `GetSelectableMoves` return a
**shared buffer** that is valid only until the same generator is next called. Every current caller
consumes it immediately and stores nothing — audited call by call in Week 16 — and the contract is
written into the field comment, because a future caller that keeps the list would break the board
silently, with no error.

## 4. State machine and panels

`GameState` is `Ready`, `Playing`, `Paused`, `GameOver`, `Profile`, `HowTo`, `Credits`. `ShowState()` is
the only place a panel is toggled, and every panel is reached from exactly one state:

| State | Panel(s) shown |
|---|---|
| `Ready` | `StartPanel` |
| `Playing` | `HUDPanel` |
| `Paused` | `HUDPanel` + `PausePanel` |
| `GameOver` | `HUDPanel` + `WinPanel` |
| `Profile` | `ProfilePanel` |
| `HowTo` | `HowToPanel` |
| `Credits` | `CreditsPanel` |

The title screen carries five controls in two rows — HOW TO PLAY / START / CREDITS, then PROFILE / EXIT —
plus a version caption anchored top-left. Each control is bound by path in `CacheUI()`
(`WireButton(canvas, "StartPanel/StartButton", StartGame)`), so a control that exists but was never wired
is inert. `TC-REL-04` exists precisely to catch that: it reads the bound delegate off each button and
names the method it resolves to.

`ExitButton` calls `QuitGame()`, which logs the build line and then quits — `EditorApplication.isPlaying
= false` under the editor, `Application.Quit()` in a build. The wire is proven (`TC-REL-04`); the
behaviour on Android is not, because no build has run.

## 5. Player data

`PlayerProfile` stores a JSON envelope: a schema number, a payload, and an HMAC signature over the
payload. The key is a per-install salt kept beside the file.

| Rule | Implementation |
|---|---|
| Atomic write | payload written to `<file>.tmp`, then moved over the real file |
| Corruption handling | a file whose signature fails is copied aside as a rejected record and a default profile is loaded |
| Clamping | every numeric field is clamped to its legal range on load, so a hand-edited 99 wins cannot become 99 |
| Single bank | the match id is fixed when the match starts; a second end-of-match call for the same id is refused |
| One writer | no other class reads, signs, writes or deletes the file |

On Android the store lives under `Application.persistentDataPath`, which is built from `companyName`
and `productName` — `…\LocalLow\DefaultCompany\dama\Dama\profile.json`. That is why neither of those
two fields may change: moving them moves the store and looks exactly like a wipe. The version lives in
`bundleVersion`, which is what `Application.version` reads.

**Stated limit.** The signature makes the save tamper-evident, not secret. Key and file share a device,
and there is no server; the record is a courtesy, not a boundary.

## 6. Release identity

`ReleaseInfo` derives everything from `Application.*` and holds no state:

| Member | Source |
|---|---|
| `Version` | `Application.version` → `1.0.0-rc1` |
| `BundleId` | `Application.identifier` → `com.defaultcompany.dama` |
| `Engine` | `Application.unityVersion` → `6000.6.0f1` |
| `Platform` | `Application.platform` → `Editor` here, `Android` in a build |
| `Caption()` | version · platform → `1.0.0-rc1 · Editor` |
| `BuildLine()` | `Build <version> · <platform> · <bundle id>` |

Both on-screen strings are written by `ReleaseInfo` at runtime, never typed into a scene label, and
`TC-REL-01`/`TC-REL-02` compare them back to `Application.version` so they cannot drift.

## 7. Editor tooling

Building the UI in code rather than by hand is what makes the scene reproducible, and it has a matching
risk: a builder that runs twice can duplicate what it builds.

| Tool | Menu | What it does |
|---|---|---|
| `DamaBoardBuilder` | `Tools/Dama/Build Board` | board, squares, HUD, pieces root |
| `ProfileSceneBuilder` | `Tools/Dama/Build Profile UI` | `StartPanel/ProfileButton`, `ProfilePanel` and its children |
| `Week15Fixes` | `Tools/Dama/Week 15` | pause/end MENU buttons, caption sizes |
| `Week17ReleasePanels` | `Tools/Dama/Week 17/1` | version caption, HOW TO PLAY, CREDITS, EXIT |
| `AccessibilityBuild` | `Tools/Dama/Accessibility` | touch floors, caption sizes, contrast fills |
| `AndroidBuildSetup` | `Tools/Android` | ARM64, IL2CPP, build scenes, per-platform quality |
| `SubmissionDocBuilder` | `Tools/Dama/Submission` | markdown → print-ready HTML per week |

**The duplicate-builder defect is the important lesson here.** `Week17ReleasePanels` had no cleanup for
the three title buttons it created, so a second run appended a second HOW TO PLAY / CREDITS / EXIT at the
same anchored positions: `StartPanel` held 11 children instead of 8, and the accessibility case reported
each control as overlapping *itself* (`DEF-17-05`). `DestroyIfExists` now loops until nothing with that
name remains, and the regression run is what proves the fix.

## 8. Defect history

| Id | Severity | What it was | How it closed |
|---|---|---|---|
| `DEF-15-01` | S2 | no route back to the title once a match began | `PausePanel/MenuButton`, `WinPanel/MenuButtonWin`, `ReturnToMenu()` — `TC-FLOW-06`, `TC-RULE-11` |
| `DEF-15-02` | S3 | text did not scale with the OS setting | closed for the **size floor only** (captions to 22 px bold, status to 20 px); no dynamic-type path exists |
| `DEF-17-02` | S2 | no exit control at all | `StartPanel/ExitButton` → `QuitGame()`; `TC-REL-04` names the listener |
| `DEF-17-05` | S3 | a second builder run left duplicate title controls | `DestroyIfExists` loops; `StartPanel` back to 8 children; `TC-A11Y-02` 0 overlaps |
| `DEF-17-06` | S3 | the release check read the wrong field of a uGUI listener and reported `null` | the check reads the delegate's `Target`/`Method.Name`; the listeners were always correct |
| Week 18 | — | three harness faults in the demonstration capture (refused `ShowProfile`, 0.0 s best times, real store gained a match per run) | see `Week_18/Evidence/final_results.md` §3 — all three fixed, all three disclosed |

## 9. Test architecture

Every harness is an editor script that enters play mode, drives the game through its own entry points,
measures what it can read back, and writes a log. None of them asserts a number it did not measure.

| Harness | Cases | Output |
|---|---|---|
| `Week13Checks` | 15 data and security cases | `Week_13/Evidence/pt5_checks.log` |
| `Week14Checks` + `A11yAudit` | usability, first audit (50) | `Week_14/Evidence/a11y_*.log` |
| `Week15Checks` | 27 cases: rules, data, boundaries, a11y, flow | `Week_15/Evidence/test_logs_final/` |
| `Week16Profile` | three profiling runs, same 32 turns | `Week_16/Evidence/profile_*` |
| `Week17Checks` | `TC-REL-01…05` | `Week_17/Evidence/test_logs_release/` |
| `Week17Captures` | six images + distinct-hash gate | `Week_17/Evidence/week17_shots.txt` |
| `Week18Captures` | nine images + distinct-hash gate | `Week_18/Evidence/demo_rehearsal.txt` |
| `A11yAudit` | 203 checks over 27 buttons and 1 field | `Week_18/Evidence/a11y_week18_after.log` |

Two things the harnesses do deliberately: they seed positions **by reflection** and then rebuild the
visible pieces from the grid, so an image can never disagree with the state it claims; and they hash
every capture and fail on a collision, because the first Week 17 run wrote six names over four pictures.

## 10. Commit history and reproducibility

| Commit / tag | What it carries |
|---|---|
| `b9a612d`, `0f01608` | Week 13: signed player profile, PT5 package |
| `1e27cc2`, `8adbce3` | Week 14: accessibility audit, feedback, PT6 package |
| `fdb3cc1`, `3761802` | Week 15: test harness, defect fixes, AS3 milestone |
| `aacfc7f`, `356e99f`, `bb7e7f3`, `e5b6c1a` | Week 16: profiling harness, two optimizations, SA6/AS3 |
| `week16-final` → `4cce8c0` | the frozen baseline Week 17 was built on |
| `b8e69b9`, `d2d3ad8`, `a3da06c` | Week 17: release identity, three screens, release package |
| `v1.0.0-pa2` | the Week 18 submission commit |
| post-Week-18 revision | the side choice, the turned board, PLAYER vs AI, the draw rule, UNDO, HINT and the move animation - hashes in `../README.md`, repository-state table |

Every result in Weeks 13–18 can be regenerated: each harness has a menu entry and each week's
`HOW_TO_RERUN.txt` names the order. The demonstration is
`Tools > Dama > Week 18 > 1. Capture Demonstration`; the gates are `Week 17/2`, `Week 18/2` and
`Week 18/3`.

## 11. What this report does not claim

- No APK, no install, no device. The installable-build row is BLOCKED with its reason.
- No phone performance: no frame rate, battery, thermal or memory figure in this project is a device figure.
- No human playtest: the Week 14 kit was never run with a volunteer.
- No dynamic type and no screen-reader path.
- The oral defense is not described as passed; `Evidence/e3_record.md` is blank by design.
- The eight requirements below were added **after** the Week 18 submission and are not graded
  work. They are recorded here because they are in the tree the repository now holds, and a tree
  nobody describes is worse than one described with its limits.

## 12. Requirements added after the Week 18 submission

Eight requirements were written for work that came after PA2 was submitted: the RED/BLUE side
choice, the turned board, PLAYER vs AI, the two draw counts, UNDO, HINT and the move animation.
They are numbered from `REQ-23` so they cannot be confused with the Week 15 set (`REQ-01…REQ-22`,
`Week_15/Evidence/requirements.md`), which is left exactly as it was measured.

| # | Requirement | Where it lives | Evidence | Standing |
|---|---|---|---|---|
| REQ-23 | The player's side (RED or BLUE) is chosen on CHOOSE MODE or on PROFILE and kept in the saved record | `ModePanel/SideButton`, `ProfilePanel/SideButton`, `DamaBoard.SelectSide`, `PlayerProfile.SetSide` - the payload and `SchemaVersion 1` are unchanged, so no field was added to the signed record | `Guzman_Charibelle_PA2_User_Guide.pdf` §2.1 and §5; the store is still covered by `TC-DATA-01…04` in `Evidence/test_logs_regression/` | implemented; no new payload field, no separate log |
| REQ-24 | A BLUE player sees the board turned so the blue men are nearest them, and a tap still selects the piece under the finger | `DamaBoard.ApplyBoardView` (180° about Z on the board root), tap mapped through `transform.InverseTransformPoint` | `Guzman_Charibelle_PA2_User_Guide.pdf` §2.1; `Week17Checks`/`Week18Captures` still pass because `StartGame` forces the identity view | implemented; confirmed by playing in the Editor, no log |
| REQ-25 | PLAY opens PLAYER vs AI with EASY, NORMAL and HARD, and the AI obeys the same rules as the human | `DamaBoard.StartVsAi`, `DamaAI.Choose`, `AiLevel` | `Evidence/ai_checks_postweek18.log` - rules parity on 200 positions x 2 sides with **0 disagreements**, identical mandatory capture at all three levels, **HARD won 10/10** against EASY, AI turn moves a BLUE piece and hands the move back to RED | PASS |
| REQ-26 | A position that appears for the third time, with the same side to move, ends the match in a draw | `DamaPlyHistory.Verdict` (FNV-1a key over cells, kings and side to move), `DamaBoard.EndTurn` -> `DeclareDraw` | `Evidence/postweek18_features.log` case **A**: 8 plies, `DRAW.  RED 1-1, the same position 3 times.`, `draws 2->3` | PASS |
| REQ-27 | 50 plies without a capture or a promotion end the match in a draw | the no-progress counter in `DamaPlyHistory`, `DamaBoard.DeclareDraw` | `Evidence/postweek18_features.log` case **B**: 50 plies, counter 50, `DRAW.  RED 1-1, 50 plies without a capture.`, `draws 3->4` | PASS |
| REQ-28 | UNDO in the pause panel returns the board, both piece counts, the side to move and the captured piece to the last human turn | `PausePanel/UndoButton`, `DamaBoard.UndoPressed`, `DamaPlyHistory.TryPop` | `Evidence/postweek18_features.log` case **C**: capture left RED 12 / BLUE 11, after undo RED 12 / BLUE 12, turn RED, the captured piece re-activated at (1,4) under `Pieces`; audit row `PausePanel/UndoButton` 272x96 with its label at **6.24:1** | PASS |
| REQ-29 | HINT paints a legal move for the side to move and moves nothing | `HUDPanel/HintButton`, `DamaBoard.HintPressed`, `DamaAI.Choose` at `AiLevel.Easy` | `Evidence/postweek18_features.log` case **D**: opening (3,2)->(2,3), forced capture (3,5)->(1,3) and flying king (3,3)->(2,2) all reported legal by the board's own generator, piece count unchanged; audit row `HUDPanel/HintButton` 160x96 at **12.44:1** | PASS |
| REQ-30 | A move travels for 120 ms instead of teleporting, and a capture pops | `DamaBoard.AnimatePieceTo` (ease-out on `localPosition`, unscaled), `CapturePop` | none - the four edit-mode cases snap the piece on purpose, so the travel is **not measured** anywhere | implemented, **not measured** |

The accessibility audit was re-run over the tree that contains these changes: **203 passed, 0 failed,
0 warnings, 6 not measurable** (`Evidence/a11y_week18_after.log`; the four row totals were 148 / 0 / 0
/ 4 at the PA2 submission). The six not-measurable rows are the three HUD texts, PAUSE, HINT and the
name input - all drawn over the 3D board with no opaque backdrop, so they are reported, never passed.
