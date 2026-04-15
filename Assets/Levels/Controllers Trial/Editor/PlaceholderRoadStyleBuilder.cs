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

    private const float SegmentMinLength = 1.6f;
    private const float SegmentMaxLength = 3.4f;
    private const float RoadWidth = 9.6f;
    private const float RoadHeight = 0.42f;
    private const float BaseWidth = 10.176f;
    private const float BaseHeight = 0.6f;
    private const float BaseYOffset = -0.158f;
    private const float BaseLengthPadding = 0.08f;
    private const float EdgeWidth = 0.34f;
    private const float EdgeHeight = 0.46f;
    private const float EdgeLateralOffset = 4.595f;
    private const float EdgeYOffset = 0.236f;
    private const float SegmentOverlap = 0.18f;

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

        BuildSegments(splinePoints, roadRoot, baseRoot, edgesRoot,
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

    private static void BuildSegments(
        List<Vector3> splinePoints,
        Transform roadRoot,
        Transform baseRoot,
        Transform edgesRoot,
        Material roadMaterial,
        Material baseMaterial,
        Material edgeMaterial)
    {
        List<Vector3> sampled = ResamplePath(splinePoints, SegmentMinLength, SegmentMaxLength);

        for (int i = 0; i < sampled.Count - 1; i++)
        {
            Vector3 start = sampled[i];
            Vector3 end = sampled[i + 1];
            Vector3 direction = end - start;
            float length = direction.magnitude;
            if (length < 0.05f)
                continue;

            Vector3 forward = direction / length;
            Vector3 up = Vector3.up;
            Vector3 right = Vector3.Cross(up, forward).normalized;
            Quaternion rotation = Quaternion.LookRotation(forward, up);
            Vector3 center = (start + end) * 0.5f;

            CreateCubeSegment(
                $"Road_Placeholder_{i:000}",
                roadRoot,
                center,
                rotation,
                new Vector3(RoadWidth, RoadHeight, length + SegmentOverlap),
                roadMaterial,
                true);

            CreateCubeSegment(
                $"Base_Road_Placeholder_{i:000}",
                baseRoot,
                center + (up * BaseYOffset),
                rotation,
                new Vector3(BaseWidth, BaseHeight, length + BaseLengthPadding),
                baseMaterial,
                true);

            CreateCubeSegment(
                $"Edge_Road_Placeholder_{i:000}_L",
                edgesRoot,
                center - (right * EdgeLateralOffset) + (up * EdgeYOffset),
                rotation,
                new Vector3(EdgeWidth, EdgeHeight, length),
                edgeMaterial,
                true);

            CreateCubeSegment(
                $"Edge_Road_Placeholder_{i:000}_R",
                edgesRoot,
                center + (right * EdgeLateralOffset) + (up * EdgeYOffset),
                rotation,
                new Vector3(EdgeWidth, EdgeHeight, length),
                edgeMaterial,
                true);
        }
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

    private static GameObject CreateCubeSegment(
        string name,
        Transform parent,
        Vector3 position,
        Quaternion rotation,
        Vector3 scale,
        Material material,
        bool castShadows)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.position = position;
        go.transform.rotation = rotation;
        go.transform.localScale = scale;
        go.isStatic = true;

        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        renderer.receiveShadows = true;

        return go;
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
