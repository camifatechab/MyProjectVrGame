using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class FixLandWaterEdges : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform waterContainer;
    [SerializeField] private Transform landSurfaces;
    
    [Header("Settings")]
    [SerializeField] private float edgeOverlap = 0.1f;
    [SerializeField] private bool autoFindReferences = true;
    
    [Header("Water Container Info")]
    [SerializeField] private Vector3 waterContainerPosition = Vector3.zero;
    [SerializeField] private Vector3 waterContainerScale = new Vector3(2f, 2f, 2f);
    
    [Header("Land Info")]
    [SerializeField] private Vector3 landPosition = Vector3.zero;
    [SerializeField] private Vector3 landScale = new Vector3(2f, 2f, 2f);
    
    private void OnValidate()
    {
        if (autoFindReferences)
        {
            if (waterContainer == null)
            {
                GameObject water = GameObject.Find("WaterContainer");
                if (water != null) waterContainer = water.transform;
            }
            
            if (landSurfaces == null)
            {
                GameObject land = GameObject.Find("LandSurfaces");
                if (land != null) landSurfaces = land.transform;
            }
        }
    }
    
#if UNITY_EDITOR
    [ContextMenu("Fix Land-Water Edges")]
    public void FixEdges()
    {
        if (waterContainer == null || landSurfaces == null)
        {
            Debug.LogError("Water Container or Land Surfaces not assigned!");
            return;
        }
        
        // Get the water walls
        Transform northWall = waterContainer.Find("NorthWall");
        Transform southWall = waterContainer.Find("SouthWall");
        Transform eastWall = waterContainer.Find("EastWall");
        Transform westWall = waterContainer.Find("WestWall");
        
        // Get land surfaces
        Transform landNorth = landSurfaces.Find("LandSurface_North");
        Transform landSouth = landSurfaces.Find("LandSurface_South");
        Transform landEast = landSurfaces.Find("LandSurface_East");
        Transform landWest = landSurfaces.Find("LandSurface_West");
        
        // North edge - extend land surface to overlap with water wall
        if (landNorth != null && northWall != null)
        {
            Vector3 landPos = landNorth.localPosition;
            landPos.z = 37.5f - edgeOverlap; // Move slightly closer to water
            landNorth.localPosition = landPos;
            
            Vector3 landScl = landNorth.localScale;
            landScl.z = 25f + edgeOverlap * 2; // Extend to overlap
            landNorth.localScale = landScl;
        }
        
        // South edge
        if (landSouth != null && southWall != null)
        {
            Vector3 landPos = landSouth.localPosition;
            landPos.z = -37.5f + edgeOverlap;
            landSouth.localPosition = landPos;
            
            Vector3 landScl = landSouth.localScale;
            landScl.z = 25f + edgeOverlap * 2;
            landSouth.localScale = landScl;
        }
        
        // East edge
        if (landEast != null && eastWall != null)
        {
            Vector3 landPos = landEast.localPosition;
            landPos.x = 37.5f - edgeOverlap;
            landEast.localPosition = landPos;
            
            Vector3 landScl = landEast.localScale;
            landScl.x = 25f + edgeOverlap * 2;
            landEast.localScale = landScl;
        }
        
        // West edge
        if (landWest != null && westWall != null)
        {
            Vector3 landPos = landWest.localPosition;
            landPos.x = -37.5f + edgeOverlap;
            landWest.localPosition = landPos;
            
            Vector3 landScl = landWest.localScale;
            landScl.x = 25f + edgeOverlap * 2;
            landWest.localScale = landScl;
        }
        
        // Fix corner pieces
        FixCorner("LandSurface_NE_Corner", 37.5f - edgeOverlap, 37.5f - edgeOverlap);
        FixCorner("LandSurface_NW_Corner", -37.5f + edgeOverlap, 37.5f - edgeOverlap);
        FixCorner("LandSurface_SE_Corner", 37.5f - edgeOverlap, -37.5f + edgeOverlap);
        FixCorner("LandSurface_SW_Corner", -37.5f + edgeOverlap, -37.5f + edgeOverlap);
        
        Debug.Log("✅ Land-Water edges fixed! Overlap: " + edgeOverlap);
        EditorUtility.SetDirty(landSurfaces.gameObject);
    }
    
    private void FixCorner(string cornerName, float xPos, float zPos)
    {
        Transform corner = landSurfaces.Find(cornerName);
        if (corner != null)
        {
            Vector3 pos = corner.localPosition;
            pos.x = xPos;
            pos.z = zPos;
            corner.localPosition = pos;
            
            Vector3 scl = corner.localScale;
            scl.x = 25f + edgeOverlap * 2;
            scl.z = 25f + edgeOverlap * 2;
            corner.localScale = scl;
        }
    }
    
    [ContextMenu("Add Edge Colliders")]
    public void AddEdgeColliders()
    {
        if (waterContainer == null)
        {
            Debug.LogError("Water Container not assigned!");
            return;
        }
        
        // Add colliders to water walls to prevent falling through edges
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
                }
                collider.isTrigger = false; // Make it solid
            }
        }
        
        Debug.Log("✅ Edge colliders added!");
    }
    
    [ContextMenu("Create Seamless Edge Mesh")]
    public void CreateSeamlessEdgeMesh()
    {
        // Create a thin strip mesh that covers the seam
        GameObject edgeStrip = new GameObject("EdgeSeamCover");
        edgeStrip.transform.SetParent(waterContainer);
        edgeStrip.transform.localPosition = Vector3.zero;
        edgeStrip.transform.localRotation = Quaternion.identity;
        edgeStrip.transform.localScale = Vector3.one;
        
        MeshFilter meshFilter = edgeStrip.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = edgeStrip.AddComponent<MeshRenderer>();
        
        // Create a simple quad mesh for the seam cover
        Mesh mesh = new Mesh();
        mesh.name = "SeamCoverMesh";
        
        // This is a placeholder - you'd generate proper geometry here
        mesh.vertices = new Vector3[]
        {
            new Vector3(-25, 0, 25),
            new Vector3(25, 0, 25),
            new Vector3(25, 0, -25),
            new Vector3(-25, 0, -25)
        };
        
        mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        
        meshFilter.sharedMesh = mesh;
        
        // Try to match the land material
        if (landSurfaces != null)
        {
            Transform landSurface = landSurfaces.Find("LandSurfaces");
            if (landSurface != null)
            {
                MeshRenderer landRenderer = landSurface.GetComponent<MeshRenderer>();
                if (landRenderer != null && landRenderer.sharedMaterial != null)
                {
                    meshRenderer.sharedMaterial = landRenderer.sharedMaterial;
                }
            }
        }
        
        Debug.Log("✅ Seamless edge mesh created!");
    }
#endif
}
