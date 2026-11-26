# Week 14 Reflection - Game Usability, Accessibility and Playtesting

**Game:** Dama
**Student:** Charibelle Guzman
**Section:** BSIT-4
**Course:** MOBILE DEV 002 Advanced Mobile Application Development, Module 5
**Week:** 14
**Date:** November 26, 2026

The structure follows the module's Weekly Self-Assessment and Reflection Guide: the four Week 14
prompts in order, then the required authenticity declaration.

---

## 1. What I built this week, and which decisions were actually mine

An accessibility audit that measures the game instead of describing it, a repair pass that fixes what
it measures, a playtest kit for sessions that have not happened yet - and sound, because a game with
no audio at all cannot be evaluated for usability. Half of what a player needs to know was arriving
after they had to look for it.

The first decision was the touch floor. Everyone quotes 48 dp and the tempting move is to copy the
number. But this project draws in canvas units at a 1.50 scale on a 1080p phone, so 48 dp at xxhdpi
(144 physical px) is **96 canvas px**, not 144. Using 144 would have meant rebuilding the HUD to
satisfy an arithmetic error - a build that looks more accessible on paper and is wrong. The
derivation is in the log header, in both runs, in words a reader can check.

The second was that contrast should be a **rule, not a palette swap**. I could have picked three
darker hex values and ticked the box. Instead the pass takes whatever fill it finds, darkens it 3 % at
a time in its own hue, and re-measures until the label clears its threshold. Green still means go and
amber still means careful, and the rule will still work on a button that does not exist yet.
`#268C40` became `#24843C` and `#D98C1A` became `#A06713`; I chose neither value, the rule did.

The third is small: the toggle says **`SOUND: ON`** in words. An icon needs a second icon to explain
itself, on the panel where a player is least willing to decode anything.

The fourth was the order of the work. "Playtest" is the week's title, so the temptation is to book
volunteers first. Running a machine pass over the same five tasks first meant the four defects found
cost nobody twenty minutes - and two of them, a 14 px caption and no route back to the start screen,
are exactly what a volunteer would have hit in the first two minutes.

---

## 2. How I reported it, and where I stopped claiming things

**Zero human sessions were run.** PT6 section 7 says so at the top, and its results table has five
rows reading `not run` in every cell. I could have written plausible times - a first-time player takes
about forty seconds to find a name field - and no reader could have told. That would have been the
worst thing in this submission, because it would look exactly like measured data.

Three smaller refusals. Two HUD labels are drawn over the board, so their backdrop moves; both logs
report them `NOT MEASURABLE` and neither counts them as a pass, which is why the after-total is 58 and
not 60. The haptic is not claimed, because `Handheld.Vibrate()` is a no-op in the Editor and every
capture this week is from the Editor. And one control passes at **3.01:1 against a 3.0** requirement -
a hundredth of a point, listed in PT6 and the README rather than rounded up, because the next person
to edit that placeholder will put it back under the line and nothing will warn them.

The machine pre-test also contradicted itself: it alerted that it was about to play the wrong level,
then played mine. I recorded the alert and said which observations I would draw findings from, rather
than deleting the line that made the run look unreliable.

---

## 3. A problem I hit, and how the checks found it

Two failures, both of which produced output that looked correct.

The first was in the repair script - code I wrote to fix code I wrote. Its de-overlap pass sorted
controls by `rect.yMax` to work out what sat where, and `RectTransform.rect` is **pivot-local**: `yMax`
is a distance from that control's own pivot, not a position on the panel. Two buttons in different
rows compared as if they shared a place, and the pass shoved them 96 to 137 px down, some off the
canvas entirely. Nothing threw. The scene came back wrong and I only knew because I opened it and
looked. The fix was to compare real world corners converted into the panel's own space. Two more bugs
fell out behind it: a clamp against `0` instead of the panel's `yMin`, and a row nudge that moved one
button of three because it looked for a control *at a position* instead of for the siblings sharing an
anchor row.

The second taught me more. My harness moved a man to the back row expecting a crown. No crown fired.
The move played, the sound played, the log filled up - and only the transcript's last line caught it:
`Kinds fired: 4 of 5`. That square held a BLUE piece, so it was a BLUE move, and BLUE promotes at the
other end of the board. A check for "did some audio play" would have passed. The check that noticed
compared the *set* of event kinds against the set that should exist.

---

## 4. What I would do differently, and what carries forward

**I said I would commit per step, and I did not.** Week 13's reflection ends with that intention in
writing; this week landed as one code commit and one documentation commit again. The cost is concrete:
the three bugs above are invisible in the history, so a reviewer sees a finished repair script with no
evidence it ever pushed a button off a screen. I kept the promise in the code and not in the habit,
and saying so is worth more than quietly dropping the sentence.

**Run the audit before believing a layout is finished.** Every one of the seven touch failures was a
control that was wide enough and too short - exactly what a screenshot makes easy to miss, because the
eye judges width. Run in Week 13, those seven would have cost one menu item instead of a rebuild and a
re-taken set of captures.

**A pass with 0.2 of margin is a warning.** `4.73:1` against `4.5`, twice, and `3.01:1` against `3.0`.
The audit is a floor, and it is now a thing to re-run before every submission rather than a thing I did
once. The four findings the machine pre-test already produced are the first entries in Week 15's
defect log.

---

## Declaration

I confirm that this reflection describes my own development experience, and that the use of AI
assistance in this week's work - for code, for structure and for phrasing - is disclosed in PT6
section 10 as the module requires.
