using UnityEngine;
using UnityEditor;

public class FixOceanFloorTerrain
{
    [MenuItem("Tools/Fix Ocean Floor Terrain Size")]
    public static void FixTerrainSize()
    {
        // Find the Ocean Floor Terrain
        GameObject terrainObj = GameObject.Find("Ocean Floor Terrain");
        if (terrainObj == null)
        {
            Debug.LogError("Ocean Floor Terrain not found!");
            return;
        }
        
        Terrain terrain = terrainObj.GetComponent<Terrain>();
        if (terrain == null || terrain.terrainData == null)
        {
            Debug.LogError("Terrain component or terrain data not found!");
            return;
        }
        
        TerrainData terrainData = terrain.terrainData;
        
        // Set the terrain size to match the plane (100x100 units, with some height)
        terrainData.size = new Vector3(100f, 20f, 100f);
        
        // Reset the terrain position to origin
        terrainObj.transform.position = Vector3.zero;
        
        // Create simple ocean floor heightmap
        int resolution = terrainData.heightmapResolution;
        float[,] heights = new float[resolution, resolution];
        
        // Create varied ocean floor using Perlin noise
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                float xCoord = (float)x / resolution * 3f;
                float yCoord = (float)y / resolution * 3f;
                
                // Create ocean floor with gentle variations
                float height = 0f;
                
                // Large underwater dunes/ridges
                height += Mathf.PerlinNoise(xCoord * 0.3f, yCoord * 0.3f) * 0.1f;
                
                // Medium details (rocky formations)
                height += Mathf.PerlinNoise(xCoord * 1.5f, yCoord * 1.5f) * 0.05f;
                
                // Fine details (sand ripples)
                height += Mathf.PerlinNoise(xCoord * 6f, yCoord * 6f) * 0.02f;
                
                heights[x, y] = Mathf.Clamp01(height);
            }
        }
        
        terrainData.SetHeights(0, 0, heights);
        
        // Refresh the terrain
        terrain.Flush();
        
        Debug.Log("Ocean Floor Terrain size fixed to 100x100 units and positioned at origin with ocean floor topology.");
    }
}