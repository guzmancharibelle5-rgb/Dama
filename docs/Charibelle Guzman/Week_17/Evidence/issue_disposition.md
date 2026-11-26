# Week 17 — issue disposition

**Student:** Charibelle Guzman · BSIT-4 · MOBILE DEV 002, Module 5, Week 17
**Tree under test:** `week16-final` (`337b436`) plus the Week 17 working tree
**Severity scale:** S1 blocker · S2 major · S3 minor · S4 cosmetic (unchanged from PT6/AS3)

Every row below was found after the freeze. A row is closed only when the artefact named in
**Resolution** exists and the check named in **Evidence** passes against it. A row that cannot be
closed is carried as **BLOCKED** with its reason, or deferred to the Week 18 planning task — it is
never deleted to make the count look better.

## 1. Completeness gaps opened by the module's Week 17 checklist

These are not defects in the sense of Week 15's log: the game behaved as designed, and the design
was missing something the module requires. They are recorded so that a reviewer can see the gap was
found by the checklist and not discovered late.

| Id | Sev | Gap | Requirement | Status | Resolution | Evidence |
|---|---|---|---|---|---|---|
| DEF-17-01 | S2 | No credits screen anywhere in the scene or the code — no author, no course, no engine, no licence, no AI-assistance statement | Week 17 procedure step 2: "settings, credits, and exit or restart" | **Closed** | `StartPanel/CreditsButton` opens a new `CreditsPanel` carrying the author, course, engine, licence and AI-assistance statements, plus `BuildText` naming the build; `BackButton` closes it | `TC-REL-02`, `TC-REL-04` (`CREDITS[1 call(s): DamaBoard.ShowCredits]`, `CREDITS-BACK[1 call(s): DamaBoard.CloseCredits]`) |
| DEF-17-02 | S2 | No exit control. `Application.Quit` exists nowhere in the game, so the only way out of the title screen is the Android task switcher | Week 17 procedure step 2: "exit or restart behaviour" | **Closed** | `StartPanel/ExitButton` calls `DamaBoard.QuitGame()`, which logs the build line and then quits (`EditorApplication.isPlaying = false` under the editor, `Application.Quit()` on a build) | `TC-REL-04` (`EXIT[1 call(s): DamaBoard.QuitGame]`) |
| DEF-17-03 | S3 | The running game never states which build it is. `bundleVersion` is `1.0` and no surface prints it, so two builds are indistinguishable to a reviewer holding both | Week 17 procedure step 5: "a unique version number and build identifier" | **Closed** | `bundleVersion` set to `1.0.0-rc1` (version code 2); `ReleaseInfo` derives every string from `Application.*`; `VersionLabel` prints `1.0.0-rc1 · Editor` on the title and `CreditsPanel` prints the full build line | `TC-REL-01`, `TC-REL-02` |
| DEF-17-04 | S3 | The title screen states the three rules in 156 characters and there is no further instruction surface; the pause menu explains nothing | Week 17 procedure step 2: "clear instructions or tutorial" | **Closed** | `StartPanel/HowToButton` opens `HowToPanel`, whose body is 1122 characters fitted to 19 px in a 440 px box (measured 399.3 px); `BackButton` returns to the title | `TC-REL-03` (`bodyChars=1122 bodyFont=19`) |

## 2. Environment and infrastructure rows

| Id | Sev | Finding | Status | Note |
|---|---|---|---|---|
| ENV-17-01 | S2 | No Android release candidate is built this week, so the module's "install and run on the target device from a clean state" is **BLOCKED** | BLOCKED | No handset is attached (`adb` reports no serial) and no APK is produced by decision; see `clean_install.md` for what stands in its place and what it does not prove |
| ENV-17-02 | S3 | `TC-COMPAT-01` remains blocked in the regression suite, exactly as in Weeks 15 and 16 | BLOCKED | Device compatibility is a hardware question; asserting it would make the suite lie |

## 3. Rows found while closing the gaps

These were not on the checklist. They surfaced when the gates were run against the rebuilt scene, and both are
recorded with the numbers they produced.

| Id | Sev | Finding | Status | Resolution | Evidence |
|---|---|---|---|---|---|
| DEF-17-05 | S2 | `TC-A11Y-02` reported three controls overlapping **themselves** on `StartPanel`. Cause: `Week17ReleasePanels.BuildReleasePanels()` called `DestroyIfExists` for `VersionLabel`, `HowToPanel` and `CreditsPanel` but not for the three new title buttons, so its second run added a second `HowToButton`/`CreditsButton`/`ExitButton` at the same anchored position — 11 children, indices 4–6 and 8–10, each pair coincident. The regression run read **26 passed, 1 failed, 1 blocked** | **Closed** | `DestroyIfExists` now loops until no child of that name remains, and the builder calls it for all three buttons before creating them; builder re-run (8 children, one of each), scene re-saved, all three gates re-run | `test_logs_regression/` after the fix: **27 passed, 0 failed, 1 blocked**, `TC-A11Y-02 0 overlapping pairs`; audit fell 163 → 148 because the 15 duplicate-derived checks left with the duplicates |
| DEF-17-06 | S3 | `TC-REL-04` read `1 call(s): null.?` on its first run, so the first Week 17 release run reported **4 passed, 1 failed**. The check inspected `InvokableCall.Target`/`.Method`, which uGUI's serialized listeners do not populate — the handler lives in the private `m_Delegate` field | **Closed** | The check now reads the delegate's `Target` and `Method.Name`, which reports `DamaBoard.ShowHowTo` and the other four; the listeners were correct all along. `FindFirstObjectByType` → `FindAnyObjectByType` in the same pass | `test_logs_release/` after the fix: **5 passed, 0 failed**, all five listeners named |

`DEF-17-06` is a row against the **check**, not the game, and it is kept here rather than dropped so that the
first `4 / 1` release result is not silently contradicted by the `5 / 0` that replaced it.

The two environment rows above (`ENV-17-01`, `ENV-17-02`) stay **BLOCKED** at the end of the week. Neither an
APK nor a handset exists, and no document in this folder claims otherwise.
