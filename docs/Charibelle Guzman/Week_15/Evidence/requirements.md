# Testable requirements - Dama (AS3 Milestone, Week 15)

**Game:** Dama, a two-player draughts game on an 8x8 board, Philippine Games and Sports theme
**Engine:** Unity 6000.6.0f1, URP 17.6.0 (2D Renderer)
**Scene:** `Assets/Scenes/DamaGame.unity` (build index 0)
**Written:** before any test in this week was executed.

Every requirement below is stated so that a test can pass or fail it. "Implemented by" names the
script that holds the behaviour, so `traceability_matrix.md` can link a requirement to both a test
case **and** a place in the code. Where a requirement has no code behind it yet, that is stated -
a requirement that fails at baseline is more useful than one quietly written to match what the game
already does.

| REQ | Requirement (testable form) | Implemented by |
|---|---|---|
| REQ-01 | From the launched scene the title screen is shown, with START and PROFILE, and the three rules of play are readable | `DamaBoard.ShowState`, `DamaBoard.Start` (`DamaBoard.cs:172`, `:81`) |
| REQ-02 | START hides the title screen, spawns all 24 pieces, sets the turn to RED and shows both counts on the HUD | `DamaBoard.StartGame` (`:195`), `SpawnStartingPieces` (`:437`), `RefreshUI` (`:184`) |
| REQ-03 | Tapping a piece selects it and tapping a highlighted destination moves it, then the turn passes | `DamaBoard.HandleTap` (`:557`), `ExecuteMove` (`:598`), `EndTurn` (`:663`) |
| REQ-04 | When a capture exists for the side to move, no non-capture destination is highlighted or accepted | `GetSelectableMoves`, `PlayerHasCapture`, `ExecuteMove` |
| REQ-05 | A piece that lands on another jump keeps the turn and can continue the chain until no jump remains | `chainPiece`, `RehighlightChain` (`:673`), `EndTurn` |
| REQ-06 | A piece reaching its far row is crowned a Dama: RED at `y == 7`, BLUE at `y == 0`, and a Dama may move any distance along a diagonal in any direction | `DamaBoard.PromoteToDama` (`:470`), `GetSimpleMoves` |
| REQ-07 | A side with no pieces left loses, and the winner is declared exactly once | `CheckForWin` (`:312`), `CountPieces` (`:375`), `DeclareWinner` (`:354`) |
| REQ-08 | A side that still holds pieces but has no legal move loses | `HasAnyLegalMove` (`:387`), `CheckForWin` |
| REQ-09 | The end screen states the winner, both final counts and the reason the match ended, and offers RESTART and MENU | `DamaBoard.DeclareWinner` (`:354`) - **not met at baseline, defect DEF-15-03** |
| REQ-10 | PAUSE freezes board input and offers RESUME, RESTART, SOUND and MENU; RESUME returns to the same position and the same side to move | `PauseGame` (`:205`), `ResumeGame` (`:213`), `ShowState` |
| REQ-11 | SOUND silences all feedback and spells its own state in words; the choice survives pause, restart and relaunch on the same device | `GameFeedback.ToggleSound`, `DamaBoard.RefreshSoundLabel` (`:249`), `PlayerPrefs["audio.feedback"]` |
| REQ-12 | Move, capture, crown, win and lose each play their own clip, driven by the rules rather than by a button | `Assets/Scripts/Core/GameFeedback.cs`, `[Feedback]` log lines |
| REQ-13 | A profile name and side entered on the profile screen survive an app restart | `PlayerProfile.SetName`, `SetSide`, `Assets/Scripts/Profile/ProfilePanel.cs` |
| REQ-14 | Finished matches are banked once each: no repeat end-of-match call inflates the record | `DamaBoard.BankMatch` (`:330`), `PlayerProfile.RecordMatch` |
| REQ-15 | Profile reset clears name, record and best-times board and leaves a usable profile | `PlayerProfile.ResetProfile` (`:280`), `ProfilePanel` two-press reset |
| REQ-16 | A corrupt or tampered save is rejected with a message and the game still starts | `PlayerProfile.Load` (`:113`), `RejectedPath` |
| REQ-17 | From inside a match the player can return to the title screen without closing the app | `DamaBoard.ReturnToMenu` - **not met at baseline, defect DEF-15-01** |
| REQ-18 | Every label meets WCAG 2.2 contrast (4.5:1, or 3:1 for large text) against its own backdrop and fits its rect height | `Assets/Scripts/Editor/A11yAudit.cs` (measures), `AccessibilityBuild.cs` (fixes) |
| REQ-19 | Every button and input field is at least 96x96 canvas px | `A11yAudit.cs` (measures) |
| REQ-20 | No button caption is below 20 px and no other label below 18 px, and every control fill is at least 3:1 against its panel (WCAG 1.4.11) | `A11yAudit.cs` (measures, extended this week) - **not met at baseline, defects DEF-15-02 and DEF-15-04** |
| REQ-21 | 100 rapid taps in one second do not crash the game, corrupt the board or bank a match twice | `DamaBoard.HandleTap`, `BankMatch` |
| REQ-22 | The game has no network path, so interrupted network access is not applicable | verified by absence, see `environment.md` |

## Test types used

- **Functional** - played the way a player plays it, through the game's own entry points.
- **Negative** - corrupt save, out-of-turn taps, a capture refused when a capture is mandatory.
- **Boundary** - last piece, no legal move, far row for each side, 100 rapid taps, 0 s match time.
- **Build** - the measurement layer that ships with the project (`A11yAudit`), not the editor's eye.
- **Compatibility** - two configurations where resources permit; recorded **BLOCKED** with the
  reason where they do not.
- **N/A** - interrupted network access: no networking code exists in this project.

## Requirements known-broken before testing started

These come from the Week 14 machine pre-test (`../Week_14/Evidence/autopilot_session.md` section 4)
and from the Week 14 deferred-work table (`Guzman_Charibelle_SA6.md` section 8). They are listed here
so that the baseline run is expected to fail them: a test that only confirms what is already known
broken is worth nothing, while a test that reproduces it is the evidence a fix needs.

| REQ | State at the start | Defect | Severity |
|---|---|---|---|
| REQ-17 | No route back to the title screen or the profile once a match begins (Week 14 finding M2) | DEF-15-01 | S2 |
| REQ-20 | Five button captions are 14 px; `ProfilePanel/StatusText` is 17 px (Week 14 finding M1 and M4) | DEF-15-02 | S3 |
| REQ-09 | The end screen states only the winner, not the score or why the match ended (Week 14 finding M3) | DEF-15-03 | S3 |
| REQ-20 | The audit has no non-text contrast check, so a control whose fill matches its panel is invisible to it (deferred in SA6 section 8) | DEF-15-04 | S3 |

The severity scale is the one used in PT6: **S1** blocker, **S2** major, **S3** minor, **S4** cosmetic.
