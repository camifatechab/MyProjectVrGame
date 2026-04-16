#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

internal static class PlaceholderRoadStyleBuilder
{
    private const string PlaceholderRoadPath = "TerrainRoverCourse/RoverRoad/Road_PlaceholderReference";
    private const string RoverRoadParentPath = "TerrainRoverCourse/RoverRoad";
    private const string RoverBaseParentPath = "TerrainRoverCourse/RoverRoad_Base";
    private const string RoverEdgesParentPath = "TerrainRoverCourse/RoverRoad_Base";

    private const string RoadTemplateName = "Road_395";
    private const string BaseTemplateName = "Base_Road_395";
    private const string EdgeTemplateName = "Edge_Road_395_L";

    private const float VisualSegmentMinLength = 0.45f;
    private const float VisualSegmentMaxLength = 0.9f;
    private const float ColliderSegmentMinLength = 1.2f;
    private const float ColliderSegmentMaxLength = 2.2f;
    private const float RoadWidth = 9.6f;
    private const float RoadHeight = 0.42f;
    private const float BaseWidth = 10.176f;
    private const float BaseHeight = 0.6f;
    private const float BaseYOffset = -0.158f;
    private const float EdgeWidth = 0.34f;
    private const float EdgeHeight = 0.46f;
    private const float EdgeLateralOffset = 4.595f;
    private const float EdgeYOffset = 0.236f;
    private const float SegmentOverlap = 0.18f;
    private const float RoadUvTileLength = 4f;
    private const float BaseUvTileLength = 4f;
    private const float EdgeUvTileLength = 2f;

    [MenuItem("Tools/Placeholder Road/Build Rover Style")]
    private static void BuildRoverStyle()
    {
        GameObject placeholderRoad = GameObject.Find(PlaceholderRoadPath);
        GameObject roverRoadParent = GameObject.Find(RoverRoadParentPath);
        GameObject roverBaseParent = GameObject.Find(RoverBaseParentPath);
        GameObject roverEdgesParent = GameObject.Find(RoverEdgesParentPath);

        if (placeholderRoad == null || roverRoadParent == null || roverBaseParent == null || roverEdgesParent == null)
        {
            Debug.LogError("[PlaceholderRoadStyleBuilder] Required scene objects are missing.");
            return;
        }

        MeshRenderer roadTemplateRenderer = FindTemplateRenderer(RoadTemplateName);
        MeshRenderer baseTemplateRenderer = FindTemplateRenderer(BaseTemplateName);
        MeshRenderer edgeTemplateRenderer = FindTemplateRenderer(EdgeTemplateName);

        if (roadTemplateRenderer == null || baseTemplateRenderer == null || edgeTemplateRenderer == null)
        {
            Debug.LogError("[PlaceholderRoadStyleBuilder] Could not find rover road template renderers.");
            return;
        }

        List<Vector3> splinePoints = ExtractSplinePoints(placeholderRoad);
        if (splinePoints.Count < 2)
        {
            Debug.LogError("[PlaceholderRoadStyleBuilder] Could not extract spline points from Road_PlaceholderReference.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        Transform roadRoot = CreateOrResetContainer("Road_PlaceholderStyled", roverRoadParent.transform);
        Transform baseRoot = CreateOrResetContainer("Base_Road_PlaceholderStyled", roverBaseParent.transform);
        Transform edgesRoot = CreateOrResetContainer("Edges_Road_PlaceholderStyled", roverEdgesParent.transform);

        BuildStyledMeshes(splinePoints, roadRoot, baseRoot, edgesRoot,
            roadTemplateRenderer.sharedMaterial,
            baseTemplateRenderer.sharedMaterial,
            edgeTemplateRenderer.sharedMaterial);

        HidePlaceholderVisuals(placeholderRoad);

        EditorSceneManager.MarkSceneDirty(placeholderRoad.scene);
        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log($"[PlaceholderRoadStyleBuilder] Built rover-style road, base, and edges using {splinePoints.Count} path points.");
    }

    private static MeshRenderer FindTemplateRenderer(string gameObjectName)
    {
        GameObject go = GameObject.Find(gameObjectName);
        return go != null ? go.GetComponent<MeshRenderer>() : null;
    }

    private static List<Vector3> ExtractSplinePoints(GameObject placeholderRoad)
    {
        List<Vector3> points = new List<Vector3>();
        Component roadComponent = placeholderRoad.GetComponent("ERModularRoad");
        if (roadComponent == null)
            return points;

        FieldInfo field = roadComponent.GetType().GetField("splinePoints", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null)
            return points;

        object value = field.GetValue(roadComponent);
        if (value is IEnumerable enumerable)
        {
            foreach (object entry in enumerable)
            {
                if (entry is Vector3 point)
                    points.Add(point);
            }
        }

        return points;
    }

    private static Transform CreateOrResetContainer(string name, Transform parent)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            for (int i = existing.childCount - 1; i >= 0; i--)
                Undo.DestroyObjectImmediate(existing.GetChild(i).gameObject);
            return existing;
        }

        GameObject container = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(container, $"Create {name}");
        container.transform.SetParent(parent, false);
        container.transform.localPosition = Vector3.zero;
        container.transform.localRotation = Quaternion.identity;
        container.transform.localScale = Vector3.one;
        return container.transform;
    }

    private static void BuildStyledMeshes(
        List<Vector3> splinePoints,
        Transform roadRoot,
        Transform baseRoot,
        Transform edgesRoot,
        Material roadMaterial,
        Material baseMaterial,
        Material edgeMaterial)
    {
        List<Vector3> visualPoints = ResamplePath(splinePoints, VisualSegmentMinLength, VisualSegmentMaxLength);
        List<Vector3> colliderPoints = ResamplePath(splinePoints, ColliderSegmentMinLength, ColliderSegmentMaxLength);

        CreateOrUpdateStripObject("Road_PlaceholderSurface", roadRoot, visualPoints, RoadWidth, RoadHeight, 0f, 0f, roadMaterial, RoadUvTileLength);
        CreateOrUpdateStripObject("Base_Road_PlaceholderSurface", baseRoot, visualPoints, BaseWidth, BaseHeight, BaseYOffset, 0f, baseMaterial, BaseUvTileLength);
        CreateOrUpdateStripObject("Edge_Road_PlaceholderSurface_L", edgesRoot, visualPoints, EdgeWidth, EdgeHeight, EdgeYOffset, -EdgeLateralOffset, edgeMaterial, EdgeUvTileLength);
        CreateOrUpdateStripObject("Edge_Road_PlaceholderSurface_R", edgesRoot, visualPoints, EdgeWidth, EdgeHeight, EdgeYOffset, EdgeLateralOffset, edgeMaterial, EdgeUvTileLength);

        Transform roadColliderRoot = CreateOrResetContainer("Road_PlaceholderColliders", roadRoot);
        Transform baseColliderRoot = CreateOrResetContainer("Base_Road_PlaceholderColliders", baseRoot);
        Transform edgeColliderRootL = CreateOrResetContainer("Edge_Road_PlaceholderColliders_L", edgesRoot);
        Transform edgeColliderRootR = CreateOrResetContainer("Edge_Road_PlaceholderColliders_R", edgesRoot);

        BuildColliderStrip(roadColliderRoot, colliderPoints, RoadWidth, RoadHeight, 0f, 0f);
        BuildColliderStrip(baseColliderRoot, colliderPoints, BaseWidth, BaseHeight, BaseYOffset, 0f);
        BuildColliderStrip(edgeColliderRootL, colliderPoints, EdgeWidth, EdgeHeight, EdgeYOffset, -EdgeLateralOffset);
        BuildColliderStrip(edgeColliderRootR, colliderPoints, EdgeWidth, EdgeHeight, EdgeYOffset, EdgeLateralOffset);
    }

    private static List<Vector3> ResamplePath(List<Vector3> splinePoints, float minLength, float maxLength)
    {
        List<Vector3> result = new List<Vector3>();
        if (splinePoints.Count == 0)
            return result;

        result.Add(splinePoints[0]);
        for (int i = 1; i < splinePoints.Count; i++)
        {
            Vector3 start = splinePoints[i - 1];
            Vector3 end = splinePoints[i];
            float distance = Vector3.Distance(start, end);
            if (distance < 0.001f)
                continue;

            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / maxLength));
            float stepT = 1f / steps;
            for (int s = 1; s <= steps; s++)
            {
                Vector3 point = Vector3.Lerp(start, end, s * stepT);
                if (Vector3.Distance(result[result.Count - 1], point) >= minLength * 0.5f || s == steps)
                    result.Add(point);
            }
        }

        return result;
    }

    private static void CreateOrUpdateStripObject(
        string name,
        Transform parent,
        List<Vector3> points,
        float width,
        float height,
        float verticalOffset,
        float lateralOffset,
        Material material,
        float uvTileLength)
    {
        Transform existing = parent.Find(name);
        GameObject go;
        MeshFilter meshFilter;
        MeshRenderer renderer;

        if (existing == null)
        {
            go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            go.transform.SetParent(parent, false);
            meshFilter = go.AddComponent<MeshFilter>();
            renderer = go.AddComponent<MeshRenderer>();
        }
        else
        {
            go = existing.gameObject;
            meshFilter = go.GetComponent<MeshFilter>() ?? go.AddComponent<MeshFilter>();
            renderer = go.GetComponent<MeshRenderer>() ?? go.AddComponent<MeshRenderer>();
        }

        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        go.isStatic = true;

        Mesh mesh = BuildStripMesh(points, width, height, verticalOffset, lateralOffset, uvTileLength);
        mesh.name = $"{name}_Mesh";
        meshFilter.sharedMesh = mesh;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.On;
        renderer.receiveShadows = true;
    }

    private static void BuildColliderStrip(
        Transform parent,
        List<Vector3> points,
        float width,
        float height,
        float verticalOffset,
        float lateralOffset)
    {
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 start = points[i];
            Vector3 end = points[i + 1];
            Vector3 forward = end - start;
            float length = forward.magnitude;
            if (length < 0.05f)
                continue;

            forward /= length;
            Vector3 right = ComputeRight(points, i);
            Vector3 center = (start + end) * 0.5f + (Vector3.up * verticalOffset) + (right * lateralOffset);
            Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);

            GameObject colliderGo = new GameObject($"Collider_{i:000}");
            Undo.RegisterCreatedObjectUndo(colliderGo, "Create placeholder collider");
            colliderGo.transform.SetParent(parent, true);
            colliderGo.transform.position = center;
            colliderGo.transform.rotation = rotation;
            colliderGo.transform.localScale = Vector3.one;
            colliderGo.isStatic = true;

            BoxCollider collider = colliderGo.AddComponent<BoxCollider>();
            collider.size = new Vector3(width, height, length + SegmentOverlap);
        }
    }

    private static Mesh BuildStripMesh(
        List<Vector3> points,
        float width,
        float height,
        float verticalOffset,
        float lateralOffset,
        float uvTileLength)
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = points.Count > 1000 ? IndexFormat.UInt32 : IndexFormat.UInt16;

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        float halfWidth = width * 0.5f;
        float halfHeight = height * 0.5f;
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

            Vector3 c0 = p0 + (Vector3.up * verticalOffset) + (right0 * lateralOffset);
            Vector3 c1 = p1 + (Vector3.up * verticalOffset) + (right1 * lateralOffset);

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

    private static Vector3 ComputeRight(List<Vector3> points, int index)
    {
        Vector3 tangent;
        if (index <= 0)
            tangent = points[1] - points[0];
        else if (index >= points.Count - 1)
            tangent = points[points.Count - 1] - points[points.Count - 2];
        else
            tangent = points[index + 1] - points[index - 1];

        tangent.y = 0f;
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

    private static void HidePlaceholderVisuals(GameObject placeholderRoad)
    {
        MeshRenderer[] renderers = placeholderRoad.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Undo.RecordObject(renderers[i], "Hide placeholder renderer");
            renderers[i].enabled = false;
        }

        Collider[] colliders = placeholderRoad.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Undo.RecordObject(colliders[i], "Disable placeholder collider");
            colliders[i].enabled = false;
        }
    }
}
#endif
