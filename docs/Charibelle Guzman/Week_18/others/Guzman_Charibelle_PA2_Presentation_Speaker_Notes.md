# PA2 — Speaker Notes

**Charibelle Guzman · BSIT-4 · MOBILE DEV 002, Module 5 · Week 18**

These are the notes that go with `Guzman_Charibelle_PA2_Presentation` — what to say on each slide and
which file to open if a question follows. They are notes, not a script, and they deliberately contain no
prepared answers: the questions in `Evidence/defense_prep.md` are the ones to be answered live.

---

**Slide 1 — Title.** Fifteen seconds. Name, course, week, and the one-line pitch: draughts as it is
played here, two people, one phone. Say the tag `v1.0.0-pa2` out loud so the submission is identifiable.

**Slide 2 — Why this game.** Explain that the theme is carried by the rules and not by a texture:
compulsory capture, chaining, the flying Dama are what make it Dama. If asked where the rules live, open
`Assets/Scripts/DamaBoard.cs` and point at the move generators.

**Slide 3 — Target player.** The important sentence is that the player is *two* people sharing one device.
Everything else on this slide is a consequence: the 96 canvas-pixel touch floor, the top-down board, the
local record. If asked for the derivation of 96: 48 dp at xxhdpi is 144 physical px, and 144 / 1.50 with
the project's 1280x720 scaler is 96 canvas px.

**Slide 4 — Core loop.** Walk the loop on the deck, then say it is three taps from a finished match to a
new one. Mention that MENU on the pause panel exists because of a defect found in Week 15, not because it
was planned — the defect log is `Week_15/Evidence/defect_log.md`.

**Slide 5 — Rules.** The key engineering point: these are constraints on generation, not checks after the
fact. `GetCaptureMoves` is consulted first, and if it returns anything the simple moves are never offered —
which is also why tapping a man with no legal move does nothing.

**Slide 6 — Onboarding, credits, exit.** Describe the three panels as a module requirement that was
missing until Week 17. EXIT is worth a sentence: the wire is proven by a test that names the handler,
the Android behaviour is not proven, because no build ran.

**Slide 7 — Architecture.** Draw the boundary out loud: `DamaBoard` decides what happened,
`ProfilePanel` decides what is shown, `PlayerProfile` decides what is stored. If asked why the UI is built
in code, the answer is reproducibility — and be honest that it has a cost: a builder that runs twice can
duplicate what it builds, which is exactly `DEF-17-05`.

**Slide 8 — Player data.** Say both halves: what the signature does (tamper-evident, atomic write, clamped
fields, rejected-file recovery) and what it does not (encryption, or any defence against the device's
owner). Then the path: `persistentDataPath` is built from `productName` and `companyName`, so the version
went into `bundleVersion` instead.

**Slide 9 — Demonstration.** Point at the nine files and at `Evidence/demo_rehearsal.txt`. Mention the hash
gate and why it exists: the first Week 17 run wrote six names over four pictures because the Editor window
was not foreground. Say plainly that the run is an Editor run.

**Slide 10 — Testing.** Give the three counts: 27 / 0 / 1, 5 / 0 / 0, 148 / 0 / 0 / 4. Then the sentence
that matters: `TC-COMPAT-01` is blocked on hardware and is not counted as a pass anywhere in the package.

**Slide 11 — Profiling.** Lead with the method: measure, change one thing, measure again. The two changes
were allocation-free move generation and piece pooling. Do not claim a frame-time win — the frame was
already inside budget and the mean moved with the session. The evidence is
`Week_16/Evidence/metrics.md`.

**Slide 12 — Usability and accessibility.** The audit is a floor. Name the thin margins (3.16–3.19:1) and
the missing pieces (dynamic type, screen reader) before anyone asks. Say that no human playtest was run,
and that the Week 14 kit is in `Week_14/Playtest/` unused.

**Slide 13 — Limitations.** This is the slide to be slow on. No APK, no handset, Editor numbers only,
haptics unproven, EXIT unfired, caption reads `· Editor`, defense not in the package. Each one is a row in
the README's known-limitations table and in `Evidence/final_pre_submission_checklist.md`.

**Slide 14 — Next.** Close with the one gap that matters most: an installable build on a handset. Then the
playtest, then dynamic type. End on the rule that produced the whole method: measure before claiming.

**If there is time left.** Open the three artefacts that carry the submission: `Evidence/final_results.md`
(the gates), `Evidence/demo_rehearsal.txt` (the nine captures with hashes), and `Evidence/repo_state.txt`
(the tag and commit). Those three answer most questions about what was actually done.
