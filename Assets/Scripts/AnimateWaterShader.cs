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
    
    [Header("Material Properties")]
    [Tooltip("Water smoothness (reflectivity)")]
    [Range(0f, 1f)]
    public float smoothness = 0.9f;
    
    [Tooltip("Water transparency")]
    [Range(0f, 1f)]
    public float alpha = 0.6f;
    
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
            
        // Update smoothness
        waterMaterial.SetFloat("_Smoothness", smoothness);
        
        // Update alpha (transparency)
        Color currentColor = waterMaterial.GetColor("_BaseColor");
        currentColor.a = alpha;
        waterMaterial.SetColor("_BaseColor", currentColor);
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
