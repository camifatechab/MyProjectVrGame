#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-shot editor utility to raise an existing Unity Terrain so it forms a narrow road
/// bed underneath an EasyRoads3D-style placeholder road mesh.
///
/// Constraints honored:
/// - Modifies an EXISTING TerrainSurface (does not create a new terrain).
/// - Only RAISES terrain heights (never carves down, never trenches).
/// - Keeps support narrow and local to the road footprint (road half-width + small shoulder).
/// - Leaves the rest of the terrain untouched so the volcano environment is preserved.
/// </summary>
internal static class RoadBedTerrainShaper
{
    private const string TerrainPath = "TerrainRoverCourse/TerrainSurface";
    private const string RoadPath    = "TerrainRoverCourse/RoverRoad/Road_PlaceholderReference";

    // Tuning - kept narrow and local on purpose.
    private const float RoadHalfWidth   = 6.5f;   // metres on each side fully raised under road
    private const float ShoulderWidth   = 4.0f;   // metres of smooth blend out to existing terrain
    private const float RoadThickness   = 0.6f;   // terrain sits this far below the road surface
    private const float SamplesPerMetre = 1.0f;   // density of road samples along path

    [MenuItem("Tools/Road Bed/1 - Inspect Setup")]
    private static void InspectSetup()
    {
        if (!TryResolveScene(out Terrain terrain, out TerrainData data, out Transform road, out MeshFilter[] roadMeshes))
            return;

        Vector3 terrainOrigin = terrain.transform.position;
        Vector3 terrainSize   = data.size;
        Bounds  roadBounds    = ComputeRoadWorldBounds(roadMeshes);
        List<Vector3> samples = ExtractRoadSamples(roadMeshes);

        Debug.Log("<color=cyan>[RoadBedTerrainShaper] === INSPECTION ===</color>");
        Debug.Log($"[RBTS] TerrainSurface world pos: {terrainOrigin}");
        Debug.Log($"[RBTS] TerrainData size: {terrainSize}  heightmapResolution: {data.heightmapResolution}");
        Debug.Log($"[RBTS] Terrain world bounds X[{terrainOrigin.x:F1}..{terrainOrigin.x + terrainSize.x:F1}] Y[{terrainOrigin.y:F1}..{terrainOrigin.y + terrainSize.y:F1}] Z[{terrainOrigin.z:F1}..{terrainOrigin.z + terrainSize.z:F1}]");
        Debug.Log($"[RBTS] Road meshes: {roadMeshes.Length}  samples: {samples.Count}");
        Debug.Log($"[RBTS] Road bounds center {roadBounds.center} size {roadBounds.size}");
        Debug.Log($"[RBTS] Road extents X[{roadBounds.min.x:F1}..{roadBounds.max.x:F1}] Y[{roadBounds.min.y:F1}..{roadBounds.max.y:F1}] Z[{roadBounds.min.z:F1}..{roadBounds.max.z:F1}]");
        Debug.Log($"[RBTS] Tuning - HalfWidth:{RoadHalfWidth}m Shoulder:{ShoulderWidth}m Thickness:{RoadThickness}m");
        bool insideMin = roadBounds.min.x >= terrainOrigin.x && roadBounds.min.z >= terrainOrigin.z;
        bool insideMax = roadBounds.max.x <= terrainOrigin.x + terrainSize.x && roadBounds.max.z <= terrainOrigin.z + terrainSize.z;
        Debug.Log($"[RBTS] Road inside terrain XZ? min:{insideMin} max:{insideMax}");
    }

    [MenuItem("Tools/Road Bed/2 - Apply Road Bed (raise only)")]
    private static void ApplyRoadBed()
    {
        if (!TryResolveScene(out Terrain terrain, out TerrainData data, out Transform road, out MeshFilter[] roadMeshes))
            return;

        Vector3 terrainOrigin = terrain.transform.position;
        Vector3 terrainSize   = data.size;
        if (terrainSize.y <= 0.001f)
        {
            Debug.LogError("[RoadBedTerrainShaper] Terrain size Y is zero - cannot normalize heights.");
            return;
        }

        List<Vector3> samples = ExtractRoadSamples(roadMeshes);
        if (samples.Count == 0)
        {
            Debug.LogError("[RoadBedTerrainShaper] No road samples extracted from Road_PlaceholderReference.");
            return;
        }

        // Build a 2D grid of road samples for quick nearest lookup.
        Bounds roadBounds = ComputeRoadWorldBounds(roadMeshes);
        float cellSize    = Mathf.Max(2f, RoadHalfWidth + ShoulderWidth);
        SampleGrid grid   = new SampleGrid(roadBounds, cellSize, samples);

        int res = data.heightmapResolution;
        float[,] heights = data.GetHeights(0, 0, res, res);

        float radius        = RoadHalfWidth + ShoulderWidth;
        float radiusSqr     = radius * radius;
        float halfWidthSqr  = RoadHalfWidth * RoadHalfWidth;

        // Determine the heightmap rectangle that overlaps the road's XZ bounds + radius
        // so we only iterate relevant cells (keeps everything else untouched).
        float pxPerMetreX = (res - 1) / terrainSize.x;
        float pxPerMetreZ = (res - 1) / terrainSize.z;

        int x0 = Mathf.Clamp(Mathf.FloorToInt((roadBounds.min.x - radius - terrainOrigin.x) * pxPerMetreX) - 1, 0, res - 1);
        int x1 = Mathf.Clamp(Mathf.CeilToInt ((roadBounds.max.x + radius - terrainOrigin.x) * pxPerMetreX) + 1, 0, res - 1);
        int z0 = Mathf.Clamp(Mathf.FloorToInt((roadBounds.min.z - radius - terrainOrigin.z) * pxPerMetreZ) - 1, 0, res - 1);
        int z1 = Mathf.Clamp(Mathf.CeilToInt ((roadBounds.max.z + radius - terrainOrigin.z) * pxPerMetreZ) + 1, 0, res - 1);

        int cellsTouched = 0;
        int cellsRaised  = 0;
        float maxRaiseM  = 0f;

        for (int z = z0; z <= z1; z++)
        {
            float worldZ = terrainOrigin.z + (z / (float)(res - 1)) * terrainSize.z;
            for (int x = x0; x <= x1; x++)
            {
                float worldX = terrainOrigin.x + (x / (float)(res - 1)) * terrainSize.x;

                if (!grid.TryFindNearest(worldX, worldZ, radius, out Vector3 nearest, out float distSqr))
                    continue;
                if (distSqr > radiusSqr)
                    continue;

                cellsTouched++;

                // Smooth blend: k=1 inside half-width, falls to 0 at half-width + shoulder.
                float k;
                if (distSqr <= halfWidthSqr)
                {
                    k = 1f;
                }
                else
                {
                    float dist = Mathf.Sqrt(distSqr);
                    float t    = Mathf.InverseLerp(RoadHalfWidth, radius, dist);
                    k = 1f - (t * t * (3f - 2f * t)); // smoothstep falloff
                }

                // Desired terrain height (normalized) under the road sample.
                float roadWorldY     = nearest.y - RoadThickness;
                float roadNormalised = Mathf.Clamp01((roadWorldY - terrainOrigin.y) / terrainSize.y);

                float current = heights[z, x];
                // Blend toward target, then clamp to "raise only".
                float blended = Mathf.Lerp(current, roadNormalised, k);
                float newH    = Mathf.Max(current, blended);

                if (newH > current + 1e-6f)
                {
                    heights[z, x] = newH;
                    cellsRaised++;
                    float raiseMetres = (newH - current) * terrainSize.y;
                    if (raiseMetres > maxRaiseM) maxRaiseM = raiseMetres;
                }
            }
        }

        if (cellsRaised == 0)
        {
            Debug.LogWarning(
                $"[RoadBedTerrainShaper] No cells needed raising - terrain is already at or above the road. " +
                $"Cells inside road radius: {cellsTouched}.");
            return;
        }

        Undo.RegisterCompleteObjectUndo(data, "Raise Terrain Under Road");
        data.SetHeights(0, 0, heights);
        EditorUtility.SetDirty(data);
        EditorUtility.SetDirty(terrain);
        if (terrain.gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);
        AssetDatabase.SaveAssets();

        int totalCells = (x1 - x0 + 1) * (z1 - z0 + 1);
        int totalRes   = res * res;
        float bedCoverageOfTerrain = totalRes > 0 ? (cellsRaised * 100f / totalRes) : 0f;
        Debug.Log("<color=lime>[RoadBedTerrainShaper] === ROAD BED APPLIED ===</color>");
        Debug.Log($"[RBTS] Heightmap window x[{x0}..{x1}] z[{z0}..{z1}] of {res}x{res}");
        Debug.Log($"[RBTS] Cells inside radius:{cellsTouched}  cells raised:{cellsRaised}");
        Debug.Log($"[RBTS] Road bed footprint covers only {bedCoverageOfTerrain:F2}% of the whole heightmap");
        Debug.Log($"[RBTS] Max raise applied: {maxRaiseM:F2} m");
    }

    // ---------------------------------------------------------------------
    // Resolution helpers
    // ---------------------------------------------------------------------
    private static bool TryResolveScene(out Terrain terrain, out TerrainData data, out Transform road, out MeshFilter[] roadMeshes)
    {
        terrain    = null;
        data       = null;
        road       = null;
        roadMeshes = null;

        GameObject terrainGo = GameObject.Find(TerrainPath);
        if (terrainGo == null)
        {
            Debug.LogError($"[RoadBedTerrainShaper] Cannot find '{TerrainPath}' in active scene.");
            return false;
        }
        terrain = terrainGo.GetComponent<Terrain>();
        if (terrain == null || terrain.terrainData == null)
        {
            Debug.LogError("[RoadBedTerrainShaper] TerrainSurface has no Terrain or TerrainData.");
            return false;
        }
        data = terrain.terrainData;

        GameObject roadGo = GameObject.Find(RoadPath);
        if (roadGo == null)
        {
            Debug.LogError($"[RoadBedTerrainShaper] Cannot find '{RoadPath}' in active scene.");
            return false;
        }
        road       = roadGo.transform;
        roadMeshes = roadGo.GetComponentsInChildren<MeshFilter>(true);
        if (roadMeshes.Length == 0)
        {
            Debug.LogError("[RoadBedTerrainShaper] Road_PlaceholderReference has no MeshFilters.");
            return false;
        }
        return true;
    }

    private static Bounds ComputeRoadWorldBounds(MeshFilter[] meshes)
    {
        bool init = false;
        Bounds b  = new Bounds();
        for (int i = 0; i < meshes.Length; i++)
        {
            MeshFilter mf = meshes[i];
            if (mf == null || mf.sharedMesh == null) continue;
            Mesh mesh    = mf.sharedMesh;
            Transform t  = mf.transform;
            Vector3[] vs = mesh.vertices;
            for (int v = 0; v < vs.Length; v++)
            {
                Vector3 w = t.TransformPoint(vs[v]);
                if (!init) { b = new Bounds(w, Vector3.zero); init = true; }
                else b.Encapsulate(w);
            }
        }
        return b;
    }

    /// <summary>
    /// Returns dedup-by-XZ road vertices in world space. We keep the highest Y per XZ bucket
    /// so the road bed tracks the road's top surface.
    /// </summary>
    private static List<Vector3> ExtractRoadSamples(MeshFilter[] meshes)
    {
        const float bucket = 1.0f / SamplesPerMetre;
        Dictionary<long, Vector3> highestByCell = new Dictionary<long, Vector3>(4096);
        for (int i = 0; i < meshes.Length; i++)
        {
            MeshFilter mf = meshes[i];
            if (mf == null || mf.sharedMesh == null) continue;
            Mesh mesh    = mf.sharedMesh;
            Transform t  = mf.transform;
            Vector3[] vs = mesh.vertices;
            for (int v = 0; v < vs.Length; v++)
            {
                Vector3 w = t.TransformPoint(vs[v]);
                long cx  = (long)Mathf.Floor(w.x / bucket);
                long cz  = (long)Mathf.Floor(w.z / bucket);
                long key = (cx << 32) ^ (cz & 0xffffffffL);
                if (!highestByCell.TryGetValue(key, out Vector3 prev) || w.y > prev.y)
                    highestByCell[key] = w;
            }
        }
        return new List<Vector3>(highestByCell.Values);
    }

    // ---------------------------------------------------------------------
    // Spatial grid for fast nearest-sample lookup
    // ---------------------------------------------------------------------
    private sealed class SampleGrid
    {
        private readonly float _cellSize;
        private readonly float _originX;
        private readonly float _originZ;
        private readonly int _width;
        private readonly int _height;
        private readonly List<Vector3>[] _cells;

        public SampleGrid(Bounds bounds, float cellSize, List<Vector3> samples)
        {
            _cellSize = cellSize;
            float pad = cellSize * 2f;
            _originX  = bounds.min.x - pad;
            _originZ  = bounds.min.z - pad;
            _width    = Mathf.Max(1, Mathf.CeilToInt((bounds.size.x + pad * 2f) / cellSize));
            _height   = Mathf.Max(1, Mathf.CeilToInt((bounds.size.z + pad * 2f) / cellSize));
            _cells    = new List<Vector3>[_width * _height];
            for (int i = 0; i < samples.Count; i++)
            {
                Vector3 s = samples[i];
                int cx = Mathf.Clamp(Mathf.FloorToInt((s.x - _originX) / cellSize), 0, _width - 1);
                int cz = Mathf.Clamp(Mathf.FloorToInt((s.z - _originZ) / cellSize), 0, _height - 1);
                int idx = cz * _width + cx;
                if (_cells[idx] == null) _cells[idx] = new List<Vector3>(8);
                _cells[idx].Add(s);
            }
        }

        public bool TryFindNearest(float worldX, float worldZ, float maxRadius, out Vector3 nearest, out float distSqr)
        {
            nearest = default;
            distSqr = float.MaxValue;
            int cx  = Mathf.FloorToInt((worldX - _originX) / _cellSize);
            int cz  = Mathf.FloorToInt((worldZ - _originZ) / _cellSize);
            int rad = Mathf.Max(1, Mathf.CeilToInt(maxRadius / _cellSize));

            bool found = false;
            for (int dz = -rad; dz <= rad; dz++)
            {
                int z = cz + dz;
                if (z < 0 || z >= _height) continue;
                for (int dx = -rad; dx <= rad; dx++)
                {
                    int x = cx + dx;
                    if (x < 0 || x >= _width) continue;
                    List<Vector3> bucket = _cells[z * _width + x];
                    if (bucket == null) continue;
                    for (int i = 0; i < bucket.Count; i++)
                    {
                        Vector3 s  = bucket[i];
                        float ddx  = s.x - worldX;
                        float ddz  = s.z - worldZ;
                        float d2   = ddx * ddx + ddz * ddz;
                        if (d2 < distSqr)
                        {
                            distSqr = d2;
                            nearest = s;
                            found   = true;
                        }
                    }
                }
            }
            return found;
        }
    }
}
#endif
