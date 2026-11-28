# PA2 — User Guide

**Game:** Dama — the Philippine form of draughts
**Student:** Charibelle Guzman — BSIT-4 · MOBILE DEV 002, Module 5, Week 18
**Version:** `1.0.0-rc1`
**Platform:** Android, 8.0 (API 26) or newer — **see the note below before you look for an installer**
**Revised:** 2026-11-27 — adds UNDO, HINT, the draw rule and the move animation (the post-submission revision, not a graded week)

---

## 1. Before you start — there is no installer

This submission contains **no APK**. No Android build was produced for this project, so there is nothing
to install on a phone. That is recorded as BLOCKED, with the reasons, in
`Week_18/Evidence/final_pre_submission_checklist.md` §3.

To play the submitted game:

1. Open the project in **Unity 6000.6.0f1** (or newer 6000.x).
2. Open `Assets/Scenes/DamaGame.unity`.
3. Press **Play**. The game runs at the Editor's game view resolution.

Everything in the rest of this guide describes that build.

## 2. The title screen

```
1.0.0-rc1 · Editor                       <- the build you are running

        HOW TO PLAY     [ PLAY ]    CREDITS
              PROFILE          [ EXIT ]
```

| Control | What it does |
|---|---|
| **HOW TO PLAY** | the rules, in one screen, with BACK to return |
| **PLAY** | opens CHOOSE MODE (section 2.1): PLAYER vs PLAYER or PLAYER vs AI, the AI level, and which side you take. A match then starts with RED to move |
| **CREDITS** | who made the game, what engine, the licence, the AI-assistance statement, and the build line |
| **PROFILE** | your local name, record, best times and side |
| **EXIT** | quits the game (under the Editor it leaves play mode) |

The caption is not a label someone typed: it is produced by `ReleaseInfo` from the build itself, so it
cannot claim a version the code was not compiled with.

### 2.1 CHOOSE MODE

**PLAY** opens one screen that decides the match before it starts:

| Control | What it does |
|---|---|
| **PLAYER vs AI** | you against the computer, at the level armed below |
| **PLAYER vs PLAYER** | two people passing one device between them |
| **EASY / NORMAL / HARD** | the AI level; the armed one is drawn in the brighter fill and named above the row |
| **PLAYING AS: RED / BLUE** | which side you take; the choice is stored in the profile |
| **BACK** | returns to the title screen without starting anything |

Your side also decides which end of the board is nearest you: play RED and the red men start at the bottom,
play BLUE and the whole board is turned half a turn so the blue men are at the bottom. A tap is read from the
turned board, so the square you touch is the square that is played. The view does not turn again during a
match, so two people passing one device keep one board.

## 3. The rules, as the game enforces them

Dama is played on an 8x8 board, twelve men a side. Men move one square diagonally forward; the board is
drawn from above so both players can read it across a table.

- **A capture must be taken.** If a jump is available to you, the game will not offer you a quiet move.
  Tap a man with no legal move and nothing will be selected — that is the rule, not a broken tap.
- **A capture chains.** If your man lands where another jump is available, it keeps going until the chain
  ends. You do not choose to stop early.
- **Reaching the far row crowns a Dama.** A Dama moves any distance along any diagonal, forward or back.
- **The match ends** when a side has no men left, or has men but no legal move.
- **A match can also end level.** Two counts end it in a draw: the same position, with the same side to move,
  appearing for the **third time**, or **50 turns** in a row with neither a capture nor a promotion. A capture or a
  promotion resets both counts.

**How to move:** tap the man you want, then tap the empty square you want it to go to. The HUD at the top
tells you whose turn it is and how many men each side has.

## 4. In a match

| On screen | Meaning |
|---|---|
| `Turn: RED` / `Turn: BLUE` | whose move it is, in a two-player match |
| `Turn: RED (you)` / `Turn: BLUE (AI)  [thinking]` | against the AI: which colour is yours, and which one is the machine |
| `PLAYER vs PLAYER` / `PLAYER vs AI - HARD` | the mode this match is being played in, under the turn line |
| `Red: 12   Blue: 12` | men remaining, both sides |
| **PAUSE** | the pause panel (below) |
| **HINT** | paints a legal move for the side to move in green. It suggests; it never moves anything, and the next tap clears it |

**Pause panel.** RESUME returns to the match; RESTART starts a new board; **UNDO** takes your last turn back; SOUND
turns the move and end-of-match audio on or off; **MENU** abandons the match and returns to the title screen. That
last one exists because of a defect found in Week 15: before it, once a match started there was no way back to the
title without closing the app.

**UNDO** retracts one *turn* — one move of yours, including a whole forced jump chain, because a chain is one turn
and not three. There is no limit: press it again and again to walk further back. Against the AI one press also takes
back the reply that followed, so the board always returns to you rather than to the machine. The draw counters come
back with the position, so undoing a repetition does not leave the match one appearance closer to a draw.

**End screen.** The winner is named, the reason is written out (`BLUE has no pieces left`, `BLUE has no
legal move`) and the counts are given. A level match says **DRAW** and gives the count that ended it —
`DRAW.  RED 3-3, 50 plies without a capture.` or `DRAW.  RED 3-3, the same position 3 times.` RESTART plays again;
MENU returns to the title.

The match is recorded in your profile when it ends — once, even if the end screen is reached twice. A draw is
recorded as neither a win nor a loss.

## 5. Your profile

The profile stores everything on this device and nothing anywhere else. There is no account and no
network call.

| Field | What it is |
|---|---|
| NAME | up to 16 characters; type it, then **SAVE** |
| SIDE | which colour you play, and which end of the board you sit at; set here or on CHOOSE MODE (section 2.1); wins and losses are counted against it |
| Matches / Wins / Losses / Draws | your record; a draw is counted on its own line, not as a win or a loss |
| Current streak / Best streak | consecutive wins |
| Fastest victories | your three quickest wins, with the match number |
| **RESET PROFILE** | erases name, record and best times — **two presses**, within four seconds |

The two-press reset is deliberate. One tap that wipes an evening's record is a defect waiting to happen
on a phone, and a confirmation dialog is dismissed by habit; so the button arms, changes its label to
PRESS AGAIN, says what will be lost, and waits.

## 6. If something looks wrong

| What you see | Why | What to do |
|---|---|---|
| Tapping a man selects nothing | capture is compulsory and that man has no legal move | tap a man that can jump |
| The game seems frozen after a match | the end screen is modal, and the clock is stopped under it | RESTART or MENU |
| SOUND does nothing in the Editor | the audio device is the Editor's, and the clips are short | it is a settings toggle, not a test |
| UNDO does nothing on the first turn | there is no completed turn to take back yet | play a turn, then pause again |
| UNDO in an AI match takes back two turns | the AI had already answered, so its reply came back with your move | that is intended — the board comes back to you |
| No vibration | `Handheld.Vibrate()` is a no-op in the Editor | unproven on a phone, and not claimed |
| The profile is empty after moving the project | the save lives under `Application.persistentDataPath`, outside the project | nothing to recover; a fresh profile starts |
| The profile says a save was rejected | the file's signature did not match | the old file is kept aside and a clean profile starts |

## 7. Accessibility

- Every control clears a **96 canvas-pixel** touch floor at the project's 1280x720 reference scaler
  (48 dp at xxhdpi is 144 physical px; 144 / 1.50 = 96 canvas px). The two controls added in the post-submission revision also clear it:
  **UNDO** is 272x96 and **HINT** is 160x96.
- Captions are 22 px bold and body labels 19–20 px, measured by the audit, not chosen by eye. UNDO's label measures
  6.24:1 on its own fill and HINT's 12.44:1, both over the 4.5:1 text floor.
- 25 control fills are measurable, and **17 of them sit within a fifth of the 3:1 non-text line** — the closest
  is 3.03:1. All pass; the tightest ones have under 0.6 of headroom, which is the honest reading of that number.
- **What is missing:** text does not follow the phone's font-size setting, and there is no screen-reader
  path. A player who needs either cannot use this game yet; that is stated here rather than hidden.

## 8. What the game does not do

- It does not connect to the internet, and it has no accounts, ads or purchases.
- It does not send anything off the device; the only file it writes is the profile.
- It does not run on a phone in this submission — no build exists. §1 is the whole of that story.
