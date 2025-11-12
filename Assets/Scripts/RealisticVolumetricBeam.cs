using UnityEngine;

/// <summary>
/// Realistic volumetric flashlight beam with full inspector control
/// Creates a see-through beam that looks like real light in water
/// </summary>
[RequireComponent(typeof(Light))]
public class RealisticVolumetricBeam : MonoBehaviour
{
    [Header("Beam Appearance")]
    [SerializeField] private Light spotlight;
    
    [Tooltip("Base color of the beam - try white or slight cyan for underwater")]
    [SerializeField] private Color beamColor = new Color(0.9f, 0.95f, 1f, 0.2f);
    
    [Tooltip("How far the beam extends (meters)")]
    [SerializeField] [Range(5f, 50f)] private float beamLength = 25f;
    
    [Tooltip("Transparency: 0.1=very see-through, 0.5=solid")]
    [SerializeField] [Range(0.05f, 0.8f)] private float beamTransparency = 0.2f;
    
    [Header("Realism Settings")]
    [Tooltip("How much the beam fades from source to end (higher=more fade)")]
    [SerializeField] [Range(0f, 2f)] private float falloffStrength = 1.2f;
    
    [Tooltip("Brightness boost at the flashlight source")]
    [SerializeField] [Range(0f, 5f)] private float sourceGlow = 1.5f;
    
    [Tooltip("How soft the beam edges are (higher=softer)")]
    [SerializeField] [Range(0f, 1f)] private float edgeSoftness = 0.3f;
    
    [Header("Performance")]
    [Tooltip("Number of sides for the cone (16=smooth, 8=faster)")]
    [SerializeField] [Range(6, 32)] private int coneSides = 16;
    
    [Header("Advanced")]
    [Tooltip("Add slight blue/cyan tint for underwater atmosphere")]
    [SerializeField] private bool useUnderwaterTint = true;
    
    [Tooltip("Pulse the beam slightly for organic feel")]
    [SerializeField] private bool enablePulse = false;
    
    [SerializeField] [Range(0f, 0.1f)] private float pulseAmount = 0.03f;
    [SerializeField] [Range(0.5f, 3f)] private float pulseSpeed = 1.5f;
    
    private GameObject beamObject;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Material beamMaterial;
    private float pulseTimer = 0f;
    
    void Start()
    {
        if (spotlight == null)
        {
            spotlight = GetComponent<Light>();
        }
        
        if (spotlight == null || spotlight.type != LightType.Spot)
        {
            Debug.LogError("RealisticVolumetricBeam requires a Spotlight!");
            enabled = false;
            return;
        }
        
        CreateBeamMesh();
        CreateBeamMaterial();
        
        Debug.Log("<color=cyan>✓ Realistic Volumetric Beam created</color>");
        Debug.Log("  Adjust settings in Inspector for perfect look!");
    }
    
    void CreateBeamMesh()
    {
        beamObject = new GameObject("RealisticBeam");
        beamObject.transform.SetParent(transform);
        beamObject.transform.localPosition = Vector3.zero;
        beamObject.transform.localRotation = Quaternion.identity;
        
        meshFilter = beamObject.AddComponent<MeshFilter>();
        meshRenderer = beamObject.AddComponent<MeshRenderer>();
        
        UpdateBeamMesh();
    }
    
    void UpdateBeamMesh()
    {
        if (meshFilter == null) return;
        
        Mesh coneMesh = CreateConeMeshWithGradient();
        meshFilter.mesh = coneMesh;
    }
    
    Mesh CreateConeMeshWithGradient()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Realistic Beam Cone";
        
        // Create vertices with UV coordinates for gradient
        int vertexCount = (coneSides + 1) * 2; // Top ring + bottom ring + centers
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        Color[] colors = new Color[vertexCount];
        
        float halfAngleRad = spotlight.spotAngle * 0.5f * Mathf.Deg2Rad;
        float baseRadius = Mathf.Tan(halfAngleRad) * beamLength;
        
        int index = 0;
        
        // Tip vertices (bright at source)
        for (int i = 0; i <= coneSides; i++)
        {
            vertices[index] = Vector3.zero;
            uvs[index] = new Vector2(0f, 0f); // Start of gradient
            colors[index] = new Color(1f, 1f, 1f, 1f); // Full brightness at source
            index++;
        }
        
        // Base vertices (dim at end)
        for (int i = 0; i <= coneSides; i++)
        {
            float angle = (float)i / coneSides * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * baseRadius;
            float y = Mathf.Sin(angle) * baseRadius;
            
            vertices[index] = new Vector3(x, y, beamLength);
            uvs[index] = new Vector2(1f, (float)i / coneSides); // End of gradient
            
            // Fade out based on falloff strength
            float alpha = 1f - (falloffStrength * 0.5f);
            colors[index] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
            index++;
        }
        
        // Create triangles
        int[] triangles = new int[coneSides * 6];
        int triIndex = 0;
        
        for (int i = 0; i < coneSides; i++)
        {
            int topCurrent = i;
            int topNext = i + 1;
            int bottomCurrent = coneSides + 1 + i;
            int bottomNext = coneSides + 1 + i + 1;
            
            // Side triangle 1
            triangles[triIndex++] = topCurrent;
            triangles[triIndex++] = bottomNext;
            triangles[triIndex++] = bottomCurrent;
            
            // Side triangle 2
            triangles[triIndex++] = topCurrent;
            triangles[triIndex++] = topNext;
            triangles[triIndex++] = bottomNext;
        }
        
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        return mesh;
    }
    
    void CreateBeamMaterial()
    {
        beamMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        
        // Configure for transparency
        beamMaterial.SetFloat("_Surface", 1); // Transparent
        beamMaterial.SetFloat("_Blend", 0); // Alpha blend
        beamMaterial.SetFloat("_SrcBlend", 5); // SrcAlpha
        beamMaterial.SetFloat("_DstBlend", 10); // OneMinusSrcAlpha
        beamMaterial.SetFloat("_ZWrite", 0); // No depth write
        beamMaterial.renderQueue = 3000; // Transparent queue
        
        // Smooth/soft rendering
        beamMaterial.SetFloat("_Smoothness", edgeSoftness);
        
        UpdateBeamMaterial();
        
        meshRenderer.material = beamMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }
    
    void UpdateBeamMaterial()
    {
        if (beamMaterial == null) return;
        
        // Apply underwater tint if enabled
        Color finalColor = beamColor;
        if (useUnderwaterTint)
        {
            // Add subtle cyan/blue tint
            finalColor.r *= 0.9f;
            finalColor.g *= 0.95f;
            finalColor.b *= 1.0f;
        }
        
        // Apply transparency setting
        finalColor.a = beamTransparency;
        
        beamMaterial.SetColor("_BaseColor", finalColor);
        
        // Add emission for glow at source
        Color emissionColor = finalColor * spotlight.intensity * sourceGlow * 0.1f;
        beamMaterial.SetColor("_EmissionColor", emissionColor);
        beamMaterial.EnableKeyword("_EMISSION");
        
        // Update smoothness for edge softness
        beamMaterial.SetFloat("_Smoothness", edgeSoftness);
    }
    
    void Update()
    {
        if (spotlight == null || beamObject == null) return;
        
        // Match beam visibility to light
        beamObject.SetActive(spotlight.enabled);
        
        // Update material based on light intensity
        if (beamMaterial != null)
        {
            Color currentColor = beamColor;
            if (useUnderwaterTint)
            {
                currentColor.r *= 0.9f;
                currentColor.g *= 0.95f;
            }
            
            // Apply pulse if enabled
            float currentAlpha = beamTransparency;
            if (enablePulse)
            {
                pulseTimer += Time.deltaTime * pulseSpeed;
                float pulse = Mathf.Sin(pulseTimer) * pulseAmount;
                currentAlpha = Mathf.Clamp(beamTransparency + pulse, 0.05f, 0.8f);
            }
            
            currentColor.a = currentAlpha;
            beamMaterial.SetColor("_BaseColor", currentColor);
            
            // Update emission
            Color emissionColor = currentColor * spotlight.intensity * sourceGlow * 0.1f;
            beamMaterial.SetColor("_EmissionColor", emissionColor);
        }
    }
    
    void OnValidate()
    {
        // Update mesh when settings change in editor
        if (Application.isPlaying && meshFilter != null)
        {
            UpdateBeamMesh();
            UpdateBeamMaterial();
        }
    }
    
    void OnDestroy()
    {
        if (beamMaterial != null)
        {
            Destroy(beamMaterial);
        }
    }
}
