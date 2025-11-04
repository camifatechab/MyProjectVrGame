using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class CaveWaterWaves : MonoBehaviour
{
    [Header("Wave Settings")]
    [Tooltip("Height of the waves (0.05-0.2 recommended for cave water)")]
    [Range(0.01f, 0.5f)]
    public float waveHeight = 0.1f;
    
    [Tooltip("Speed of wave movement")]
    [Range(0.1f, 2f)]
    public float waveSpeed = 0.3f;
    
    [Tooltip("Frequency of waves (higher = more waves)")]
    [Range(0.5f, 5f)]
    public float waveFrequency = 1.5f;
    
    [Tooltip("Secondary wave for more natural movement")]
    [Range(0f, 0.3f)]
    public float secondaryWaveHeight = 0.05f;
    
    [Range(0.5f, 3f)]
    public float secondaryWaveSpeed = 0.5f;
    
    [Header("Mesh Settings")]
    [Tooltip("Enable if you want to subdivide the mesh for smoother waves")]
    public bool subdivideMesh = false;
    
    [Range(2, 20)]
    public int subdivisions = 5;
    
    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] displacedVertices;
    
    void Start()
    {
        // Get or create mesh
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        mesh = meshFilter.mesh;
        
        // Subdivide if requested for smoother waves
        if (subdivideMesh)
        {
            SubdivideMesh();
        }
        
        // Store original vertex positions
        originalVertices = mesh.vertices;
        displacedVertices = new Vector3[originalVertices.Length];
    }
    
    void Update()
    {
        if (originalVertices == null || originalVertices.Length == 0)
            return;
            
        // Calculate wave displacement for each vertex
        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 vertex = originalVertices[i];
            
            // Primary wave (moves in X direction)
            float primaryWave = Mathf.Sin((vertex.x * waveFrequency) + (Time.time * waveSpeed)) * waveHeight;
            
            // Secondary wave (moves in Z direction for more natural look)
            float secondaryWave = Mathf.Sin((vertex.z * waveFrequency * 0.7f) + (Time.time * secondaryWaveSpeed * 1.3f)) * secondaryWaveHeight;
            
            // Combine waves (only affects Y position)
            displacedVertices[i] = vertex;
            displacedVertices[i].y = vertex.y + primaryWave + secondaryWave;
        }
        
        // Apply displaced vertices to mesh
        mesh.vertices = displacedVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
    
    void SubdivideMesh()
    {
        // Simple mesh subdivision for smoother waves
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh originalMesh = meshFilter.sharedMesh;
        
        // Create a new mesh with more vertices
        // Note: This is a simple subdivision - for production use a proper subdivision algorithm
        int gridSize = subdivisions;
        Vector3[] newVertices = new Vector3[(gridSize + 1) * (gridSize + 1)];
        int[] newTriangles = new int[gridSize * gridSize * 6];
        Vector2[] newUVs = new Vector2[newVertices.Length];
        
        // Generate grid vertices
        for (int z = 0; z <= gridSize; z++)
        {
            for (int x = 0; x <= gridSize; x++)
            {
                int index = z * (gridSize + 1) + x;
                float xPos = (x / (float)gridSize - 0.5f) * 10f; // Plane is 10 units
                float zPos = (z / (float)gridSize - 0.5f) * 10f;
                newVertices[index] = new Vector3(xPos, 0, zPos);
                newUVs[index] = new Vector2(x / (float)gridSize, z / (float)gridSize);
            }
        }
        
        // Generate triangles
        int triangleIndex = 0;
        for (int z = 0; z < gridSize; z++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                int vertIndex = z * (gridSize + 1) + x;
                
                // First triangle
                newTriangles[triangleIndex++] = vertIndex;
                newTriangles[triangleIndex++] = vertIndex + gridSize + 1;
                newTriangles[triangleIndex++] = vertIndex + 1;
                
                // Second triangle
                newTriangles[triangleIndex++] = vertIndex + 1;
                newTriangles[triangleIndex++] = vertIndex + gridSize + 1;
                newTriangles[triangleIndex++] = vertIndex + gridSize + 2;
            }
        }
        
        // Create new mesh
        Mesh subdividedMesh = new Mesh();
        subdividedMesh.name = "Subdivided Water Plane";
        subdividedMesh.vertices = newVertices;
        subdividedMesh.triangles = newTriangles;
        subdividedMesh.uv = newUVs;
        subdividedMesh.RecalculateNormals();
        subdividedMesh.RecalculateBounds();
        
        meshFilter.mesh = subdividedMesh;
    }
    
    private void OnDestroy()
    {
        // Clean up mesh on destroy
        if (mesh != null)
        {
            mesh.vertices = originalVertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }
}
