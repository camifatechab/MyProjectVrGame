using UnityEngine;

public class UnderwaterVolumetricFog : MonoBehaviour
{
    [Header("Fog Settings")]
    [Tooltip("Enable Unity fog for volumetric light effect")]
    public bool enableFog = true;
    
    [Tooltip("Fog color (underwater tint)")]
    public Color fogColor = new Color(0.2f, 0.4f, 0.6f, 1f);
    
    [Tooltip("Fog density (higher = thicker fog)")]
    [Range(0.001f, 0.1f)]
    public float fogDensity = 0.02f;
    
    [Tooltip("Fog start distance")]
    public float fogStart = 0f;
    
    [Tooltip("Fog end distance")]
    public float fogEnd = 50f;
    
    void Start()
    {
        ApplyFogSettings();
        Debug.Log("UnderwaterVolumetricFog: Fog enabled for flashlight beam visibility");
    }
    
    void ApplyFogSettings()
    {
        RenderSettings.fog = enableFog;
        
        if (enableFog)
        {
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = fogDensity;
            
            Debug.Log($"Fog enabled: Color={fogColor}, Density={fogDensity}");
        }
    }
    
    void OnValidate()
    {
        // Update in editor when values change
        if (Application.isPlaying)
        {
            ApplyFogSettings();
        }
    }
}
