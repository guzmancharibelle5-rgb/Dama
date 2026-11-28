using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Week 20: builds the two controls the new features need.
///
/// Menu: Tools > Dama > Week 20 > 1. Build Undo and Hint Buttons
///
/// What it creates, and nothing else:
///   1. <c>GameCanvas/PausePanel/UndoButton</c> - takes back the last turn. It shares the pause
///      panel's last row with SOUND, so the panel keeps four rows and the layout rhythm it already
///      has: UNDO on the left, SOUND on the right.
///   2. <c>GameCanvas/HUDPanel/HintButton</c> - sits immediately left of PAUSE and is filled in by
///      DamaBoard from DamaAI. The HUD is not one of MenuLayout's six panels, so this file places it
///      against PAUSE's own live rect rather than against a hand-typed anchor.
///
/// Re-runnable: every object it owns is removed and rebuilt (the DEF-17-05 lesson: a builder without
/// a destroy loop left a duplicate control set behind on its second run). It ends by running
/// MenuLayout, then saves the scene.
///
/// Sizes and fills follow the audit's floors: both controls clear 96 canvas px, and the pause fill
/// (0.34/0.38/0.46) already measures 3.37:1 against the 0.94-black backdrop.
/// </summary>
public static class Week20Ui
{
    private const string MenuPath = "Tools/Dama/Week 20/1. Build Undo and Hint Buttons";
    private const string ScenePath = "Assets/Scenes/DamaGame.unity";

    private const float TouchFloor = 96f;
    private const float UndoWidth = 272f;
    private const float HintWidth = 160f;
    private const float HintGap = 12f;

    private static readonly Color LevelFill = new Color(0.34f, 0.38f, 0.46f, 1f);   // the pause panel's slate
    private static readonly Color HudFill = new Color(0.20f, 0.20f, 0.25f, 1f);     // the PAUSE button's charcoal

    [MenuItem(MenuPath, false, 51)]
    public static void BuildUndoAndHint()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[Week20Ui] No Canvas in the open scene. Open Assets/Scenes/DamaGame.unity first.");
            return;
        }

        Transform canvasTransform = canvas.transform;
        Transform pausePanel = canvasTransform.Find("PausePanel");
        Transform hud = canvasTransform.Find("HUDPanel");

        if (pausePanel == null || hud == null)
        {
            Debug.LogError($"[Week20Ui] GameCanvas/PausePanel ({pausePanel != null}) or GameCanvas/HUDPanel " +
                           $"({hud != null}) was not found. Run Tools > Dama > Week 17 > 1. Build Release Panels first.");
            return;
        }

        Font font = GetFont();

        // ---- 1. PausePanel/UNDO ------------------------------------------------
        DestroyIfExists(pausePanel, "UndoButton");

        // A provisional box; MenuLayout re-flows the whole panel a few lines below, so the final
        // position is the row rule's, not this one's. Creating it at the audit's floor means the
        // panel is never briefly holding an untappable control.
        GameObject undo = CreateButton("UndoButton", pausePanel, font, "UNDO",
            new Vector2(0.5f, 0.24f), new Vector2(-130f, 0f),
            new Vector2(UndoWidth, TouchFloor), LevelFill, 22);

        // ---- 2. HUDPanel/HINT ---------------------------------------------------
        DestroyIfExists(hud, "HintButton");

        Vector2 hintAnchor = new Vector2(1f, 1f);
        Vector2 hintPosition = new Vector2(-HintWidth - HintGap, -60f);
        Vector2 hintSize = new Vector2(HintWidth, TouchFloor);

        RectTransform pauseRect = hud.Find("PauseButton") as RectTransform;
        if (pauseRect != null)
        {
            // Read PAUSE's own box instead of guessing: the HUD was laid out by hand in Week 13 and
            // is not covered by MenuLayout, so its anchors are the only authority on where a second
            // control fits. x_left is the pause button's left edge measured from the shared anchor;
            // the hint's right edge then lands HintGap to the left of it, in PAUSE's own pivot space.
            hintAnchor = pauseRect.anchorMin;
            float pauseWidth = pauseRect.rect.width;
            float pauseHeight = pauseRect.rect.height;
            Vector2 pivot = pauseRect.pivot;

            float leftEdge = pauseRect.anchoredPosition.x - pivot.x * pauseWidth;
            hintSize = new Vector2(HintWidth, Mathf.Max(TouchFloor, pauseHeight));
            hintPosition = new Vector2(leftEdge - HintGap - hintSize.x * (1f - pivot.x),
                                       pauseRect.anchoredPosition.y);

            Debug.Log($"[Week20Ui] PAUSE rect {pauseWidth:0.0}x{pauseHeight:0.0} at {pauseRect.anchoredPosition} " +
                      $"(anchor {pauseRect.anchorMin}, pivot {pivot}) -> HINT at {hintPosition}, size {hintSize}.");
        }
        else
        {
            Debug.LogWarning("[Week20Ui] HUDPanel/PauseButton was not found; HINT was placed at the fallback " +
                             "top-right anchor and may need to be moved by hand.");
        }

        GameObject hint = CreateButton("HintButton", hud, font, "HINT",
            hintAnchor, hintPosition, hintSize, HudFill, 22);

        RectTransform hintRect = hint.GetComponent<RectTransform>();
        RectTransform undoRect = undo.GetComponent<RectTransform>();
        hintRect.pivot = pauseRect != null ? pauseRect.pivot : new Vector2(0.5f, 0.5f);

        EditorUtility.SetDirty(undo);
        EditorUtility.SetDirty(hint);
        EditorUtility.SetDirty(canvas.gameObject);

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveScene(canvas.gameObject.scene);

        // The pause panel gained a row member, so the one row rule set has to re-flow it. Running it
        // here means a freshly built UNDO cannot end up on a different rhythm from SOUND.
        MenuLayout.Apply();

        Debug.Log($"[Week20Ui] Built PausePanel/UndoButton {undoRect.rect.width:0.0}x{undoRect.rect.height:0.0} " +
                  $"and HUDPanel/HintButton {hintRect.rect.width:0.0}x{hintRect.rect.height:0.0}, " +
                  $"rebalanced the panels, then saved {ScenePath}.");
    }

    // ---- Building blocks ------------------------------------------------------

    private static GameObject CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPosition, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        return go;
    }

    private static GameObject CreateButton(string name, Transform parent, Font font, string label,
        Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color color, int fontSize)
    {
        GameObject go = CreateRect(name, parent, anchor, anchor, anchoredPosition, size);

        Image background = go.AddComponent<Image>();
        background.color = color;

        Button button = go.AddComponent<Button>();
        button.targetGraphic = background;

        GameObject labelObject = CreateRect("Text", go.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Text text = labelObject.AddComponent<Text>();
        text.font = font;
        text.text = label;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        return go;
    }

    private static void DestroyIfExists(Transform parent, string childName)
    {
        int removed = 0;
        Transform existing = parent.Find(childName);

        while (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
            removed++;
            existing = parent.Find(childName);
        }

        if (removed > 0)
            Debug.Log($"[Week20Ui] Removed {removed} previous {childName} object(s) before rebuilding.");
    }

    private static Font GetFont()
    {
        Text existing = Object.FindAnyObjectByType<Text>(FindObjectsInactive.Include);
        if (existing != null && existing.font != null) return existing.font;

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
