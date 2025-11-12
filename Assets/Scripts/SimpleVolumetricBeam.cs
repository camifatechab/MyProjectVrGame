using UnityEngine;

/// <summary>
/// Simple volumetric flashlight beam using a mesh cone and transparent material.
/// This is a fallback if V-Light doesn't work - it creates a visible beam
/// </summary>
public class SimpleVolumetricBeam : MonoBehaviour
{
    [Header("Beam Settings")]
    [SerializeField] private Light spotlight;
    [SerializeField] private float beamLength = 20f;
    [SerializeField] private Color beamColor = new Color(1f, 1f, 0.9f, 0.15f);
    [SerializeField] private int coneSides = 16;
    
    private GameObject beamObject;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    
    void Start()
    {
        // Find spotlight if not assigned
        if (spotlight == null)
        {
            spotlight = GetComponent<Light>();
        }
        
        if (spotlight == null || spotlight.type != LightType.Spot)
        {
            Debug.LogError("SimpleVolumetricBeam requires a Spotlight component!");
            enabled = false;
            return;
        }
        
        CreateBeamMesh();
        CreateBeamMaterial();
        
        Debug.Log("<color=cyan>✓ Simple Volumetric Beam created as fallback</color>");
    }
    
    void CreateBeamMesh()
    {
        // Create beam GameObject
        beamObject = new GameObject("VolumetricBeam");
        beamObject.transform.SetParent(transform);
        beamObject.transform.localPosition = Vector3.zero;
        beamObject.transform.localRotation = Quaternion.identity;
        
        // Add mesh components
        meshFilter = beamObject.AddComponent<MeshFilter>();
        meshRenderer = beamObject.AddComponent<MeshRenderer>();
        
        // Create cone mesh
        Mesh coneMesh = CreateConeMesh();
        meshFilter.mesh = coneMesh;
    }
    
    Mesh CreateConeMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Volumetric Beam Cone";
        
        int vertexCount = coneSides + 2; // +1 for tip, +1 for center of base
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[coneSides * 6]; // 2 triangles per side
        
        // Tip vertex
        vertices[0] = Vector3.zero;
        
        // Base center vertex
        vertices[1] = new Vector3(0, 0, beamLength);
        
        // Calculate base radius from spotlight angle
        float halfAngleRad = spotlight.spotAngle * 0.5f * Mathf.Deg2Rad;
        float baseRadius = Mathf.Tan(halfAngleRad) * beamLength;
        
        // Create base circle vertices
        for (int i = 0; i < coneSides; i++)
        {
            float angle = (float)i / coneSides * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * baseRadius;
            float y = Mathf.Sin(angle) * baseRadius;
            vertices[i + 2] = new Vector3(x, y, beamLength);
        }
        
        // Create triangles for cone sides
        int triIndex = 0;
        for (int i = 0; i < coneSides; i++)
        {
            int next = (i + 1) % coneSides + 2;
            int current = i + 2;
            
            // Triangle from tip to base edge
            triangles[triIndex++] = 0;
            triangles[triIndex++] = next;
            triangles[triIndex++] = current;
            
            // Triangle for base cap
            triangles[triIndex++] = 1;
            triangles[triIndex++] = current;
            triangles[triIndex++] = next;
        }
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        return mesh;
    }
    
    void CreateBeamMaterial()
    {
        // Create a transparent material for the beam
        Material beamMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        
        // Set it to transparent
        beamMat.SetFloat("_Surface", 1); // Transparent
        beamMat.SetFloat("_Blend", 0); // Alpha blend
        beamMat.SetFloat("_SrcBlend", 5); // SrcAlpha
        beamMat.SetFloat("_DstBlend", 10); // OneMinusSrcAlpha
        beamMat.SetFloat("_ZWrite", 0); // No depth write
        beamMat.renderQueue = 3000; // Transparent queue
        
        // Set beam color
        beamMat.SetColor("_BaseColor", beamColor);
        beamMat.SetColor("_EmissionColor", beamColor * 2f);
        beamMat.EnableKeyword("_EMISSION");
        
        // Assign to renderer
        meshRenderer.material = beamMat;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }
    
    void Update()
    {
        // Match beam intensity to light
        if (spotlight != null && meshRenderer != null)
        {
            Material mat = meshRenderer.material;
            float alpha = Mathf.Clamp01(spotlight.intensity / 10f) * beamColor.a;
            Color currentColor = beamColor;
            currentColor.a = alpha;
            mat.SetColor("_BaseColor", currentColor);
            mat.SetColor("_EmissionColor", currentColor * spotlight.intensity);
            
            // Match beam active state to light
            if (beamObject != null)
            {
                beamObject.SetActive(spotlight.enabled);
            }
        }
    }
    
    void OnValidate()
    {
        // Recreate mesh if settings change in editor
        if (Application.isPlaying && meshFilter != null)
        {
            meshFilter.mesh = CreateConeMesh();
        }
    }
}
