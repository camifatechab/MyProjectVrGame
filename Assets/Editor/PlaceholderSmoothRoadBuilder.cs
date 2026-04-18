#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

internal static class PlaceholderSmoothRoadBuilder
{
    private const string RoadRootPath = "TerrainRoverCourse/RoverRoad";
    private const string PlaceholderRootPath = "TerrainRoverCourse/RoverRoad/Road_PlaceholderStyled";
    private const string SmoothRoadName = "Road_PlaceholderSmooth";
    private const string SmoothBaseName = "Base_Road_PlaceholderSmooth";
    private const string SmoothLeftEdgeName = "Edge_Road_PlaceholderSmooth_Left";
    private const string SmoothRightEdgeName = "Edge_Road_PlaceholderSmooth_Right";
    private const string BaseSegmentsParentName = "Base_Road_PlaceholderSmooth_Segments";
    private const string LeftSegmentsParentName = "Edge_Road_PlaceholderSmooth_Left_Segments";
    private const string RightSegmentsParentName = "Edge_Road_PlaceholderSmooth_Right_Segments";

    private const float SampleStep = 1.35f;
    private const float RoadThickness = 0.42f;
    private const float SurfaceLift = 0.02f;
    private const float UvTileLength = 4f;
    private const float BaseWidthPadding = 5f;
    private const float BaseThickness = 2.8f;
    private const float BaseLift = -1.25f;
    private const float EdgeHeight = 1.45f;
    private const float EdgeThickness = 0.55f;
    private const float EdgeInset = 0.15f;
    private const float EdgeBaseDrop = 0.25f;
    private const float EdgeTopLift = 0.1f;
    private const int CompanionStride = 4;
    private const float CompanionSegmentOverlap = 0.85f;
    private const float FallbackBaseYOffset = -1.45f;
    private const float FallbackEdgeYOffset = 0.4f;
    private const int VrCurveStartTile = 14;
    private const int VrCurveEndTile = 29;
    private const int VrSlopeStartTile = 13;
    private const int VrSlopeEndTile = 32;
    private const int VrHorizontalSmoothIterations = 5;
    private const float VrHorizontalBlend = 0.6f;
    private const float VrMidSlopeLift = 1.75f;

    private static readonly Regex PlaceholderNameRegex = new Regex(@"^\s*m_Name: Road_Placeholder_(\d{3})\s*$");
    private static readonly Regex Vector3Regex = new Regex(@"x:\s*([^,]+), y:\s*([^,]+), z:\s*([^}]+)");

    private readonly struct SourceTile
    {
        public SourceTile(int number, Vector3 position, float width)
        {
            Number = number;
            Position = position;
            Width = width;
        }

        public int Number { get; }
        public Vector3 Position { get; }
        public float Width { get; }
    }

    [MenuItem("Tools/Rover/Build Smooth Placeholder Road")]
    private static void BuildSmoothRoad()
    {
        RebuildSmoothRoad();
    }

    internal static void RebuildSmoothRoad()
    {
        GameObject roadRoot = GameObject.Find(RoadRootPath);
        GameObject placeholderRoot = GameObject.Find(PlaceholderRootPath);
        if (roadRoot == null)
        {
            Debug.LogError("[SmoothRoad] Could not find TerrainRoverCourse/RoverRoad.");
            return;
        }

        List<SourceTile> sourceTiles = GetSourceTiles(roadRoot.transform, placeholderRoot);
        if (sourceTiles.Count < 4)
        {
            Debug.LogError("[SmoothRoad] Need at least 4 placeholder source tiles to build a smooth road.");
            return;
        }

        float roadWidth = sourceTiles.Average(t => t.Width);
        Material roadMaterial = GetRoadMaterial(roadRoot.transform, placeholderRoot);
        Material baseMaterial = GetTemplateMaterial("Base_Road_000", roadMaterial);
        Material edgeMaterial = GetTemplateMaterial("Edge_Road_000", roadMaterial);
        if (roadMaterial == null)
        {
            Debug.LogError("[SmoothRoad] Could not find a road material.");
            return;
        }

        List<int> tileNumbers = sourceTiles.Select(t => t.Number).ToList();
        List<Vector3> controlPoints = sourceTiles.Select(t => t.Position).ToList();
        ApplyVrFriendlyCorner(tileNumbers, controlPoints);
        List<Vector3> sampledPoints = SampleCatmullRom(controlPoints, SampleStep);
        if (sampledPoints.Count < 2)
        {
            Debug.LogError("[SmoothRoad] Failed to sample a usable smooth path.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        GameObject smoothRoad = FindOrCreateChild(roadRoot.transform, SmoothRoadName);
        Undo.RegisterCompleteObjectUndo(smoothRoad, "Build smooth placeholder road");

        MeshFilter meshFilter = EnsureComponent<MeshFilter>(smoothRoad);
        MeshRenderer meshRenderer = EnsureComponent<MeshRenderer>(smoothRoad);
        MeshCollider meshCollider = EnsureComponent<MeshCollider>(smoothRoad);

        Mesh mesh = BuildStripMesh(sampledPoints, roadWidth, RoadThickness, SurfaceLift, UvTileLength);
        Vector3 pivotOffset = CenterMeshPivot(mesh);
        mesh.name = "Road_PlaceholderSmooth_Mesh";
        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterial = roadMaterial;
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;
        smoothRoad.transform.localPosition = pivotOffset;
        smoothRoad.transform.localRotation = Quaternion.identity;
        smoothRoad.transform.localScale = Vector3.one;
        smoothRoad.isStatic = true;

        GameObject smoothBase = FindOrCreateChild(roadRoot.transform, SmoothBaseName);
        ConfigureCompanionMesh(
            smoothBase,
            BuildStripMesh(sampledPoints, roadWidth + BaseWidthPadding, BaseThickness, BaseLift, UvTileLength),
            "Base_Road_PlaceholderSmooth_Mesh",
            baseMaterial,
            addCollider: false);

        GameObject smoothLeftEdge = FindOrCreateChild(roadRoot.transform, SmoothLeftEdgeName);
        ConfigureCompanionMesh(
            smoothLeftEdge,
            BuildEdgeMesh(sampledPoints, roadWidth, leftSide: true, EdgeInset, EdgeThickness, EdgeHeight, EdgeBaseDrop, EdgeTopLift, UvTileLength),
            "Edge_Road_PlaceholderSmooth_Left_Mesh",
            edgeMaterial,
            addCollider: true);

        GameObject smoothRightEdge = FindOrCreateChild(roadRoot.transform, SmoothRightEdgeName);
        ConfigureCompanionMesh(
            smoothRightEdge,
            BuildEdgeMesh(sampledPoints, roadWidth, leftSide: false, EdgeInset, EdgeThickness, EdgeHeight, EdgeBaseDrop, EdgeTopLift, UvTileLength),
            "Edge_Road_PlaceholderSmooth_Right_Mesh",
            edgeMaterial,
            addCollider: true);

        if (placeholderRoot != null)
        {
            foreach (Transform tile in placeholderRoot.transform)
            {
                MeshRenderer renderer = tile.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    Undo.RecordObject(renderer, "Disable placeholder tile renderer");
                    renderer.enabled = false;
                }

                Collider collider = tile.GetComponent<Collider>();
                if (collider != null)
                {
                    Undo.RecordObject(collider, "Disable placeholder tile collider");
                    collider.enabled = false;
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(roadRoot.scene);
        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"[SmoothRoad] Built smooth road mesh from {sourceTiles.Count} source tiles with {sampledPoints.Count} sampled points.");
    }

    internal static void BuildSegmentedCompanionsFallback()
    {
        GameObject roadRoot = GameObject.Find(RoadRootPath);
        GameObject placeholderRoot = GameObject.Find(PlaceholderRootPath);
        if (roadRoot == null)
        {
            Debug.LogWarning("[SmoothRoad] Fallback skipped: road root not found.");
            return;
        }

        List<SourceTile> sourceTiles = GetSourceTiles(roadRoot.transform, placeholderRoot);
        if (sourceTiles.Count < 4)
        {
            Debug.LogWarning($"[SmoothRoad] Fallback skipped: only found {sourceTiles.Count} source tiles.");
            return;
        }

        float roadWidth = sourceTiles.Average(t => t.Width);
        Material roadMaterial = GetRoadMaterial(roadRoot.transform, placeholderRoot);
        Material baseMaterial = GetTemplateMaterial("Base_Road_000", roadMaterial);
        Material edgeMaterial = roadMaterial;

        List<int> tileNumbers = sourceTiles.Select(t => t.Number).ToList();
        List<Vector3> controlPoints = sourceTiles.Select(t => t.Position).ToList();
        ApplyVrFriendlyCorner(tileNumbers, controlPoints);
        List<Vector3> sampledPoints = SampleCatmullRom(controlPoints, SampleStep);
        List<Vector3> segmentPoints = DownsamplePoints(sampledPoints, CompanionStride);

        Transform baseParent = FindOrCreateChild(roadRoot.transform, BaseSegmentsParentName).transform;
        Transform leftParent = FindOrCreateChild(roadRoot.transform, LeftSegmentsParentName).transform;
        Transform rightParent = FindOrCreateChild(roadRoot.transform, RightSegmentsParentName).transform;

        ClearGeneratedChildren(baseParent);
        ClearGeneratedChildren(leftParent);
        ClearGeneratedChildren(rightParent);

        for (int i = 0; i < segmentPoints.Count - 1; i++)
        {
            Vector3 p0 = segmentPoints[i];
            Vector3 p1 = segmentPoints[i + 1];
            Vector3 segment = p1 - p0;
            float length = segment.magnitude;
            if (length < 0.5f)
                continue;

            Vector3 center = (p0 + p1) * 0.5f;
            Quaternion rotation = Quaternion.LookRotation(segment.normalized, Vector3.up);
            Vector3 right = ComputeRight(segmentPoints, i);

            CreateSegmentCube(
                baseParent,
                $"Base_Road_Placeholder_{i:000}",
                center + (Vector3.up * FallbackBaseYOffset),
                rotation,
                new Vector3(roadWidth + BaseWidthPadding, BaseThickness, length + CompanionSegmentOverlap),
                baseMaterial,
                enableCollider: false);

            Vector3 leftCenter = center - (right * ((roadWidth * 0.5f) + (EdgeThickness * 0.5f) - EdgeInset)) + (Vector3.up * FallbackEdgeYOffset);
            CreateSegmentCube(
                leftParent,
                $"Edge_Road_Placeholder_Left_{i:000}",
                leftCenter,
                rotation,
                new Vector3(EdgeThickness, EdgeHeight, length + CompanionSegmentOverlap),
                edgeMaterial,
                enableCollider: true);

            Vector3 rightCenter = center + (right * ((roadWidth * 0.5f) + (EdgeThickness * 0.5f) - EdgeInset)) + (Vector3.up * FallbackEdgeYOffset);
            CreateSegmentCube(
                rightParent,
                $"Edge_Road_Placeholder_Right_{i:000}",
                rightCenter,
                rotation,
                new Vector3(EdgeThickness, EdgeHeight, length + CompanionSegmentOverlap),
                edgeMaterial,
                enableCollider: true);
        }

        EditorSceneManager.MarkSceneDirty(roadRoot.scene);
        Debug.Log($"[SmoothRoad] Built fallback segmented companions from {segmentPoints.Count} path points.");
    }

    private static void ConfigureCompanionMesh(GameObject go, Mesh mesh, string meshName, Material material, bool addCollider)
    {
        Undo.RegisterCompleteObjectUndo(go, $"Build {go.name}");

        MeshFilter meshFilter = EnsureComponent<MeshFilter>(go);
        MeshRenderer meshRenderer = EnsureComponent<MeshRenderer>(go);
        MeshCollider meshCollider = go.GetComponent<MeshCollider>();

        Vector3 pivotOffset = CenterMeshPivot(mesh);
        mesh.name = meshName;
        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterial = material;
        go.transform.localPosition = pivotOffset;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        go.isStatic = true;

        if (addCollider)
        {
            meshCollider ??= EnsureComponent<MeshCollider>(go);
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;
            meshCollider.enabled = true;
        }
        else if (meshCollider != null)
        {
            Undo.RecordObject(meshCollider, $"Disable {go.name} collider");
            meshCollider.enabled = false;
        }
    }

    private static List<SourceTile> GetSourceTiles(Transform roadRoot, GameObject placeholderRoot)
    {
        if (placeholderRoot != null)
        {
            return placeholderRoot.transform.Cast<Transform>()
                .Where(t => t.gameObject.activeInHierarchy && t.name.StartsWith("Road_Placeholder_"))
                .OrderBy(t => t.name)
                .Select(t => new SourceTile(GetTileNumber(t.name), t.position, t.localScale.x))
                .Where(t => t.Number >= 0)
                .ToList();
        }

        return LoadSerializedPlaceholderTiles(roadRoot);
    }

    private static int GetTileNumber(string tileName)
    {
        string suffix = tileName.Substring(tileName.LastIndexOf('_') + 1);
        return int.TryParse(suffix, out int value) ? value : -1;
    }

    private static List<SourceTile> LoadSerializedPlaceholderTiles(Transform roadRoot)
    {
        string scenePath = EditorSceneManager.GetActiveScene().path;
        if (string.IsNullOrEmpty(scenePath))
            return new List<SourceTile>();

        string absolutePath = Path.GetFullPath(scenePath);
        if (!File.Exists(absolutePath))
            absolutePath = Path.Combine(Directory.GetCurrentDirectory(), scenePath);

        if (!File.Exists(absolutePath))
            return new List<SourceTile>();

        string[] lines = File.ReadAllLines(absolutePath);
        Dictionary<int, SourceTile> tilesByNumber = new Dictionary<int, SourceTile>();

        for (int i = 0; i < lines.Length; i++)
        {
            Match nameMatch = PlaceholderNameRegex.Match(lines[i]);
            if (!nameMatch.Success)
                continue;

            int tileNumber = int.Parse(nameMatch.Groups[1].Value);
            Vector3? localPosition = null;
            float width = 9.6f;

            for (int j = i; j < Mathf.Min(lines.Length, i + 32); j++)
            {
                if (lines[j].Contains("m_LocalPosition:"))
                    localPosition = ParseVector3(lines[j]);
                else if (lines[j].Contains("m_LocalScale:"))
                {
                    Vector3 scale = ParseVector3(lines[j]);
                    width = scale.x;
                    break;
                }
            }

            if (!localPosition.HasValue)
                continue;

            Vector3 worldPosition = roadRoot.TransformPoint(localPosition.Value);
            tilesByNumber[tileNumber] = new SourceTile(tileNumber, worldPosition, width);
        }

        return tilesByNumber.Values.OrderBy(t => t.Number).ToList();
    }

    private static Vector3 ParseVector3(string line)
    {
        Match match = Vector3Regex.Match(line);
        if (!match.Success)
            return Vector3.zero;

        return new Vector3(
            ParseFloat(match.Groups[1].Value),
            ParseFloat(match.Groups[2].Value),
            ParseFloat(match.Groups[3].Value));
    }

    private static float ParseFloat(string value)
    {
        return float.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void ApplyVrFriendlyCorner(List<int> tileNumbers, List<Vector3> controlPoints)
    {
        if (tileNumbers.Count != controlPoints.Count || controlPoints.Count < 4)
            return;

        int curveStart = tileNumbers.IndexOf(VrCurveStartTile);
        int curveEnd = tileNumbers.IndexOf(VrCurveEndTile);
        int slopeStart = tileNumbers.IndexOf(VrSlopeStartTile);
        int slopeEnd = tileNumbers.IndexOf(VrSlopeEndTile);
        if (curveStart < 0 || curveEnd < 0 || slopeStart < 0 || slopeEnd < 0)
            return;

        SmoothHorizontalRange(controlPoints, curveStart, curveEnd, VrHorizontalSmoothIterations, VrHorizontalBlend);
        EaseVerticalRange(controlPoints, slopeStart, slopeEnd, VrMidSlopeLift);
    }

    private static void SmoothHorizontalRange(List<Vector3> points, int startIndex, int endIndex, int iterations, float blend)
    {
        if (startIndex < 0 || endIndex >= points.Count || endIndex - startIndex < 3)
            return;

        for (int iteration = 0; iteration < iterations; iteration++)
        {
            List<Vector3> snapshot = new List<Vector3>(points);
            for (int i = startIndex + 1; i < endIndex; i++)
            {
                float t = Mathf.InverseLerp(startIndex, endIndex, i);
                float edgeWeight = Mathf.SmoothStep(0f, 1f, Mathf.Min(t, 1f - t) * 2f);
                float localBlend = blend * edgeWeight;

                Vector2 previous = new Vector2(snapshot[i - 1].x, snapshot[i - 1].z);
                Vector2 current = new Vector2(snapshot[i].x, snapshot[i].z);
                Vector2 next = new Vector2(snapshot[i + 1].x, snapshot[i + 1].z);
                Vector2 average = (previous + next) * 0.5f;
                Vector2 smoothed = Vector2.Lerp(current, average, localBlend);

                Vector3 adjusted = points[i];
                adjusted.x = smoothed.x;
                adjusted.z = smoothed.y;
                points[i] = adjusted;
            }
        }
    }

    private static void EaseVerticalRange(List<Vector3> points, int startIndex, int endIndex, float midLift)
    {
        if (startIndex < 0 || endIndex >= points.Count || endIndex - startIndex < 2)
            return;

        float[] distances = new float[endIndex - startIndex + 1];
        float totalDistance = 0f;
        for (int i = startIndex + 1; i <= endIndex; i++)
        {
            totalDistance += Vector3.Distance(points[i - 1], points[i]);
            distances[i - startIndex] = totalDistance;
        }

        if (totalDistance < 0.01f)
            return;

        float startY = points[startIndex].y;
        float endY = points[endIndex].y;
        for (int i = startIndex + 1; i < endIndex; i++)
        {
            float t = distances[i - startIndex] / totalDistance;
            float eased = t * t * (3f - (2f * t));
            float liftedProfile = Mathf.Lerp(startY, endY, eased) + (Mathf.Sin(t * Mathf.PI) * midLift);

            Vector3 adjusted = points[i];
            adjusted.y = Mathf.Min(startY, liftedProfile);
            points[i] = adjusted;
        }
    }

    private static Material GetRoadMaterial(Transform roadRoot, GameObject placeholderRoot)
    {
        if (placeholderRoot != null)
        {
            foreach (Transform tile in placeholderRoot.transform)
            {
                MeshRenderer renderer = tile.GetComponent<MeshRenderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                    return renderer.sharedMaterial;
            }
        }

        Transform smoothRoad = roadRoot.Find(SmoothRoadName);
        if (smoothRoad != null)
        {
            MeshRenderer smoothRenderer = smoothRoad.GetComponent<MeshRenderer>();
            if (smoothRenderer != null && smoothRenderer.sharedMaterial != null)
                return smoothRenderer.sharedMaterial;
        }

        GameObject template = GameObject.Find("Road_000");
        return template != null ? template.GetComponent<MeshRenderer>()?.sharedMaterial : null;
    }

    private static Material GetTemplateMaterial(string objectName, Material fallback)
    {
        GameObject template = GameObject.Find(objectName);
        MeshRenderer renderer = template != null ? template.GetComponent<MeshRenderer>() : null;
        return renderer != null && renderer.sharedMaterial != null ? renderer.sharedMaterial : fallback;
    }

    private static List<Vector3> DownsamplePoints(List<Vector3> points, int stride)
    {
        List<Vector3> result = new List<Vector3>();
        for (int i = 0; i < points.Count; i += Mathf.Max(1, stride))
            result.Add(points[i]);

        if (result.Count == 0 || result[result.Count - 1] != points[points.Count - 1])
            result.Add(points[points.Count - 1]);

        return result;
    }

    private static void ClearGeneratedChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
    }

    private static void CreateSegmentCube(Transform parent, string name, Vector3 position, Quaternion rotation, Vector3 scale, Material material, bool enableCollider)
    {
        GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        segment.name = name;
        segment.transform.SetParent(parent, true);
        segment.transform.position = position;
        segment.transform.rotation = rotation;
        segment.transform.localScale = scale;
        segment.isStatic = true;

        MeshRenderer renderer = segment.GetComponent<MeshRenderer>();
        if (renderer != null && material != null)
            renderer.sharedMaterial = material;

        Collider collider = segment.GetComponent<Collider>();
        if (collider != null)
            collider.enabled = enableCollider;
    }

    private static GameObject FindOrCreateChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing.gameObject;

        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go;
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        T existing = go.GetComponent<T>();
        if (existing != null)
            return existing;

        T added = go.AddComponent<T>();
        Undo.RegisterCreatedObjectUndo(added, $"Add {typeof(T).Name}");
        return added;
    }

    private static List<Vector3> SampleCatmullRom(List<Vector3> controlPoints, float sampleStep)
    {
        List<Vector3> sampled = new List<Vector3>();
        if (controlPoints.Count < 2)
            return sampled;

        sampled.Add(controlPoints[0]);
        for (int i = 0; i < controlPoints.Count - 1; i++)
        {
            Vector3 p0 = controlPoints[Mathf.Max(0, i - 1)];
            Vector3 p1 = controlPoints[i];
            Vector3 p2 = controlPoints[i + 1];
            Vector3 p3 = controlPoints[Mathf.Min(controlPoints.Count - 1, i + 2)];

            float segmentLength = Vector3.Distance(p1, p2);
            int steps = Mathf.Max(1, Mathf.CeilToInt(segmentLength / Mathf.Max(0.25f, sampleStep)));
            for (int step = 1; step <= steps; step++)
            {
                float t = step / (float)steps;
                Vector3 point = CatmullRom(p0, p1, p2, p3, t);
                if (Vector3.Distance(sampled[sampled.Count - 1], point) > 0.2f || step == steps)
                    sampled.Add(point);
            }
        }

        return sampled;
    }

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    private static Mesh BuildStripMesh(List<Vector3> points, float width, float thickness, float lift, float uvTileLength)
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        float halfWidth = width * 0.5f;
        float halfHeight = thickness * 0.5f;
        float v0 = 0f;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 p0 = points[i];
            Vector3 p1 = points[i + 1];
            float segmentLength = Vector3.Distance(p0, p1);
            if (segmentLength < 0.001f)
                continue;

            float v1 = v0 + (segmentLength / Mathf.Max(0.01f, uvTileLength));

            Vector3 right0 = ComputeRight(points, i);
            Vector3 right1 = ComputeRight(points, i + 1);
            Vector3 c0 = p0 + (Vector3.up * lift);
            Vector3 c1 = p1 + (Vector3.up * lift);

            Vector3 tl0 = c0 - (right0 * halfWidth) + (Vector3.up * halfHeight);
            Vector3 tr0 = c0 + (right0 * halfWidth) + (Vector3.up * halfHeight);
            Vector3 bl0 = c0 - (right0 * halfWidth) - (Vector3.up * halfHeight);
            Vector3 br0 = c0 + (right0 * halfWidth) - (Vector3.up * halfHeight);
            Vector3 tl1 = c1 - (right1 * halfWidth) + (Vector3.up * halfHeight);
            Vector3 tr1 = c1 + (right1 * halfWidth) + (Vector3.up * halfHeight);
            Vector3 bl1 = c1 - (right1 * halfWidth) - (Vector3.up * halfHeight);
            Vector3 br1 = c1 + (right1 * halfWidth) - (Vector3.up * halfHeight);

            AddQuad(vertices, uvs, triangles, tl0, tl1, tr1, tr0, new Vector2(0f, v0), new Vector2(0f, v1), new Vector2(1f, v1), new Vector2(1f, v0));
            AddQuad(vertices, uvs, triangles, bl0, br0, br1, bl1, new Vector2(0f, v0), new Vector2(1f, v0), new Vector2(1f, v1), new Vector2(0f, v1));
            AddQuad(vertices, uvs, triangles, bl0, bl1, tl1, tl0, new Vector2(0f, v0), new Vector2(0f, v1), new Vector2(1f, v1), new Vector2(1f, v0));
            AddQuad(vertices, uvs, triangles, br0, tr0, tr1, br1, new Vector2(0f, v0), new Vector2(1f, v0), new Vector2(1f, v1), new Vector2(0f, v1));

            if (i == 0)
                AddQuad(vertices, uvs, triangles, bl0, tl0, tr0, br0, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f));

            if (i == points.Count - 2)
                AddQuad(vertices, uvs, triangles, bl1, br1, tr1, tl1, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f));

            v0 = v1;
        }

        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh BuildEdgeMesh(
        List<Vector3> points,
        float roadWidth,
        bool leftSide,
        float inset,
        float thickness,
        float height,
        float baseDrop,
        float topLift,
        float uvTileLength)
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        float halfRoadWidth = roadWidth * 0.5f;
        float v0 = 0f;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 p0 = points[i];
            Vector3 p1 = points[i + 1];
            float segmentLength = Vector3.Distance(p0, p1);
            if (segmentLength < 0.001f)
                continue;

            float v1 = v0 + (segmentLength / Mathf.Max(0.01f, uvTileLength));
            Vector3 right0 = ComputeRight(points, i);
            Vector3 right1 = ComputeRight(points, i + 1);

            Vector3 side0 = leftSide ? -right0 : right0;
            Vector3 side1 = leftSide ? -right1 : right1;
            Vector3 c0 = p0 + (Vector3.up * SurfaceLift);
            Vector3 c1 = p1 + (Vector3.up * SurfaceLift);

            Vector3 innerTop0 = c0 + (side0 * (halfRoadWidth - inset)) + (Vector3.up * topLift);
            Vector3 outerTop0 = c0 + (side0 * (halfRoadWidth + thickness)) + (Vector3.up * topLift);
            Vector3 innerBottom0 = c0 + (side0 * (halfRoadWidth - inset)) - (Vector3.up * baseDrop);
            Vector3 outerBottom0 = c0 + (side0 * (halfRoadWidth + thickness)) - (Vector3.up * (baseDrop + height));

            Vector3 innerTop1 = c1 + (side1 * (halfRoadWidth - inset)) + (Vector3.up * topLift);
            Vector3 outerTop1 = c1 + (side1 * (halfRoadWidth + thickness)) + (Vector3.up * topLift);
            Vector3 innerBottom1 = c1 + (side1 * (halfRoadWidth - inset)) - (Vector3.up * baseDrop);
            Vector3 outerBottom1 = c1 + (side1 * (halfRoadWidth + thickness)) - (Vector3.up * (baseDrop + height));

            AddQuad(vertices, uvs, triangles, innerTop0, innerTop1, outerTop1, outerTop0, new Vector2(0f, v0), new Vector2(0f, v1), new Vector2(1f, v1), new Vector2(1f, v0));
            AddQuad(vertices, uvs, triangles, outerBottom0, outerBottom1, innerBottom1, innerBottom0, new Vector2(0f, v0), new Vector2(0f, v1), new Vector2(1f, v1), new Vector2(1f, v0));
            AddQuad(vertices, uvs, triangles, innerBottom0, innerBottom1, innerTop1, innerTop0, new Vector2(0f, v0), new Vector2(0f, v1), new Vector2(1f, v1), new Vector2(1f, v0));
            AddQuad(vertices, uvs, triangles, outerTop0, outerTop1, outerBottom1, outerBottom0, new Vector2(0f, v0), new Vector2(0f, v1), new Vector2(1f, v1), new Vector2(1f, v0));

            if (i == 0)
                AddQuad(vertices, uvs, triangles, innerBottom0, innerTop0, outerTop0, outerBottom0, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f));

            if (i == points.Count - 2)
                AddQuad(vertices, uvs, triangles, innerBottom1, outerBottom1, outerTop1, innerTop1, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f));

            v0 = v1;
        }

        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Vector3 CenterMeshPivot(Mesh mesh)
    {
        Vector3 offset = mesh.bounds.center;
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
            vertices[i] -= offset;

        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return offset;
    }

    private static Vector3 ComputeRight(List<Vector3> points, int index)
    {
        Vector3 tangent;
        if (index <= 0)
            tangent = points[1] - points[0];
        else if (index >= points.Count - 1)
            tangent = points[points.Count - 1] - points[points.Count - 2];
        else
            tangent = points[index + 1] - points[index - 1];

        tangent = Vector3.ProjectOnPlane(tangent, Vector3.up);
        if (tangent.sqrMagnitude < 0.0001f)
            tangent = Vector3.forward;

        tangent.Normalize();
        return Vector3.Cross(Vector3.up, tangent).normalized;
    }

    private static void AddQuad(
        List<Vector3> vertices,
        List<Vector2> uvs,
        List<int> triangles,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 d,
        Vector2 uvA,
        Vector2 uvB,
        Vector2 uvC,
        Vector2 uvD)
    {
        int start = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        vertices.Add(d);

        uvs.Add(uvA);
        uvs.Add(uvB);
        uvs.Add(uvC);
        uvs.Add(uvD);

        triangles.Add(start);
        triangles.Add(start + 1);
        triangles.Add(start + 2);
        triangles.Add(start);
        triangles.Add(start + 2);
        triangles.Add(start + 3);
    }
}
#endif
