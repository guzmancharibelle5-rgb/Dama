# PT5 - Secure Player Profile, Integrity-Checked Save, and Local Best-Times Board

**Game:** Dama
**Student:** Charibelle Guzman
**Section:** BSIT-4
**Course:** MOBILE DEV 002 Advanced Mobile Application Development, Module 5
**Theme:** Philippine Games and Sports
**Week:** 13 - Secure Player Data and Responsible Development
**Build:** Dama MVP (Android target: ARM64 / IL2CPP, `com.defaultcompany.dama`, minimum API 26)
**Engine:** Unity 6000.6.0f1, Universal Render Pipeline 17.6.0 (2D Renderer), C#
**Repository:** local Git repository, branch `main`, code commit `b6b477b`
**Component under test:** `Assets/Scripts/Profile/PlayerProfile.cs`, `Assets/Scripts/Profile/ProfilePanel.cs`, `Assets/Scripts/DamaBoard.cs`

---

## 1. What was built

Dama now remembers the player. A profile panel reachable from the start screen holds a display
name, the side the player takes (RED or BLUE), a win/loss/draw record with a win streak, and a
local best-times board of the five fastest victories, each labelled with the match it came
from. The record is stored in a signed JSON file in the app's own sandbox and survives closing
and reopening the game.

| Piece | File | Responsibility |
|---|---|---|
| Profile store | `Assets/Scripts/Profile/PlayerProfile.cs` | The only thing that reads, signs, writes, clamps or deletes the save. Static, so the record survives the scene state changes that Restart performs |
| Panel controller | `Assets/Scripts/Profile/ProfilePanel.cs` | Drives `GameCanvas/ProfilePanel`: name field, record lines, best-times board, side toggle, SAVE / RESET / BACK, status line. Never touches the file and never parses JSON |
| Game state | `Assets/Scripts/DamaBoard.cs` | New `GameState.Profile`, `ShowProfile()` / `CloseProfile()`, the panel lookup in `CacheUI()`, panel visibility in `ShowState()`, and one guarded `BankMatch()` call from `DeclareWinner` |
| Scene builder | `Assets/Scripts/Editor/ProfileSceneBuilder.cs` | Menu `Tools/Dama/Build Profile UI`: builds the panel and the `StartPanel/ProfileButton`, wires every listener, saves the scene. Re-runnable |
| Test harness | `Assets/Scripts/Editor/Week13Checks.cs` | `RunDataChecks()` writes the L1-L15 log against a scratch folder; `BeginShots()` captures the four play-mode screenshots |

### Public surface of `PlayerProfile`

| Member | Purpose |
|---|---|
| `PlayerName`, `PlayerSide`, `Wins`, `Losses`, `Draws`, `MatchesPlayed` | The record as the panel reads it. `MatchesPlayed` is derived, so it can never disagree with its parts |
| `CurrentStreak`, `BestStreak`, `LastPlayedUtc`, `LastMatchId`, `NextMatchId` | Streak and the identity of the last banked match |
| `Recent` | Up to 20 finished matches, newest first |
| `BestWins()` | The best-times board: victories sorted by `seconds` ascending, then by `matchId`, capped at 5 |
| `Load()`, `Save()`, `SetName()`, `SetSide()`, `RecordMatch(...)`, `ResetProfile()` | The state changes; every one returns a bool instead of throwing |
| `SanitizeName()`, `ComputeSignature()` | The two pure functions the harness proves directly |
| `SetDataDirectory()`, `UseDefaultDataDirectory()`, `ResetCacheForTests()`, `WriteRawForTests()`, `WriteSignedForTests()`, `WriteGarbageForTests()` | Test seams, so the failure cases can be proven without touching the player's real save |
| `DataDirectory`, `SavePath`, `RejectedPath`, `LastError`, `IsLoaded` | Inspection, for the panel's status line and for a bug report |

The panel's status line is part of the design, not decoration. On open it reads
*"Type a name, then SAVE. Nothing leaves this device."*

---

## 2. Where the player meets it

1. The start screen gains a **PROFILE** button under **START**.
2. Pressing it puts `DamaBoard` into `GameState.Profile`, which hides every other panel and
   shows `ProfilePanel`.
3. The player types a name (the store caps it at 16 characters and strips control characters),
   taps **SIDE: RED** to switch to BLUE if that is the side they play, and presses **SAVE**.
   The status line confirms.
4. Playing a match to an end banks exactly one entry: win, loss or draw **from the profile
   owner's side**, with the winning side, the match clock in seconds and the pieces each side
   had left.
5. Reopening PROFILE shows the updated record and, for a victory, a line on the best-times
   board.
6. **RESET PROFILE** is deliberately two presses within 4 seconds; the first press renames the
   button to `PRESS AGAIN` and says what will be lost. **BACK** returns to the start screen.

Each step is photographed in `Evidence/` (E1 to E4, listed in section 8).

---

## 3. The save file

### Location

| Platform | Path |
|---|---|
| Android | `/data/data/com.defaultcompany.dama/files/Dama/profile.json` (that is, `Application.persistentDataPath/Dama/profile.json`) |
| Windows editor | `C:/Users/<user>/AppData/LocalLow/DefaultCompany/dama/Dama/profile.json` |

The folder is the app's private sandbox. No other app on the phone can read it, and
uninstalling the game deletes it. Nothing is written anywhere else.

### Format

One UTF-8 JSON envelope: an `int schema` for future migrations, a `string sig` holding the
signature, and the `payload` the signature covers.

```json
{
    "schema": 1,
    "sig": "9c1f6a0d3b7e42f8a5d0c94e17b3a6f25d8c4e0b91a7f36c2d5e8b4a1f907c3de",
    "payload": {
        "schema": 1,
        "playerName": "Charibelle G.",
        "playerSide": 1,
        "wins": 2,
        "losses": 1,
        "draws": 0,
        "currentStreak": 0,
        "bestStreak": 2,
        "lastMatchId": 3,
        "lastPlayedUtc": "2026-11-26T03:12:10Z",
        "recent": [
            {
                "matchId": 3,
                "result": "LOSS",
                "winner": "BLUE",
                "seconds": 62.0,
                "redPieces": 1,
                "bluePieces": 4,
                "whenUtc": "2026-11-26T03:12:10Z"
            }
        ]
    }
}
```

The `sig` value above is illustrative - a real file's signature is the HMAC of that exact
payload, so it changes whenever any field changes.

| Field | Type | Rule enforced by `PlayerProfile` |
|---|---|---|
| `schema` | int | A file newer than `SchemaVersion` (1) is refused and quarantined rather than guessed at |
| `sig` | string | Lowercase hex HMAC-SHA256 over the canonical payload, compared as an ordinal string |
| `payload.playerName` | string | Trimmed, control characters stripped, runs of spaces collapsed, capped at 16; blank becomes `PLAYER` |
| `payload.playerSide` | int | Only 1 (RED) or 2 (BLUE) is accepted; anything else falls back to RED |
| `payload.wins` / `losses` / `draws` | int | Floored at 0 after the signature passes |
| `payload.currentStreak` | int | Floored at 0; a loss resets it, a draw keeps it |
| `payload.bestStreak` | int | Raised to at least `currentStreak`, so an edited save cannot claim a best streak below the current one |
| `payload.lastMatchId` | int | Floored at 0. `RecordMatch` refuses any id at or below it, which is what makes a double bank impossible |
| `payload.recent` | list | Nulls removed, oldest entries dropped past 20 |

---

## 4. Integrity design

Six decisions, in the order they protect the file.

**4.1 The save is signed, and the signature is over the canonical payload.**
On write, the payload is serialised with `JsonUtility.ToJson(state)` and that exact string is
signed. On read, the file is parsed and the payload is re-serialised with the same call before
the signature is recomputed. Signing the raw text of the file instead would have made the check
depend on whitespace and key order, so a cosmetic difference would have looked like tampering.
The canonical form is the contract.

**4.2 The key is per install and never shipped.**
`ComputeSignature` keys HMAC-SHA256 with 16 bytes generated by
`RandomNumberGenerator.Create()` on the first save and kept in `PlayerPrefs["profile.salt"]` as
base64. There is no constant in the source, so a compile of the game does not contain the key,
and two installs sign differently.

**4.3 The write is atomic.**
The JSON goes to `profile.json.tmp`, the old file is deleted, and the temp file is moved into
place. A process killed mid-write leaves a `.tmp` file behind, never a half-written
`profile.json` where a complete one used to be.

**4.4 A rejected file is quarantined, not destroyed.**
When parsing or the signature check fails, the file is moved to `profile.rejected.json` and a
fresh default profile starts. A player's only copy is never overwritten by a game that did not
understand it, and if this ever happens for real, the rejected file is the evidence.

**4.5 Numbers are clamped even when the signature passes.**
A correctly signed file is still a file, and a correctly signed file is exactly what an
attacker holding the salt produces. So after the signature passes, negatives are floored,
`bestStreak` is raised to at least `currentStreak`, the name is re-sanitised and the recent
list is cleaned. Conservative clamping means an edited save degrades into something plausible
instead of something impossible.

**4.6 There is exactly one gate, and it is a match id.**
`DamaBoard` fixes `currentMatchId = PlayerProfile.LastMatchId + 1` when a match starts.
`BankMatch` returns early when `currentMatchId <= PlayerProfile.LastMatchId`, and
`PlayerProfile.RecordMatch` independently refuses any id at or below `lastMatchId`. Two
independent guards for the same failure, because an inflated record is the failure mode that
actually matters to a player.

**Rejected alternative: encrypting the file.** There is no key the attacker does not have, so
encryption would add code, a decryption-failure path and the false impression of a guarantee,
without changing who can produce a valid file. It is not in the build, and section 5 says so
on the record.

---

## 5. What the signature does not do (the honest limit)

The save is **tamper-evident, not tamper-proof.**

- It is not encrypted. Anyone who can read the file can read the name, the side and the record.
- The salt lives on the same device as the file. A determined local attacker can read it and
  re-sign an edited save, and the game will accept that file.
- There is no server, no account and no online leaderboard, so there is nothing to check the
  record against. For an offline hot-seat board game that is the true ceiling.

This is recorded in the check log as **L15 KNOWN LIMIT** rather than passed off as a pass, and
it is repeated in the README under known limitations.

---

## 6. Security check results

Every row below was executed by `Assets/Scripts/Editor/Week13Checks.cs` inside the Unity
editor on **2026-11-26 03:12:10 UTC**, against a scratch folder in `%TEMP%`
(`C:\Users\Zedrich\AppData\Local\Temp\Dama_week13_scratch`), one clean sub-folder per check, so
the player's real save was never written to. The measured value in the right-hand column is
copied from `Evidence/pt5_checks.log`, not typed from memory.

| ID | What is being proven | Result | Measured value |
|---|---|---|---|
| L1 | No save file at all is not an error | PASS | `loaded=True name=PLAYER matches=0 error=''` |
| L2 | A valid name is trimmed and inner spaces collapsed | PASS | `saved=True name='Dama Player'` |
| L3 | The profile survives a reload from disk and the file carries a signature | PASS | `name='Charibelle G.' file=True sig=True` |
| L4 | A blank name is refused and the stored name is kept | PASS | `refused=True name='Charibelle G.'` |
| L5 | A name longer than 16 characters is capped | PASS | `length=16 name='ABCDEFGHIJKLMNOP'` |
| L6 | Control characters are stripped out of a name | PASS | `name='DamaPlayer'` |
| L7 | A finished match is recorded and written to disk | PASS | `returned=True wins=1 recent=1 file=True` |
| L8 | The best-times board keeps the 5 fastest victories, quickest first | PASS | `count=5 first=38.9 last=61.2 sorted=True` |
| L9 | The same match id cannot be banked twice | PASS | `refused=True wins=1 (before=1)` |
| L10 | An unreadable save starts empty without a crash, and is set aside | PASS | `name=PLAYER matches=0 rejected_kept=True` |
| L11 | A hand-edited save whose signature does not match is rejected | PASS | `wins=0 name=PLAYER error='the signature does not match the contents (the file was edited by hand)'` |
| L12 | Impossible numbers in a correctly signed save are clamped, not trusted | PASS | `wins=0 losses=0 bestStreak=9 name='Charibelle G.'` |
| L13 | Reset erases the record on disk and returns the default name | PASS | `returned=True file=False name=PLAYER matches=0` |
| L14 | An unwritable save location reports an error instead of crashing | PASS | `saved=False error='Cannot create ...\l14\blocked because a file or directory with the same name already exists.'` |
| L15 | **Known limit:** the save is tamper-evident, not encrypted | documented | Not asserted as a pass; printed as a limitation |

```
TOTAL: 14 passed, 0 failed, 1 documented known limit (L15)
```

### About the count - stated plainly

The log contains fifteen rows, `L1` to `L15`, which is the shape the weekly task asks for.
Fourteen of them are assertions that can fail. The fifteenth is not an assertion: it records
what the design does **not** protect against, and it prints as `KNOWN LIMIT` every run.

A test that "passes" a limitation would be a dishonest test, so L15 is not counted as a pass
and the total line says so. The alternative - writing a check that always succeeds and calling
it a fifteenth pass - would have made this document look stronger and told the reader less.

### Two details worth stating, because they are what the checks are worth

- **L12 plants a save whose numbers are impossible but whose signature is correct.** It passes
  the signature gate and is then clamped to `wins=0 losses=0`. That is the proof that the
  signature is not being used as the only defence.
- **L14 does not merely fail to crash.** `LastError` carries the operating system's own
  message and the panel prints it, so a real player would see *"Could not save."* followed by
  the reason instead of a silent no-op.

---

## 7. Reproducing every check

`Evidence/HOW_TO_RERUN.txt` holds the exact procedure: which editor menu item runs the
harness, where the log lands, how the scratch folder is isolated from the real save, how the
four screenshots are re-captured, and the two project quirks (domain reload is disabled, and
the game sets `Time.timeScale = 0` when it pauses) that will break a naive re-run. Nothing in
that procedure needs a phone.

---

## 8. Evidence

| File | What it shows |
|---|---|
| `Evidence/pt5_checks.log` | The fifteen rows with their measured values and the total line |
| `Evidence/E1_profile_empty.png` | The panel on a fresh install: `PLAYER`, 0 matches, board reads *none yet* |
| `Evidence/E2_profile_stats.png` | The record after three matches: `Charibelle G.`, 3 matches, 2 wins 1 loss, best streak 2, board 47.3 s and 55.0 s |
| `Evidence/E3_leaderboard.png` | The best-times board with five entries, quickest first: 38.9, 42.5, 47.3, 50.0, 55.0 |
| `Evidence/E4_after_reset.png` | After the two-press reset: back to `PLAYER`, 0 matches, *"Profile erased."* |
| `Evidence/week13_shots.log` | The timestamped record of the capture run, including `wins before=0 after=1 delta=1 lastMatchId=1` |
| `Evidence/build_and_commit.txt` | The Android settings, the scene order, the commit id, and what was not built |
| `Evidence/secrets_and_gitignore.txt` | The credential scan and the ignore rules |
| `Evidence/HOW_TO_RERUN.txt` | How to reproduce every check and every screenshot |

The screenshots were captured in play mode from the real panel driving the real save file - not
a mock. The capture run is gated on each file landing on disk before it changes the profile
state, because `ScreenCapture.CaptureScreenshot` writes asynchronously. An earlier run without
that gate produced an E2 that showed E3's numbers; the four images here are the gated set, and
`week13_shots.log` is the timestamped proof of the order.

---

## 9. Disclosure record

Stated plainly, because the module asks for it and because the alternative is worse.

| Activity | How it was done |
|---|---|
| SA5 analysis, PT5 write-up, reflection drafting | Written by me with AI assistance for structure and phrasing, then reviewed and corrected against the code |
| `PlayerProfile.cs`, `ProfilePanel.cs`, `ProfileSceneBuilder.cs`, `Week13Checks.cs` | Written with AI assistance, then read line by line; every constant and every measured value in this document was taken from the code or the generated log rather than from the plan |
| The fifteen rows | Executed by the harness in the editor; the values in section 6 are copied from `pt5_checks.log` |
| The four screenshots | Captured by the harness in play mode; they are the real panel |
| The Android build settings | Applied by `Assets/Scripts/Editor/AndroidBuildSetup.cs` and read back from `ProjectSettings.asset`, so they can be reproduced rather than only described |
| The decision in section 4 (sign, do not encrypt) | Mine, and section 5 is the reason I am not claiming more than it does |

Nothing in the project was fabricated for the write-up. Where the build falls short - no APK
was produced this week, the save is not encrypted, the profile panel has never been tested on a
physical phone - it is listed as a limitation in section 10 and in the README instead of being
left out.

---

## 10. Build and repository state

| Item | Value |
|---|---|
| APK | none attached this week - the runtime evidence is the play-mode captures |
| Build target | Android, ARM64 only, IL2CPP, minimum API 26, package `com.defaultcompany.dama` |
| Scenes in build | `DamaGame.unity` (enabled, index 0), `SampleScene.unity` (disabled) |
| Signing | not applicable - no build produced |
| Repository branch | `main` |
| Code commit | `b6b477b` "Week 13 PT5: signed player profile, PT5 data checks and play-mode evidence", 2026-11-26 |
| Commit contents | 19 files changed, 3570 insertions, 134 deletions |
| Documentation commit | follows this document; a document cannot contain the hash of the commit that contains it, so the code commit above is the one to check out |

`Evidence/build_and_commit.txt` records the settings, the scene order and the same commit id.

---

## 11. How this meets the Week 13 laboratory requirements

| Module requirement | Where it is satisfied |
|---|---|
| Secure and responsible handling of player data | One profile file inside the app sandbox; no account, no identifier, no networking code, no forced `INTERNET` permission, no analytics SDK (SA5 section 4) |
| An identity model proportionate to the game | Local profile only, argued in SA5 section 6 and implemented as `PlayerProfile` |
| An integrity check on the saved data | HMAC-SHA256 over the canonical payload, salted per install (sections 4.1, 4.2) |
| Graceful behaviour on corrupt or tampered data | Quarantine plus clamps (L10, L11, L12) |
| A recoverable, provable reset | Two-press reset with a 4-second disarm (L13, `ProfilePanel.ResetPressed`) |
| Evidence rather than assertion | `pt5_checks.log` with measured values, four play-mode captures, a reproducible commit |
| Responsible development and disclosure | Section 9, plus SA5 section 7 on secret handling, plus the limits in section 5 |
| A reflective account of the work | `Guzman_Charibelle_Week13_Reflection.md` |
