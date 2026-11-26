# Week 17 — Feature-freeze checklist

**Game:** Dama, a two-player draughts game on an 8x8 board, Philippine Games and Sports theme
**Student:** Charibelle Guzman · BSIT-4 · MOBILE DEV 002, Module 5, Week 17
**Engine:** Unity 6000.6.0f1, URP 17.6.0 (2D Renderer), Input System 1.20.0
**Scene:** `Assets/Scenes/DamaGame.unity` (build index 0)
**Frozen:** commit **`337b436`**, annotated tag **`week16-final`**, frozen 2026-11-26
**Written:** before any Week 17 change was made, so that every later change can be checked against
this file rather than against memory.

---

## 1. What is frozen

From `week16-final` onward the game's behaviour, rules, level content and art do not change. The
tree at that tag is the last one that was measured in Week 16: 27 passed / 0 failed / 1 blocked on
the Week 15 regression suite, 105 passed / 0 failed / 4 not measurable on the accessibility audit,
and both of the Week 16 profiling targets met on the editor.

No new rule, no new board, no new art direction, no new AI, no new sound is added after this
commit. If a reviewer diffs `week16-final` against the Week 17 tree, every change must fall into
one of the three categories below, and each one is listed with the evidence it was accepted on.

## 2. What may still change, and only this

| # | Category | What it covers | What it does **not** cover |
|---|---|---|---|
| 1 | **Completeness gaps the module itself requires** | a screen or control the Week 17 completion checklist names and that the game does not have yet | anything the checklist does not name, however nice it would be |
| 2 | **Defect corrections** | a change that keeps an already-accepted behaviour working, or makes the end screen, pause menu and profile tell the truth | a behaviour being redesigned because it is not liked |
| 3 | **Release mechanics** | version identifier, build configuration, documentation, evidence | gameplay tuning of any kind |

## 3. The accepted change sets of Weeks 13–16

Each row names the script or asset the work lives in, and the evidence it was accepted on. These
are the changes the release candidate is *allowed* to contain; nothing else from Weeks 13–16 is
still moving.

| Week | Change set | Where it lives | Accepted on |
|---|---|---|---|
| 13 | Signed local player profile: UTF-8 JSON envelope with an `int schema`, a `string sig` and a `payload`; HMAC-SHA256 over the canonical payload; per-install 16-byte salt in `PlayerPrefs["profile.salt"]`; a rejected file is quarantined to `profile.rejected.json`, never overwritten; numbers clamped even when the signature passes | `Assets/Scripts/Profile/PlayerProfile.cs`, `Assets/Scripts/Profile/ProfilePanel.cs`, `Assets/Scripts/Editor/ProfileSceneBuilder.cs` | `Week_13/Evidence/pt5_checks.log` — 14 passed, 0 failed, L1–L14 including a hand-edited save rejected and an impossibly-large but correctly signed save clamped to `wins=0 losses=0` |
| 14 | Accessibility repair pass, idempotent by layout fingerprint | `Assets/Scripts/Editor/AccessibilityBuild.cs` (fingerprint `4C3E2E0C`, a re-run is a no-op) | `Week_14/Evidence/` — audit before/after, fingerprint unchanged on the second run |
| 14 | The measurement layer: contrast, touch-target, type-size and (from Week 15) non-text-contrast checks | `Assets/Scripts/Editor/A11yAudit.cs` | `Week_14/Evidence/`, `Week_15/Evidence/a11y_delta_week15.txt` |
| 14 | Five feedback clips, one haptic, one persisted mute; the component listens and never decides | `Assets/Scripts/Core/GameFeedback.cs`, `DamaGame.unity` | `Week_14/Evidence/feedback_lines.txt` — each clip fired from the rule that owns it |
| 15 | Four defect fixes: DEF-15-01 route back to title (`PausePanel/MenuButton`, `WinPanel/MenuButtonWin`, `DamaBoard.ReturnToMenu`), DEF-15-02 type sizes, DEF-15-03 end-screen score and reason (`WinPanel/SummaryText`), DEF-15-04 non-text contrast | `Assets/Scripts/Editor/Week15Fixes.cs`, `Assets/Scripts/DamaBoard.cs` | `Week_15/Evidence/defect_log.md`, `traceability_matrix.md` — 27 passed, 0 failed, 1 blocked |
| 15 | The regression suite itself, 28 cases, with a `runWeek` seam so a later week can re-run it without writing into Week 15's folder | `Assets/Scripts/Editor/Week15Checks.cs` | `Week_15/Evidence/test_logs/`, re-run in Week 16 as `test_logs_regression/` |
| 16 | Allocation-free move generation: `captureBuffer` / `simpleBuffer` reused, `noMoves` static | `Assets/Scripts/DamaBoard.cs` | `Week_16/Evidence/profile_after_alloc/` — fixture A −6.8 B, fixture B −0.1 B, target PT-2 met |
| 16 | Piece pooling and cached square binding: `piecePool`, `pieceNames[,]`, inactive `PiecePool` root, `RentPiece`/`ReleasePiece` | `Assets/Scripts/DamaBoard.cs` | `Week_16/Evidence/profile_after_pool/` — fixture C −1,665.5 B at 2σ, restart worst frame 5.154 ms |
| 16 | The profiling harness and three scenario presets | `Assets/Scripts/Editor/Week16Profile.cs` | `Week_16/Evidence/{targets,method,metrics,bottlenecks}.md` |

The rule invariant that all of the above had to preserve is unchanged and is still asserted by the
suite: men capture forward only (RED `+y`, BLUE `−y`), RED promotes at `y == 7` and BLUE at `y == 0`,
kings fly any diagonal and stop before the first piece, and any available capture is mandatory
board-wide. `DamaBoard.DeclareWinner` still takes exactly one parameter, and `PlayerProfile` is still
the only class that reads, signs, writes, clamps or deletes the save file.

## 4. Checked at the freeze

| # | Check | Result |
|---|---|---|
| 1 | The game boots and a match runs start to finish in `DamaGame.unity` | pass — Week 16's harness replayed 32 turns and 3 restarts with hash `0xE432E83D` |
| 2 | No half-finished mechanic is switched on | pass — the board is the Week 16 one; no debug flag is enabled in the scene |
| 3 | Android is the active target with ARM64, IL2CPP, min API 26, Build App Bundle off | pass — `ProjectSettings` + `Tools/Android/Apply Mobile Build Settings` |
| 4 | The profile save is signed and the store path is bound to `productName`/`companyName` | pass — `DefaultCompany` / `dama`; see §5 |
| 5 | Every Week 13–16 evidence folder is present and is not to be regenerated | pass — `docs/Charibelle Guzman/Week_13…Week_16/` |
| 6 | The repository holds no committed secret | see `secret_scan.md` |

## 5. What the freeze does **not** freeze, deliberately

- **`productName` and `companyName`.** They are `dama` and `DefaultCompany`, and the save file lives
  under `.../LocalLow/DefaultCompany/dama/Dama/profile.json`. Unity derives both the
  `Application.persistentDataPath` and the `PlayerPrefs` namespace from them, so moving either one
  would point the release candidate at an empty store and silently erase a player's wins, name and
  best board. The version identifier therefore lives in `bundleVersion` only, which is what
  `Application.version` reads.
- **The four completeness gaps.** They are opened by the module's own Week 17 checklist, not by a
  change of plan, and they are the only additions this week makes. Each is recorded in
  `issue_disposition.md` with the requirement it answers.

## 6. The gaps the freeze opens (category 1, and nothing else)

| Gap | Module requirement it answers | Planned resolution |
|---|---|---|
| No credits anywhere in the scene or the code | "settings, credits, and exit or restart" | `GameCanvas/CreditsPanel`, built by `Assets/Scripts/Editor/Week17ReleasePanels.cs` |
| No exit control; `Application.Quit` exists nowhere in the game | "exit or restart behaviour" | `StartPanel/ExitButton` → `DamaBoard.QuitGame()` |
| No version shown in the running game | "a unique version number and build identifier" | `StartPanel/VersionLabel` and the credits build line, both filled from `ReleaseInfo` at runtime |
| The title states the rules in 156 characters and offers no fuller instruction surface | "clear instructions or tutorial" | `GameCanvas/HowToPanel`, reachable from `StartPanel/HowToButton` |

## 7. What this checklist does not claim

The freeze is a statement about the source tree, not about a device. No APK was built in Weeks
13–16 and none is built this week (`clean_install.md` records why), so nothing here proves device
performance, thermals, battery behaviour or Play Store readiness. Every number cited above was
measured in the Unity editor on the machine named in `Week_16/Evidence/method.md`.
