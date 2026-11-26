# Week 17 - clean install record

**Status: BLOCKED.** The module's "install and run on the target device from a clean state" did not happen this
week, and nothing in this package claims it did.

**Student:** Charibelle Guzman · BSIT-4 · MOBILE DEV 002, Module 5, Week 17
**Release under test:** `1.0.0-rc1`, version code `2`, `com.defaultcompany.dama`
**Recorded:** 2026-11-26

---

## 1. What was attempted

| Step | What was done | What came back |
|---|---|---|
| 1. Find a package to install | looked for an APK or AAB produced for this release | **none exists.** No Android build was produced this week; the week was scoped to Editor-only evidence |
| 2. Check for an attached device | `adb devices` using the platform-tools bundled with Unity 6000.6.0f1 (`...\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe`; `adb` is not on `PATH` in this shell) | `List of devices attached` - **empty**, no serial listed |
| 3. Check the signing configuration | Player Settings → Publishing | `androidUseCustomKeystore: 0`, `AndroidKeystoreName:` empty, `AndroidKeyaliasName:` empty - **debug signing only** |

Two independent blockers, either of which is enough on its own: there is no package, and there is no device.
A release build would additionally need a keystore that this project deliberately does not carry in the
repository (see `secret_scan.md`).

## 2. What stands in its place, and what it is worth

| In place of the install | What it does prove | What it does **not** prove |
|---|---|---|
| Editor play-mode run of `Assets/Scenes/DamaGame.unity` | the scene boots, the panels open and close, the controls are bound, the match runs | anything about an installed APK: the package name, the activity launch, permissions at runtime, the Android lifecycle |
| `TC-REL-01…05` (5 passed) | the version caption and build line are derived from the application and agree; both new panels open, close and hide | nothing about touch response on glass |
| `TC-REL-05` | the new panels cannot leak into a running match | nothing about how Android hands the app back after a call or a home press |
| `A11yAudit` (148 passed, 4 not measurable) | every measurable control clears the touch, contrast and type-size floors **in canvas pixels** | physical size on a particular handset, or how a real thumb lands on a 96 px target |
| `E1`-`E6` captures | the six screens exist and are legible at 1151x584 | frame pacing, battery, thermal behaviour, memory headroom |
| Week 15 regression (27 / 0 / 1) | the frozen tree still behaves as it did in Weeks 13-16 | that the same is true after an Android build strips and compiles the scripts |

The honest summary: an Editor run and an installed build share the same scene and the same scripts, so the
Editor evidence is worth something - but it is evidence about behaviour, not about a phone.

## 3. What would close this row

1. Build an APK: IL2CPP, ARM64, minimum SDK 26, and a keystore held **outside** the repository.
2. `adb install -r <file>.apk` on a physical device running Android 8.0 or newer.
3. Launch it and walk the six screens: title → HOW TO PLAY → BACK → CREDITS → BACK → START → a full match →
   pause → end → restart → EXIT.
4. Re-run `TC-COMPAT-01` with the device model, Android version and build number recorded, and attach the
   `adb logcat` output of the walkthrough.

Until steps 1-4 are done, `TC-COMPAT-01` stays **BLOCKED** and this file stays as it is. It is not a
placeholder to be filled in with a pass later; it is the record of a step that could not be taken in this
environment.
