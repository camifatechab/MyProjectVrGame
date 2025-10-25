using UnityEngine;

public class PulsingGlow : MonoBehaviour
{
    [Header("Glow Settings")]
    public float pulseSpeed = 2f;
    public float minIntensity = 1f;
    public float maxIntensity = 3f;
    public Color glowColor = new Color(0f, 2f, 2f); // Bright cyan
    
    private Material glowMaterial;
    private MeshRenderer meshRenderer;
    
    private void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            // Create a unique material instance to avoid affecting the original
            glowMaterial = meshRenderer.material;
            glowMaterial.EnableKeyword("_EMISSION");
        }
    }
    
    private void Update()
    {
        if (glowMaterial != null)
        {
            // Calculate pulsing intensity using sine wave
            float pulseIntensity = Mathf.Lerp(minIntensity, maxIntensity, 
                (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
            
            // Add twinkling effect - random flickers
            float twinkle = Mathf.PerlinNoise(Time.time * pulseSpeed * 3f, 0f) * 0.3f;
            float finalIntensity = pulseIntensity + twinkle;
            
            // Apply the pulsing + twinkling glow color
            Color emissionColor = glowColor * finalIntensity;
            glowMaterial.SetColor("_EmissionColor", emissionColor);
        }
    }
}
