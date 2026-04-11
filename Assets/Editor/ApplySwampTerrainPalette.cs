using System.IO;
using UnityEditor;
using UnityEngine;

public static class ApplySwampTerrainPalette
{
    private const string OutputFolder = "Assets/Generated/TerrainPalette";
    private const string DarkTexturePath = OutputFolder + "/SwampDark_112520.asset";
    private const string OliveTexturePath = OutputFolder + "/SwampOlive_9A9616.asset";
    private const string MidTexturePath = OutputFolder + "/SwampMid_1E2C22.asset";
    private const string DarkLayerPath = OutputFolder + "/SwampDark_112520.terrainlayer";
    private const string OliveLayerPath = OutputFolder + "/SwampOlive_9A9616.terrainlayer";
    private const string MidLayerPath = OutputFolder + "/SwampMid_1E2C22.terrainlayer";

    private static readonly Color DarkColor = ParseHexColor("112520");
    private static readonly Color OliveColor = ParseHexColor("9A9616");
    private static readonly Color MidColor = ParseHexColor("1E2C22");

    [MenuItem("Tools/Terrain/Apply Swamp Palette")]
    public static void ApplyPalette()
    {
        Terrain terrain = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponent<Terrain>()
            : null;

        if (terrain == null)
        {
            terrain = Object.FindFirstObjectByType<Terrain>();
        }

        if (terrain == null)
        {
            Debug.LogError("Apply Swamp Palette: no Terrain found in the scene.");
            return;
        }

        TerrainData terrainData = terrain.terrainData;
        if (terrainData == null)
        {
            Debug.LogError("Apply Swamp Palette: target terrain has no TerrainData.");
            return;
        }

        EnsureFolder(OutputFolder);

        TerrainLayer darkLayer = LoadOrCreateLayer(DarkLayerPath, DarkTexturePath, "Swamp Dark", DarkColor);
        TerrainLayer oliveLayer = LoadOrCreateLayer(OliveLayerPath, OliveTexturePath, "Swamp Olive", OliveColor);
        TerrainLayer midLayer = LoadOrCreateLayer(MidLayerPath, MidTexturePath, "Swamp Mid", MidColor);

        Undo.RegisterCompleteObjectUndo(terrainData, "Apply Swamp Palette");
        terrainData.terrainLayers = new[] { darkLayer, oliveLayer, midLayer };
        PaintTerrain(terrainData);

        EditorUtility.SetDirty(terrainData);
        EditorUtility.SetDirty(terrain);
        AssetDatabase.SaveAssets();

        Debug.Log($"Apply Swamp Palette: painted '{terrain.name}' using palette 112520 / 9A9616 / 1E2C22.");
    }

    private static void PaintTerrain(TerrainData terrainData)
    {
        int width = terrainData.alphamapWidth;
        int height = terrainData.alphamapHeight;
        int layerCount = terrainData.terrainLayers.Length;
        float[,,] alphamaps = new float[height, width, layerCount];

        Vector3 size = terrainData.size;
        float invMaxHeight = size.y > 0.0001f ? 1f / size.y : 1f;

        for (int y = 0; y < height; y++)
        {
            float normY = y / (float)(height - 1);
            for (int x = 0; x < width; x++)
            {
                float normX = x / (float)(width - 1);
                float terrainHeight = terrainData.GetInterpolatedHeight(normX, normY) * invMaxHeight;
                float steepness = terrainData.GetSteepness(normX, normY) / 90f;

                float macroNoise = Mathf.PerlinNoise(normX * 2.15f + 4.7f, normY * 2.15f + 1.3f);
                float breakupNoise = Mathf.PerlinNoise(normX * 6.8f + 8.2f, normY * 6.8f + 2.6f);
                float detailNoise = Mathf.PerlinNoise(normX * 15.4f + 3.1f, normY * 15.4f + 9.4f);
                float wetness = Mathf.Clamp01((0.22f - terrainHeight) * 0.8f + (0.45f - macroNoise) * 0.45f);

                float dark = Mathf.Clamp01(1f - Mathf.Abs(macroNoise - 0.18f) / 0.26f);
                float olive = Mathf.Clamp01(1f - Mathf.Abs(macroNoise - 0.52f) / 0.34f);
                float mid = Mathf.Clamp01(1f - Mathf.Abs(macroNoise - 0.82f) / 0.24f);

                dark = Mathf.Clamp01(dark + wetness * 0.45f + (1f - detailNoise) * 0.1f);
                olive = Mathf.Clamp01(olive + breakupNoise * 0.18f - wetness * 0.12f);
                mid = Mathf.Clamp01(mid + steepness * 0.2f + detailNoise * 0.08f);

                float total = dark + olive + mid;
                if (total <= 0.0001f)
                {
                    dark = 0.2f;
                    olive = 0.6f;
                    mid = 0.2f;
                    total = 1f;
                }

                alphamaps[y, x, 0] = dark / total;
                alphamaps[y, x, 1] = olive / total;
                alphamaps[y, x, 2] = mid / total;
            }
        }

        terrainData.SetAlphamaps(0, 0, alphamaps);
    }

    private static TerrainLayer LoadOrCreateLayer(string layerPath, string texturePath, string layerName, Color color)
    {
        TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

        if (texture == null)
        {
            texture = CreateColorTexture(texturePath, color);
        }

        if (layer == null)
        {
            layer = new TerrainLayer();
            AssetDatabase.CreateAsset(layer, layerPath);
        }

        layer.diffuseTexture = texture;
        layer.tileSize = new Vector2(20f, 20f);
        layer.tileOffset = Vector2.zero;
        layer.diffuseRemapMax = new Vector4(1f, 1f, 1f, 1f);
        layer.diffuseRemapMin = Vector4.zero;
        layer.name = layerName;
        EditorUtility.SetDirty(layer);
        return layer;
    }

    private static Texture2D CreateColorTexture(string assetPath, Color color)
    {
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
        {
            name = Path.GetFileNameWithoutExtension(assetPath),
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };

        Color[] pixels = { color, color, color, color };
        texture.SetPixels(pixels);
        texture.Apply();
        AssetDatabase.CreateAsset(texture, assetPath);
        return texture;
    }

    private static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static Color ParseHexColor(string value)
    {
        if (ColorUtility.TryParseHtmlString("#" + value, out Color color))
        {
            return color;
        }

        return Color.magenta;
    }
}
