using UnityEngine;

public class VolumetricLightBeam : MonoBehaviour
{
    [Header("Beam Settings")]
    [Tooltip("Length of the light beam")]
    public float beamLength = 30f;
    
    [Tooltip("Starting radius at flashlight")]
    public float startRadius = 0.1f;
    
    [Tooltip("Ending radius at beam end")]
    public float endRadius = 8f;
    
    [Tooltip("Beam color")]
    public Color beamColor = new Color(1f, 1f, 1f, 0.1f);
    
    [Tooltip("Number of segments for smoothness")]
    public int segments = 32;
    
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Material beamMaterial;
    
    void Start()
    {
        CreateBeamMesh();
        CreateBeamMaterial();
        Debug.Log("VolumetricLightBeam: Created volumetric beam effect");
    }
    
void CreateBeamMesh()
    {
        // Add MeshFilter and MeshRenderer
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        
        Mesh mesh = new Mesh();
        mesh.name = "VolumetricBeam";
        
        // Create cone mesh with gradient
        int vertexCount = (segments + 1) * 2;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
                int[] triangles = new int[segments * 6];
        
        // Create vertices with gradient colors
        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            
            // Start circle (near flashlight) - brighter
            vertices[i * 2] = new Vector3(
                Mathf.Cos(angle) * startRadius,
                Mathf.Sin(angle) * startRadius,
                0f
            );
                        
            // End circle (far from flashlight) - fade out
            vertices[i * 2 + 1] = new Vector3(
                Mathf.Cos(angle) * endRadius,
                Mathf.Sin(angle) * endRadius,
                beamLength
            );
                        
            // UVs
            uvs[i * 2] = new Vector2((float)i / segments, 0f);
            uvs[i * 2 + 1] = new Vector2((float)i / segments, 1f);
        }
        
        // Create triangles
        int triIndex = 0;
        for (int i = 0; i < segments; i++)
        {
            int current = i * 2;
            int next = ((i + 1) % (segments + 1)) * 2;
            
            // First triangle
            triangles[triIndex++] = current;
            triangles[triIndex++] = next;
            triangles[triIndex++] = current + 1;
            
            // Second triangle
            triangles[triIndex++] = current + 1;
            triangles[triIndex++] = next;
            triangles[triIndex++] = next + 1;
        }
        
        mesh.vertices = vertices;
        mesh.uv = uvs;
        
                // Create gradient: transparent enough to see through, fading to invisible at tip
        Color32[] colors32 = new Color32[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            // Calculate fade based on Z position (distance along beam)
            float t = vertices[i].z / beamLength;
            
            // Ultra-aggressive falloff - end must be completely transparent
            float falloff = 1f - (t * t * t * t); // Quartic (t^4) for ultra-soft fade
            
            // MUCH lower maximum alpha so you can see through the beam
            // Start at 40% opacity (100/255), fade to 0%
            byte alpha = (byte)(100 * falloff); // Max 100 (~40%) instead of 255
            
            // Pure white with subtle warm tint
            byte r = 255;
            byte g = 255;
            byte b = (byte)(255 * (0.85f + 0.15f * falloff));
            
            colors32[i] = new Color32(r, g, b, alpha);
        }
        mesh.colors32 = colors32;
                mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        meshFilter.mesh = mesh;
    }
    
void CreateBeamMaterial()
    {
        // Create a material with transparency and soft additive blending
        beamMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        
                // Use WHITE base color so vertex gradient colors show properly in VR
                beamMaterial.SetColor("_BaseColor", Color.white);
        beamMaterial.SetFloat("_Surface", 1); // Transparent
        beamMaterial.SetFloat("_Blend", 1); // Additive blend for glow effect
        beamMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                beamMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); // Alpha blend for softer look// Additive
        beamMaterial.SetFloat("_ZWrite", 0); // Don't write to depth buffer
        beamMaterial.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off); // Render both sides
        
        beamMaterial.renderQueue = 3000; // Transparent queue
        beamMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        beamMaterial.EnableKeyword("_BLENDMODE_ADD");
        
        meshRenderer.material = beamMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }
    
    void OnValidate()
    {
        // Update mesh when values change in editor
        if (Application.isPlaying && meshFilter != null)
        {
            CreateBeamMesh();
        }
        
        if (Application.isPlaying && beamMaterial != null)
        {
            beamMaterial.SetColor("_BaseColor", beamColor);
        }
    }
}
