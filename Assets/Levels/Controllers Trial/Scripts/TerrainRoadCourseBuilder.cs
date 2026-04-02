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
        List<List<RoadSample>> roads = BuildRoadNetwork();
        List<RoadSample> road = FlattenRoads(roads);
        TerrainData data = GetTerrainData();
        ApplyHeights(data, road);
        ApplyTextures(data, road);
        CreateOcean();
        CreateTerrain(data);
        CreateRoad(roads);
        CreateFinish(roads[0][roads[0].Count - 1].position);
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

    List<List<RoadSample>> BuildRoadNetwork()
    {
        return new List<List<RoadSample>> { BuildMainRoad() };
    }

    List<RoadSample> BuildMainRoad()
    {
        Vector3[] cp =
        {
            new Vector3(-188f, 5.2f, -420f),
            new Vector3(-154f, 5.8f, -348f),
            new Vector3(-102f, 6.5f, -304f),
            new Vector3(-30f, 7.1f, -286f),
            new Vector3(54f, 7.8f, -264f),
            new Vector3(122f, 8.8f, -214f),
            new Vector3(164f, 10.0f, -140f),
            new Vector3(174f, 11.4f, -48f),
            new Vector3(146f, 12.8f, 36f),
            new Vector3(92f, 14.2f, 106f),
            new Vector3(16f, 15.6f, 154f),
            new Vector3(-70f, 16.4f, 172f),
            new Vector3(-140f, 17.4f, 132f),
            new Vector3(-182f, 18.6f, 56f),
            new Vector3(-170f, 19.8f, -30f),
            new Vector3(-112f, 21.2f, -104f),
            new Vector3(-28f, 23.4f, -138f),
            new Vector3(44f, 26.6f, -112f),
            new Vector3(74f, 31.0f, -46f),
            new Vector3(52f, 37.2f, 28f),
            new Vector3(18f, 45.2f, 92f),
            new Vector3(2f, 53.8f, 152f),
            new Vector3(-4f, 63.5f, 210f)
        };
        List<Vector3> pts = SamplePath(cp, 18);
        List<float> dist = Distances(pts); float total = dist[dist.Count - 1];
        List<RoadSample> road = new List<RoadSample>(pts.Count);
        for (int i = 0; i < pts.Count; i++)
        {
            float t = total > 0.01f ? dist[i] / total : 0f;
            Vector3 p = pts[i];
            p.y += Humps(t, 0.04f, 0.16f, 2, 0.24f);
            p.y += Bell(t, 0.24f, 0.38f) * 0.45f;
            p.y += Bell(t, 0.54f, 0.68f) * 0.55f;
            p.y += Bell(t, 0.78f, 0.90f) * 0.75f;
            Vector3 prev = i == 0 ? p - (pts[1] - pts[0]) : pts[i - 1];
            Vector3 next = i == pts.Count - 1 ? p + (pts[i] - pts[i - 1]) : pts[i + 1];
            Vector3 a = new Vector3(p.x - prev.x, 0f, p.z - prev.z).normalized, b = new Vector3(next.x - p.x, 0f, next.z - p.z).normalized;
            float bank = Mathf.Clamp(Vector3.SignedAngle(a, b, Vector3.up) * 0.36f, -5.5f, 5.5f);
            bank += Bell(t, 0.08f, 0.16f) * 1.6f;
            bank -= Bell(t, 0.28f, 0.36f) * 1.8f;
            bank += Bell(t, 0.46f, 0.58f) * 1.7f;
            bank -= Bell(t, 0.62f, 0.72f) * 1.2f;
            bank += Bell(t, 0.82f, 0.92f) * 2.2f;
            float width = t < 0.32f ? 11.8f : (t < 0.74f ? 10.4f : 9.6f);
            float terrainBlend = 1f;
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
        float island =
            Ellipse(x, z, -120f, -170f, 126f, 172f, -18f) * 0.18f +
            Ellipse(x, z, 24f, -172f, 162f, 168f, 8f) * 0.22f +
            Ellipse(x, z, 126f, -42f, 128f, 178f, 14f) * 0.21f +
            Ellipse(x, z, -104f, 40f, 146f, 176f, -12f) * 0.18f +
            Ellipse(x, z, 16f, 78f, 166f, 154f, 4f) * 0.20f +
            Ellipse(x, z, -8f, 184f, 120f, 112f, -6f) * 0.14f;
        float coast = Mathf.Clamp01(island);

        float broad = (Mathf.PerlinNoise(x * 0.0065f + 22f, z * 0.0065f + 35f) - 0.5f) * 0.022f;
        float detail = (Mathf.PerlinNoise(x * 0.018f + 120f, z * 0.018f + 78f) - 0.5f) * 0.010f;

        float beachShelf = coast * 0.032f;
        float interiorLift = Mathf.Pow(coast, 1.35f) * 0.065f;
        float centralPlateau = Ellipse(x, z, -8f, 18f, 136f, 126f, 0f) * 0.040f;
        float westCliffs = Ellipse(x, z, -168f, -36f, 68f, 214f, -12f) * 0.055f;
        float eastCliffs = Ellipse(x, z, 170f, -24f, 74f, 224f, 8f) * 0.060f;
        float southLagoonRim = Ellipse(x, z, 12f, -204f, 100f, 72f, 0f) * 0.028f;
        float volcanoBase =
            Ellipse(x, z, -4f, 182f, 88f, 84f, 0f) * 0.22f +
            Ellipse(x, z, -6f, 220f, 54f, 48f, 0f) * 0.18f;
        float volcanoCone = Ellipse(x, z, -6f, 224f, 28f, 24f, 0f) * 0.16f;
        float crater = Ellipse(x, z, -4f, 230f, 12f, 10f, 0f) * 0.10f;

        float coves =
            Ellipse(x, z, 122f, 154f, 48f, 42f, 0f) * 0.08f +
            Ellipse(x, z, -160f, -258f, 48f, 52f, 0f) * 0.10f +
            Ellipse(x, z, 182f, 84f, 42f, 52f, 0f) * 0.08f +
            Ellipse(x, z, -188f, 128f, 46f, 54f, 0f) * 0.08f +
            Ellipse(x, z, 70f, -278f, 52f, 38f, 0f) * 0.08f;
        float innerLagoon = Ellipse(x, z, 18f, -198f, 48f, 28f, 0f) * 0.07f;

        float outsideDrop = Mathf.Pow(1f - coast, 1.8f) * 0.02f;
        float height = beachShelf + interiorLift + centralPlateau + westCliffs + eastCliffs + southLagoonRim + volcanoBase + volcanoCone + broad + detail - crater - coves - innerLagoon - outsideDrop;
        return Mathf.Clamp01(height);
    }

    float Ravine(float x, float z)
    {
        return 0f;
    }

    void CreateOcean()
    {
        GameObject root = new GameObject("Water");
        root.transform.SetParent(transform, false);
        Box(root.transform, "Ocean", new Vector3(0f, 2.1f, 0f), Vector3.zero, new Vector3(terrainSize.x + 220f, 4.2f, terrainSize.z + 220f), new Color(0.16f, 0.48f, 0.76f), false);
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

    void CreateRoad(List<List<RoadSample>> roads)
    {
        GameObject root = new GameObject("RoverRoad"); root.transform.SetParent(transform, false);
        int roadIndex = 0;
        for (int pathIndex = 0; pathIndex < roads.Count; pathIndex++)
        {
            List<RoadSample> road = roads[pathIndex];
            for (int i = 0; i < road.Count - 1; i++)
            {
                RoadSample a = road[i], b = road[i + 1]; Vector3 seg = b.position - a.position; float len = seg.magnitude; if (len <= 0.01f) continue;
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = "Road_" + roadIndex.ToString("000"); go.transform.SetParent(root.transform, false);
                go.transform.localPosition = (a.position + b.position) * 0.5f + Vector3.up * (roadThickness * 0.5f);
                go.transform.localRotation = Quaternion.LookRotation(seg.normalized, Vector3.up) * Quaternion.AngleAxis((a.bank + b.bank) * 0.5f, Vector3.forward);
                go.transform.localScale = new Vector3(a.width, roadThickness, len + 0.8f); PaintRoad(go.GetComponent<Renderer>());
                roadIndex++;
            }
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

    static List<RoadSample> FlattenRoads(List<List<RoadSample>> roads)
    {
        List<RoadSample> merged = new List<RoadSample>();
        for (int i = 0; i < roads.Count; i++) merged.AddRange(roads[i]);
        return merged;
    }

    static List<Vector3> SamplePath(Vector3[] cp, int samplesPerSegment)
    {
        List<Vector3> pts = new List<Vector3>();
        for (int i = 0; i < cp.Length - 1; i++)
        {
            Vector3 p0 = i == 0 ? cp[i] : cp[i - 1], p1 = cp[i], p2 = cp[i + 1], p3 = i + 2 >= cp.Length ? cp[i + 1] : cp[i + 2];
            for (int j = 0; j < samplesPerSegment; j++)
            {
                float t = j / (float)samplesPerSegment, t2 = t * t, t3 = t2 * t;
                Vector3 p = 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
                pts.Add(p);
            }
        }
        pts.Add(cp[cp.Length - 1]);
        return pts;
    }

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
