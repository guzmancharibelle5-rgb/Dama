using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Week 17 release-readiness harness for Dama.
///
/// Menu: Tools > Dama > Week 17 > 2. Release Checks
///
/// Five cases, each answering a question the release checklist asks and a scene-only inspection
/// cannot answer:
///
///   TC-REL-01  the title caption names the version the binary was compiled with
///   TC-REL-02  the credits build line names the same version and the application id, and both
///              surfaces agree - the failure mode being a string baked into the scene, which can
///              name a different build while looking correct
///   TC-REL-03  HOW TO PLAY opens, carries a real instruction body, and closes back to the title
///   TC-REL-04  CREDITS opens and closes, and every new control has a live runtime listener bound
///              to the method it claims to call (read from the UnityEvent, not assumed)
///   TC-REL-05  the two new panels are hidable and cannot leak into a running match
///
/// It drives the game through DamaBoard's own public entry points, in play mode, exactly as the
/// player's taps do. Output: docs/Charibelle Guzman/Week_17/Evidence/test_logs_release/
/// </summary>
public static class Week17Checks
{
    private const string MenuPath = "Tools/Dama/Week 17/2. Release Checks";

    private sealed class CaseResult
    {
        public string Id;
        public string Reqs;
        public string Title;
        public bool Passed;
        public string Detail;
    }

    private static readonly List<CaseResult> results = new List<CaseResult>();
    private static readonly List<string> console = new List<string>();
    private static readonly List<Action> steps = new List<Action>();

    private static bool running;
    private static int index;
    private static double nextActionAt;
    private static double deadlineAt;
    private static DamaBoard board;
    private static bool logHooked;
    private static int errorCount;

    public static string RunDir =>
        Path.Combine(Directory.GetParent(Application.dataPath).FullName,
            "docs", "Charibelle Guzman", "Week_17", "Evidence", "test_logs_release");

    [MenuItem(MenuPath, false, 41)]
    public static void Begin()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[Week17] Stop play mode first; this harness starts it itself.");
            return;
        }

        results.Clear();
        console.Clear();
        steps.Clear();
        index = 0;
        errorCount = 0;
        board = null;
        running = true;

        Directory.CreateDirectory(RunDir);

        deadlineAt = EditorApplication.timeSinceStartup + 180.0;

        if (!logHooked)
        {
            Application.logMessageReceived += OnLog;
            logHooked = true;
        }

        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();

        Debug.Log("[Week17] Entering play mode for the RELEASE checks.");
    }

    private static void OnLog(string condition, string stackTrace, LogType type)
    {
        console.Add($"{DateTime.UtcNow:HH:mm:ss.fff} {type,-8} {condition}");

        if (type == LogType.Error || type == LogType.Exception)
            errorCount++;
    }

    private static void Tick()
    {
        if (!running) return;
        if (!EditorApplication.isPlaying) return;

        if (EditorApplication.timeSinceStartup > deadlineAt)
        {
            Abort("the run exceeded its 180 s budget");
            return;
        }

        if (Time.frameCount < 5) return;
        if (EditorApplication.timeSinceStartup < nextActionAt) return;

        if (board == null)
        {
            board = UnityEngine.Object.FindAnyObjectByType<DamaBoard>();
            if (board == null) { Abort("no DamaBoard in the open scene"); return; }

            steps.Add(CaseRel01_TitleCaption);
            steps.Add(CaseRel02_SurfacesAgree);
            steps.Add(CaseRel03_HowToOpensAndCloses);
            steps.Add(CaseRel04_CreditsAndWiring);
            steps.Add(CaseRel05_NoPanelLeak);
        }

        if (index >= steps.Count) { Finish(); return; }

        try
        {
            steps[index]();
        }
        catch (Exception exception)
        {
            Record("TC-INTERNAL", "harness", "harness step " + index, false,
                "threw " + exception.GetType().Name + ": " + exception.Message);
            Debug.LogError($"[Week17] Step {index} threw: {exception}");
        }

        index++;
        nextActionAt = EditorApplication.timeSinceStartup + 0.05;
    }

    // =====================================================================
    // Cases
    // =====================================================================

    private static void CaseRel01_TitleCaption()
    {
        Text caption = FindText("StartPanel/VersionLabel");
        string expected = ReleaseInfo.Caption();

        bool pass = caption != null
                    && !string.IsNullOrEmpty(expected)
                    && caption.text == expected
                    && caption.text.Contains(Application.version)
                    && caption.fontSize >= 18;

        Record("TC-REL-01", "release", "The title caption names the version the binary was compiled with", pass,
            caption == null
                ? "StartPanel/VersionLabel missing"
                : $"caption='{caption.text}' expected='{expected}' Application.version='{Application.version}' fontSize={caption.fontSize}");
    }

    private static void CaseRel02_SurfacesAgree()
    {
        Text caption = FindText("StartPanel/VersionLabel");
        Text build = FindText("CreditsPanel/BuildText");

        string version = Application.version;
        string id = Application.identifier;

        string captionText = caption != null ? caption.text : "MISSING";
        string buildText = build != null ? build.text : "MISSING";

        bool bothNameVersion = caption != null && build != null
                               && captionText.Contains(version)
                               && buildText.Contains(version);

        bool buildNamesId = build != null && buildText.Contains(id);

        // The failure this exists to catch: two surfaces printing two different versions.
        string captionVersion = captionText.Contains(" · ")
            ? captionText.Substring(0, captionText.IndexOf(" · ", StringComparison.Ordinal))
            : captionText;

        string buildVersion = buildText.StartsWith("Build ", StringComparison.Ordinal)
            ? buildText.Substring(6)
            : buildText;
        int separator = buildVersion.IndexOf(" · ", StringComparison.Ordinal);
        if (separator >= 0) buildVersion = buildVersion.Substring(0, separator);

        bool agree = captionVersion == buildVersion && captionVersion == version;

        string detail = $"title='{captionText}' credits='{buildText}' version='{version}' id='{id}' " +
                        $"bothNameVersion={bothNameVersion} buildNamesId={buildNamesId} agree={agree} " +
                        $"(title says '{captionVersion}', credits say '{buildVersion}')";

        Record("TC-REL-02", "release", "The credits build line agrees with the title and names the application id",
            bothNameVersion && buildNamesId && agree, detail);
    }

    private static void CaseRel03_HowToOpensAndCloses()
    {
        board.ShowHowTo();

        bool opened = Active("HowToPanel") && !Active("StartPanel");

        Text body = FindText("HowToPanel/BodyText");
        bool hasBody = body != null && body.text.Length > 400;

        board.CloseHowTo();

        bool closed = !Active("HowToPanel") && Active("StartPanel");

        bool howToVisible = Active("HowToPanel");
        bool titleVisible = Active("StartPanel");
        string detail = $"opened={opened} bodyChars={(body != null ? body.text.Length : 0)} " +
                        $"bodyFont={(body != null ? body.fontSize : 0)} closed={closed} " +
                        $"howToStillOn={howToVisible} titleOn={titleVisible}";

        Record("TC-REL-03", "release", "HOW TO PLAY opens with a real instruction body and closes back to the title",
            opened && hasBody && closed, detail);
    }

    private static void CaseRel04_CreditsAndWiring()
    {
        board.ShowCredits();
        bool opened = Active("CreditsPanel") && !Active("StartPanel");
        board.CloseCredits();
        bool closed = !Active("CreditsPanel") && Active("StartPanel");

        string howToCalls = DescribeRuntimeCalls(FindButton("StartPanel/HowToButton"));
        string creditsCalls = DescribeRuntimeCalls(FindButton("StartPanel/CreditsButton"));
        string exitCalls = DescribeRuntimeCalls(FindButton("StartPanel/ExitButton"));
        string howToBack = DescribeRuntimeCalls(FindButton("HowToPanel/BackButton"));
        string creditsBack = DescribeRuntimeCalls(FindButton("CreditsPanel/BackButton"));

        bool wired = howToCalls.Contains("DamaBoard.ShowHowTo")
                     && creditsCalls.Contains("DamaBoard.ShowCredits")
                     && exitCalls.Contains("DamaBoard.QuitGame")
                     && howToBack.Contains("DamaBoard.CloseHowTo")
                     && creditsBack.Contains("DamaBoard.CloseCredits");

        Record("TC-REL-04", "release", "CREDITS opens and closes, and all five new controls are bound to the method they claim",
            opened && closed && wired,
            $"opened={opened} closed={closed} wired={wired} | HOWTO[{howToCalls}] CREDITS[{creditsCalls}] EXIT[{exitCalls}] " +
            $"HOWTO-BACK[{howToBack}] CREDITS-BACK[{creditsBack}]");
    }

    private static void CaseRel05_NoPanelLeak()
    {
        string[] titlePanels = { "HowToPanel", "CreditsPanel", "ProfilePanel", "PausePanel", "WinPanel", "HUDPanel" };

        board.CloseHowTo();
        board.CloseCredits();
        board.CloseProfile();

        string atTitle = "";
        bool quiet = true;
        foreach (string panel in titlePanels)
        {
            bool on = Active(panel);
            atTitle += $"{panel}={on} ";
            quiet &= !on;
        }
        bool titleOn = Active("StartPanel");

        board.StartGame();

        bool howToAfter = Active("HowToPanel");
        bool creditsAfter = Active("CreditsPanel");
        bool startAfter = Active("StartPanel");
        bool hudAfter = Active("HUDPanel");
        bool matchQuiet = !howToAfter && !creditsAfter && !startAfter && hudAfter;

        board.ReturnToMenu();

        string afterStart = $"HowTo={howToAfter} Credits={creditsAfter} Start={startAfter} HUD={hudAfter}";
        string detail = $"atTitle: StartPanel={titleOn} {atTitle}| after StartGame: {afterStart}";

        Record("TC-REL-05", "release", "The new panels hide completely and cannot leak into a running match",
            quiet && titleOn && matchQuiet, detail);
    }

    // =====================================================================
    // Helpers
    // =====================================================================

    /// <summary>
    /// Reads the live listeners off a Button's UnityEvent and names them. A Button wired at runtime
    /// with AddListener has no persistent call, so GetPersistentEventCount() would report 0 for a
    /// perfectly working button - the runtime call list is the only honest source.
    /// </summary>
    private static string DescribeRuntimeCalls(Button button)
    {
        if (button == null) return "button missing";

        FieldInfo callsField = typeof(UnityEventBase).GetField("m_Calls", BindingFlags.Instance | BindingFlags.NonPublic);
        object calls = callsField != null ? callsField.GetValue(button.onClick) : null;
        if (calls == null) return "no calls object";

        FieldInfo runtimeField = calls.GetType().GetField("m_RuntimeCalls", BindingFlags.Instance | BindingFlags.NonPublic);
        IList runtimeCalls = runtimeField != null ? runtimeField.GetValue(calls) as IList : null;
        if (runtimeCalls == null) return "no runtime list";

        StringBuilder names = new StringBuilder();
        foreach (object call in runtimeCalls)
        {
            // An AddListener() call is stored as an InvokableCall wrapping the delegate, not as a
            // (target, method) pair, so the delegate is what has to be read.
            object stored = ReadField(call, "m_Delegate") ?? ReadField(call, "Delegate");

            if (stored is Delegate handler)
            {
                string targetName = handler.Target is UnityEngine.Object unityObject
                    ? unityObject.GetType().Name
                    : (handler.Target != null ? handler.Target.GetType().Name : "null");

                names.Append(targetName).Append('.').Append(handler.Method.Name).Append(' ');
                continue;
            }

            object target = ReadField(call, "Target");
            object method = ReadField(call, "Method");
            string fallbackTarget = target is UnityEngine.Object targetObject
                ? targetObject.GetType().Name
                : (target != null ? target.GetType().Name : "null");
            names.Append(fallbackTarget).Append('.').Append(method is MethodInfo info ? info.Name : "?").Append(' ');
        }

        return $"{runtimeCalls.Count} call(s): {names.ToString().Trim()}";
    }

    private static object ReadField(object instance, string name)
    {
        Type type = instance.GetType();
        while (type != null)
        {
            FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null) return field.GetValue(instance);
            type = type.BaseType;
        }

        return null;
    }

    private static bool Active(string path)
    {
        Transform transform = Find(path);
        return transform != null && transform.gameObject.activeInHierarchy;
    }

    private static Transform Find(string path)
    {
        GameObject canvas = GameObject.Find("GameCanvas");
        return canvas != null ? canvas.transform.Find(path) : null;
    }

    private static Text FindText(string path)
    {
        Transform transform = Find(path);
        return transform != null ? transform.GetComponent<Text>() : null;
    }

    private static Button FindButton(string path)
    {
        Transform transform = Find(path);
        return transform != null ? transform.GetComponent<Button>() : null;
    }

    // =====================================================================
    // Recording and output
    // =====================================================================

    private static void Record(string id, string reqs, string title, bool passed, string detail)
    {
        results.Add(new CaseResult { Id = id, Reqs = reqs, Title = title, Passed = passed, Detail = detail });
        Debug.Log($"[Week17] {id} {(passed ? "PASS" : "FAIL")} ({reqs}) {title} :: {detail}");
    }

    private static void Finish()
    {
        Time.timeScale = 1f;

        try
        {
            File.WriteAllText(Path.Combine(RunDir, "case_log.tsv"), BuildCaseLog(), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(RunDir, "summary.txt"), BuildSummary(), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(RunDir, "console.log"), string.Join("\n", console) + "\n", new UTF8Encoding(false));
        }
        catch (Exception exception)
        {
            Debug.LogError($"[Week17] Could not write the log: {exception.Message}");
        }

        Debug.Log($"[Week17] RELEASE checks finished: {SummaryLine()} Written to {RunDir}");

        Stop("finished");
    }

    private static void Abort(string reason)
    {
        Debug.LogError($"[Week17] RELEASE checks aborted at step {index}: {reason}");

        try
        {
            Directory.CreateDirectory(RunDir);
            File.WriteAllText(Path.Combine(RunDir, "case_log.tsv"), BuildCaseLog(), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(RunDir, "summary.txt"),
                $"ABORTED: {reason}\n\n" + BuildSummary(), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(RunDir, "console.log"), string.Join("\n", console) + "\n", new UTF8Encoding(false));
        }
        catch (Exception exception)
        {
            Debug.LogError($"[Week17] Could not write the partial log: {exception.Message}");
        }

        Stop("aborted");
    }

    private static void Stop(string how)
    {
        running = false;
        EditorApplication.update -= Tick;

        if (logHooked)
        {
            Application.logMessageReceived -= OnLog;
            logHooked = false;
        }

        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();

        Debug.Log($"[Week17] Harness {how}.");
    }

    private static string SummaryLine()
    {
        int pass = 0, fail = 0;
        foreach (CaseResult result in results)
        {
            if (result.Passed) pass++; else fail++;
        }

        return $"{pass} passed, {fail} failed, {errorCount} console error(s).";
    }

    private static string BuildCaseLog()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("id\treq\tstatus\ttitle\tmeasured");

        foreach (CaseResult result in results)
            builder.AppendLine($"{result.Id}\t{result.Reqs}\t{(result.Passed ? "PASS" : "FAIL")}\t{result.Title}\t{result.Detail}");

        return builder.ToString();
    }

    private static string BuildSummary()
    {
        int pass = 0, fail = 0;
        foreach (CaseResult result in results)
        {
            if (result.Passed) pass++; else fail++;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("DAMA - WEEK 17 RELEASE CHECKS");
        builder.AppendLine("Game: Dama  |  Theme: Philippine Games and Sports  |  Student: Charibelle Guzman (BSIT-4)");
        builder.AppendLine("Harness: Assets/Scripts/Editor/Week17Checks.cs");
        builder.AppendLine($"Scene: DamaGame.unity   Unity: {Application.unityVersion}   Editor: {Application.platform}");
        builder.AppendLine($"Run at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC   Game view: {Screen.width}x{Screen.height}");
        builder.AppendLine($"Version under test: {ReleaseInfo.Version}   Application id: {ReleaseInfo.BundleId}");
        builder.AppendLine($"TOTAL: {pass} passed, {fail} failed, {errorCount} console error(s)");
        builder.AppendLine(new string('-', 100));

        foreach (CaseResult result in results)
        {
            builder.AppendLine($"{result.Id,-13} {(result.Passed ? "PASS" : "FAIL"),-7} {result.Reqs,-11} {result.Title}");
            builder.AppendLine($"                {result.Detail}");
        }

        return builder.ToString();
    }
}
