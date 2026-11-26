using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Week 17: builds the three completion surfaces the module's release checklist names, plus the
/// version caption that makes the binary identifiable.
///
/// Menu: Tools > Dama > Week 17 > 1. Build Release Panels
///
/// What it creates, and nothing else:
///   1. <c>GameCanvas/StartPanel/VersionLabel</c>  - the version caption, filled from ReleaseInfo at runtime.
///   2. <c>GameCanvas/StartPanel/HowToButton</c>   - opens <c>HowToPanel</c>.
///   3. <c>GameCanvas/StartPanel/CreditsButton</c> - opens <c>CreditsPanel</c>.
///   4. <c>GameCanvas/StartPanel/ExitButton</c>    - calls <c>DamaBoard.QuitGame()</c>.
///   5. <c>GameCanvas/HowToPanel</c>               - the instruction surface (title, body, BACK).
///   6. <c>GameCanvas/CreditsPanel</c>             - title, body, build line, BACK.
///
/// The title row is re-laid out to hold them: START stays centred, HOW TO PLAY and CREDITS sit either
/// side of it, and PROFILE moves to the left of the second row so EXIT has a home on the right. No
/// object changes size below the 96 canvas-px touch floor the audit enforces.
///
/// Re-runnable: every object it owns is removed and rebuilt, so running it twice does not duplicate
/// anything. The scene is saved at the end.
/// </summary>
public static class Week17ReleasePanels
{
    private const string MenuPath = "Tools/Dama/Week 17/1. Build Release Panels";
    private const string ScenePath = "Assets/Scenes/DamaGame.unity";

    // Row geometry, in canvas units at the 1280x720 reference resolution.
    private const float RowAY = 0.30f;          // START's existing anchor
    private const float RowBY = -115f;          // offset under row A
    private const float SideX = 440f;           // distance from centre for the flanking controls
    private const float ButtonWidth = 320f;
    private const float ButtonHeight = 100f;

    // Fills chosen so the non-text contrast check (>= 3:1 against the near-black panel) passes
    // on the first run rather than after a repair pass.
    private static readonly Color NavFill = new Color(0.24f, 0.40f, 0.66f, 1f);
    private static readonly Color ExitFill = new Color(0.62f, 0.26f, 0.26f, 1f);
    private static readonly Color BackFill = new Color(0.34f, 0.38f, 0.46f, 1f);
    private static readonly Color BodyTextColor = new Color(0.94f, 0.94f, 0.96f, 1f);
    private static readonly Color CaptionColor = new Color(0.82f, 0.86f, 0.90f, 1f);
    private static readonly Color BuildLineColor = new Color(0.95f, 0.85f, 0.55f, 1f);
    private static readonly Color PanelBackdrop = new Color(0f, 0f, 0f, 0.94f);

    private const string HowToBody =
        "The board is 8x8. RED moves first and moves UP the board; BLUE moves DOWN.\n\n" +
        "SELECT   Tap one of your pieces. The squares it can reach light up yellow.\n" +
        "MOVE     Tap a lit square. The turn then passes to the other side.\n\n" +
        "CAPTURE  Jump over an adjacent enemy piece onto the empty square beyond it, and the\n" +
        "         piece jumped is removed. If any capture is available to you, you must take\n" +
        "         one: a plain move is refused, and the board says so.\n" +
        "CHAIN    If the piece that just captured can capture again, it keeps the turn until\n" +
        "         no jump remains.\n" +
        "CROWN    A piece that reaches the far row becomes a DAMA (a king). A Dama travels any\n" +
        "         distance along a diagonal and stops before the first piece in its path.\n\n" +
        "WIN      A side with no pieces left, or with no legal move, loses.\n\n" +
        "PAUSE    The pause button in the top-right corner offers RESUME, RESTART, SOUND and\n" +
        "         MENU. MENU abandons the match and returns to this screen.\n" +
        "SOUND    SOUND spells out its own state and silences every clip and the capture haptic.\n" +
        "PROFILE  Your name, side, record and fastest wins stay on this device, in a signed file.";

    private const string CreditsBody =
        "DAMA - a two-player draughts game on an 8x8 board,\n" +
        "built on the Philippine games and sports theme.\n\n" +
        "Author        Charibelle Guzman, BSIT-4\n" +
        "Course        MOBILE DEV 002 - Advanced Mobile Application Development, Module 5\n" +
        "Institution   University of La Salette, Inc.\n" +
        "Engine        Unity, Universal Render Pipeline (2D Renderer)\n\n" +
        "Sounds        Five short feedback clips generated from text descriptions for this\n" +
        "              project. No clip is a recording of anything.\n" +
        "Typeface      Unity's built-in legacy font, used under the Unity licence.\n" +
        "Third party   No third-party code or asset ships in this build.\n\n" +
        "AI assistance Scripts and these screens were written with AI assistance and reviewed\n" +
        "              line by line. Every number in the Week 13-17 evidence came from a\n" +
        "              harness in this repository, not from memory.\n" +
        "Privacy       The game has no network path. Nothing leaves this device.";

    [MenuItem(MenuPath, false, 40)]
    public static void BuildReleasePanels()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[Week17ReleasePanels] No Canvas in the open scene. Open Assets/Scenes/DamaGame.unity first.");
            return;
        }

        Transform canvasTransform = canvas.transform;
        Transform startPanel = canvasTransform.Find("StartPanel");
        if (startPanel == null)
        {
            Debug.LogError("[Week17ReleasePanels] GameCanvas/StartPanel was not found.");
            return;
        }

        Font font = GetFont();

        // ---- 1. The version caption, top-left of the title screen --------------
        DestroyIfExists(startPanel, "VersionLabel");
        Text version = CreateLabel("VersionLabel", startPanel, font, ReleaseInfo.Caption(),
            new Vector2(0f, 1f), new Vector2(20f, -14f), new Vector2(560f, 34f),
            22, TextAnchor.MiddleLeft, FontStyle.Normal, CaptionColor);
        version.rectTransform.pivot = new Vector2(0f, 1f);
        version.horizontalOverflow = HorizontalWrapMode.Overflow;

        // ---- 2-4. Three controls around START, and PROFILE moved aside --------
        // The builder has to be re-runnable: a leftover copy sitting at the same anchored position
        // overlaps its replacement, which is the self-pair TC-A11Y-02 reports.
        DestroyIfExists(startPanel, "HowToButton");
        DestroyIfExists(startPanel, "CreditsButton");
        DestroyIfExists(startPanel, "ExitButton");

        GameObject howToButton = CreateButton("HowToButton", startPanel, font, "HOW TO PLAY",
            new Vector2(0.5f, RowAY), new Vector2(0f, 0f), new Vector2(ButtonWidth, ButtonHeight), NavFill, 24);
        GameObject creditsButton = CreateButton("CreditsButton", startPanel, font, "CREDITS",
            new Vector2(0.5f, RowAY), new Vector2(0f, 0f), new Vector2(ButtonWidth, ButtonHeight), NavFill, 24);
        GameObject exitButton = CreateButton("ExitButton", startPanel, font, "EXIT",
            new Vector2(0.5f, RowAY), new Vector2(0f, RowBY), new Vector2(ButtonWidth, ButtonHeight), ExitFill, 24);

        // START keeps its place; the two new controls flank it.
        howToButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-SideX, 0f);
        creditsButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(SideX, 0f);

        Transform profileButton = startPanel.Find("ProfileButton");
        if (profileButton != null)
        {
            RectTransform profileRect = profileButton.GetComponent<RectTransform>();
            Vector2 was = profileRect.anchoredPosition;
            profileRect.anchoredPosition = new Vector2(-SideX, RowBY);
            EditorUtility.SetDirty(profileButton);
            Debug.Log($"[Week17ReleasePanels] ProfileButton moved from {was} to {profileRect.anchoredPosition} so EXIT can take the right-hand seat on the same row. Size unchanged ({profileRect.sizeDelta}).");
        }
        else
        {
            Debug.LogWarning("[Week17ReleasePanels] StartPanel/ProfileButton was not found; the second row has no PROFILE control.");
        }

        // ---- 5. HOW TO PLAY ---------------------------------------------------
        DestroyIfExists(canvasTransform, "HowToPanel");
        GameObject howToPanel = CreatePanel("HowToPanel", canvasTransform);

        CreateLabel("TitleText", howToPanel.transform, font, "HOW TO PLAY",
            new Vector2(0.5f, 0.905f), Vector2.zero, new Vector2(760f, 56f),
            44, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white);

        Text body = CreateLabel("BodyText", howToPanel.transform, font, HowToBody,
            new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(1220f, 440f),
            26, TextAnchor.UpperLeft, FontStyle.Normal, BodyTextColor);
        FitText(body, 440f, 26, 18);

        CreateButton("BackButton", howToPanel.transform, font, "BACK",
            new Vector2(0.5f, 0.085f), Vector2.zero, new Vector2(ButtonWidth, ButtonHeight), BackFill, 24);

        // ---- 6. CREDITS -------------------------------------------------------
        DestroyIfExists(canvasTransform, "CreditsPanel");
        GameObject creditsPanel = CreatePanel("CreditsPanel", canvasTransform);

        CreateLabel("TitleText", creditsPanel.transform, font, "CREDITS",
            new Vector2(0.5f, 0.905f), Vector2.zero, new Vector2(600f, 56f),
            44, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white);

        Text creditsBody = CreateLabel("BodyText", creditsPanel.transform, font, CreditsBody,
            new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(1120f, 380f),
            22, TextAnchor.UpperLeft, FontStyle.Normal, BodyTextColor);
        FitText(creditsBody, 380f, 22, 18);

        // Filled at runtime by DamaBoard from ReleaseInfo, so it can never describe another build.
        CreateLabel("BuildText", creditsPanel.transform, font, ReleaseInfo.BuildLine(),
            new Vector2(0.5f, 0.235f), Vector2.zero, new Vector2(1120f, 36f),
            22, TextAnchor.MiddleCenter, FontStyle.Bold, BuildLineColor);

        CreateButton("BackButton", creditsPanel.transform, font, "BACK",
            new Vector2(0.5f, 0.085f), Vector2.zero, new Vector2(ButtonWidth, ButtonHeight), BackFill, 24);

        howToPanel.SetActive(false);
        creditsPanel.SetActive(false);

        EditorUtility.SetDirty(howToPanel);
        EditorUtility.SetDirty(creditsPanel);
        EditorUtility.SetDirty(canvas.gameObject);

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveScene(canvas.gameObject.scene);

        Debug.Log($"[Week17ReleasePanels] Built StartPanel/{{VersionLabel,HowToButton,CreditsButton,ExitButton}}, " +
                  $"HowToPanel and CreditsPanel, then saved {ScenePath}. Caption reads '{version.text}'.");
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

    /// <summary>
    /// Shrinks a paragraph's font until its measured height fits its rect, instead of guessing a size
    /// and hoping. The measured height is printed either way, so the number in the document is the one
    /// this run produced.
    /// </summary>
    private static void FitText(Text text, float maxHeight, int maxFont, int minFont)
    {
        float width = text.rectTransform.rect.width;

        for (int size = maxFont; size >= minFont; size--)
        {
            text.fontSize = size;
            float height = MeasureHeight(text, width);

            if (height <= maxHeight)
            {
                Debug.Log($"[Week17ReleasePanels] FitText '{text.name}': {size}px needs {height:0.0} px of {maxHeight:0.0} px (width {width:0.0}).");
                return;
            }
        }

        Debug.LogWarning($"[Week17ReleasePanels] FitText '{text.name}': even {minFont}px needs " +
                         $"{MeasureHeight(text, width):0.0} px of {maxHeight:0.0} px. The audit's label-fit check will report it.");
    }

    private static float MeasureHeight(Text text, float width)
    {
        TextGenerationSettings settings = text.GetGenerationSettings(new Vector2(width, 0f));
        settings.horizontalOverflow = HorizontalWrapMode.Wrap;
        settings.verticalOverflow = VerticalWrapMode.Overflow;

        var generator = new TextGenerator();
        float pixelsPerUnit = text.pixelsPerUnit <= 0f ? 1f : text.pixelsPerUnit;
        return generator.GetPreferredHeight(text.text, settings) / pixelsPerUnit;
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
            Debug.Log($"[Week17ReleasePanels] Removed {removed} previous {childName} object(s) before rebuilding.");
    }

    private static Font GetFont()
    {
        Text existing = Object.FindAnyObjectByType<Text>(FindObjectsInactive.Include);
        if (existing != null && existing.font != null) return existing.font;

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
