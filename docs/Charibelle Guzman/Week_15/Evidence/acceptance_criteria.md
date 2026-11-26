# Acceptance criteria - Dama AS3 Milestone (Week 15)

**Written before any test was executed.** Each condition below is phrased so that a reader can check
it against a named artefact rather than against an opinion. The four checkpoint conditions come from
the module's Week 15 completion checkpoint; the last two are this project's own, added because a
board game can be "not crashing" and still be unplayable on the screen it ships on.

## The four checkpoint conditions

| # | Condition | How it is checked | Where the result is recorded |
|---|---|---|---|
| CC-1 | **No unresolved defect causes data loss** | Corrupt-save rejection (TC-DATA-04), reset then reload (TC-DATA-03), a match banked during a session that still exists after reload (TC-DATA-02) | `test_logs_*/case_log.tsv`, `defect_log.md` |
| CC-2 | **No unresolved defect prevents completion of the core gameplay loop** | TC-FLOW-01 to TC-RULE-06: launch, start, select, move, capture, chain, crown, win by elimination and by immobilisation, then reach the end screen | same |
| CC-3 | **No unresolved defect exposes another player's information** | The game has no accounts and no network: one local signed file per install. Verified by absence of networking code and by the save-file check in TC-DATA-05 | `environment.md`, `traceability_matrix.md` |
| CC-4 | **No unresolved defect repeatedly crashes the application** | Full run with zero unhandled exceptions in the console transcript; a 100-rapid-tap boundary case (TC-BOUND-01) | `test_logs_*/console.log`, TC-BOUND-01 |

## This project's own conditions

| # | Condition | How it is checked |
|---|---|---|
| AC-5 | **A first-time player can reach and use every control on every screen** - no control below the 96 x 96 canvas px touch floor, no control overlapping another, no label clipped | `a11y_week15_after.log`, TC-A11Y-01..03 |
| AC-6 | **Nothing on screen is text a player cannot read** - every label meets its contrast threshold, captions are at least 20 px and other labels at least 18 px | `a11y_week15_after.log` |

## Ready-for-optimization test

The module asks the acceptance summary to say whether the build is ready for Week 16's optimization
work. The answer is **ready** only if all six conditions above hold **and** the defect log contains no
open S1 or S2 defect. Anything else is recorded as **not ready, with the blocking defect named** -
the same rule Week 14 used when it refused to call an unrun playtest a pass.

## What acceptance does not mean

Passing acceptance means the game does what these requirements say on the configuration it was
tested on. It does not mean: playable by someone who has never seen dama (that needed the Week 14
human sessions, which have not been run), proven on a handset (no APK and no device were available
this week), or accessible to a screen-reader user (uGUI exposes no accessibility tree). Those limits
are stated in the milestone document's "what this milestone does not prove" section rather than
implied by a green table.
