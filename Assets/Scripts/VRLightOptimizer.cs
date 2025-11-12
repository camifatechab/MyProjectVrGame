using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Optimizes light settings specifically for VR rendering.
/// VR headsets have different perception and rendering requirements than desktop screens.
/// This script ensures the flashlight works identically in both VR and desktop views.
/// </summary>
public class VRLightOptimizer : MonoBehaviour
{
    [Header("Target Light")]
    [Tooltip("The light to optimize for VR (will auto-find if not assigned)")]
    public Light targetLight;
    
    [Header("VR-Specific Settings")]
    [Tooltip("Multiplier for light intensity when in VR mode")]
    [Range(1f, 5f)]
    public float vrIntensityMultiplier = 2.5f;
    
    [Tooltip("Enable additional light for VR to fill shadows")]
    public bool useVRFillLight = true;
    
    [Tooltip("Fill light intensity (percentage of main light)")]
    [Range(0f, 0.5f)]
    public float fillLightIntensity = 0.15f;
    
    [Header("Rendering Optimization")]
    [Tooltip("Enable realtime shadows for this light")]
    public bool enableShadows = true;
    
    [Tooltip("Shadow resolution")]
    public LightShadowResolution shadowResolution = LightShadowResolution.High;
    
    [Tooltip("Shadow distance for this light")]
    public float shadowDistance = 50f;
    
    [Header("Performance")]
    [Tooltip("Render mode - ForwardAdd for multiple lights, Auto for URP decision")]
    public LightRenderMode renderMode = LightRenderMode.Auto;
    
    [Tooltip("Enable per-pixel lighting (more accurate but more expensive)")]
    public bool perPixelLighting = true;
    
    // Private
    private Light fillLight;
    private float baseIntensity;
    private bool isVRActive;
    private UniversalAdditionalLightData additionalLightData;
    
    void Start()
    {
        // Auto-find light if not assigned
        if (targetLight == null)
        {
            targetLight = GetComponent<Light>();
            if (targetLight == null)
            {
                targetLight = GetComponentInChildren<Light>();
            }
        }
        
        if (targetLight == null)
        {
            Debug.LogError("VRLightOptimizer: No light found to optimize!");
            enabled = false;
            return;
        }
        
        // Store base intensity
        baseIntensity = targetLight.intensity;
        
        // Get or add URP additional light data
        additionalLightData = targetLight.GetUniversalAdditionalLightData();
        
        // Detect if VR is active
        isVRActive = UnityEngine.XR.XRSettings.enabled && UnityEngine.XR.XRSettings.isDeviceActive;
        
        // Apply optimal settings
        ApplyOptimalSettings();
        
        // Create fill light if needed
        if (useVRFillLight && isVRActive)
        {
            CreateFillLight();
        }
        
        Debug.Log($"VRLightOptimizer: Configured for {(isVRActive ? "VR" : "Desktop")} rendering");
        Debug.Log($"VRLightOptimizer: Final intensity = {targetLight.intensity}");
    }
    
    void ApplyOptimalSettings()
    {
        // Apply VR intensity boost if in VR
        if (isVRActive)
        {
            targetLight.intensity = baseIntensity * vrIntensityMultiplier;
            Debug.Log($"VRLightOptimizer: Boosted intensity from {baseIntensity} to {targetLight.intensity} for VR");
        }
        
        // Configure shadows
        if (enableShadows)
        {
            targetLight.shadows = LightShadows.Soft;
            targetLight.shadowStrength = 0.8f;
            targetLight.shadowResolution = shadowResolution;
            targetLight.shadowNearPlane = 0.1f;
        }
        else
        {
            targetLight.shadows = LightShadows.None;
        }
        
        // Configure render mode
        targetLight.renderMode = renderMode;
        
        // URP Additional Light Data settings
        if (additionalLightData != null)
        {
            // Rendering layers set to all
            additionalLightData.renderingLayers = uint.MaxValue;
            
            Debug.Log("VRLightOptimizer: URP Additional Light Data configured");
        }
        
        // Ensure light is set to important (won't be culled)
        targetLight.lightmapBakeType = LightmapBakeType.Realtime;
        targetLight.cullingMask = -1; // Everything
    }
    
    void CreateFillLight()
    {
        // Create a fill light to reduce harsh shadows in VR
        GameObject fillLightObj = new GameObject("VR Fill Light");
        fillLightObj.transform.SetParent(targetLight.transform, false);
        
        fillLight = fillLightObj.AddComponent<Light>();
        fillLight.type = LightType.Point;
        fillLight.intensity = targetLight.intensity * fillLightIntensity;
        fillLight.range = targetLight.range * 0.4f;
        fillLight.color = new Color(1f, 1f, 1f, 1f); // Neutral white
        fillLight.shadows = LightShadows.None; // Fill light doesn't need shadows
        fillLight.renderMode = LightRenderMode.ForcePixel;
        fillLight.cullingMask = targetLight.cullingMask;
        
        // Position slightly forward of main light
        fillLight.transform.localPosition = new Vector3(0, 0, 0.2f);
        
        Debug.Log($"VRLightOptimizer: Created fill light with intensity {fillLight.intensity}");
    }
    
    void Update()
    {
        // Sync fill light state with main light
        if (fillLight != null && targetLight != null)
        {
            fillLight.enabled = targetLight.enabled;
            
            // Dynamically adjust fill light intensity if main light changes
            fillLight.intensity = targetLight.intensity * fillLightIntensity;
        }
    }
    
    void OnValidate()
    {
        // Update settings in editor when values change
        if (Application.isPlaying && targetLight != null)
        {
            ApplyOptimalSettings();
            
            if (fillLight != null)
            {
                fillLight.intensity = targetLight.intensity * fillLightIntensity;
            }
        }
    }
    
    // Public method to manually refresh VR detection
    public void RefreshVRDetection()
    {
        bool wasVRActive = isVRActive;
        isVRActive = UnityEngine.XR.XRSettings.enabled && UnityEngine.XR.XRSettings.isDeviceActive;
        
        if (wasVRActive != isVRActive)
        {
            ApplyOptimalSettings();
            Debug.Log($"VRLightOptimizer: VR mode changed to {(isVRActive ? "Active" : "Inactive")}");
        }
    }
}
