# PT7 - Release Preparation and Documentation

**Game:** Dama
**Student:** Charibelle Guzman
**Section:** BSIT-4
**Course:** MOBILE DEV 002 Advanced Mobile Application Development, Module 5
**Theme:** Philippine Games and Sports
**Week:** 17 - Release Preparation and Documentation
**Release identity:** `1.0.0-rc1`, version code `2`, application id `com.defaultcompany.dama`
**Engine:** Unity 6000.6.0f1, Universal Render Pipeline 17.6.0 (2D Renderer), Input System 1.20.0
**Platform range:** Android 8.0 (API 26) and above, ARM64, IL2CPP
**Repository:** local Git repository, branch `main`, tree frozen at tag `week16-final` (`337b436`)
**Produced:** 2026-11-26

---

## 1. What this week was asked to produce, and what it did

The week asks for a game that can be handed to somebody else: a clear version identity, credits, a way out
of the game, instructions a new player can follow, and proof that the things built in Weeks 13-16 still work
on the frozen tree. The package below is that hand-over.

| Module step | What it asks for | What this build does | Proof |
|---|---|---|---|
| Settings | release settings and an identifier | `bundleVersion` raised `1.0` → `1.0.0-rc1`, version code `1` → `2`; `productName` and `companyName` deliberately unchanged | `Evidence/build_record.txt` |
| Credits | a credits surface | a new CREDITS panel: author, course, engine, licence and AI-assistance statement, plus a build line naming this build | E3, `TC-REL-02` |
| Exit or restart | exit or restart behaviour | EXIT on the title screen calls `DamaBoard.QuitGame()`; RESTART exists on both the pause and end screens | E1, E5, E6, `TC-REL-04` |
| Clear instructions | instructions or a tutorial | a new HOW TO PLAY panel with 1,122 characters at 19 px, reachable in one tap from the title | E2, `TC-REL-03` |
| Unique version | a version number and build identifier | the title caption and the credits build line are both **derived from `Application.version`** and never typed in | E1, E3, `TC-REL-01`, `TC-REL-02` |
| Install on a device | a clean install on the target device | **BLOCKED.** No APK was built and no handset was attached; see section 6 | `Evidence/clean_install.md` |

Three of those six rows are new work, one is a settings change, one was already true and is now provable, and
one cannot be done in this environment at all. The last one is recorded as blocked rather than passed - a
submission that claims a device install it never performed is worse than one that says it could not.

## 2. The three gates

Every number below was produced by a harness in `Assets/Scripts/Editor/` and written to a log in
`Evidence/`. None of them was typed in by hand.

| Gate | Result | Target | Where |
|---|---|---|---|
| Week 17 release checks (`TC-REL-01…05`) | **5 passed, 0 failed, 0 console errors** | 5 / 5 | `Evidence/test_logs_release/summary.txt` |
| Week 15 regression re-run at `Week_17` | **27 passed, 0 failed, 1 blocked, 0 console errors** | 27 / 0 / 1 | `Evidence/test_logs_regression/summary.txt` |
| Accessibility audit (post-fix) | **148 passed, 0 failed, 0 warnings, 4 not measurable** | 0 failed, more passes than Week 16's 105 | `Evidence/a11y_week17_after.log` |

The single blocked case is `TC-COMPAT-01`, device compatibility, blocked for the same reason the install is:
no handset. It has been blocked since Week 15 and this week does not change that by asserting it.

## 3. What the release looks like

Six screens, captured from the running game by `Week17Captures.cs`; each image is hashed and the run fails if
two of them are the same picture.

| Image | Screen | What a reviewer should be able to check |
|---|---|---|
| `E1_title.png` | title | `1.0.0-rc1 · Editor` top-left; HOW TO PLAY, START, CREDITS on one row; PROFILE and EXIT on the next |
| `E2_how_to_play.png` | HOW TO PLAY | the rules, the capture rule, the Dama, the win condition, the pause menu, the profile - and BACK |
| `E3_credits.png` | CREDITS | author and course, engine and pipeline versions, licence, AI-assistance statement, build line |
| `E4_mid_match.png` | match in progress | board after a real capture; HUD turn and both counts agree with the position |
| `E5_pause.png` | pause | RESUME, RESTART, SOUND, MENU |
| `E6_end_screen.png` | end screen | winner, both counts, the reason the match ended, RESTART and MENU |

`Evidence/week17_shots.txt` is the harness's own transcript of that run: which state each image was taken in,
the size and SHA-256 of each file, and the lines proving the six are byte-distinct.

## 4. What changed in the game, in one page

| File | Change | Why |
|---|---|---|
| `Assets/Scripts/Core/ReleaseInfo.cs` | new: `Version`, `BundleId`, `Engine`, `Platform`, `Caption()`, `BuildLine()` | one place that reads the build identity from `Application.*`, so no screen can describe a different build |
| `Assets/Scripts/DamaBoard.cs` | `GameState` gains `HowTo` and `Credits`; three new `WireButton` calls; two `ShowState` lines; `ShowHowTo`/`CloseHowTo`/`ShowCredits`/`CloseCredits`/`QuitGame` | the two new panels are state-driven like every other panel, and EXIT is a real control |
| `Assets/Scripts/Editor/Week17ReleasePanels.cs` | new idempotent builder | builds the title controls and both panels in code and saves the scene; re-runnable |
| `Assets/Scripts/Editor/Week17Checks.cs` | new: `TC-REL-01…05` | proves the caption, the build line, both panels, the five listeners, and that the panels cannot leak into a match |
| `Assets/Scripts/Editor/Week17Captures.cs` | new | E1-E6 plus the hash check |
| `Assets/Scripts/Editor/A11yAudit.cs` | `RunWeek17()` seam | the Week 16 log is never overwritten by a Week 17 run |
| `ProjectSettings/ProjectSettings.asset` | `bundleVersion`, `AndroidBundleVersionCode` | the release identity |

The version is **not** in `productName`. On Android, `productName` decides the save folder under
`Application.persistentDataPath`; moving it would move the profile store and look exactly like a wipe to the
player. That is why the identity lives in `bundleVersion`, which is what `Application.version` reads, and why
`TC-REL-01` compares the on-screen caption to `Application.version` instead of to a string in the scene.

## 5. What the week found

Two defects surfaced while running the gates, and both are in `Evidence/issue_disposition.md` with their
numbers:

1. **DEF-17-05** - the release-panel builder destroyed `VersionLabel` and the two panels before rebuilding
   them, but not the three new title buttons. Its second run therefore left a second HOW TO PLAY, CREDITS and
   EXIT on top of the first. `TC-A11Y-02` reported three controls overlapping themselves; the regression read
   **26 passed, 1 failed, 1 blocked** until the builder was made to clean up after itself. After the fix the
   panel has eight children, one of each control, and the row reads **27 / 0 / 1**.
2. **DEF-17-06** - the release check `TC-REL-04` read `null` where a listener should have been, because it
   looked at `InvokableCall.Target` instead of the delegate uGUI actually stores. The wiring was correct; the
   check was not. It now names all five handlers.

Both are recorded because a log that only shows the final green run cannot tell a reader whether the work was
clean or the failures were hidden.

## 6. What this release does not claim

- **No APK and no handset.** `adb` reports no attached device and no build was produced this week, so the
  module's "install and run from a clean state" is **BLOCKED**, and `TC-COMPAT-01` stays blocked with it.
- **No device performance, battery or thermal claim.** Every measurement in this package came from the Editor.
- **No participant usability study.** Week 14 recorded that no participant was available; Week 17 does not
  invent one.
- **The four not-measurable accessibility rows are unmeasured, not passed**: `HUDPanel/TurnText` and
  `HUDPanel/CountsText` are drawn over the board with no opaque backdrop, `HUDPanel/PauseButton` has no panel
  behind it, and `ProfilePanel/NameInput` has no opaque fill of its own.

## 7. The rest of the package

| Document | What it covers |
|---|---|
| `Guzman_Charibelle_PT7_Technical_Documentation.md` | the identity mechanism, the state machine change, the builder, the defects, the gates and their limits |
| `Guzman_Charibelle_PT7_User_Guide.md` | how a player moves through the six screens, where the save file lives, what to do when it is damaged |
| `Guzman_Charibelle_PT7_Evidence_Index.md` | every claim in this document mapped to the file and case id that proves it |
| `Guzman_Charibelle_Week17_Reflection.md` | the week's self-assessment |

## 8. Authorship and AI use

The game, its harnesses and these documents were produced by Charibelle Guzman for MOBILE DEV 002, Module 5,
with AI assistance used as a tool for drafting and code review. Every number in the package comes from a run
recorded in `Evidence/`, every screenshot from a harness run, and every limitation is stated as one. Where the
work could not be done - a device install, a participant study, a device performance profile - the package says
so instead of filling the gap.
