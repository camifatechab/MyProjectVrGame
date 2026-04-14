#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Generates RoverRoad-style cube segments + companion structures (Base, Edge L/R,
/// Guide L/R, Shelf L/R) along the path of the EasyRoads3D Road_PlaceholderReference,
/// matching the existing RoverRoad style and parented into the existing
/// RoverRoad / RoverRoad_Base / RoverRoad_GuideRails groups.
///
/// The placeholder mesh itself is left untouched.
/// </summary>
internal static class PlaceholderRoadStyler
{
    private const string CourseRoot = "TerrainRoverCourse";
    private const string RoadGroup  = "TerrainRoverCourse/RoverRoad";
    private const string PlaceholderPath = "TerrainRoverCourse/RoverRoad/Road_PlaceholderReference";

    private const string RoadSegPrefix  = "Road_Placeholder_";
    private const string BaseSegPrefix  = "Base_Placeholder_";
    private const string EdgeSegPrefix  = "Edge_Placeholder_";
    private const string GuideSegPrefix = "Guide_Placeholder_";
    private const string ShelfSegPrefix = "Shelf_Placeholder_";

    // Sampling along the placeholder road's centerline.
    private const float CenterlineStep = 3.0f; // metres between centerline samples
    private const int   CrossSliceMin  = 4;    // need at least this many verts in a slice

    // ---------------------------------------------------------------------
    // Menu 1 - inspect existing RoverRoad segment styles
    // ---------------------------------------------------------------------
    [MenuItem("Tools/Placeholder Road/1 - Inspect Existing Style")]
    private static void InspectStyle()
    {
        Transform road  = FindByExactName("Road_000");
        if (road == null) { Debug.LogError("[PRS] Road_000 not found."); return; }

        Transform baseT  = FindByExactName("Base_Road_000");
        Transform edgeL  = FindByExactName("Edge_Road_000_L");
        Transform edgeR  = FindByExactName("Edge_Road_000_R");
        Transform guideL = FindByExactName("Guide_Road_000_L");
        Transform guideR = FindByExactName("Guide_Road_000_R");
        Transform shelfL = FindByExactName("Shelf_Road_000_L");
        Transform shelfR = FindByExactName("Shelf_Road_000_R");

        Debug.Log("<color=cyan>[PRS] === EXISTING STYLE INSPECTION ===</color>");
        LogStyle("Road_000",      road,   road);
        LogStyle("Base_Road_000", baseT,  road);
        LogStyle("Edge_L",        edgeL,  road);
        LogStyle("Edge_R",        edgeR,  road);
        LogStyle("Guide_L",       guideL, road);
        LogStyle("Guide_R",       guideR, road);
        LogStyle("Shelf_L",       shelfL, road);
        LogStyle("Shelf_R",       shelfR, road);
    }

    private static void LogStyle(string label, Transform t, Transform road)
    {
        if (t == null || road == null)
        {
            Debug.Log($"[PRS] {label}: <missing>");
            return;
        }
        float yaw = road.eulerAngles.y * Mathf.Deg2Rad;
        Vector3 forward = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
        Vector3 right   = new Vector3(Mathf.Cos(yaw), 0f, -Mathf.Sin(yaw));
        Vector3 diff    = t.position - road.position;
        float fwdOff    = Vector3.Dot(diff, forward);
        float rightOff  = Vector3.Dot(diff, right);
        float upOff     = diff.y;
        float zRotRel   = Mathf.DeltaAngle(road.eulerAngles.z, t.eulerAngles.z);
        Vector3 s       = t.localScale;
        Debug.Log($"[PRS] {label}: rightOff={rightOff:F3} upOff={upOff:F3} fwdOff={fwdOff:F3}  zRotRel={zRotRel:F2}deg  scale=({s.x:F3},{s.y:F3},{s.z:F3})");
    }

    private static Transform FindByExactName(string name)
    {
        GameObject[] all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
            if (all[i].name == name)
                return all[i].transform;
        return null;
    }

    // ---------------------------------------------------------------------
    // Menu 2 - build placeholder road style
    // ---------------------------------------------------------------------
    [MenuItem("Tools/Placeholder Road/2 - Build Companions Along Placeholder")]
    private static void Build()
    {
        if (!ResolveTargets(out Transform courseRoot,
                            out Transform roadGroup,
                            out Transform baseGroup,
                            out Transform edgeGroup,
                            out Transform guideGroup,
                            out Transform shelfGroup,
                            out MeshFilter[] placeholderMeshes,
                            out StyleTemplate style))
        {
            return;
        }

        List<CenterlineSample> centerline = SampleCenterline(placeholderMeshes);
        if (centerline.Count < 2)
        {
            Debug.LogError("[PRS] Could not extract a usable centerline (need at least 2 samples).");
            return;
        }

        Debug.Log($"[PRS] Centerline samples: {centerline.Count}");

        // Remove any prior placeholder companions (idempotent regeneration).
        int removed = 0;
        removed += RemoveChildrenWithPrefix(roadGroup,  RoadSegPrefix);
        removed += RemoveChildrenWithPrefix(baseGroup,  BaseSegPrefix);
        removed += RemoveChildrenWithPrefix(edgeGroup,  EdgeSegPrefix);
        removed += RemoveChildrenWithPrefix(guideGroup, GuideSegPrefix);
        removed += RemoveChildrenWithPrefix(shelfGroup, ShelfSegPrefix);
        if (removed > 0) Debug.Log($"[PRS] Removed {removed} stale placeholder companion segments.");

        Undo.SetCurrentGroupName("Build Placeholder Road Companions");
        int undoGroup = Undo.GetCurrentGroup();

        int created = 0;
        for (int i = 0; i < centerline.Count - 1; i++)
        {
            CenterlineSample a = centerline[i];
            CenterlineSample b = centerline[i + 1];
            Vector3 segCenter  = (a.position + b.position) * 0.5f;
            Vector3 segDelta   = b.position - a.position;
            float   segLen     = segDelta.magnitude;
            if (segLen < 0.05f) continue;

            // Forward in XZ (so cubes don't pitch). Up Y. Right perpendicular.
            Vector3 fwdXZ = new Vector3(segDelta.x, 0f, segDelta.z);
            float   fwdLen = fwdXZ.magnitude;
            if (fwdLen < 0.001f) continue;
            fwdXZ /= fwdLen;
            Vector3 right = Vector3.Cross(Vector3.up, fwdXZ);
            // Yaw rotation only (no pitch / no bank for placeholder - keep it stable).
            Quaternion baseRot = Quaternion.LookRotation(fwdXZ, Vector3.up);

            float width = (a.width + b.width) * 0.5f;
            string idx  = i.ToString("000");

            // Road surface (top-Y of placeholder mesh at segCenter).
            float roadTopY = (a.topY + b.topY) * 0.5f;

            // ---------------- Road segment ----------------
            Vector3 roadCenter = new Vector3(segCenter.x, roadTopY - style.RoadThickness * 0.5f, segCenter.z);
            Vector3 roadScale  = new Vector3(width, style.RoadThickness, segLen + style.RoadLengthPad);
            Transform roadCube = SpawnCube(roadGroup, RoadSegPrefix + idx, roadCenter, baseRot, roadScale, style.RoadMaterial);

            // ---------------- Base segment ----------------
            Vector3 baseCenter = roadCenter + new Vector3(0f, style.BaseUpOff, 0f);
            Vector3 baseScale  = new Vector3(width + style.BaseExtraWidth, style.BaseThickness, segLen + style.BaseLengthPad);
            SpawnCube(baseGroup, BaseSegPrefix + idx, baseCenter, baseRot, baseScale, style.BaseMaterial);

            // ---------------- Edge segments L/R ----------------
            float edgeRightOff = (width * 0.5f) + style.EdgeRightOffPastHalfWidth;
            Vector3 edgeL_pos  = roadCenter + (-edgeRightOff * right) + new Vector3(0f, style.EdgeUpOff, 0f);
            Vector3 edgeR_pos  = roadCenter + ( edgeRightOff * right) + new Vector3(0f, style.EdgeUpOff, 0f);
            Vector3 edgeScale  = new Vector3(style.EdgeWidth, style.EdgeThickness, segLen + style.EdgeLengthPad);
            Quaternion edgeRotL = baseRot * Quaternion.Euler(0f, 0f, -style.EdgeZTilt);
            Quaternion edgeRotR = baseRot * Quaternion.Euler(0f, 0f,  style.EdgeZTilt);
            SpawnCube(edgeGroup, EdgeSegPrefix + idx + "_L", edgeL_pos, edgeRotL, edgeScale, style.EdgeMaterial);
            SpawnCube(edgeGroup, EdgeSegPrefix + idx + "_R", edgeR_pos, edgeRotR, edgeScale, style.EdgeMaterial);

            // ---------------- Guide rail segments L/R ----------------
            float guideRightOff = (width * 0.5f) + style.GuideRightOffPastHalfWidth;
            Vector3 guideL_pos  = roadCenter + (-guideRightOff * right) + new Vector3(0f, style.GuideUpOff, 0f);
            Vector3 guideR_pos  = roadCenter + ( guideRightOff * right) + new Vector3(0f, style.GuideUpOff, 0f);
            Vector3 guideScale  = new Vector3(style.GuideWidth, style.GuideThickness, segLen + style.GuideLengthPad);
            Quaternion guideRotL = baseRot * Quaternion.Euler(0f, 0f, -style.GuideZTilt);
            Quaternion guideRotR = baseRot * Quaternion.Euler(0f, 0f,  style.GuideZTilt);
            SpawnCube(guideGroup, GuideSegPrefix + idx + "_L", guideL_pos, guideRotL, guideScale, style.GuideMaterial);
            SpawnCube(guideGroup, GuideSegPrefix + idx + "_R", guideR_pos, guideRotR, guideScale, style.GuideMaterial);

            // ---------------- Catch shelf segments L/R ----------------
            float shelfRightOff = (width * 0.5f) + style.ShelfRightOffPastHalfWidth;
            Vector3 shelfL_pos  = roadCenter + (-shelfRightOff * right) + new Vector3(0f, style.ShelfUpOff, 0f);
            Vector3 shelfR_pos  = roadCenter + ( shelfRightOff * right) + new Vector3(0f, style.ShelfUpOff, 0f);
            Vector3 shelfScale  = new Vector3(style.ShelfWidth, style.ShelfThickness, segLen + style.ShelfLengthPad);
            Quaternion shelfRotL = baseRot * Quaternion.Euler(0f, 0f, -style.ShelfZTilt);
            Quaternion shelfRotR = baseRot * Quaternion.Euler(0f, 0f,  style.ShelfZTilt);
            SpawnCube(shelfGroup, ShelfSegPrefix + idx + "_L", shelfL_pos, shelfRotL, shelfScale, style.ShelfMaterial);
            SpawnCube(shelfGroup, ShelfSegPrefix + idx + "_R", shelfR_pos, shelfRotR, shelfScale, style.ShelfMaterial);

            created += 8; // 1 road + 1 base + 2 edges + 2 guides + 2 shelves
        }

        Undo.CollapseUndoOperations(undoGroup);
        if (courseRoot.gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(courseRoot.gameObject.scene);

        Debug.Log($"<color=lime>[PRS] === BUILD COMPLETE ===</color>");
        Debug.Log($"[PRS] Created {created} cubes across {centerline.Count - 1} segments along the placeholder road.");
    }

    // ---------------------------------------------------------------------
    // Style template captured from existing RoverRoad segments
    // ---------------------------------------------------------------------
    private struct StyleTemplate
    {
        public float RoadThickness;
        public float RoadLengthPad;
        public Material RoadMaterial;

        public float BaseExtraWidth;
        public float BaseThickness;
        public float BaseLengthPad;
        public float BaseUpOff;
        public Material BaseMaterial;

        public float EdgeWidth;
        public float EdgeThickness;
        public float EdgeLengthPad;
        public float EdgeUpOff;
        public float EdgeRightOffPastHalfWidth;
        public float EdgeZTilt;
        public Material EdgeMaterial;

        public float GuideWidth;
        public float GuideThickness;
        public float GuideLengthPad;
        public float GuideUpOff;
        public float GuideRightOffPastHalfWidth;
        public float GuideZTilt;
        public Material GuideMaterial;

        public float ShelfWidth;
        public float ShelfThickness;
        public float ShelfLengthPad;
        public float ShelfUpOff;
        public float ShelfRightOffPastHalfWidth;
        public float ShelfZTilt;
        public Material ShelfMaterial;
    }

    private static StyleTemplate CaptureStyleFromScene(Transform roadGroup)
    {
        // Defaults if nothing matches (mirrors observed values from Road_000 row).
        StyleTemplate s = new StyleTemplate
        {
            RoadThickness  = 0.42f,
            RoadLengthPad  = 0.8f,

            BaseExtraWidth = 0.71f,
            BaseThickness  = 0.60f,
            BaseLengthPad  = 0.83f,
            BaseUpOff      = -0.16f,

            EdgeWidth                 = 0.7f,
            EdgeThickness             = 0.42f,
            EdgeLengthPad             = 0.85f,
            EdgeUpOff                 = 0.0f,
            EdgeRightOffPastHalfWidth = 0.5f,
            EdgeZTilt                 = 0f,

            GuideWidth                 = 1.24f,
            GuideThickness             = 0.62f,
            GuideLengthPad             = 1.05f,
            GuideUpOff                 = 0.16f,
            GuideRightOffPastHalfWidth = 0.28f,
            GuideZTilt                 = 17.0f,

            ShelfWidth                 = 1.6f,
            ShelfThickness             = 0.4f,
            ShelfLengthPad             = 1.1f,
            ShelfUpOff                 = -0.5f,
            ShelfRightOffPastHalfWidth = 1.2f,
            ShelfZTilt                 = 0f,
        };

        Transform road   = FindByExactName("Road_000");
        if (road == null) return s;

        Transform baseT  = FindByExactName("Base_Road_000");
        Transform edgeL  = FindByExactName("Edge_Road_000_L");
        Transform edgeR  = FindByExactName("Edge_Road_000_R");
        Transform guideL = FindByExactName("Guide_Road_000_L");
        Transform guideR = FindByExactName("Guide_Road_000_R");
        Transform shelfL = FindByExactName("Shelf_Road_000_L");
        Transform shelfR = FindByExactName("Shelf_Road_000_R");

        float roadHalfWidth = road.localScale.x * 0.5f;
        s.RoadThickness     = road.localScale.y;

        if (baseT != null)
        {
            s.BaseExtraWidth = baseT.localScale.x - road.localScale.x;
            s.BaseThickness  = baseT.localScale.y;
            s.BaseLengthPad  = Mathf.Max(0f, baseT.localScale.z - road.localScale.z) + 0.8f;
            s.BaseUpOff      = baseT.position.y - road.position.y;
            s.BaseMaterial   = GetSharedMaterial(baseT);
        }

        if (edgeL != null && edgeR != null)
        {
            s.EdgeWidth     = edgeL.localScale.x;
            s.EdgeThickness = edgeL.localScale.y;
            s.EdgeLengthPad = Mathf.Max(0f, edgeL.localScale.z - road.localScale.z) + 0.8f;
            s.EdgeUpOff     = ((edgeL.position.y + edgeR.position.y) * 0.5f) - road.position.y;
            float rightOffL = ProjectOnRoadRight(edgeL.position - road.position, road.eulerAngles.y);
            float rightOffR = ProjectOnRoadRight(edgeR.position - road.position, road.eulerAngles.y);
            float lateralAvg = (Mathf.Abs(rightOffL) + Mathf.Abs(rightOffR)) * 0.5f;
            s.EdgeRightOffPastHalfWidth = Mathf.Max(0f, lateralAvg - roadHalfWidth);
            float zRel = Mathf.DeltaAngle(road.eulerAngles.z, edgeR.eulerAngles.z);
            s.EdgeZTilt = Mathf.Abs(zRel);
            s.EdgeMaterial = GetSharedMaterial(edgeL);
        }

        if (guideL != null && guideR != null)
        {
            s.GuideWidth     = guideL.localScale.x;
            s.GuideThickness = guideL.localScale.y;
            s.GuideLengthPad = Mathf.Max(0f, guideL.localScale.z - road.localScale.z) + 0.8f;
            s.GuideUpOff     = ((guideL.position.y + guideR.position.y) * 0.5f) - road.position.y;
            float rightOffL = ProjectOnRoadRight(guideL.position - road.position, road.eulerAngles.y);
            float rightOffR = ProjectOnRoadRight(guideR.position - road.position, road.eulerAngles.y);
            float lateralAvg = (Mathf.Abs(rightOffL) + Mathf.Abs(rightOffR)) * 0.5f;
            s.GuideRightOffPastHalfWidth = Mathf.Max(0f, lateralAvg - roadHalfWidth);
            float zRel = Mathf.DeltaAngle(road.eulerAngles.z, guideR.eulerAngles.z);
            s.GuideZTilt = Mathf.Abs(zRel);
            s.GuideMaterial = GetSharedMaterial(guideL);
        }

        if (shelfL != null && shelfR != null)
        {
            s.ShelfWidth     = shelfL.localScale.x;
            s.ShelfThickness = shelfL.localScale.y;
            s.ShelfLengthPad = Mathf.Max(0f, shelfL.localScale.z - road.localScale.z) + 0.8f;
            s.ShelfUpOff     = ((shelfL.position.y + shelfR.position.y) * 0.5f) - road.position.y;
            float rightOffL = ProjectOnRoadRight(shelfL.position - road.position, road.eulerAngles.y);
            float rightOffR = ProjectOnRoadRight(shelfR.position - road.position, road.eulerAngles.y);
            float lateralAvg = (Mathf.Abs(rightOffL) + Mathf.Abs(rightOffR)) * 0.5f;
            s.ShelfRightOffPastHalfWidth = Mathf.Max(0f, lateralAvg - roadHalfWidth);
            float zRel = Mathf.DeltaAngle(road.eulerAngles.z, shelfR.eulerAngles.z);
            s.ShelfZTilt = Mathf.Abs(zRel);
            s.ShelfMaterial = GetSharedMaterial(shelfL);
        }

        s.RoadMaterial = GetSharedMaterial(road);
        return s;
    }

    private static float ProjectOnRoadRight(Vector3 worldDelta, float roadYawDeg)
    {
        float yaw = roadYawDeg * Mathf.Deg2Rad;
        Vector3 right = new Vector3(Mathf.Cos(yaw), 0f, -Mathf.Sin(yaw));
        return Vector3.Dot(worldDelta, right);
    }

    private static Material GetSharedMaterial(Transform t)
    {
        if (t == null) return null;
        Renderer r = t.GetComponent<Renderer>();
        return r != null ? r.sharedMaterial : null;
    }

    // ---------------------------------------------------------------------
    // Resolution
    // ---------------------------------------------------------------------
    private static bool ResolveTargets(
        out Transform courseRoot,
        out Transform roadGroup,
        out Transform baseGroup,
        out Transform edgeGroup,
        out Transform guideGroup,
        out Transform shelfGroup,
        out MeshFilter[] placeholderMeshes,
        out StyleTemplate style)
    {
        courseRoot = null; roadGroup = null; baseGroup = null; edgeGroup = null;
        guideGroup = null; shelfGroup = null; placeholderMeshes = null; style = default;

        GameObject courseGo = GameObject.Find(CourseRoot);
        if (courseGo == null) { Debug.LogError($"[PRS] Cannot find '{CourseRoot}'"); return false; }
        courseRoot = courseGo.transform;

        roadGroup = courseRoot.Find("RoverRoad");
        if (roadGroup == null) { Debug.LogError("[PRS] Missing TerrainRoverCourse/RoverRoad"); return false; }

        // Find duplicate-named groups by inspecting children prefix.
        Transform foundBase = null, foundEdge = null, foundGuide = null, foundShelf = null;
        for (int i = 0; i < courseRoot.childCount; i++)
        {
            Transform c = courseRoot.GetChild(i);
            if (c.name == "RoverRoad_Base")
            {
                if (HasChildWithPrefix(c, "Base_Road_"))      foundBase = c;
                else if (HasChildWithPrefix(c, "Edge_Road_")) foundEdge = c;
            }
            else if (c.name == "RoverRoad_GuideRails")
            {
                if (HasChildWithPrefix(c, "Guide_Road_"))      foundGuide = c;
                else if (HasChildWithPrefix(c, "Shelf_Road_")) foundShelf = c;
            }
        }
        baseGroup  = foundBase;
        edgeGroup  = foundEdge;
        guideGroup = foundGuide;
        shelfGroup = foundShelf;

        if (baseGroup  == null) { Debug.LogError("[PRS] Could not identify Base group (RoverRoad_Base containing Base_Road_*)"); return false; }
        if (edgeGroup  == null) { Debug.LogError("[PRS] Could not identify Edge group (RoverRoad_Base containing Edge_Road_*)"); return false; }
        if (guideGroup == null) { Debug.LogError("[PRS] Could not identify Guide group (RoverRoad_GuideRails containing Guide_Road_*)"); return false; }
        if (shelfGroup == null) { Debug.LogError("[PRS] Could not identify Shelf group (RoverRoad_GuideRails containing Shelf_Road_*)"); return false; }

        GameObject placeholderGo = GameObject.Find(PlaceholderPath);
        if (placeholderGo == null) { Debug.LogError($"[PRS] Cannot find '{PlaceholderPath}'"); return false; }
        placeholderMeshes = placeholderGo.GetComponentsInChildren<MeshFilter>(true);
        if (placeholderMeshes.Length == 0) { Debug.LogError("[PRS] Placeholder has no MeshFilters"); return false; }

        style = CaptureStyleFromScene(roadGroup);
        Debug.Log(
            $"[PRS] Style captured: roadThk={style.RoadThickness:F3}  baseExtra={style.BaseExtraWidth:F2} " +
            $"edgeOff={style.EdgeRightOffPastHalfWidth:F2} guideOff={style.GuideRightOffPastHalfWidth:F2} " +
            $"guideTilt={style.GuideZTilt:F1} shelfOff={style.ShelfRightOffPastHalfWidth:F2}");
        return true;
    }

    private static bool HasChildWithPrefix(Transform group, string prefix)
    {
        if (group == null) return false;
        int n = Mathf.Min(group.childCount, 10);
        for (int i = 0; i < n; i++)
            if (group.GetChild(i).name.StartsWith(prefix))
                return true;
        return false;
    }

    private static int RemoveChildrenWithPrefix(Transform group, string prefix)
    {
        if (group == null) return 0;
        List<GameObject> doomed = new List<GameObject>();
        for (int i = 0; i < group.childCount; i++)
        {
            Transform c = group.GetChild(i);
            if (c.name.StartsWith(prefix)) doomed.Add(c.gameObject);
        }
        for (int i = 0; i < doomed.Count; i++) Undo.DestroyObjectImmediate(doomed[i]);
        return doomed.Count;
    }

    // ---------------------------------------------------------------------
    // Centerline sampling along the placeholder road mesh
    // ---------------------------------------------------------------------
    private struct CenterlineSample
    {
        public Vector3 position; // (centerX, midY, centerZ) in world space
        public float topY;       // road TOP surface Y in world space
        public float width;      // estimated road width in metres
    }

    private static List<CenterlineSample> SampleCenterline(MeshFilter[] meshes)
    {
        List<Vector3> verts = new List<Vector3>(8192);
        for (int i = 0; i < meshes.Length; i++)
        {
            MeshFilter mf = meshes[i];
            if (mf == null || mf.sharedMesh == null) continue;
            Transform t = mf.transform;
            Vector3[] vs = mf.sharedMesh.vertices;
            for (int v = 0; v < vs.Length; v++)
                verts.Add(t.TransformPoint(vs[v]));
        }
        if (verts.Count == 0) return new List<CenterlineSample>();

        // Pick the dominant XZ axis (longer extent) for slicing.
        float minX = float.MaxValue, maxX = -float.MaxValue;
        float minZ = float.MaxValue, maxZ = -float.MaxValue;
        for (int i = 0; i < verts.Count; i++)
        {
            Vector3 v = verts[i];
            if (v.x < minX) minX = v.x; if (v.x > maxX) maxX = v.x;
            if (v.z < minZ) minZ = v.z; if (v.z > maxZ) maxZ = v.z;
        }
        bool sliceAlongZ = (maxZ - minZ) >= (maxX - minX);

        float axisMin = sliceAlongZ ? minZ : minX;
        float axisMax = sliceAlongZ ? maxZ : maxX;
        float length  = axisMax - axisMin;
        int sliceCount = Mathf.Max(2, Mathf.CeilToInt(length / CenterlineStep));

        // Bin vertices by slice index.
        List<Vector3>[] bins = new List<Vector3>[sliceCount];
        float invStep = sliceCount / length;
        for (int i = 0; i < verts.Count; i++)
        {
            float a = sliceAlongZ ? verts[i].z : verts[i].x;
            int idx = Mathf.Clamp(Mathf.FloorToInt((a - axisMin) * invStep), 0, sliceCount - 1);
            if (bins[idx] == null) bins[idx] = new List<Vector3>(16);
            bins[idx].Add(verts[i]);
        }

        // For each bin compute centerline sample.
        List<CenterlineSample> samples = new List<CenterlineSample>(sliceCount);
        for (int s = 0; s < sliceCount; s++)
        {
            List<Vector3> bin = bins[s];
            if (bin == null || bin.Count < CrossSliceMin) continue;

            // Use the cross-section's TOP surface only: take top Y-half of vertices.
            float maxY = float.MinValue, minY = float.MaxValue;
            for (int i = 0; i < bin.Count; i++) { if (bin[i].y > maxY) maxY = bin[i].y; if (bin[i].y < minY) minY = bin[i].y; }
            float yCut = Mathf.Lerp(minY, maxY, 0.6f); // keep upper 40% as "top surface"

            float sumX = 0f, sumZ = 0f, topY = float.MinValue;
            int kept = 0;
            float perpMin = float.MaxValue, perpMax = float.MinValue;
            for (int i = 0; i < bin.Count; i++)
            {
                Vector3 v = bin[i];
                if (v.y < yCut) continue;
                sumX += v.x; sumZ += v.z;
                if (v.y > topY) topY = v.y;
                float perp = sliceAlongZ ? v.x : v.z;
                if (perp < perpMin) perpMin = perp;
                if (perp > perpMax) perpMax = perp;
                kept++;
            }
            if (kept < CrossSliceMin) continue;

            CenterlineSample cs;
            cs.position = new Vector3(sumX / kept, topY, sumZ / kept);
            cs.topY     = topY;
            cs.width    = Mathf.Max(2f, perpMax - perpMin);
            samples.Add(cs);
        }

        // Sort by axis position so segments connect correctly even if bins were filled unevenly.
        if (sliceAlongZ) samples = samples.OrderBy(s => s.position.z).ToList();
        else             samples = samples.OrderBy(s => s.position.x).ToList();

        // Smooth widths a bit (3-tap moving average) - cross-section width is noisy.
        if (samples.Count >= 3)
        {
            float[] w = new float[samples.Count];
            for (int i = 0; i < samples.Count; i++) w[i] = samples[i].width;
            for (int i = 1; i < samples.Count - 1; i++)
            {
                CenterlineSample cs = samples[i];
                cs.width = (w[i - 1] + w[i] + w[i + 1]) / 3f;
                samples[i] = cs;
            }
        }
        return samples;
    }

    // ---------------------------------------------------------------------
    // Cube spawning
    // ---------------------------------------------------------------------
    private static Transform SpawnCube(Transform parent, string name, Vector3 worldPos, Quaternion worldRot, Vector3 worldScale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = worldPos;
        go.transform.rotation = worldRot;
        go.transform.localScale = worldScale;
        if (mat != null)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;
        }
        Undo.RegisterCreatedObjectUndo(go, "Spawn Placeholder Companion");
        return go.transform;
    }
}
#endif
