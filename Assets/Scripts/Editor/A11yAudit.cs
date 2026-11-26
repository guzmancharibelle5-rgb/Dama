using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Week 14 accessibility audit for Dama.
///
/// Menu: Tools > Dama > Week 14 > 1. Run Accessibility Audit
///
/// Walks every Text, Button and InputField under GameCanvas in the open scene - including the
/// panels that are inactive, because a hidden panel is still a panel a player will meet - and
/// writes the result to docs/Charibelle Guzman/Week_14/Evidence/.
///
/// The first run of a pair writes a11y_baseline.log; every later run writes a11y_after.log, so
/// the two files are produced by the same code with the same thresholds. A timestamped copy is
/// kept on every run so neither file can be quietly hand-edited into agreement.
///
/// Three checks:
///   C  contrast  - WCAG 2.2 ratio of the label against its nearest opaque-enough backdrop
///   L  label fit - a legacy uGUI Text whose rect is shorter than its line height spills or clips
///   T  touch     - Button and InputField size against a 96 canvas-px floor (see the derivation in
///                  the log header; it is a canvas-px measurement on an assumed phone, not a
///                  measurement taken on a device)
/// </summary>
public static class A11yAudit
{
    private const string Menu = "Tools/Dama/Week 14/1. Run Accessibility Audit";
    private const string CanvasName = "GameCanvas";
    private const string SceneName = "DamaGame";

    // 48 dp at xxhdpi is 144 physical px; the canvas scale on a 1080p phone is 1.5, so the floor
    // in canvas units is 144 / 1.5 = 96. See WrittenHeader() for the full derivation.
    private const float TouchFloorCanvasPx = 96f;

    private const float ContrastNormal = 4.5f;
    private const float ContrastLarge = 3f;
    private const float LineHeightFactor = 1.12f;
    private const float BackdropAlpha = 0.5f;

    private static readonly List<string> lines = new List<string>();
    private static int passed;
    private static int failed;
    private static int warnings;
    private static int notMeasurable;

    public static string EvidenceDir =>
        Path.Combine(Directory.GetParent(Application.dataPath).FullName,
            "docs", "Charibelle Guzman", "Week_14", "Evidence");

    private static string BaselinePath => Path.Combine(EvidenceDir, "a11y_baseline.log");
    private static string AfterPath => Path.Combine(EvidenceDir, "a11y_after.log");

    [MenuItem(Menu)]
    public static void RunAudit()
    {
        Directory.CreateDirectory(EvidenceDir);

        lines.Clear();
        passed = 0;
        failed = 0;
        warnings = 0;
        notMeasurable = 0;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != SceneName)
        {
            Debug.LogError($"[A11yAudit] Open {SceneName}.unity first. The active scene is '{scene.name}'.");
            return;
        }

        GameObject canvas = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == CanvasName) { canvas = root; break; }
        }

        if (canvas == null)
        {
            Debug.LogError($"[A11yAudit] No '{CanvasName}' in the open scene.");
            return;
        }

        string label = File.Exists(BaselinePath) ? "after" : "baseline";

        WrittenHeader(label, canvas);

        AuditLabels(canvas);
        AuditTouchTargets(canvas);
        WriteTotals();

        string stamped = Path.Combine(EvidenceDir, $"a11y_audit_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        string body = string.Join("\n", lines) + "\n";

        File.WriteAllText(stamped, body, new UTF8Encoding(false));
        File.WriteAllText(label == "baseline" ? BaselinePath : AfterPath, body, new UTF8Encoding(false));

        Debug.Log($"[A11yAudit] {label.ToUpper()} run: {passed} passed, {failed} failed, " +
                  $"{warnings} warnings, {notMeasurable} not measurable. " +
                  $"Written to {(label == "baseline" ? "a11y_baseline.log" : "a11y_after.log")} and {Path.GetFileName(stamped)}");
    }

    // =====================================================================
    // Header
    // =====================================================================

    private static void WrittenHeader(string label, GameObject canvas)
    {
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        string scalerText = "none";

        if (scaler != null)
        {
            scalerText = scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize
                ? $"Scale With Screen Size, reference {scaler.referenceResolution.x:0}x{scaler.referenceResolution.y:0}, " +
                  $"match {scaler.matchWidthOrHeight:0.00}"
                : $"Constant Pixel Size, scaleFactor {scaler.scaleFactor:0.00}";
        }

        lines.Add("DAMA - WEEK 14 ACCESSIBILITY AUDIT (WCAG 2.2 subset)");
        lines.Add("Game: Dama  |  Theme: Philippine Games and Sports");
        lines.Add("Student: Charibelle Guzman (BSIT-4)");
        lines.Add("Harness: Assets/Scripts/Editor/A11yAudit.cs");
        lines.Add($"Scene: {SceneName}.unity   Unity: {Application.unityVersion}   Run at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        lines.Add($"Run label: {label.ToUpper()}   (the first run writes a11y_baseline.log, later runs write a11y_after.log)");
        lines.Add($"Canvas: {CanvasName}   CanvasScaler: {scalerText}");
        lines.Add(new string('-', 108));
        lines.Add("CHECKS AND THRESHOLDS");
        lines.Add($"  C  contrast      {ContrastNormal:0.0}:1 for normal text, {ContrastLarge:0.0}:1 for large text (>=28 px, or >=24 px bold).");
        lines.Add($"                   Measured against the nearest ancestor Image with alpha >= {BackdropAlpha:0.00}, composited over black,");
        lines.Add("                   which is the darkest plausible backdrop. For light text that is the most favourable case.");
        lines.Add($"  L  label fit     rect.height >= fontSize x {LineHeightFactor:0.00}. Split into 'clipped' (overflow truncates: the label can vanish)");
        lines.Add("                   and 'spills' (overflow allowed: it draws outside its box). Clipped fails, spills warns.");
        lines.Add($"  T  touch target  {TouchFloorCanvasPx:0} x {TouchFloorCanvasPx:0} canvas px, for every Button and InputField.");
        lines.Add("     Derivation, stated rather than assumed: on a 1080p landscape phone (1920x1080) the scaler above gives a uniform");
        lines.Add("     scale of 1.50 (1920/1280 = 1080/720 = 1.5). Android's 48 dp minimum touch target at xxhdpi (3.0 density) is");
        lines.Add("     144 physical px, and 144 / 1.5 = 96 canvas px. This is a canvas-px measurement against a reference resolution on");
        lines.Add("     an assumed phone class - NOT a measurement taken with a ruler on a handset.");
        lines.Add("  Not measurable   a label with no opaque backdrop (drawn over the 3D board) has no contrast ratio. It is reported as");
        lines.Add("                   not measurable rather than given an invented number.");
        lines.Add(new string('-', 108));
    }

    // =====================================================================
    // C and L: every label
    // =====================================================================

    private static void AuditLabels(GameObject canvas)
    {
        Text[] texts = canvas.GetComponentsInChildren<Text>(true);
        lines.Add($"LABELS ({texts.Length})");

        foreach (Text text in texts)
        {
            string path = RelativePath(text.transform, canvas.transform);
            if (text == null) continue;

            AuditContrast(text, path);
            AuditLabelFit(text, path);
        }

        lines.Add(string.Empty);
    }

    private static void AuditContrast(Text text, string path)
    {
        Image backdrop = FindBackdrop(text.transform);
        float fontSize = text.fontSize;
        bool large = fontSize >= 28f || (fontSize >= 24f && text.fontStyle == FontStyle.Bold);
        float needed = large ? ContrastLarge : ContrastNormal;

        if (backdrop == null)
        {
            notMeasurable++;
            lines.Add($"C  NOT MEASURABLE  {path}  fg={Hex(text.color)} on the board (no opaque backdrop)");
            return;
        }

        Color bg = Composite(backdrop.color, Color.black);
        Color fg = Composite(text.color, bg);
        float ratio = Ratio(fg, bg);

        string measured = $"ratio={ratio:0.00}:1 need={needed:0.0} fg={Hex(text.color)} bg={Hex(backdrop.color)} size={fontSize:0}";

        if (ratio + 0.005f >= needed)
        {
            passed++;
            lines.Add($"C  PASS  {path}  {measured}");
        }
        else
        {
            failed++;
            lines.Add($"C  FAIL  {path}  {measured}");
        }
    }

    private static void AuditLabelFit(Text text, string path)
    {
        Rect rect = text.rectTransform.rect;
        float needed = text.fontSize * LineHeightFactor;

        if (rect.height + 0.5f >= needed)
        {
            passed++;
            lines.Add($"L  PASS  {path}  height={rect.height:0.0} need={needed:0.0} size={text.fontSize:0}");
            return;
        }

        if (text.verticalOverflow == VerticalWrapMode.Truncate)
        {
            failed++;
            lines.Add($"L  FAIL  {path}  CLIPPED height={rect.height:0.0} need={needed:0.0} size={text.fontSize:0}");
        }
        else
        {
            warnings++;
            lines.Add($"L  WARN  {path}  SPILLS height={rect.height:0.0} need={needed:0.0} size={text.fontSize:0} (overflow allowed, draws outside its box)");
        }
    }

    // =====================================================================
    // T: every control a thumb has to hit
    // =====================================================================

    private static void AuditTouchTargets(GameObject canvas)
    {
        Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
        InputField[] fields = canvas.GetComponentsInChildren<InputField>(true);

        lines.Add($"TOUCH TARGETS ({buttons.Length} buttons, {fields.Length} input fields, floor {TouchFloorCanvasPx:0} x {TouchFloorCanvasPx:0} canvas px)");

        foreach (Button button in buttons) AuditTarget(button.gameObject, canvas.transform);
        foreach (InputField field in fields) AuditTarget(field.gameObject, canvas.transform);

        lines.Add(string.Empty);
    }

    private static void AuditTarget(GameObject target, Transform canvasRoot)
    {
        RectTransform rect = target.transform as RectTransform;
        if (rect == null) return;

        string path = RelativePath(target.transform, canvasRoot);
        float width = rect.rect.width;
        float height = rect.rect.height;

        bool ok = width + 0.5f >= TouchFloorCanvasPx && height + 0.5f >= TouchFloorCanvasPx;
        string measured = $"{width:0}x{height:0} px";

        if (ok)
        {
            passed++;
            lines.Add($"T  PASS  {path}  {measured}");
        }
        else
        {
            failed++;
            lines.Add($"T  FAIL  {path}  {measured}  short by {Mathf.Max(0f, TouchFloorCanvasPx - height):0} px of height");
        }
    }

    private static void WriteTotals()
    {
        lines.Add(new string('-', 108));
        lines.Add($"TOTAL: {passed} passed, {failed} failed, {warnings} warnings, {notMeasurable} not measurable");

        if (failed == 0 && warnings == 0)
            lines.Add("Every measurable control meets the three thresholds. That is a floor, not a ceiling: an audit cannot see a");
        else
            lines.Add("Failures above are measurable and fixable; warnings and not-measurable rows are not claimed as passes.");

        lines.Add("an accessible game, and it cannot read a label that is the right size and still says nothing.");
    }

    // =====================================================================
    // Colour maths
    // =====================================================================

    /// <summary>The nearest ancestor Image a label is actually drawn on.</summary>
    private static Image FindBackdrop(Transform from)
    {
        Transform current = from;
        int guard = 0;

        while (current != null && guard++ < 32)
        {
            Image image = current.GetComponent<Image>();
            if (image != null && image.enabled && image.color.a >= BackdropAlpha) return image;
            current = current.parent;
        }

        return null;
    }

    /// <summary>Puts a translucent colour onto an opaque one, as the GPU would.</summary>
    private static Color Composite(Color over, Color under)
    {
        float a = Mathf.Clamp01(over.a);
        return new Color(
            over.r * a + under.r * (1f - a),
            over.g * a + under.g * (1f - a),
            over.b * a + under.b * (1f - a),
            1f);
    }

    private static float Channel(float value)
    {
        value = Mathf.Clamp01(value);
        return value <= 0.03928f ? value / 12.92f : Mathf.Pow((value + 0.055f) / 1.055f, 2.4f);
    }

    private static float Luminance(Color colour)
    {
        return 0.2126f * Channel(colour.r) + 0.7152f * Channel(colour.g) + 0.0722f * Channel(colour.b);
    }

    private static float Ratio(Color a, Color b)
    {
        float la = Luminance(a);
        float lb = Luminance(b);
        float lighter = Mathf.Max(la, lb);
        float darker = Mathf.Min(la, lb);
        return (lighter + 0.05f) / (darker + 0.05f);
    }

    private static string Hex(Color colour)
    {
        Color32 c = colour;
        return $"#{c.r:X2}{c.g:X2}{c.b:X2}";
    }

    private static string RelativePath(Transform target, Transform root)
    {
        List<string> parts = new List<string>();
        Transform current = target;

        while (current != null && current != root)
        {
            parts.Insert(0, current.name);
            current = current.parent;
        }

        return string.Join("/", parts);
    }
}
