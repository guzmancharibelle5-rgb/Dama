using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Interactive Dama board (MVP step 1).
/// Builds the 8x8 board and 24 pieces from simple square/circle sprites and
/// handles tap-select, move highlighting, movement, jump captures, forced
/// multi-jump capture chains and promotion to a flying "Dama" king on the back row.
/// Captures are mandatory whenever any piece can capture.
/// Turns alternate between two local players, or between the player and a three-level AI
/// (Week 19: the mode is chosen on the title screen).
/// UI: start screen, pause menu, restart and win screen on a single Canvas.
/// Not yet included (next steps): online multiplayer.
/// </summary>
public enum PlayMode { TwoPlayer = 0, VsAi = 1 }

public class DamaBoard : MonoBehaviour
{
    public const int Size = 8;

    [Header("Sprites")]
    [SerializeField] private Sprite squareSprite;
    [SerializeField] private Sprite circleSprite;

    [Header("Board Colors")]
    [SerializeField] private Color lightSquareColor = new Color(0.92f, 0.85f, 0.70f, 1f);
    [SerializeField] private Color darkSquareColor = new Color(0.55f, 0.37f, 0.22f, 1f);

    [Header("Piece Colors")]
    [SerializeField] private Color player1Color = new Color(0.85f, 0.20f, 0.20f, 1f);
    [SerializeField] private Color player2Color = new Color(0.15f, 0.20f, 0.45f, 1f);

    [Header("Highlight Colors")]
    [SerializeField] private Color selectTint = new Color(0.30f, 1f, 1f, 1f);
    [SerializeField] private Color moveTint = new Color(1f, 0.90f, 0.25f, 1f);
    [SerializeField] private Color hintTint = new Color(0.35f, 0.95f, 0.55f, 1f);

    [Header("Dama (King)")]
    [SerializeField] private Color crownColor = new Color(1f, 0.82f, 0.15f, 1f);
    [SerializeField] private float crownScale = 0.55f;

    private enum Cell { Empty = 0, Player1 = 1, Player2 = 2 }

    private readonly Cell[,] grid = new Cell[Size, Size];
    private readonly SpriteRenderer[,] squareRenderers = new SpriteRenderer[Size, Size];
    private readonly SpriteRenderer[,] pieceRenderers = new SpriteRenderer[Size, Size];
    private readonly List<Vector2Int> highlightedCells = new List<Vector2Int>();
    private readonly List<Vector2Int> selectedMoves = new List<Vector2Int>();

    // ---- Week 16 optimization 1: move generation without allocation ----
    // One reusable buffer per generator. The list a generator returns stays valid until the next
    // call of that same generator, which every caller already respects: each one consumes it on
    // the spot with Count or AddRange and never keeps it. Before this, every piece scanned by
    // PlayerHasCapture, HasAnyLegalMove and a tap cost two fresh lists (556.7 B, measured).
    private readonly List<Vector2Int> captureBuffer = new List<Vector2Int>(32);
    private readonly List<Vector2Int> simpleBuffer = new List<Vector2Int>(32);

    /// <summary>Shared empty result, so the mandatory-capture refusal allocates nothing.</summary>
    private static readonly List<Vector2Int> noMoves = new List<Vector2Int>();

    private static bool moveBuffersReported;
    private readonly bool[,] isKing = new bool[Size, Size];

    // ---- Week 16 optimization 2: a piece pool, so a restart stops rebuilding the board ----
    // Measured before this change: one restart allocated 5,701 B and Destroyed 24 GameObjects to
    // create 24 new ones. A piece that leaves the board - captured, or cleared by a restart - is
    // parked under an inactive PiecePool root and rented again, so the same objects live for the
    // whole session and a restart only moves and recolours them.
    private readonly List<GameObject> piecePool = new List<GameObject>(32);
    private static readonly string[,] pieceNames = BuildPieceNames();
    private Transform piecesRootCached;
    private Transform piecePoolRoot;
    private static bool piecePoolReported;

    /// <summary>Every piece name this board can use, built once so a rent never formats a string.</summary>
    private static string[,] BuildPieceNames()
    {
        string[,] names = new string[2, Size * Size];
        for (int p = 1; p <= 2; p++)
            for (int i = 0; i < Size * Size; i++)
                names[p - 1, i] = $"Piece_{p}_{i / Size}_{i % Size}";
        return names;
    }

    private static string PieceName(int player, int x, int y) => pieceNames[player - 1, x * Size + y];

    private Camera mainCamera;
    private int currentPlayer = 1;
    private Vector2Int selected = new Vector2Int(-1, -1);
    private Vector2Int chainPiece = new Vector2Int(-1, -1);
    private Coroutine promotionPulse;

    // ---- Week 19: mode and opponent ----
    // The default stays TwoPlayer, so every harness written in Weeks 13-18 keeps driving the same
    // two-sided board it always did. An AI match is only ever started from the title screen.
    /// <summary>How long the AI appears to think, in unscaled seconds, so the reply is visible.</summary>
    [Header("AI (Week 19)")]
    [SerializeField] private float aiThinkSeconds = 0.35f;

    private PlayMode playMode = PlayMode.TwoPlayer;
    private AiLevel aiLevel = AiLevel.Normal;
    private int aiSide = 2;
    private Coroutine aiTurn;
    private bool aiThinking;

    // ---- Week 19: which side faces the player ----
    // 1 = RED at the bottom (the board exactly as it is built), 2 = BLUE at the bottom (the board
    // is turned half a turn). Only this transform's rotation changes: the grid, isKing and the
    // piece array do not move, so the rules are untouched and a turn is still a swap of
    // currentPlayer. The turn is fixed for the whole match, so a pass-and-play pair never has the
    // board swing under their hands mid-game.
    private int viewSide = 1;
    private Text sideButtonLabel;

    // HowTo and Credits are title-screen panels, like Profile: they only make sense from Ready.
    // ModeSelect (Week 19) is the same shape: it is reached from Ready and returns to it.
    private enum GameState { Ready, Playing, Paused, GameOver, Profile, HowTo, Credits, ModeSelect }

    private GameState gameState = GameState.Ready;
    private GameObject startPanel;
    private GameObject hudPanel;
    private GameObject pausePanel;
    private GameObject winPanel;
    private Text turnText;
    private Text countsText;
    private Text winText;
    private Text winSummaryText;
    private GameObject profilePanel;
    private GameObject howToPanel;
    private GameObject creditsPanel;
    private GameObject modePanel;
    private Text modeLabel;
    private Text difficultyLabel;
    private Text versionLabel;
    private Text creditsBuildText;
    private Text soundButtonLabel;
    private GameFeedback feedback;

    // ---- Week 20: draw rule, undo pile, hint and move animation ----
    // The history holds one snapshot per *turn*, not per jump: a three-jump chain is one turn, so
    // it is one undo. The draw counters live inside the history, so taking a turn back also puts
    // the repetition and no-progress counts back where they were.
    private readonly DamaPlyHistory history = new DamaPlyHistory();
    private PlySnapshot pendingPly;
    private Coroutine moveAnimation;
    private Coroutine capturePop;
    private bool moveAnimating;

    private float matchStartUnscaled;
    private int currentMatchId = -1;

    /// <summary>Assigns the sprites used when the board is built.</summary>
    public void SetSprites(Sprite square, Sprite circle)
    {
        squareSprite = square;
        circleSprite = circle;
    }

    private void Start()
    {
        mainCamera = Camera.main;

        if (!BindLayout())
        {
            BuildLayout();
            BindLayout();
        }

        CacheUI();
        ShowState(GameState.Ready);
        RefreshUI();
    }

    private void Update()
    {
        if (gameState != GameState.Playing) return;
        if (aiThinking) return;   // Week 19: the opponent owns the board while it thinks.
        if (!TryGetTapScreenPoint(out Vector2 screenPoint)) return;

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 world = mainCamera.ScreenToWorldPoint(screenPoint);

        // InverseTransformPoint, not a raw world read: when the board is turned to give BLUE the
        // bottom of the screen, the cell under the finger is the mirror of the world coordinate.
        Vector3 local = transform.InverseTransformPoint(world);
        int x = Mathf.FloorToInt(local.x + Size * 0.5f);
        int y = Mathf.FloorToInt(local.y + Size * 0.5f);

        if (x < 0 || x >= Size || y < 0 || y >= Size)
        {
            // Never lose the forced chain to an off-board tap.
            if (chainPiece.x >= 0) RehighlightChain();
            else ClearSelection();
            return;
        }

        HandleTap(new Vector2Int(x, y));
    }

    // ---- UI / Game State ----

    private void CacheUI()
    {
        GameObject canvas = GameObject.Find("GameCanvas");
        if (canvas == null) return;

        startPanel = canvas.transform.Find("StartPanel")?.gameObject;
        hudPanel = canvas.transform.Find("HUDPanel")?.gameObject;
        pausePanel = canvas.transform.Find("PausePanel")?.gameObject;
        winPanel = canvas.transform.Find("WinPanel")?.gameObject;
        profilePanel = canvas.transform.Find("ProfilePanel")?.gameObject;
        howToPanel = canvas.transform.Find("HowToPanel")?.gameObject;
        creditsPanel = canvas.transform.Find("CreditsPanel")?.gameObject;
        modePanel = canvas.transform.Find("ModePanel")?.gameObject;
        difficultyLabel = modePanel != null ? modePanel.transform.Find("DifficultyLabel")?.GetComponent<Text>() : null;

        Transform hud = canvas.transform.Find("HUDPanel");
        if (hud != null)
        {
            turnText = hud.Find("TurnText")?.GetComponent<Text>();
            countsText = hud.Find("CountsText")?.GetComponent<Text>();
            modeLabel = hud.Find("ModeLabel")?.GetComponent<Text>();
        }

        Transform win = canvas.transform.Find("WinPanel");
        if (win != null)
        {
            winText = win.Find("WinText")?.GetComponent<Text>();
            winSummaryText = win.Find("SummaryText")?.GetComponent<Text>();
        }

        WireButton(canvas, "StartPanel/StartButton", ShowModeSelect);
        WireButton(canvas, "StartPanel/PlayButton", ShowModeSelect);
        WireButton(canvas, "HUDPanel/PauseButton", PauseGame);
        WireButton(canvas, "PausePanel/ResumeButton", ResumeGame);
        WireButton(canvas, "PausePanel/RestartButtonPause", RestartGame);
        WireButton(canvas, "PausePanel/MenuButton", ReturnToMenu);
        WireButton(canvas, "WinPanel/RestartButtonWin", RestartGame);
        WireButton(canvas, "WinPanel/MenuButtonWin", ReturnToMenu);
        WireButton(canvas, "StartPanel/ProfileButton", ShowProfile);
        WireButton(canvas, "ProfilePanel/BackButton", CloseProfile);
        WireButton(canvas, "PausePanel/SoundButton", ToggleSound);

        // ---- Week 20: undo and hint ----
        WireButton(canvas, "PausePanel/UndoButton", UndoPressed);
        WireButton(canvas, "HUDPanel/HintButton", HintPressed);
        WireButton(canvas, "StartPanel/HowToButton", ShowHowTo);
        WireButton(canvas, "StartPanel/CreditsButton", ShowCredits);
        WireButton(canvas, "StartPanel/ExitButton", QuitGame);
        WireButton(canvas, "HowToPanel/BackButton", CloseHowTo);
        WireButton(canvas, "CreditsPanel/BackButton", CloseCredits);

        // ---- Week 19: the mode panel ----
        WireButton(canvas, "ModePanel/VsAiButton", StartVsAiMatch);
        WireButton(canvas, "ModePanel/VsPlayerButton", StartTwoPlayer);
        WireButton(canvas, "ModePanel/BackButton", CloseModeSelect);
        WireButton(canvas, "ModePanel/EasyButton", SelectEasy);
        WireButton(canvas, "ModePanel/NormalButton", SelectNormal);
        WireButton(canvas, "ModePanel/HardButton", SelectHard);
        WireButton(canvas, "ModePanel/SideButton", ToggleSide);

        if (modePanel != null)
        {
            Transform side = modePanel.transform.Find("SideButton");
            if (side != null) sideButtonLabel = side.Find("Text")?.GetComponent<Text>();
        }

        if (modePanel == null)
            Debug.LogWarning("[Week19] GameCanvas/ModePanel was not found: run Tools > Dama > Week 19 > 1. Build Mode UI. " +
                             "Until then the title screen's PLAY button has nowhere to go.");

        Transform sound = canvas.transform.Find("PausePanel/SoundButton");
        if (sound != null) soundButtonLabel = sound.Find("Text")?.GetComponent<Text>();

        RefreshReleaseLabels(canvas);

        feedback = GetComponent<GameFeedback>();
        if (feedback == null) feedback = FindFirstObjectByType<GameFeedback>();
        if (feedback == null)
            Debug.LogWarning("[DamaBoard] No GameFeedback in the scene; the game will stay silent.");
    }

    private static void WireButton(GameObject canvas, string path, UnityAction action)
    {
        Transform target = canvas.transform.Find(path);
        if (target == null) return;

        Button button = target.GetComponent<Button>();
        if (button == null) return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void ShowState(GameState state)
    {
        gameState = state;

        if (startPanel != null) startPanel.SetActive(state == GameState.Ready);
        if (hudPanel != null)
            hudPanel.SetActive(state == GameState.Playing || state == GameState.Paused || state == GameState.GameOver);
        if (pausePanel != null) pausePanel.SetActive(state == GameState.Paused);
        if (winPanel != null) winPanel.SetActive(state == GameState.GameOver);
        if (profilePanel != null) profilePanel.SetActive(state == GameState.Profile);
        if (howToPanel != null) howToPanel.SetActive(state == GameState.HowTo);
        if (creditsPanel != null) creditsPanel.SetActive(state == GameState.Credits);
        if (modePanel != null) modePanel.SetActive(state == GameState.ModeSelect);
    }

    /// <summary>
    /// Week 17: every build-identifying string is derived from the running binary at startup, never
    /// stored in the scene. A caption baked into a scene survives into a build it does not describe,
    /// which is the one way a release candidate can name the wrong version while looking correct.
    /// </summary>
    private void RefreshReleaseLabels(GameObject canvas)
    {
        versionLabel = canvas.transform.Find("StartPanel/VersionLabel")?.GetComponent<Text>();
        creditsBuildText = canvas.transform.Find("CreditsPanel/BuildText")?.GetComponent<Text>();

        if (versionLabel != null) versionLabel.text = ReleaseInfo.Caption();
        if (creditsBuildText != null) creditsBuildText.text = ReleaseInfo.BuildLine();

        if (versionLabel == null || creditsBuildText == null)
            Debug.LogWarning("[Week17] A release label is missing: run Tools > Dama > Week 17 > 1. Build Release Panels.");
        else
            Debug.Log($"[Week17] Release labels filled from the binary: title='{versionLabel.text}' credits='{creditsBuildText.text}'.");
    }

    private void RefreshUI()
    {
        if (turnText != null)
        {
            string side = currentPlayer == 1 ? "RED" : "BLUE";
            string who = side;

            // Week 19: in an AI match the HUD says which colour the player owns and which one thinks.
            if (playMode == PlayMode.VsAi)
                who = $"{side} ({(currentPlayer == aiSide ? "AI" : "you")})";

            turnText.text = $"Turn: {who}" +
                            (chainPiece.x >= 0 ? "  (chain jump!)" : "") +
                            (playMode == PlayMode.VsAi && currentPlayer == aiSide ? "  [thinking]" : "");
        }

        if (countsText != null)
            countsText.text = $"Red: {CountPieces(1)}   Blue: {CountPieces(2)}";

        if (modeLabel != null)
            modeLabel.text = ModeCaption();
    }

    /// <summary>
    /// Hides the title screen and starts a two-player match. Kept as the single two-sided entry
    /// point the Week 13-18 harnesses call, so their behaviour is unchanged by the mode work.
    /// </summary>
    public void StartGame()
    {
        playMode = PlayMode.TwoPlayer;
        aiSide = 0;

        // The harness entry point keeps the board exactly as built, so every Week 13-18 capture
        // and play-mode case still measures the same board it always did.
        viewSide = 1;

        BeginMatch();
    }

    /// <summary>Mode panel: PLAYER vs PLAYER.</summary>
    public void StartTwoPlayer()
    {
        playMode = PlayMode.TwoPlayer;
        aiSide = 0;
        viewSide = PlayerProfile.PlayerSide;

        Debug.Log($"[Week19] Mode selected: PLAYER vs PLAYER (pass and play on this device), " +
                  $"{(viewSide == 1 ? "RED" : "BLUE")} at the bottom for the whole match.");

        BeginMatch();
    }

    /// <summary>Mode panel: PLAYER vs AI, using the difficulty that is currently chosen.</summary>
    public void StartVsAiMatch()
    {
        StartVsAi(aiLevel);
    }

    /// <summary>
    /// Starts a match against the AI. The human plays the side stored in the player profile, because
    /// the end screen already judges win/loss against that side (DeclareWinner -> PlayerProfile).
    /// </summary>
    public void StartVsAi(AiLevel level)
    {
        aiLevel = level;
        playMode = PlayMode.VsAi;

        int humanSide = PlayerProfile.PlayerSide;
        aiSide = humanSide == 1 ? 2 : 1;
        viewSide = humanSide;

        DamaRules.Report();

        Debug.Log($"[Week19] mode=VsAi level={aiLevel} you={(humanSide == 1 ? "RED" : "BLUE")} " +
                  $"ai={(aiSide == 1 ? "RED" : "BLUE")}{(aiSide == 1 ? " (the AI opens)" : "")}");

        BeginMatch();
    }

    /// <summary>The one place a match begins, whichever mode asked for it.</summary>
    private void BeginMatch()
    {
        Time.timeScale = 1f;
        ApplyBoardView();

        // Week 20: a new match starts with an empty ply stack, and the position it starts from is
        // the first entry in the draw watch.
        pendingPly = null;
        history.Reset(BuildPositionKey(currentPlayer));

        matchStartUnscaled = Time.unscaledTime;
        currentMatchId = PlayerProfile.LastMatchId + 1;
        ShowState(GameState.Playing);
        RefreshUI();

        Debug.Log($"[Week19] Match {currentMatchId} started: {ModeCaption()}, turn={(currentPlayer == 1 ? "RED" : "BLUE")}");

        MaybeStartAiTurn();
    }

    // ---- Week 19: mode panel ----

    /// <summary>PLAY on the title screen opens the mode panel.</summary>
    public void ShowModeSelect()
    {
        if (gameState != GameState.Ready) return;
        Time.timeScale = 1f;
        RefreshModePanel();
        ShowState(GameState.ModeSelect);
        Debug.Log($"[Week19] ModePanel opened. Difficulty {aiLevel}, side {(PlayerProfile.PlayerSide == 1 ? "RED" : "BLUE")}.");
    }

    /// <summary>Mode panel BACK: nothing is started.</summary>
    public void CloseModeSelect()
    {
        if (gameState != GameState.ModeSelect) return;
        ShowState(GameState.Ready);
        Debug.Log("[Week19] ModePanel closed without starting a match.");
    }

    public void SelectEasy() => SelectLevel(AiLevel.Easy);
    public void SelectNormal() => SelectLevel(AiLevel.Normal);
    public void SelectHard() => SelectLevel(AiLevel.Hard);

    /// <summary>
    /// Difficulty is a choice made before the match, so it lives on the mode panel rather than in the
    /// pause menu; the label and the three fills spell out which one is armed.
    /// </summary>
    public void SelectLevel(AiLevel level)
    {
        aiLevel = level;
        RefreshModePanel();
        Debug.Log($"[Week19] Difficulty set to {aiLevel}.");
    }

    private void RefreshDifficultyPanel()
    {
        if (difficultyLabel != null)
            difficultyLabel.text = $"AI LEVEL: {aiLevel.ToString().ToUpperInvariant()}";

        if (modePanel == null) return;

        TintDifficultyButton("ModePanel/EasyButton", aiLevel == AiLevel.Easy);
        TintDifficultyButton("ModePanel/NormalButton", aiLevel == AiLevel.Normal);
        TintDifficultyButton("ModePanel/HardButton", aiLevel == AiLevel.Hard);
    }

    private void TintDifficultyButton(string path, bool selected)
    {
        if (modePanel == null) return;

        Transform button = modePanel.transform.Find(path.Substring("ModePanel/".Length));
        if (button == null) return;

        Image image = button.GetComponent<Image>();
        if (image == null) return;

        image.color = selected ? new Color(0.24f, 0.40f, 0.66f, 1f) : new Color(0.34f, 0.38f, 0.46f, 1f);
    }

    // ---- Week 19: the side the player takes ----

    /// <summary>Mode panel SIDE pressed while RED is wanted.</summary>
    public void SelectRed() => SelectSide(1);

    /// <summary>Mode panel SIDE pressed while BLUE is wanted.</summary>
    public void SelectBlue() => SelectSide(2);

    /// <summary>One control that swaps the side, for a screen that has room for only one.</summary>
    public void ToggleSide() => SelectSide(PlayerProfile.PlayerSide == 1 ? 2 : 1);

    /// <summary>
    /// Stores the side through PlayerProfile.SetSide. That is also the side DeclareWinner judges the
    /// record against, so the scoreboard can never contradict the colour that was on the screen.
    /// The visible end of the board follows the same value, so choosing BLUE turns the board.
    /// </summary>
    public void SelectSide(int side)
    {
        if (side != 1 && side != 2) return;

        if (!PlayerProfile.SetSide(side))
        {
            Debug.LogWarning($"[Week19] Could not store side {side}: {PlayerProfile.LastError}");
            return;
        }

        viewSide = side;
        RefreshModePanel();

        Debug.Log($"[Week19] Side set to {(side == 1 ? "RED" : "BLUE")}; the profile records " +
                  $"{(PlayerProfile.PlayerSide == 1 ? "RED" : "BLUE")} and that end faces the player.");
    }

    /// <summary>Fills the side label, the difficulty label and the three level fills from state.</summary>
    private void RefreshModePanel()
    {
        RefreshDifficultyPanel();

        if (sideButtonLabel != null)
            sideButtonLabel.text = $"PLAYING AS: {(PlayerProfile.PlayerSide == 1 ? "RED" : "BLUE")}";
    }

    /// <summary>
    /// Turns the whole board so the chosen side sits nearest the player: RED at the bottom is the
    /// board exactly as built, BLUE at the bottom is that same board turned half a turn. The rule
    /// state is untouched - a turn is still a swap of currentPlayer - and the tap mapping in Update
    /// reads the point in local space, so the square under the finger is the square that is played.
    /// Called once per match, never between turns, so a pass-and-play pair keeps one view.
    /// </summary>
    private void ApplyBoardView()
    {
        float degrees = viewSide == 2 ? 180f : 0f;
        transform.localRotation = Quaternion.Euler(0f, 0f, degrees);

        Debug.Log($"[Week19] Board view: {(viewSide == 2 ? "BLUE" : "RED")} at the bottom " +
                  $"({degrees:0} deg on the board root).");
    }

    // ---- Week 19: the AI's turn ----

    /// <summary>
    /// Starts the opponent's turn when - and only when - it is the AI's move in an AI match. Safe to
    /// call from anywhere: every guard is repeated here, because being called twice is the one
    /// mistake that would let two coroutines fight over the board.
    /// </summary>
    private void MaybeStartAiTurn()
    {
        if (playMode != PlayMode.VsAi) return;
        if (gameState != GameState.Playing) return;
        if (currentPlayer != aiSide) return;
        if (aiThinking) return;

        aiTurn = StartCoroutine(AiTurnRoutine());
    }

    /// <summary>
    /// One AI turn: wait, ask DamaAI for a step, and play it through ExecuteMove - the same path a tap
    /// takes, so captures, crowns, chains, sounds and the win check happen exactly once, in one place.
    /// A jump chain keeps the same side to move, so the routine simply loops for the next jump.
    /// Waits are unscaled: pause sets Time.timeScale to 0, and a scaled wait would never finish.
    /// </summary>
    private IEnumerator AiTurnRoutine()
    {
        aiThinking = true;

        while (true)
        {
            // realtimeSinceStartup, not Time.deltaTime: pause sets timeScale to 0, and a scaled wait
            // would never finish, while an unscaled-delta accumulation is zero in the editor.
            float startedAt = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - startedAt < aiThinkSeconds)
            {
                if (gameState != GameState.Playing)
                {
                    Debug.Log($"[Week19] AI turn abandoned while thinking (state={gameState}).");
                    FinishAiTurn();
                    yield break;
                }

                yield return null;
            }

            if (gameState != GameState.Playing || currentPlayer != aiSide)
            {
                Debug.Log($"[Week19] AI turn abandoned before moving (state={gameState}, turn={(currentPlayer == 1 ? "RED" : "BLUE")}).");
                FinishAiTurn();
                yield break;
            }

            Vector2Int forced = chainPiece.x >= 0 ? chainPiece : new Vector2Int(-1, -1);
            DamaRules.Position position = SnapshotPosition();
            DamaAI.Choice choice = DamaAI.Choose(position, aiSide, aiLevel, forced);

            if (!choice.valid)
            {
                Debug.LogWarning($"[Week19] The AI found no legal move for {(aiSide == 1 ? "RED" : "BLUE")}; " +
                                 "the board's own win check has already been asked, so the turn is left alone.");
                FinishAiTurn();
                yield break;
            }

            if (!IsLegalBoardMove(choice.from, choice.to))
            {
                Debug.LogError($"[Week19] The AI proposed an illegal move {choice} (forcedFrom={forced}); refused, so the board is untouched.");
                FinishAiTurn();
                yield break;
            }

            Debug.Log($"[Week19] AI {(aiSide == 1 ? "RED" : "BLUE")} plays {choice}{(choice.nodes > 0 ? $" ({aiLevel}, {choice.nodes} nodes)" : $" ({aiLevel})")}.");
            ExecuteMove(choice.from, choice.to);

            // A chain capture keeps the same side to move: think again for the next jump.
            if (gameState == GameState.Playing && chainPiece.x >= 0) continue;

            FinishAiTurn();
            yield break;
        }
    }

    private void FinishAiTurn()
    {
        aiThinking = false;
        aiTurn = null;
    }

    /// <summary>Stops the opponent. Called by restart, by returning to the menu, and by the end screen.</summary>
    private void CancelAiTurn()
    {
        if (aiTurn != null) StopCoroutine(aiTurn);
        aiTurn = null;

        if (!aiThinking) return;
        aiThinking = false;
        Debug.Log("[Week19] AI turn cancelled.");
    }

    /// <summary>A copy of the live board for the AI to think on, so nothing it does can touch the real one.</summary>
    private DamaRules.Position SnapshotPosition()
    {
        DamaRules.Position position = new DamaRules.Position();

        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
                if (grid[x, y] != Cell.Empty)
                    position.Set(x, y, (int)grid[x, y], isKing[x, y]);

        return position;
    }

    /// <summary>
    /// The board's own verdict on the AI's proposal, asked with the board's own generator. DamaRules
    /// mirrors these rules, and Week 19's harness compares the two, but a disagreement at runtime must
    /// leave the board untouched rather than move a piece illegally.
    /// </summary>
    private bool IsLegalBoardMove(Vector2Int from, Vector2Int to)
    {
        if (!InBounds(from.x, from.y) || !InBounds(to.x, to.y)) return false;
        if (grid[from.x, from.y] != (aiSide == 1 ? Cell.Player1 : Cell.Player2)) return false;
        if (grid[to.x, to.y] != Cell.Empty) return false;

        List<Vector2Int> legal = GetSelectableMoves(from.x, from.y);
        return legal.Contains(to);
    }

    /// <summary>One short caption for the mode, used by the HUD and by the transcript.</summary>
    private string ModeCaption()
    {
        if (playMode != PlayMode.VsAi) return "PLAYER vs PLAYER";
        return $"PLAYER vs AI - {aiLevel.ToString().ToUpperInvariant()}";
    }

    /// <summary>Freezes the board (input is gated in Update) and shows the pause menu.</summary>
    public void PauseGame()
    {
        if (gameState != GameState.Playing) return;
        Time.timeScale = 0f;
        RefreshSoundLabel();
        ShowState(GameState.Paused);
    }

    public void ResumeGame()
    {
        if (gameState != GameState.Paused) return;
        Time.timeScale = 1f;
        ShowState(GameState.Playing);

        // The AI routine abandons itself on pause, so resuming is what puts it back on the board.
        MaybeStartAiTurn();
    }

    /// <summary>Opens the profile screen from the title screen.</summary>
    public void ShowProfile()
    {
        if (gameState != GameState.Ready) return;
        Time.timeScale = 1f;
        ShowState(GameState.Profile);
    }

    /// <summary>
    /// Pause panel and end screen: abandon the match and go back to the title screen, so a player
    /// never has to close the app to start over. The board is rebuilt and the clock restored, or the
    /// title screen would be frozen by the pause the caller came from.
    /// </summary>
    public void ReturnToMenu()
    {
        RestartGame();
        ShowState(GameState.Ready);
        Time.timeScale = 1f;
        CancelAiTurn();

        // The title screen is the board as built: a match that was viewed from the BLUE end must
        // not leave the menu sitting behind a turned board.
        viewSide = 1;
        ApplyBoardView();

        Debug.Log("[Week15] ReturnToMenu: board reset, title screen shown, timeScale restored to 1.");
    }

    /// <summary>Returns from the profile screen to the title screen.</summary>
    public void CloseProfile()
    {
        if (gameState != GameState.Profile) return;
        ShowState(GameState.Ready);
    }

    /// <summary>Opens the instruction screen from the title screen.</summary>
    public void ShowHowTo()
    {
        if (gameState != GameState.Ready) return;
        Time.timeScale = 1f;
        ShowState(GameState.HowTo);
        Debug.Log("[Week17] HowToPanel opened.");
    }

    /// <summary>Returns from the instruction screen to the title screen.</summary>
    public void CloseHowTo()
    {
        if (gameState != GameState.HowTo) return;
        ShowState(GameState.Ready);
        Debug.Log("[Week17] HowToPanel closed.");
    }

    /// <summary>Opens the credits screen from the title screen.</summary>
    public void ShowCredits()
    {
        if (gameState != GameState.Ready) return;
        Time.timeScale = 1f;
        ShowState(GameState.Credits);
        Debug.Log("[Week17] CreditsPanel opened.");
    }

    /// <summary>Returns from the credits screen to the title screen.</summary>
    public void CloseCredits()
    {
        if (gameState != GameState.Credits) return;
        ShowState(GameState.Ready);
        Debug.Log("[Week17] CreditsPanel closed.");
    }

    /// <summary>
    /// The EXIT control the release checklist asks for. In the editor it leaves play mode, because
    /// Application.Quit does nothing there; on a device it closes the application.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log($"[Week17] EXIT pressed on build {ReleaseInfo.Version}: closing the application.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>Pause panel SOUND button: flips feedback and renames itself in words, not a tint.</summary>
    public void ToggleSound()
    {
        if (feedback == null) feedback = GetComponent<GameFeedback>();
        if (feedback == null)
        {
            Debug.LogWarning("[DamaBoard] SOUND was pressed but there is no GameFeedback in the scene.");
            return;
        }

        feedback.ToggleSound();
        RefreshSoundLabel();
    }

    private void RefreshSoundLabel()
    {
        if (soundButtonLabel == null)
        {
            GameObject canvas = GameObject.Find("GameCanvas");
            Transform sound = canvas != null ? canvas.transform.Find("PausePanel/SoundButton") : null;
            if (sound != null) soundButtonLabel = sound.Find("Text")?.GetComponent<Text>();
        }

        if (soundButtonLabel == null) return;

        bool on = feedback == null || feedback.SoundEnabled;
        soundButtonLabel.text = GameFeedback.SoundLabel(on);
    }

    /// <summary>
    /// Single restart entry point (Pause panel and Win panel): clears the board,
    /// respawns all 24 pieces, resets turn/chain/king state and UI, unpauses.
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;

        CancelAiTurn();

        // Week 20: a restart drops the undo pile and any move still in flight, or the first ply of
        // the new match would be able to walk back into the old one.
        StopMoveAnimation();
        history.Clear();
        pendingPly = null;

        if (promotionPulse != null)
        {
            StopCoroutine(promotionPulse);
            promotionPulse = null;
        }

        chainPiece = new Vector2Int(-1, -1);
        ClearSelection();
        currentPlayer = 1;
        currentMatchId = PlayerProfile.LastMatchId + 1;
        matchStartUnscaled = Time.unscaledTime;

        Transform piecesRoot = PiecesRoot();
        if (piecesRoot != null) ReleaseAllPieces(piecesRoot);

        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                grid[x, y] = Cell.Empty;
                pieceRenderers[x, y] = null;
                isKing[x, y] = false;
            }
        }

        if (piecesRoot != null)
            SpawnStartingPieces(piecesRoot, circleSprite != null ? circleSprite : CreateCircleSprite());

        // Week 20: the board is back in its opening position, so that is what the draw watch counts
        // from - and nothing from the abandoned match is left in the undo pile.
        history.Reset(BuildPositionKey(currentPlayer));

        if (!piecePoolReported)
        {
            piecePoolReported = true;
            Debug.Log($"[Perf] restart reuses pooled piece GameObjects ({piecePool.Count} parked): " +
                      "no Destroy and no new GameObject per piece");
        }

        ShowState(GameState.Playing);
        RefreshUI();

        // A restart in an AI match must hand the turn back if the AI is the side that opens.
        ApplyBoardView();
        MaybeStartAiTurn();
    }

    /// <summary>
    /// The player to move loses when they have zero pieces or no legal move.
    /// Checked at the start of every turn (i.e. right after any move/captures).
    /// </summary>
    private void CheckForWin()
    {
        if (gameState != GameState.Playing) return;

        if (CountPieces(currentPlayer) == 0 || !HasAnyLegalMove(currentPlayer))
            DeclareWinner(currentPlayer == 1 ? 2 : 1);
    }

    /// <summary>Test seam (Week 13 harness): banks a match through the path the end screen uses.</summary>
    public void BankMatchForTests(int winner)
    {
        BankMatch(winner);
    }

    /// <summary>
    /// Banks the finished match exactly once. The match id is fixed at match start, so
    /// the store's id gate and the check below both refuse a repeated end-of-match call.
    /// </summary>
    private void BankMatch(int winner)
    {
        if (currentMatchId <= 0) currentMatchId = PlayerProfile.LastMatchId + 1;

        // One bank per match: the id is fixed when the match starts, so a repeated
        // end-of-match callback is refused here instead of inflating the record.
        if (currentMatchId <= PlayerProfile.LastMatchId)
        {
            Debug.Log($"[DamaBoard] Match {currentMatchId} was already banked; ignoring the repeat.");
            return;
        }

        float seconds = Mathf.Max(0f, Time.unscaledTime - matchStartUnscaled);

        if (PlayerProfile.RecordMatch(currentMatchId, winner, seconds, CountPieces(1), CountPieces(2)))
        {
            // A draw banks as winner 0: PlayerProfile records it as "DRAW" and raises the draw count.
            Debug.Log($"[DamaBoard] Banked match {currentMatchId}: winner={(winner == 1 ? "RED" : winner == 2 ? "BLUE" : "DRAW")} seconds={seconds:0.0}");
        }
        else
        {
            Debug.LogWarning($"[DamaBoard] Match {currentMatchId} was not banked: {PlayerProfile.LastError}");
        }
    }

    /// <summary>
    /// Shows who won and why. The loser, the two piece counts and the reason are derived here rather
    /// than passed in: CheckForWin has already made the only decision worth naming - which side won -
    /// and a second parameter would only add a way for the two to disagree.
    /// </summary>
    private void DeclareWinner(int winner)
    {
        if (winText != null)
        {
            winText.text = winner == 1 ? "RED WIN!" : "BLUE WIN!";
            winText.color = winner == 1
                ? new Color(1f, 0.25f, 0.25f)
                : new Color(0.35f, 0.55f, 1f);
        }

        int loser = winner == 1 ? 2 : 1;
        string winnerLabel = winner == 1 ? "RED" : "BLUE";
        string loserLabel = loser == 1 ? "RED" : "BLUE";
        int winnerCount = CountPieces(winner);
        int loserCount = CountPieces(loser);
        string reason = loserCount == 0 ? "no pieces left" : "no legal move";

        if (winSummaryText != null)
        {
            winSummaryText.text = $"{winnerLabel} WINS.  {winnerLabel} {winnerCount}-{loserCount}, " +
                                  $"{loserLabel} has {reason}.";
        }

        Debug.Log($"[Week15] End screen: {winnerLabel} WINS {winnerCount}-{loserCount}, {loserLabel} has {reason}.");

        bool playerWon = (winner == PlayerProfile.PlayerSide);
        if (feedback != null) feedback.PlayResult(playerWon);
        Debug.Log($"[Feedback] {(playerWon ? "win" : "lose")} - winner {(winner == 1 ? "RED" : "BLUE")}, " +
                  $"your side {(PlayerProfile.PlayerSide == 1 ? "RED" : "BLUE")} -> {(playerWon ? "Win" : "Lose")}");

        BankMatch(winner);

        Time.timeScale = 0f;
        ShowState(GameState.GameOver);
    }

    private int CountPieces(int player)
    {
        Cell me = player == 1 ? Cell.Player1 : Cell.Player2;
        int count = 0;

        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
                if (grid[x, y] == me) count++;

        return count;
    }

    private bool HasAnyLegalMove(int player)
    {
        Cell me = player == 1 ? Cell.Player1 : Cell.Player2;

        // The mandatory-capture rule is a property of the whole side, so it is asked once, and the
        // per-piece questions below are answered with captures or with steps, never both.
        bool mustCapture = PlayerHasCapture(me);

        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                if (grid[x, y] != me) continue;
                if (mustCapture)
                {
                    if (HasCaptureAt(x, y)) return true;
                }
                else if (HasSimpleMoveAt(x, y))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>True when this piece has a capture; answers without building a list for the caller.</summary>
    private bool HasCaptureAt(int x, int y) => GetCaptureMoves(x, y).Count > 0;

    /// <summary>True when this piece has a plain step; answers without building a list for the caller.</summary>
    private bool HasSimpleMoveAt(int x, int y) => GetSimpleMoves(x, y).Count > 0;



    // ---- Layout ----

    /// <summary>Creates the 8x8 squares and the 24 starting pieces.</summary>
    public void BuildLayout()
    {
        Sprite square = squareSprite != null ? squareSprite : CreateSquareSprite();
        Sprite circle = circleSprite != null ? circleSprite : CreateCircleSprite();

        Transform squaresRoot = new GameObject("Squares").transform;
        squaresRoot.SetParent(transform, false);
        Transform piecesRoot = new GameObject("Pieces").transform;
        piecesRoot.SetParent(transform, false);

        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                GameObject squareObject = new GameObject($"Square_{x}_{y}");
                squareObject.transform.SetParent(squaresRoot, false);
                squareObject.transform.localPosition = CellToLocal(x, y);

                SpriteRenderer renderer = squareObject.AddComponent<SpriteRenderer>();
                renderer.sprite = square;
                renderer.color = IsDark(x, y) ? darkSquareColor : lightSquareColor;
                renderer.sortingOrder = 0;
            }
        }

        SpawnStartingPieces(piecesRoot, circle);
    }

    /// <summary>Spawns and registers the 24 starting pieces under the given root.</summary>
    private void SpawnStartingPieces(Transform piecesRoot, Sprite circle)
    {
        for (int y = 0; y <= 2; y++)
            for (int x = 0; x < Size; x++)
                if (IsDark(x, y)) RegisterPiece(RentPiece(piecesRoot, circle, 1, x, y), 1, x, y);

        for (int y = 5; y <= 7; y++)
            for (int x = 0; x < Size; x++)
                if (IsDark(x, y)) RegisterPiece(RentPiece(piecesRoot, circle, 2, x, y), 2, x, y);
    }

    /// <summary>
    /// Places a spawned piece into the board state arrays.
    /// </summary>
    private void RegisterPiece(GameObject piece, int player, int x, int y)
    {
        grid[x, y] = player == 1 ? Cell.Player1 : Cell.Player2;
        pieceRenderers[x, y] = piece.GetComponent<SpriteRenderer>();
        isKing[x, y] = false;
    }

    /// <summary>
    /// Rents a piece for the board: a pooled one when the pool has any, a new GameObject only when
    /// it is empty. The name, the colour and the crown are reset here, because a pooled piece may
    /// have been captured under a different name or promoted in an earlier match.
    /// </summary>
    private GameObject RentPiece(Transform parent, Sprite sprite, int player, int x, int y)
    {
        GameObject piece;
        if (piecePool.Count > 0)
        {
            int last = piecePool.Count - 1;
            piece = piecePool[last];
            piecePool.RemoveAt(last);
            piece.transform.SetParent(parent, false);
            piece.SetActive(true);
        }
        else
        {
            piece = new GameObject();
            piece.AddComponent<SpriteRenderer>();
            piece.transform.SetParent(parent, false);
            piece.transform.localScale = Vector3.one * 0.82f;
        }

        piece.name = PieceName(player, x, y);
        piece.transform.localPosition = CellToLocal(x, y);

        SpriteRenderer renderer = piece.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = player == 1 ? player1Color : player2Color;
        renderer.sortingOrder = 1;

        Transform crown = CrownChild(piece.transform);
        if (crown != null) crown.gameObject.SetActive(false);
        return piece;
    }

    /// <summary>
    /// The gold crown child of a piece, or null. Index-based on purpose: a rent happens 24 times per
    /// restart and Transform.Find would allocate a name comparison on the very path being optimized.
    /// </summary>
    private static Transform CrownChild(Transform piece) =>
        piece.childCount > 0 ? piece.GetChild(piece.childCount - 1) : null;

    /// <summary>The board's Pieces child, found once and then remembered.</summary>
    private Transform PiecesRoot()
    {
        if (piecesRootCached == null) piecesRootCached = transform.Find("Pieces");
        return piecesRootCached;
    }

    /// <summary>Inactive parent for recycled pieces: an inactive object draws nothing.</summary>
    private Transform PiecePoolRoot()
    {
        if (piecePoolRoot == null)
        {
            GameObject root = new GameObject("PiecePool");
            root.transform.SetParent(transform, false);
            root.SetActive(false);
            piecePoolRoot = root.transform;
        }

        return piecePoolRoot;
    }

    /// <summary>Parks a piece for reuse instead of destroying it.</summary>
    private void ReleasePiece(GameObject piece)
    {
        if (piece == null) return;

        // A piece can be parked mid-pop. Reset the scale here so a rented piece never starts a
        // match bulged by an animation that was interrupted before it finished.
        piece.transform.localScale = Vector3.one * 0.82f;
        piece.SetActive(false);
        piece.transform.SetParent(PiecePoolRoot(), false);
        piecePool.Add(piece);
    }

    private void ReleaseAllPieces(Transform root)
    {
        // Backwards, because each release reparents the child out of this root.
        for (int i = root.childCount - 1; i >= 0; i--)
            ReleasePiece(root.GetChild(i).gameObject);
    }
    /// <summary>Turns a man into a flying Dama: sets the flag and stacks a gold crown.</summary>
    private void PromoteToDama(int x, int y)
    {
        isKing[x, y] = true;

        if (feedback != null) feedback.PlayCrown();
        Debug.Log($"[Feedback] crown - a man reached the back row at ({x},{y}) -> Crown");

        SpriteRenderer pieceRenderer = pieceRenderers[x, y];
        if (pieceRenderer == null) return;

        Transform crowned = CrownChild(pieceRenderer.transform);
        if (crowned == null)
        {
            GameObject crown = new GameObject("Crown");
            crown.transform.SetParent(pieceRenderer.transform, false);
            crown.transform.localPosition = Vector3.zero;
            crown.transform.localScale = Vector3.one * crownScale;

            SpriteRenderer crownRenderer = crown.AddComponent<SpriteRenderer>();
            crownRenderer.sprite = pieceRenderer.sprite;
            crownRenderer.color = crownColor;
            crownRenderer.sortingOrder = pieceRenderer.sortingOrder + 1;
        }
        else
        {
            // A pooled piece keeps its crown object; promoting it again only shows the crown.
            crowned.gameObject.SetActive(true);
        }

        if (promotionPulse != null) StopCoroutine(promotionPulse);
        promotionPulse = StartCoroutine(PromotionPulse(pieceRenderer.transform));
    }

    /// <summary>Brief scale-up-and-back pulse so the promotion is noticeable.</summary>
    private IEnumerator PromotionPulse(Transform piece)
    {
        const float duration = 0.25f;
        Vector3 baseScale = Vector3.one * 0.82f;
        float elapsed = 0f;

        while (piece != null && elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            piece.localScale = Vector3.one * (Mathf.Lerp(1.35f, 1f, t) * 0.82f);
            yield return null;
        }

        if (piece != null) piece.localScale = baseScale;
        promotionPulse = null;
    }

    /// <summary>Finds board children by name and rebuilds grid state from them.</summary>
    private bool BindLayout()
    {
        Transform squaresRoot = transform.Find("Squares");
        Transform piecesRoot = transform.Find("Pieces");
        if (squaresRoot == null || piecesRoot == null || squaresRoot.childCount != Size * Size) return false;

        // The 64 squares are created x-major, y-inner (BuildLayout), so the child index is known.
        // Reading by index and confirming the position replaces 64 Transform.Find calls per bind;
        // Find stays as the fallback for a hierarchy that was reordered by hand.
        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                Transform square = squaresRoot.GetChild(x * Size + y);
                if (!IsSquareAt(square, x, y)) square = squaresRoot.Find($"Square_{x}_{y}");
                if (square == null) return false;

                squareRenderers[x, y] = square.GetComponent<SpriteRenderer>();
                pieceRenderers[x, y] = null;
                grid[x, y] = Cell.Empty;
                isKing[x, y] = false;
            }
        }

        foreach (Transform piece in piecesRoot)
        {
            // Name format: Piece_{player}_{x}_{y}
            string[] parts = piece.name.Split('_');
            if (parts.Length != 4) continue;
            if (!int.TryParse(parts[1], out int player)) continue;
            if (!int.TryParse(parts[2], out int x)) continue;
            if (!int.TryParse(parts[3], out int y)) continue;
            if (x < 0 || x >= Size || y < 0 || y >= Size) continue;

            grid[x, y] = player == 1 ? Cell.Player1 : Cell.Player2;
            pieceRenderers[x, y] = piece.GetComponent<SpriteRenderer>();
            Transform crown = CrownChild(piece);
            isKing[x, y] = crown != null && crown.gameObject.activeSelf;
        }

        return true;
    }

    /// <summary>True when this child sits on cell (x,y), which is how a bind confirms the index.</summary>
    private static bool IsSquareAt(Transform square, int x, int y)
    {
        if (square == null) return false;
        return (square.localPosition - CellToLocal(x, y)).sqrMagnitude < 0.0001f;
    }

    // ---- Interaction ----

    private void HandleTap(Vector2Int cell)
    {
        if (aiThinking) return;   // Week 19: taps are ignored while the opponent moves.
        if (moveAnimating) return; // Week 20: a piece is still travelling; a second tap would jump it.

        // Chain capture in progress: only the chained piece's landing squares act.
        if (chainPiece.x >= 0)
        {
            if (selectedMoves.Contains(cell))
            {
                ExecuteMove(chainPiece, cell);
                return;
            }

            if (cell == chainPiece) RehighlightChain();
            return; // Every other tap is ignored while the chain is active.
        }

        if (selected.x >= 0 && selectedMoves.Contains(cell))
        {
            ExecuteMove(selected, cell);
            return;
        }

        Cell content = grid[cell.x, cell.y];
        bool ownPiece = (currentPlayer == 1 && content == Cell.Player1) ||
                        (currentPlayer == 2 && content == Cell.Player2);

        if (ownPiece)
        {
            List<Vector2Int> moves = GetSelectableMoves(cell.x, cell.y);
            if (moves.Count == 0) return; // Mandatory capture locks this piece out.

            ClearSelection();
            selected = cell;
            selectedMoves.AddRange(moves);
            HighlightSquare(cell, selectTint);
            foreach (Vector2Int move in selectedMoves) HighlightSquare(move, moveTint);
            return;
        }

        ClearSelection();
    }

    private void ExecuteMove(Vector2Int from, Vector2Int to)
    {
        Cell moving = grid[from.x, from.y];
        int player = moving == Cell.Player1 ? 1 : 2;
        Vector2Int jumped = FindJumpedCell(from, to);

        // Week 20: the first move of a turn opens the snapshot; the rest of a forced chain adds to
        // the same one, so a three-jump chain undoes as one turn rather than as three.
        if (chainPiece.x < 0) pendingPly = CaptureSnapshot();

        grid[to.x, to.y] = moving;
        grid[from.x, from.y] = Cell.Empty;
        isKing[to.x, to.y] = isKing[from.x, from.y];
        isKing[from.x, from.y] = false;
        pieceRenderers[to.x, to.y] = pieceRenderers[from.x, from.y];
        pieceRenderers[from.x, from.y] = null;

        if (pieceRenderers[to.x, to.y] != null)
        {
            Transform piece = pieceRenderers[to.x, to.y].transform;
            AnimatePieceTo(piece, from, to);
            piece.name = PieceName(player, to.x, to.y);
        }

        // Jump capture: remove the jumped piece from the board immediately.
        if (jumped.x >= 0)
        {
            GameObject taken = pieceRenderers[jumped.x, jumped.y] != null
                ? pieceRenderers[jumped.x, jumped.y].gameObject
                : null;

            if (taken != null)
            {
                // Held by the snapshot before it goes to the pool: undo needs that same object back,
                // because its colour and crown are the state it was captured in.
                if (pendingPly != null)
                {
                    pendingPly.captured.Add(taken);
                    pendingPly.capturedCells.Add(new Vector2Int(jumped.x, jumped.y));
                }

                ReleasePiece(taken);
            }

            pieceRenderers[jumped.x, jumped.y] = null;
            grid[jumped.x, jumped.y] = Cell.Empty;
            isKing[jumped.x, jumped.y] = false;

            if (feedback != null) feedback.PlayCapture();
            Debug.Log($"[Feedback] capture - {(player == 1 ? "RED" : "BLUE")} jumped the piece at ({jumped.x},{jumped.y}) -> Capture");
        }
        else
        {
            if (feedback != null) feedback.PlayMove();
            Debug.Log($"[Feedback] move - {(player == 1 ? "RED" : "BLUE")} ({from.x},{from.y}) to ({to.x},{to.y}) -> Move");
        }

        // Promotion is checked after every landing, including mid-chain jumps.
        bool crownedHere = false;
        if (!isKing[to.x, to.y] &&
            ((player == 1 && to.y == Size - 1) || (player == 2 && to.y == 0)))
        {
            PromoteToDama(to.x, to.y);
            crownedHere = true;
        }

        if (crownedHere && pendingPly != null) pendingPly.promoted = true;

        // Week 20: a capture lands with a small scale pop. A promotion owns localScale through
        // PromotionPulse, so the two never run on the same piece in the same frame.
        if (jumped.x >= 0 && !crownedHere && pieceRenderers[to.x, to.y] != null)
            CapturePop(pieceRenderers[to.x, to.y].transform);

        // Chain capture: the same piece must keep jumping while captures remain.
        if (jumped.x >= 0)
        {
            List<Vector2Int> continuations = GetCaptureMoves(to.x, to.y);
            if (continuations.Count > 0)
            {
                ClearSelection();
                chainPiece = to;
                selected = to;
                selectedMoves.AddRange(continuations);
                RefreshUI();
                HighlightSquare(to, selectTint);
                foreach (Vector2Int move in selectedMoves) HighlightSquare(move, moveTint);
                return;
            }
        }

        EndTurn();
    }

    private void EndTurn()
    {
        chainPiece = new Vector2Int(-1, -1);
        ClearSelection();

        int nextPlayer = currentPlayer == 1 ? 2 : 1;

        // Week 20: one ply is one turn, so the snapshot opened before the turn's first move is
        // filed here - after the last jump of the chain, before the draw counters are asked.
        if (pendingPly != null)
        {
            // The key is the position the move has just left the board in - not the one the
            // snapshot holds, which is the position this turn started from.
            history.Push(pendingPly,
                         BuildPositionKey(nextPlayer),
                         pendingPly.captured.Count > 0,
                         pendingPly.promoted);
            pendingPly = null;
        }

        currentPlayer = nextPlayer;
        RefreshUI();
        CheckForWin();

        // Week 20: a match can also end level. Asked only if nobody lost, because a side with no
        // pieces has lost even when the position also repeated.
        if (gameState == GameState.Playing) CheckForDraw();

        // Week 19: the opponent answers the move that was just made.
        MaybeStartAiTurn();
    }

    /// <summary>Re-shows the forced jump chain after the player re-taps the piece.</summary>
    private void RehighlightChain()
    {
        ClearSelection();
        selected = chainPiece;
        selectedMoves.AddRange(GetCaptureMoves(chainPiece.x, chainPiece.y));
        HighlightSquare(chainPiece, selectTint);
        foreach (Vector2Int move in selectedMoves) HighlightSquare(move, moveTint);
    }

    /// <summary>
    /// Returns the enemy piece standing between two diagonal squares, or (-1,-1)
    /// when the move passes over empty squares only (a non-capture move).
    /// </summary>
    private Vector2Int FindJumpedCell(Vector2Int from, Vector2Int to)
    {
        int stepX = (to.x > from.x) ? 1 : (to.x < from.x ? -1 : 0);
        int stepY = (to.y > from.y) ? 1 : (to.y < from.y ? -1 : 0);
        if (stepX == 0 || stepY == 0) return new Vector2Int(-1, -1);
        if (Mathf.Abs(to.x - from.x) != Mathf.Abs(to.y - from.y)) return new Vector2Int(-1, -1);

        Vector2Int jumped = new Vector2Int(-1, -1);
        int cx = from.x + stepX;
        int cy = from.y + stepY;

        while (cx != to.x || cy != to.y)
        {
            if (grid[cx, cy] != Cell.Empty)
            {
                if (jumped.x >= 0) return new Vector2Int(-1, -1);
                jumped = new Vector2Int(cx, cy);
            }
            cx += stepX;
            cy += stepY;
        }

        return jumped;
    }

    /// <summary>Non-capture destinations: one step forward for men, any distance for a Dama.</summary>
    private List<Vector2Int> GetSimpleMoves(int x, int y)
    {
        List<Vector2Int> moves = simpleBuffer;
        moves.Clear();
        Cell me = grid[x, y];
        if (me == Cell.Empty) return moves;

        if (isKing[x, y])
        {
            // Flying Dama: slide any distance along any of the 4 diagonals.
            for (int dx = -1; dx <= 1; dx += 2)
            {
                for (int dy = -1; dy <= 1; dy += 2)
                {
                    int cx = x + dx;
                    int cy = y + dy;
                    while (InBounds(cx, cy) && grid[cx, cy] == Cell.Empty)
                    {
                        moves.Add(new Vector2Int(cx, cy));
                        cx += dx;
                        cy += dy;
                    }
                }
            }
            return moves;
        }

        int direction = me == Cell.Player1 ? 1 : -1;
        for (int dx = -1; dx <= 1; dx += 2)
        {
            int stepX = x + dx;
            int stepY = y + direction;
            if (InBounds(stepX, stepY) && grid[stepX, stepY] == Cell.Empty)
                moves.Add(new Vector2Int(stepX, stepY));
        }

        return moves;
    }

    /// <summary>
    /// Capture destinations. Men jump an adjacent enemy forward; a Dama jumps the
    /// first enemy found on a diagonal from any distance and may land on any
    /// empty square beyond it.
    /// </summary>
    private List<Vector2Int> GetCaptureMoves(int x, int y)
    {
        List<Vector2Int> moves = captureBuffer;
        moves.Clear();
        Cell me = grid[x, y];
        if (me == Cell.Empty) return moves;

        if (isKing[x, y])
        {
            for (int dx = -1; dx <= 1; dx += 2)
            {
                for (int dy = -1; dy <= 1; dy += 2)
                {
                    int cx = x + dx;
                    int cy = y + dy;

                    // Scan to the first piece along this diagonal.
                    while (InBounds(cx, cy) && grid[cx, cy] == Cell.Empty)
                    {
                        cx += dx;
                        cy += dy;
                    }
                    if (!InBounds(cx, cy) || grid[cx, cy] == me) continue;

                    // Enemy found: every empty square beyond it is a landing square.
                    int landX = cx + dx;
                    int landY = cy + dy;
                    while (InBounds(landX, landY) && grid[landX, landY] == Cell.Empty)
                    {
                        moves.Add(new Vector2Int(landX, landY));
                        landX += dx;
                        landY += dy;
                    }
                }
            }
            return moves;
        }

        // Man: jump over an adjacent enemy piece (forward only, as before).
        int direction = me == Cell.Player1 ? 1 : -1;
        for (int dx = -1; dx <= 1; dx += 2)
        {
            int stepX = x + dx;
            int stepY = y + direction;
            int landX = x + 2 * dx;
            int landY = y + 2 * direction;
            if (InBounds(landX, landY) &&
                grid[stepX, stepY] != Cell.Empty &&
                grid[stepX, stepY] != me &&
                grid[landX, landY] == Cell.Empty)
            {
                moves.Add(new Vector2Int(landX, landY));
            }
        }

        return moves;
    }

    /// <summary>Legal destinations under the mandatory-capture rule.</summary>
    private List<Vector2Int> GetSelectableMoves(int x, int y)
    {
        if (!moveBuffersReported)
        {
            moveBuffersReported = true;
            Debug.Log("[Perf] move generation writes into reusable buffers (captureBuffer, simpleBuffer): no List is allocated per piece scanned");
        }

        List<Vector2Int> captures = GetCaptureMoves(x, y);
        if (captures.Count > 0) return captures;

        // Another piece must capture: this piece may only move if none can.
        if (PlayerHasCapture(grid[x, y])) return noMoves;

        return GetSimpleMoves(x, y);
    }

    /// <summary>True when any piece of the given side can capture right now.</summary>
    private bool PlayerHasCapture(Cell me)
    {
        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                if (grid[x, y] != me) continue;
                if (HasCaptureAt(x, y)) return true;
            }
        }

        return false;
    }

    private void HighlightSquare(Vector2Int cell, Color tint)
    {
        SpriteRenderer renderer = squareRenderers[cell.x, cell.y];
        if (renderer == null) return;

        renderer.color = Color.Lerp(BaseSquareColor(cell.x, cell.y), tint, 0.65f);
        if (!highlightedCells.Contains(cell)) highlightedCells.Add(cell);
    }

    private void ClearSelection()
    {
        foreach (Vector2Int cell in highlightedCells)
            if (squareRenderers[cell.x, cell.y] != null)
                squareRenderers[cell.x, cell.y].color = BaseSquareColor(cell.x, cell.y);

        highlightedCells.Clear();
        selectedMoves.Clear();
        selected = new Vector2Int(-1, -1);
    }

    // ---- Week 20: undo, hint, draw rule and move animation ----

    /// <summary>Scratch arrays for the position key, so a draw check allocates nothing.</summary>
    private static readonly int[] keyCells = new int[PlySnapshot.BoardCells];
    private static readonly bool[] keyKings = new bool[PlySnapshot.BoardCells];

    /// <summary>The FNV-1a key of the live board with <paramref name="sideToMove"/> to move.</summary>
    private ulong BuildPositionKey(int sideToMove)
    {
        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                int index = x * Size + y;
                keyCells[index] = (int)grid[x, y];
                keyKings[index] = isKing[x, y];
            }
        }

        return DamaPlyHistory.Key(keyCells, keyKings, sideToMove);
    }

    /// <summary>
    /// Opens an undo record for the turn that is about to be played. Called from ExecuteMove before
    /// the rule arrays change, so the snapshot holds the position the player is looking at - the one
    /// an undo has to put back.
    /// </summary>
    private PlySnapshot CaptureSnapshot()
    {
        PlySnapshot snapshot = new PlySnapshot();

        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                int index = x * Size + y;
                snapshot.cells[index] = (int)grid[x, y];
                snapshot.kings[index] = isKing[x, y];
                snapshot.renderers[index] = pieceRenderers[x, y];
            }
        }

        snapshot.player = currentPlayer;
        snapshot.chainX = chainPiece.x;
        snapshot.chainY = chainPiece.y;
        snapshot.selected = selected;

        return snapshot;
    }

    /// <summary>
    /// Puts a snapshot back on the board: the rule arrays first, then one pass over the scene that
    /// makes every piece agree with them. A captured piece comes back out of the pool because it was
    /// only ever parked there, never destroyed.
    /// </summary>
    private void RestoreFrom(PlySnapshot snapshot)
    {
        if (snapshot == null) return;

        Transform piecesRoot = PiecesRoot();

        for (int i = 0; i < snapshot.captured.Count; i++)
        {
            GameObject piece = snapshot.captured[i];
            if (piece == null) continue;

            piecePool.Remove(piece);
            if (piecesRoot != null) piece.transform.SetParent(piecesRoot, false);
            piece.SetActive(true);
        }

        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                int index = x * Size + y;
                grid[x, y] = (Cell)snapshot.cells[index];
                isKing[x, y] = snapshot.kings[index];
                pieceRenderers[x, y] = snapshot.renderers[index];
            }
        }

        // One sweep that makes the scene match the arrays: a chain may already have moved or crowned
        // pieces before the turn was taken back, and this is what un-moves them.
        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                SpriteRenderer renderer = pieceRenderers[x, y];
                if (renderer == null) continue;

                int owner = snapshot.cells[x * Size + y];
                if (owner == 0) continue;

                Transform piece = renderer.transform;
                if (piecesRoot != null) piece.SetParent(piecesRoot, false);
                piece.localPosition = CellToLocal(x, y);
                piece.localScale = Vector3.one * 0.82f;
                piece.name = PieceName(owner, x, y);

                Transform crown = CrownChild(piece);
                if (crown != null) crown.gameObject.SetActive(isKing[x, y]);
            }
        }

        currentPlayer = snapshot.player;
        chainPiece = new Vector2Int(snapshot.chainX, snapshot.chainY);

        ClearSelection();
        if (chainPiece.x >= 0) RehighlightChain();

        RefreshUI();
        RefreshModePanel();

        Debug.Log($"[Week20] UNDO: board restored to the position before " +
                  $"{(snapshot.player == 1 ? "RED" : "BLUE")}'s turn ({snapshot.captured.Count} piece(s) back), " +
                  $"{history.Count} ply/plies still undoable, turn={(currentPlayer == 1 ? "RED" : "BLUE")}.");
    }

    /// <summary>PAUSE panel: takes the last turn back, and in an AI match hands the board to you.</summary>
    public void UndoPressed()
    {
        if (gameState != GameState.Paused && gameState != GameState.Playing) return;

        // The opponent must not move while the board is being rewound.
        CancelAiTurn();
        StopMoveAnimation();

        if (!history.TryPop(out PlySnapshot snapshot))
        {
            Debug.Log("[Week20] UNDO: there is no completed turn to take back yet.");
            return;
        }

        RestoreFrom(snapshot);
        int undone = 1;

        // One press hands the turn back to the human: if the AI has already answered, its reply is
        // taken back with the human's own move, so a single press never leaves the AI to move again.
        while (playMode == PlayMode.VsAi && currentPlayer == aiSide && history.Count > 0)
        {
            if (!history.TryPop(out PlySnapshot earlier)) break;
            RestoreFrom(earlier);
            undone++;
        }

        Debug.Log($"[Week20] UNDO taken: {undone} turn(s) rewound, {history.Count} left in the pile.");

        // Paused is the normal case (the button lives in the pause menu): the AI restarts on RESUME.
        MaybeStartAiTurn();
    }

    /// <summary>HUD: paints a legal move for the side to move. It suggests, it never plays.</summary>
    public void HintPressed()
    {
        if (gameState != GameState.Playing || aiThinking)
        {
            Debug.Log("[Week20] HINT ignored: the board is not waiting for a move.");
            return;
        }

        Vector2Int forced = chainPiece.x >= 0 ? chainPiece : new Vector2Int(-1, -1);
        DamaAI.Choice choice = DamaAI.Choose(SnapshotPosition(), currentPlayer, AiLevel.Easy, forced);

        if (!choice.valid)
        {
            Debug.LogWarning($"[Week20] HINT found no legal move for {(currentPlayer == 1 ? "RED" : "BLUE")}.");
            return;
        }

        // The tint goes through HighlightSquare, so the next ClearSelection repaints these squares.
        ClearSelection();
        HighlightSquare(choice.from, hintTint);
        HighlightSquare(choice.to, hintTint);

        Debug.Log($"[Week20] HINT for {(currentPlayer == 1 ? "RED" : "BLUE")}: {choice} (from and to painted green).");
    }

    /// <summary>The draw rule: threefold repetition, or 50 plies with no capture and no promotion.</summary>
    private void CheckForDraw()
    {
        DamaDrawReason reason = history.Verdict(out int evidence);
        if (reason == DamaDrawReason.None) return;

        DeclareDraw(reason, evidence);
    }

    /// <summary>
    /// Shows that the match ended level. A draw banks as winner 0, which PlayerProfile already
    /// records as "DRAW" and counts separately from a win or a loss.
    /// </summary>
    private void DeclareDraw(DamaDrawReason reason, int evidence)
    {
        int red = CountPieces(1);
        int blue = CountPieces(2);

        if (winText != null)
        {
            winText.text = "DRAW";
            winText.color = new Color(0.88f, 0.88f, 0.88f);
        }

        if (winSummaryText != null)
        {
            winSummaryText.text = reason == DamaDrawReason.Repetition
                ? $"DRAW.  RED {red}-{blue}, the same position {evidence} times."
                : $"DRAW.  RED {red}-{blue}, {evidence} plies without a capture.";
        }

        Debug.Log($"[Week20] End screen: DRAW by {reason}, RED {red}-{blue}.");

        if (feedback != null) feedback.PlayDraw();

        BankMatch(0);

        Time.timeScale = 0f;
        ShowState(GameState.GameOver);
    }

    /// <summary>Stops a move or a pop that is still running, e.g. on restart.</summary>
    private void StopMoveAnimation()
    {
        if (moveAnimation != null)
        {
            StopCoroutine(moveAnimation);
            moveAnimation = null;
        }

        if (capturePop != null)
        {
            StopCoroutine(capturePop);
            capturePop = null;
        }

        moveAnimating = false;
    }

    /// <summary>
    /// Moves a piece to its new cell over 120 ms instead of teleporting. localPosition, not position:
    /// the board root is turned for the BLUE view, and the cell coordinates are local to it.
    /// </summary>
    private void AnimatePieceTo(Transform piece, Vector2Int from, Vector2Int to)
    {
        if (piece == null) return;

        // Edit mode has no coroutines and no delta time, so a move is snapped there and every
        // harness that drives ExecuteMove still finds the piece on its cell immediately.
        if (!Application.isPlaying)
        {
            piece.localPosition = CellToLocal(to.x, to.y);
            return;
        }

        if (moveAnimation != null) StopCoroutine(moveAnimation);

        moveAnimating = true;
        moveAnimation = StartCoroutine(MovePieceRoutine(piece, from, to));

        Debug.Log($"[Week20] animate ({from.x},{from.y}) -> ({to.x},{to.y}) over 120 ms.");
    }

    private IEnumerator MovePieceRoutine(Transform piece, Vector2Int from, Vector2Int to)
    {
        const float duration = 0.12f;

        Vector3 start = CellToLocal(from.x, from.y);
        Vector3 end = CellToLocal(to.x, to.y);
        float elapsed = 0f;

        while (piece != null && elapsed < duration)
        {
            // Unscaled: the pause menu stops scaled time, and a move in flight must still finish.
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            piece.localPosition = Vector3.Lerp(start, end, 1f - (1f - t) * (1f - t));
            yield return null;
        }

        if (piece != null) piece.localPosition = end;

        moveAnimation = null;
        moveAnimating = false;
    }

    /// <summary>A short scale pop so a capture reads as a hit rather than as a piece vanishing.</summary>
    private void CapturePop(Transform piece)
    {
        if (piece == null) return;
        if (!Application.isPlaying) return;

        // PromotionPulse owns localScale for a quarter of a second; the pop waits its turn.
        if (promotionPulse != null) return;

        if (capturePop != null) StopCoroutine(capturePop);
        capturePop = StartCoroutine(CapturePopRoutine(piece));
    }

    private IEnumerator CapturePopRoutine(Transform piece)
    {
        const float duration = 0.12f;
        const float baseScale = 0.82f;
        const float bulge = 0.12f;
        float elapsed = 0f;

        while (piece != null && elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            piece.localScale = Vector3.one * (baseScale * (1f + bulge * Mathf.Sin(t * Mathf.PI)));
            yield return null;
        }

        if (piece != null) piece.localScale = Vector3.one * baseScale;
        capturePop = null;
    }

    // ---- Helpers ----

    private static bool TryGetTapScreenPoint(out Vector2 screenPoint)
    {
        screenPoint = default;

        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
        {
            screenPoint = touchscreen.primaryTouch.position.ReadValue();
            return true;
        }

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            screenPoint = mouse.position.ReadValue();
            return true;
        }

        return false;
    }

    private static Vector3 CellToLocal(int x, int y)
    {
        return new Vector3(x - (Size - 1) * 0.5f, y - (Size - 1) * 0.5f, 0f);
    }

    private static bool InBounds(int x, int y)
    {
        return x >= 0 && x < Size && y >= 0 && y < Size;
    }

    private static bool IsDark(int x, int y)
    {
        return (x + y) % 2 == 1;
    }

    private Color BaseSquareColor(int x, int y)
    {
        return IsDark(x, y) ? darkSquareColor : lightSquareColor;
    }

    private static Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
    }

    private static Sprite CreateCircleSprite()
    {
        const int resolution = 64;
        Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[resolution * resolution];
        Vector2 center = new Vector2(resolution * 0.5f, resolution * 0.5f);
        float radius = resolution * 0.5f - 1f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float alpha = Mathf.Clamp01((radius - distance) / 1.5f);
                pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, resolution, resolution), new Vector2(0.5f, 0.5f), resolution);
    }
}
