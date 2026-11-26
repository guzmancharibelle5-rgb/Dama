# Week 18 — secret and repository scan

**Student:** Charibelle Guzman · BSIT-4 · MOBILE DEV 002, Module 5, Week 18 (PA2)
**Recorded:** 2026-11-26
**Question this file answers:** does the submitted tree carry a signing key, a credential, a build
artifact or a stray working file that should not be handed to an instructor?

---

## 1. Key and credential files in the index

```
git ls-files | findstr /I /R "\.keystore$ \.jks$ \.apk$ \.aab$ \.env$ \.pfx$"
-> (no lines)
```

**No keystore, key, `.env`, APK, AAB or certificate is tracked.** `.gitignore` covers `*.apk`, `*.aab`,
`*.unitypackage`, `*.keystore` and `*.jks`, so an accidental build output or signing key cannot be
committed by habit.

## 2. Signing configuration

| Field | Value | Meaning |
|---|---|---|
| `androidUseCustomKeystore` | `0` | no custom keystore in use |
| `AndroidKeystoreName` | *(empty)* | no key file referenced |
| `AndroidKeyaliasName` | *(empty)* | no alias referenced |
| `AndroidKeystorePass` / `AndroidKeyaliasPass` | *(empty)* | no password stored |

This project has never carried a signing key. It also has never produced an APK, so there is no
signed artifact to leak — the installable-build row in `final_pre_submission_checklist.md` is BLOCKED
for exactly that reason and is not dressed up as a pass.

## 3. Working-tree check for this week

```
git status --porcelain -- "docs/Charibelle Guzman"
?? "docs/Charibelle Guzman/Week_18/"
```

The only change under the submission folder is the new `Week_18/` directory. Nothing under `Week_13`
to `Week_17` is modified: those are committed records and every Week 18 harness writes elsewhere.

Deliberately **not** staged, and unchanged from Week 17's position:

| Path | Why it stays out |
|---|---|
| `Assets/Plugins/NuGet/*` | editor tooling binaries that are rebuilt by the toolchain, not submission content |
| `Packages/Aura/*`, `Packages/manifest.json`, `Packages/packages-lock.json` | assistant tooling and its dependency resolution |
| `docs/Zedrich Abrera/` | another student's submission, used privately for shape and never cited or committed |
| `docs/MAD2-…docx` | the course module, which is not the student's work to redistribute |

## 4. Files this week wrote outside the repository

The demonstration banks three matches into a temporary profile store so that it can show a record and
then erase it without touching the real one:

```
%TEMP%\DamaWeek18Demo\
```

That folder is outside the project and outside version control by construction. The harness deletes and
recreates it at the start of every run, so it holds nothing but the run's own three matches.

## 5. What is not claimed

There is no API key, token or credential scan of the entire disk here — this is a scan of what the
repository carries. The Week 13 scan (`Week_13/Evidence/secrets_and_gitignore.txt`) recorded eight
benign matches (public key-token names, empty keystore password fields, prose); nothing in Week 18
changed any of those files, so that result stands and is not restated as new evidence.
