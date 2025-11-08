using UnityEngine;

public class AnimateWaterShader : MonoBehaviour
{
    [Header("Normal Map Animation")]
    [Tooltip("Speed of normal map panning (creates ripple effect)")]
    [Range(0f, 0.5f)]
    public float normalMapSpeed = 0.05f;
    
    [Tooltip("Direction of normal map movement")]
    public Vector2 normalMapDirection = new Vector2(0.3f, 0.2f);
    
    [Header("Secondary Normal (Optional)")]
    [Tooltip("Enable second normal map layer for more detail")]
    public bool useSecondaryNormal = true;
    
    [Range(0f, 0.3f)]
    public float secondaryNormalSpeed = 0.03f;
    
    public Vector2 secondaryNormalDirection = new Vector2(-0.2f, 0.3f);
    
    [Header("Wave Distortion")]
    [Tooltip("Subtle refraction/distortion amount")]
    [Range(0f, 0.1f)]
    public float distortionAmount = 0.02f;
    
    [Header("Water Color")]
    [Tooltip("Water color (your dark navy blue from the scene)")]
    public Color waterColor = new Color(0.08f, 0.18f, 0.35f, 1f); // Dark navy blue to match fog // Dark navy blue to match fog
    
    [Header("Material Properties")]
    [Tooltip("Water smoothness (reflectivity)")]
    [Range(0f, 1f)]
    public float smoothness = 0.9f;
    
    [Tooltip("Water transparency")]
    [Range(0f, 1f)]
    public float alpha = 0.6f;
    
    [Header("Surface Quality")]
    [Tooltip("Makes water non-metallic (should be 0 for realistic water)")]
    [Range(0f, 1f)]
    public float metallic = 0f;
    
    [Tooltip("Enable specular highlights (makes water sparkle)")]
    public bool specularHighlights = true;
    
    [Tooltip("Enable environment reflections (sky/surroundings reflect on water)")]
    public bool environmentReflections = true;
    
    private Material waterMaterial;
    private Vector2 normalOffset = Vector2.zero;
    private Vector2 secondaryOffset = Vector2.zero;
    
    void Start()
    {
        // Get the material from the renderer
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            // Use material (instance) not sharedMaterial to avoid affecting all instances
            waterMaterial = renderer.material;
            
            // Set initial properties
            if (waterMaterial != null)
            {
                UpdateMaterialProperties();
            }
        }
        else
        {
            Debug.LogError("AnimateWaterShader: No MeshRenderer found!");
        }
    }
    
    void Update()
    {
        if (waterMaterial == null)
            return;
            
        // Animate normal map offset (creates moving ripple effect)
        normalOffset += normalMapDirection * normalMapSpeed * Time.deltaTime;
        
        if (useSecondaryNormal)
        {
            secondaryOffset += secondaryNormalDirection * secondaryNormalSpeed * Time.deltaTime;
        }
        
        // Apply to material
        // Note: URP/Lit shader uses _BaseMap for main texture
        waterMaterial.SetTextureOffset("_BaseMap", normalOffset);
        
        // If your material has a normal map, uncomment this:
        // waterMaterial.SetTextureOffset("_BumpMap", normalOffset);
        
        // Update other properties if changed
        UpdateMaterialProperties();
    }
    
    void UpdateMaterialProperties()
    {
        if (waterMaterial == null)
            return;
            
        // Update smoothness for reflections
        waterMaterial.SetFloat("_Smoothness", smoothness);
        
        // Update metallic (water should be non-metallic)
        waterMaterial.SetFloat("_Metallic", metallic);
        
        // Update water color with current alpha
        Color finalColor = waterColor;
        finalColor.a = alpha;
        waterMaterial.SetColor("_BaseColor", finalColor);
        
        // Update rendering features
        waterMaterial.SetFloat("_SpecularHighlights", specularHighlights ? 1f : 0f);
        waterMaterial.SetFloat("_EnvironmentReflections", environmentReflections ? 1f : 0f);
    }
    
    private void OnDestroy()
    {
        // Clean up material instance
        if (waterMaterial != null)
        {
            Destroy(waterMaterial);
        }
    }
    
    private void OnValidate()
    {
        // Update material when values change in Inspector
        if (Application.isPlaying && waterMaterial != null)
        {
            UpdateMaterialProperties();
        }
    }
}