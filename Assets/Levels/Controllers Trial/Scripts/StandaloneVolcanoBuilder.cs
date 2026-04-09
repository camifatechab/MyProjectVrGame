using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
public class StandaloneVolcanoBuilder : MonoBehaviour
{
    [System.Serializable]
    struct RouteSample
    {
        public Vector3 position;
        public float width;

        public RouteSample(Vector3 position, float width)
        {
            this.position = position;
            this.width = width;
        }
    }

    public Vector3 terrainSize = new Vector3(260f, 70f, 260f);
    public int heightmapResolution = 513;
    public int alphamapResolution = 512;
    public float volcanoRadius = 118f;
    public float craterRadius = 18f;
    public float rimRadius = 36f;
    public float peakHeight = 56f;
    public float baseHeight = 5.5f;
    public float pathTurns = 2.15f;
    public float pathWidth = 14f;
    public float pathShoulder = 9f;
    public TerrainLayer baseLayer;
    public TerrainLayer hotLayer;

#if UNITY_EDITOR
    const string GeneratedFolder = "Assets/Levels/Controllers Trial/Generated";
    const string TerrainDataPath = GeneratedFolder + "/StandaloneVolcano_TerrainData.asset";
    const string BaseLayerPath = "Assets/Handpainted_Grass_and_Ground_Textures/Demo/terrain_layers/dirt_desatured_rocks_up.terrainlayer";
    const string HotLayerPath = "Assets/Handpainted_Grass_and_Ground_Textures/Demo/terrain_layers/dirt_corrupted_lighted_up.terrainlayer";
    const string LavaMaterialPath = GeneratedFolder + "/StandaloneVolcano_Lava.mat";

    [MenuItem("Tools/Codex/Create Standalone Volcano Module")]
    static void CreateStandaloneVolcanoModule()
    {
        GameObject root = GameObject.Find("VolcanoModule");
        if (root == null)
        {
            root = new GameObject("VolcanoModule");
            root.transform.position = new Vector3(380f, 0f, -120f);
        }

        StandaloneVolcanoBuilder builder = root.GetComponent<StandaloneVolcanoBuilder>();
        if (builder == null)
        {
            builder = root.AddComponent<StandaloneVolcanoBuilder>();
        }

        builder.Build();
        Selection.activeGameObject = root;

        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.FrameSelected();
        }
    }

    [MenuItem("Tools/Codex/Rebuild Selected Volcano Module")]
    static void RebuildSelectedVolcanoModule()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogWarning("Select a VolcanoModule root or one of its children first.");
            return;
        }

        StandaloneVolcanoBuilder builder = selected.GetComponentInParent<StandaloneVolcanoBuilder>();
        if (builder == null)
        {
            Debug.LogWarning("No StandaloneVolcanoBuilder found in the selected hierarchy.");
            return;
        }

        builder.Build();
        Selection.activeGameObject = builder.gameObject;
    }

    [ContextMenu("Build Standalone Volcano")]
    public void Build()
    {
        EnsureFolders();
        EnsureLayers();
        ClearChildren();

        List<RouteSample> route = BuildRoute();
        TerrainData terrainData = GetTerrainData();
        ApplyHeights(terrainData, route);
        ApplyTextures(terrainData, route);
        CreateTerrainSurface(terrainData);
        CreateRouteMarkers(route);
        CreateCraterLava();

        EditorUtility.SetDirty(this);
        if (gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Standalone volcano terrain generated.");
    }

    void EnsureFolders()
    {
        string[] folders = GeneratedFolder.Split('/');
        string current = folders[0];
        for (int i = 1; i < folders.Length; i++)
        {
            string next = current + "/" + folders[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, folders[i]);
            }

            current = next;
        }
    }

    void EnsureLayers()
    {
        if (baseLayer == null)
        {
            baseLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(BaseLayerPath);
        }

        if (hotLayer == null)
        {
            hotLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(HotLayerPath);
        }
    }

    void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

    TerrainData GetTerrainData()
    {
        TerrainData terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
        if (terrainData == null)
        {
            terrainData = new TerrainData();
            AssetDatabase.CreateAsset(terrainData, TerrainDataPath);
        }

        terrainData.heightmapResolution = Mathf.ClosestPowerOfTwo(Mathf.Clamp(heightmapResolution - 1, 32, 4096)) + 1;
        terrainData.alphamapResolution = Mathf.Clamp(Mathf.ClosestPowerOfTwo(Mathf.Max(16, alphamapResolution)), 16, 2048);
        terrainData.baseMapResolution = 512;
        terrainData.size = terrainSize;

        if (baseLayer != null && hotLayer != null)
        {
            terrainData.terrainLayers = new[] { baseLayer, hotLayer };
        }

        EditorUtility.SetDirty(terrainData);
        return terrainData;
    }

    List<RouteSample> BuildRoute()
    {
        List<RouteSample> route = new List<RouteSample>();
        int sampleCount = 160;
        float startAngle = -150f * Mathf.Deg2Rad;
        float endAngle = startAngle + pathTurns * Mathf.PI * 2f;
        float startRadius = volcanoRadius * 0.94f;
        float endRadius = rimRadius + 4f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (sampleCount - 1f);
            float angle = Mathf.Lerp(startAngle, endAngle, t);
            float radius = Mathf.Lerp(startRadius, endRadius, Mathf.Pow(t, 0.92f));
            float y = Mathf.Lerp(baseHeight + 2f, peakHeight - 1.5f, Mathf.SmoothStep(0f, 1f, t));
            float width = Mathf.Lerp(pathWidth + 4f, pathWidth - 1.5f, t);

            if (t > 0.86f)
            {
                float k = Mathf.InverseLerp(0.86f, 1f, t);
                radius = Mathf.Lerp(radius, rimRadius + 6f, k);
                y = Mathf.Lerp(y, peakHeight - 2.5f, k);
                width = Mathf.Lerp(width, pathWidth + 1f, k);
            }

            Vector3 position = new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
            route.Add(new RouteSample(position, width));
        }

        return route;
    }

    void ApplyHeights(TerrainData terrainData, List<RouteSample> route)
    {
        int resolution = terrainData.heightmapResolution;
        float[,] heights = new float[resolution, resolution];
        float halfWidth = terrainSize.x * 0.5f;
        float halfLength = terrainSize.z * 0.5f;

        for (int z = 0; z < resolution; z++)
        {
            float localZ = Mathf.Lerp(-halfLength, halfLength, z / (float)(resolution - 1));
            for (int x = 0; x < resolution; x++)
            {
                float localX = Mathf.Lerp(-halfWidth, halfWidth, x / (float)(resolution - 1));
                float height = EvaluateBaseHeight(localX, localZ);

                RouteSample sample;
                float distanceToRoute = NearestRouteDistance(localX, localZ, route, out sample);
                float blendRadius = sample.width * 0.5f + pathShoulder;
                if (distanceToRoute < blendRadius)
                {
                    float targetHeight = Mathf.Clamp01(sample.position.y / terrainSize.y);
                    float blend = distanceToRoute <= sample.width * 0.5f
                        ? 1f
                        : 1f - Mathf.InverseLerp(sample.width * 0.5f, blendRadius, distanceToRoute);
                    blend = blend * blend * (3f - 2f * blend);
                    height = Mathf.Lerp(height, targetHeight, blend);
                }

                Vector2 point = new Vector2(localX, localZ);
                float startPad = 1f - Mathf.Clamp01(Vector2.Distance(point, new Vector2(route[0].position.x, route[0].position.z)) / 20f);
                if (startPad > 0f)
                {
                    float padBlend = startPad * startPad * (3f - 2f * startPad);
                    float padHeight = Mathf.Clamp01((baseHeight + 1.25f) / terrainSize.y);
                    height = Mathf.Lerp(height, padHeight, padBlend);
                }

                heights[z, x] = Mathf.Clamp01(height);
            }
        }

        terrainData.SetHeights(0, 0, heights);
    }

    void ApplyTextures(TerrainData terrainData, List<RouteSample> route)
    {
        if (terrainData.terrainLayers == null || terrainData.terrainLayers.Length < 2)
        {
            return;
        }

        int width = terrainData.alphamapWidth;
        int height = terrainData.alphamapHeight;
        float[,,] alphamaps = new float[height, width, 2];
        float halfWidth = terrainSize.x * 0.5f;
        float halfLength = terrainSize.z * 0.5f;

        for (int z = 0; z < height; z++)
        {
            float localZ = Mathf.Lerp(-halfLength, halfLength, z / (float)(height - 1));
            for (int x = 0; x < width; x++)
            {
                float localX = Mathf.Lerp(-halfWidth, halfWidth, x / (float)(width - 1));
                Vector2 point = new Vector2(localX, localZ);
                float radius = point.magnitude;

                RouteSample sample;
                float distanceToRoute = NearestRouteDistance(localX, localZ, route, out sample);
                float routeBlend = distanceToRoute < sample.width * 0.6f
                    ? 1f - Mathf.InverseLerp(0f, sample.width * 0.6f, distanceToRoute)
                    : 0f;
                float craterHeat = radius < rimRadius + 8f
                    ? 1f - Mathf.InverseLerp(craterRadius * 0.4f, rimRadius + 8f, radius)
                    : 0f;
                float hotMask = Mathf.Clamp01(craterHeat * 0.85f + routeBlend * 0.2f);

                alphamaps[z, x, 0] = 1f - hotMask;
                alphamaps[z, x, 1] = hotMask;
            }
        }

        terrainData.SetAlphamaps(0, 0, alphamaps);
    }

    void CreateTerrainSurface(TerrainData terrainData)
    {
        GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
        terrainObject.name = "VolcanoTerrain";
        terrainObject.transform.SetParent(transform, false);
        terrainObject.transform.localPosition = new Vector3(-terrainSize.x * 0.5f, 0f, -terrainSize.z * 0.5f);

        Terrain terrain = terrainObject.GetComponent<Terrain>();
        terrain.drawInstanced = true;
        terrain.heightmapPixelError = 3f;
        terrain.basemapDistance = 1000f;
        terrain.materialTemplate = null;
        terrain.Flush();
    }

    void CreateRouteMarkers(List<RouteSample> route)
    {
        CreateMarker("RideStart", route[0].position, new Color(0.18f, 0.68f, 0.93f));
        CreateMarker("MidOverlook", route[route.Count / 2].position + Vector3.up * 0.4f, new Color(1f, 0.72f, 0.22f));
        CreateMarker("CraterRim", route[route.Count - 1].position + Vector3.up * 0.5f, new Color(1f, 0.38f, 0.18f));
    }

    void CreateMarker(string markerName, Vector3 localPosition, Color color)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = markerName;
        marker.transform.SetParent(transform, false);
        marker.transform.localPosition = localPosition;
        marker.transform.localScale = new Vector3(4.5f, 0.35f, 4.5f);

        Collider markerCollider = marker.GetComponent<Collider>();
        if (markerCollider != null)
        {
            DestroyImmediate(markerCollider);
        }

        Renderer renderer = marker.GetComponent<Renderer>();
        if (renderer != null && renderer.sharedMaterial != null)
        {
            Material markerMaterial = new Material(renderer.sharedMaterial);
            if (markerMaterial.HasProperty("_BaseColor"))
            {
                markerMaterial.SetColor("_BaseColor", color);
            }

            if (markerMaterial.HasProperty("_Color"))
            {
                markerMaterial.color = color;
            }

            renderer.sharedMaterial = markerMaterial;
        }
    }

    void CreateCraterLava()
    {
        GameObject lava = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lava.name = "CraterLava";
        lava.transform.SetParent(transform, false);
        lava.transform.localPosition = new Vector3(0f, peakHeight - 12.5f, 0f);
        lava.transform.localScale = new Vector3(craterRadius * 1.25f, 0.75f, craterRadius * 1.25f);

        Collider lavaCollider = lava.GetComponent<Collider>();
        if (lavaCollider != null)
        {
            DestroyImmediate(lavaCollider);
        }

        Renderer renderer = lava.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = GetOrCreateLavaMaterial();
        }
    }

    Material GetOrCreateLavaMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(LavaMaterialPath);
        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        material = new Material(shader)
        {
            name = "StandaloneVolcano_Lava"
        };

        Color baseColor = new Color(1f, 0.36f, 0.08f, 1f);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", baseColor);
        }

        if (material.HasProperty("_Color"))
        {
            material.color = baseColor;
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", new Color(1.6f, 0.45f, 0.08f));
            material.EnableKeyword("_EMISSION");
        }

        AssetDatabase.CreateAsset(material, LavaMaterialPath);
        return material;
    }

    float EvaluateBaseHeight(float localX, float localZ)
    {
        Vector2 point = new Vector2(localX, localZ);
        float radius = point.magnitude;
        float normalizedRadius = Mathf.Clamp01(radius / volcanoRadius);
        float cone = Mathf.Pow(1f - normalizedRadius, 1.45f) * (peakHeight / terrainSize.y);
        float skirt = Mathf.Pow(1f - Mathf.Clamp01(radius / (volcanoRadius + 28f)), 0.78f) * (baseHeight / terrainSize.y);

        float angle = Mathf.Atan2(localZ, localX);
        float ridges = Mathf.Sin(angle * 3.2f + radius * 0.065f) * 0.018f * Mathf.Pow(1f - normalizedRadius, 0.55f);
        float broadNoise = (Mathf.PerlinNoise(localX * 0.025f + 11f, localZ * 0.025f + 17f) - 0.5f) * 0.04f;
        float detailNoise = (Mathf.PerlinNoise(localX * 0.065f + 42f, localZ * 0.065f + 9f) - 0.5f) * 0.012f;

        float rimBoost = Gaussian(radius, rimRadius, 11f) * 0.08f;
        float craterDip = radius < craterRadius
            ? Mathf.Pow(1f - radius / craterRadius, 1.8f) * 0.42f
            : 0f;

        float outerFade = radius > volcanoRadius
            ? Mathf.Pow(Mathf.InverseLerp(volcanoRadius + 34f, volcanoRadius, radius), 2f) * 0.025f
            : 0f;

        float height = skirt + cone + rimBoost + ridges + broadNoise + detailNoise - craterDip + outerFade;
        return Mathf.Clamp01(height);
    }

    float NearestRouteDistance(float x, float z, List<RouteSample> route, out RouteSample nearest)
    {
        Vector2 point = new Vector2(x, z);
        nearest = route[0];
        float bestDistance = float.MaxValue;

        for (int i = 0; i < route.Count; i++)
        {
            float distance = Vector2.Distance(point, new Vector2(route[i].position.x, route[i].position.z));
            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = route[i];
            }
        }

        return bestDistance;
    }

    float Gaussian(float value, float center, float width)
    {
        float scaled = (value - center) / Mathf.Max(0.0001f, width);
        return Mathf.Exp(-(scaled * scaled));
    }
#endif
}
