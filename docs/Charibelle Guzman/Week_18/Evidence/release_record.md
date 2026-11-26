# Release Record — PA2 submission

**Student:** Charibelle Guzman · BSIT-4 · MOBILE DEV 002, Module 5, Week 18
**Recorded:** 2026-11-26
**What is being submitted:** the source tree at tag `v1.0.0-pa2` — **and no Android package**.

---

## 1. What the submission is

| Item | Value |
|---|---|
| Project | Dama — the Philippine form of draughts, two players on one phone |
| Engine | Unity 6000.6.0f1, Universal Render Pipeline 17.6.0 (2D Renderer) |
| Version string the app reports | **`1.0.0-rc1`** (`bundleVersion`), Android version code **2** |
| Application id | `com.defaultcompany.dama` |
| `productName` / `companyName` | `dama` / `DefaultCompany` — **unchanged since Week 13** |
| Build settings | Android, IL2CPP, ARM64 only, minimum API 26, target API automatic, **no custom keystore** |
| Scene | `Assets/Scenes/DamaGame.unity`, build index 0; `SampleScene` disabled |
| Submitted binary | **none** — see §3 |
| Submission tag | `v1.0.0-pa2` on the PA2 commit (§4 of `repo_state.txt`) |

## 2. Why the version string did not move to `1.0.0-pa2`

Version strings in this project are load-bearing. On Android the player's save file lives under
`Application.persistentDataPath`, which is built from `companyName` and `productName`:

```
C:/Users/Zedrich/AppData/LocalLow/DefaultCompany/dama/Dama/profile.json
```

and both on-screen version strings (`StartPanel/VersionLabel`, `CreditsPanel/BuildText`) are derived from
`Application.version` by `ReleaseInfo.Caption()` and `ReleaseInfo.BuildLine()` — never typed into a scene label.
Moving the identity around to mark the submission would either present every player with an empty profile
(the failure the Week 17 build record warns about: a store path built from `productName` and `companyName`)
or print a string the code did not generate.

So the submission is marked where marking costs nothing: **a git tag on the submission commit**. The app keeps
reporting `1.0.0-rc1`, and the tag is what identifies the tree that was submitted. `v1.0.0-rc1` was never created
as a tag in this repository — Week 17 tagged its frozen baseline `week16-final` instead — so `v1.0.0-pa2` is the
first release tag this project carries.

## 3. What is not in this submission — BLOCKED, with the reason

**No APK, no AAB, no Windows package.** Three separate reasons, each independently sufficient:

1. **Scope decision.** Week 17 and Week 18 were scoped to Editor evidence. No Android package has been produced for this project in any week.
2. **No signing key.** `androidUseCustomKeystore: 0` and the keystore, key alias and passwords are empty. Any package this machine produced would be debug-signed; calling that a final installable build would misrepresent it.
3. **No device.** `adb devices` (Unity's bundled platform-tools) returns `List of devices attached` with no serial, so nothing could be installed or launched even if a package existed.

Consequence, stated once and carried everywhere: every number and every screenshot in this submission comes from
the **Editor in play mode**, at a 1151x584 game view on a Windows machine (AMD Ryzen 5 5600G, 16,276 MB,
GeForce GTX 1070). Nothing here is evidence about a phone.

## 4. What the submission still proves

| Claim | Evidence |
|---|---|
| The tree behaves as tested | `Week_18/Evidence/final_results.md` — release checks, regression at `Week_18`, accessibility audit, all re-run on this tree |
| Every screen a demonstration needs exists and is legible | `Week_18/Evidence/D1…D9.png`, nine distinct SHA-256 values, `demo_rehearsal.txt` |
| The identity is derived, not typed | `Week_17/Evidence/test_logs_release/` — `TC-REL-01`, `TC-REL-02` compare the on-screen strings to `Application.version` |
| The rules hold | `Week_18/Evidence/test_logs_regression/` — 27 passed, 0 failed, 1 blocked (`TC-COMPAT-01`, hardware) |
| Accessibility floors are met in canvas pixels | `Week_18/Evidence/a11y_week18_after.log` |
| Security work is real and its limit is known | `Week_13/Evidence/pt5_checks.log`; the save is tamper-evident, not encrypted |

## 5. What would close the open rows

1. Create a keystore **outside** the repository, set `androidUseCustomKeystore: 1` and the four keystore fields.
2. Build: Android, IL2CPP, ARM64, minimum API 26, **release** (Development Build off), then hash the output and record size + SHA-256 here.
3. `adb install -r <file>.apk` on a device running Android 8.0 or newer, launch it, and walk D1–D9 on the handset.
4. Re-run `TC-COMPAT-01` with device model, Android version and build number, and attach `adb logcat` for the walkthrough.

Until steps 1–4 happen, the installable-build row in `final_pre_submission_checklist.md` stays **BLOCKED** and
`TC-COMPAT-01` stays blocked with it.
