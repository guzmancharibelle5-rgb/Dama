# Machine Pre-Test Session — Dama, Week 14

**Game:** Dama v0.4 (Week 14 build — accessibility fixes + audio feedback)
**Engine:** Unity 6000.6.0f1, URP 17.6.0, Windows Editor, 1151 × 584 game view (canvas scale 0.854)
**Harness:** Aura autopilot, play mode, task "explore", 15-minute budget
**Recording:** `Library/AuraVerify/autopilot/2026-11-26T04-32-57-195Z-nb6xkx/recording.mp4`
**Session:** one automated episode (episode 2), 53.2 s of play inside the match plus the setup steps

> **What this document is.** This is a **machine pre-test**, not a playtest with people. It exists to
> catch mechanical defects — unreachable controls, unreadable text, missing feedback — before a
> volunteer is asked to spend twenty minutes on the game. **No human participant took part, and no
> human result is reported here.** The human sessions are not yet run; the kit for them is in
> `../Playtest/`, and `PT6` says plainly that their rows are empty.

---

## 1. Why run a machine pre-test at all

A first-time player is expensive to recruit and easy to waste. If the game has a control that cannot
be reached or a label that cannot be read, a volunteer will find it in the first two minutes — and
those two minutes are then spent on a defect that needed no person to discover. The machine run is
therefore not evidence that the game is usable. It is evidence that the game is *ready to be sat in
front of someone*.

The five tasks given to the run are the same five the human script in `../Playtest/02_session_script.md`
uses, so a defect found here is a defect the volunteers would have hit.

---

## 2. What the run did

| # | Task as given | What happened | Time |
|---|---|---|---|
| 1 | Set a name on your player profile and save it | Name field changed `PLAYTEST` → `PLAYTEST2`; status line became "Saved. Your record is stored on this device only."; `profile.json` existed afterwards | 1.6 s |
| 2 | Start a match | Start panel hidden; HUD showed `Turn: RED`, `Red: 12  Blue: 12` | 2.8 s |
| — | (added by the run) toggle SOUND | `SOUND: ON` → `OFF` → `ON`, both directions | 3.3 s |
| 3 | Catch an enemy | BLUE count fell 12 → 11 on a real click pair | 12.9 s |
| 4 | Crown a man into a Dama | RED chain `(4,3) ×(5,4) → (6,5)`, then `×(5,6) → (4,7)`, king ring visible, BLUE 10 | 13.3 s |
| 5 | Finish the match | Win panel active, `RED WIN!`, profile matches 5 → 6, wins 3 → 4, lastMatchId 6 | 53.2 s |

Every board move in the trace is a real pointer-down on `DamaGame:/DamaBoard/Squares/Square_x_y`
followed by a release. No game state was written by the harness: the capture and the crown were
played, not injected.

The match ended with RED 6, BLUE 1 — BLUE still held a piece but had no legal move, which is the
rule that ends a dama game.

---

## 3. What the run measured

Taken live from the running uGUI, not from the editor:

- **12 interactive controls**, smallest `HUDPanel/PauseButton` at 170 × 96 canvas px. Every one
  meets the 96 px floor. (82 screen px tall at this window size.)
- **No label with an opaque backdrop fails its ratio.** Lowest: START 4.94:1, RESUME 4.73:1,
  RESTART 4.71:1 — all above 4.5.
- `PausePanel/SoundButton` reads **22 px bold, 9.29:1**, and its state is spelled in words.
- `ProfilePanel/StatusText` is the smallest text in the game at **17 px** (12.86:1, so a readability
  note rather than a contrast failure); `BestText` is 20 px.
- `TurnText` (34 px) and `CountsText` (28 px) have no opaque backdrop and are therefore not
  measurable by ratio — the same two rows the audit reports as "not measurable".

---

## 4. Findings

One row per distinct defect. Severity uses the same scale as `PT6`: S1 blocker, S2 major, S3 minor,
S4 cosmetic.

| # | Finding | Severity | Evidence |
|---|---|---|---|
| M1 | **The button captions are 14 px while every other label is 17–110 px.** START, `II PAUSE`, RESUME, RESTART (pause) and RESTART (end screen) are the five smallest strings in the game, and they sit on the largest controls. The audit cannot see this: it measures contrast and box height, not type size. | S3 | §3; frames at t = 2.12 s and 3.12 s |
| M2 | **There is no way back to the start screen once a match has begun.** The pause panel offers RESUME, RESTART and SOUND; the end screen offers RESTART. PROFILE exists only on the start screen, so a player cannot change their name or side without restarting the level or the app. | S2 | `DamaBoard.CacheUI` wires no button back to `StartPanel`/`ProfilePanel` |
| M3 | **The end screen states only the winner.** `RED WIN!` and RESTART; no final counts, and nothing that explains a win by immobilisation — the match ended 6–1 with BLUE still holding a piece, and the screen does not say why. | S3 | t = 53.2 s observation |
| M4 | **The profile status line is the faintest text on the profile panel** at 17 px, alongside a 20 px fastest-victories line. Legible in the capture, marginal at arm's length. | S4 | §3 |

M2 is the one worth acting on first: it is the only finding that stops a player doing something they
would reasonably want to do, and it makes every later session longer for no reason.

---

## 5. What the run got wrong, and how that was handled

The harness raised an alert at the start of the run — *"the game didn't answer session within 60 s;
the run played the editor's own level"* — and then played the game anyway. Every observation in the
trace, however, is against `DamaGame` objects in `DamaGame.unity`: the pointer targets, the HUD text,
the profile store and the win panel are all this project's. The alert is recorded here rather than
quietly dropped, because a harness that contradicts itself should be described, not smoothed over. The
findings above are drawn only from observations that name a DamaGame object or a Dama script.

---

## 6. What this does not replace

- It cannot say whether a rule is **understandable**. The run was told to catch a piece and crown a
  man; a volunteer will not be, and the difference between those two situations is the whole point
  of `PT6`.
- It cannot measure **time to first understanding**, hesitation, or the moment a player gives up.
- It reports one episode. One run is an anecdote with a timestamp, and is presented as such.

The side effect to disclose: the run banked a sixth match into the local profile on this machine
(matches 5 → 6, wins 4). The profile is a local file and the Week 13 reset control erases it.
