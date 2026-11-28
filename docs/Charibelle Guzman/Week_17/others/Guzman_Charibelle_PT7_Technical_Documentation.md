# PT7 - Technical Documentation

**Game:** Dama
**Student:** Charibelle Guzman · BSIT-4
**Course:** MOBILE DEV 002 Advanced Mobile Application Development, Module 5
**Week:** 17 - Release Preparation and Documentation
**Release identity:** `1.0.0-rc1`, version code `2`, `com.defaultcompany.dama`
**Engine:** Unity 6000.6.0f1 · URP 17.6.0 (2D Renderer) · Input System 1.20.0 · uGUI
**Scene:** `Assets/Scenes/DamaGame.unity`, build index 0
**Tree:** `week16-final` (`4cce8c0`) plus the Week 17 change set
**Produced:** 2026-11-26

---

## 1. The problem this week actually has to solve

A build that cannot name itself is not releasable. Two copies of Dama - one from Week 16, one from this week -
look identical on screen and write to the same profile, so a reviewer holding both cannot tell which is which,
and a bug report cannot say which build produced it. The same applies to a build with no credits, no way out,
and no instructions beyond three lines on the title.

Week 17 therefore has four concrete jobs, and each one has a failure mode worth designing against:

| Job | The failure mode it invites |
|---|---|
| State the version | typing `1.0.0` into a `Text` component, so the label drifts away from the build it describes |
| Add credits | a screen that lists the engine version by hand, contradicting `Application.unityVersion` a month later |
| Add an exit control | a button built into the scene with no listener, which a screenshot cannot distinguish from a working one |
| Add instructions | a panel that a player can open but that also stays visible during a match |

Every design decision below is a response to one of those four rows.

## 2. Version identity: derived, never typed

`Assets/Scripts/Core/ReleaseInfo.cs` is a static class with no state and no scene reference:

```csharp
public static string Version  => Application.version;      // "1.0.0-rc1"
public static string BundleId => Application.identifier;   // "com.defaultcompany.dama"
public static string Engine   => Application.unityVersion;
public static string Platform { get; }                     // "Editor" / "Android" / ...
public static string Caption()   => $"{Version} · {Platform}";
public static string BuildLine() => $"Build {Version} · {Platform} · {BundleId}";
```

`DamaBoard.RefreshReleaseLabels()` writes `ReleaseInfo.Caption()` into `StartPanel/VersionLabel` and
`ReleaseInfo.BuildLine()` into `CreditsPanel/BuildText` at `Awake`. The builder writes the same strings once so
the scene is not empty in edit mode, and the runtime overwrites them - which means no scene value can survive
into a build that disagrees with `Application.version`.

`TC-REL-01` is the check that keeps this honest: it compares the on-screen caption to `Application.version`,
not to a literal. A regression that re-introduced a typed version would fail it.

**Why the version is not in `productName`.** On Android the save store lives under
`Application.persistentDataPath`, which is derived from `companyName` and `productName`:

```
C:/Users/Zedrich/AppData/LocalLow/DefaultCompany/dama/Dama/profile.json
```

Putting `1.0.0-rc1` into `productName` would move that path, and every player would open the new build to an
empty profile - a silent wipe caused by a naming decision. The identity therefore goes into `bundleVersion`
only, and `productName` / `companyName` are asserted unchanged in the release record.

## 3. Screen structure

The game has one canvas and one state enum; panels are shown and hidden in exactly one place.

```
GameCanvas
├── StartPanel            Ready      title, instructions, 5 controls, version caption
├── HowToPanel            HowTo      1,122-character instruction body, BACK      [new]
├── CreditsPanel          Credits    credits body, build line, BACK              [new]
├── ProfilePanel          Profile    name, side, record, RESET/SAVE/BACK
├── PausePanel            Paused     RESUME, RESTART, SOUND, MENU
├── WinPanel              GameOver   win text, summary, RESTART, MENU
└── HUDPanel              Playing    turn, counts, PAUSE
```

`ShowState(GameState)` is still the only function that toggles panels; the two new lines are

```csharp
howToPanel.SetActive(state == GameState.HowTo);
creditsPanel.SetActive(state == GameState.Credits);
```

That is what `TC-REL-05` verifies from the other end: it opens the title, asserts both new panels are inactive,
starts a match, and asserts they are still inactive. A panel that exists but is never turned off cannot pass
it, and a panel whose `ShowState` line was forgotten would be found by it.

### 3.1 Title layout, in canvas units

The canvas uses `CanvasScaler` `Scale With Screen Size`, reference **1280x720**, match **0.50**; the game view
during capture is 1151x584. All controls are anchored at `(0.5, 0.3)` for the button block:

| Control | Anchored position | Size | Note |
|---|---|---|---|
| `HowToButton` | (-440, 0) | 320x100 | new |
| `StartButton` | (0, 0) | 320x100 | unchanged |
| `CreditsButton` | (440, 0) | 320x100 | new |
| `ProfileButton` | (-440, -115) | 320x96 | moved from (0, -110); size untouched |
| `ExitButton` | (0, -115) | 320x100 | new |
| `VersionLabel` | (20, -14) against the top-left corner | 560x34, 22 px | new |

Two rows rather than three: five 100 px controls do not fit in the vertical band the title text leaves
(the title occupies the top 26% of the canvas and the rule text another 22%), and a 320 px control at this
reference resolution is already 96 px taller than the audit's touch floor.

### 3.2 The two new panels

| Panel | Title | Body | Body fit | Back |
|---|---|---|---|---|
| `HowToPanel` | 760x56 at `(0.5, 0.905)`, 44 px bold | 1220x440 at `(0.5, 0.52)`, 1,122 characters | **19 px**, measured **399.3 px of 440 px** | 320x100 at `(0.5, 0.085)` |
| `CreditsPanel` | 600x56 at `(0.5, 0.905)`, 44 px bold | 1120x380 at `(0.5, 0.55)`, 903 characters | **19 px**, measured **357.1 px of 380 px** | 320x100 at `(0.5, 0.085)` |
| | | `BuildText` 1120x36 at `(0.5, 0.235)`, 22 px bold | | |

The body size is not guessed. `FitText()` measures the paragraph with `TextGenerator.GetPreferredHeight` at
the panel's real width and steps the size down from 26 (HOW TO PLAY) or 22 (CREDITS) until the measured height
fits, logging the result either way:

```
[Week17ReleasePanels] FitText 'BodyText': 19px needs 399.3 px of 440.0 px (width 1220.0).
[Week17ReleasePanels] FitText 'BodyText': 19px needs 357.1 px of 380.0 px (width 1120.0).
```

The first attempt at the credits panel logged the warning path instead - `even 18px needs 358.3 px of 330.0 px`
- which is the fit check refusing to pretend. The rect was enlarged and the body shortened, and the run after
that fitted at 19 px. 19 px is also the audit's floor for a non-caption label (18 px), so the paragraph is
simultaneously as large as it can be and above the floor.

## 4. The builder, and why it has to be idempotent

`Assets/Scripts/Editor/Week17ReleasePanels.cs` (menu `Tools/Dama/Week 17/1. Build Release Panels`) follows the
`ProfileSceneBuilder` pattern: find `GameCanvas/StartPanel`, build children in code, log, save the scene. There
are no prefabs, so a rebuild is the only way to change the layout.

A builder that appends instead of replacing is a defect generator, and that is exactly what happened:

> **DEF-17-05.** `DestroyIfExists` was called for `VersionLabel`, `HowToPanel` and `CreditsPanel`, but not for
> `HowToButton`, `CreditsButton` or `ExitButton`. Running the builder a second time left **11 children** under
> `StartPanel` - the three controls existed twice, at indices 4-6 and 8-10, exactly coincident. `TC-A11Y-02`
> reported three controls overlapping *themselves*, and the Week 15 regression read **26 passed, 1 failed,
> 1 blocked** rather than 27/0/1.

The fix has two parts, and the first is the one that matters:

```csharp
private static void DestroyIfExists(Transform parent, string childName)
{
    int removed = 0;
    Transform existing = parent.Find(childName);

    while (existing != null)                    // was: a single Find + Destroy
    {
        Object.DestroyImmediate(existing.gameObject);
        removed++;
        existing = parent.Find(childName);
    }

    if (removed > 0)
        Debug.Log($"[Week17ReleasePanels] Removed {removed} previous {childName} object(s) before rebuilding.");
}
```

...plus the three missing calls before the buttons are created. The loop matters because a build that is
already wrong must be repairable by re-running the tool, not require a manual scene edit. `StartPanel` now has
**8** children, one of each control, and the builder logs what it removed so the next reader can see the
cleanup happened.

## 5. Verification

### 5.1 Week 17 release checks (`Week17Checks.cs`)

| Case | What it asserts | Reading |
|---|---|---|
| `TC-REL-01` | the title caption equals `Application.version` | `caption='1.0.0-rc1 · Editor' expected='1.0.0-rc1 · Editor' fontSize=22` |
| `TC-REL-02` | the credits build line agrees with the title and names the application id | `title='1.0.0-rc1 · Editor' credits='Build 1.0.0-rc1 · Editor · com.defaultcompany.dama' agree=True` |
| `TC-REL-03` | HOW TO PLAY opens with a real body and closes back to the title | `bodyChars=1122 bodyFont=19 opened=True closed=True` |
| `TC-REL-04` | all five new controls are bound to the method they claim | `HOWTO[DamaBoard.ShowHowTo] CREDITS[ShowCredits] EXIT[QuitGame] HOWTO-BACK[CloseHowTo] CREDITS-BACK[CloseCredits]` |
| `TC-REL-05` | the new panels cannot leak into a match | `atTitle: HowToPanel=False CreditsPanel=False` → `after StartGame: HowTo=False Credits=False HUD=True` |

`TC-REL-04` exists because a button without a listener is invisible in a screenshot. Its first implementation
reported `1 call(s): null.?` for every control: it read `InvokableCall.Target` and `.Method`, which a
serialized uGUI listener does not populate - the handler lives in the private `m_Delegate` field. Reading the
delegate's `Target` and `Method.Name` gives the five names above. **The wiring was correct; the check was
wrong**, and that is recorded as `DEF-17-06` rather than deleted.

### 5.2 Regression at the frozen tree (`Week15Checks.cs`, `runWeek = "Week_17"`)

All 27 cases from Weeks 15-16 pass on the frozen tree: ten rule cases, eleven flow cases, five data cases
(including corrupt-save quarantine and recovery), two boundary cases (100 rapid taps, 50 pause cycles) and the
accessibility cases. `TC-A11Y-01` now measures **18 buttons** and 1 input field with 0 below the 96 px floor;
`TC-A11Y-02` reports **0 overlapping pairs**. `TC-COMPAT-01` remains **BLOCKED** on hardware.

The regression is the reason the duplicate-control defect was found at all: it is the only gate that looks at
the whole scene rather than at the week's new feature.

### 5.3 Accessibility audit (`A11yAudit.RunWeek17()`)

**148 passed, 0 failed, 0 warnings, 4 not measurable** over 18 buttons and 1 input field. The count is lower
than the 163 an earlier pre-fix audit printed, and the difference is explained exactly: the three duplicated
controls contributed 15 checks of their own (touch, fill, label), so removing them removed those checks. 148 is
still 43 more than Week 16's 105, which is what the two new panels add.

Four rows stay **not measurable**, and they are named rather than rounded to a pass:

| Row | Reason |
|---|---|
| `HUDPanel/TurnText`, `HUDPanel/CountsText` | white text drawn over the 3D board, no opaque backdrop |
| `HUDPanel/PauseButton` | no opaque panel behind the control |
| `ProfilePanel/NameInput` | the input has no opaque fill of its own |

### 5.4 Captures (`Week17Captures.cs`)

Six images, each hashed on landing; the run fails if any two share a digest. That check earned its place on the
first run: the editor window was not the foreground application, the Game view stopped drawing after `E3`, and
`E4`, `E5` and `E6` came back **byte-identical to `E3`**. The harness now calls
`InternalEditorUtility.RepaintAllViews()` and `EditorApplication.QueuePlayerLoopUpdate()` on every tick while a
shot is pending, and the transcript records the hash of each file:

```
E1_title.png           69c4a1306197b0930915333e9404c498
E2_how_to_play.png     984fabeaccbf8100b0508d913430f789
E3_credits.png         6342537d78e673f088c1f6e2b50fedce
E4_mid_match.png       a86927876e60267ec657082d8de1975d
E5_pause.png           b3e7837baf691f39e06485fa6905b8eb
E6_end_screen.png      f40dcf8b79d0454e427663af9bf6b590
```

The in-match images are seeded by reflection, so the harness rebuilds the visible pieces from the grid through
the board's own pooled `RentPiece`/`RegisterPiece`/`ReleasePiece` before capturing. Without that step the HUD
would read `Red: 1 Blue: 2` over a picture of 24 pieces, which is worse evidence than no picture.

## 6. Environment and its limits

| Item | Value |
|---|---|
| Machine | AMD Ryzen 5 5600G, 16,276 MB, NVIDIA GeForce GTX 1070 |
| Editor | Unity 6000.6.0f1, Windows Editor target |
| Pipeline | URP 17.6.0, 2D Renderer, quality level 5 (Ultra) |
| Game view | 1151x584, canvas reference 1280x720, match 0.50 |
| Domain reload on play-mode entry | disabled (statics survive, needed by the capture harness) |
| Save file | `C:/Users/Zedrich/AppData/LocalLow/DefaultCompany/dama/Dama/profile.json` |

- **No APK, no handset.** `adb devices` has no serial and no build was produced this week. The install step is
  **BLOCKED**; every number in this package is an Editor number.
- **Touch and contrast are canvas-pixel measurements** against the reference resolution, not ruler readings on
  a phone. The derivation is written in the header of `a11y_week17_after.log`.
- **An audit is a floor**: it cannot show a player understood the game, and it cannot show that six distinct
  PNGs prove six distinct screens - a human has to look, which is what section 3 of the front page is for.

## 7. Files touched

| File | Status |
|---|---|
| `Assets/Scripts/Core/ReleaseInfo.cs` | new |
| `Assets/Scripts/Editor/Week17ReleasePanels.cs` | new (and fixed: DEF-17-05) |
| `Assets/Scripts/Editor/Week17Checks.cs` | new (and fixed: DEF-17-06) |
| `Assets/Scripts/Editor/Week17Captures.cs` | new |
| `Assets/Scripts/DamaBoard.cs` | edited: state, panels, wiring, release labels, `QuitGame` |
| `Assets/Scripts/Editor/A11yAudit.cs` | edited: `RunWeek17()` seam |
| `Assets/Scripts/Editor/Week15Checks.cs` | edited: `ReleaseMenu` → `BeginRelease()` at `Week_17` |
| `Assets/Scripts/Editor/SubmissionDocBuilder.cs` | edited: `Week_17` entry + menu order 14 |
| `ProjectSettings/ProjectSettings.asset` | edited: `bundleVersion` `1.0.0-rc1`, version code `2` |
| `Assets/Scenes/DamaGame.unity` | edited: three controls, version caption, two panels |
