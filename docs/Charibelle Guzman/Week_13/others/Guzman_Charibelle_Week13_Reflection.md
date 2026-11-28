# Week 13 Reflection - Secure Player Data and Responsible Development

**Game:** Dama
**Student:** Charibelle Guzman
**Section:** BSIT-4
**Course:** MOBILE DEV 002 Advanced Mobile Application Development, Module 5
**Week:** 13
**Date:** November 26, 2026

The structure follows the module's Weekly Self-Assessment and Reflection Guide: the four Week 13
prompts in order, then the required authenticity declaration.

---

## 1. What I built this week, and which decisions were actually mine

The deliverable was a player profile: a name, the side the player takes, a win/loss/draw record,
a win streak and a local best-times board of the five fastest victories, reachable from a PROFILE
button on the start screen and remembered between launches.

The first decision was where the data should live. The easy move was `PlayerPrefs`, but its
values cannot carry an integrity check - there is no place to put a signature over them. If the
point of the week is that an edited record must be *detectable*, the store has to be something a
signature can cover, so the profile is a JSON file at
`Application.persistentDataPath/Dama/profile.json`, and `PlayerPrefs` keeps one key: the salt.

The second was what "secure" should mean with no account, no server and no leaderboard.
Encrypting the save would have been theatre - the key would ship inside the same build as the
file. So `ComputeSignature` takes HMAC-SHA256 over the canonical payload, keyed with 16 bytes
generated at runtime and kept in `PlayerPrefs["profile.salt"]`. Detection over a false promise of
prevention, and I said so on the panel and in the log instead of implying the record is protected.

The third I did not see coming. In a hot-seat game - two people on one phone - a win/loss record
is meaningless: after five matches the phone has three wins, but which of the two people owns
them? So the profile stores a **side**, and `RecordMatch` turns the winner into a result from the
owner's side. Without that field the feature produces a number true about a device and false
about a person. I found it by asking what the record would say after the second person played.

The fourth is small but a player will notice: RESET takes two presses, the button renaming itself
to `PRESS AGAIN`. One tap that wipes an evening's record is a defect waiting to happen on a
touchscreen.

---

## 2. How the build handles the data, and where I stopped claiming things

Everything the game stores is in the SA5 data-inventory table, and the important half is the
"not collected" row: no account, no e-mail, no contacts, no location, no advertising id, no
analytics. The project installs no cloud package, does not force the `INTERNET` permission and
makes no web request. That is what the build does, not a policy I wrote in a document.

Where I stopped claiming things is what mattered most. The save is tamper-evident, not
tamper-proof: the salt sits on the same device as the file, a determined attacker can re-sign an
edited save, and there is no server to check a record against. I recorded that as
`L15 KNOWN LIMIT`.

Keeping it honest took effort, because the temptation was concrete. The weekly task asks for
fifteen checks; if L15 were "run" like the others the log would end *15 passed, 0 failed*. It
ends *14 passed, 0 failed, 1 documented known limit* instead. A test that passes a limitation is
decoration, and the bigger number would have been the worse result.

---

## 3. A problem I hit, and how the checks found it

Two problems came from the same mistake: trusting something that reports later than I assumed.

The first was the double-bank guard. The rule is "one finished match is banked exactly once", and
the obvious implementation compares the match to `LastMatchId` - but `NextMatchId` is *derived*
as `LastMatchId + 1`, so the guard was reading a value that moved as soon as the first bank
succeeded. The test reported `delta=2`: the same match recorded twice, and a record that lies
about how many games were played, which is the one thing a record must never do. The fix was to
pin the id when the match *starts*, so both guards look at a fixed number. It now reports
`wins before=0 after=1 delta=1 lastMatchId=1`.

The second taught me about testing a *picture*. The capture pass seeds three matches, photographs
the panel, seeds four more, photographs again. The first version produced an E2 showing E3's
numbers - seven matches and a five-entry board in an image named `E2_profile_stats`. Nothing in
the code was wrong: `ScreenCapture.CaptureScreenshot` does not finish in the frame that calls it,
so the PNG was written after the profile had moved on. The fix was to stop trusting my own
ordering and trust the disk: delete the old file, request the shot, and do not change the profile
until that file exists and is non-empty.

I would not have caught either by playing the game. Both produced output that looked right at a
glance, and both were caught by a check whose only job was to compare a value with what it should
have been.

---

## 4. What I would do differently, and what carries forward

**Commit while building, not after.** The work is committed (`b9a612d`) and the hash is in the
documents, but it is one large commit made after all the iterations, so the history does not show
the double-bank bug being found or the panel being rebuilt when it overflowed a 584-pixel canvas.
Committing after each working step would make the record reviewable instead of merely present.

**Deciding the failure cases before writing the feature.** Quarantine, the atomic `.tmp` replace
and the refusal of a blank name all came out of the check list rather than my first draft. SA5
section 8 is now a table of failures, and writing that table first would have saved a revision.

**The shape of a good claim carries forward.** The strongest line here is
`L12 IMPOSSIBLE NUMBERS IN A CORRECTLY SIGNED SAVE ARE CLAMPED, NOT TRUSTED [wins=0 losses=0 bestStreak=9]`
- specific, measurable and falsifiable; anyone can re-run it from `HOW_TO_RERUN.txt`. Week 14 is
a usability week, and I intend to write its findings the same way: not "the panel is
understandable" but "three of five testers found the two-press RESET without being told", or
whatever the number turns out to be.

---

## Declaration

I confirm that this reflection describes my own development experience, and that the use of AI
assistance in this week's work - for code, for structure and for phrasing - is disclosed in PT5
section 9 as the module requires.
