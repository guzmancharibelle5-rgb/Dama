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
/// Turns alternate between two local players.
/// UI: start screen, pause menu, restart and win screen on a single Canvas.
/// Not yet included (next steps): AI opponent, online multiplayer.
/// </summary>
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

    [Header("Dama (King)")]
    [SerializeField] private Color crownColor = new Color(1f, 0.82f, 0.15f, 1f);
    [SerializeField] private float crownScale = 0.55f;

    private enum Cell { Empty = 0, Player1 = 1, Player2 = 2 }

    private readonly Cell[,] grid = new Cell[Size, Size];
    private readonly SpriteRenderer[,] squareRenderers = new SpriteRenderer[Size, Size];
    private readonly SpriteRenderer[,] pieceRenderers = new SpriteRenderer[Size, Size];
    private readonly List<Vector2Int> highlightedCells = new List<Vector2Int>();
    private readonly List<Vector2Int> selectedMoves = new List<Vector2Int>();
    private readonly bool[,] isKing = new bool[Size, Size];

    private Camera mainCamera;
    private int currentPlayer = 1;
    private Vector2Int selected = new Vector2Int(-1, -1);
    private Vector2Int chainPiece = new Vector2Int(-1, -1);
    private Coroutine promotionPulse;

    private enum GameState { Ready, Playing, Paused, GameOver }

    private GameState gameState = GameState.Ready;
    private GameObject startPanel;
    private GameObject hudPanel;
    private GameObject pausePanel;
    private GameObject winPanel;
    private Text turnText;
    private Text countsText;
    private Text winText;

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
        if (!TryGetTapScreenPoint(out Vector2 screenPoint)) return;

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 world = mainCamera.ScreenToWorldPoint(screenPoint);
        int x = Mathf.FloorToInt(world.x + Size * 0.5f);
        int y = Mathf.FloorToInt(world.y + Size * 0.5f);

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

        Transform hud = canvas.transform.Find("HUDPanel");
        if (hud != null)
        {
            turnText = hud.Find("TurnText")?.GetComponent<Text>();
            countsText = hud.Find("CountsText")?.GetComponent<Text>();
        }

        Transform win = canvas.transform.Find("WinPanel");
        if (win != null) winText = win.Find("WinText")?.GetComponent<Text>();

        WireButton(canvas, "StartPanel/StartButton", StartGame);
        WireButton(canvas, "HUDPanel/PauseButton", PauseGame);
        WireButton(canvas, "PausePanel/ResumeButton", ResumeGame);
        WireButton(canvas, "PausePanel/RestartButtonPause", RestartGame);
        WireButton(canvas, "WinPanel/RestartButtonWin", RestartGame);
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
        if (hudPanel != null) hudPanel.SetActive(state != GameState.Ready);
        if (pausePanel != null) pausePanel.SetActive(state == GameState.Paused);
        if (winPanel != null) winPanel.SetActive(state == GameState.GameOver);
    }

    private void RefreshUI()
    {
        if (turnText != null)
            turnText.text = $"Turn: {(currentPlayer == 1 ? "RED" : "BLUE")}" +
                            (chainPiece.x >= 0 ? "  (chain jump!)" : "");

        if (countsText != null)
            countsText.text = $"Red: {CountPieces(1)}   Blue: {CountPieces(2)}";
    }

    /// <summary>Hides the title screen and starts the game.</summary>
    public void StartGame()
    {
        Time.timeScale = 1f;
        ShowState(GameState.Playing);
        RefreshUI();
    }

    /// <summary>Freezes the board (input is gated in Update) and shows the pause menu.</summary>
    public void PauseGame()
    {
        if (gameState != GameState.Playing) return;
        Time.timeScale = 0f;
        ShowState(GameState.Paused);
    }

    public void ResumeGame()
    {
        if (gameState != GameState.Paused) return;
        Time.timeScale = 1f;
        ShowState(GameState.Playing);
    }

    /// <summary>
    /// Single restart entry point (Pause panel and Win panel): clears the board,
    /// respawns all 24 pieces, resets turn/chain/king state and UI, unpauses.
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;

        if (promotionPulse != null)
        {
            StopCoroutine(promotionPulse);
            promotionPulse = null;
        }

        chainPiece = new Vector2Int(-1, -1);
        ClearSelection();
        currentPlayer = 1;

        Transform piecesRoot = transform.Find("Pieces");
        if (piecesRoot != null)
        {
            for (int i = piecesRoot.childCount - 1; i >= 0; i--)
                Destroy(piecesRoot.GetChild(i).gameObject);
        }

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

        ShowState(GameState.Playing);
        RefreshUI();
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

    private void DeclareWinner(int winner)
    {
        if (winText != null)
        {
            winText.text = winner == 1 ? "RED WIN!" : "BLUE WIN!";
            winText.color = winner == 1
                ? new Color(1f, 0.25f, 0.25f)
                : new Color(0.35f, 0.55f, 1f);
        }

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

        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                if (grid[x, y] != me) continue;
                if (GetSelectableMoves(x, y).Count > 0) return true;
            }
        }

        return false;
    }



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
                if (IsDark(x, y)) RegisterPiece(CreatePiece(piecesRoot, circle, 1, x, y), 1, x, y);

        for (int y = 5; y <= 7; y++)
            for (int x = 0; x < Size; x++)
                if (IsDark(x, y)) RegisterPiece(CreatePiece(piecesRoot, circle, 2, x, y), 2, x, y);
    }

    /// <summary>Places a spawned piece into the board state arrays.</summary>
    private void RegisterPiece(GameObject piece, int player, int x, int y)
    {
        grid[x, y] = player == 1 ? Cell.Player1 : Cell.Player2;
        pieceRenderers[x, y] = piece.GetComponent<SpriteRenderer>();
        isKing[x, y] = false;
    }

    private GameObject CreatePiece(Transform parent, Sprite sprite, int player, int x, int y)
    {
        GameObject piece = new GameObject($"Piece_{player}_{x}_{y}");
        piece.transform.SetParent(parent, false);
        piece.transform.localPosition = CellToLocal(x, y);
        piece.transform.localScale = Vector3.one * 0.82f;

        SpriteRenderer renderer = piece.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = player == 1 ? player1Color : player2Color;
        renderer.sortingOrder = 1;
        return piece;
    }
    /// <summary>Turns a man into a flying Dama: sets the flag and stacks a gold crown.</summary>
    private void PromoteToDama(int x, int y)
    {
        isKing[x, y] = true;

        SpriteRenderer pieceRenderer = pieceRenderers[x, y];
        if (pieceRenderer == null) return;

        if (pieceRenderer.transform.Find("Crown") == null)
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

        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                Transform square = squaresRoot.Find($"Square_{x}_{y}");
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
            isKing[x, y] = piece.Find("Crown") != null;
        }

        return true;
    }

    // ---- Interaction ----

    private void HandleTap(Vector2Int cell)
    {
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

        grid[to.x, to.y] = moving;
        grid[from.x, from.y] = Cell.Empty;
        isKing[to.x, to.y] = isKing[from.x, from.y];
        isKing[from.x, from.y] = false;
        pieceRenderers[to.x, to.y] = pieceRenderers[from.x, from.y];
        pieceRenderers[from.x, from.y] = null;

        if (pieceRenderers[to.x, to.y] != null)
        {
            Transform piece = pieceRenderers[to.x, to.y].transform;
            piece.position = transform.TransformPoint(CellToLocal(to.x, to.y));
            piece.name = $"Piece_{player}_{to.x}_{to.y}";
        }

        // Jump capture: remove the jumped piece from the board immediately.
        if (jumped.x >= 0)
        {
            if (pieceRenderers[jumped.x, jumped.y] != null)
                Destroy(pieceRenderers[jumped.x, jumped.y].gameObject);
            pieceRenderers[jumped.x, jumped.y] = null;
            grid[jumped.x, jumped.y] = Cell.Empty;
            isKing[jumped.x, jumped.y] = false;
        }

        // Promotion is checked after every landing, including mid-chain jumps.
        if (!isKing[to.x, to.y] &&
            ((player == 1 && to.y == Size - 1) || (player == 2 && to.y == 0)))
        {
            PromoteToDama(to.x, to.y);
        }

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
        currentPlayer = currentPlayer == 1 ? 2 : 1;
        RefreshUI();
        CheckForWin();
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
        List<Vector2Int> moves = new List<Vector2Int>();
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
        List<Vector2Int> moves = new List<Vector2Int>();
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
        List<Vector2Int> captures = GetCaptureMoves(x, y);
        if (captures.Count > 0) return captures;

        // Another piece must capture: this piece may only move if none can.
        if (PlayerHasCapture(grid[x, y])) return new List<Vector2Int>();

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
                if (GetCaptureMoves(x, y).Count > 0) return true;
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
