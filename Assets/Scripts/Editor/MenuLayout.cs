using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Week 19: puts every title/overlay menu on one layout rhythm.
///
/// Menu: Tools > Dama > Week 19 > 3. Rebalance Menus
///
/// The panels were built over six weeks, each one placing its own children at fractional
/// anchors with fixed pixel sizes. Two things went wrong as a result: the blocks sat at
/// uneven heights inside the panel (a control could hang a few pixels past the bottom edge),
/// and the gaps between controls differed from panel to panel because each builder chose its
/// own anchors. This pass re-flows the EXISTING children of six panels into rows: a fixed
/// row height, one gap rule, the block centred so the top and bottom margins are equal.
///
/// It never renames, reparents, creates or destroys an object, so every name lookup in
/// DamaBoard, ProfilePanel and the Week 13-18 harnesses keeps resolving.
///
/// A row is a list of children that share a horizontal line. A slot with a
/// weight above zero is stretched into the panel's content column; a slot with weight zero
/// keeps the size it already had (captions and body text are never squeezed into a column
/// narrower than their text). A row with height 0 takes the tallest child's own height.
///
/// ModePanel is deliberately NOT in this table: Week19ModeUi lays it out with its own six
/// rows, because it also has to add the side control, and that builder calls Apply() at the
/// end so both passes agree on the gap rule.
/// </summary>
public static class MenuLayout
{
    private const string MenuPath = "Tools/Dama/Week 19/3. Rebalance Menus";
    private const string ScenePath = "Assets/Scenes/DamaGame.unity";

    /// <summary>The one control height every panel uses (the audit's floor is 96).</summary>
    private const float RowHeight = 96f;

    /// <summary>Smallest gap a row may be squeezed to before the layout refuses to shrink further.</summary>
    private const float MinGap = 12f;

    /// <summary>A panel with room to spare spends it here rather than leaving one huge hole.</summary>
    private const float MaxGap = 36f;

    private const float SideMargin = 24f;
    private const float InnerGap = 16f;

    private sealed class Row
    {
        public string[] Names;
        public float[] Weights;
        public float Height;

        /// <summary>A caption or body block: keeps its own size, only its line moves.</summary>
        public static Row Keep(string name) =>
            new Row { Names = new[] { name }, Weights = new[] { 0f }, Height = 0f };

        /// <summary>One control stretched across the content column.</summary>
        public static Row One(string name) =>
            new Row { Names = new[] { name }, Weights = new[] { 1f }, Height = RowHeight };

        /// <summary>Several controls side by side, sharing the column by weight.</summary>
        public static Row Across(float height, params (string Name, float Weight)[] slots)
        {
            string[] names = new string[slots.Length];
            float[] weights = new float[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                names[i] = slots[i].Name;
                weights[i] = slots[i].Weight;
            }

            return new Row { Names = names, Weights = weights, Height = height };
        }
    }

    private sealed class PanelSpec
    {
        public string Name;
        public float ColumnWidth;

        /// <summary>Space reserved at the top of the panel that no row may enter (a corner caption).</summary>
        public float TopInset;

        public Row[] Rows;
    }

    private static readonly PanelSpec[] Panels =
    {
        new PanelSpec
        {
            Name = "StartPanel",
            ColumnWidth = 1040f,

            // VersionLabel is a 34 px caption pinned to the top-left corner. 48 px keeps the block
            // clear of it, which is what stops the audit finding a title/caption overlap.
            TopInset = 48f,
            Rows = new[]
            {
                Row.Keep("TitleText"),
                Row.Keep("InstructionsText"),
                Row.Across(RowHeight, ("HowToButton", 1f), ("StartButton", 1.4f), ("CreditsButton", 1f)),
                Row.Across(RowHeight, ("ProfileButton", 1f), ("ExitButton", 1f))
            }
        },
        new PanelSpec
        {
            Name = "PausePanel",
            ColumnWidth = 560f,
            Rows = new[]
            {
                Row.Keep("PauseTitleText"),
                Row.One("ResumeButton"),
                Row.Across(RowHeight, ("RestartButtonPause", 1f), ("MenuButton", 1f)),
                Row.Across(RowHeight, ("UndoButton", 1f), ("SoundButton", 1f))
            }
        },
        new PanelSpec
        {
            Name = "WinPanel",
            ColumnWidth = 560f,
            Rows = new[]
            {
                Row.Keep("WinText"),
                Row.Keep("SummaryText"),
                Row.Across(RowHeight, ("RestartButtonWin", 1f), ("MenuButtonWin", 1f))
            }
        },
        new PanelSpec
        {
            Name = "ProfilePanel",
            ColumnWidth = 560f,
            Rows = new[]
            {
                Row.Keep("TitleText"),
                Row.Keep("NameLabel"),
                Row.Across(RowHeight, ("NameInput", 1f), ("SideButton", 0.62f)),
                Row.Keep("RecordText"),
                Row.Keep("BestText"),
                Row.Keep("StatusText"),
                Row.Across(RowHeight, ("BackButton", 1f), ("SaveButton", 1f), ("ResetButton", 1f))
            }
        },
        new PanelSpec
        {
            Name = "HowToPanel",
            ColumnWidth = 1120f,
            Rows = new[]
            {
                Row.Keep("TitleText"),
                Row.Keep("BodyText"),
                Row.Keep("BackButton")
            }
        },
        new PanelSpec
        {
            Name = "CreditsPanel",
            ColumnWidth = 1120f,
            Rows = new[]
            {
                Row.Keep("TitleText"),
                Row.Keep("BodyText"),
                Row.Keep("BuildText"),
                Row.Keep("BackButton")
            }
        }
    };

    [MenuItem(MenuPath, false, 43)]
    public static void Apply()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != "DamaGame")
        {
            if (!System.IO.File.Exists(ScenePath))
            {
                Debug.LogError($"[MenuLayout] Open DamaGame.unity first - {ScenePath} does not exist.");
                return;
            }

            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log($"[MenuLayout] Opened {ScenePath}.");
        }

        GameObject canvas = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "GameCanvas") { canvas = root; break; }
        }

        if (canvas == null)
        {
            Debug.LogError("[MenuLayout] No 'GameCanvas' in the open scene.");
            return;
        }

        // A stale CanvasScaler is the one way this pass can measure the wrong panel. In the editor
        // the scaler only re-applies when the Game view renders, so a session that opens with the
        // Game view hidden leaves the canvas at screen pixels (measured here: 767.3 x 389.3, scale
        // 1.0) instead of the design surface (1347.7 x 683.8, scale 0.854) - and every row rule then
        // computes against a panel two thirds of its real height. Re-enabling the component makes it
        // handle the scale immediately; nothing in the scene is rewritten except Canvas.scaleFactor,
        // which the scaler owns and overwrites again on play.
        EnsureCanvasScaled(canvas);
        Canvas.ForceUpdateCanvases();

        int panels = 0;
        int moved = 0;

        foreach (PanelSpec spec in Panels)
        {
            if (LayoutPanel(canvas.transform, spec, out int changed)) panels++;
            moved += changed;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[MenuLayout] Rebalanced {panels} panel(s), moved {moved} control(s), then saved {ScenePath}.");
    }

    // =====================================================================
    // One panel
    // =====================================================================

    /// <summary>
    /// Re-applies the CanvasScaler when the editor has not rendered the Game view since the canvas
    /// was opened. Without this, <c>rect</c> is read in screen pixels rather than in canvas units.
    /// </summary>
    private static void EnsureCanvasScaled(GameObject canvas)
    {
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) return;

        Canvas component = canvas.GetComponent<Canvas>();
        if (component == null || !Mathf.Approximately(component.scaleFactor, 1f)) return;

        scaler.enabled = false;
        scaler.enabled = true;

        Debug.Log($"[MenuLayout] CanvasScaler was stale (scaleFactor 1.0); re-applied it at " +
                  $"{component.scaleFactor:0.000} so the panel rects are measured in canvas units.");
    }

    private static bool LayoutPanel(Transform canvas, PanelSpec spec, out int changed)
    {
        changed = 0;

        Transform panel = canvas.Find(spec.Name);
        if (panel == null)
        {
            Debug.LogWarning($"[MenuLayout] GameCanvas/{spec.Name} was not found; skipped.");
            return false;
        }

        RectTransform panelRect = panel as RectTransform;
        if (panelRect == null) return false;

        float panelWidth = panelRect.rect.width;
        float panelHeight = panelRect.rect.height;
        if (panelWidth < 1f || panelHeight < 1f)
        {
            Debug.LogWarning($"[MenuLayout] GameCanvas/{spec.Name} has no measurable rect yet; skipped.");
            return false;
        }

        float column = Mathf.Min(spec.ColumnWidth, panelWidth - 2f * SideMargin);

        // ---- Resolve each row against what the scene actually contains ----------
        List<Row> rows = new List<Row>();
        List<List<RectTransform>> slots = new List<List<RectTransform>>();
        List<List<float>> weights = new List<List<float>>();
        List<float> heights = new List<float>();
        HashSet<RectTransform> placed = new HashSet<RectTransform>();

        foreach (Row row in spec.Rows)
        {
            List<RectTransform> found = new List<RectTransform>();
            List<float> foundWeights = new List<float>();

            for (int i = 0; i < row.Names.Length; i++)
            {
                Transform child = panel.Find(row.Names[i]);
                if (child is not RectTransform rect)
                {
                    Debug.LogWarning($"[MenuLayout] {spec.Name}/{row.Names[i]} is missing; the row keeps its other slots.");
                    continue;
                }

                found.Add(rect);
                foundWeights.Add(row.Weights[i]);
                placed.Add(rect);
            }

            if (found.Count == 0) continue;

            float height = row.Height;
            if (height <= 0f)
            {
                height = 0f;
                for (int i = 0; i < found.Count; i++) height = Mathf.Max(height, found[i].rect.height);
            }

            rows.Add(row);
            slots.Add(found);
            weights.Add(foundWeights);
            heights.Add(height);
        }

        if (rows.Count == 0)
        {
            Debug.LogWarning($"[MenuLayout] {spec.Name} had no recognised children; left alone.");
            return false;
        }

        // ---- The gap: the spare space, shared, within the two bounds -----------
        float needed = 0f;
        foreach (float height in heights) needed += height;

        int gaps = rows.Count - 1;
        float gap = MinGap;
        float usable = panelHeight - spec.TopInset;

        if (gaps > 0)
        {
            float spare = (usable - 2f * SideMargin - needed) / gaps;
            gap = Mathf.Clamp(spare, MinGap, MaxGap);
        }

        float block = needed + gap * gaps;
        float margin = spec.TopInset + (usable - block) * 0.5f;

        if (margin < 0f)
        {
            Debug.LogError($"[MenuLayout] {spec.Name} needs {block:0.0} px of content but its rect is {panelHeight:0.0} px tall. " +
                           "A row is too tall for this panel; the block is placed from the top and may clip.");
            margin = 0f;
        }

        // ---- Vertical placement, from the top of the block down ---------------
        float top = panelHeight * 0.5f - margin;
        float cursor = top;

        for (int r = 0; r < rows.Count; r++)
        {
            float height = heights[r];
            float centreY = cursor - height * 0.5f;
            float anchorY = 0.5f + centreY / panelHeight;

            PlaceRow(slots[r], weights[r], rowSpan: column, keepLimit: panelWidth - 2f * SideMargin,
                rowY: anchorY, rowHeight: height, spec.Name, ref changed, placed);

            cursor -= height + gap;
        }

        // ---- Report what it measured ----------------------------------------
        float smallestW = float.MaxValue;
        float smallestH = float.MaxValue;
        int outside = 0;

        foreach (List<RectTransform> row in slots)
        {
            foreach (RectTransform rect in row)
            {
                smallestW = Mathf.Min(smallestW, rect.rect.width);
                smallestH = Mathf.Min(smallestH, rect.rect.height);

                if (!InsidePanel(rect, panelRect)) outside++;
            }
        }

        int untouched = 0;
        foreach (Transform child in panel)
        {
            if (child is RectTransform rect && !placed.Contains(rect)) untouched++;
        }

        Debug.Log($"[MenuLayout] {spec.Name}: {rows.Count} row(s), block {block:0.0} px of {panelHeight:0.0} px, " +
                  $"margins {margin:0.0}/{margin:0.0}, gap {gap:0.0} px, column {column:0.0} px, " +
                  $"smallest control {smallestW:0}x{smallestH:0}, outside the panel {outside}, " +
                  $"{(untouched > 0 ? $"{untouched} child(ren) not in a row (left where they were)" : "every child placed")}.");

        if (outside > 0)
            Debug.LogError($"[MenuLayout] {spec.Name}: {outside} control(s) sit outside the panel rect after the pass.");

        return true;
    }

    /// <summary>
    /// Places one row's slots horizontally. A single slot is centred whatever its width, so a
    /// caption or a BACK button sits on the panel's middle line rather than on the column's left
    /// edge; a stretched slot fills the column; a slot that keeps its own size never exceeds the
    /// panel's inner width.
    /// </summary>
    private static void PlaceRow(List<RectTransform> slots, List<float> weights, float rowSpan, float keepLimit,
        float rowY, float rowHeight, string panelName, ref int changed, HashSet<RectTransform> placed)
    {
        int count = slots.Count;
        float[] widths = new float[count];

        if (count == 1)
        {
            widths[0] = weights[0] > 0f ? rowSpan : Mathf.Min(slots[0].rect.width, keepLimit);
        }
        else
        {
            float own = 0f;
            float totalWeight = 0f;

            for (int i = 0; i < count; i++)
            {
                if (weights[i] > 0f)
                {
                    totalWeight += weights[i];
                    continue;
                }

                widths[i] = Mathf.Min(slots[i].rect.width, keepLimit);
                own += widths[i];
            }

            float space = rowSpan - InnerGap * (count - 1) - own;
            if (space < 0f) space = 0f;

            float unit = totalWeight > 0f ? space / totalWeight : 0f;

            for (int i = 0; i < count; i++)
                if (weights[i] > 0f) widths[i] = unit * weights[i];
        }

        float cursor = count == 1 ? -widths[0] * 0.5f : -rowSpan * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float centreX = cursor + widths[i] * 0.5f;
            cursor += widths[i] + InnerGap;

            RectTransform rect = slots[i];
            Vector2 anchor = new Vector2(0.5f, rowY);
            Vector2 size = new Vector2(widths[i], rowHeight);
            Vector2 position = new Vector2(centreX, 0f);

            bool already = rect.anchorMin == anchor && rect.anchorMax == anchor &&
                           rect.pivot == new Vector2(0.5f, 0.5f) &&
                           Vector2.Distance(rect.anchoredPosition, position) < 0.01f &&
                           Vector2.Distance(rect.sizeDelta, size) < 0.01f;

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            EditorUtility.SetDirty(rect);

            if (!already) changed++;

            placed.Add(rect);

            if (widths[i] < 96f)
                Debug.LogWarning($"[MenuLayout] {panelName}/{rect.name} is {widths[i]:0} px wide, under the 96 px touch floor.");
        }
    }

    /// <summary>The control's box expressed in its panel's local space, whatever its anchors.</summary>
    private static bool InsidePanel(RectTransform rect, RectTransform panel)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);

        Rect bounds = panel.rect;
        const float slack = 0.6f;

        for (int i = 0; i < 4; i++)
        {
            Vector3 local = panel.InverseTransformPoint(corners[i]);

            if (local.x < bounds.xMin - slack || local.x > bounds.xMax + slack) return false;
            if (local.y < bounds.yMin - slack || local.y > bounds.yMax + slack) return false;
        }

        return true;
    }
}
