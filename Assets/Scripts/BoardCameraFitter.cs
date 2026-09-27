using UnityEngine;

/// <summary>
/// Keeps the full 8x8 Dama board visible on any screen size or aspect ratio.
/// Recomputes the orthographic camera size from the screen dimensions and
/// re-fits automatically when the screen changes (device rotation, resize).
/// </summary>
[RequireComponent(typeof(Camera))]
public class BoardCameraFitter : MonoBehaviour
{
    [Header("Framing")]
    [Tooltip("Half the board's world size. The 8x8 board spans -4..4 around the origin.")]
    [SerializeField] private float boardHalfExtent = 4f;
    [Tooltip("Extra breathing room around the board, in world units.")]
    [SerializeField] private float margin = 0.4f;

    private Camera boardCamera;
    private int lastScreenWidth;
    private int lastScreenHeight;

    private void Awake()
    {
        TryGetComponent(out boardCamera);
        Fit();
    }

    private void Update()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            Fit();
    }

    /// <summary>Recomputes the orthographic size for the current screen.</summary>
    public void Fit()
    {
        if (boardCamera == null) TryGetComponent(out boardCamera);
        if (boardCamera == null) return;

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        float halfExtent = boardHalfExtent + margin;

        boardCamera.orthographic = true;
        boardCamera.orthographicSize = Mathf.Max(halfExtent, halfExtent / aspect);
    }
}
