using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class SimpleWaterWaves : MonoBehaviour
{
    [Header("Wave Settings")]
    [Range(0.01f, 2f)]
    public float waveHeight = 0.4f;
    
    [Range(0.1f, 3f)]
    public float waveSpeed = 0.8f;
    
    [Range(0.5f, 5f)]
    public float waveFrequency = 2.0f;
    
    private Mesh mesh;
    private Vector3[] baseVertices;
    private Vector3[] vertices;
    
    void Start()
    {
        Debug.Log("=== SimpleWaterWaves START ===");
        
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        mesh = meshFilter.mesh;
        
        if (mesh == null)
        {
            Debug.LogError("No mesh found!");
            return;
        }
        
        // Store base vertices
        baseVertices = mesh.vertices;
        vertices = new Vector3[baseVertices.Length];
        
        Debug.Log($"SimpleWaterWaves: Mesh has {baseVertices.Length} vertices");
        Debug.Log($"SimpleWaterWaves: Wave settings - height:{waveHeight}, speed:{waveSpeed}, freq:{waveFrequency}");
    }
    
    void Update()
    {
        if (baseVertices == null || baseVertices.Length == 0)
            return;
        
        // Animate each vertex
        for (int i = 0; i < baseVertices.Length; i++)
        {
            Vector3 baseVert = baseVertices[i];
            
            // Calculate wave offset
            float wave = Mathf.Sin((baseVert.x * waveFrequency) + (Time.time * waveSpeed)) * waveHeight;
            float wave2 = Mathf.Sin((baseVert.z * waveFrequency * 0.8f) + (Time.time * waveSpeed * 1.2f)) * waveHeight * 0.5f;
            
            // Apply to Y position
            vertices[i] = baseVert;
            vertices[i].y += wave + wave2;
        }
        
        // Update mesh
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        // Debug log every 120 frames (2 seconds at 60fps)
        if (Time.frameCount % 120 == 0)
        {
            Debug.Log($"Wave animating: frame {Time.frameCount}, max offset: {waveHeight}");
        }
    }
}
