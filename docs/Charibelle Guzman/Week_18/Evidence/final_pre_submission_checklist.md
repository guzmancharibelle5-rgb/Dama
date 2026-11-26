# Final Pre-Submission Checklist — PA2

**Student:** Charibelle Guzman · BSIT-4 · MOBILE DEV 002 Advanced Mobile Application Development, Module 5
**Week:** 18 · **Assessment:** PA2 (final project, demonstration and defense)
**Tree under submission:** `main` at the PA2 commit, version string `1.0.0-rc1` (version code `2`), application id `com.defaultcompany.dama`
**Written first, at the start of Week 18, before any other artefact** — so the week is measured against this list rather than a list written to fit what happened. The status column is re-read at the end of the week and every row is then moved to PASS or left BLOCKED with its reason.

Legend — **PASS** = done and provable by a file in this repository · **PENDING** = scheduled in this week, not yet produced · **BLOCKED** = cannot be produced in this environment, reason given and not faked.

---

## 1. Laboratory procedure (module, Week 18)

| # | Requirement | Status at start of week | Where it will be proved |
|---|---|---|---|
| 1 | Run the final pre-submission checklist: repository, build, documentation, evidence, credits, licences and disclosure records complete | **PASS** (this file) | `final_pre_submission_checklist.md`, `repo_state.txt`, `release_record.md` |
| 2 | Create the final tagged repository version and record tag and commit in the documentation | **PENDING** | tag `v1.0.0-pa2`; `repo_state.txt` |
| 3 | Submit an installable build, source/repository access, technical report, user guide, evidence portfolio, presentation and signed originality declaration | **PENDING / BLOCKED** | documents: `Week_18/*.pdf`. **Installable build: BLOCKED** — no APK exists and no keystore is held (§3.1). Repository access: `repo_state.txt` §4 |
| 4 | Install or launch the submitted build **from the same package given to the instructor** | **BLOCKED** | §3.1 — no package and no attached device |
| 5 | Deliver a concise presentation: theme, target player, core mechanics, architecture, services, security, usability findings, testing, optimisation, limitations | **PENDING** | `Guzman_Charibelle_PA2_Presentation.pdf` + speaker notes |
| 6 | Demonstrate onboarding, core loop, theme mechanics, settings, data/service features, error handling, completion/progression | **PENDING** | nine captures D1–D9 + `demo_rehearsal.txt` |
| 7 | Complete the individual technical defense, naming specific files, classes, commits, test records and profiling evidence | **PENDING (not sat)** | `defense_prep.md` (pointers, no scripted answers); `e3_record.md` (blank fields until the viva) |
| 8 | Submit a brief reflection: strongest result, most important limitation, next improvement | **PENDING** | `Guzman_Charibelle_Week18_Reflection.pdf` |

## 2. The module's named submission items

| Item | File | Status at start of week |
|---|---|---|
| Installable build | — | **BLOCKED** — no APK produced; see §3.1 |
| Main PA2 document | `Guzman_Charibelle_PA2.pdf` | **PENDING** |
| Technical report | `Guzman_Charibelle_PA2_Technical_Report.pdf` | **PENDING** |
| User guide | `Guzman_Charibelle_PA2_User_Guide.pdf` | **PENDING** |
| Evidence portfolio | `Guzman_Charibelle_PA2_Evidence_Portfolio.pdf` | **PENDING** |
| Signed originality declaration | `Guzman_Charibelle_PA2_Originality_Declaration.pdf` | **PENDING** — the signature line is left blank for a hand signature; an unsigned copy is not a signed declaration |
| Presentation | `Guzman_Charibelle_PA2_Presentation.pdf` + speaker notes | **PENDING** — a PDF deck is produced, not a `.pptx`; the format limit is stated in the document |
| Week 18 reflection | `Guzman_Charibelle_Week18_Reflection.pdf` | **PENDING** |

## 3. What is BLOCKED, and why it is not being faked

### 3.1 Installable build and handset launch — BLOCKED

Two independent blockers, either sufficient on its own, and both carried forward unchanged from Week 17:

- **No package exists.** No Android build was produced for this project in any week. The Week 18 scope decision was Editor-only.
- **No device answers `adb`.** `adb` is not on `PATH`, so the platform-tools bundled with the editor were used:
  `"...\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe" devices` → `List of devices attached`, nothing under it.
- **No keystore.** `androidUseCustomKeystore: 0`, `AndroidKeystoreName:` and `AndroidKeyaliasName:` are empty, so any package this project could produce would be **debug-signed** — not a release artifact an instructor should install as final.

Consequence: `TC-COMPAT-01` stays **BLOCKED** in the regression suite, exactly as in Weeks 15–17. Nothing in this package claims an installed build, a device frame rate, battery, thermals or memory. The full record, including what stands in its place, is `Week_17/Evidence/clean_install.md`; Week 18 repeats the check and adds the demonstration sequence that a phone run would have shown.

### 3.2 The oral defense — NOT SAT

Item 7 is a viva held under supervision. This package supplies the pointer sheet (`defense_prep.md`) and the blank record (`e3_record.md`); it cannot sit the defense. Those fields stay empty until they are filled in front of the instructor.

### 3.3 The originality declaration — UNSIGNED IN THIS COPY

The declaration is produced complete except for the signature line. Printing a signature into a PDF from this toolchain would be a fabricated signature; the line is therefore left blank and the document says so.

## 4. Common mistakes — self-audit against the module's list

| Listed mistake | At risk? | What prevents it |
|---|---|---|
| Demonstrating a different build from the submission | **No build is submitted at all** | The demonstration and the documents name the same tree and the same version string `1.0.0-rc1`; §3.1 says plainly that no binary exists, so there is no build to mismatch |
| Reading an AI-generated defense answer | **Addressed** | `defense_prep.md` holds questions with file, class, commit and test-log pointers and **no answers**; `e3_record.md` is blank |
| Being unable to identify important files and commits | **Addressed** | `defense_prep.md` names the actual paths; `repo_state.txt` carries the raw log and tags |
| Submitting unverifiable or fabricated evidence | **Addressed** | Every number in `Week_18/Evidence/` is copied from a harness log or a command output; unmeasurable rows stay `NOT MEASURABLE` and blocked rows stay BLOCKED |

## 5. Final confirmation

This table is re-read after the documents, PDFs, deck, tag and commits exist; the PENDING rows are then moved to PASS or left BLOCKED. The final status of every row is recorded at the end of `Week_18/Evidence/final_results.md`, in its section 5 — see below.

## 6. Re-read at the end of the week

The two status columns above are headed **"status at start of week"** and are left exactly as they were written,
because that is the whole point of writing this file first: it is the record of what was expected, not a table
edited until it matched what happened.

The re-read happened on 2026-11-26, after the documents, the eight PDFs (page-counted: 5, 5, 3, 4, 3, 14, 2 and 2
pages), the nine captures, the tag `v1.0.0-pa2` and the push. Every row's final status is in
`Week_18/Evidence/final_results.md` section 5. Summary of that re-read:

- **PASS** — procedure rows 1, 2, 3 (documents half), 5, 6 and 8; named items: PA2, technical report, user guide, evidence portfolio, presentation, reflection.
- **BLOCKED** — procedure row 4, and the installable build: no package exists and no device answers `adb` (§3.1).
- **NOT SAT** — procedure row 7: the defense. `e3_record.md` is blank by design (§3.2).
- **UNSIGNED** — the originality declaration: produced complete except the signature line (§3.3).

No row was moved to PASS because it "probably would have" — the blocked and unsigned rows stay exactly as they
started, with their reasons.
