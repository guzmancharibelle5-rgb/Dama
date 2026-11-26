using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Week 15: closes the four defects the baseline runs found (M1-M4) in one pass that can be
/// run again without doing the work twice.
///
/// Menu: Tools > Dama > Week 15 > 3. Apply Defect Fixes
///
/// The four rules, in this order (creation first, so anything new is measured by the same run):
///   M1  Size fixes   every caption on a Button below the 20 px floor becomes 22 px bold; every other
///                    label below the 18 px floor becomes 20 px. Only the offenders move.
///   M2  Pause exit   PausePanel/MenuButton joins RestartButtonPause on its anchor line, one button
///                    width to the right, so the pause screen can leave the match.
///   M3  End screen   WinPanel/MenuButtonWin does the same beside RestartButtonWin, and
///                    WinPanel/SummaryText gets the band between WinText and the button row.
///   M4  Fill contrast  a button fill the audit reports below 3.0:1 against its panel is lightened in
///                    its own hue until it clears the floor, and never so far that its white label
///                    drops under 4.5:1.
///
/// The log reports what changed. A second run reports zeros and an unchanged layout hash, which is
/// how idempotence is measured rather than asserted.
/// </summary>
public static class Week15Fixes
{
    private const string Menu = "Tools/Dama/Week 15/3. Apply Defect Fixes";
    private const string SceneName = "DamaGame";
    private const string CanvasName = "GameCanvas";

    // M1. The two floors are A11yAudit's own, so the fix and the audit cannot drift apart:
    // a caption on a Button must be >= 20 px, any other label >= 18 px.
    private const int CaptionFloorPx = 20;
    private const int CaptionFixedPx = 22;
    private const int LabelFloorPx = 18;
    private const int LabelFixedPx = 20;

    // M2 / M3. Same box as every other control on those panels, so it inherits the 96 px floor.
    private const float ButtonHeight = 96f;
    private const float RowOffsetPx = 340f;

    // M4. 3.0:1 is the audit floor; the pass aims just above it so the result does not sit on the
    // boundary, and stops before the white label would fail its own 4.5:1.
    private const float ContrastFloor = 3.0f;
    private const float ContrastTarget = 3.15f;
    private const float WhiteLabelFloor = 4.6f;
    private const float LightenStep = 1.01f;

    private static readonly Color Slate = new Color(0.251f, 0.278f, 0.349f, 1f);   // #404759, the BACK/SOUND slate

    [MenuItem(Menu)]
    public static void Apply()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != SceneName)
        {
            string path = $"Assets/Scenes/{SceneName}.unity";
            if (!File.Exists(path))
            {
                Debug.LogError($"[Week15Fixes] Open {SceneName}.unity first - {path} does not exist.");
                return;
            }

            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Debug.Log($"[Week15Fixes] Opened {path}.");
        }

        GameObject canvas = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == CanvasName) { canvas = root; break; }
        }

        if (canvas == null)
        {
            Debug.LogError($"[Week15Fixes] No '{CanvasName}' in the open scene.");
            return;
        }

        string beforeHash = Fingerprint(canvas);

        Transform pause = canvas.transform.Find("PausePanel");
        Transform win = canvas.transform.Find("WinPanel");

        int added = 0;
        if (pause == null) Debug.LogError("[Week15Fixes] No PausePanel; M2 was not applied.");
        else if (EnsureButton(pause, "MenuButton", "RestartButtonPause", "MENU")) added++;

        if (win == null) Debug.LogError("[Week15Fixes] No WinPanel; M3 was not applied.");
        else
        {
            if (EnsureButton(win, "MenuButtonWin", "RestartButtonWin", "MENU")) added++;
            if (EnsureSummaryText(win)) added++;
        }

        int resized = RaiseTypeSizes(canvas);
        int lifted = LiftFills(canvas);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        string afterHash = Fingerprint(canvas);

        Debug.Log($"[Week15Fixes] Run complete: {added} control(s) created, {resized} label(s) resized, " +
                  $"{lifted} fill(s) lightened. Layout hash {beforeHash} -> {afterHash} " +
                  $"({(beforeHash == afterHash ? "UNCHANGED - this run was a no-op" : "changed")}). Scene saved.");

        Verify();
    }

    /// <summary>
    /// Measures the result of the pass with the audit's own arithmetic and prints it, so the fix
    /// reports a number rather than a claim. It runs after the save and touches nothing.
    /// </summary>
    private static void Verify()
    {
        GameObject canvas = GameObject.Find(CanvasName);
        if (canvas == null) return;

        int captions = 0;
        int labels = 0;
        int belowFloor = 0;
        string shortOf = "";

        foreach (Text text in canvas.GetComponentsInChildren<Text>(true))
        {
            if (text == null) continue;

            bool isCaption = text.transform.parent != null && text.transform.parent.GetComponent<Button>() != null;
            float floor = isCaption ? CaptionFloorPx : LabelFloorPx;
            if (isCaption) captions++; else labels++;

            if (text.fontSize + 0.01f < floor)
            {
                belowFloor++;
                shortOf += $"{RelativePath(text.transform, canvas.transform)}={text.fontSize:0}px(need {floor:0}); ";
            }
        }

        int unreadable = 0;
        string unreadableList = "";

        foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
        {
            Image fill = button.targetGraphic as Image ?? button.GetComponent<Image>();
            Image panel = FindBackdrop(button.transform.parent);
            if (fill == null || panel == null) continue;

            Color panelColor = Composite(panel.color, Color.black);
            float ratio = Ratio(Composite(fill.color, panelColor), panelColor);
            if (ratio + 0.005f >= ContrastFloor) continue;

            unreadable++;
            unreadableList += $"{RelativePath(button.transform, canvas.transform)}={ratio:0.00}:1; ";
        }

        Debug.Log($"[Week15Fixes] Measured after the pass: {captions} caption(s) and {labels} label(s), " +
                  $"{belowFloor} below its size floor{(belowFloor > 0 ? " - " + shortOf : "")}. " +
                  $"{unreadable} button fill(s) below {ContrastFloor:0.0}:1{(unreadable > 0 ? " - " + unreadableList : "")}.");
    }

    /// <summary>
    /// A stable digest of every box, fill and type size under the canvas. A second run must produce the
    /// same digest, which is what makes idempotence a measurement instead of a promise.
    /// </summary>
    private static string Fingerprint(GameObject canvas)
    {
        StringBuilder builder = new StringBuilder();

        foreach (RectTransform rect in canvas.GetComponentsInChildren<RectTransform>(true))
        {
            builder.Append(RelativePath(rect, canvas.transform))
                   .Append(':').Append(rect.anchorMin.x.ToString("F2")).Append(',').Append(rect.anchorMin.y.ToString("F2"))
                   .Append('|').Append(rect.anchoredPosition.x.ToString("F2")).Append(',').Append(rect.anchoredPosition.y.ToString("F2"))
                   .Append('|').Append(rect.sizeDelta.x.ToString("F2")).Append(',').Append(rect.sizeDelta.y.ToString("F2"));

            if (rect.TryGetComponent(out Image image))
                builder.Append('|').Append(ColorUtility.ToHtmlStringRGBA(image.color));

            if (rect.TryGetComponent(out Text text))
                builder.Append('|').Append(text.fontSize.ToString("F0")).Append('|').Append((int)text.fontStyle);
        }

        uint hash = 2166136261u;
        foreach (char c in builder.ToString())
        {
            hash ^= c;
            hash *= 16777619u;
        }

        return hash.ToString("X8");
    }

    // =====================================================================
    // M2 / M3: the two MENU controls
    // =====================================================================

    /// <summary>
    /// Adds a MENU button beside an existing one on the same anchor line. Copying the sibling's anchor
    /// and size is what keeps the row straight: both controls stay point-anchored, both keep the 96 px
    /// floor, and the horizontal offset is larger than a button width, so nothing overlaps.
    /// </summary>
    private static bool EnsureButton(Transform panel, string name, string siblingName, string caption)
    {
        if (panel.Find(name) != null) return false;

        RectTransform template = panel.Find(siblingName) as RectTransform;
        if (template == null)
        {
            Debug.LogError($"[Week15Fixes] {panel.name}/{siblingName} not found; {name} was not added.");
            return false;
        }

        Text fontSource = template.GetComponentInChildren<Text>(true);

        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(panel, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = template.anchorMin;
        rect.anchorMax = template.anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = template.anchoredPosition + new Vector2(RowOffsetPx, 0f);
        rect.sizeDelta = new Vector2(template.sizeDelta.x, ButtonHeight);

        Image background = go.AddComponent<Image>();
        background.color = Slate;

        Button button = go.AddComponent<Button>();
        button.targetGraphic = background;

        GameObject labelObject = new GameObject("Text", typeof(RectTransform));
        labelObject.transform.SetParent(go.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text label = labelObject.AddComponent<Text>();
        label.font = fontSource != null ? fontSource.font : BuiltinFont();
        label.fontSize = CaptionFixedPx;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;
        label.text = caption;

        Debug.Log($"[Week15Fixes] Added {panel.name}/{name} at anchor ({rect.anchorMin.x:0.00}, {rect.anchorMin.y:0.00}) " +
                  $"offset {RowOffsetPx:0} px from {siblingName}, {rect.sizeDelta.x:0} x {rect.sizeDelta.y:0}, reading '{caption}'.");
        return true;
    }

    // =====================================================================
    // M3: the end-screen summary line
    // =====================================================================

    /// <summary>
    /// The band between WinText (bottom edge 0.56) and the button row (top edge 0.41) is the only
    /// place a summary line fits: 0.43-0.54 leaves a 13 px gap above the buttons and 13 px below the
    /// title. DamaBoard writes the sentence at run time; the box is created empty.
    /// </summary>
    private static bool EnsureSummaryText(Transform win)
    {
        if (win.Find("SummaryText") != null) return false;

        Transform restart = win.Find("RestartButtonWin");
        Text fontSource = restart != null ? restart.GetComponentInChildren<Text>(true) : win.GetComponentInChildren<Text>(true);

        GameObject go = new GameObject("SummaryText", typeof(RectTransform));
        go.transform.SetParent(win, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.10f, 0.43f);
        rect.anchorMax = new Vector2(0.90f, 0.54f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text text = go.AddComponent<Text>();
        text.font = fontSource != null ? fontSource.font : BuiltinFont();
        text.fontSize = CaptionFixedPx;
        text.fontStyle = FontStyle.Normal;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.85f, 0.88f, 0.93f, 1f);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.text = string.Empty;

        Debug.Log($"[Week15Fixes] Added WinPanel/SummaryText: anchors x 0.10-0.90, y 0.43-0.54, " +
                  $"{rect.rect.height:0} px tall, {text.fontSize:0} px, centred. DamaBoard fills it when a match ends.");
        return true;
    }

    private static Font BuiltinFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }

    // =====================================================================
    // M1: type sizes
    // =====================================================================

    /// <summary>
    /// Only the controls under a floor move. Raising every caption to one size would redraw screens
    /// the audit already passes, so a caption at 20 px or more is left exactly as the designer set it.
    /// </summary>
    private static int RaiseTypeSizes(GameObject canvas)
    {
        int changed = 0;

        foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
        {
            foreach (Text caption in button.GetComponentsInChildren<Text>(true))
            {
                if (caption == null) continue;
                if (caption.fontSize + 0.01f >= CaptionFloorPx) continue;

                float before = caption.fontSize;
                caption.fontSize = CaptionFixedPx;
                caption.fontStyle = FontStyle.Bold;
                changed++;

                Debug.Log($"[Week15Fixes] size {RelativePath(caption.transform, canvas.transform)}: " +
                          $"{before:0} -> {CaptionFixedPx:0} px bold (caption floor {CaptionFloorPx:0} px)");
            }
        }

        foreach (Text label in canvas.GetComponentsInChildren<Text>(true))
        {
            if (label == null) continue;

            bool isCaption = label.transform.parent != null && label.transform.parent.GetComponent<Button>() != null;
            if (isCaption) continue;
            if (label.fontSize + 0.01f >= LabelFloorPx) continue;

            float before = label.fontSize;
            label.fontSize = LabelFixedPx;
            changed++;

            Debug.Log($"[Week15Fixes] size {RelativePath(label.transform, canvas.transform)}: " +
                      $"{before:0} -> {LabelFixedPx:0} px (label floor {LabelFloorPx:0} px)");
        }

        return changed;
    }

    // =====================================================================
    // M4: non-text contrast of the control fill
    // =====================================================================

    /// <summary>
    /// A control whose fill cannot be told from the panel behind it has no visible edge, so its label
    /// contrast buys nothing. Only a fill the audit reports as failing is touched, and it is lightened
    /// in its own hue - the screen keeps its palette and only the lightness that lost the edge changes.
    /// </summary>
    private static int LiftFills(GameObject canvas)
    {
        int lifted = 0;

        foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
        {
            Image fill = button.targetGraphic as Image ?? button.GetComponent<Image>();
            Text label = button.GetComponentInChildren<Text>(true);
            if (fill == null || label == null) continue;

            Image panel = FindBackdrop(button.transform.parent);
            if (panel == null) continue;   // nothing opaque behind it; the audit reports that as not measurable

            Color panelColor = Composite(panel.color, Color.black);
            float ratio = Ratio(Composite(fill.color, panelColor), panelColor);
            if (ratio + 0.005f >= ContrastFloor) continue;

            Color original = fill.color;
            Color candidate = original;
            int guard = 0;

            while (guard++ < 400)
            {
                Color next = new Color(candidate.r * LightenStep, candidate.g * LightenStep, candidate.b * LightenStep, candidate.a);
                Color nextComposite = Composite(next, panelColor);

                // The edge and the label are in tension: lighten for one, and the white label above
                // it loses contrast. The label wins, because an unreadable caption is worse than a
                // soft edge.
                if (Ratio(Composite(label.color, nextComposite), nextComposite) < WhiteLabelFloor) break;

                candidate = next;
                if (Ratio(nextComposite, panelColor) + 0.005f >= ContrastTarget) break;
            }

            fill.color = candidate;
            lifted++;

            Debug.Log($"[Week15Fixes] contrast {RelativePath(button.transform, canvas.transform)}: " +
                      $"{Hex(original)} ({ratio:0.00}:1) -> {Hex(candidate)} " +
                      $"({Ratio(Composite(candidate, panelColor), panelColor):0.00}:1), floor {ContrastFloor:0.0}:1.");
        }

        return lifted;
    }

    // =====================================================================
    // Colour maths and paths
    // =====================================================================

    private static Image FindBackdrop(Transform from)
    {
        Transform current = from;
        int guard = 0;

        while (current != null && guard++ < 32)
        {
            Image image = current.GetComponent<Image>();
            if (image != null && image.enabled && image.color.a >= 0.5f) return image;
            current = current.parent;
        }

        return null;
    }

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

    private static float Ratio(Color foreground, Color background)
    {
        float a = Luminance(foreground);
        float b = Luminance(background);
        return (Mathf.Max(a, b) + 0.05f) / (Mathf.Min(a, b) + 0.05f);
    }

    private static string Hex(Color colour)
    {
        Color32 c = colour;
        return $"#{c.r:X2}{c.g:X2}{c.b:X2}";
    }

    private static string RelativePath(Transform target, Transform root)
    {
        StringBuilder builder = new StringBuilder();
        System.Collections.Generic.List<string> parts = new System.Collections.Generic.List<string>();
        Transform current = target;

        while (current != null && current != root)
        {
            parts.Insert(0, current.name);
            current = current.parent;
        }

        builder.Append(string.Join("/", parts));
        return builder.ToString();
    }
}
