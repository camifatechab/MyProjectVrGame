using UnityEditor;
using UnityEngine;

public static class FlattenRoverFinishArea
{
    private const float LowFlatWorldHeight = 2.6f;
    private const float RoadPreservePadding = 7.5f;
    private const float RoadBlendPadding = 11f;
    private const float RoadEndBlendPadding = 3f;

    // Terrain-local coordinates relative to TerrainSurface (not world space).
    private static readonly Vector2 Center = new(8f, 445f);
    private static readonly Vector2 InnerSize = new(120f, 78f);
    private static readonly Vector2 OuterSize = new(168f, 108f);
    private static readonly Vector2 EastRidgeCenter = new(515f, 545f);
    private static readonly Vector2 EastRidgeInnerSize = new(140f, 96f);
    private static readonly Vector2 EastRidgeOuterSize = new(220f, 150f);
    private static readonly Vector2 EastRidgeReferencePoint = new(428f, 535f);
    private static readonly Vector2 BrownPatchCenter = new(340f, 180f);
    private static readonly Vector2 BrownPatchInnerSize = new(470f, 210f);
    private static readonly Vector2 BrownPatchOuterSize = new(560f, 270f);

    [MenuItem("Tools/Terrain/Flatten Rover Finish Area")]
    public static void FlattenArea()
    {
        FlattenPatch(
            "Flatten Rover Finish Area",
            "Flatten Rover Finish Area: flattened the finish-side plateau patch.",
            Center,
            InnerSize,
            OuterSize,
            null);
    }

    [MenuItem("Tools/Terrain/Flatten Rover East Ridge")]
    public static void FlattenEastRidge()
    {
        FlattenPatch(
            "Flatten Rover East Ridge",
            "Flatten Rover East Ridge: flattened the east-side ridge patch.",
            EastRidgeCenter,
            EastRidgeInnerSize,
            EastRidgeOuterSize,
            EastRidgeReferencePoint);
    }

    [MenuItem("Tools/Terrain/Low Flat Outside Rover Road")]
    public static void FlattenOutsideRoadLow()
    {
        GameObject courseObject = GameObject.Find("TerrainRoverCourse");
        GameObject terrainObject = GameObject.Find("TerrainRoverCourse/TerrainSurface");
        Transform roadRoot = GameObject.Find("TerrainRoverCourse/RoverRoad")?.transform;

        if (courseObject == null || terrainObject == null || roadRoot == null)
        {
            Debug.LogError("Low Flat Outside Rover Road: missing TerrainRoverCourse, TerrainSurface, or RoverRoad.");
            return;
        }

        Terrain terrain = terrainObject.GetComponent<Terrain>();
        TerrainData terrainData = terrain != null ? terrain.terrainData : null;
        if (terrainData == null)
        {
            Debug.LogError("Low Flat Outside Rover Road: target terrain has no TerrainData.");
            return;
        }

        Undo.RegisterCompleteObjectUndo(terrainData, "Low Flat Outside Rover Road");

        int resolution = terrainData.heightmapResolution;
        float[,] originalHeights = terrainData.GetHeights(0, 0, resolution, resolution);
        float[,] targetHeights = new float[resolution, resolution];
        float[,] roadWeights = new float[resolution, resolution];
        float flatHeight01 = LowFlatWorldHeight / terrainData.size.y;

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                targetHeights[z, x] = flatHeight01;
            }
        }

        Vector3 terrainOffset = terrainObject.transform.localPosition;
        foreach (Transform child in roadRoot)
        {
            if (!child.name.StartsWith("Road_"))
            {
                continue;
            }

            StampRoadSegment(
                child,
                terrainData,
                terrainOffset,
                originalHeights,
                targetHeights,
                roadWeights);
        }

        terrainData.SetHeights(0, 0, targetHeights);
        EditorUtility.SetDirty(terrainData);
        EditorUtility.SetDirty(terrain);
        EditorUtility.SetDirty(courseObject);

        Debug.Log("Low Flat Outside Rover Road: flattened the terrain to a low plane while preserving the road corridor elevation.");
    }

    [MenuItem("Tools/Terrain/Remove Rover Brown Patch")]
    public static void RemoveBrownPatch()
    {
        GameObject terrainObject = GameObject.Find("TerrainRoverCourse/TerrainSurface");
        if (terrainObject == null)
        {
            Debug.LogError("Remove Rover Brown Patch: could not find 'TerrainRoverCourse/TerrainSurface'.");
            return;
        }

        Terrain terrain = terrainObject.GetComponent<Terrain>();
        TerrainData terrainData = terrain != null ? terrain.terrainData : null;
        if (terrainData == null || terrainData.terrainLayers == null || terrainData.terrainLayers.Length < 2)
        {
            Debug.LogError("Remove Rover Brown Patch: target terrain is missing the expected green/brown terrain layers.");
            return;
        }

        Undo.RegisterCompleteObjectUndo(terrainData, "Remove Rover Brown Patch");

        int width = terrainData.alphamapWidth;
        int height = terrainData.alphamapHeight;
        int layers = terrainData.terrainLayers.Length;
        float[,,] maps = terrainData.GetAlphamaps(0, 0, width, height);

        for (int z = 0; z < height; z++)
        {
            float localZ = z / (float)(height - 1) * terrainData.size.z;
            float dzInner = Mathf.Abs(localZ - BrownPatchCenter.y) / Mathf.Max(0.01f, BrownPatchInnerSize.y * 0.5f);
            float dzOuter = Mathf.Abs(localZ - BrownPatchCenter.y) / Mathf.Max(0.01f, BrownPatchOuterSize.y * 0.5f);

            for (int x = 0; x < width; x++)
            {
                float localX = x / (float)(width - 1) * terrainData.size.x;
                float dxInner = Mathf.Abs(localX - BrownPatchCenter.x) / Mathf.Max(0.01f, BrownPatchInnerSize.x * 0.5f);
                float dxOuter = Mathf.Abs(localX - BrownPatchCenter.x) / Mathf.Max(0.01f, BrownPatchOuterSize.x * 0.5f);

                float innerEllipse = dxInner * dxInner + dzInner * dzInner;
                float weight;
                if (innerEllipse <= 1f)
                {
                    weight = 1f;
                }
                else
                {
                    float outerEllipse = dxOuter * dxOuter + dzOuter * dzOuter;
                    if (outerEllipse > 1f)
                    {
                        continue;
                    }

                    weight = 1f - Mathf.Clamp01(outerEllipse);
                    weight = weight * weight * (3f - 2f * weight);
                }

                float[] blended = new float[layers];
                float total = 0f;
                for (int layer = 0; layer < layers; layer++)
                {
                    blended[layer] = maps[z, x, layer];
                }

                blended[0] = Mathf.Lerp(blended[0], 1f, weight);
                blended[1] = Mathf.Lerp(blended[1], 0f, weight);

                for (int layer = 2; layer < layers; layer++)
                {
                    blended[layer] = Mathf.Lerp(blended[layer], 0f, weight);
                }

                for (int layer = 0; layer < layers; layer++)
                {
                    total += blended[layer];
                }

                if (total <= 0.0001f)
                {
                    blended[0] = 1f;
                    total = 1f;
                }

                for (int layer = 0; layer < layers; layer++)
                {
                    maps[z, x, layer] = blended[layer] / total;
                }
            }
        }

        terrainData.SetAlphamaps(0, 0, maps);
        EditorUtility.SetDirty(terrainData);
        EditorUtility.SetDirty(terrain);

        Debug.Log("Remove Rover Brown Patch: repainted the isolated brown patch back to the green terrain layer.");
    }

    private static void FlattenPatch(
        string undoLabel,
        string successMessage,
        Vector2 center,
        Vector2 innerSize,
        Vector2 outerSize,
        Vector2? referencePoint)
    {
        GameObject terrainObject = GameObject.Find("TerrainRoverCourse/TerrainSurface");
        if (terrainObject == null)
        {
            Debug.LogError(undoLabel + ": could not find 'TerrainRoverCourse/TerrainSurface'.");
            return;
        }

        Terrain terrain = terrainObject.GetComponent<Terrain>();
        TerrainData terrainData = terrain != null ? terrain.terrainData : null;
        if (terrainData == null)
        {
            Debug.LogError(undoLabel + ": target terrain has no TerrainData.");
            return;
        }

        Undo.RegisterCompleteObjectUndo(terrainData, undoLabel);

        int resolution = terrainData.heightmapResolution;
        float[,] heights = terrainData.GetHeights(0, 0, resolution, resolution);

        Vector2 targetPoint = referencePoint ?? center;
        float targetX01 = Mathf.Clamp01(targetPoint.x / terrainData.size.x);
        float targetZ01 = Mathf.Clamp01(targetPoint.y / terrainData.size.z);
        float targetHeight = SampleAverageHeight(terrainData, targetX01, targetZ01, 0.04f);

        for (int z = 0; z < resolution; z++)
        {
            float localZ = z / (float)(resolution - 1) * terrainData.size.z;
            float dzInner = Mathf.Abs(localZ - center.y) / Mathf.Max(0.01f, innerSize.y * 0.5f);
            float dzOuter = Mathf.Abs(localZ - center.y) / Mathf.Max(0.01f, outerSize.y * 0.5f);

            for (int x = 0; x < resolution; x++)
            {
                float localX = x / (float)(resolution - 1) * terrainData.size.x;
                float dxInner = Mathf.Abs(localX - center.x) / Mathf.Max(0.01f, innerSize.x * 0.5f);
                float dxOuter = Mathf.Abs(localX - center.x) / Mathf.Max(0.01f, outerSize.x * 0.5f);

                float innerEllipse = dxInner * dxInner + dzInner * dzInner;
                if (innerEllipse <= 1f)
                {
                    heights[z, x] = targetHeight;
                    continue;
                }

                float outerEllipse = dxOuter * dxOuter + dzOuter * dzOuter;
                if (outerEllipse <= 1f)
                {
                    float blend = 1f - Mathf.Clamp01(outerEllipse);
                    blend = blend * blend * (3f - 2f * blend);
                    heights[z, x] = Mathf.Lerp(heights[z, x], targetHeight, blend * 0.78f);
                }
            }
        }

        terrainData.SetHeights(0, 0, heights);
        EditorUtility.SetDirty(terrainData);
        EditorUtility.SetDirty(terrain);

        Debug.Log(successMessage);
    }

    private static float SampleAverageHeight(TerrainData terrainData, float centerX01, float centerZ01, float radius01)
    {
        const int samples = 5;
        float total = 0f;
        int count = 0;

        for (int z = 0; z < samples; z++)
        {
            for (int x = 0; x < samples; x++)
            {
                float offsetX = Mathf.Lerp(-radius01, radius01, x / (float)(samples - 1));
                float offsetZ = Mathf.Lerp(-radius01, radius01, z / (float)(samples - 1));
                total += terrainData.GetInterpolatedHeight(
                    Mathf.Clamp01(centerX01 + offsetX),
                    Mathf.Clamp01(centerZ01 + offsetZ)) / terrainData.size.y;
                count++;
            }
        }

        return count > 0 ? total / count : 0f;
    }

    private static void StampRoadSegment(
        Transform roadSegment,
        TerrainData terrainData,
        Vector3 terrainOffset,
        float[,] originalHeights,
        float[,] targetHeights,
        float[,] roadWeights)
    {
        int resolution = terrainData.heightmapResolution;
        Vector3 center = roadSegment.localPosition - terrainOffset;
        float yaw = roadSegment.localEulerAngles.y * Mathf.Deg2Rad;
        Vector2 forward = new(Mathf.Sin(yaw), Mathf.Cos(yaw));
        Vector2 right = new(forward.y, -forward.x);

        float halfWidth = roadSegment.localScale.x * 0.5f + RoadPreservePadding;
        float outerHalfWidth = halfWidth + RoadBlendPadding;
        float halfLength = roadSegment.localScale.z * 0.5f;
        float outerHalfLength = halfLength + RoadEndBlendPadding;

        float boundX = Mathf.Abs(right.x) * outerHalfWidth + Mathf.Abs(forward.x) * outerHalfLength;
        float boundZ = Mathf.Abs(right.y) * outerHalfWidth + Mathf.Abs(forward.y) * outerHalfLength;

        int minX = Mathf.Clamp(Mathf.FloorToInt(((center.x - boundX) / terrainData.size.x) * (resolution - 1)), 0, resolution - 1);
        int maxX = Mathf.Clamp(Mathf.CeilToInt(((center.x + boundX) / terrainData.size.x) * (resolution - 1)), 0, resolution - 1);
        int minZ = Mathf.Clamp(Mathf.FloorToInt(((center.z - boundZ) / terrainData.size.z) * (resolution - 1)), 0, resolution - 1);
        int maxZ = Mathf.Clamp(Mathf.CeilToInt(((center.z + boundZ) / terrainData.size.z) * (resolution - 1)), 0, resolution - 1);

        for (int z = minZ; z <= maxZ; z++)
        {
            float localZ = z / (float)(resolution - 1) * terrainData.size.z;
            for (int x = minX; x <= maxX; x++)
            {
                float localX = x / (float)(resolution - 1) * terrainData.size.x;
                Vector2 delta = new(localX - center.x, localZ - center.z);
                float lateral = Mathf.Abs(Vector2.Dot(delta, right));
                float longitudinal = Mathf.Abs(Vector2.Dot(delta, forward));

                if (lateral > outerHalfWidth || longitudinal > outerHalfLength)
                {
                    continue;
                }

                float widthWeight = lateral <= halfWidth
                    ? 1f
                    : 1f - Mathf.InverseLerp(halfWidth, outerHalfWidth, lateral);
                float lengthWeight = longitudinal <= halfLength
                    ? 1f
                    : 1f - Mathf.InverseLerp(halfLength, outerHalfLength, longitudinal);
                float weight = Mathf.Clamp01(Mathf.Min(widthWeight, lengthWeight));
                weight = weight * weight * (3f - 2f * weight);

                if (weight <= roadWeights[z, x])
                {
                    continue;
                }

                roadWeights[z, x] = weight;
                targetHeights[z, x] = Mathf.Lerp(LowFlatWorldHeight / terrainData.size.y, originalHeights[z, x], weight);
            }
        }
    }
}
