# Week 18 — Reflection

**Charibelle Guzman · BSIT-4 · MOBILE DEV 002, Module 5**
**Week 18 · PA2 — Final Project, Demonstration and Defense**

---

## 1. Strongest result

The strongest thing in this submission is not a feature. It is that the three gates can be re-run in
twenty minutes and produce the same answers on the same tree: **5 passed / 0 failed** for the release
checks, **27 passed / 0 failed / 1 blocked** for the regression suite, **148 passed / 0 failed / 0 warnings
/ 4 not measurable** for the accessibility audit. Those numbers are in
`Week_18/Evidence/final_results.md` and each one is copied from the log beside it.

That matters because a claim nobody can re-check is a story. When the Week 17 builder duplicated three
title buttons, the regression suite reported each control as overlapping *itself* — a sentence no human
would write by looking at a screenshot — and the fix was accepted only once the same suite returned zero
overlaps. Testing that can catch its own week's mistake is worth more than a feature list.

The second result is smaller but I like it more: the demonstration harness disproved itself twice before
its output was used. It refused to open the profile because the game was mid-match, and it measured best
times of `0.0 s` because it banked three matches inside one tick. Both were caught by the harness's own
logging, both were fixed, and both are written down in `final_results.md` §3 instead of being quietly
re-run until the pictures looked right.

## 2. Most important limitation

**There is no installable build.** The module asks for one, and this submission does not have one — no
APK, no keystore, no device. It is recorded as BLOCKED in three places and `TC-COMPAT-01` stays blocked
in every log.

I could describe that as a scheduling problem, but that would be the comfortable answer. The real reason
is that I chose, week after week, the work that could be finished inside the Editor: a harness, a log, a
capture, a document. Every one of those was verifiable, and none of them required me to produce something
I could not check — a signed package running on glass. So the submission is strong exactly where Editor
evidence is strong and silent exactly where it is not, and the module's central deliverable is the one
thing missing.

The honest cost is not "no screenshots from a phone". It is that nobody — including me — knows whether
this game installs, launches, respects the Android lifecycle, or responds correctly to a real thumb. The
Editor shares the scene and the scripts with a build, so the behaviour is likely similar; likely is not
measured, and this project spent six weeks refusing to publish the difference.

## 3. Next improvement

In order:

1. **Build it, sign it, install it.** A keystore held outside the repository, `androidUseCustomKeystore:
   1`, a release build at IL2CPP/ARM64/API 26, then `adb install -r` on a handset and the nine
   demonstration screens re-taken on the device. That single change would turn the largest BLOCKED row in
   the submission into a measured result, and it is the only item on this list that closes a gap rather
   than adding a feature.
2. **Run the playtest kit that already exists.** Consent form, session script, five task cards,
   observation sheet — written in Week 14 and never used. Three volunteers would tell me whether the
   compulsory-capture rule is understood or merely enforced, which no audit can answer.
3. **Follow the phone's font scale.** The audit measures a size floor; it cannot make the game obey a
   system setting. That is WCAG 2.2 SC 1.4.4, and it stays unmet until the UI reads the platform scale.

Everything else — a nicer board, more sounds, an animated crowning — is decoration next to those three.

## 4. What the six weeks actually taught me

The method, more than the code: decide what would count as proof *before* doing the work, then refuse to
write a number you did not measure. Week 16 is where it became concrete — a byte counter that looked
right was tested against a deliberate 64 KB allocation, failed, and was replaced rather than reported.
Week 17 repeated it when a check read the wrong field of a UI listener and reported `null`: the check was
wrong, the code was fine, and the record says so.

The cost of that discipline is visible too. It is slower, it produces documents about what did *not* work,
and it means handing in a package whose largest row says BLOCKED. I think that is the right trade. A
submission that says "nine distinct captures, 27 cases, and here is the one thing I could not do" is worth
more than one that implies everything was finished.

## 5. AI assistance declaration

AI was used as an assistant for code drafts, document structure and phrasing, and for running the
harnesses. Every number in every document of this submission was copied from a log or a command output
that the tool produced on this machine; nothing was typed from memory or estimated to fill a table. Where
the evidence could not support a claim — a device install, a phone frame rate, a human playtest, the oral
defense — the documents say so. The full disclosure is §6 of
`Guzman_Charibelle_PA2_Originality_Declaration`, and the defect history that came out of working with an
assistant is §8 of the technical report.

The defense itself is mine to sit. Nothing in this package is a prepared answer to a defense question;
`Evidence/defense_prep.md` holds pointers to files, classes and commits, and `Evidence/e3_record.md` is
blank until it is filled in front of the instructor.
