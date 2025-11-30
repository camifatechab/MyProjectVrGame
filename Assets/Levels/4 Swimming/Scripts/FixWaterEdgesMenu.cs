using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FixWaterEdgesMenu : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Lake/Fix Water-Land Edges")]
    public static void FixWaterLandEdges()
    {
        GameObject waterContainer = GameObject.Find("WaterContainer");
        GameObject landSurfaces = GameObject.Find("LandSurfaces");
        
        if (waterContainer == null || landSurfaces == null)
        {
            Debug.LogError("Could not find WaterContainer or LandSurfaces!");
            return;
        }
        
        Transform landParent = landSurfaces.transform;
        float overlap = 0.25f; // Small overlap to eliminate gaps
        
        // Adjust each land surface to overlap slightly with water walls
        AdjustLandPiece(landParent, "LandSurface_North", 0f, 37.3f, 50f, 25.5f);
        AdjustLandPiece(landParent, "LandSurface_South", 0f, -37.3f, 50f, 25.5f);
        AdjustLandPiece(landParent, "LandSurface_East", 37.3f, 0f, 25.5f, 50f);
        AdjustLandPiece(landParent, "LandSurface_West", -37.3f, 0f, 25.5f, 50f);
        
        // Fix corners
        AdjustLandPiece(landParent, "LandSurface_NE_Corner", 37.3f, 37.3f, 25.5f, 25.5f);
        AdjustLandPiece(landParent, "LandSurface_NW_Corner", -37.3f, 37.3f, 25.5f, 25.5f);
        AdjustLandPiece(landParent, "LandSurface_SE_Corner", 37.3f, -37.3f, 25.5f, 25.5f);
        AdjustLandPiece(landParent, "LandSurface_SW_Corner", -37.3f, -37.3f, 25.5f, 25.5f);
        
        // Make sure water walls have proper colliders
        EnsureWallColliders(waterContainer.transform);
        
        Debug.Log("✅ Water-Land edges fixed! All gaps should be closed.");
        EditorUtility.SetDirty(landSurfaces);
    }
    
    private static void AdjustLandPiece(Transform parent, string name, float localX, float localZ, float scaleX, float scaleZ)
    {
        Transform piece = parent.Find(name);
        if (piece != null)
        {
            Vector3 pos = piece.localPosition;
            pos.x = localX;
            pos.z = localZ;
            piece.localPosition = pos;
            
            Vector3 scale = piece.localScale;
            scale.x = scaleX;
            scale.z = scaleZ;
            piece.localScale = scale;
            
            EditorUtility.SetDirty(piece.gameObject);
        }
    }
    
    private static void EnsureWallColliders(Transform waterContainer)
    {
        string[] wallNames = { "NorthWall", "SouthWall", "EastWall", "WestWall" };
        
        foreach (string wallName in wallNames)
        {
            Transform wall = waterContainer.Find(wallName);
            if (wall != null)
            {
                BoxCollider collider = wall.GetComponent<BoxCollider>();
                if (collider == null)
                {
                    collider = wall.gameObject.AddComponent<BoxCollider>();
                    collider.isTrigger = false;
                    EditorUtility.SetDirty(wall.gameObject);
                }
            }
        }
    }
    
    [MenuItem("Tools/Lake/Reset Land-Water Positions")]
    public static void ResetLandWaterPositions()
    {
        GameObject landSurfaces = GameObject.Find("LandSurfaces");
        
        if (landSurfaces == null)
        {
            Debug.LogError("Could not find LandSurfaces!");
            return;
        }
        
        Transform landParent = landSurfaces.transform;
        
        // Reset to original positions
        AdjustLandPiece(landParent, "LandSurface_North", 0f, 37.5f, 50f, 25f);
        AdjustLandPiece(landParent, "LandSurface_South", 0f, -37.5f, 50f, 25f);
        AdjustLandPiece(landParent, "LandSurface_East", 37.5f, 0f, 25f, 50f);
        AdjustLandPiece(landParent, "LandSurface_West", -37.5f, 0f, 25f, 50f);
        
        AdjustLandPiece(landParent, "LandSurface_NE_Corner", 37.5f, 37.5f, 25f, 25f);
        AdjustLandPiece(landParent, "LandSurface_NW_Corner", -37.5f, 37.5f, 25f, 25f);
        AdjustLandPiece(landParent, "LandSurface_SE_Corner", 37.5f, -37.5f, 25f, 25f);
        AdjustLandPiece(landParent, "LandSurface_SW_Corner", -37.5f, -37.5f, 25f, 25f);
        
        Debug.Log("✅ Land surfaces reset to original positions.");
        EditorUtility.SetDirty(landSurfaces);
    }
    
    [MenuItem("Tools/Lake/Create Edge Blend Material")]
    public static void CreateEdgeBlendMaterial()
    {
        // Create a material that helps blend the edges
        Material blendMat = new Material(Shader.Find("Standard"));
        blendMat.name = "LandWaterEdgeBlend";
        blendMat.color = new Color(0.7f, 0.6f, 0.5f); // Sandy/earthy color
        
        string path = "Assets/Lake/Materials/LandWaterEdgeBlend.mat";
        AssetDatabase.CreateAsset(blendMat, path);
        AssetDatabase.SaveAssets();
        
        Debug.Log("✅ Edge blend material created at: " + path);
        Selection.activeObject = blendMat;
    }
#endif
}
