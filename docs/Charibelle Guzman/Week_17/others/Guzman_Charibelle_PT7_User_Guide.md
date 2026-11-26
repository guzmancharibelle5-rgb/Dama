# PT7 - User Guide

**Game:** Dama
**Student:** Charibelle Guzman · BSIT-4
**Release:** `1.0.0-rc1` (version code 2) · `com.defaultcompany.dama`
**Platform:** Android 8.0 (API 26) and above, ARM64
**Produced:** 2026-11-26

---

## 1. What Dama is

Dama is the Filipino game of dama - checkers on an 8x8 board - played by two people sharing one phone.
RED starts, RED moves up the board, BLUE moves down it, and a piece that reaches the far row becomes a
**Dama** (a king) that travels any distance along a diagonal. The board is the theme; there is no timer, no
advertisement and no network connection.

## 2. Before you start

| Requirement | Value |
|---|---|
| Android | 8.0 (API 26) or newer, ARM64 device |
| Screen | landscape; the interface is designed for 1280x720 and scales to the screen |
| Internet | **not required** - the game has no network code |
| Storage | one small JSON file, described in section 7 |

### 2.1 Installing this release candidate

**This release candidate has no APK.** The build step for Week 17 was recorded as **BLOCKED**: no Android
package was produced and no handset was attached to test one, so there is nothing here to install and no
install has been claimed. `Evidence/clean_install.md` records the attempt and the reason.

The build can still be reviewed from source, in the Editor:

1. open the project in Unity **6000.6.0f1**;
2. open `Assets/Scenes/DamaGame.unity`;
3. press Play.

That is exactly the scene the six screenshots in `Evidence/` were taken from.

## 3. The screens

### 3.1 Title screen (E1)

```
1.0.0-rc1 · Editor                     <- the build you are running

                      DAMA
        Tap a piece to select, then tap a yellow square to move.
        Captures are mandatory. Keep jumping while captures remain.
        Reach the far row to promote to a Dama.

   HOW TO PLAY        [ START ]        CREDITS
   PROFILE            [ EXIT ]
```

| Control | What it does |
|---|---|
| **START** | begins a match. RED moves first, 12 pieces a side |
| **HOW TO PLAY** | opens the full instructions (section 3.2) |
| **CREDITS** | author, course, engine, licence and the build line (section 3.3) |
| **PROFILE** | your name, side, record and fastest wins |
| **EXIT** | closes the game |

The caption in the top-left corner is the build the game was compiled as. It is read from the application at
start-up, so it cannot say something the game is not.

### 3.2 HOW TO PLAY (E2)

One tap from the title, and **BACK** returns. It covers: the board and the two directions of play; how to
select and move; the capture rule and the requirement to keep jumping while a capture remains; the Dama and
its free travel along a diagonal; how a match ends; the pause menu; the sound toggle; and where the profile is
stored. The body is 1,122 characters at 19 px in a 440 px box - it is the whole rule set, not a summary.

### 3.3 CREDITS (E3)

Author, course and section, engine and render-pipeline versions, the licence the project uses, a statement that
AI assistance was used as a tool, and a build line:

```
Build 1.0.0-rc1 · Editor · com.defaultcompany.dama
```

If that line and the title caption ever disagree, the build is inconsistent - they are both generated from the
same source. **BACK** returns to the title.

### 3.4 A match (E4) and the pause menu (E5)

The HUD shows whose turn it is and how many pieces each side has left, plus a **PAUSE** control in the top-right
corner. Pausing freezes the board and offers:

| Control | What it does |
|---|---|
| **RESUME** | returns to the same side to move, same position |
| **RESTART** | starts a fresh match |
| **SOUND** | toggles sound and spells its own state: `SOUND: ON` / `SOUND: OFF`; remembered between sessions |
| **MENU** | abandons the match and returns to the title screen |

### 3.5 End screen (E6)

The winner is announced, both counts are named, and the summary says **why** the match ended - for example
`RED WINS.  RED 1-1, BLUE has no legal move.` **RESTART** plays again; **MENU** returns to the title.

## 4. How to play, in six steps

1. Tap one of your pieces. The squares it can reach light up yellow.
2. Tap a lit square to move there. The turn passes to the other side.
3. To capture, jump over an adjacent enemy piece onto the empty square beyond it. The jumped piece is removed.
4. If a capture is available you **must** take it - a quiet move will be refused and the board will say so.
5. If the piece that just captured can capture again, the turn stays with you until no jump remains.
6. Reach the far row to become a Dama. A Dama may travel any distance along a diagonal and stops before the
   first piece in its path.

A side loses when it has no pieces left, **or** when it still has pieces but no legal move.

## 5. Reading the board

| Element | Meaning |
|---|---|
| red piece | RED, moving up the board |
| blue piece | BLUE, moving down the board |
| gold crown | that piece is a Dama |
| yellow square | a legal destination for the selected piece |
| darkened board | the match is over; the end panel is in front |

## 6. Accessibility of this interface

Measured in `Evidence/a11y_week17_after.log` against five thresholds (contrast 4.5:1 text / 3:1 large and
non-text, 96x96 canvas px touch targets, caption text 20 px, other labels 18 px):

- every one of the **18 buttons** and the one input field clears the 96 px touch floor;
- no two controls on a panel overlap - a control you can see is a control you can tap;
- every measurable label clears its contrast threshold; the five controls this week added are 3.32:1 to 3.67:1
  against the panel behind them, above the 3:1 non-text floor;
- four items are reported **not measurable**, not passed: the turn and count text drawn over the board, the
  pause button over the board, and the name input which has no fill of its own.

The interface is fixed-size text: it does **not** follow the Android system font-size setting. That is a real
limitation of a fixed-canvas uGUI screen, recorded in Week 15 and unchanged since.

## 7. Your profile and where it lives

The profile holds your name, chosen side, wins, losses and fastest wins.

| Question | Answer |
|---|---|
| Where is it? | one local file inside the app sandbox: `.../DefaultCompany/dama/Dama/profile.json` |
| Is it shared? | no. Nothing leaves the device; the game has no network code |
| Is it protected? | it is signed, so a hand-edited file is detected and set aside rather than trusted |
| What if it is damaged? | the game sets it aside, reports it, and starts from defaults - it does not lose the old file silently |
| How do I clear it? | PROFILE → **RESET**. That is the supported way |

Deleting the app deletes the profile. There is no cloud copy to restore from.

## 8. Troubleshooting

| Symptom | Cause | What to do |
|---|---|---|
| A quiet move is refused | a capture was available | take the capture - the rule is mandatory, and the board says so |
| The turn does not pass after a capture | the capturing piece can capture again | finish the chain; the turn passes when no jump remains |
| Sound is silent | SOUND is off | open the pause menu; the button shows `SOUND: OFF`; tap it |
| The screen looks different from the screenshots | the screenshots are 1151x584 from the Editor | the canvas scales to the device; the layout, not the pixel count, is what to compare |
| A profile message appears at start | the save file could not be read | start a fresh profile; the damaged file was set aside, not overwritten |
| Nothing happens when EXIT is tapped in the Editor | the Editor stops play mode instead of quitting | that is the intended Editor behaviour |

## 9. Telling two builds apart

Open the title screen and read the caption: `1.0.0-rc1 · Editor` means an Editor run of release candidate 1.
Open CREDITS for the same version plus the application id. Both strings come from the application, not from
the scene, so two builds that differ will say so.

A note on what this guide cannot tell you: because this release candidate was never installed on a handset,
nothing here is verified on a phone - not the touch response, not the frame rate, not the battery cost. Those
questions remain **BLOCKED** until a device build exists.
