using UnityEditor;
using UnityEngine;

public static class CreateSwampRoadGuide
{
    private const string OutputFolder = "Assets/Generated/RoadGuide";
    private const string MaterialPath = OutputFolder + "/SwampRoadGuide.mat";
    private const float SegmentWidth = 12f;
    private const float SegmentThickness = 0.8f;
    private const float HeightOffset = 4f;

    private static readonly Vector3[] LeftToHub =
    {
        new(8f, 0f, 165f),
        new(22f, 0f, 150f),
        new(44f, 0f, 145f),
        new(68f, 0f, 149f),
        new(87f, 0f, 145f),
        new(105f, 0f, 143f),
    };

    private static readonly Vector3[] HubToTop =
    {
        new(105f, 0f, 143f),
        new(110f, 0f, 120f),
        new(119f, 0f, 95f),
        new(132f, 0f, 70f),
        new(149f, 0f, 42f),
        new(163f, 0f, 30f),
        new(174f, 0f, 42f),
        new(166f, 0f, 66f),
        new(149f, 0f, 89f),
        new(134f, 0f, 114f),
        new(122f, 0f, 132f),
    };

    private static readonly Vector3[] HubToRightToSouth =
    {
        new(105f, 0f, 143f),
        new(132f, 0f, 145f),
        new(160f, 0f, 145f),
        new(188f, 0f, 150f),
        new(212f, 0f, 164f),
        new(206f, 0f, 188f),
        new(190f, 0f, 210f),
        new(172f, 0f, 224f),
        new(150f, 0f, 232f),
        new(132f, 0f, 240f),
        new(118f, 0f, 255f),
        new(114f, 0f, 276f),
        new(107f, 0f, 295f),
    };

    [MenuItem("Tools/Terrain/Create Swamp Road Guide")]
    public static void CreateGuide()
    {
        GameObject terrainAnchor = GameObject.Find("Swamp/Terrains");
        Terrain terrain = Object.FindFirstObjectByType<Terrain>();

        if (terrainAnchor == null)
        {
            Debug.LogError("Create Swamp Road Guide: could not find 'Swamp/Terrains'.");
            return;
        }

        if (terrain == null || terrain.terrainData == null)
        {
            Debug.LogError("Create Swamp Road Guide: could not find a valid Terrain.");
            return;
        }

        EnsureFolder(OutputFolder);
        Material guideMaterial = LoadOrCreateMaterial();

        GameObject existing = GameObject.Find("RoadGuide");
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }

        GameObject root = new("RoadGuide");
        Undo.RegisterCreatedObjectUndo(root, "Create Swamp Road Guide");

        CreateRoute(root.transform, terrainAnchor.transform, terrain, guideMaterial, "LeftToHub", LeftToHub);
        CreateRoute(root.transform, terrainAnchor.transform, terrain, guideMaterial, "HubToTop", HubToTop);
        CreateRoute(root.transform, terrainAnchor.transform, terrain, guideMaterial, "HubToRightToSouth", HubToRightToSouth);

        Selection.activeGameObject = root;
        Debug.Log("Create Swamp Road Guide: generated a smoother rover route guide for the swamp.");
    }

    private static void CreateRoute(Transform parent, Transform swampRoot, Terrain terrain, Material material, string routeName, Vector3[] localPoints)
    {
        GameObject routeRoot = new(routeName);
        routeRoot.transform.SetParent(parent, false);

        for (int i = 0; i < localPoints.Length - 1; i++)
        {
            Vector3 start = ToWorld(localPoints[i], swampRoot, terrain);
            Vector3 end = ToWorld(localPoints[i + 1], swampRoot, terrain);
            CreateSegment(routeRoot.transform, material, routeName, i, start, end);
        }
    }

    private static Vector3 ToWorld(Vector3 localPoint, Transform swampRoot, Terrain terrain)
    {
        Vector3 world = swampRoot.TransformPoint(localPoint);
        float y = terrain.SampleHeight(world) + terrain.transform.position.y + HeightOffset;
        return new Vector3(world.x, y, world.z);
    }

    private static void CreateSegment(Transform parent, Material material, string routeName, int index, Vector3 start, Vector3 end)
    {
        Vector3 delta = end - start;
        float length = delta.magnitude;
        if (length <= 0.01f)
        {
            return;
        }

        GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        segment.name = $"{routeName}_Segment_{index:00}";
        segment.transform.SetParent(parent, false);
        segment.transform.position = start + delta * 0.5f;
        segment.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
        segment.transform.localScale = new Vector3(SegmentWidth, SegmentThickness, length);

        Renderer renderer = segment.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        Collider collider = segment.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }
    }

    private static Material LoadOrCreateMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        Color color = new(1f, 0.18f, 0.12f, 1f);
        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.4f);
        }

        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
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
}
