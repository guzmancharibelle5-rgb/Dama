# Week 17 - Self-Assessment and Reflection

**Student:** Charibelle Guzman
**Section:** BSIT-4
**Course:** MOBILE DEV 002 Advanced Mobile Application Development, Module 5
**Week:** 17 - Release Preparation and Documentation
**Game:** Dama, release candidate `1.0.0-rc1`

---

## 1. What I set out to do

Week 17 is the week the game stops being a class project and becomes something another person could pick up:
it has to say which build it is, who made it, how to play it, and how to leave it. I planned four things -
freeze the tree, set the release identity, build the missing screens, and re-run every gate from Weeks 13-16
against the frozen build - and I wrote the plan down before starting, because the last four weeks taught me
that a step I did not write down is a step I skip when I am tired.

I did all four. I also could not do one of them: the module's clean install on a device. That is recorded as
**BLOCKED**, not as done, and section 5 explains why I think that was the right call.

## 2. What was harder than I expected

**The version number.** I assumed this would be a two-minute settings change. Then I read why the version must
not go into `productName`: on Android the save file lives under a path built from the company and product names,
so putting `1.0.0-rc1` there would have moved the profile store and looked like a wipe to every player. The
version had to go into `bundleVersion`, which is what `Application.version` reads, and the screens had to read
`Application.version` rather than hold a copy of the string. That one decision changed the design of both new
screens: `ReleaseInfo` derives every string, and the title caption is written at start-up, not baked into the
scene.

**The builder.** My panel builder had a real bug and the regression suite found it, not me. I had taught the
builder to delete the old version label and the old panels before rebuilding, but I forgot the three new
buttons - so its second run left two HOW TO PLAY buttons, two CREDITS buttons and two EXIT buttons stacked on
top of each other. The accessibility case reported each control overlapping *itself*, which sounds like a
nonsense message until you realise it is exactly what two objects with the same name at the same position look
like. The regression went from 27 passing to 26 with 1 failed. That failure is the single most useful thing
that happened this week: it means the gate I built in Week 15 is still doing its job.

**The screenshots.** My first capture run produced six file names over four pictures. The editor window was not
the front window, so the Game view stopped drawing after the credits panel and the last three captures were
byte-for-byte copies of it. I only noticed because I hashed the files. The fix was to force a repaint while a
capture is pending - and the lesson was that a screenshot is evidence only if something checks that it is not a
duplicate.

## 3. What I learned about evidence

Three things I did not understand well enough before this week:

1. **A check that passes for the wrong reason is worse than a check that fails.** `TC-REL-04` reported `null`
   where a button's listener should have been. The wiring was fine; the check was reading the wrong field of
   the event object. Had I "fixed" it by loosening the check, I would have shipped a green result that proved
   nothing. It now prints the method name of each of the five handlers.
2. **Idempotence is a property of the tool, not of my memory.** A release tool that appends instead of
   replacing will keep working until the day someone runs it twice - and then it quietly corrupts the scene.
   `DestroyIfExists` now keeps going until nothing with that name is left, so a broken scene can be repaired by
   re-running the tool.
3. **Numbers have to be produced, not remembered.** Every figure in this package - 5/0/0 release checks, 27/0/1
   regression, 148/0/0 with 4 not measurable in the audit, six distinct hashes - comes from a log I can point
   at. When the audit count changed from 163 to 148 because the duplicate controls disappeared, I wrote down
   the reason instead of replacing the old number and hoping nobody noticed.

## 4. The design decisions I would defend

- **Two rows of controls instead of three.** Five 100 px controls do not fit in the band the title leaves at
  the 1280x720 reference; the third row would have pushed EXIT under the bottom edge on the shortest phone the
  canvas scaler supports.
- **Text fitted by measuring, not guessing.** The builder measures the paragraph and steps the size down until
  it fits, logging the result. The first attempt said `even 18px needs 358.3 px of 330.0 px` and refused to
  pretend; I enlarged the box rather than shrinking the rules a player needs.
- **EXIT is a real control with a real method.** In the editor it stops play mode, on a device it quits the
  app, and `TC-REL-04` proves it is bound to `DamaBoard.QuitGame` rather than merely drawn.
- **The panels are state-driven.** They are shown and hidden by the one `ShowState` function, which is why
  `TC-REL-05` can prove they do not leak into a running match. A panel toggled from two places would have been
  a bug waiting for the right sequence of taps.

## 5. What I could not do, and why I did not pretend otherwise

The module asks for the game to be installed on the target device from a clean state. I did not build an APK
this week and no handset was attached, so there is nothing to install and no install happened. I recorded it as
**BLOCKED** with the reason, in three places: the issue log, the clean-install note, and the front page.

I could have made the documents look complete by describing the install as "verified in the editor". That would
have been false: an Editor run says nothing about touch response, frame pacing, memory or battery on a phone.
The compatibility case `TC-COMPAT-01` is blocked for the same reason and has been since Week 15. A reviewer who
needs that evidence needs a device build, and no amount of writing changes that.

The same applies to usability. Week 14 recorded that no participant was available; this week does not invent
one. What I can show is that the interface is legible and reachable by measurement, and that the rules are
written out in full on a screen a player can reach in one tap.

## 6. Self-assessment against the week's checklist

| Item | My judgement | Why |
|---|---|---|
| Release settings and identity | **Met** | `1.0.0-rc1` / code 2, derived onto two surfaces, asserted by `TC-REL-01`/`02` |
| Credits surface | **Met** | CREDITS panel with author, course, engine, licence, AI statement, build line |
| Exit or restart behaviour | **Met** | EXIT wired to `QuitGame`; RESTART on pause and end screens |
| Clear instructions | **Met** | HOW TO PLAY, 1,122 characters, fitted at 19 px, BACK returns |
| Version number and build identifier | **Met** | visible on the title and in credits, both read from the application |
| Regression of Weeks 13-16 | **Met** | 27 passed, 0 failed, 1 blocked, 0 console errors |
| Accessibility not regressed | **Met** | 148 passed, 0 failed, 4 not measurable, all named |
| Six submission images | **Met** | E1-E6, hashes distinct, transcript written |
| Clean install on a device | **Not met - BLOCKED** | no APK, no handset; `ENV-17-01`, `TC-COMPAT-01` |

## 7. What I would do next

1. **Build an APK and install it.** Everything in this package is an Editor measurement; the first device build
   is the only way to close `TC-COMPAT-01` and to find out whether the touch targets are as comfortable on a
   phone as 96 canvas px suggests.
2. **Have one person who has never played it open HOW TO PLAY and then play a match.** That is the only way to
   know whether 1,122 characters at 19 px is instructions or wallpaper.
3. **Keep the release identity boring.** The pattern - derive from `Application.*`, assert it on screen, never
   type it - is cheap and it is the thing most likely to rot if someone edits a label by hand later.
4. **Treat every builder as a tool that will be run twice.** Any future scene builder gets its own
   `DestroyIfExists` discipline from the first line, and a gate that counts the children it expects.
