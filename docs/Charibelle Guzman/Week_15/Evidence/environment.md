# Test environment - Dama (Week 15, AS3 Milestone)

**Student:** Charibelle Guzman (BSIT-4)
**Recorded:** 2026-11-26, before the final run was read back.

This file exists because three things elsewhere point at it: `TC-COMPAT-01` says "see
Evidence/environment.md", `acceptance_criteria.md` CC-3 checks the no-network claim here, and a
Reader needs to know which configuration any green table was green **on**. Everything below is a fact
read off the machine that ran the tests, not the machine the game is aimed at.

## 1. Where the tests ran

| | |
|---|---|
| Machine | Zedrich, x64-based PC, American Megatrends BIOS 2409 (2020-12-02) |
| Operating system | Microsoft Windows 10 Home, version 10.0.19045, build 19045 |
| Physical memory | 16,277 MB |
| Editor | Unity **6000.6.0f1**, Windows Editor platform |
| Render pipeline | Universal RP **17.6.0**, 2D Renderer |
| Input | `com.unity.inputsystem` **1.20.0**, the new Input System only; no legacy `Input` calls remain in the game scripts |
| UI | `com.unity.ugui` **2.6.0** (uGUI, `CanvasScaler` + `Canvas`), not UI Toolkit |
| Tests | `com.unity.test-framework` **1.8.0** is installed but **not used**: the harnesses are Editor menu items that drive the real game in play mode, the same pattern as Week 13 and Week 14 |

**This is a desktop configuration.** Every "the game works" statement in this week's evidence is a
statement about Windows 10 x64 in the Editor. It is not a statement about a phone, and the sections
below say exactly which phone claims could not be made.

## 2. The scene under test

| | |
|---|---|
| Scene | `Assets/Scenes/DamaGame.unity`, build index 0, the only game scene |
| Game view at measurement | **1151 x 584** |
| Canvas | `GameCanvas`, Screen Space - Overlay |
| Canvas Scaler | Scale With Screen Size, **reference 1280 x 720**, **match 0.50** |
| Canvas rect at that game view | 1347.73 x 683.82 canvas px |
| Camera | Orthographic, the board is 8 x 8 world units |
| Save file | `C:/Users/Zedrich/AppData/LocalLow/DefaultCompany/dama/Dama/profile.json` |
| Sound preference | `PlayerPrefs["audio.feedback"]` |

The 1151 x 584 game view is not a phone aspect. It is the Editor window this project happened to have,
and the canvas scaler turns any window into the same 1280 x 720 design space, which is why the audit can
report canvas px at all. What it cannot do is tell anyone how the result looks on glass, at arm's length,
under a thumb.

## 3. Settings that changed how the tests had to be written

- **Domain reload on entering play mode is disabled.** Static state survives a play-mode session, so the harnesses load and reset the profile explicitly (`PlayerProfile.UseDefaultDataDirectory()` then `ResetProfile()`) instead of assuming a clean domain.
- **`Time.timeScale` is set to 0 by the pause menu and by the end screen.** The harness ticks on `EditorApplication.timeSinceStartup`, so a frozen game does not freeze the test run - and each case restores the clock before the next one starts.
- **The harnesses write to the real local profile.** `test_logs_*/summary.txt` records that the local `profile.json` was reset during the run. A player's banked matches from an earlier session are gone after a harness run. That is a side effect of testing, disclosed here and in the Week 14 evidence rather than discovered later.
- **Nothing is built as an APK this week.** The locked scope is Editor-only evidence, so no player build exists to install, and no build-configuration claim is made.

## 4. Compatibility: what was attempted and what came back

The module asks for a second approved configuration "when resources permit". This lab tried and could
not, and the attempt is recorded rather than skipped.

```
> adb devices
'adb' is not recognized as an internal or external command,
operable program or batch file.

> "%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe" devices
List of devices attached
```

The Android SDK is installed on this machine, so the device list can be asked for by full path; what it
returns is the standard empty list - a header and no rows, i.e. **no attached handset**. Bare `adb`
fails because `platform-tools` is not on the system `PATH`. Both facts are quoted because they are
different facts: the SDK exists, and no device is plugged in.

Consequently:

| Intended configuration | State | Reason |
|---|---|---|
| Windows Editor, this machine | **Tested** | Every case in `test_logs_final/` and every row in `a11y_week15_after.log` |
| Android handset (any) | **BLOCKED** | No APK built this week and `adb devices` lists no device - the command and its output are above |
| Second Editor configuration (different resolution or aspect) | **BLOCKED** | It was considered and rejected as evidence: a resized Game view scales the same canvas through the same scaler, so it would re-measure the same thing and prove the wrong claim. Doing it properly needs a real second device |

`TC-COMPAT-01` is therefore **BLOCKED**, never PASS. A blocked case with its attempted command and its
output is worth more than a pass nobody can reproduce.

## 5. Network and other-player data (CC-3)

`CC-3` asks that no defect expose another player's information. The answer here is structural rather
than tested:

- A search of `Assets/Scripts/` for `UnityWebRequest`, `System.Net`, `HttpClient`, `Socket`, `WebRequest` and `Application.OpenURL` returns **no matches**. There is no networking code in the game.
- There are no accounts, no sign-in and no server. The only persisted data is one local file per install, at the path above, and `TC-DATA-05` asserts it is inside the app sandbox and that the quarantined copy of a rejected save lands there too.
- The game is two-player **on one device**. The second player's moves are not transmitted anywhere and there is nothing to breach.

That is why `REQ-22` is written as "no network path, so interrupted network access is not applicable"
rather than as an untested case: the test type is N/A because the code path does not exist.

## 6. What this environment cannot prove

- **Anything about a handset**: frame rate, thermals, real touch latency, real dp sizing, or how 22 px type looks on a 6-inch screen at arm's length.
- **Anything about a second configuration**: see section 4.
- **Anything requiring a person**: the five Week 14 playtest sessions were never run, so no first-time-player observation exists for this or any other week. The audit measures thresholds; it cannot measure comprehension.
- **Anything about a screen reader**: uGUI exposes no accessibility tree, so `com.unity.modules.accessibility` being present in the project does not give a non-visual player a route through this game.
