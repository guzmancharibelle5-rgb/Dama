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
   temporary folder and adds nothing. The three extra rows are **left in place and disclosed here**:
   removing them would mean hand-editing the player's save, and `PlayerProfile` is the only class
   permitted to write that file.

## 4. What these results do not prove

- **Nothing about an installed build.** No APK exists, so the demonstration is an Editor demonstration.
- **Nothing about a phone.** No frame rate, battery, thermal or memory behaviour is claimed anywhere in
  this package; the Week 16 numbers are Editor numbers and stay recorded as such.
- **Nothing about a real player.** The demonstration is the harness driving the game through its own
  entry points; no human playtest session has been run, in this week or any other.
- **The defense was not sat.** `e3_record.md` is blank by design and `defense_prep.md` holds pointers,
  not answers.
