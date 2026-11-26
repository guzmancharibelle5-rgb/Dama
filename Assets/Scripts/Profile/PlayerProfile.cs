using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>
/// The player's own data for Dama: a display name, the side they play, a
/// win/loss/draw record with a win streak, the last 20 finished matches and a
/// local best-times board of the five fastest victories.
///
/// This class is the only thing that reads, signs, writes, clamps or deletes the
/// save file. The UI never parses JSON and never touches the disk.
///
/// Security position (see PT5 section 4): the save is signed with HMAC-SHA256
/// keyed by a 16-byte salt generated on first save and kept in
/// <c>PlayerPrefs["profile.salt"]</c>. There is no constant key in the source, so
/// two installs of the same build sign differently. The file is
/// tamper-EVIDENT, not encrypted and not tamper-proof: the salt lives on the
/// same device as the file, so a determined local attacker can re-sign an edited
/// save. That limit is stated in the documents rather than hidden.
/// </summary>
public static class PlayerProfile
{
    public const int SchemaVersion = 1;
    public const int MaxNameLength = 16;
    public const int MaxRecentMatches = 20;
    public const int MaxBestWins = 5;
    public const string DefaultName = "PLAYER";

    private const string SaltPrefsKey = "profile.salt";
    private const string FolderName = "Dama";
    private const string FileName = "profile.json";
    private const string RejectedFileName = "profile.rejected.json";

    /// <summary>One finished match, as banked at the end screen.</summary>
    [Serializable]
    public class MatchRecord
    {
        public int matchId;
        public string result = "LOSS";   // WIN | LOSS | DRAW, from the profile owner's side
        public string winner = "NONE";   // RED | BLUE | NONE
        public float seconds;
        public int redPieces;
        public int bluePieces;
        public string whenUtc = "";
    }

    /// <summary>Everything the signature covers. Field order is the canonical form.</summary>
    [Serializable]
    public class Payload
    {
        public int schema = SchemaVersion;
        public string playerName = DefaultName;
        public int playerSide = 1;       // 1 = RED, 2 = BLUE
        public int wins;
        public int losses;
        public int draws;
        public int currentStreak;
        public int bestStreak;
        public int lastMatchId;
        public string lastPlayedUtc = "";
        public List<MatchRecord> recent = new List<MatchRecord>();
    }

    [Serializable]
    private class Envelope
    {
        public int schema = SchemaVersion;
        public string sig = "";
        public Payload payload = new Payload();
    }

    private static Payload state = new Payload();
    private static string dataDirectory;

    // ---- Inspection ----

    public static bool IsLoaded { get; private set; }
    public static string LastError { get; private set; } = "";

    public static string DataDirectory
    {
        get
        {
            if (string.IsNullOrEmpty(dataDirectory))
                dataDirectory = Path.Combine(Application.persistentDataPath, FolderName);
            return dataDirectory;
        }
    }

    public static string SavePath => Path.Combine(DataDirectory, FileName);
    public static string RejectedPath => Path.Combine(DataDirectory, RejectedFileName);

    public static string PlayerName { get { EnsureLoaded(); return state.playerName; } }
    public static int PlayerSide { get { EnsureLoaded(); return state.playerSide; } }
    public static int Wins { get { EnsureLoaded(); return state.wins; } }
    public static int Losses { get { EnsureLoaded(); return state.losses; } }
    public static int Draws { get { EnsureLoaded(); return state.draws; } }
    public static int MatchesPlayed { get { EnsureLoaded(); return state.wins + state.losses + state.draws; } }
    public static int CurrentStreak { get { EnsureLoaded(); return state.currentStreak; } }
    public static int BestStreak { get { EnsureLoaded(); return state.bestStreak; } }
    public static int LastMatchId { get { EnsureLoaded(); return state.lastMatchId; } }
    public static int NextMatchId { get { EnsureLoaded(); return state.lastMatchId + 1; } }
    public static string LastPlayedUtc { get { EnsureLoaded(); return state.lastPlayedUtc; } }

    public static IReadOnlyList<MatchRecord> Recent { get { EnsureLoaded(); return state.recent; } }

    // ---- Loading and saving ----

    /// <summary>Reads the save once. Never throws; a bad file is set aside and an empty profile starts.</summary>
    public static bool Load()
    {
        EnsureLoaded();
        return IsLoaded;
    }

    private static void EnsureLoaded()
    {
        if (IsLoaded) return;
        IsLoaded = true;

        try
        {
            if (!File.Exists(SavePath))
            {
                state = new Payload();
                return;
            }

            string raw = File.ReadAllText(SavePath, Encoding.UTF8);
            Envelope envelope = JsonUtility.FromJson<Envelope>(raw);
            if (envelope == null || envelope.payload == null)
                throw new InvalidDataException("the save file is not a Dama profile");

            if (envelope.schema > SchemaVersion)
                throw new InvalidDataException("the save was written by a newer version of the game");

            // The signature covers the canonical re-serialisation, not the raw bytes,
            // so whitespace or key order in the file cannot look like tampering.
            string canonical = JsonUtility.ToJson(envelope.payload);
            string expected = ComputeSignature(canonical);

            if (!string.Equals(expected, envelope.sig, StringComparison.Ordinal))
                throw new InvalidDataException("the signature does not match the contents (the file was edited by hand)");

            state = Clamp(envelope.payload);
            LastError = "";
        }
        catch (Exception error)
        {
            LastError = error.Message;
            Quarantine();
            state = new Payload();
        }
    }

    /// <summary>Writes the current record. Returns false and fills LastError instead of throwing.</summary>
    public static bool Save()
    {
        EnsureLoaded();

        try
        {
            Directory.CreateDirectory(DataDirectory);

            string canonical = JsonUtility.ToJson(state);
            Envelope envelope = new Envelope
            {
                schema = SchemaVersion,
                sig = ComputeSignature(canonical),
                payload = state
            };

            string json = JsonUtility.ToJson(envelope);
            string tempPath = SavePath + ".tmp";

            // Atomic replace: a process killed mid-write leaves a .tmp file, never a
            // half-written profile.json where a complete one used to be.
            File.WriteAllText(tempPath, json, new UTF8Encoding(false));
            if (File.Exists(SavePath)) File.Delete(SavePath);
            File.Move(tempPath, SavePath);

            LastError = "";
            return true;
        }
        catch (Exception error)
        {
            LastError = error.Message;
            Debug.LogError($"[PlayerProfile] Could not write {SavePath}: {error.Message}");
            return false;
        }
    }

    // ---- State changes ----

    /// <summary>Stores a cleaned name. A blank name is refused so the old one survives.</summary>
    public static bool SetName(string name)
    {
        EnsureLoaded();

        if (name == null || name.Trim().Length == 0) return false;

        string clean = SanitizeName(name);
        if (string.Equals(clean, state.playerName, StringComparison.Ordinal)) return true;

        state.playerName = clean;
        return Save();
    }

    /// <summary>Stores which side the profile owner plays (1 = RED, 2 = BLUE).</summary>
    public static bool SetSide(int side)
    {
        EnsureLoaded();

        if (side != 1 && side != 2) return false;
        if (state.playerSide == side) return true;

        state.playerSide = side;
        return Save();
    }

    /// <summary>
    /// Banks one finished match. Refused when the id is at or below the last banked
    /// id, which is what makes a double-bank impossible. Rolls memory back if the
    /// write fails, so memory and disk never disagree.
    /// </summary>
    public static bool RecordMatch(int matchId, int winnerSide, float seconds, int redPieces, int bluePieces)
    {
        EnsureLoaded();

        if (matchId <= state.lastMatchId) return false;

        Payload before = Clone(state);

        string result = winnerSide == 0 ? "DRAW" : (winnerSide == state.playerSide ? "WIN" : "LOSS");

        MatchRecord record = new MatchRecord
        {
            matchId = matchId,
            result = result,
            winner = winnerSide == 0 ? "NONE" : (winnerSide == 1 ? "RED" : "BLUE"),
            seconds = Mathf.Max(0f, seconds),
            redPieces = Mathf.Max(0, redPieces),
            bluePieces = Mathf.Max(0, bluePieces),
            whenUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        state.recent.Insert(0, record);
        if (state.recent.Count > MaxRecentMatches)
            state.recent.RemoveRange(MaxRecentMatches, state.recent.Count - MaxRecentMatches);

        if (result == "WIN")
        {
            state.wins++;
            state.currentStreak++;
            if (state.currentStreak > state.bestStreak) state.bestStreak = state.currentStreak;
        }
        else if (result == "LOSS")
        {
            state.losses++;
            state.currentStreak = 0;
        }
        else
        {
            state.draws++;
        }

        state.lastMatchId = matchId;
        state.lastPlayedUtc = record.whenUtc;

        if (Save()) return true;

        state = before;
        return false;
    }

    /// <summary>Erases the record. The install salt is kept, so a reset profile signs the same way.</summary>
    public static bool ResetProfile()
    {
        EnsureLoaded();

        try
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            if (File.Exists(SavePath + ".tmp")) File.Delete(SavePath + ".tmp");
        }
        catch (Exception error)
        {
            LastError = error.Message;
            return false;
        }

        state = new Payload();
        LastError = "";
        return true;
    }

    /// <summary>The five fastest victories, quickest first, then by match id.</summary>
    public static List<MatchRecord> BestWins()
    {
        EnsureLoaded();

        List<MatchRecord> wins = new List<MatchRecord>();
        foreach (MatchRecord record in state.recent)
            if (record != null && record.result == "WIN") wins.Add(record);

        wins.Sort((a, b) =>
        {
            int byTime = a.seconds.CompareTo(b.seconds);
            return byTime != 0 ? byTime : a.matchId.CompareTo(b.matchId);
        });

        if (wins.Count > MaxBestWins) wins.RemoveRange(MaxBestWins, wins.Count - MaxBestWins);
        return wins;
    }

    // ---- Pure functions ----

    /// <summary>Trims, strips control characters, collapses spaces, caps at 16, and blanks become PLAYER.</summary>
    public static string SanitizeName(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return DefaultName;

        StringBuilder builder = new StringBuilder(raw.Length);
        bool lastWasSpace = false;

        foreach (char c in raw)
        {
            if (char.IsControl(c)) continue;

            if (char.IsWhiteSpace(c))
            {
                if (!lastWasSpace && builder.Length > 0) builder.Append(' ');
                lastWasSpace = true;
                continue;
            }

            builder.Append(c);
            lastWasSpace = false;
            if (builder.Length >= MaxNameLength) break;
        }

        string clean = builder.ToString().Trim();
        return clean.Length == 0 ? DefaultName : clean;
    }

    /// <summary>HMAC-SHA256 over the canonical payload, keyed with the per-install salt.</summary>
    public static string ComputeSignature(string canonical)
    {
        using (HMACSHA256 hmac = new HMACSHA256(GetSaltBytes()))
        {
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical ?? string.Empty));
            StringBuilder builder = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash) builder.Append(b.ToString("x2"));
            return builder.ToString();
        }
    }

    private static byte[] GetSaltBytes()
    {
        string stored = PlayerPrefs.GetString(SaltPrefsKey, "");

        if (!string.IsNullOrEmpty(stored))
        {
            try { return Convert.FromBase64String(stored); }
            catch { /* a damaged salt is replaced below */ }
        }

        byte[] fresh = new byte[16];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create()) rng.GetBytes(fresh);

        PlayerPrefs.SetString(SaltPrefsKey, Convert.ToBase64String(fresh));
        PlayerPrefs.Save();
        return fresh;
    }

    // ---- Internals ----

    private static Payload Clamp(Payload payload)
    {
        payload.schema = SchemaVersion;
        payload.playerName = SanitizeName(payload.playerName);
        if (payload.playerSide != 1 && payload.playerSide != 2) payload.playerSide = 1;

        payload.wins = Mathf.Max(0, payload.wins);
        payload.losses = Mathf.Max(0, payload.losses);
        payload.draws = Mathf.Max(0, payload.draws);
        payload.currentStreak = Mathf.Max(0, payload.currentStreak);
        payload.bestStreak = Mathf.Max(payload.bestStreak, payload.currentStreak);
        payload.lastMatchId = Mathf.Max(0, payload.lastMatchId);
        if (payload.lastPlayedUtc == null) payload.lastPlayedUtc = "";

        if (payload.recent == null) payload.recent = new List<MatchRecord>();

        List<MatchRecord> clean = new List<MatchRecord>();
        foreach (MatchRecord record in payload.recent)
        {
            if (record == null) continue;
            record.matchId = Mathf.Max(0, record.matchId);
            record.seconds = float.IsNaN(record.seconds) || float.IsInfinity(record.seconds)
                ? 0f
                : Mathf.Max(0f, record.seconds);
            record.redPieces = Mathf.Max(0, record.redPieces);
            record.bluePieces = Mathf.Max(0, record.bluePieces);
            if (string.IsNullOrEmpty(record.result)) record.result = "LOSS";
            if (string.IsNullOrEmpty(record.winner)) record.winner = "NONE";
            if (record.whenUtc == null) record.whenUtc = "";
            clean.Add(record);
            if (clean.Count >= MaxRecentMatches) break;
        }

        payload.recent = clean;
        return payload;
    }

    private static void Quarantine()
    {
        try
        {
            if (!File.Exists(SavePath)) return;
            if (File.Exists(RejectedPath)) File.Delete(RejectedPath);
            File.Move(SavePath, RejectedPath);
        }
        catch (Exception error)
        {
            Debug.LogWarning($"[PlayerProfile] Could not set aside the rejected save: {error.Message}");
        }
    }

    private static Payload Clone(Payload source)
    {
        return JsonUtility.FromJson<Payload>(JsonUtility.ToJson(source));
    }

    // ---- Test seams (used by Assets/Scripts/Editor/Week13Checks.cs) ----

    /// <summary>Points the store at a scratch folder so checks never touch the real save.</summary>
    public static void SetDataDirectory(string path)
    {
        dataDirectory = path;
        state = new Payload();
        IsLoaded = false;
        LastError = "";
    }

    /// <summary>Returns the store to the real save folder and forgets the cached copy.</summary>
    public static void UseDefaultDataDirectory()
    {
        dataDirectory = null;
        ResetCacheForTests();
    }

    /// <summary>Forgets the in-memory copy so the next read comes from disk.</summary>
    public static void ResetCacheForTests()
    {
        state = new Payload();
        IsLoaded = false;
        LastError = "";
    }

    /// <summary>Writes text to the save path exactly as given, signature included.</summary>
    public static void WriteRawForTests(string raw)
    {
        Directory.CreateDirectory(DataDirectory);
        File.WriteAllText(SavePath, raw ?? string.Empty, new UTF8Encoding(false));
        ResetCacheForTests();
    }

    /// <summary>Writes a correctly signed envelope for the given payload, however implausible it is.</summary>
    public static void WriteSignedForTests(Payload payload)
    {
        Directory.CreateDirectory(DataDirectory);
        Envelope envelope = new Envelope
        {
            schema = SchemaVersion,
            sig = ComputeSignature(JsonUtility.ToJson(payload)),
            payload = payload
        };

        File.WriteAllText(SavePath, JsonUtility.ToJson(envelope), new UTF8Encoding(false));
        ResetCacheForTests();
    }

    public static void WriteGarbageForTests()
    {
        WriteRawForTests("this is not json {{{");
    }
}
