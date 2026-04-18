#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class RoadPlaceholderAnalyzer
{
    [MenuItem("Tools/Rover/Analyze Road_Placeholder Layout")]
    public static void Analyze()
    {
        var all = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Where(t => IsValidRoadTile(t.name))
            .OrderBy(t => t.name)
            .ToList();

        if (all.Count == 0) { Debug.LogWarning("[RoadAnalyzer] No segments found."); return; }
        Debug.Log($"[RoadAnalyzer] BEGIN --- {all.Count} segments");

        var deltas = new float[all.Count];
        var dists = new float[all.Count];

        for (int i = 1; i < all.Count; i++)
        {
            deltas[i] = Mathf.DeltaAngle(all[i - 1].eulerAngles.y, all[i].eulerAngles.y);
            dists[i] = Vector3.Distance(all[i - 1].position, all[i].position);
        }

        for (int i = 1; i < all.Count; i++)
        {
            if (Mathf.Abs(deltas[i]) > 12f)
                Debug.LogWarning($"[SHARP] [{i - 1:D3}to{i:D3}] dY={deltas[i]:F1}deg dist={dists[i]:F1}m");
        }

        for (int i = 1; i < all.Count; i++)
        {
            if (Mathf.Abs(deltas[i]) > 8f && dists[i] < 3.5f)
                Debug.LogWarning($"[OVERLAP] [{i - 1:D3}to{i:D3}] dY={deltas[i]:F1}deg dist={dists[i]:F1}m");
        }

        int ci = 1;
        while (ci < all.Count)
        {
            if (Mathf.Abs(deltas[ci]) < 2f) { ci++; continue; }
            int sign = System.Math.Sign(deltas[ci]);
            float sum = 0f;
            int start = ci;
            while (ci < all.Count && System.Math.Sign(deltas[ci]) == sign)
            {
                sum += deltas[ci];
                ci++;
            }
            if (Mathf.Abs(sum) > 40f)
                Debug.LogWarning($"[CURVE] [{start:D3}to{ci - 1:D3}] total={sum:F1}deg over {ci - start} tiles");
        }

        for (int i = 0; i < all.Count; i++)
        {
            Debug.Log(
                $"[ROW] [{i:D3}] Y={all[i].eulerAngles.y:F1} dY={deltas[i]:F1} dist={dists[i]:F1} pos=({all[i].position.x:F1},{all[i].position.y:F1},{all[i].position.z:F1})");
        }

        Debug.Log("[RoadAnalyzer] END ---");
    }

    [MenuItem("Tools/Rover/Fix Road_Placeholder Curves")]
    public static void FixCurves()
    {
        var all = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Where(t => IsValidRoadTile(t.name))
            .OrderBy(t => t.name)
            .ToList();

        if (all.Count < 3) { Debug.LogWarning("[RoadFix] Not enough segments found."); return; }

        var originalPositions = all.Select(t => t.position).ToArray();
        var originalPitch = all.Select(t => t.eulerAngles.x).ToArray();
        var currentDeltas = ComputeYawDeltas(originalPositions);
        var zones = BuildSharpZones(currentDeltas, 10f, 2, all.Count);

        if (zones.Count == 0)
        {
            Debug.Log("[RoadFix] No sharp curve clusters detected.");
            return;
        }

        Undo.RecordObjects(all.Cast<Object>().ToArray(), "Smooth Road_Placeholder Curves");

        var smoothed = (Vector3[])originalPositions.Clone();
        var smoothedXZ = smoothed.Select(p => new Vector2(p.x, p.z)).ToArray();

        foreach (var zone in zones)
        {
            for (int iteration = 0; iteration < 6; iteration++)
            {
                var nextXZ = (Vector2[])smoothedXZ.Clone();
                for (int i = zone.start; i <= zone.end; i++)
                {
                    Vector2 prev = smoothedXZ[i - 1];
                    Vector2 cur = smoothedXZ[i];
                    Vector2 next = smoothedXZ[i + 1];
                    Vector2 target = (prev + (cur * 2f) + next) * 0.25f;
                    nextXZ[i] = Vector2.Lerp(cur, target, 0.65f);
                }

                smoothedXZ = nextXZ;
            }
        }

        for (int i = 0; i < smoothed.Length; i++)
            smoothed[i] = new Vector3(smoothedXZ[i].x, originalPositions[i].y, smoothedXZ[i].y);

        var rebuilt = ResamplePolyline(smoothed, all.Count);
        for (int i = 0; i < rebuilt.Length; i++)
        {
            Quaternion rot = BuildTangentRotation(rebuilt, i, originalPositions, originalPitch);
            all[i].position = rebuilt[i];
            all[i].rotation = rot;
            all[i].name = $"Road_Placeholder_{i:D3}";
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log(
            $"[RoadFix] Smoothed {zones.Count} curve zones. Max dY {GetMaxDeltaFromPositions(originalPositions):F1}deg -> {GetMaxDeltaFromPositions(rebuilt):F1}deg. Ctrl+S to save.");
    }

    [MenuItem("Tools/Rover/Compare Road Spacing")]
    public static void CompareSpacing()
    {
        var refTiles = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Where(t => System.Text.RegularExpressions.Regex.IsMatch(t.name, @"^Road_\d{3}$"))
            .OrderBy(t => t.name)
            .ToList();

        var phTiles = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Where(t => IsValidRoadTile(t.name))
            .OrderBy(t => t.name)
            .ToList();

        if (refTiles.Count < 2 || phTiles.Count < 2) { Debug.LogWarning("[Compare] Not enough tiles"); return; }

        float refTotalDist = 0f;
        float refMaxDY = 0f;
        int refSample = Mathf.Min(20, refTiles.Count - 1);
        for (int i = 1; i <= refSample; i++)
        {
            refTotalDist += Vector3.Distance(refTiles[i - 1].position, refTiles[i].position);
            refMaxDY = Mathf.Max(refMaxDY, Mathf.Abs(Mathf.DeltaAngle(refTiles[i - 1].eulerAngles.y, refTiles[i].eulerAngles.y)));
        }
        float refAvgSpacing = refTotalDist / refSample;

        float phTotalDist = 0f;
        float phMaxDY = 0f;
        int phSample = Mathf.Min(20, phTiles.Count - 1);
        for (int i = 1; i <= phSample; i++)
        {
            phTotalDist += Vector3.Distance(phTiles[i - 1].position, phTiles[i].position);
            phMaxDY = Mathf.Max(phMaxDY, Mathf.Abs(Mathf.DeltaAngle(phTiles[i - 1].eulerAngles.y, phTiles[i].eulerAngles.y)));
        }
        float phAvgSpacing = phTotalDist / phSample;

        float phPathLength = 0f;
        for (int i = 1; i < phTiles.Count; i++)
            phPathLength += Vector3.Distance(phTiles[i - 1].position, phTiles[i].position);

        int idealTileCount = Mathf.RoundToInt(phPathLength / refAvgSpacing);

        Debug.Log($"[Compare] REFERENCE Road_000-395: avg spacing={refAvgSpacing:F2}m, max dY={refMaxDY:F1}deg, tiles sampled={refSample}");
        Debug.Log($"[Compare] PLACEHOLDER Road_Placeholder: avg spacing={phAvgSpacing:F2}m, max dY={phMaxDY:F1}deg, tiles sampled={phSample}");
        Debug.Log($"[Compare] Placeholder path length={phPathLength:F1}m, has {phTiles.Count} tiles");
        Debug.Log($"[Compare] At reference spacing ({refAvgSpacing:F2}m), placeholder road needs {idealTileCount} tiles (has {phTiles.Count}, excess={phTiles.Count - idealTileCount})");
        Debug.Log($"[Compare] RATIO: Placeholder tiles are {refAvgSpacing / phAvgSpacing:F1}x MORE DENSE than reference road");
    }

    [MenuItem("Tools/Rover/Fix Road_Placeholder Problem Curve")]
    public static void FixProblemCurve()
    {
        var all = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Where(t => IsValidRoadTile(t.name))
            .OrderBy(t => t.name)
            .ToList();

        const int startAnchor = 15;
        const int endAnchor = 25;
        if (all.Count <= endAnchor)
        {
            Debug.LogWarning("[RoadFix] Not enough Road_Placeholder tiles for the targeted curve fix.");
            return;
        }

        Undo.RecordObjects(all.Cast<Object>().ToArray(), "Fix Road_Placeholder Problem Curve");

        Vector3 start = all[startAnchor].position;
        Vector3 end = all[endAnchor].position;
        Vector3 startForward = GetPlanarForward(all[startAnchor]);
        Vector3 endForward = GetPlanarForward(all[endAnchor]);
        float controlLength = Mathf.Min(
            Vector3.ProjectOnPlane(end - start, Vector3.up).magnitude * 0.35f,
            7.5f);

        Vector3 controlA = start + (startForward * controlLength);
        controlA.y = Mathf.Lerp(start.y, end.y, 0.22f);

        Vector3 controlB = end - (endForward * controlLength);
        controlB.y = Mathf.Lerp(start.y, end.y, 0.78f);

        for (int i = startAnchor + 1; i < endAnchor; i++)
        {
            float t = (float)(i - startAnchor) / (endAnchor - startAnchor);
            Vector3 position = CubicBezierPoint(start, controlA, controlB, end, t);
            Vector3 tangent = CubicBezierTangent(start, controlA, controlB, end, t);
            if (tangent.sqrMagnitude < 0.0001f)
                tangent = end - start;

            all[i].position = position;
            all[i].rotation = Quaternion.LookRotation(tangent.normalized, Vector3.up);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[RoadFix] Rebuilt tiles 016-024 from a clean local spline between tiles 015 and 025. Ctrl+S to save.");
    }

    [MenuItem("Tools/Rover/Rebuild Road_Placeholder at Reference Spacing")]
    public static void RebuildAtReferenceSpacing()
    {
        var all = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Where(t => IsValidRoadTile(t.name))
            .OrderBy(t => t.name)
            .ToList();

        if (all.Count == 0) { Debug.LogWarning("[RoadRebuild] No segments found."); return; }

        const float targetSpacing = 4.42f;

        var arcLen = new float[all.Count];
        for (int i = 1; i < all.Count; i++)
            arcLen[i] = arcLen[i - 1] + Vector3.Distance(all[i - 1].position, all[i].position);
        float totalLen = arcLen[all.Count - 1];

        int keepCount = Mathf.Clamp(Mathf.RoundToInt(totalLen / targetSpacing) + 1, 2, all.Count);
        float spacing = totalLen / (keepCount - 1);
        Debug.Log($"[RoadRebuild] Path={totalLen:F1}m  keeping {keepCount} tiles at {spacing:F2}m spacing  deleting {all.Count - keepCount}");

        var sampledPos = new Vector3[keepCount];
        for (int j = 0; j < keepCount; j++)
        {
            float targetArc = (j == keepCount - 1) ? totalLen : j * spacing;
            int seg = System.Array.BinarySearch(arcLen, targetArc);
            if (seg < 0) seg = Mathf.Clamp(~seg - 1, 0, all.Count - 2);
            else seg = Mathf.Clamp(seg, 0, all.Count - 2);

            float segLen = arcLen[seg + 1] - arcLen[seg];
            float t = segLen > 0.0001f ? (targetArc - arcLen[seg]) / segLen : 0f;
            sampledPos[j] = Vector3.Lerp(all[seg].position, all[seg + 1].position, t);
        }

        var sampledRot = new Quaternion[keepCount];
        for (int j = 0; j < keepCount; j++)
        {
            Vector3 fwd = (j < keepCount - 1)
                ? sampledPos[j + 1] - sampledPos[j]
                : sampledPos[j] - sampledPos[j - 1];

            Vector3 planar = Vector3.ProjectOnPlane(fwd, Vector3.up);
            if (planar.sqrMagnitude < 0.0001f) planar = Vector3.forward;

            float minD = float.MaxValue;
            int closest = 0;
            for (int i = 0; i < all.Count; i++)
            {
                float d = (all[i].position - sampledPos[j]).sqrMagnitude;
                if (d < minD) { minD = d; closest = i; }
            }

            float pitchX = all[closest].eulerAngles.x;
            float yaw = Mathf.Atan2(planar.x, planar.z) * Mathf.Rad2Deg;
            sampledRot[j] = Quaternion.Euler(pitchX, yaw, 0f);
        }

        Undo.RecordObjects(all.Take(keepCount).Cast<Object>().ToArray(), "Rebuild Road_Placeholder");
        for (int j = 0; j < keepCount; j++)
        {
            all[j].position = sampledPos[j];
            all[j].rotation = sampledRot[j];
            all[j].name = $"Road_Placeholder_{j:D3}";
        }

        int deleted = 0;
        for (int i = all.Count - 1; i >= keepCount; i--)
        {
            Undo.DestroyObjectImmediate(all[i].gameObject);
            deleted++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log($"[RoadRebuild] Done. {keepCount} tiles repositioned, {deleted} deleted. Ctrl+S to save.");
    }

    private static float GetMaxDelta(List<Transform> all, int from, int to)
    {
        float max = 0f;
        for (int i = Mathf.Max(from + 1, 1); i <= Mathf.Min(to, all.Count - 1); i++)
            max = Mathf.Max(max, Mathf.Abs(Mathf.DeltaAngle(all[i - 1].eulerAngles.y, all[i].eulerAngles.y)));
        return max;
    }

    private static float[] ComputeYawDeltas(IReadOnlyList<Vector3> positions)
    {
        var deltas = new float[positions.Count];
        if (positions.Count < 3) return deltas;

        float prevYaw = GetPlanarYaw(positions[1] - positions[0]);
        for (int i = 1; i < positions.Count - 1; i++)
        {
            float nextYaw = GetPlanarYaw(positions[i + 1] - positions[i]);
            deltas[i] = Mathf.DeltaAngle(prevYaw, nextYaw);
            prevYaw = nextYaw;
        }

        return deltas;
    }

    private static List<(int start, int end)> BuildSharpZones(float[] deltas, float threshold, int padding, int tileCount)
    {
        var zones = new List<(int start, int end)>();
        int i = 1;
        while (i < deltas.Length - 1)
        {
            if (Mathf.Abs(deltas[i]) < threshold)
            {
                i++;
                continue;
            }

            int sign = System.Math.Sign(deltas[i]);
            int start = i;
            int end = i;
            i++;

            while (i < deltas.Length - 1 && Mathf.Abs(deltas[i]) > 2f && System.Math.Sign(deltas[i]) == sign)
            {
                end = i;
                i++;
            }

            start = Mathf.Max(1, start - padding);
            end = Mathf.Min(tileCount - 2, end + padding);

            if (zones.Count > 0 && start <= zones[zones.Count - 1].end + 1)
            {
                zones[zones.Count - 1] = (zones[zones.Count - 1].start, Mathf.Max(zones[zones.Count - 1].end, end));
            }
            else
            {
                zones.Add((start, end));
            }
        }

        return zones;
    }

    private static Vector3[] ResamplePolyline(IReadOnlyList<Vector3> points, int sampleCount)
    {
        var arcLen = new float[points.Count];
        for (int i = 1; i < points.Count; i++)
            arcLen[i] = arcLen[i - 1] + Vector3.Distance(points[i - 1], points[i]);

        float totalLen = arcLen[points.Count - 1];
        var sampled = new Vector3[sampleCount];

        for (int j = 0; j < sampleCount; j++)
        {
            float targetArc = (j == sampleCount - 1) ? totalLen : totalLen * j / (sampleCount - 1);
            int seg = System.Array.BinarySearch(arcLen, targetArc);
            if (seg < 0) seg = Mathf.Clamp(~seg - 1, 0, points.Count - 2);
            else seg = Mathf.Clamp(seg, 0, points.Count - 2);

            float segLen = arcLen[seg + 1] - arcLen[seg];
            float t = segLen > 0.0001f ? (targetArc - arcLen[seg]) / segLen : 0f;
            sampled[j] = Vector3.Lerp(points[seg], points[seg + 1], t);
        }

        return sampled;
    }

    private static Quaternion BuildTangentRotation(
        IReadOnlyList<Vector3> points,
        int index,
        IReadOnlyList<Vector3> originalPositions,
        IReadOnlyList<float> originalPitch)
    {
        Vector3 fwd = (index < points.Count - 1)
            ? points[index + 1] - points[index]
            : points[index] - points[index - 1];

        Vector3 planar = Vector3.ProjectOnPlane(fwd, Vector3.up);
        if (planar.sqrMagnitude < 0.0001f) planar = Vector3.forward;

        int closest = 0;
        float minDist = float.MaxValue;
        for (int i = 0; i < originalPositions.Count; i++)
        {
            float dist = (originalPositions[i] - points[index]).sqrMagnitude;
            if (dist < minDist)
            {
                minDist = dist;
                closest = i;
            }
        }

        float yaw = GetPlanarYaw(planar);
        return Quaternion.Euler(originalPitch[closest], yaw, 0f);
    }

    private static float GetMaxDeltaFromPositions(IReadOnlyList<Vector3> positions)
    {
        float max = 0f;
        float[] deltas = ComputeYawDeltas(positions);
        for (int i = 0; i < deltas.Length; i++)
            max = Mathf.Max(max, Mathf.Abs(deltas[i]));
        return max;
    }

    private static float GetPlanarYaw(Vector3 direction)
    {
        Vector3 planar = Vector3.ProjectOnPlane(direction, Vector3.up);
        if (planar.sqrMagnitude < 0.0001f) return 0f;
        return Mathf.Atan2(planar.x, planar.z) * Mathf.Rad2Deg;
    }

    private static Vector3 GetPlanarForward(Transform t)
    {
        Vector3 forward = Vector3.ProjectOnPlane(t.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f) return Vector3.forward;
        return forward.normalized;
    }

    private static Vector3 CubicBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        return
            (u * u * u * p0) +
            (3f * u * u * t * p1) +
            (3f * u * t * t * p2) +
            (t * t * t * p3);
    }

    private static Vector3 CubicBezierTangent(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        return
            (3f * u * u * (p1 - p0)) +
            (6f * u * t * (p2 - p1)) +
            (3f * t * t * (p3 - p2));
    }

    private static bool IsValidRoadTile(string n)
        => System.Text.RegularExpressions.Regex.IsMatch(n, @"^Road_Placeholder_\d{3}$");

    [MenuItem("Tools/Rover/REVERT Road_Placeholder to Pre-Session State")]
    public static void RevertAllChanges()
    {
        var roads = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Where(t => IsValidRoadTile(t.name))
            .OrderBy(t => t.name)
            .ToList();

        if (roads.Count > 0)
        {
            Undo.RecordObjects(roads.Cast<Object>().ToArray(), "Revert Road_Placeholder Z-Scale");
            foreach (Transform t in roads)
            {
                Vector3 s = t.localScale;
                s.z = 2.34f;
                t.localScale = s;
                BoxCollider box = t.GetComponent<BoxCollider>();
                if (box != null)
                {
                    Undo.RecordObject(box, "Revert PhysMat");
                    box.sharedMaterial = null;
                }
            }

            Debug.Log($"[Revert] Restored Z=2.34f and cleared physics material on {roads.Count} Road_Placeholder tiles.");
        }

        string[] prefixes =
        {
            "Base_Road_Placeholder_", "Edge_Road_Placeholder_",
            "Guide_Road_Placeholder_", "Shelf_Road_Placeholder_"
        };

        int destroyed = 0;
        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string n = go.name;
            foreach (string prefix in prefixes)
            {
                if (n.StartsWith(prefix))
                {
                    Undo.DestroyObjectImmediate(go);
                    destroyed++;
                    break;
                }
            }
        }

        if (destroyed > 0) Debug.Log($"[Revert] Destroyed {destroyed} companion objects.");

        string[] newGroups = { "Guide_Road_PlaceholderStyled", "Shelf_Road_PlaceholderStyled" };
        foreach (string grpName in newGroups)
        {
            GameObject grp = GameObject.Find(grpName);
            if (grp != null && grp.transform.childCount == 0)
            {
                Undo.DestroyObjectImmediate(grp);
                Debug.Log($"[Revert] Deleted empty group '{grpName}'.");
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[Revert] Done. Road_Placeholder is back to pre-session state. Ctrl+S to save.");
    }

    [MenuItem("Tools/Rover/1 - Fix Road_Placeholder Z-Scale")]
    public static void FixTileZScale()
    {
        var all = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Where(t => IsValidRoadTile(t.name))
            .OrderBy(t => t.name)
            .ToList();

        if (all.Count == 0) { Debug.LogWarning("[ZFix] No Road_Placeholder_XXX tiles found."); return; }

        float lastSpacing = 4.42f;
        float avSum = 0f;
        int avN = 0;
        for (int i = Mathf.Max(0, all.Count - 6); i < all.Count - 1; i++)
        {
            avSum += Vector3.Distance(all[i].position, all[i + 1].position);
            avN++;
        }

        if (avN > 0) lastSpacing = avSum / avN;

        Undo.RecordObjects(all.Cast<Object>().ToArray(), "Fix Road_Placeholder Z-Scale");

        for (int i = 0; i < all.Count; i++)
        {
            float spacing = (i < all.Count - 1)
                ? Vector3.Distance(all[i].position, all[i + 1].position)
                : lastSpacing;
            Vector3 s = all[i].localScale;
            s.z = spacing + 0.8f;
            all[i].localScale = s;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log($"[ZFix] Z-scale fixed on {all.Count} tiles. Formula: spacing+0.8f (avg spacing≈{lastSpacing:F2}m -> Z≈{lastSpacing + 0.8f:F2}m). Ctrl+S to save.");
    }

    [MenuItem("Tools/Rover/2 - Apply Physics Material to Road_Placeholder")]
    public static void ApplyPhysicsMaterial()
    {
        var all = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Where(t => IsValidRoadTile(t.name))
            .OrderBy(t => t.name)
            .ToList();

        if (all.Count == 0) { Debug.LogWarning("[PhysMat] No Road_Placeholder_XXX tiles found."); return; }

        PhysicsMaterial mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(
            "Assets/Levels/5 Rover/Physical Materials/Ramp.physicMaterial");

        if (mat == null)
        {
            Debug.LogWarning("[PhysMat] Could not load 'Assets/Levels/5 Rover/Physical Materials/Ramp.physicMaterial'. Verify the path.");
            return;
        }

        int modified = 0;
        foreach (Transform t in all)
        {
            BoxCollider box = t.GetComponent<BoxCollider>();
            if (box == null) continue;
            Undo.RecordObject(box, "Apply Physics Material");
            box.sharedMaterial = mat;
            modified++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log($"[PhysMat] Applied '{mat.name}' to {modified}/{all.Count} tiles. Ctrl+S to save.");
    }

    [MenuItem("Tools/Rover/3 - Spawn Road_Placeholder Companions")]
    public static void SpawnCompanionObjects()
    {
        var roads = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Where(t => IsValidRoadTile(t.name))
            .OrderBy(t => t.name)
            .ToList();

        if (roads.Count == 0) { Debug.LogWarning("[Companions] No Road_Placeholder_XXX tiles found."); return; }

        Transform baseParent = FindOrCreateGroup("Base_Road_PlaceholderStyled");
        Transform edgeParent = FindOrCreateGroup("Edges_Road_PlaceholderStyled");
        Transform guideParent = FindOrCreateGroup("Guide_Road_PlaceholderStyled");
        Transform shelfParent = FindOrCreateGroup("Shelf_Road_PlaceholderStyled");

        ClearChildrenWithPrefix(baseParent, "Base_Road_Placeholder_");
        ClearChildrenWithPrefix(edgeParent, "Edge_Road_Placeholder_");
        ClearChildrenWithPrefix(guideParent, "Guide_Road_Placeholder_");
        ClearChildrenWithPrefix(shelfParent, "Shelf_Road_Placeholder_");

        const float roadHalfWidth = 4.8f;
        const float baseExtraWidth = 0.71f;
        const float baseThickness = 0.60f;
        const float baseUpOff = -0.16f;
        const float edgeWidth = 0.70f;
        const float edgeThickness = 0.42f;
        const float edgeUpOff = 0.00f;
        const float edgeRightOffPastHalfWidth = 0.50f;
        const float guideWidth = 1.24f;
        const float guideThickness = 0.62f;
        const float guideUpOff = 0.16f;
        const float guideRightOffPastHalfWidth = 0.28f;
        const float guideZTilt = 17f;
        const float shelfWidth = 1.60f;
        const float shelfThickness = 0.40f;
        const float shelfUpOff = -0.50f;
        const float shelfRightOffPastHalfWidth = 1.20f;

        Material roadMat = roads[0].GetComponent<Renderer>()?.sharedMaterial;

        Undo.SetCurrentGroupName("Spawn Road_Placeholder Companions");
        int undoGroup = Undo.GetCurrentGroup();

        int created = 0;
        for (int i = 0; i < roads.Count; i++)
        {
            Transform t = roads[i];
            string idx = i.ToString("D3");

            Vector3 centre = t.position;
            Quaternion rot = t.rotation;
            Vector3 right = t.right;
            float segZ = t.localScale.z;

            SpawnCube(baseParent, "Base_Road_Placeholder_" + idx,
                centre + Vector3.up * baseUpOff, rot,
                new Vector3(roadHalfWidth * 2f + baseExtraWidth, baseThickness, segZ + 0.03f), roadMat);

            float edgeLat = roadHalfWidth + edgeRightOffPastHalfWidth;
            Vector3 eScale = new Vector3(edgeWidth, edgeThickness, segZ + 0.05f);
            SpawnCube(edgeParent, "Edge_Road_Placeholder_" + idx + "_L",
                centre + (-right * edgeLat) + Vector3.up * edgeUpOff, rot, eScale, roadMat);
            SpawnCube(edgeParent, "Edge_Road_Placeholder_" + idx + "_R",
                centre + (right * edgeLat) + Vector3.up * edgeUpOff, rot, eScale, roadMat);

            float guideLat = roadHalfWidth + guideRightOffPastHalfWidth;
            Vector3 gScale = new Vector3(guideWidth, guideThickness, segZ + 0.25f);
            Quaternion gRotL = rot * Quaternion.Euler(0f, 0f, -guideZTilt);
            Quaternion gRotR = rot * Quaternion.Euler(0f, 0f, guideZTilt);
            SpawnCube(guideParent, "Guide_Road_Placeholder_" + idx + "_L",
                centre + (-right * guideLat) + Vector3.up * guideUpOff, gRotL, gScale, null);
            SpawnCube(guideParent, "Guide_Road_Placeholder_" + idx + "_R",
                centre + (right * guideLat) + Vector3.up * guideUpOff, gRotR, gScale, null);

            float shelfLat = roadHalfWidth + shelfRightOffPastHalfWidth;
            Vector3 sScale = new Vector3(shelfWidth, shelfThickness, segZ + 0.30f);
            SpawnCube(shelfParent, "Shelf_Road_Placeholder_" + idx + "_L",
                centre + (-right * shelfLat) + Vector3.up * shelfUpOff, rot, sScale, roadMat);
            SpawnCube(shelfParent, "Shelf_Road_Placeholder_" + idx + "_R",
                centre + (right * shelfLat) + Vector3.up * shelfUpOff, rot, sScale, roadMat);

            created += 7;
        }

        Undo.CollapseUndoOperations(undoGroup);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log($"[Companions] Spawned {created} objects ({roads.Count} tiles x 7 types). Run 'Fix Road Placeholder Gaps' next for curve correction. Ctrl+S to save.");
    }

    private static Transform FindOrCreateGroup(string groupName)
    {
        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (go.name == groupName) return go.transform;
        GameObject fresh = new GameObject(groupName);
        Undo.RegisterCreatedObjectUndo(fresh, "Create group " + groupName);
        return fresh.transform;
    }

    private static void ClearChildrenWithPrefix(Transform parent, string prefix)
    {
        if (parent == null) return;
        var doomed = new List<GameObject>();
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform c = parent.GetChild(i);
            if (c.name.StartsWith(prefix)) doomed.Add(c.gameObject);
        }

        foreach (GameObject go in doomed) Undo.DestroyObjectImmediate(go);
    }

    private static void SpawnCube(Transform parent, string objName, Vector3 worldPos, Quaternion worldRot, Vector3 worldScale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = objName;
        go.transform.SetParent(parent, true);
        go.transform.position = worldPos;
        go.transform.rotation = worldRot;
        go.transform.localScale = worldScale;
        if (mat != null)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;
        }

        Undo.RegisterCreatedObjectUndo(go, "Spawn companion " + objName);
    }
}
#endif
