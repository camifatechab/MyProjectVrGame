using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RoadBaseMaterialApplier
{
    private const string MaterialPath = "Assets/Levels/Controllers Trial/Materials/RoverRoad_BaseBrown.mat";
    private const string EdgeMaterialPath = "Assets/Levels/Controllers Trial/Materials/RoverRoad_EdgeBrown.mat";
    private const string GuidePhysicsMaterialPath = "Assets/Levels/Controllers Trial/Materials/RoverRoad_Guide.physicMaterial";
    private const string BaseRootName = "RoverRoad_Base";
    private const string EdgeRootName = "RoverRoad_Edges";
    private const string ShelfRootName = "RoverRoad_CatchShelves";
    private const string GuideRootName = "RoverRoad_GuideRails";
    private const int BranchRoadStartIndex = 324;
    private const int BranchRoadEndIndex = 435;
    private const float EdgeGuideTiltDegrees = 17f;
    private const float EdgeGuideWidth = 1.24f;
    private const float EdgeGuideHeight = 0.62f;
    private const float EdgeGuideLengthScale = 1.08f;
    private const float EdgeGuideInnerOverlap = 0.34f;
    private const float CatchShelfWidth = 3.4f;
    private const float CatchShelfHeight = 0.42f;
    private const float CatchShelfLengthScale = 1.08f;
    private const float CatchShelfInnerOverlap = 0.54f;
    private const float CatchShelfDrop = 0.34f;
    private static readonly (int start, int end)[] MarkedCurveRanges =
    {
        (211, 236),
        (300, 320),
    };
    private static readonly Vector3 Zone03BridgePosition = new(1371.1950f, 4.6114f, 264.2616f);
    private static readonly Vector3 Zone03BridgeEuler = new(-13.3779f, 54.2311f, 0f);
    private static readonly Vector3 Zone03BridgeScale = new(1f, 1f, 0.9412f);
    private static readonly string[] ClosedCurveRoadNames =
    {
        "Road_137",
        "Road_138",
        "Road_139",
        "Road_140",
        "Road_141",
        "Road_142",
        "Road_143",
        "Road_144",
        "Road_145",
        "Road_146",
        "Road_147",
        "Road_148",
        "Road_149",
        "Road_150",
        "Road_151",
        "Road_152",
        "Road_153",
        "Road_154",
        "Road_155",
    };
    private static readonly RoadCurveState[] DownhillComfortConnectorStates =
    {
        new("Road_137", new Vector3(25.8153f, 15.7025f, 118.8988f), new Vector3(12.6233f, 285.1472f, 0f), new Vector3(10.2f, 0.42f, 2.5000f)),
        new("Road_138", new Vector3(23.9780f, 15.2735f, 119.3282f), new Vector3(13.1270f, 279.2887f, 0f), new Vector3(10.2f, 0.42f, 2.5000f)),
        new("Road_139", new Vector3(22.1069f, 14.8262f, 119.5054f), new Vector3(13.5974f, 268.2093f, 0f), new Vector3(10.2f, 0.42f, 2.5000f)),
        new("Road_140", new Vector3(20.2514f, 14.3717f, 119.2117f), new Vector3(13.4986f, 256.7490f, 0f), new Vector3(10.2f, 0.42f, 2.5000f)),
        new("Road_141", new Vector3(18.4561f, 13.9258f, 118.6456f), new Vector3(13.3600f, 249.6563f, 0f), new Vector3(10.2f, 0.42f, 2.5000f)),
        new("Road_142", new Vector3(16.7255f, 13.4786f, 117.9044f), new Vector3(13.5858f, 244.6124f, 0f), new Vector3(10.2f, 0.42f, 2.5000f)),
        new("Road_143", new Vector3(15.0598f, 13.0173f, 117.0339f), new Vector3(14.1147f, 239.7606f, 0f), new Vector3(10.2f, 0.42f, 2.5000f)),
        new("Road_144", new Vector3(13.4865f, 12.5358f, 116.0162f), new Vector3(14.5096f, 234.3741f, 0f), new Vector3(10.2f, 0.42f, 2.5000f)),
        new("Road_145", new Vector3(12.0179f, 12.0488f, 114.8540f), new Vector3(14.5063f, 229.7927f, 0f), new Vector3(10.2f, 0.42f, 2.5000f)),
        new("Road_146", new Vector3(10.6262f, 11.5668f, 113.5985f), new Vector3(14.2250f, 226.7908f, 0f), new Vector3(10.2f, 0.42f, 2.5000f)),
        new("Road_147", new Vector3(9.2836f, 11.0978f, 112.2855f), new Vector3(13.7500f, 224.5638f, 0f), new Vector3(10.2f, 0.42f, 2.5000f)),
        new("Road_148", new Vector3(7.9884f, 10.6469f, 110.9201f), new Vector3(13.2480f, 221.8380f, 0f), new Vector3(10.2f, 0.42f, 2.5000f)),
        new("Road_149", new Vector3(6.7715f, 10.2112f, 109.4796f), new Vector3(12.7595f, 219.0528f, 0f), new Vector3(10.2f, 0.42f, 2.5000f)),
        new("Road_150", new Vector3(5.6102f, 9.7922f, 107.9889f), new Vector3(12.2099f, 217.3118f, 0f), new Vector3(10.2f, 0.42f, 2.5000f)),
        new("Road_151", new Vector3(4.4783f, 9.3925f, 106.4706f), new Vector3(11.6418f, 216.2184f, 0f), new Vector3(10.2f, 0.42f, 2.5000f)),
        new("Road_152", new Vector3(3.3701f, 9.0110f, 104.9302f), new Vector3(10.9654f, 214.7399f, 0f), new Vector3(10.2f, 0.42f, 2.5000f)),
        new("Road_153", new Vector3(2.3131f, 8.6563f, 103.3482f), new Vector3(9.8029f, 212.2182f, 0f), new Vector3(10.2f, 0.42f, 2.5000f)),
        new("Road_154", new Vector3(1.3376f, 8.3523f, 101.7049f), new Vector3(7.8937f, 208.4723f, 0f), new Vector3(10.2f, 0.42f, 2.5000f)),
        new("Road_155", new Vector3(0.4871f, 8.1253f, 99.9814f), new Vector3(6.2100f, 205.2451f, 0f), new Vector3(10.2f, 0.42f, 2.5000f)),
    };
    private static readonly RoadCurveState[] BridgeComfortCurveStates =
    {
        new("Road_131", new Vector3(34.0790f, 17.8111f, 106.8410f), new Vector3(4.9839f, 340.8802f, 0f), new Vector3(10.2f, 0.42f, 2.8470f)),
        new("Road_132", new Vector3(33.1633f, 17.5680f, 109.3542f), new Vector3(5.4874f, 338.7151f, 0f), new Vector3(10.2f, 0.42f, 2.8467f)),
        new("Road_133", new Vector3(32.1386f, 17.2976f, 111.8218f), new Vector3(6.3904f, 334.7347f, 0f), new Vector3(10.2f, 0.42f, 2.8443f)),
        new("Road_134", new Vector3(30.8887f, 16.9711f, 114.1737f), new Vector3(7.8197f, 328.2810f, 0f), new Vector3(10.2f, 0.42f, 2.8462f)),
        new("Road_135", new Vector3(29.3485f, 16.5688f, 116.3360f), new Vector3(9.1417f, 321.9084f, 0f), new Vector3(10.2f, 0.42f, 2.8461f)),
        new("Road_136", new Vector3(27.6214f, 16.1188f, 118.3419f), new Vector3(10.0929f, 316.4656f, 0f), new Vector3(10.2f, 0.42f, 2.8459f)),
        new("Road_137", new Vector3(25.7114f, 15.6289f, 120.1641f), new Vector3(10.8836f, 310.5403f, 0f), new Vector3(10.2f, 0.42f, 2.8457f)),
        new("Road_138", new Vector3(23.6201f, 15.1065f, 121.7642f), new Vector3(11.3787f, 304.8749f, 0f), new Vector3(10.2f, 0.42f, 2.8465f)),
        new("Road_139", new Vector3(21.3966f, 14.5704f, 123.1713f), new Vector3(11.7136f, 299.8184f, 0f), new Vector3(10.2f, 0.42f, 2.8460f)),
        new("Road_140", new Vector3(19.0621f, 14.0172f, 124.3765f), new Vector3(12.2387f, 293.9547f, 0f), new Vector3(10.2f, 0.42f, 2.8450f)),
        new("Road_141", new Vector3(16.6096f, 13.4342f, 125.2981f), new Vector3(13.0235f, 285.8055f, 0f), new Vector3(10.2f, 0.42f, 2.8425f)),
        new("Road_142", new Vector3(14.0491f, 12.8121f, 125.7956f), new Vector3(13.9439f, 275.1771f, 0f), new Vector3(10.2f, 0.42f, 2.8435f)),
        new("Road_143", new Vector3(11.4502f, 12.1479f, 125.7655f), new Vector3(14.4961f, 264.8598f, 0f), new Vector3(10.2f, 0.42f, 2.8450f)),
        new("Road_144", new Vector3(8.8891f, 11.4727f, 125.3314f), new Vector3(14.3248f, 256.7641f, 0f), new Vector3(10.2f, 0.42f, 2.8456f)),
        new("Road_145", new Vector3(6.3963f, 10.8222f, 124.5768f), new Vector3(13.4261f, 249.8441f, 0f), new Vector3(10.2f, 0.42f, 2.8456f)),
        new("Road_146", new Vector3(3.9947f, 10.2281f, 123.5349f), new Vector3(11.7344f, 243.4027f, 0f), new Vector3(10.2f, 0.42f, 2.8460f)),
        new("Road_147", new Vector3(1.7030f, 9.7319f, 122.2269f), new Vector3(9.5097f, 238.0419f, 0f), new Vector3(10.2f, 0.42f, 2.8462f)),
        new("Road_148", new Vector3(-0.4946f, 9.3417f, 120.7343f), new Vector3(7.6548f, 232.8939f, 0f), new Vector3(10.2f, 0.42f, 2.8454f)),
        new("Road_149", new Vector3(-2.5353f, 9.0176f, 119.0207f), new Vector3(6.5137f, 225.9388f, 0f), new Vector3(10.2f, 0.42f, 2.8440f)),
        new("Road_150", new Vector3(-4.3172f, 8.7343f, 117.0350f), new Vector3(5.8300f, 216.4505f, 0f), new Vector3(10.2f, 0.42f, 2.8417f)),
        new("Road_151", new Vector3(4.0571f, 9.2768f, 112.3642f), new Quaternion(-0.014603265f, 0.9874007f, -0.051007465f, -0.14908002f), new Vector3(10.2f, 0.42f, 3.8123987f)),
        new("Road_152", new Vector3(3.1830f, 8.9674f, 109.4664f), new Quaternion(-0.0097058015f, 0.98849154f, -0.048349924f, -0.14301248f), new Vector3(10.2f, 0.42f, 3.8728470f)),
        new("Road_153", new Vector3(2.3201f, 8.6743f, 106.5336f), new Quaternion(-0.005425208f, 0.9887985f, -0.04599145f, -0.14189087f), new Vector3(10.2f, 0.42f, 3.8693607f)),
        new("Road_154", new Vector3(1.4583f, 8.4016f, 103.6358f), new Quaternion(-0.0011896238f, 0.98836297f, -0.04388945f, -0.14563982f), new Vector3(10.2f, 0.42f, 3.8017268f)),
        new("Road_155", new Vector3(0.5873f, 8.1534f, 100.8429f), new Quaternion(0.0036213635f, 0.9870409f, -0.04203136f, -0.15482447f), new Vector3(10.2f, 0.42f, 3.6706824f)),
        new("Road_156", new Vector3(-0.3033f, 7.9337f, 98.2250f), new Quaternion(0.009880775f, 0.98441726f, -0.040482473f, -0.17083983f), new Vector3(10.2f, 0.42f, 3.4781387f)),
        new("Road_157", new Vector3(-1.2238f, 7.7466f, 95.8518f), new Quaternion(0.019104604f, 0.9795398f, -0.039525736f, -0.19640388f), new Vector3(10.2f, 0.42f, 3.2281050f)),
        new("Road_158", new Vector3(-2.1843f, 7.5962f, 93.7935f), new Quaternion(0.034218606f, 0.9701685f, -0.040090766f, -0.23663284f), new Vector3(10.2f, 0.42f, 2.9286280f)),
        new("Road_159", new Vector3(-3.1953f, 7.4864f, 92.1200f), new Quaternion(0.06066577f, 0.9505662f, -0.045087714f, -0.30118242f), new Vector3(10.2f, 0.42f, 2.5969690f)),
    };

    [MenuItem("Tools/Controllers Trial/Apply Road Base Brown")]
    public static void ApplyRoadBaseBrown()
    {
        GameObject roadRoot = FindRoadRoot();
        if (roadRoot == null)
        {
            Debug.LogError("Could not find TerrainRoverCourse/RoverRoad in the active scene.");
            return;
        }

        Material baseMaterial = GetOrCreateBaseMaterial();
        if (baseMaterial == null)
        {
            Debug.LogError("Could not create or load the base brown material.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        Transform existingBaseRoot = roadRoot.transform.parent.Find(BaseRootName);
        if (existingBaseRoot != null)
        {
            Undo.DestroyObjectImmediate(existingBaseRoot.gameObject);
        }

        GameObject baseRoot = new GameObject(BaseRootName);
        Undo.RegisterCreatedObjectUndo(baseRoot, "Create road base root");
        baseRoot.transform.SetParent(roadRoot.transform.parent, false);

        for (int i = 0; i < roadRoot.transform.childCount; i++)
        {
            Transform road = roadRoot.transform.GetChild(i);
            if (!road.name.StartsWith("Road_"))
            {
                continue;
            }

            GameObject basePiece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(basePiece, "Create road base piece");
            basePiece.name = "Base_" + road.name;
            basePiece.transform.SetParent(baseRoot.transform, false);

            Vector3 scale = road.localScale;
            Vector3 up = road.up;

            basePiece.transform.position = road.position - up * 0.16f;
            basePiece.transform.rotation = road.rotation;
            basePiece.transform.localScale = new Vector3(scale.x * 1.06f, scale.y + 0.18f, scale.z * 1.01f);

            Renderer renderer = basePiece.GetComponent<Renderer>();
            renderer.sharedMaterial = baseMaterial;
        }

        Undo.CollapseUndoOperations(undoGroup);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Applied opaque brown bases under RoverRoad segments.");
    }

    [MenuItem("Tools/Controllers Trial/Rebuild Generated Rover Course")]
    public static void RebuildGeneratedRoverCourse()
    {
        TerrainRoadCourseBuilder builder = Object.FindFirstObjectByType<TerrainRoadCourseBuilder>();
        if (builder == null)
        {
            Debug.LogError("Could not find a TerrainRoadCourseBuilder in the active scene.");
            return;
        }

        builder.Build();
        ApplyRoadBaseBrown();
        ApplyRoadEdgeGuards();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Rebuilt the generated rover course and refreshed the road support pieces.");
    }

    private static Material GetOrCreateBaseMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            return null;
        }

        material = new Material(shader);
        material.color = new Color(0.34f, 0.22f, 0.12f, 1f);
        material.SetFloat("_Surface", 0f);
        material.SetFloat("_AlphaClip", 0f);
        material.SetFloat("_Smoothness", 0.08f);
        material.SetFloat("_Metallic", 0f);

        AssetDatabase.CreateAsset(material, MaterialPath);
        AssetDatabase.SaveAssets();
        return material;
    }

    [MenuItem("Tools/Controllers Trial/Apply Road Edge Guards")]
    public static void ApplyRoadEdgeGuards()
    {
        GameObject roadRoot = FindRoadRoot();
        if (roadRoot == null)
        {
            Debug.LogError("Could not find TerrainRoverCourse/RoverRoad in the active scene.");
            return;
        }

        Material edgeMaterial = GetOrCreateEdgeMaterial();
        if (edgeMaterial == null)
        {
            Debug.LogError("Could not create or load the road edge material.");
            return;
        }

        PhysicsMaterial guidePhysicsMaterial = GetOrCreateGuidePhysicsMaterial();
        if (guidePhysicsMaterial == null)
        {
            Debug.LogError("Could not create or load the road guide physics material.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        Transform existingEdgeRoot = roadRoot.transform.parent.Find(EdgeRootName);
        if (existingEdgeRoot != null)
        {
            Undo.DestroyObjectImmediate(existingEdgeRoot.gameObject);
        }

        Transform existingGuideRoot = roadRoot.transform.parent.Find(GuideRootName);
        if (existingGuideRoot != null)
        {
            Undo.DestroyObjectImmediate(existingGuideRoot.gameObject);
        }

        Transform existingShelfRoot = roadRoot.transform.parent.Find(ShelfRootName);
        if (existingShelfRoot != null)
        {
            Undo.DestroyObjectImmediate(existingShelfRoot.gameObject);
        }

        GameObject edgeRoot = new GameObject(EdgeRootName);
        Undo.RegisterCreatedObjectUndo(edgeRoot, "Create road edge root");
        edgeRoot.transform.SetParent(roadRoot.transform.parent, false);

        GameObject guideRoot = new GameObject(GuideRootName);
        Undo.RegisterCreatedObjectUndo(guideRoot, "Create road guide root");
        guideRoot.transform.SetParent(roadRoot.transform.parent, false);

        GameObject shelfRoot = new GameObject(ShelfRootName);
        Undo.RegisterCreatedObjectUndo(shelfRoot, "Create road catch shelf root");
        shelfRoot.transform.SetParent(roadRoot.transform.parent, false);

        for (int i = 0; i < roadRoot.transform.childCount; i++)
        {
            Transform road = roadRoot.transform.GetChild(i);
            if (!road.name.StartsWith("Road_"))
            {
                continue;
            }

            Vector3 scale = road.localScale;
            Vector3 right = road.right;
            Vector3 up = road.up;
            float offset = scale.x * 0.5f - 0.18f;

            GameObject leftEdge = CreateEdgePiece(edgeRoot.transform, road, edgeMaterial, "L", right * -offset + up * 0.23f,
                new Vector3(0.34f, 0.46f, scale.z * 0.98f));
            GameObject rightEdge = CreateEdgePiece(edgeRoot.transform, road, edgeMaterial, "R", right * offset + up * 0.23f,
                new Vector3(0.34f, 0.46f, scale.z * 0.98f));

            DisableEdgeCollider(leftEdge);
            DisableEdgeCollider(rightEdge);

            CreateCatchShelf(shelfRoot.transform, road, guidePhysicsMaterial, "L", -1f);
            CreateCatchShelf(shelfRoot.transform, road, guidePhysicsMaterial, "R", 1f);
            CreateGuidePiece(guideRoot.transform, road, guidePhysicsMaterial, "L", -1f);
            CreateGuidePiece(guideRoot.transform, road, guidePhysicsMaterial, "R", 1f);
        }

        EnsureRecoveryAssistComponents();

        Undo.CollapseUndoOperations(undoGroup);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Applied forgiving edge guards, catch shelves, and guide rails on RoverRoad segments.");
    }

    [MenuItem("Tools/Controllers Trial/Smooth Bridge Curve For Comfort")]
    public static void SmoothBridgeCurveForComfort()
    {
        GameObject roadRoot = FindRoadRoot();
        if (roadRoot == null)
        {
            Debug.LogError("Could not find TerrainRoverCourse/RoverRoad in the active scene.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        foreach (RoadCurveState state in BridgeComfortCurveStates)
        {
            Transform road = roadRoot.transform.Find(state.Name);
            if (road == null)
            {
                Debug.LogWarning($"Could not find {state.Name} while smoothing the bridge curve.");
                continue;
            }

            Undo.RecordObject(road, "Smooth bridge curve");
            road.localPosition = state.LocalPosition;
            road.localRotation = state.LocalRotation;
            road.localScale = state.LocalScale;
            EditorUtility.SetDirty(road);
        }

        Undo.CollapseUndoOperations(undoGroup);

        ApplyRoadBaseBrown();
        ApplyRoadEdgeGuards();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Smoothed the bridge curve and rebuilt the matching road support pieces.");
    }

    [MenuItem("Tools/Controllers Trial/Replace Closed Curve With Zone03 Bridge")]
    public static void ReplaceClosedCurveWithZone03Bridge()
    {
        GameObject roadRoot = FindRoadRoot();
        GameObject zoneRoot = FindZone03Root();
        if (roadRoot == null)
        {
            Debug.LogError("Could not find TerrainRoverCourse/RoverRoad in the active scene.");
            return;
        }

        if (zoneRoot == null)
        {
            Debug.LogError("Could not find Zone03_SteepRamps (1) in the active scene.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        Undo.RecordObject(zoneRoot.transform, "Replace closed curve with Zone03 bridge");
        zoneRoot.transform.position = Zone03BridgePosition;
        zoneRoot.transform.rotation = Quaternion.Euler(Zone03BridgeEuler);
        zoneRoot.transform.localScale = Zone03BridgeScale;
        EditorUtility.SetDirty(zoneRoot.transform);

        foreach (string roadName in ClosedCurveRoadNames)
        {
            Transform road = roadRoot.transform.Find(roadName);
            if (road == null)
            {
                continue;
            }

            Undo.DestroyObjectImmediate(road.gameObject);
        }

        Undo.CollapseUndoOperations(undoGroup);

        ApplyRoadBaseBrown();
        ApplyRoadEdgeGuards();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Replaced the closed curve with the Zone03 bridge connection and rebuilt matching support pieces.");
    }

    [MenuItem("Tools/Controllers Trial/Rebuild Smooth Downhill Connector")]
    public static void RebuildSmoothDownhillConnector()
    {
        GameObject roadRoot = FindRoadRoot();
        if (roadRoot == null)
        {
            Debug.LogError("Could not find TerrainRoverCourse/RoverRoad in the active scene.");
            return;
        }

        Material roadMaterial = GetRoadMaterial(roadRoot.transform);
        if (roadMaterial == null)
        {
            Debug.LogError("Could not find an existing road material to apply.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        foreach (RoadCurveState state in DownhillComfortConnectorStates)
        {
            Transform road = EnsureRoadSegment(roadRoot.transform, state.Name, roadMaterial);
            if (road == null)
            {
                Debug.LogWarning($"Could not create or find {state.Name}.");
                continue;
            }

            Undo.RecordObject(road, "Rebuild smooth downhill connector");
            road.localPosition = state.LocalPosition;
            road.localRotation = state.LocalRotation;
            road.localScale = state.LocalScale;
            EditorUtility.SetDirty(road);
        }

        Undo.CollapseUndoOperations(undoGroup);

        ApplyRoadBaseBrown();
        ApplyRoadEdgeGuards();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Rebuilt the downhill connector with smoother curves and a gentler descent.");
    }

    [MenuItem("Tools/Controllers Trial/Build Long Right Edge Descent")]
    public static void BuildLongRightEdgeDescent()
    {
        GameObject roadRoot = FindRoadRoot();
        if (roadRoot == null)
        {
            Debug.LogError("Could not find TerrainRoverCourse/RoverRoad in the active scene.");
            return;
        }

        Material roadMaterial = GetRoadMaterial(roadRoot.transform);
        if (roadMaterial == null)
        {
            Debug.LogError("Could not find an existing road material to apply.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        DeleteRoadRange(roadRoot.transform, BranchRoadStartIndex, BranchRoadEndIndex);

        RoadCurveState[] states = BuildLongRightEdgeBranchStates();
        foreach (RoadCurveState state in states)
        {
            Transform road = EnsureRoadSegment(roadRoot.transform, state.Name, roadMaterial);
            if (road == null)
            {
                Debug.LogWarning($"Could not create or find {state.Name}.");
                continue;
            }

            Undo.RecordObject(road, "Build long right edge descent");
            road.localPosition = state.LocalPosition;
            road.localRotation = state.LocalRotation;
            road.localScale = state.LocalScale;
            EditorUtility.SetDirty(road);
        }

        Transform testDup = roadRoot.transform.Find("Road_TestDup");
        if (testDup != null)
        {
            Undo.DestroyObjectImmediate(testDup.gameObject);
        }

        Undo.CollapseUndoOperations(undoGroup);

        ApplyRoadBaseBrown();
        ApplyRoadEdgeGuards();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Built the long right-edge downhill branch and refreshed the road support pieces.");
    }

    [MenuItem("Tools/Controllers Trial/Smooth Marked Bridge Curves")]
    public static void SmoothMarkedBridgeCurves()
    {
        GameObject roadRoot = FindRoadRoot();
        if (roadRoot == null)
        {
            Debug.LogError("Could not find TerrainRoverCourse/RoverRoad in the active scene.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        foreach ((int start, int end) in MarkedCurveRanges)
        {
            SmoothRoadRange(roadRoot.transform, start, end, 3);
        }

        Undo.CollapseUndoOperations(undoGroup);

        ApplyRoadBaseBrown();
        ApplyRoadEdgeGuards();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Smoothed the two marked bridge curves and rebuilt the matching support pieces.");
    }

    private static GameObject CreateEdgePiece(Transform parent, Transform road, Material material, string sideSuffix, Vector3 offset, Vector3 scale)
    {
        GameObject edgePiece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(edgePiece, "Create road edge piece");
        edgePiece.name = "Edge_" + road.name + "_" + sideSuffix;
        edgePiece.transform.SetParent(parent, false);
        edgePiece.transform.position = road.position + offset;
        edgePiece.transform.rotation = road.rotation;
        edgePiece.transform.localScale = scale;

        Renderer renderer = edgePiece.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        return edgePiece;
    }

    private static void DisableEdgeCollider(GameObject edgePiece)
    {
        if (edgePiece == null)
            return;

        BoxCollider collider = edgePiece.GetComponent<BoxCollider>();
        if (collider != null)
        {
            collider.enabled = false;
            EditorUtility.SetDirty(collider);
        }
    }

    private static void CreateGuidePiece(Transform parent, Transform road, PhysicsMaterial guidePhysicsMaterial, string sideSuffix, float sideSign)
    {
        Vector3 scale = road.localScale;
        Vector3 right = road.right;
        Vector3 up = road.up;
        float centerOffset = scale.x * 0.5f + (EdgeGuideWidth * 0.5f) - EdgeGuideInnerOverlap;

        GameObject guidePiece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(guidePiece, "Create road guide piece");
        guidePiece.name = "Guide_" + road.name + "_" + sideSuffix;
        guidePiece.transform.SetParent(parent, false);
        guidePiece.transform.position = road.position + right * (sideSign * centerOffset) + up * 0.14f;
        guidePiece.transform.rotation = road.rotation * Quaternion.AngleAxis(sideSign * EdgeGuideTiltDegrees, Vector3.forward);
        guidePiece.transform.localScale = new Vector3(EdgeGuideWidth, EdgeGuideHeight, scale.z * EdgeGuideLengthScale);

        Renderer renderer = guidePiece.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }

        BoxCollider collider = guidePiece.GetComponent<BoxCollider>();
        if (collider != null)
        {
            collider.sharedMaterial = guidePhysicsMaterial;
        }
    }

    private static void CreateCatchShelf(Transform parent, Transform road, PhysicsMaterial guidePhysicsMaterial, string sideSuffix, float sideSign)
    {
        Vector3 scale = road.localScale;
        Vector3 right = road.right;
        Vector3 up = road.up;
        float centerOffset = scale.x * 0.5f + (CatchShelfWidth * 0.5f) - CatchShelfInnerOverlap;

        GameObject shelfPiece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(shelfPiece, "Create road catch shelf");
        shelfPiece.name = "Shelf_" + road.name + "_" + sideSuffix;
        shelfPiece.transform.SetParent(parent, false);
        shelfPiece.transform.position = road.position + right * (sideSign * centerOffset) - up * CatchShelfDrop;
        shelfPiece.transform.rotation = road.rotation;
        shelfPiece.transform.localScale = new Vector3(CatchShelfWidth, CatchShelfHeight, scale.z * CatchShelfLengthScale);

        Renderer renderer = shelfPiece.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }

        BoxCollider collider = shelfPiece.GetComponent<BoxCollider>();
        if (collider != null)
        {
            collider.sharedMaterial = guidePhysicsMaterial;
        }
    }

    private static void EnsureRecoveryAssistComponents()
    {
        RoverPhysicsController[] controllers = Object.FindObjectsByType<RoverPhysicsController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (RoverPhysicsController controller in controllers)
        {
            if (controller == null)
                continue;

            RoverEdgeRecoveryAssist assist = controller.GetComponent<RoverEdgeRecoveryAssist>();
            if (assist == null)
            {
                assist = Undo.AddComponent<RoverEdgeRecoveryAssist>(controller.gameObject);
            }

            Undo.RecordObject(assist, "Configure rover edge recovery assist");
            assist.rb = controller.rb != null ? controller.rb : controller.GetComponent<Rigidbody>();
            assist.controller = controller;
            EditorUtility.SetDirty(assist);

            RoverRoadFailSafe failSafe = controller.GetComponent<RoverRoadFailSafe>();
            if (failSafe == null)
            {
                failSafe = Undo.AddComponent<RoverRoadFailSafe>(controller.gameObject);
            }

            Undo.RecordObject(failSafe, "Configure rover road fail-safe");
            failSafe.rb = controller.rb != null ? controller.rb : controller.GetComponent<Rigidbody>();
            failSafe.controller = controller;
            EditorUtility.SetDirty(failSafe);
        }
    }

    private static Material GetOrCreateEdgeMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(EdgeMaterialPath);
        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            return null;
        }

        material = new Material(shader);
        material.color = new Color(0.28f, 0.18f, 0.10f, 1f);
        material.SetFloat("_Surface", 0f);
        material.SetFloat("_AlphaClip", 0f);
        material.SetFloat("_Smoothness", 0.06f);
        material.SetFloat("_Metallic", 0f);

        AssetDatabase.CreateAsset(material, EdgeMaterialPath);
        AssetDatabase.SaveAssets();
        return material;
    }

    private static PhysicsMaterial GetOrCreateGuidePhysicsMaterial()
    {
        PhysicsMaterial material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(GuidePhysicsMaterialPath);
        if (material != null)
        {
            return material;
        }

        material = new PhysicsMaterial("RoverRoad_Guide")
        {
            dynamicFriction = 0.02f,
            staticFriction = 0.02f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };

        AssetDatabase.CreateAsset(material, GuidePhysicsMaterialPath);
        AssetDatabase.SaveAssets();
        return material;
    }

    private static Material GetRoadMaterial(Transform roadRoot)
    {
        for (int i = 0; i < roadRoot.childCount; i++)
        {
            Renderer renderer = roadRoot.GetChild(i).GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                return renderer.sharedMaterial;
            }
        }

        return null;
    }

    private static void DeleteRoadRange(Transform roadRoot, int startIndex, int endIndex)
    {
        for (int i = roadRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = roadRoot.GetChild(i);
            if (!child.name.StartsWith("Road_"))
            {
                continue;
            }

            if (!int.TryParse(child.name.Substring("Road_".Length), out int number))
            {
                continue;
            }

            if (number >= startIndex && number <= endIndex)
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }
    }

    private static RoadCurveState[] BuildLongRightEdgeBranchStates()
    {
        Vector3[] mainControlPoints =
        {
            new(-6f, 4.8f, -438f),
            new(-4f, 5.0f, -380f),
            new(8f, 5.4f, -322f),
            new(42f, 6.2f, -278f),
            new(86f, 7.4f, -232f),
            new(54f, 8.2f, -178f),
            new(2f, 9.2f, -132f),
            new(-56f, 10.4f, -96f),
            new(-28f, 11.8f, -40f),
            new(12f, 14.8f, 6f),
            new(42f, 18.0f, 42f),
            new(58f, 20.8f, 86f),
            new(28f, 23.6f, 126f),
            new(-20f, 27.4f, 160f),
            new(-70f, 32.8f, 198f),
            new(-88f, 38.0f, 244f),
            new(-36f, 43.2f, 292f),
            new(18f, 48.8f, 322f),
            new(62f, 54.0f, 342f),
        };

        List<Vector3> mainPoints = SampleCatmullPath(mainControlPoints, 18);
        Vector3 spurStart = mainPoints[Mathf.Clamp(mainPoints.Count - 12, 0, mainPoints.Count - 1)];
        Vector3[] spurControlPoints =
        {
            spurStart,
            new(92f, 52f, 304f),
            new(114f, 47f, 238f),
            new(124f, 41f, 156f),
            new(122f, 33f, 72f),
            new(110f, 24f, 12f),
            new(84f, 15f, -8f),
            new(50f, 9f, 2f),
            new(26f, 6f, 28f),
            new(18f, 5.6f, 60f),
        };

        List<Vector3> points = SampleCatmullPath(spurControlPoints, 10);
        RoadCurveState[] states = new RoadCurveState[points.Count - 1];
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[i + 1];
            float t = i / (float)(points.Count - 1);
            Vector3 center = (a + b) * 0.5f + Vector3.up * 0.21f;
            Vector3 segment = b - a;
            float width = Mathf.Lerp(10.4f, 9.8f, t);
            Vector3 scale = new(width, 0.42f, segment.magnitude + 0.8f);
            Quaternion rotation = Quaternion.LookRotation(segment.normalized, Vector3.up);
            states[i] = new RoadCurveState($"Road_{BranchRoadStartIndex + i:000}", center, rotation, scale);
        }

        return states;
    }

    private static List<Vector3> SampleCatmullPath(Vector3[] controlPoints, int samplesPerSegment)
    {
        List<Vector3> points = new();
        for (int i = 0; i < controlPoints.Length - 1; i++)
        {
            Vector3 p0 = i == 0 ? controlPoints[i] : controlPoints[i - 1];
            Vector3 p1 = controlPoints[i];
            Vector3 p2 = controlPoints[i + 1];
            Vector3 p3 = i + 2 >= controlPoints.Length ? controlPoints[i + 1] : controlPoints[i + 2];
            for (int j = 0; j < samplesPerSegment; j++)
            {
                float t = j / (float)samplesPerSegment;
                float t2 = t * t;
                float t3 = t2 * t;
                Vector3 point = 0.5f * ((2f * p1)
                    + (-p0 + p2) * t
                    + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
                    + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
                points.Add(point);
            }
        }

        points.Add(controlPoints[^1]);
        return points;
    }

    private static Transform EnsureRoadSegment(Transform roadRoot, string roadName, Material material)
    {
        Transform road = roadRoot.Find(roadName);
        if (road != null)
        {
            return road;
        }

        GameObject roadObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(roadObject, "Create downhill road segment");
        roadObject.name = roadName;
        roadObject.transform.SetParent(roadRoot, false);

        Renderer renderer = roadObject.GetComponent<Renderer>();
        renderer.sharedMaterial = material;

        return roadObject.transform;
    }

    private static void SmoothRoadRange(Transform roadRoot, int startIndex, int endIndex, int iterations)
    {
        List<Transform> roads = new();
        for (int i = startIndex; i <= endIndex; i++)
        {
            Transform road = roadRoot.Find($"Road_{i:000}");
            if (road != null)
            {
                roads.Add(road);
            }
        }

        if (roads.Count < 4)
        {
            Debug.LogWarning($"Not enough road segments found to smooth range {startIndex}-{endIndex}.");
            return;
        }

        List<Vector3> smoothedPositions = new();
        foreach (Transform road in roads)
        {
            smoothedPositions.Add(road.localPosition);
        }

        for (int iteration = 0; iteration < iterations; iteration++)
        {
            List<Vector3> next = new(smoothedPositions);
            for (int i = 1; i < smoothedPositions.Count - 1; i++)
            {
                next[i] = smoothedPositions[i - 1] * 0.2f + smoothedPositions[i] * 0.6f + smoothedPositions[i + 1] * 0.2f;
            }

            smoothedPositions = next;
        }

        for (int i = 0; i < roads.Count; i++)
        {
            Transform road = roads[i];
            Undo.RecordObject(road, "Smooth marked bridge curves");
            road.localPosition = smoothedPositions[i];

            Vector3 tangent;
            if (i == 0)
            {
                tangent = smoothedPositions[i + 1] - smoothedPositions[i];
            }
            else if (i == smoothedPositions.Count - 1)
            {
                tangent = smoothedPositions[i] - smoothedPositions[i - 1];
            }
            else
            {
                tangent = smoothedPositions[i + 1] - smoothedPositions[i - 1];
            }

            if (tangent.sqrMagnitude > 0.0001f)
            {
                float bank = NormalizeAngle(road.localEulerAngles.z);
                road.localRotation = Quaternion.LookRotation(tangent.normalized, Vector3.up) * Quaternion.AngleAxis(bank, Vector3.forward);
            }

            Vector3 scale = road.localScale;
            float prevDistance = i > 0 ? Vector3.Distance(smoothedPositions[i], smoothedPositions[i - 1]) : Mathf.Max(0.1f, scale.z - 0.8f);
            float nextDistance = i < smoothedPositions.Count - 1 ? Vector3.Distance(smoothedPositions[i], smoothedPositions[i + 1]) : Mathf.Max(0.1f, scale.z - 0.8f);
            scale.z = ((prevDistance + nextDistance) * 0.5f) + 0.8f;
            road.localScale = scale;
            EditorUtility.SetDirty(road);
        }
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    private static GameObject FindRoadRoot()
    {
        return GameObject.Find("TerrainRoverCourse/RoverRoad");
    }

    private static GameObject FindZone03Root()
    {
        return GameObject.Find("Zone03_SteepRamps (1)") ?? GameObject.Find("Zone03_SteepRamps");
    }

    private readonly struct RoadCurveState
    {
        public readonly string Name;
        public readonly Vector3 LocalPosition;
        public readonly Quaternion LocalRotation;
        public readonly Vector3 LocalScale;

        public RoadCurveState(string name, Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale)
        {
            Name = name;
            LocalPosition = localPosition;
            LocalRotation = Quaternion.Euler(localEulerAngles);
            LocalScale = localScale;
        }

        public RoadCurveState(string name, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
        {
            Name = name;
            LocalPosition = localPosition;
            LocalRotation = localRotation;
            LocalScale = localScale;
        }
    }
}
