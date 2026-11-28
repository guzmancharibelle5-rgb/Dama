# PT7 - User Guide

**Game:** Dama
**Student:** Charibelle Guzman · BSIT-4
**Release:** `1.0.0-rc1` (version code 2) · `com.defaultcompany.dama`
**Platform:** Android 8.0 (API 26) and above, ARM64
**Produced:** 2026-11-26
**Revised:** 2026-11-27 - adds UNDO, HINT, the draw rule and the move animation (the post-submission revision, not a graded week)

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

   HOW TO PLAY        [ PLAY ]        CREDITS
   PROFILE            [ EXIT ]
```

| Control | What it does |
|---|---|
| **PLAY** | opens CHOOSE MODE (section 3.2): PLAYER vs PLAYER or PLAYER vs AI, the AI level, and which side you take. A match then starts with RED to move, 12 pieces a side |
| **HOW TO PLAY** | opens the full instructions (section 3.3) |
| **CREDITS** | author, course, engine, licence and the build line (section 3.4) |
| **PROFILE** | your name, side, record and fastest wins |
| **EXIT** | closes the game |

The caption in the top-left corner is the build the game was compiled as. It is read from the application at
start-up, so it cannot say something the game is not.

### 3.2 CHOOSE MODE

PLAY opens one screen that decides the match before it starts:

| Control | What it does |
|---|---|
| **PLAYER vs AI** | you against the computer, at the level armed below |
| **PLAYER vs PLAYER** | two people passing one device between them |
| **EASY / NORMAL / HARD** | the AI level; the armed one is drawn in the brighter fill and named above the row |
| **PLAYING AS: RED / BLUE** | which side you take. The choice is stored in the profile, so it is the same side the record is kept against |
| **BACK** | returns to the title screen without starting anything |

Your side decides which end of the board is nearest you: play RED and the red men start at the bottom, play BLUE and
the whole board is turned half a turn so the blue men are at the bottom. A tap is read from the turned board, so the
square you touch is the square that is played. The view does not turn again during a match, so two people passing one
device keep one board.

### 3.3 HOW TO PLAY (E2)

One tap from the title, and **BACK** returns. It covers: the board and the two directions of play; how to
select and move; the capture rule and the requirement to keep jumping while a capture remains; the Dama and
its free travel along a diagonal; how a match ends; the pause menu; the sound toggle; and where the profile is
stored. The body is 1,122 characters at 19 px in a 440 px box - it is the whole rule set, not a summary.

### 3.4 CREDITS (E3)

Author, course and section, engine and render-pipeline versions, the licence the project uses, a statement that
AI assistance was used as a tool, and a build line:

```
Build 1.0.0-rc1 · Editor · com.defaultcompany.dama
```

If that line and the title caption ever disagree, the build is inconsistent - they are both generated from the
same source. **BACK** returns to the title.

### 3.5 A match (E4) and the pause menu (E5)

The HUD shows whose turn it is and how many pieces each side has left, plus **PAUSE** and **HINT** in the top-right
corner. **HINT** paints a legal move for the side to move in green - it suggests, it never moves anything - and the
next tap clears it. Pausing freezes the board and offers:

| Control | What it does |
|---|---|
| **RESUME** | returns to the same side to move, same position |
| **RESTART** | starts a fresh match |
| **UNDO** | takes your last turn back (section 4.1) |
| **SOUND** | toggles sound and spells its own state: `SOUND: ON` / `SOUND: OFF`; remembered between sessions |
| **MENU** | abandons the match and returns to the title screen |

### 3.6 End screen (E6)

The winner is announced, both counts are named, and the summary says **why** the match ended - for example
`RED WINS.  RED 1-1, BLUE has no legal move.` A match that ends level says **DRAW** and gives the reason:
`DRAW.  RED 3-3, 50 plies without a capture.` or `DRAW.  RED 3-3, the same position 3 times.` **RESTART** plays
again; **MENU** returns to the title. A draw is recorded in the profile as neither a win nor a loss.

## 4. How to play, in six steps

1. Tap one of your pieces. The squares it can reach light up yellow.
2. Tap a lit square to move there. The piece travels across the board in about a tenth of a second, so you can see
   where it went; a capture lands with a short scale pop. The turn passes when the move has landed.
3. To capture, jump over an adjacent enemy piece onto the empty square beyond it. The jumped piece is removed.
4. If a capture is available you **must** take it - a quiet move will be refused and the board will say so.
5. If the piece that just captured can capture again, the turn stays with you until no jump remains.
6. Reach the far row to become a Dama. A Dama may travel any distance along a diagonal and stops before the
   first piece in its path.

A side loses when it has no pieces left, **or** when it still has pieces but no legal move. A match can also end
level, on either of two counts: **the same position, with the same side to move, appearing for the third time**, or
**50 turns in a row with neither a capture nor a promotion**. Both counters are reset by a capture or a promotion,
and UNDO rewinds them with the position.

### 4.1 UNDO, and what a turn is

UNDO, in the pause menu, retracts **one turn** - one move of yours, including a whole forced jump chain, because a
chain is one turn and not three. It can be pressed again and again; there is no limit and no cost. Against the AI
one press also takes back the reply that followed, so the board always comes back to you and never to the machine.
The draw counters rewind with the position, so a position that had appeared twice before the undo does not become a
third appearance after it.

## 5. Reading the board

| Element | Meaning |
|---|---|
| red piece | RED, moving up the board |
| blue piece | BLUE, moving down the board |
| gold crown | that piece is a Dama |
| yellow square | a legal destination for the selected piece |
| green square | HINT's suggestion: the piece to move and where it should go |
| darkened board | the match is over; the end panel is in front |
| the board turned round | you are playing BLUE, so the blue end is nearest you; the squares are read from the turned board |

## 6. Accessibility of this interface

Measured in `Evidence/a11y_week17_after.log` against five thresholds (contrast 4.5:1 text / 3:1 large and
non-text, 96x96 canvas px touch targets, caption text 20 px, other labels 18 px):

- every one of the **18 buttons** and the one input field clears the 96 px touch floor;
- no two controls on a panel overlap - a control you can see is a control you can tap;
- every measurable label clears its contrast threshold; the five controls this week added are 3.32:1 to 3.67:1
  against the panel behind them, above the 3:1 non-text floor;
- four items are reported **not measurable**, not passed: the turn and count text drawn over the board, the
  pause button over the board, and the name input which has no fill of its own.

The post-submission revision added two controls, measured in the Week 18 audit rerun of 2026-11-27
(`Week_18/Evidence/a11y_week18_audit_20261127_182110.log`, 203 passed / 0 failed / 0 warnings):

- **UNDO** is 272x96 canvas px with its label at 6.24:1 on its own fill - over both the 96 px touch floor and
  the 4.5:1 text floor;
- **HINT** is 160x96 px and its label is 12.44:1. Like PAUSE it sits over the board with no panel behind it, so
  the audit reports its fill as **not measurable** rather than passing it;
- that rerun measured **28 controls** across all eight panels: every one clears the 96 px floor, no two overlap, and
  the six items it cannot measure are the three HUD texts, PAUSE, HINT and the name input.

The interface is fixed-size text: it does **not** follow the Android system font-size setting. That is a real
limitation of a fixed-canvas uGUI screen, recorded in Week 15 and unchanged since.

## 7. Your profile and where it lives

The profile holds your name, chosen side, wins, losses, draws and fastest wins.

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
| UNDO does nothing on the first turn | there is no completed turn to take back yet | play a turn, then pause again |
| UNDO in an AI match takes back two turns | the AI had already answered, so its reply came back with your move | that is the intended behaviour - the board returns to you |

## 9. Telling two builds apart

Open the title screen and read the caption: `1.0.0-rc1 · Editor` means an Editor run of release candidate 1.
Open CREDITS for the same version plus the application id. Both strings come from the application, not from
the scene, so two builds that differ will say so.

A note on what this guide cannot tell you: because this release candidate was never installed on a handset,
nothing here is verified on a phone - not the touch response, not the frame rate, not the battery cost. Those
questions remain **BLOCKED** until a device build exists.
