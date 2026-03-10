using UnityEngine;

/// Procedurally builds a rectangular loop test track.
/// Rover starts at (0,0,-6) facing +Z, enters from the bottom gap.
/// Right-click this component → "Rebuild Track" to regenerate in Edit mode.
[ExecuteInEditMode]
public class TestTrackBuilder : MonoBehaviour
{
    [Header("Track Shape")]
    public float laneWidth   = 8f;    // width of each lane
    public float trackLength = 50f;   // total length (Z axis)
    public float wallHeight  = 2.5f;  // how tall the barrier walls are
    public float wallThick   = 0.5f;

    void Awake()
    {
        if (transform.childCount == 0)
            Build();
    }

    [ContextMenu("Rebuild Track")]
    public void Build()
    {
        // Clear old geometry
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        // Derived measurements
        float hw      = laneWidth * 0.5f;       // 4  — inner half-width
        float ow      = laneWidth * 1.5f;       // 12 — outer wall X
        float zStart  = -6f;                    // rover Z position
        float zEnd    = zStart + trackLength;   // 44
        float zCenter = zStart + trackLength * 0.5f; // 19

        // ── Outer boundary walls ──────────────────────────────────────────
        Wall("Wall_Outer_Right",  new Vector3( ow,  0, zCenter), new Vector3(wallThick, wallHeight, trackLength), WallColor());
        Wall("Wall_Outer_Left",   new Vector3(-ow,  0, zCenter), new Vector3(wallThick, wallHeight, trackLength), WallColor());
        Wall("Wall_Outer_Top",    new Vector3(  0,  0, zEnd),    new Vector3(ow * 2 + wallThick, wallHeight, wallThick), WallColor());

        // Bottom outer — two segments leaving a gap at center for rover entry
        float gapHalf = hw; // 4 — entry gap goes from x=-4 to x=4
        float segW    = ow - gapHalf; // 8
        Wall("Wall_Outer_BotRight", new Vector3( ow - segW * 0.5f, 0, zStart), new Vector3(segW, wallHeight, wallThick), WallColor());
        Wall("Wall_Outer_BotLeft",  new Vector3(-ow + segW * 0.5f, 0, zStart), new Vector3(segW, wallHeight, wallThick), WallColor());

        // ── Inner island walls ────────────────────────────────────────────
        float innerLen  = trackLength - laneWidth * 2f; // 34
        float innerZCtr = zCenter;
        float innerZBot = innerZCtr - innerLen * 0.5f;  // z = 2
        float innerZTop = innerZCtr + innerLen * 0.5f;  // z = 36

        Wall("Wall_Inner_Right",  new Vector3( hw, 0, innerZCtr), new Vector3(wallThick, wallHeight, innerLen), IslandColor());
        Wall("Wall_Inner_Left",   new Vector3(-hw, 0, innerZCtr), new Vector3(wallThick, wallHeight, innerLen), IslandColor());
        Wall("Wall_Inner_Top",    new Vector3(  0, 0, innerZTop), new Vector3(hw * 2 + wallThick, wallHeight, wallThick), IslandColor());
        Wall("Wall_Inner_Bottom", new Vector3(  0, 0, innerZBot), new Vector3(hw * 2 + wallThick, wallHeight, wallThick), IslandColor());

        // ── Road surface (visual only — no collider) ──────────────────────
        float roadX    = (hw + ow) * 0.5f; // 8 — center of each lane
        float roadW    = ow - hw;           // 8 — lane width
        Road("Road_Right",  new Vector3( roadX, 0, zCenter), new Vector3(roadW, 0.05f, trackLength));
        Road("Road_Left",   new Vector3(-roadX, 0, zCenter), new Vector3(roadW, 0.05f, trackLength));
        Road("Road_Top",    new Vector3(0,  0, zEnd   - laneWidth * 0.5f), new Vector3(ow * 2, 0.05f, laneWidth));
        Road("Road_Bottom", new Vector3(0,  0, zStart + laneWidth * 0.5f), new Vector3(ow * 2, 0.05f, laneWidth));

        // ── Start/finish line marker ──────────────────────────────────────
        Road("StartLine", new Vector3(0, 0.01f, zStart + laneWidth), new Vector3(ow * 2, 0.05f, 0.4f));
        var sl = transform.Find("StartLine");
        if (sl != null)
            sl.GetComponent<Renderer>().sharedMaterial.color = new Color(1f, 1f, 0f); // yellow

        Debug.Log("[TestTrack] Built. Drive in from the bottom-center gap!");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    void Wall(string n, Vector3 pos, Vector3 size, Color col)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = n;
        go.transform.SetParent(transform, false);
        go.transform.localPosition = pos;
        go.transform.localScale    = new Vector3(size.x, size.y, size.z);
        // Raise so base sits on y=0
        var p = go.transform.localPosition;
        p.y = size.y * 0.5f;
        go.transform.localPosition = p;

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = col;
        go.GetComponent<Renderer>().material = mat;
    }

    void Road(string n, Vector3 pos, Vector3 size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = n;
        go.transform.SetParent(transform, false);
        go.transform.localPosition = pos;
        go.transform.localScale    = size;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.18f, 0.18f, 0.18f);
        go.GetComponent<Renderer>().material = mat;
        // Road is visual only
        DestroyImmediate(go.GetComponent<Collider>());
    }

    Color WallColor()   => new Color(0.85f, 0.85f, 0.85f); // light grey
    Color IslandColor() => new Color(0.5f,  0.75f, 0.5f);  // soft green island
}
