# Week 18 — final results

**Student:** Charibelle Guzman · BSIT-4 · MOBILE DEV 002, Module 5, Week 18 (PA2)
**Tree under test:** `main` at `01d58df` plus the Week 18 tooling (`Week18Captures.cs`, the Week 18 seams in `A11yAudit` / `Week15Checks` / `Week17Checks`)
**Version under test:** `1.0.0-rc1`, version code `2`, application id `com.defaultcompany.dama`
**Environment:** Unity 6000.6.0f1, URP 17.6.0 (2D), WindowsEditor, game view 1151x584 — **Editor only. No APK was built and no handset was attached** (`TC-COMPAT-01`: BLOCKED).

Every number below is copied from the harness output that produced it. Nothing is estimated.

---

## 1. The three gates

| Gate | Harness | Result | Evidence | Run at (UTC) |
|---|---|---|---|---|
| Release checks | `Week17Checks.Begin()` with `RunDirOverride` → `Week_18/Evidence/` | **5 passed, 0 failed, 0 console errors** | `test_logs_release/summary.txt`, `case_log.tsv`, `console.log` | 2026-11-26 08:52:52 |
| Full regression | `Week15Checks.BeginWeek18()` | **27 passed, 0 failed, 1 blocked, 0 console errors** | `test_logs_regression/summary.txt`, `case_log.tsv`, `console.log` | 2026-11-26 08:53:43 |
| Accessibility audit | `A11yAudit.RunWeek18()` | **148 passed, 0 failed, 0 warnings, 4 not measurable** | `a11y_week18_after.log` | 2026-11-26 08:54:50 |

The single blocked case is `TC-COMPAT-01`, blocked on hardware and reported as blocked, never as a pass:

```
TC-COMPAT-01  BLOCKED compatibility Second device or configuration profile
              BLOCKED: no APK was built this week (Editor-only evidence) and no Android handset was attached
```

The four not-measurable audit rows are the same four Week 17 carried, and they stay unmeasured by name:

| Row | Why it cannot be measured |
|---|---|
| `HUDPanel/TurnText` | white text over the board, no opaque backdrop |
| `HUDPanel/CountsText` | white text over the board, no opaque backdrop |
| `HUDPanel/PauseButton` | the control has no opaque panel behind it |
| `ProfilePanel/NameInput` | the input has no opaque fill of its own |

**Why the logs did not overwrite Week 17.** `Week17Checks` wrote a fixed path under `Week_17/Evidence/` and `A11yAudit` a fixed per-week filename, so re-running either would have rewritten committed evidence that `Week_17/Evidence/readiness_results.md` cites. Both gained a Week 18 seam: `Week17Checks.RunDirOverride` and `A11yAudit.RunWeek18()`; `Week15Checks` gained `BeginWeek18()`, whose folder is derived from the week name. Week 17's and Week 16's logs are byte-identical to their committed state after this week's runs.

## 2. The demonstration sequence — nine screens, tested rather than described

`demo_rehearsal.txt` is one scripted run of the game, in order, with the seconds since the run began
next to every line. Each step below has its own PNG and its own offset.

| Requirement (module) | Screen | Capture | sha256 (first 16) | Offset |
|---|---|---|---|---|
| Onboarding | Title + version caption | `D1_title.png` | `d1cf682018b8ca3f` | +1.8 s |
| Onboarding | HOW TO PLAY, 1122 characters at 19 px | `D2_how_to_play.png` | `984fabeaccbf8100` | +2.8 s |
| Onboarding / disclosure | CREDITS + build line | `D3_credits.png` | `6342537d78e673f0` | +3.7 s |
| Data features | PROFILE with record and best times | `D4_profile_record.png` | `4fe0c36405c1a396` | +6.0 s |
| Error handling | PROFILE after the two-press RESET | `D5_profile_after_reset.png` | `1344436b8d81043e` | +6.9 s |
| Core loop | fresh match, 12 v 12 | `D6_match_fresh.png` | `392f131fb8e212a1` | +7.7 s |
| Theme mechanics | after a real jump: RED 1, BLUE 2 | `D7_match_after_capture.png` | `a86927876e60267e` | +8.6 s |
| Settings | pause — RESUME / RESTART / SOUND / MENU | `D8_pause.png` | `f1a218dab19ed260` | +9.5 s |
| Completion | end screen, RED wins by immobilisation | `D9_end_screen.png` | `b52cc8fd23c81245` | +10.4 s |

All nine hashes are distinct; the harness fails the run if any two match. The title reads
`1.0.0-rc1 · Editor`, the credits build line reads `Build 1.0.0-rc1 · Editor · com.defaultcompany.dama`,
and `TC-REL-01`/`TC-REL-02` above compare both to `Application.version`.

D4's record is real: three matches banked through `DamaBoard.BankMatchForTests` → `BankMatch` →
`PlayerProfile.RecordMatch`, measured at 0.5 s and 0.5 s for the two wins. They are banked into a
temporary data directory (`%TEMP%\DamaWeek18Demo`), not the player's own store, because the very next
step erases them.

## 3. What the run found, and what was corrected

Three faults, all in the harness rather than in the game, all disclosed rather than quietly fixed:

1. **D4 showed the wrong screen.** `ShowProfile()` only answers from the title screen, and the first
   version of the harness seeded the record and asked for the profile while the board was mid-match, so
   the request was refused. `D4` reported `ProfilePanel=False`, the record read all zeros, and D4, D5 and
   D6 landed **byte-identical** (`392f131fb8e212a1`) — three names over one picture, which is the exact
   failure the hash gate exists to catch. Fixed by walking the state machine back with `ReturnToMenu()`
   before `ShowProfile()`.
2. **Best times read `0.0 s`.** All three matches were banked inside one editor tick, so the game's own
   `Time.unscaledTime - matchStartUnscaled` measured microseconds. The banks are now one per stage, so
   each duration is real elapsed play time — the accepted values are `0.5 s`, `0.5 s`.
3. **The real profile gained a match per run.** The end screen is produced by the game's own
   `CheckForWin` → `DeclareWinner` path, and that path banks the match. During the first three runs the
   real store was restored *before* the match images, so each run added one RED win to
   `…\LocalLow\DefaultCompany\dama\Dama\profile.json` — the store read 5 matches before the first run
   and 8 after the third. The restore now happens after D9, so the committed harness banks into the
   temporary folder and adds nothing. The three extra rows were **left in place and disclosed here**
   rather than removed, because removing them would mean hand-editing the player's save and
   `PlayerProfile` is the only class permitted to write that file.
4. **The sentence above was itself wrong, and this is the correction.** The three rows are not in the store.
   The regression gate in section 1 ran three minutes after the captures, and its data cases are written
   against the *default* data directory: `TC-DATA-01` calls `PlayerProfile.UseDefaultDataDirectory()` and then
   `ResetProfile()`; `TC-DATA-02` banks one match; `TC-DATA-03` banks another and resets again; `TC-DATA-04`
   writes a deliberately corrupt file, which is quarantined; `TC-DATA-05` leaves the name at `QA-LOCAL`. So the
   suite erased the three rows itself and left a test record behind. The store on this machine now reads
   257 bytes, `0` matches, name `QA-LOCAL`, with `profile.rejected.json` (20 bytes) sitting beside it from
   `TC-DATA-04`, and the directory carries 2026-11-26 16:53 local — 08:53 UTC, which is the gate's run time.
   An independent play-mode verification at 09:18 UTC read the same 257 bytes and the same SHA-256
   (`bcc9ddef…2d27d`) before and after its own run, so nothing has touched it since.

   Nothing was hand-edited, and no claim in this package rests on the old sentence: what the demonstration
   proves is that the game banks a finished match, which `TC-DATA-02` proves on its own and `D4` shows on a
   record built in a scratch folder. But the original sentence was checked against the `matches=8` line in
   `demo_rehearsal.txt` instead of against the file on disk, which is the mistake worth naming.

   Two consequences. First, the gate's own data cases are destructive to whatever profile the machine holds —
   correct for a development machine whose store is a test artifact, wrong on a player's device, and now
   written down instead of assumed. Second, `docs/Charibelle Guzman/Week_18/Evidence/` describes the store as
   it was read, not as it was hoped to be: the correction stays in the file rather than replacing the record
   of the mistake.

## 4. What these results do not prove

- **Nothing about an installed build.** No APK exists, so the demonstration is an Editor demonstration.
- **Nothing about a phone.** No frame rate, battery, thermal or memory behaviour is claimed anywhere in
  this package; the Week 16 numbers are Editor numbers and stay recorded as such.
- **Nothing about a real player.** The demonstration is the harness driving the game through its own
  entry points; no human playtest session has been run, in this week or any other.
- **The defense was not sat.** `e3_record.md` is blank by design and `defense_prep.md` holds pointers,
  not answers.

## 5. Final status of the pre-submission checklist

This is the re-read of `final_pre_submission_checklist.md` promised at the top of that file: its two status
columns are headed "status at start of week" and are left untouched, and every row's final status is here.

### 5.1 The module's eight Week 18 procedure rows

| # | Requirement | Final status | What proves it |
|---|---|---|---|
| 1 | Final pre-submission checklist covering repository, build, documentation, evidence, credits, licences and disclosure | **PASS** | `final_pre_submission_checklist.md`, written before any other artefact; `secret_scan_week18.md` for the hygiene rows |
| 2 | Final tagged repository version, with tag and commit recorded | **PASS** | tag `v1.0.0-pa2` on `308c8ed`; code `b8aca5c`, documentation `2c5b275`; `repo_state.txt` §3, §6, §7 |
| 3 | Installable build, source/repository access, technical report, user guide, evidence portfolio, presentation, signed declaration | **PASS for the documents and repository access; BLOCKED for the installable build** | eight PDFs, page-counted at 5, 5, 3, 4, 3, 14, 2, 2; `repo_state.txt` §7 records `b79848f..308c8ed main -> main`; the build is BLOCKED per `release_record.md` §3 |
| 4 | Install or launch the submitted build from the same package the instructor holds | **BLOCKED** | no package exists and `adb devices` returns no serial — `final_pre_submission_checklist.md` §3.1 |
| 5 | Concise presentation of theme, player, mechanics, architecture, services, security, usability, testing, optimisation, limitations | **PASS** | `Guzman_Charibelle_PA2_Presentation.pdf` — 14 slides, every page 960x540 pt; outline in `…_Presentation_Speaker_Notes.pdf` |
| 6 | Demonstrate onboarding, core loop, theme mechanics, settings, data, error handling, completion | **PASS** | `D1`–`D9`, nine distinct SHA-256 values, `demo_rehearsal.txt` |
| 7 | Individual technical defense naming files, classes, commits, test records and profiling evidence | **NOT SAT** | `defense_prep.md` (49 questions, pointers, no answers); `e3_record.md` blank |
| 8 | Brief reflection: strongest result, most important limitation, next improvement | **PASS** | `Guzman_Charibelle_Week18_Reflection.pdf` — 2 pages |

### 5.2 The named submission items

| Item | Final status | Note |
|---|---|---|
| Installable build | **BLOCKED** | three independent reasons, `release_record.md` §3; four steps that would close it, §5 |
| `Guzman_Charibelle_PA2.pdf` | **PASS** | 5 pages |
| `Guzman_Charibelle_PA2_Technical_Report.pdf` | **PASS** | 5 pages |
| `Guzman_Charibelle_PA2_User_Guide.pdf` | **PASS** | 3 pages |
| `Guzman_Charibelle_PA2_Evidence_Portfolio.pdf` | **PASS** | 4 pages |
| Originality declaration | **UNSIGNED** | complete except the signature line, which is deliberately blank; an unsigned copy is not a signed declaration |
| Presentation + speaker notes | **PASS** | 14 slides at 16:9 and 2 pages of outline; a PDF deck, not a `.pptx` |
| `Guzman_Charibelle_Week18_Reflection.pdf` | **PASS** | 2 pages |

### 5.3 How the page counts were taken

The counts above are the `/Count` values inside each printed file, read with `findstr`, not estimates:

```
== Guzman_Charibelle_PA2.pdf                        /Count 5
== Guzman_Charibelle_PA2_Evidence_Portfolio.pdf     /Count 4
== Guzman_Charibelle_PA2_Originality_Declaration.pdf /Count 3
== Guzman_Charibelle_PA2_Presentation.pdf           /Count 8, /Count 6, /Count 14   (page tree: two nodes of 8 and 6, root 14)
== Guzman_Charibelle_PA2_Presentation_Speaker_Notes.pdf /Count 2
== Guzman_Charibelle_PA2_Technical_Report.pdf       /Count 5
== Guzman_Charibelle_PA2_User_Guide.pdf             /Count 3
== Guzman_Charibelle_Week18_Reflection.pdf          /Count 2
```

The deck's geometry was checked in the same way rather than assumed from the CSS: all fourteen pages carry
`/MediaBox [0 0 960 540]`, which is 16:9. Every other document carries `/MediaBox [0 0 594.96 841.92]`, A4 portrait,
five pages of it for the PA2 document and so on.

### 5.4 What is still open, and it is not a row that can be closed by writing

Three rows stay as they were on the first day of the week: the installable build (BLOCKED on hardware and on
signing), the originality declaration (UNSIGNED, a hand signature), and the defense (NOT SAT, a supervised viva).
Nothing in this package moves them, and no later edit to this file will.

## 6. Independent play-mode verification — 2026-11-26 09:18 UTC

Everything in sections 1–3 was produced by harnesses written for this week, so a second pair of eyes was put on
the same flow: an **automation agent drove the running game through its own UI**, clicking the real buttons and
reading the real components, with only UI clicks — no direct game-state writes, no reflection, no seeding.

| Step | What was read back |
|---|---|
| Title | `StartPanel` alone; `HUDPanel`, `PausePanel`, `ProfilePanel`, `WinPanel`, `HowToPanel`, `CreditsPanel` all false; `VersionLabel` = `1.0.0-rc1 · Editor`; buttons HOW TO PLAY / START / CREDITS / PROFILE / EXIT |
| HOW TO PLAY | `HowToPanel` the only active panel, `StartPanel` false, body 1,122 characters at 19 px; BACK returned the title |
| CREDITS | `CreditsPanel` the only active panel; `BuildText` = `Build 1.0.0-rc1 · Editor · com.defaultcompany.dama`; body 903 characters; BACK returned the title |
| PROFILE | `ProfilePanel` the only active panel; `ResetLabel` still `RESET PROFILE` (never armed); BACK returned the title |
| START | state `Playing`, 24 pieces under `DamaBoard/Pieces`, `Turn: RED`, `Red: 12   Blue: 12` |
| Pause / resume | `PausePanel` with `Time.timeScale` 0 and the same side to move; after RESUME, `timeScale` 1 and still `Turn: RED` |
| MENU from the pause panel | title screen, state `Ready`, `timeScale` 1, HUD hidden |

No console error or exception was logged in either episode. The agent's first attempt stalled on its own harness
(a one-frame `ShowState` transition was observed as "`StartPanel` and `HowToPanel` both true" and the step never
latched); the corrected attempt passed all seven steps in 4.8 s. That is reported rather than hidden because it is
the same class of fault as item 1 in section 3 — the observer was wrong, not the game.

The real store was left untouched, which was the constraint that mattered: `profile.json` read **257 bytes,
sha256 `bcc9ddef4b46406ec7b9750e1c49705b773d5972e08c37008bca696d3ae2d27d`, before and after**, because RESET was
never pressed and the match was never finished. `git status` was empty under `Week_13` … `Week_17` before and after
the run, and play mode was stopped at the end.

**This is a machine verification, not a person.** It is one more agent walking a flow it was told to walk; it says
nothing about a human player, a phone, or a thumb, exactly like the Week 14 machine pre-test. It is labelled as
such everywhere it is mentioned.
