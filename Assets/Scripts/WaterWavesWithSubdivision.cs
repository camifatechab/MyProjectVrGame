using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class WaterWavesWithSubdivision : MonoBehaviour
{
    [Header("Wave Settings")]
    [Range(0.01f, 2f)]
    public float waveHeight = 0.5f;
    
    [Range(0.1f, 3f)]
    public float waveSpeed = 1.0f;
    
    [Range(0.5f, 5f)]
    public float waveFrequency = 2.0f;
    
    [Header("Mesh Settings")]
    [Range(5, 50)]
    public int subdivisions = 20;
    
    private Mesh mesh;
    private Vector3[] baseVertices;
    private Vector3[] vertices;
    private bool initialized = false;
    
    void Start()
    {
        Debug.Log("=== WaterWavesWithSubdivision START ===");
        
        // Create subdivided mesh
        CreateSubdividedPlaneMesh();
        
        if (mesh == null || baseVertices == null)
        {
            Debug.LogError("Failed to create mesh!");
            return;
        }
        
        Debug.Log($"Subdivided mesh created: {baseVertices.Length} vertices");
        Debug.Log($"Wave settings: height={waveHeight}, speed={waveSpeed}, freq={waveFrequency}");
        
        initialized = true;
    }
    
    void CreateSubdividedPlaneMesh()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        
        // Create new mesh
        mesh = new Mesh();
        mesh.name = "Subdivided Water Plane";
        
        int gridSize = subdivisions;
        int vertexCount = (gridSize + 1) * (gridSize + 1);
        
        baseVertices = new Vector3[vertexCount];
        vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        int[] triangles = new int[gridSize * gridSize * 6];
        
        // Generate vertices
        int index = 0;
        for (int z = 0; z <= gridSize; z++)
        {
            for (int x = 0; x <= gridSize; x++)
            {
                float xPos = (x / (float)gridSize - 0.5f) * 10f;
                float zPos = (z / (float)gridSize - 0.5f) * 10f;
                
                baseVertices[index] = new Vector3(xPos, 0, zPos);
                uvs[index] = new Vector2(x / (float)gridSize, z / (float)gridSize);
                index++;
            }
        }
        
        // Generate triangles
        int triIndex = 0;
        for (int z = 0; z < gridSize; z++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                int vertIndex = z * (gridSize + 1) + x;
                
                triangles[triIndex++] = vertIndex;
                triangles[triIndex++] = vertIndex + gridSize + 1;
                triangles[triIndex++] = vertIndex + 1;
                
                triangles[triIndex++] = vertIndex + 1;
                triangles[triIndex++] = vertIndex + gridSize + 1;
                triangles[triIndex++] = vertIndex + gridSize + 2;
            }
        }
        
        mesh.vertices = baseVertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        meshFilter.mesh = mesh;
        
        Debug.Log($"Created mesh with {baseVertices.Length} vertices and {triangles.Length / 3} triangles");
    }
    
    void Update()
    {
        if (!initialized || baseVertices == null || baseVertices.Length == 0)
            return;
        
        // Animate each vertex
        for (int i = 0; i < baseVertices.Length; i++)
        {
            Vector3 baseVert = baseVertices[i];
            
            // Calculate wave offset
            float wave1 = Mathf.Sin((baseVert.x * waveFrequency) + (Time.time * waveSpeed)) * waveHeight;
            float wave2 = Mathf.Sin((baseVert.z * waveFrequency * 0.8f) + (Time.time * waveSpeed * 1.2f)) * waveHeight * 0.5f;
            
            // Apply to Y position
            vertices[i] = baseVert;
            vertices[i].y = wave1 + wave2;
        }
        
        // Update mesh
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        
        // Debug log every 2 seconds
        if (Time.frameCount % 120 == 0)
        {
            Debug.Log($"Wave UPDATE running! Frame:{Time.frameCount}, Vertices:{vertices.Length}");
        }
    }
}
