using UnityEngine;

public class WaterDebugHelper : MonoBehaviour
{
    private Material material;
    private MeshFilter meshFilter;
    
    void Start()
    {
        Debug.Log("=== WATER DEBUG START ===");
        
        // Check mesh
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.mesh != null)
        {
            Debug.Log($"Mesh found: {meshFilter.mesh.vertexCount} vertices");
        }
        else
        {
            Debug.LogError("NO MESH FOUND!");
        }
        
        // Check material and make it RED for visibility
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            material = renderer.material;
            Debug.Log("Material found - making it RED for visibility");
            material.SetColor("_BaseColor", Color.red);
        }
        else
        {
            Debug.LogError("NO RENDERER FOUND!");
        }
        
        // Check for wave script
        CaveWaterWaves waveScript = GetComponent<CaveWaterWaves>();
        if (waveScript != null)
        {
            Debug.Log($"CaveWaterWaves found! Settings: height={waveScript.waveHeight}, speed={waveScript.waveSpeed}");
        }
        else
        {
            Debug.LogError("CaveWaterWaves script NOT FOUND!");
        }
        
        Debug.Log($"Water position: {transform.position}");
        Debug.Log("=== WATER DEBUG END ===");
    }
    
    void Update()
    {
        // Flash between red and yellow every second so we can see it's animating
        if (material != null)
        {
            float t = Mathf.PingPong(Time.time, 1f);
            material.SetColor("_BaseColor", Color.Lerp(Color.red, Color.yellow, t));
        }
        
        // Log mesh vertex count every 2 seconds to see if subdivision happened
        if (Time.frameCount % 120 == 0 && meshFilter != null && meshFilter.mesh != null)
        {
            Debug.Log($"Current vertex count: {meshFilter.mesh.vertexCount}");
        }
    }
}
