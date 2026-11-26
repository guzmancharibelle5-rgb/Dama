# Session Script - Moderator's Copy

**Study:** Dama mobile game - usability session
**Student:** Charibelle Guzman, BSIT-4
**Duration:** 20 minutes per session (5 minutes setup and consent, 10 minutes playing, 5 minutes debrief)
**Participants:** 3 to 5, coded `P1` to `P5`. No session is run without `01_consent_form.md` signed.

Say the parts in **bold**. The rest is instruction to the moderator and is not read aloud.

---

## 1. Before the participant sits down (3 minutes)

1. Read `05_facilitator_checklist.md` and tick it.
2. Build the game from the commit named on the observation sheet. Do not use a build you cannot
   name - a session on an unknown build produces notes nobody can act on.
3. Reset the in-game profile to a fresh install, so every participant starts from the same screen:
   open **PROFILE** and press **RESET PROFILE** twice. The panel must read `PLAYER`, 0 matches,
   and the best-times board must read *none yet*.
4. Set the phone to **Do Not Disturb**, screen brightness to about 70 %, and lock rotation to
   landscape if the device allows it.
5. Put `04_observation_sheet.md` in front of you with the participant code already written in.

---

## 2. Opening (2 minutes)

**"Thanks for helping. I made a small game for a school subject and I need to see how someone
who has never seen it gets on with it. I am testing the game, not you - if something is confusing,
that is a fault in the game and it is exactly what I need to find."**

**"You will play for about ten minutes. I will read out five short tasks one at a time. While you
play, please think aloud - say what you are looking at, what you expect to happen, and when you
are unsure. There is no wrong answer and no score."**

**"I will take notes, but I will not write your name. You are `P1` on my sheet. Nothing is
recorded on video or audio unless you ticked that box, and you can stop at any time and I will
throw the notes away. Any questions before we start?"**

Then hand over `01_consent_form.md` and collect the ticked boxes. Do not start without them.

---

## 3. Warm-up (1 minute)

Hand over the phone on the **start screen**, with the game already running.

**"This is the game as it opens. Have a look at it for a moment and tell me what you think it
wants you to do. Don't tap anything yet."**

Write down the first thing they say, word for word. Do not correct them.

---

## 4. The five tasks (10 minutes)

Read each task **once**, in the words below, and then stop talking. These five are the same five
the machine pre-test in `../Evidence/autopilot_session.md` was given, so a defect found there is a
defect a person would hit here.

| # | Read aloud, exactly | What it probes |
|---|---|---|
| T1 | **"Put your name on the player profile and save it. Use a made-up name, not your real one."** | Whether the PROFILE entry point is findable from the start screen, and whether a name field with a visible limit is understandable |
| T2 | **"Start a match."** | Whether the start screen's primary action is unambiguous, and whether the HUD explains whose turn it is |
| T3 | **"Catch one of the other side's pieces."** | Whether the forced-capture rule is discovered: a jump exists, and the game will not let them play anything else |
| T4 | **"Crown one of your men into a Dama."** | Whether the back-row promotion is understood, and whether the change in the piece is visible |
| T5 | **"Finish the match."** | Whether the end condition - no pieces, or no legal move - and the end screen are understandable |

### Rules of moderation

- **Do not help.** If the participant asks a direct question, answer **"What do you think it
  does?"** and write the question down. Only two things earn an intervention: the session running
  past the time budget, or the participant becoming distressed.
- **If a task stalls for 3 minutes**, mark it `GAVE UP`, move on to the next task, and record what
  they were doing at the moment they stopped. A stall is a finding, not a failure to record.
- **Write the clock.** Note the start and end time of each task to the nearest second. The task
  time is the single most useful number on the sheet.
- **Quote, do not paraphrase.** When they say something about the game, write the sentence, not
  your summary of it. `"I can't tell if it's my turn"` is evidence; `"confused by turn indicator"`
  is your opinion.
- **Never move a piece for them.** If T3 or T4 is impossible in the position reached, restart the
  match yourself, say **"I'll start a fresh game"**, and reset that task's timer.

### Optional probe, only if time allows (1 minute)

Not one of the five tasks, and skipped if the session is running long.

**"Turn the game's sound off, then turn it back on."**

Write where they looked first. This is the only task that touches the Week 14 SOUND control.

---

## 5. Debrief (5 minutes)

Ask these in order. Let silence do the work; wait three seconds before rephrasing anything.

1. **"Which task was hardest, and what made it hard?"**
2. **"When you took a piece, did the game tell you it had happened? How?"**
3. **"Who do you think won the match, and how do you know?"**
4. **"If you could change one thing about the screen, what would it be?"**
5. **"Would you play this again with a friend? Why, or why not?"**

Then the five ratings, read aloud with the scale visible on paper:

| # | Statement | 1 = strongly disagree, 5 = strongly agree |
|---|---|---|
| R1 | "I could tell what to do without being told." | 1 2 3 4 5 |
| R2 | "The pieces and the board were easy to see." | 1 2 3 4 5 |
| R3 | "The game told me when something happened." | 1 2 3 4 5 |
| R4 | "I understood why the match ended." | 1 2 3 4 5 |
| R5 | "I would play this again." | 1 2 3 4 5 |

---

## 6. Closing (1 minute)

**"That's everything. Thank you - this genuinely changes what I fix next. If you think of
something later, tell me and I'll add it."**

1. Destroy the paper that links `P___` to a name, if one was made.
2. Move the notes onto the observation sheet for that code the same day, while the session is
   still fresh. Notes that sit for a week turn into someone else's session.
3. Add the row to the results table in `PT6`. **A session that was run and went badly is a row; a
   session that was not run is an empty row, and an empty row is never filled with an estimate.**

---

## 7. What is deliberately not in this script

- **No leading questions.** Nothing in section 4 tells the participant where to look. `"Find the
  PROFILE button"` would have made T1 trivial and told us nothing.
- **No training.** The forced-capture rule is not explained before T3, because the whole question
  is whether the game teaches it. If it does not, that is the finding.
- **No timer the participant can see.** A visible countdown changes how people behave; the
  moderator's stopwatch does not.
- **No made-up severity.** Severity in `04_observation_sheet.md` is assigned from what was
  observed - a stuck task and a wrong tap are different rows.
