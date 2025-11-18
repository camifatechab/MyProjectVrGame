using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class FixLandWaterSeams : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform waterContainer;
    [SerializeField] private Transform landSurfaces;
    
    [Header("Settings")]
    [SerializeField] private float overlapAmount = 0.1f;
    [SerializeField] private bool fixOnStart = true;
    
    private void Start()
    {
        if (fixOnStart)
        {
            FixSeams();
        }
    }
    
    public void FixSeams()
    {
        // Auto-find references if not assigned
        if (waterContainer == null)
        {
            GameObject waterObj = GameObject.Find("WaterContainer");
            if (waterObj != null) waterContainer = waterObj.transform;
        }
        
        if (landSurfaces == null)
        {
            GameObject landObj = GameObject.Find("LandSurfaces");
            if (landObj != null) landSurfaces = landObj.transform;
        }
        
        if (waterContainer == null || landSurfaces == null)
        {
            Debug.LogError("FixLandWaterSeams: Could not find WaterContainer or LandSurfaces!");
            return;
        }
        
        // Get water bounds
        Vector3 waterScale = waterContainer.localScale;
        Vector3 waterPos = waterContainer.position;
        
        // Find all walls in water container
        Transform northWall = waterContainer.Find("NorthWall");
        Transform southWall = waterContainer.Find("SouthWall");
        Transform eastWall = waterContainer.Find("EastWall");
        Transform westWall = waterContainer.Find("WestWall");
        
        // Adjust land surfaces to overlap slightly with water walls
        AdjustLandSurface("LandSurface_North", new Vector3(0, 0, overlapAmount), Vector3.zero);
        AdjustLandSurface("LandSurface_South", new Vector3(0, 0, -overlapAmount), Vector3.zero);
        AdjustLandSurface("LandSurface_East", new Vector3(overlapAmount, 0, 0), Vector3.zero);
        AdjustLandSurface("LandSurface_West", new Vector3(-overlapAmount, 0, 0), Vector3.zero);
        
        // Adjust corner pieces
        AdjustLandSurface("LandSurface_NE_Corner", new Vector3(overlapAmount, 0, overlapAmount), Vector3.zero);
        AdjustLandSurface("LandSurface_NW_Corner", new Vector3(-overlapAmount, 0, overlapAmount), Vector3.zero);
        AdjustLandSurface("LandSurface_SE_Corner", new Vector3(overlapAmount, 0, -overlapAmount), Vector3.zero);
        AdjustLandSurface("LandSurface_SW_Corner", new Vector3(-overlapAmount, 0, -overlapAmount), Vector3.zero);
        
        // Also adjust wall heights to match land surface
        if (northWall != null) AdjustWallHeight(northWall);
        if (southWall != null) AdjustWallHeight(southWall);
        if (eastWall != null) AdjustWallHeight(eastWall);
        if (westWall != null) AdjustWallHeight(westWall);
        
        Debug.Log("Land-Water seams fixed successfully!");
    }
    
    private void AdjustLandSurface(string name, Vector3 positionOffset, Vector3 scaleOffset)
    {
        if (landSurfaces == null) return;
        
        Transform surface = landSurfaces.Find(name);
        if (surface != null)
        {
            surface.localPosition += positionOffset / landSurfaces.localScale.x;
            if (scaleOffset != Vector3.zero)
            {
                surface.localScale += scaleOffset;
            }
        }
    }
    
    private void AdjustWallHeight(Transform wall)
    {
        // Extend wall slightly upward to eliminate any gap
        Vector3 currentScale = wall.localScale;
        wall.localScale = new Vector3(currentScale.x, currentScale.y + 0.5f, currentScale.z);
        
        // Adjust position to keep bottom at same place
        Vector3 currentPos = wall.localPosition;
        wall.localPosition = new Vector3(currentPos.x, currentPos.y + 0.25f, currentPos.z);
    }
    
    public void ResetPositions()
    {
        // Reset to original positions (in case you need to undo)
        AdjustLandSurface("LandSurface_North", new Vector3(0, 0, -overlapAmount), Vector3.zero);
        AdjustLandSurface("LandSurface_South", new Vector3(0, 0, overlapAmount), Vector3.zero);
        AdjustLandSurface("LandSurface_East", new Vector3(-overlapAmount, 0, 0), Vector3.zero);
        AdjustLandSurface("LandSurface_West", new Vector3(overlapAmount, 0, 0), Vector3.zero);
        
        AdjustLandSurface("LandSurface_NE_Corner", new Vector3(-overlapAmount, 0, -overlapAmount), Vector3.zero);
        AdjustLandSurface("LandSurface_NW_Corner", new Vector3(overlapAmount, 0, -overlapAmount), Vector3.zero);
        AdjustLandSurface("LandSurface_SE_Corner", new Vector3(-overlapAmount, 0, overlapAmount), Vector3.zero);
        AdjustLandSurface("LandSurface_SW_Corner", new Vector3(overlapAmount, 0, overlapAmount), Vector3.zero);
        
        Debug.Log("Positions reset!");
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(FixLandWaterSeams))]
public class FixLandWaterSeamsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        FixLandWaterSeams script = (FixLandWaterSeams)target;
        
        EditorGUILayout.Space(10);
        
        if (GUILayout.Button("Fix Seams Now", GUILayout.Height(40)))
        {
            script.FixSeams();
        }
        
        EditorGUILayout.Space(5);
        
        if (GUILayout.Button("Reset Positions", GUILayout.Height(30)))
        {
            script.ResetPositions();
        }
        
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("Click 'Fix Seams Now' to automatically adjust land surfaces to overlap with water walls and eliminate visible gaps.", MessageType.Info);
    }
}
#endif
