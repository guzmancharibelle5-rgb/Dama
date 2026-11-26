# Facilitator Checklist

Tick every box. A session that skips section 2 produces notes that cannot be compared with any
other session, which is the same as having no notes.

---

## 1. The day before

- [ ] Build, or confirm the build, at a named commit. Write the short hash on the top of the
      observation sheet; `git rev-parse --short HEAD` prints it.
- [ ] Confirm the build opens on `DamaGame` and not an empty scene.
- [ ] Run **Tools > Dama > Week 14 > 1. Run Accessibility Audit** once on the build under test. If
      the log does not end `... 0 failed`, a player is about to be shown a defect already known;
      decide whether the session is still worth their time and tell them if it is not.
- [ ] Print one `01_consent_form.md` per participant, one `03_task_cards.md` set, two copies of
      `04_observation_sheet.md`.
- [ ] Charge the device and pack the charger.
- [ ] Read `02_session_script.md` once, aloud, so the wording is familiar rather than read cold.

---

## 2. Immediately before the participant arrives

- [ ] Device on **Do Not Disturb**; notifications off; rotation locked to landscape.
- [ ] Screen brightness about 70 %, and auto-brightness **off** - a screen that dims mid-task
      changes what the participant can see, and that is not a property of the game.
- [ ] Volume at about **half**, not muted, because one task may involve the sound control.
- [ ] **Reset the in-game profile**: PROFILE > RESET PROFILE twice. The panel must read `PLAYER`,
      0 matches, and the best-times board must read *none yet*.
- [ ] Game left **on the start screen**, running.
- [ ] Observation sheet in front of you with `P___` already written in.
- [ ] Stopwatch or phone timer ready, silent.
- [ ] Water, a chair, and nothing on the table between you and the participant.

---

## 3. During the session

- [ ] Consent sheet ticked and signed **before** the phone is handed over.
- [ ] Opening script read; participant told the game is under test, not them.
- [ ] Warm-up question asked, and the first answer written down **before** any tap.
- [ ] Each task read **once**, exactly as written on the card, then silence.
- [ ] Times written per task to the nearest second.
- [ ] Every direct question answered with **"What do you think it does?"** and logged as help.
- [ ] A stalled task is moved on at 3 minutes, marked `GAVE UP`, and the state at that moment is
      written down.
- [ ] No piece is ever moved for the participant. If the position cannot support T3 or T4, the
      match is restarted and the task timer reset.
- [ ] Quotes written verbatim, not summarised.
- [ ] Optional probe P1 skipped if the session is running past 20 minutes.

---

## 4. Immediately after

- [ ] Debrief asked in the script's order; ratings collected on the card.
- [ ] Participant thanked and told their session is `P___` in the notes.
- [ ] Any paper linking the code to a name destroyed.
- [ ] Notes transferred onto the observation sheet **the same day**.
- [ ] One row per task added to the results table in `PT6` - including the rows that went badly.
- [ ] Any defect found is given a severity from the shared S1-S4 scale, not from how annoying it
      was at the time.

---

## 5. The four rules that keep the notes worth having

1. **An empty row is never filled later.** If only three sessions were run, `PT6` shows three rows
   and two empty ones that say *not run*. An estimate in a results table is a lie with a number in
   it.
2. **A participant is a code.** `P1`, `P2`, and so on, everywhere. No names, no "the girl from
   BSIT-2", no descriptions that identify a person.
3. **No help that was not logged.** If the moderator explained something, that task is
   `DONE WITH HELP` and the explanation is quoted. A task quietly rescued is a defect quietly
   deleted.
4. **One build per comparison.** Do not mix sessions from two commits in one table without saying
   so, because a difference between builds and a difference between people look identical in a
   spreadsheet.

---

## 6. If something goes wrong

| Situation | What to do |
|---|---|
| The game will not open, or opens on an empty scene | Stop. The scene order is wrong - `DamaGame` must be index 0. Fix the build; do not run the session on a broken build |
| The profile was not reset | Stop and reset it. A participant starting from someone else's record is measuring the last session, not the game |
| The participant finishes everything in under four minutes | Let them. A fast, clean session is a result. Record the times and move to the debrief |
| The participant is stuck and visibly frustrated | Mark `GAVE UP`, move on, and say **"That one's my fault, not yours - let's try the next."** Never leave someone grinding on a defect |
| The participant asks about the forced capture at T3 | Answer **"What do you think happens if you try a different move?"** - the game will refuse the move, which is the answer the game itself should be giving |
| The device behaves differently from the editor capture | Trust the device, write down what it did, and note the difference on the sheet. The editor evidence in `../Evidence/` is an editor claim and never overrides a device observation |
| A session has to be abandoned | Keep the notes that exist, mark the rest `NOT RUN`, and record the reason. An abandoned session is data about the session, not a gap to hide |

---

## 7. Where the outputs land

```
Week_14/
  Playtest/
    01_consent_form.md           signed per participant, kept out of the submission
    02_session_script.md         the moderator's wording - do not improvise the tasks
    03_task_cards.md             what the participant reads
    04_observation_sheet.md      one per participant, filled the same day
    05_facilitator_checklist.md  this file
  Evidence/
    a11y_baseline.log            audit before the fixes
    a11y_after.log               audit after the fixes
    a11y_delta.txt               every failure paired with its measured after value
    E1..E3*.png                  pause panel, HUD, profile panel
    feedback_lines.txt           the [Feedback] transcript from a scripted match
    autopilot_session.md         the machine pre-test - not a human session
  PT6                            the results, with the human rows stated as not yet run
```

The five human sessions are run with this kit and are reported in `PT6` section 7. Until they are
run, that section says so in a sentence, in a table, and in the README.
