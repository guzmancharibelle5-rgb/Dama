using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Week 14: applies the accessibility fixes the audit found, from one rule set, and can be run
/// again without doing the work twice.
///
/// Menu: Tools > Dama > Week 14 > 2. Apply Accessibility Fixes
///
/// Four rules, in this order:
///   1. Contrast   every button fill whose own label is below 4.5:1 is darkened, in its own hue,
///                 until the label passes. Nothing else is recoloured, and a button that already
///                 passes is never touched.
///   2. SOUND      a SOUND: ON / SOUND: OFF button is added to the pause panel, once.
///   3. Touch      every Button and InputField below 96 canvas px tall is raised to the floor,
///                 growing downward so the top edge - and therefore the spacing above it - is kept.
///   4. Spacing    the profile panel's BACK button joins the SAVE / RESET row, because three
///                 96-px controls no longer fit in the vertical space they used to share; then any
///                 control that ends up closer than 12 px to the thing above it is nudged down.
///
/// The run log reports what it changed. A second run reports zeros, which is how idempotence is
/// demonstrated rather than asserted.
/// </summary>
public static class AccessibilityBuild
{
    private const string Menu = "Tools/Dama/Week 14/2. Apply Accessibility Fixes";
    private const string SceneName = "DamaGame";
    private const string CanvasName = "GameCanvas";

    private const float TouchFloorCanvasPx = 96f;
    private const float ColumnGap = 12f;
    private const float BottomMargin = 12f;
    private const float ContrastTarget = 4.5f;

    [MenuItem(Menu)]
    public static void Apply()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != SceneName)
        {
            string path = $"Assets/Scenes/{SceneName}.unity";
            if (!File.Exists(path))
            {
                Debug.LogError($"[AccessibilityBuild] Open {SceneName}.unity first - {path} does not exist.");
                return;
            }

            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Debug.Log($"[AccessibilityBuild] Opened {path}.");
        }

        GameObject canvas = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == CanvasName) { canvas = root; break; }
        }

        if (canvas == null)
        {
            Debug.LogError($"[AccessibilityBuild] No '{CanvasName}' in the open scene.");
            return;
        }

        string beforeHash = Fingerprint(canvas);

        int retinted = FixContrast(canvas);
        bool addedSound = EnsureSoundButton(canvas);
        int raised = RaiseToFloor(canvas);
        int moved = MoveBackIntoRow(canvas) + MoveNameRow(canvas);
        int respaced = DeOverlap(canvas);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        string after = Fingerprint(canvas);

        Debug.Log($"[AccessibilityBuild] Run complete: {retinted} fill(s) darkened, " +
                  $"sound button {(addedSound ? "added" : "already present")}, {raised} control(s) raised to {TouchFloorCanvasPx:0} px, " +
                  $"{moved} moved into a row, {respaced} re-spaced. Layout hash {beforeHash} -> {after} " +
                  $"({(beforeHash == after ? "UNCHANGED - this run was a no-op" : "changed")}). Scene saved.");
    }

    /// <summary>
    /// A stable digest of every box and fill under the canvas. Comparing it before and after a run
    /// is what turns "the script is idempotent" from a claim into a measurement: a second run must
    /// produce the same digest.
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
    // 1. Contrast
    // =====================================================================

    private static int FixContrast(GameObject canvas)
    {
        int retinted = 0;

        foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
        {
            Image fill = button.targetGraphic as Image ?? button.GetComponent<Image>();
            Text label = button.transform.Find("Text")?.GetComponent<Text>();
            if (fill == null || label == null) continue;
            if (label.text == null || label.text.Trim().Length == 0) continue;

            bool large = label.fontSize >= 28f || (label.fontSize >= 24f && label.fontStyle == FontStyle.Bold);
            float needed = large ? 3f : ContrastTarget;

            float before = Ratio(label.color, fill.color);
            if (before + 0.005f >= needed) continue;

            Color original = fill.color;
            Color candidate = original;
            int guard = 0;

            // Darken in the button's own hue rather than swapping it for a stock colour: the screen
            // keeps its palette, and only the lightness that made the label unreadable changes.
            while (Ratio(label.color, candidate) + 0.005f < needed && guard++ < 200)
            {
                candidate = new Color(candidate.r * 0.97f, candidate.g * 0.97f, candidate.b * 0.97f, candidate.a);
            }

            fill.color = candidate;
            retinted++;

            Debug.Log($"[AccessibilityBuild] contrast {RelativePath(button.transform, canvas.transform)}: " +
                      $"{Hex(original)} ({before:0.00}:1) -> {Hex(candidate)} ({Ratio(label.color, candidate):0.00}:1), need {needed:0.0}:1");
        }

        return retinted;
    }

    // =====================================================================
    // 2. The SOUND control
    // =====================================================================

    private static bool EnsureSoundButton(GameObject canvas)
    {
        Transform pause = canvas.transform.Find("PausePanel");
        if (pause == null)
        {
            Debug.LogWarning("[AccessibilityBuild] No PausePanel; the SOUND control was not added.");
            return false;
        }

        if (pause.Find("SoundButton") != null)
        {
            RefreshSoundLabel(pause);
            return false;
        }

        Text template = pause.Find("ResumeButton/Text")?.GetComponent<Text>()
                        ?? canvas.GetComponentInChildren<Text>(true);

        GameObject go = new GameObject("SoundButton", typeof(RectTransform));
        go.transform.SetParent(pause, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.16f);
        rect.anchorMax = new Vector2(0.5f, 0.16f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(320f, TouchFloorCanvasPx);

        Image background = go.AddComponent<Image>();
        background.color = new Color(0.251f, 0.278f, 0.349f, 1f);   // the BACK button's slate

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
        label.font = template != null ? template.font : BuiltinFont();
        label.fontSize = 22;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;

        RefreshSoundLabel(pause);

        Debug.Log($"[AccessibilityBuild] Added PausePanel/SoundButton (320 x {TouchFloorCanvasPx:0}) reading '{label.text}'.");
        return true;
    }

    /// <summary>Reads the stored preference and spells the state in words.</summary>
    private static void RefreshSoundLabel(Transform pause)
    {
        Text label = pause.Find("SoundButton/Text")?.GetComponent<Text>();
        if (label == null) return;

        bool on = PlayerPrefs.GetInt(GameFeedback.SoundPrefsKey, 1) == 1;
        label.text = GameFeedback.SoundLabel(on);
    }

    private static Font BuiltinFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }

    // =====================================================================
    // 3. Touch targets
    // =====================================================================

    private static int RaiseToFloor(GameObject canvas)
    {
        int raised = 0;

        List<RectTransform> targets = new List<RectTransform>();
        foreach (Button button in canvas.GetComponentsInChildren<Button>(true)) targets.Add(button.transform as RectTransform);
        foreach (InputField field in canvas.GetComponentsInChildren<InputField>(true)) targets.Add(field.transform as RectTransform);

        foreach (RectTransform rect in targets)
        {
            if (rect == null) continue;

            float height = rect.rect.height;
            if (height + 0.5f >= TouchFloorCanvasPx) continue;

            float delta = TouchFloorCanvasPx - height;
            float width = rect.rect.width;

            rect.sizeDelta = new Vector2(width, TouchFloorCanvasPx);

            // Point-anchored controls grow from their centre, which would eat the gap above them.
            // Moving the centre down by half the growth keeps the top edge exactly where it was.
            if (rect.anchorMin == rect.anchorMax)
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y - delta * 0.5f);

            raised++;
            Debug.Log($"[AccessibilityBuild] touch {RelativePath(rect, canvas.transform)}: {width:0}x{height:0} -> {width:0}x{TouchFloorCanvasPx:0}");
        }

        return raised;
    }

    // =====================================================================
    // 4a. The profile row
    // =====================================================================

    /// <summary>
    /// Three 200-px controls at 96 px tall no longer fit inside the profile panel stacked
    /// vertically, so BACK joins SAVE and RESET on one row: BACK left, SAVE centre (primary,
    /// largest target), RESET right, away from SAVE because it is the destructive one.
    /// </summary>
    private static int MoveBackIntoRow(GameObject canvas)
    {
        Transform profile = canvas.transform.Find("ProfilePanel");
        if (profile == null) return 0;

        int moved = 0;
        moved += Place(profile, "BackButton", 0.22f);
        moved += Place(profile, "SaveButton", 0.50f);
        moved += Place(profile, "ResetButton", 0.78f);

        return moved;
    }

    /// <summary>
    /// The name field and the side toggle no longer fit stacked: 96 + 96 plus gaps does not exist
    /// in the ~140 px band between NameLabel and RecordText. Side by side they both keep their 96 px
    /// and the band gets shorter than it was, so nothing below has to move to make room.
    /// </summary>
    private static int MoveNameRow(GameObject canvas)
    {
        Transform profile = canvas.transform.Find("ProfilePanel");
        if (profile == null) return 0;

        RectTransform panelRect = profile as RectTransform;
        if (panelRect == null) return 0;

        const float nameWidth = 420f;
        const float sideWidth = 260f;
        const float gap = 24f;

        float panelWidth = panelRect.rect.width;
        if (panelWidth <= 1f) return 0;

        float left = (panelWidth - (nameWidth + gap + sideWidth)) * 0.5f;
        float nameAnchorX = (left + nameWidth * 0.5f) / panelWidth;
        float sideAnchorX = (left + nameWidth + gap + sideWidth * 0.5f) / panelWidth;

        int moved = 0;
        moved += Place(profile, "NameInput", nameAnchorX, new Vector2(nameWidth, TouchFloorCanvasPx), 0.72f);
        moved += Place(profile, "SideButton", sideAnchorX, new Vector2(sideWidth, TouchFloorCanvasPx), 0.72f);

        return moved;
    }

    private static int Place(Transform panel, string name, float anchorX)
    {
        return Place(panel, name, anchorX, new Vector2(200f, TouchFloorCanvasPx), 0.19f);
    }

    private static int Place(Transform panel, string name, float anchorX, Vector2 size, float anchorY)
    {
        RectTransform rect = panel.Find(name) as RectTransform;
        if (rect == null) return 0;

        Vector2 anchor = new Vector2(anchorX, anchorY);

        bool changed = rect.anchorMin != anchor || rect.anchorMax != anchor ||
                       rect.anchoredPosition != Vector2.zero || rect.sizeDelta != size;

        if (!changed) return 0;

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        Debug.Log($"[AccessibilityBuild] row {name}: anchor ({anchorX:0.000}, {anchorY:0.00}), {size.x:0} x {size.y:0}");
        return 1;
    }

    // =====================================================================
    // 4b. Spacing
    // =====================================================================

    /// <summary>
    /// Walks each panel from its topmost child downwards and pushes a control down when it ends up
    /// closer than 12 px to the thing above it. Labels are treated as obstacles but are never
    /// moved - they are the builder's business, and nudging a caption to make room for a button
    /// would be the tail wagging the dog.
    /// </summary>
    private static int DeOverlap(GameObject canvas)
    {
        int pushed = 0;
        Transform canvasRect = canvas.transform;

        foreach (Transform panel in canvas.transform)
        {
            List<RectTransform> children = new List<RectTransform>();
            foreach (Transform child in panel)
            {
                if (child is RectTransform rect) children.Add(rect);
            }

            if (children.Count < 2) continue;

            // Sorted by their TOP EDGE IN PANEL SPACE. A RectTransform's own rect is pivot-local,
            // so two controls in different corners can have identical rect.yMax and still be
            // nowhere near each other; the panel-space box is the only one worth comparing.
            children.Sort((a, b) => PanelRect(b, panel as RectTransform).yMax.CompareTo(PanelRect(a, panel as RectTransform).yMax));

            Rect previous = default;
            bool hasPrevious = false;

            foreach (RectTransform current in children)
            {
                Rect box = PanelRect(current, panel as RectTransform);

                if (hasPrevious && previous.xMin < box.xMax && box.xMin < previous.xMax)
                {
                    float gap = previous.yMin - box.yMax;

                    if (gap < ColumnGap - 0.05f)
                    {
                        bool movable = current.GetComponent<Button>() != null || current.GetComponent<InputField>() != null;

                        if (movable)
                        {
                            float shift = ColumnGap - gap;

                            // Never push a control through the floor of its panel to satisfy a gap:
                            // a smaller gap is a cosmetic miss, an off-screen button is a defect.
                            // Boxes are pivot-local, so the panel's floor is its own rect.yMin, not 0.
                            float panelFloor = (panel as RectTransform).rect.yMin + BottomMargin;
                            float room = box.yMin - panelFloor;
                            if (shift > room)
                            {
                                Debug.LogWarning($"[AccessibilityBuild] spacing {RelativePath(current, canvasRect)}: wanted {shift:0.0} px more room but only {room:0.0} px is left; " +
                                                 "clamped to the bottom margin.");
                                shift = Mathf.Max(0f, room);
                            }

                            if (shift > 0.05f)
                            {
                                if (current.anchorMin != current.anchorMax)
                                {
                                    Debug.LogWarning($"[AccessibilityBuild] {RelativePath(current, canvasRect)} is stretched and cannot be nudged vertically; left alone.");
                                }
                                else
                                {
                                    // A control in a row must not move on its own: three buttons on the
                                    // same anchor line are one visual object, and nudging only the first
                                    // leaves the row stepped. Everything sharing this anchor line moves.
                                    int affected = ShiftRow(panel, current, shift);

                                    pushed++;
                                    Debug.Log($"[AccessibilityBuild] spacing {RelativePath(current, canvasRect)}: " +
                                              $"gap {gap:0.0} -> {gap + shift:0.0} px, moved down {shift:0.0} px " +
                                              $"({affected} control(s) on that line)");

                                    box = PanelRect(current, panel as RectTransform);
                                }
                            }
                        }
                    }
                }

                previous = box;
                hasPrevious = true;
            }
        }

        return pushed;
    }

    /// <summary>Moves every interactive control sharing this control's anchor line down by the same amount.</summary>
    private static int ShiftRow(Transform panel, RectTransform reference, float shift)
    {
        int affected = 0;

        foreach (Transform child in panel)
        {
            if (child is not RectTransform rect) continue;
            if (Mathf.Abs(rect.anchorMin.y - reference.anchorMin.y) > 0.001f) continue;
            if (rect.anchorMin != rect.anchorMax) continue;

            bool interactive = rect.GetComponent<Button>() != null || rect.GetComponent<InputField>() != null;
            if (!interactive) continue;

            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y - shift);
            affected++;
        }

        return affected;
    }

    /// <summary>The control's box expressed in its panel's local space, whatever its anchors.</summary>
    private static Rect PanelRect(RectTransform rect, RectTransform panel)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);

        for (int i = 0; i < 4; i++)
        {
            Vector3 local = panel.InverseTransformPoint(corners[i]);
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }

        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    // =====================================================================
    // Colour maths and paths
    // =====================================================================

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
        float lighter = Mathf.Max(a, b);
        float darker = Mathf.Min(a, b);
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
