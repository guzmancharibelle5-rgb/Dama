using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor tool: generates simple square/circle sprite assets and builds the
/// Dama board layout in the open scene. Menu: Tools > Dama > Build Board Layout
/// </summary>
public static class DamaBoardBuilder
{
    private const string SpriteFolder = "Assets/Sprites";
    private const string SquareSpritePath = SpriteFolder + "/BoardSquare.png";
    private const string CircleSpritePath = SpriteFolder + "/PieceCircle.png";

    [MenuItem("Tools/Dama/Build Board Layout")]
    public static void BuildBoardLayout()
    {
        EnsureSpriteAssets();

        DamaBoard board = Object.FindAnyObjectByType<DamaBoard>();
        if (board == null)
        {
            GameObject root = new GameObject("DamaBoard");
            board = root.AddComponent<DamaBoard>();
        }

        if (board.transform.Find("Squares") != null)
        {
            Debug.Log("Dama: board layout already exists in the scene - nothing changed.");
            return;
        }

        board.SetSprites(
            AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath),
            AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath));
        board.BuildLayout();
        EditorUtility.SetDirty(board);

        EnsureCamera();

        Debug.Log("Dama: board layout built (64 squares, 24 pieces).");
    }

    private static void EnsureCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();
            mainCamera = cameraObject.AddComponent<Camera>();
        }

        mainCamera.orthographic = true;
        mainCamera.orthographicSize = 5f;
        mainCamera.transform.position = new Vector3(0f, 0f, -10f);
        mainCamera.backgroundColor = new Color(0.13f, 0.13f, 0.16f, 1f);
        EditorUtility.SetDirty(mainCamera);
    }

    private static void EnsureSpriteAssets()
    {
        if (!AssetDatabase.IsValidFolder(SpriteFolder))
            AssetDatabase.CreateFolder("Assets", "Sprites");

        CreateSpriteAsset(SquareSpritePath, false);
        CreateSpriteAsset(CircleSpritePath, true);
    }

    private static void CreateSpriteAsset(string path, bool circle)
    {
        if (File.Exists(path)) return;

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (!circle)
                {
                    pixels[y * size + x] = Color.white;
                    continue;
                }

                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float alpha = Mathf.Clamp01((size * 0.5f - 1f - distance) / 1.5f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = size;
        importer.filterMode = circle ? FilterMode.Bilinear : FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }
}
