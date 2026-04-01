using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
public class TerrainRoadCourseBuilder : MonoBehaviour
{
    [System.Serializable] struct RoadSample
    {
        public Vector3 position; public float width; public float bank; public float terrainBlend;
        public RoadSample(Vector3 p, float w, float b, float t) { position = p; width = w; bank = b; terrainBlend = t; }
    }

    public Vector3 terrainSize = new Vector3(640f, 80f, 1120f);
    public int heightmapResolution = 1025;
    public int alphamapResolution = 1024;
    public float roadThickness = 0.42f;
    public float shoulderWidth = 10f;
    public bool autoBuildIfEmpty = true;
    public Material roadMaterial;
    public TerrainLayer grassLayer;
    public TerrainLayer dirtLayer;

#if UNITY_EDITOR
    const string GeneratedFolder = "Assets/Levels/Controllers Trial/Generated";
    const string GrassLayerPath = "Assets/Handpainted_Grass_and_Ground_Textures/Demo/terrain_layers/Grass_desatured_up.terrainlayer";
    const string DirtLayerPath = "Assets/Handpainted_Grass_and_Ground_Textures/Demo/terrain_layers/dirt_desatured_up.terrainlayer";
    const string DirtMaterialPath = "Assets/EasyRoads3D/Resources/Materials/roads/dirt material.mat";

    void OnEnable() { if (!Application.isPlaying && autoBuildIfEmpty && transform.childCount == 0) Build(); }

    [ContextMenu("Rebuild Terrain Rover Course")]
    public void Build()
    {
        EnsureRefs(); ClearChildren();
        List<RoadSample> road = BuildRoad();
        TerrainData data = GetTerrainData();
        ApplyHeights(data, road);
        ApplyTextures(data, road);
        CreateTerrain(data);
        CreateRoad(road);
        CreateBridgeSet();
        CreateFinish(road[road.Count - 1].position);
        EditorUtility.SetDirty(this);
        if (gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(gameObject.scene);
        AssetDatabase.SaveAssets();
    }

    void EnsureRefs()
    {
        if (grassLayer == null) grassLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(GrassLayerPath);
        if (dirtLayer == null) dirtLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(DirtLayerPath);
        if (roadMaterial == null) roadMaterial = AssetDatabase.LoadAssetAtPath<Material>(DirtMaterialPath);
    }

    void ClearChildren() { for (int i = transform.childCount - 1; i >= 0; i--) DestroyImmediate(transform.GetChild(i).gameObject); }

    TerrainData GetTerrainData()
    {
        EnsureFolders();
        string path = GeneratedFolder + "/" + Clean(gameObject.name) + "_TerrainData.asset";
        TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
        if (data == null) { data = new TerrainData(); AssetDatabase.CreateAsset(data, path); }
        data.heightmapResolution = Mathf.ClosestPowerOfTwo(Mathf.Clamp(heightmapResolution - 1, 32, 4096)) + 1;
        data.alphamapResolution = Mathf.Clamp(Mathf.ClosestPowerOfTwo(Mathf.Max(16, alphamapResolution)), 16, 2048);
        data.baseMapResolution = 512;
        data.size = terrainSize;
        if (grassLayer != null && dirtLayer != null) data.terrainLayers = new[] { grassLayer, dirtLayer };
        EditorUtility.SetDirty(data);
        return data;
    }

    void CreateTerrain(TerrainData data)
    {
        GameObject go = Terrain.CreateTerrainGameObject(data);
        go.name = "TerrainSurface";
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(-terrainSize.x * 0.5f, 0f, -terrainSize.z * 0.5f);
        Terrain t = go.GetComponent<Terrain>();
        t.drawInstanced = true; t.heightmapPixelError = 4f; t.basemapDistance = 800f;
    }

    List<RoadSample> BuildRoad()
    {
        Vector3[] cp =
        {
            new Vector3(-6f, 4.8f, -438f),
            new Vector3(-4f, 5.0f, -380f),
            new Vector3(8f, 5.4f, -322f),
            new Vector3(42f, 6.2f, -278f),
            new Vector3(86f, 7.4f, -232f),
            new Vector3(54f, 8.2f, -178f),
            new Vector3(2f, 9.2f, -132f),
            new Vector3(-56f, 10.4f, -96f),
            new Vector3(-28f, 11.8f, -40f),
            new Vector3(12f, 14.8f, 6f),
            new Vector3(42f, 18.0f, 42f),
            new Vector3(58f, 20.8f, 86f),
            new Vector3(28f, 23.6f, 126f),
            new Vector3(-20f, 27.4f, 160f),
            new Vector3(-70f, 32.8f, 198f),
            new Vector3(-88f, 38.0f, 244f),
            new Vector3(-36f, 43.2f, 292f),
            new Vector3(18f, 48.8f, 322f),
            new Vector3(62f, 54.0f, 342f)
        };
        List<Vector3> pts = new List<Vector3>();
        for (int i = 0; i < cp.Length - 1; i++)
        {
            Vector3 p0 = i == 0 ? cp[i] : cp[i - 1], p1 = cp[i], p2 = cp[i + 1], p3 = i + 2 >= cp.Length ? cp[i + 1] : cp[i + 2];
            for (int j = 0; j < 18; j++)
            {
                float t = j / 18f, t2 = t * t, t3 = t2 * t;
                Vector3 p = 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
                pts.Add(p);
            }
        }
        pts.Add(cp[cp.Length - 1]);
        List<float> dist = Distances(pts); float total = dist[dist.Count - 1];
        List<RoadSample> road = new List<RoadSample>(pts.Count);
        for (int i = 0; i < pts.Count; i++)
        {
            float t = total > 0.01f ? dist[i] / total : 0f;
            Vector3 p = pts[i];
            p.y += Humps(t, 0.06f, 0.22f, 2, 0.35f);
            p.y += Bell(t, 0.33f, 0.43f) * 0.8f;
            p.y += Bell(t, 0.63f, 0.77f) * 0.9f;
            p.y += RampPulse(t, 0.86f, 0.93f, 1.1f);
            Vector3 prev = i == 0 ? p - (pts[1] - pts[0]) : pts[i - 1];
            Vector3 next = i == pts.Count - 1 ? p + (pts[i] - pts[i - 1]) : pts[i + 1];
            Vector3 a = new Vector3(p.x - prev.x, 0f, p.z - prev.z).normalized, b = new Vector3(next.x - p.x, 0f, next.z - p.z).normalized;
            float bank = Mathf.Clamp(Vector3.SignedAngle(a, b, Vector3.up) * 0.46f, -7f, 7f);
            bank += Bell(t, 0.10f, 0.19f) * 2.4f;
            bank -= Bell(t, 0.25f, 0.34f) * 2.8f;
            bank += Bell(t, 0.50f, 0.60f) * 2.2f;
            bank += Bell(t, 0.72f, 0.84f) * 3.1f;
            float width = t < 0.35f ? 12.4f : (t < 0.68f ? 10.8f : 9.4f);
            float terrainBlend = t >= 0.47f && t <= 0.57f ? 0.12f : 1f;
            road.Add(new RoadSample(p, width, bank, terrainBlend));
        }
        return road;
    }

    void ApplyHeights(TerrainData data, List<RoadSample> road)
    {
        int res = data.heightmapResolution; float[,] h = new float[res, res];
        float hx = terrainSize.x * 0.5f, hz = terrainSize.z * 0.5f;
        for (int z = 0; z < res; z++)
        {
            float lz = Mathf.Lerp(-hz, hz, z / (float)(res - 1));
            for (int x = 0; x < res; x++)
            {
                float lx = Mathf.Lerp(-hx, hx, x / (float)(res - 1));
                float y = BaseTerrain(lx, lz);
                RoadSample s; float d = Nearest(lx, lz, road, out s), r = s.width * 0.5f + shoulderWidth;
                if (s.terrainBlend > 0f && d < r)
                {
                    float k = d <= s.width * 0.5f ? 1f : 1f - Mathf.InverseLerp(s.width * 0.5f, r, d);
                    k = k * k * (3f - 2f * k) * s.terrainBlend;
                    y = Mathf.Lerp(y, Mathf.Clamp01((s.position.y - roadThickness * 0.45f) / terrainSize.y), k);
                }
                h[z, x] = Mathf.Clamp01(y);
            }
        }
        data.SetHeights(0, 0, h);
    }

    void ApplyTextures(TerrainData data, List<RoadSample> road)
    {
        if (data.terrainLayers == null || data.terrainLayers.Length < 2) return;
        int w = data.alphamapWidth, h = data.alphamapHeight; float[,,] a = new float[h, w, 2];
        float hx = terrainSize.x * 0.5f, hz = terrainSize.z * 0.5f;
        for (int z = 0; z < h; z++)
        {
            float lz = Mathf.Lerp(-hz, hz, z / (float)(h - 1));
            for (int x = 0; x < w; x++)
            {
                float lx = Mathf.Lerp(-hx, hx, x / (float)(w - 1));
                RoadSample s; float d = Nearest(lx, lz, road, out s), r = s.width * 0.58f + shoulderWidth * 0.9f;
                float dirt = d < r ? (d <= s.width * 0.46f ? 1f : 1f - Mathf.InverseLerp(s.width * 0.46f, r, d)) : 0f;
                dirt = Mathf.Max(Mathf.SmoothStep(0f, 1f, dirt), Ravine(lx, lz) * 0.35f);
                a[z, x, 0] = 1f - dirt; a[z, x, 1] = dirt;
            }
        }
        data.SetAlphamaps(0, 0, a);
    }

    float BaseTerrain(float x, float z)
    {
        float broad = (Mathf.PerlinNoise(x * 0.008f + 22f, z * 0.008f + 35f) - 0.5f) * 0.05f;
        float detail = (Mathf.PerlinNoise(x * 0.020f + 120f, z * 0.020f + 78f) - 0.5f) * 0.015f;
        float southPlain = 0.055f + Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(-300f, -40f, z)) * 0.02f;
        float speedBasin = Ellipse(x, z, 6f, -238f, 170f, 210f, 0f) * 0.035f;
        float bridgeShelf = Ellipse(x, z, -14f, 28f, 132f, 130f, -10f) * 0.08f;
        float ridgeWest = Ellipse(x, z, -94f, 152f, 54f, 124f, -18f) * 0.12f;
        float summit = Ellipse(x, z, -34f, 256f, 108f, 118f, -12f) * 0.34f
            + Ellipse(x, z, 28f, 322f, 80f, 62f, 6f) * 0.22f
            + Ellipse(x, z, -72f, 222f, 46f, 52f, 0f) * 0.10f;
        float border = Mathf.SmoothStep(0f, 0.14f, Mathf.InverseLerp(0.66f, 1f, Mathf.Max(Mathf.Abs(x) / (terrainSize.x * 0.5f), Mathf.Abs(z) / (terrainSize.z * 0.5f))));
        return Mathf.Clamp01(southPlain + speedBasin + bridgeShelf + ridgeWest + summit + broad + detail + border - Ravine(x, z) * 0.22f);
    }

    float Ravine(float x, float z)
    {
        float mainCut = Ellipse(x, z, 38f, 64f, 34f, 96f, 18f);
        float spillCut = Ellipse(x, z, 6f, 102f, 20f, 50f, -22f);
        return Mathf.Clamp01(Mathf.Max(mainCut, spillCut));
    }

    float Nearest(float x, float z, List<RoadSample> road, out RoadSample sample)
    {
        sample = road[0]; float best = float.MaxValue; Vector2 p = new Vector2(x, z);
        for (int i = 0; i < road.Count; i++)
        {
            float d = Vector2.Distance(p, new Vector2(road[i].position.x, road[i].position.z));
            if (d < best) { best = d; sample = road[i]; }
        }
        return best;
    }

    void CreateRoad(List<RoadSample> road)
    {
        GameObject root = new GameObject("RoverRoad"); root.transform.SetParent(transform, false);
        for (int i = 0; i < road.Count - 1; i++)
        {
            RoadSample a = road[i], b = road[i + 1]; Vector3 seg = b.position - a.position; float len = seg.magnitude; if (len <= 0.01f) continue;
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = "Road_" + i.ToString("000"); go.transform.SetParent(root.transform, false);
            go.transform.localPosition = (a.position + b.position) * 0.5f + Vector3.up * (roadThickness * 0.5f);
            go.transform.localRotation = Quaternion.LookRotation(seg.normalized, Vector3.up) * Quaternion.AngleAxis((a.bank + b.bank) * 0.5f, Vector3.forward);
            go.transform.localScale = new Vector3(a.width, roadThickness, len + 0.8f); PaintRoad(go.GetComponent<Renderer>());
        }
    }

    void CreateBridgeSet()
    {
        GameObject root = new GameObject("BridgeSet"); root.transform.SetParent(transform, false);
        Box(root.transform, "RavineFloor", new Vector3(32f, 4.6f, 62f), Vector3.zero, new Vector3(58f, 0.2f, 124f), new Color(0.16f, 0.46f, 0.72f), false);
        Box(root.transform, "BridgeRailL", new Vector3(48f, 18.2f, 50f), new Vector3(0f, 28f, 0f), new Vector3(0.3f, 1.1f, 74f), new Color(0.46f, 0.36f, 0.24f), true);
        Box(root.transform, "BridgeRailR", new Vector3(39f, 18.2f, 55f), new Vector3(0f, 28f, 0f), new Vector3(0.3f, 1.1f, 74f), new Color(0.46f, 0.36f, 0.24f), true);
        Box(root.transform, "BridgeSupportA", new Vector3(43f, 9f, 30f), Vector3.zero, new Vector3(2.4f, 10.5f, 2.4f), new Color(0.4f, 0.33f, 0.25f), true);
        Box(root.transform, "BridgeSupportB", new Vector3(54f, 11f, 84f), Vector3.zero, new Vector3(2.8f, 14.5f, 2.8f), new Color(0.4f, 0.33f, 0.25f), true);
        Box(root.transform, "BridgeSupportC", new Vector3(16f, 8.8f, 92f), Vector3.zero, new Vector3(2.4f, 9.5f, 2.4f), new Color(0.4f, 0.33f, 0.25f), true);
        Vector3 left = new Vector3(-72f, 29f, 96f), right = new Vector3(24f, 28f, 116f); Vector3 dir = (right - left).normalized; float yaw = Quaternion.LookRotation(dir, Vector3.up).eulerAngles.y;
        Box(root.transform, "OverpassPostL", left + Vector3.up * 5f, Vector3.zero, new Vector3(2.6f, 10f, 2.6f), new Color(0.45f, 0.38f, 0.27f), true);
        Box(root.transform, "OverpassPostR", right + Vector3.up * 5f, Vector3.zero, new Vector3(2.6f, 10f, 2.6f), new Color(0.45f, 0.38f, 0.27f), true);
        for (int i = 0; i < 15; i++) { float t = i / 14f; Vector3 p = Vector3.Lerp(left, right, t); p.y -= Mathf.Sin(t * Mathf.PI) * 1.8f; Box(root.transform, "OverpassPlank_" + i.ToString("00"), p, new Vector3(0f, yaw, 0f), new Vector3(2.8f, 0.24f, 3.8f), new Color(0.54f, 0.42f, 0.28f), true); }
    }

    void CreateFinish(Vector3 pos)
    {
        GameObject root = new GameObject("FinishSummit"); root.transform.SetParent(transform, false);
        Primitive(root.transform, PrimitiveType.Cylinder, "FinishPad", pos + Vector3.up * 0.18f, Vector3.zero, new Vector3(4.2f, 0.18f, 4.2f), new Color(0.88f, 0.74f, 0.2f));
        Box(root.transform, "FinishLeft", pos + new Vector3(-3.3f, 2.6f, 0f), Vector3.zero, new Vector3(0.4f, 5.2f, 0.4f), new Color(0.3f, 0.3f, 0.3f), true);
        Box(root.transform, "FinishRight", pos + new Vector3(3.3f, 2.6f, 0f), Vector3.zero, new Vector3(0.4f, 5.2f, 0.4f), new Color(0.3f, 0.3f, 0.3f), true);
        Box(root.transform, "FinishBar", pos + new Vector3(0f, 4.9f, 0f), Vector3.zero, new Vector3(7.2f, 0.35f, 0.35f), new Color(0.84f, 0.2f, 0.18f), true);
    }

    void Primitive(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 rot, Vector3 scale) { Primitive(parent, type, name, pos, rot, scale, new Color(0.76f, 0.84f, 0.92f)); }
    void Primitive(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 rot, Vector3 scale, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = pos; go.transform.localRotation = Quaternion.Euler(rot); go.transform.localScale = scale; Paint(go.GetComponent<Renderer>(), color);
    }
    void Box(Transform parent, string name, Vector3 pos, Vector3 rot, Vector3 scale, Color color, bool collider) { GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = pos; go.transform.localRotation = Quaternion.Euler(rot); go.transform.localScale = scale; Paint(go.GetComponent<Renderer>(), color); if (!collider) DestroyImmediate(go.GetComponent<BoxCollider>()); }
    void PaintRoad(Renderer r) { if (roadMaterial != null) r.sharedMaterial = roadMaterial; else Paint(r, new Color(0.42f, 0.33f, 0.23f)); }
    void Paint(Renderer r, Color c) { Material m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.color = c; r.sharedMaterial = m; }

    static List<float> Distances(List<Vector3> pts) { List<float> d = new List<float>(pts.Count) { 0f }; float t = 0f; for (int i = 1; i < pts.Count; i++) { t += Vector3.Distance(pts[i - 1], pts[i]); d.Add(t); } return d; }
    static float Bell(float t, float a, float b) { if (t <= a || t >= b) return 0f; return Mathf.Sin(Mathf.InverseLerp(a, b, t) * Mathf.PI); }
    static float Humps(float t, float a, float b, int count, float amp) { if (t <= a || t >= b || count <= 0) return 0f; return Mathf.Abs(Mathf.Sin(Mathf.InverseLerp(a, b, t) * Mathf.PI * count)) * amp; }
    static float RampPulse(float t, float a, float b, float amp) { if (t <= a || t >= b) return 0f; float k = Mathf.InverseLerp(a, b, t); return (k < 0.5f ? k * 2f : (1f - k) * 2f) * amp; }
    static float Ellipse(float x, float z, float cx, float cz, float rx, float rz, float deg) { float r = deg * Mathf.Deg2Rad, s = Mathf.Sin(r), c = Mathf.Cos(r); float dx = x - cx, dz = z - cz; float px = dx * c - dz * s, pz = dx * s + dz * c; float n = (px * px) / (rx * rx) + (pz * pz) / (rz * rz); if (n >= 1f) return 0f; float e = 1f - n; return e * e * (3f - 2f * e); }

    void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Levels")) AssetDatabase.CreateFolder("Assets", "Levels");
        if (!AssetDatabase.IsValidFolder("Assets/Levels/Controllers Trial")) AssetDatabase.CreateFolder("Assets/Levels", "Controllers Trial");
        if (!AssetDatabase.IsValidFolder(GeneratedFolder)) AssetDatabase.CreateFolder("Assets/Levels/Controllers Trial", "Generated");
    }

    static string Clean(string text) { foreach (char c in System.IO.Path.GetInvalidFileNameChars()) text = text.Replace(c, '_'); return text.Replace(' ', '_'); }
#endif
}
