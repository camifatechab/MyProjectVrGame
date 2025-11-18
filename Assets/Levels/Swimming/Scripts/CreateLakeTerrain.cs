using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[ExecuteInEditMode]
public class CreateLakeTerrain : MonoBehaviour
{
    [ContextMenu("Create Lake Terrain")]
    public void Create()
    {
        // Create TerrainData
        TerrainData terrainData = new TerrainData();
        terrainData.size = new Vector3(50, 5, 50);
        terrainData.heightmapResolution = 129;
        
        // Flatten terrain
        float[,] heights = new float[129, 129];
        for (int y = 0; y < 129; y++)
        {
            for (int x = 0; x < 129; x++)
            {
                heights[y, x] = 0f;
            }
        }
        terrainData.SetHeights(0, 0, heights);
        
        // Save asset
        AssetDatabase.CreateAsset(terrainData, "Assets/Lake/LakeTerrainData.asset");
        AssetDatabase.SaveAssets();
        
        // Assign to this object's Terrain component
        Terrain terrain = GetComponent<Terrain>();
        if (terrain != null)
        {
            terrain.terrainData = terrainData;
            transform.position = new Vector3(-25f, -20f, -25f);
        }
        
        Debug.Log("Lake Terrain created at Assets/Lake/LakeTerrainData.asset");
    }
}
#endif
