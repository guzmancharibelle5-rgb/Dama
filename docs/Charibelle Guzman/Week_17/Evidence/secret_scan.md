# Week 17 - repository hygiene scan

**Question this file answers:** does the repository this release comes from hand over a credential - a key, a
token, a password, a keystore - that should not be in version control?

**Answer: no.** The scan below found eight matches for credential-shaped strings and file names, and every one
of them is a framework identifier, an empty settings field, or a document *about* secret handling. The raw,
unedited tool output is in `secret_scan_raw.txt` next to this file.

**Student:** Charibelle Guzman · BSIT-4 · MOBILE DEV 002, Module 5, Week 17
**Scanned:** 2026-11-26, working tree at `4cce8c0` plus the Week 17 change set

---

## 1. What was scanned and how

| Step | Command | Purpose |
|---|---|---|
| 1 | `git grep -n -I -i -e password -e passwd -e secret -e token -e api_key -e apikey -e private_key -e "BEGIN RSA PRIVATE" -- . ":(exclude)docs/Zedrich Abrera"` | every tracked text file, for values that look like credentials |
| 2 | `git ls-files \| findstr /I .keystore .jks .env id_rsa secret` | file names that suggest key stores or environment files |
| 3 | `findstr /I Keystore keyalias ProjectSettings\ProjectSettings.asset` | the signing configuration Unity actually ships |
| 4 | `git status --short` | what is modified or untracked, so nothing stray is committed by accident |

Tracked files only. `docs/Zedrich Abrera/` is excluded because it is not part of this submission and is not
committed; `Library/`, `Temp/` and build outputs are not tracked.

## 2. Every match, and why it is not a secret

| # | Where | What matched | Verdict |
|---|---|---|---|
| 1 | `ProjectSettings/Packages/com.unity.learn.iet-framework/Settings.json:5` | `PublicKeyToken=b77a5c561934e089` | the **public** key token of a .NET assembly identity. Public by definition; it identifies `mscorlib`, it does not authenticate anything |
| 2 | same file, line 10 | the same token | same |
| 3 | `ProjectSettings/ProjectSettings.asset:518` | `ps4NPTitleSecret:` - **empty value** | a placeholder field Unity writes for PlayStation builds. Empty, and this is an Android project |
| 4 | `ProjectSettings/ProjectSettings.asset:671` | `metroCertificatePassword:` - **empty value** | a placeholder field for Windows Store packaging. Empty |
| 5 | `docs/Charibelle Guzman/README.md:252,326` | the words "secrets", "credential scan" | prose pointing at Week 13's scan document |
| 6 | `docs/Charibelle Guzman/Week_13/Evidence/secrets_and_gitignore.txt` (several lines) | the words "password", "secret", "token" | the Week 13 document **about** secret handling; it lists the patterns a scan should look for |
| 7 | `docs/Charibelle Guzman/Week_13/others/Guzman_Charibelle_SA5.md` / `.html` / `PT5.md` / `.html` | "password", "secret", "API key" | the same, in the Week 13 analysis and its evidence index |
| 8 | `docs/Charibelle Guzman/Week_13/Evidence/build_and_commit.txt:90` | `.gitignore   build outputs and secrets` | a line of a `git status` transcript |

The name scan (step 2) returned exactly one path - `Week_13/Evidence/secrets_and_gitignore.txt` - which is a
document, not a keystore. No `.keystore`, `.jks`, `.env`, `id_rsa` or service-account file is tracked.

## 3. The signing configuration, stated plainly

```
AndroidKeystoreName:
AndroidKeyaliasName:
androidUseCustomKeystore: 0
```

The project has **no keystore and no alias**, so any APK it produces is debug-signed. That is the correct state
for this week - a debug-signed package cannot be distributed, and a release keystore must never be committed.
It is also one of the two reasons the install step is **BLOCKED**: a store-ready build would need a key that
lives outside the repository.

The only key material anywhere in the project is the profile signature salt, and it is **generated at runtime**
into `PlayerPrefs` by `PlayerProfile`. It is never written to a file that is committed, and it is not a shared
secret - each install generates its own.

## 4. What the `git status` output also shows

`secret_scan_raw.txt` section 3 lists what is modified or untracked. Two things there matter for the commit:

- **Nothing under `docs/Charibelle Guzman/Week_14`, `Week_15` or `Week_16` is modified.** Those folders are
  committed records of earlier weeks and this week's harnesses must not rewrite them.
- `Packages/manifest.json`, `Packages/packages-lock.json`, `Packages/Aura/` and `Assets/Plugins/NuGet/*` are
  modified or untracked. **None of them is staged.** They are the editor tooling this project runs on, not part
  of the game, and committing them would add third-party binaries to the submission and change the dependency
  set a reviewer would resolve.

The Week 17 commits stage explicit paths - the scene, the scripts, the project settings, and the Week 17
documents - never `git add -A`.

## 5. What this scan cannot do

- It is a **pattern** scan. A secret written without any of the words above - a bare hex string in a script, for
  example - would not be caught by it.
- It reads the **working tree**, not the full history. A credential committed and then removed in an earlier
  week would not appear here. The project has a single local branch with no external collaborator, so the
  exposure of a rewritten history is small, but the limitation is real.
- It says nothing about the machine the project sits on: `PlayerPrefs`, the editor preferences and the profile
  file live outside the repository and are not part of the submission.
