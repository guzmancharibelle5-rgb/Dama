# Dama — Filipino Checkers Prototype

## Title and Concept
Dama is a two-player, turn-based board-strategy prototype based on Filipino/international
checkers. Players alternate tapping to select a piece and move it diagonally on an 8x8
board; captures are mandatory, multi-jump chains must be finished with the same piece, and
a piece reaching the far row is promoted to a flying "Dama" king.

## Target Users
Two local players sharing one Android device (hot-seat play) — students or casual players
who want a quick strategy match.

## Development Environment
- Unity 6000.6.0f1 (Unity 6), C#
- Universal Render Pipeline (URP) 17.6.0
- Input System package 1.20.0 (Touchscreen + Mouse)
- Target platform: Android, ARM64 architecture, IL2CPP scripting backend

## Project Structure
- `Assets/Scripts/DamaBoard.cs` — core game logic: board state, move/capture generation,
  mandatory capture, chain captures, promotion, win detection, UI panel wiring, restart.
- `Assets/Scripts/BoardCameraFitter.cs` — keeps the 8x8 board fully visible on any screen
  size or aspect ratio by recomputing the orthographic camera size.
- `Assets/Scripts/MobileRuntimeConfig.cs` — sets a 60 FPS target frame rate on Android/iOS.
- `Assets/Scripts/Editor/AndroidBuildSetup.cs` — one-click editor tool
  (`Tools > Android > Apply Mobile Build Settings`) that configures ARM64 + IL2CPP, a mobile
  quality preset, and the build scene list.
- `Assets/Scenes/DamaGame.unity` — the main (and only) gameplay scene.
- `Assets/Sprites/` — `BoardSquare.png`, `PieceCircle.png`.

## Setup and Run Instructions
1. Open the project folder in Unity Hub with Unity **6000.6.0f1**.
2. Open `Assets/Scenes/DamaGame.unity`.
3. Press Play to test with mouse input in the editor, or build to an Android device to test
   with touch.
4. To configure an Android build: `Tools > Android > Apply Mobile Build Settings`, then
   `File > Build Settings > Build` (or `Build And Run`).

## Controls
- Tap/click a piece of your color to select it (legal destinations highlight in yellow).
- Tap/click a highlighted destination to move or capture.
- If a capture is available, only capturing moves are selectable (mandatory capture); after
  a capture, the same piece must continue jumping if another capture is available.
- Pause button (top of HUD) freezes the game; Resume or Restart from the pause menu.
- Restart is also available from the win screen.

## Completed Features
- 8x8 board and 24-piece setup, generated at runtime.
- Tap-to-select / tap-to-move with legal-move highlighting.
- Mandatory capture and forced multi-jump chain captures.
- Promotion to a flying Dama king with long-range diagonal move/capture.
- Turn indicator and live piece counts.
- Start, Pause/Resume, Win, and Restart screens/states.
- Camera auto-fit for any screen size/aspect ratio.
- Android build settings pre-configured via an editor script.

## Incomplete / Deferred Features
- No AI opponent (local two-player only).
- No online multiplayer.
- No audio.
- No score/session persistence.
- Project folder does not yet follow the required
  `PEBSIT004_Prelim_<Surname>_<GameTitle>` naming convention — rename before submitting.

## Build Status
An Android build was not run as part of this documentation pass (no Unity Editor available
in this environment). The build settings are pre-configured in code; run the build locally
and record the real outcome in the Test Record / Issue Record.

## Known Issues
- Board state (grid/king/renderer arrays) does not automatically survive a Unity editor
  domain reload; `BindLayout()` reconstructs it from the scene hierarchy on `Start()`, which
  works for normal play but should be verified after any script recompile mid-session.

## Authorized Assets
All sprites (`BoardSquare.png`, `PieceCircle.png`) and code-generated fallback sprites are
student-created. No third-party or copyrighted assets are used. UI panel/button sprites and
the default font are Unity's built-in, redistributable UGUI assets.

## Next Development Steps
1. Rename the project per the required naming convention.
2. Run and record an actual Android build attempt.
3. Complete the Test Record with real Pass/Fail results from a device or emulator run.
4. Add a simple AI opponent using the existing move-generation methods.
5. Add basic sound effects and simple persistence (e.g., last winner, move count).
