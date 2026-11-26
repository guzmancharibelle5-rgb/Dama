using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor tool: builds the Week 13 profile UI into the open scene.
/// Menu: Tools > Dama > Build Profile UI
///
/// It creates two things and nothing else:
///   1. <c>GameCanvas/StartPanel/ProfileButton</c> - the PROFILE button under START.
///   2. <c>GameCanvas/ProfilePanel</c> - the panel that <c>ProfilePanel.cs</c> drives.
///
/// Re-runnable: both objects are removed and rebuilt, so running it twice does not
/// duplicate anything. The scene is saved at the end.
/// </summary>
public static class ProfileSceneBuilder
{
    private const string MenuPath = "Tools/Dama/Build Profile UI";
    private const string ScenePath = "Assets/Scenes/DamaGame.unity";

    [MenuItem(MenuPath)]
    public static void BuildProfileUi()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[ProfileSceneBuilder] No Canvas in the open scene. Open Assets/Scenes/DamaGame.unity first.");
            return;
        }

        Transform canvasTransform = canvas.transform;
        Transform startPanel = canvasTransform.Find("StartPanel");
        if (startPanel == null)
        {
            Debug.LogError("[ProfileSceneBuilder] GameCanvas/StartPanel was not found.");
            return;
        }

        Font font = GetFont();

        // --- 1. PROFILE button on the title screen -------------------------------
        DestroyIfExists(startPanel, "ProfileButton");

        GameObject profileButton = CreateButton(
            "ProfileButton", startPanel, font, "PROFILE",
            new Vector2(0.5f, 0.3f), new Vector2(320f, 84f),
            new Color(0.20f, 0.35f, 0.60f, 1f), 30);

        // START is anchored at (0.5, 0.3); the profile button sits directly under it.
        profileButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -104f);

        EditorUtility.SetDirty(profileButton);

        // --- 2. The profile panel ------------------------------------------------
        DestroyIfExists(canvasTransform, "ProfilePanel");

        GameObject panel = CreateRect("ProfilePanel", canvasTransform,
            new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

        Image backdrop = panel.AddComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.94f);
        backdrop.raycastTarget = true;

        panel.AddComponent<ProfilePanel>();

        CreateLabel("TitleText", panel.transform, font, "PROFILE",
            new Vector2(0.5f, 0.905f), new Vector2(600f, 56f), 44, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white);

        CreateLabel("NameLabel", panel.transform, font, "NAME",
            new Vector2(0.5f, 0.82f), new Vector2(420f, 26f), 22, TextAnchor.MiddleLeft, FontStyle.Bold,
            new Color(0.85f, 0.85f, 0.90f));

        CreateInputField("NameInput", panel.transform, font,
            new Vector2(0.5f, 0.745f), new Vector2(420f, 56f));

        GameObject sideButton = CreateButton("SideButton", panel.transform, font, "SIDE: RED",
            new Vector2(0.5f, 0.65f), new Vector2(260f, 52f), new Color(0.25f, 0.35f, 0.55f, 1f), 22);
        RenameLabel(sideButton, "SideLabel");

        CreateLabel("RecordText", panel.transform, font,
            "Matches: 0   Wins: 0   Losses: 0\nCurrent streak: 0   Best streak: 0",
            new Vector2(0.5f, 0.545f), new Vector2(520f, 64f), 22, TextAnchor.MiddleCenter, FontStyle.Normal,
            new Color(0.95f, 0.95f, 0.95f));

        CreateLabel("BestText", panel.transform, font, "Fastest victories\n- none yet -",
            new Vector2(0.5f, 0.40f), new Vector2(520f, 110f), 20, TextAnchor.MiddleCenter, FontStyle.Normal,
            new Color(0.95f, 0.85f, 0.55f));

        CreateLabel("StatusText", panel.transform, font, "Type a name, then SAVE. Nothing leaves this device.",
            new Vector2(0.5f, 0.27f), new Vector2(560f, 40f), 17, TextAnchor.MiddleCenter, FontStyle.Italic,
            new Color(0.75f, 0.80f, 0.85f));

        CreateButton("SaveButton", panel.transform, font, "SAVE",
            new Vector2(0.32f, 0.185f), new Vector2(200f, 54f), new Color(0.15f, 0.45f, 0.20f, 1f), 24);

        GameObject resetButton = CreateButton("ResetButton", panel.transform, font, "RESET PROFILE",
            new Vector2(0.68f, 0.185f), new Vector2(200f, 54f), new Color(0.55f, 0.18f, 0.18f, 1f), 20);
        RenameLabel(resetButton, "ResetLabel");

        CreateButton("BackButton", panel.transform, font, "BACK",
            new Vector2(0.5f, 0.09f), new Vector2(200f, 46f), new Color(0.25f, 0.28f, 0.35f, 1f), 22);

        panel.SetActive(false);

        EditorUtility.SetDirty(panel);
        EditorUtility.SetDirty(canvas.gameObject);

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveScene(canvas.gameObject.scene);

        Debug.Log($"[ProfileSceneBuilder] Built StartPanel/ProfileButton and ProfilePanel, then saved {ScenePath}.");
    }

    // ---- Building blocks -------------------------------------------------------

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
        Vector2 anchor, Vector2 size, int fontSize, TextAnchor alignment, FontStyle style, Color color)
    {
        GameObject go = CreateRect(name, parent, anchor, anchor, Vector2.zero, size);

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
        Vector2 anchor, Vector2 size, Color color, int fontSize)
    {
        GameObject go = CreateRect(name, parent, anchor, anchor, Vector2.zero, size);

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

    private static void CreateInputField(string name, Transform parent, Font font,
        Vector2 anchor, Vector2 size)
    {
        GameObject go = CreateRect(name, parent, anchor, anchor, Vector2.zero, size);

        Image background = go.AddComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.12f);

        InputField field = go.AddComponent<InputField>();
        field.lineType = InputField.LineType.SingleLine;
        field.characterLimit = PlayerProfile.MaxNameLength;
        field.targetGraphic = background;

        GameObject textObject = CreateRect("Text", go.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.offsetMin = new Vector2(14f, 4f);
        textRect.offsetMax = new Vector2(-14f, -4f);

        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = 30;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.white;
        text.supportRichText = false;
        text.raycastTarget = false;

        GameObject placeholderObject = CreateRect("Placeholder", go.transform, Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero);
        RectTransform placeholderRect = placeholderObject.GetComponent<RectTransform>();
        placeholderRect.offsetMin = new Vector2(14f, 4f);
        placeholderRect.offsetMax = new Vector2(-14f, -4f);

        Text placeholder = placeholderObject.AddComponent<Text>();
        placeholder.font = font;
        placeholder.fontSize = 28;
        placeholder.alignment = TextAnchor.MiddleLeft;
        placeholder.color = new Color(1f, 1f, 1f, 0.35f);
        placeholder.text = "Type your name";
        placeholder.raycastTarget = false;

        field.textComponent = text;
        field.placeholder = placeholder;
    }

    /// <summary>Renames a button's label child so ProfilePanel can retitle it at runtime.</summary>
    private static void RenameLabel(GameObject button, string newName)
    {
        Transform label = button.transform.Find("Text");
        if (label != null) label.name = newName;
    }

    private static void DestroyIfExists(Transform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        if (existing == null) return;

        // Undo is not needed here; the builder is the only owner of these objects.
        Object.DestroyImmediate(existing.gameObject);
        Debug.Log($"[ProfileSceneBuilder] Removed the previous {childName} before rebuilding.");
    }

    private static Font GetFont()
    {
        Text existing = Object.FindAnyObjectByType<Text>(FindObjectsInactive.Include);
        if (existing != null && existing.font != null) return existing.font;

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
