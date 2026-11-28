using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Week 19: builds the surfaces the two modes need.
///
/// Menu: Tools > Dama > Week 19 > 1. Build Mode UI
///
/// What it creates, and nothing else:
///   1. <c>GameCanvas/StartPanel/StartButton</c> - relabelled PLAY (object name kept, so every
///      earlier harness and capture tool that measures it keeps working).
///   2. <c>GameCanvas/ModePanel</c>           - PLAYER vs AI, PLAYER vs PLAYER, EASY / NORMAL / HARD,
///                                              then PLAYING AS: RED/BLUE beside BACK on the last row.
///   3. <c>GameCanvas/HUDPanel/ModeLabel</c>  - which mode this match is, filled by DamaBoard.
///
/// It ends by running MenuLayout, which puts the other six panels on the same row rhythm.
///
/// Re-runnable: every object it owns is removed and rebuilt, so running it twice does not duplicate
/// anything. The scene is saved at the end.
///
/// Sizes and fills follow the audit's floors: no control is smaller than 96 canvas px in either
/// direction, and the two fills used here (0.24/0.40/0.66 and 0.34/0.38/0.46) already pass the
/// 3:1 non-text contrast check against the 0.94-black panel backdrop.
/// </summary>
public static class Week19ModeUi
{
    private const string MenuPath = "Tools/Dama/Week 19/1. Build Mode UI";
    private const string ScenePath = "Assets/Scenes/DamaGame.unity";

    // Row geometry, in canvas units. One control height, one gap rule, the block centred in the
    // panel so the space above the title and below the bottom row match. These anchors were
    // computed from the panel's own rect at the 1280x720 reference, not chosen by eye:
    //   block = 56 + 36 + 96 + 96 + 40 + 96 + 96 + 6 gaps of 20 = 636 px of a 683.8 px panel.
    private const float ModeColumnWidth = 520f;
    private const float ModeButtonHeight = 96f;
    private const float ModeButtonWidth = ModeColumnWidth;
    private const float LevelButtonWidth = (ModeColumnWidth - 32f) / 3f;      // 162.7
    private const float HalfRowWidth = (ModeColumnWidth - 16f) / 2f;          // 252

    private const float TitleY = 0.924f;
    private const float SubtitleY = 0.827f;
    private const float VsAiY = 0.702f;
    private const float VsPlayerY = 0.532f;
    private const float DifficultyY = 0.403f;
    private const float LevelRowY = 0.275f;
    private const float BottomRowY = 0.105f;

    private static readonly Color NavFill = new Color(0.24f, 0.40f, 0.66f, 1f);
    private static readonly Color LevelFill = new Color(0.34f, 0.38f, 0.46f, 1f);
    private static readonly Color CaptionColor = new Color(0.82f, 0.86f, 0.90f, 1f);
    private static readonly Color PanelBackdrop = new Color(0f, 0f, 0f, 0.94f);

    [MenuItem(MenuPath, false, 41)]
    public static void BuildModeUi()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[Week19ModeUi] No Canvas in the open scene. Open Assets/Scenes/DamaGame.unity first.");
            return;
        }

        Transform canvasTransform = canvas.transform;
        Transform startPanel = canvasTransform.Find("StartPanel");
        if (startPanel == null)
        {
            Debug.LogError("[Week19ModeUi] GameCanvas/StartPanel was not found.");
            return;
        }

        Font font = GetFont();

        // ---- 1. START becomes PLAY --------------------------------------------
        Transform startButton = startPanel.Find("StartButton");
        if (startButton != null)
        {
            Text label = startButton.Find("Text")?.GetComponent<Text>();
            string was = label != null ? label.text : "(no Text child)";
            if (label != null) label.text = "PLAY";
            EditorUtility.SetDirty(startButton);
            Debug.Log($"[Week19ModeUi] StartPanel/StartButton relabelled '{was}' -> 'PLAY'. " +
                      "The object keeps its name so DamaBoard's wiring and the Week 17/18 captures still find it.");
        }
        else
        {
            Debug.LogWarning("[Week19ModeUi] StartPanel/StartButton was not found; the title screen has no PLAY control.");
        }

        // ---- 2. The mode panel ------------------------------------------------
        DestroyIfExists(canvasTransform, "ModePanel");
        GameObject modePanel = CreatePanel("ModePanel", canvasTransform);

        CreateLabel("TitleText", modePanel.transform, font, "CHOOSE MODE",
            new Vector2(0.5f, TitleY), Vector2.zero, new Vector2(800f, 56f),
            44, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white);

        CreateLabel("SubtitleText", modePanel.transform, font,
            "Two people sharing this device, or one player against the AI.",
            new Vector2(0.5f, SubtitleY), Vector2.zero, new Vector2(980f, 36f),
            22, TextAnchor.MiddleCenter, FontStyle.Normal, CaptionColor);

        CreateButton("VsAiButton", modePanel.transform, font, "PLAYER vs AI",
            new Vector2(0.5f, VsAiY), Vector2.zero, new Vector2(ModeButtonWidth, ModeButtonHeight), NavFill, 24);

        CreateButton("VsPlayerButton", modePanel.transform, font, "PLAYER vs PLAYER",
            new Vector2(0.5f, VsPlayerY), Vector2.zero, new Vector2(ModeButtonWidth, ModeButtonHeight), NavFill, 24);

        CreateLabel("DifficultyLabel", modePanel.transform, font, "AI LEVEL: NORMAL",
            new Vector2(0.5f, DifficultyY), Vector2.zero, new Vector2(640f, 40f),
            24, TextAnchor.MiddleCenter, FontStyle.Bold, CaptionColor);

        CreateButton("EasyButton", modePanel.transform, font, "EASY",
            new Vector2(0.5f, LevelRowY), new Vector2(-(LevelButtonWidth + 16f), 0f),
            new Vector2(LevelButtonWidth, ModeButtonHeight), LevelFill, 24);

        CreateButton("NormalButton", modePanel.transform, font, "NORMAL",
            new Vector2(0.5f, LevelRowY), Vector2.zero, new Vector2(LevelButtonWidth, ModeButtonHeight), LevelFill, 24);

        CreateButton("HardButton", modePanel.transform, font, "HARD",
            new Vector2(0.5f, LevelRowY), new Vector2(LevelButtonWidth + 16f, 0f),
            new Vector2(LevelButtonWidth, ModeButtonHeight), LevelFill, 24);

        // The side control shares the bottom row with BACK: the panel has room for six rows of
        // controls, not seven, and a stacked BACK was the row that used to hang past the edge.
        CreateButton("SideButton", modePanel.transform, font,
            "PLAYING AS: " + (PlayerProfile.PlayerSide == 1 ? "RED" : "BLUE"),
            new Vector2(0.5f, BottomRowY), new Vector2(-(HalfRowWidth + 8f), 0f),
            new Vector2(HalfRowWidth, ModeButtonHeight), LevelFill, 22);

        CreateButton("BackButton", modePanel.transform, font, "BACK",
            new Vector2(0.5f, BottomRowY), new Vector2(HalfRowWidth + 8f, 0f),
            new Vector2(HalfRowWidth, ModeButtonHeight), LevelFill, 24);

        // ---- 3. The HUD's mode caption ---------------------------------------
        Transform hud = canvasTransform.Find("HUDPanel");
        if (hud != null)
        {
            DestroyIfExists(hud, "ModeLabel");

            Text mode = CreateLabel("ModeLabel", hud, font, "PLAYER vs PLAYER",
                new Vector2(0f, 1f), new Vector2(284f, -150f), new Vector2(520f, 40f),
                24, TextAnchor.MiddleLeft, FontStyle.Bold, CaptionColor);
            mode.raycastTarget = false;
        }
        else
        {
            Debug.LogWarning("[Week19ModeUi] GameCanvas/HUDPanel was not found; the HUD will not name the mode.");
        }

        modePanel.SetActive(false);

        EditorUtility.SetDirty(modePanel);
        EditorUtility.SetDirty(canvas.gameObject);

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveScene(canvas.gameObject.scene);

        // The other six panels are laid out by one rule set (MenuLayout); running it here means a
        // freshly built mode panel and a freshly built title screen cannot end up on two rhythms.
        MenuLayout.Apply();

        Debug.Log("[Week19ModeUi] Built StartPanel/StartButton (PLAY), GameCanvas/ModePanel " +
                  "{VsAiButton, VsPlayerButton, EasyButton, NormalButton, HardButton, SideButton, BackButton} and " +
                  $"HUDPanel/ModeLabel, rebalanced the other panels, then saved {ScenePath}.");
    }

    // ---- Building blocks ------------------------------------------------------

    private static GameObject CreatePanel(string name, Transform parent)
    {
        GameObject panel = CreateRect(name, parent, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

        Image backdrop = panel.AddComponent<Image>();
        backdrop.color = PanelBackdrop;
        backdrop.raycastTarget = true;

        return panel;
    }

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

    private static Text CreateLabel(string name, Transform parent, Font font, string content,
        Vector2 anchor, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment,
        FontStyle style, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Text text = go.AddComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        return text;
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
            Debug.Log($"[Week19ModeUi] Removed {removed} previous {childName} object(s) before rebuilding.");
    }

    private static Font GetFont()
    {
        Text existing = Object.FindAnyObjectByType<Text>(FindObjectsInactive.Include);
        if (existing != null && existing.font != null) return existing.font;

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
