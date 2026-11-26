# Observation Sheet

One sheet per participant, filled in during and immediately after the session. Copy this file per
session (`P1`, `P2`, ...) or print it. **The tables below are empty on purpose: no human session
has been run yet.** `PT6` section 7 says the same thing, and neither file contains a row that was
not observed.

---

## Session header

| Field | Value |
|---|---|
| Participant code | `P___` |
| Date and start time | `____-__-__  __:__` |
| Build under test | commit `_________` (short hash from `git rev-parse --short HEAD`) |
| Device or editor | `______________________` |
| Screen size | `______ x ______ px` |
| Second player present | yes / no - if yes, their code `P___` |
| Consent form signed | yes / no - **a session without a signed form does not happen** |
| Profile reset to a fresh install before starting | yes / no |
| Screen photographed (optional box ticked) | yes / no |

---

## Per-task record

Fill one row per task. Times to the nearest second, taken from the moderator's stopwatch.

| Task | Result | Time (s) | Help given | Errors or wrong taps | What they said (verbatim) | Severity |
|---|---|---|---|---|---|---|
| T1 - name on the profile and save | | | | | | |
| T2 - start a match | | | | | | |
| T3 - catch a piece | | | | | | |
| T4 - crown a Dama | | | | | | |
| T5 - finish the match | | | | | | |
| P1 - sound off and on *(optional)* | | | | | | |

**Result** is one of:

- `DONE` - finished the task without any help.
- `DONE WITH HELP` - finished, but only after the moderator answered something. Record what was
  answered in "Help given".
- `GAVE UP` - stopped, or was moved on at the 3-minute limit.
- `NOT RUN` - the session ended early, or the optional probe was skipped for time.

---

## Severity scale

The same four levels are used here, in `PT6` and in the machine pre-test's findings, so a reader can
compare a person's problem with a machine's.

| Level | Meaning | Example from this project |
|---|---|---|
| S1 | Blocker - the task cannot be done at all | A control that cannot be reached at all |
| S2 | Major - the task is done late or wrongly, or a route the player needs does not exist | No way back to the start screen once a match has begun |
| S3 | Minor - the task is done, but with visible hesitation or a wrong first tap | A caption small enough to need a second look |
| S4 | Cosmetic - noticed, does not slow anyone down | The faintest line on a panel being the least important one |

---

## Warm-up record

What the participant said the game wanted them to do, before touching anything. This is the only
unprompted statement in the session and is worth more than any answer in the debrief.

> `_______________________________________________________________________________`

---

## Debrief record

| Question | Answer, verbatim where possible |
|---|---|
| Which task was hardest, and what made it hard? | |
| Did the game tell you when a piece was taken? How? | |
| Who won the match, and how do you know? | |
| One thing you would change | |
| Would you play again? Why? | |

### Ratings

| # | Statement | Rating |
|---|---|---|
| R1 | I could tell what to do without being told | `___` / 5 |
| R2 | The pieces and the board were easy to see | `___` / 5 |
| R3 | The game told me when something happened | `___` / 5 |
| R4 | I understood why the match ended | `___` / 5 |
| R5 | I would play this again | `___` / 5 |

---

## Moderator's own notes

Things the tables above have no column for: the moment the participant hesitated, the thing they
tried twice, the thing they said after the session had formally ended.

> `_______________________________________________________________________________`
>
> `_______________________________________________________________________________`
>
> `_______________________________________________________________________________`

---

## What goes into `PT6`

Only these, and no more:

1. The per-task result and time, as measured above.
2. The severity, as assigned above.
3. One or two verbatim quotes per participant, with the code and not the name.

Nothing is averaged into a score, because five people is not a sample and an average of five
opinions would look like a measurement without being one.
