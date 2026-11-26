# PA2 — Presentation

**Charibelle Guzman · BSIT-4 · MOBILE DEV 002, Module 5 · Week 18**

A deck of fourteen slides. The speaker notes for each slide are a separate document
(`Guzman_Charibelle_PA2_Presentation_Speaker_Notes`).

---

## Slide 1 — Dama: the Philippine board game, on one phone

- Charibelle Guzman, BSIT-4
- MOBILE DEV 002, Module 5 — Week 18, PA2
- Theme: Philippine Games and Sports
- Submission: `main` at the PA2 commit, tag `v1.0.0-pa2`

## Slide 2 — Why this game

- *Dama* is draughts as it is played here: 8x8, twelve men a side, two players sharing one board.
- It was played in schoolyards, on bottle caps and chalk squares — no equipment required.
- The theme is in the rules, not in the decoration: compulsory capture, chained capture, the flying Dama.
- The mobile question: can two people share one phone without the interface getting in the way?

## Slide 3 — The target player

- Students and young adults who played *dama*, *patintero* or *agawan base* as children.
- One mid-range Android phone, passed between two players.
- What that decided: a 96 canvas-pixel touch floor, a top-down 2D board readable across a table, and a
  local record because the two players share one device.

## Slide 4 — The core loop

- Title → a match → end screen → restart or menu, in three taps.
- Tap a man, tap a square; the HUD names the turn and both counts.
- The match ends when a side has no men or no legal move; the record is banked once.
- Pause offers RESUME, RESTART, SOUND and MENU — the last one added to fix a Week 15 defect.

## Slide 5 — The rules, as enforced

- **Capture is compulsory**: if a jump exists, quiet moves are never offered.
- **Capture chains**: a landing that can jump again keeps going.
- **The Dama flies**: any distance, any diagonal, forward or back.
- Rules are constraints on generation, not checks after the fact — that is why an illegal move cannot be made.

## Slide 6 — Onboarding, credits, exit

- HOW TO PLAY: 1,122 characters fitted to 19 px in a 440 px box.
- CREDITS: author, course, engine, licence, AI-assistance statement, and the build line.
- EXIT: quits; under the editor it leaves play mode. Bound, and proven by a test that names the handler.
- All three panels are state-driven, so none can leak into a running match.

## Slide 7 — Architecture

- `DamaBoard` owns the grid, the rules and the panel state machine.
- `PlayerProfile` is the only class that touches the save file.
- `ProfilePanel` asks the store; it never parses JSON.
- `ReleaseInfo` derives every version string from `Application.*` — nothing is typed into a label.
- One scene, no prefabs, UI built in code so the scene is reproducible.

## Slide 8 — Player data and security

- Save: a JSON envelope with schema, payload and an HMAC signature; written atomically through a temp file.
- A file that fails its signature is set aside, not trusted; every numeric field is clamped on load.
- **Limit, stated:** tamper-evident, not encrypted. Key and file share a device, and there is no server.
- The store path is built from `productName` and `companyName`, so neither may ever change.

## Slide 9 — What the demonstration shows

- Nine screens, one run, nine distinct SHA-256 values.
- D1 title, D2 how to play, D3 credits, D4 profile with a record, D5 after the two-press reset.
- D6 fresh match, D7 after a mandatory jump, D8 pause, D9 end screen.
- Error handling is the reset confirmation; completion is the end screen naming why the match ended.

## Slide 10 — Testing

- 27 regression cases: ten rule cases, five data cases, two boundary cases, flow and accessibility.
- 5 release cases compare the on-screen version strings back to `Application.version`.
- 148 accessibility checks over 18 buttons and 1 field; 4 rows are NOT MEASURABLE and stay unmeasured.
- `TC-COMPAT-01` is **BLOCKED** — no APK, no device — and is never counted as a pass.

## Slide 11 — Profiling and optimisation

- Measured first: 2.062 ms mean frame, 6,975.9 B per legality scan, 5,596 B per restart.
- Change 1: allocation-free move generation → per-scan allocation below the method's resolution.
- Change 2: piece pooling → restart allocation to ~1.7 KB or less, 24 pooled objects.
- The frame claim is **not** made: the frame was already inside budget, and the mean moved with the session.

## Slide 12 — Usability and accessibility findings

- The audit is a floor, not a review: it can show thresholds are met, not that a player understood anything.
- Three fills pass the 3:1 line at 3.16–3.19:1, and two labels at 4.7:1 — thin margins, said plainly.
- Missing: dynamic type (WCAG 2.2 SC 1.4.4) and any screen-reader path.
- No human playtest: the Week 14 kit exists; zero volunteers played.

## Slide 13 — Limitations and honest gaps

- **No APK, no handset**: the module's installable build does not exist in this submission; it is BLOCKED.
- Every performance number is an Editor number: no phone frame rate, battery or thermals are claimed.
- Haptics unproven; `EXIT` bound but never fired on Android; the caption reads `· Editor` because it is.
- The defense is not in this package, and the declaration is unsigned in this copy.

## Slide 14 — What I would do next

- Build the APK with a keystore held outside the repository, install it on a handset, and re-run the
  demonstration on the device — the one gap this submission cannot close.
- Run the Week 14 playtest kit with real volunteers and report what they did, not what I predicted.
- Add a dynamic-type path so the game follows the phone's font scale.
- Keep the one rule that made this work: measure before claiming.
