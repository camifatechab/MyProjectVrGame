using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Editor helper to verify and configure URP settings for optimal VR lighting.
/// Run this once to ensure your project is set up correctly for VR flashlight rendering.
/// </summary>
#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
#endif
public class URPVRSetupValidator : MonoBehaviour
{
    [Header("Validation Settings")]
    [Tooltip("Log detailed information")]
    public bool verboseLogging = true;
    
    [Header("Recommended VR Settings")]
    [Tooltip("Enable HDR for better light bloom")]
    public bool enableHDR = true;
    
    [Tooltip("MSAA level for VR (higher = better quality, lower performance)")]
    public MsaaQuality msaaQuality = MsaaQuality._4x;
    
    [ContextMenu("Validate URP Settings")]
    public void ValidateSettings()
    {
        var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        
        if (urpAsset == null)
        {
            Debug.LogError("URPVRSetupValidator: No URP asset found! Make sure you're using Universal Render Pipeline.");
            return;
        }
        
        Debug.Log("=== URP VR Setup Validation ===");
        
        // Check renderer
        ValidateRenderer(urpAsset);
        
        // Check main light settings
        ValidateMainLight(urpAsset);
        
        // Check shadows
        ValidateShadows(urpAsset);
        
        // Check rendering features
        ValidateRenderingFeatures(urpAsset);
        
        Debug.Log("=== Validation Complete ===");
    }
    
    void ValidateRenderer(UniversalRenderPipelineAsset urpAsset)
    {
        Debug.Log($"<color=cyan>Renderer Type:</color> {urpAsset.GetType().Name}");
        
        if (verboseLogging)
        {
            Debug.Log($"  - HDR: {urpAsset.supportsHDR}");
            Debug.Log($"  - MSAA: {urpAsset.msaaSampleCount}");
            Debug.Log($"  - Render Scale: {urpAsset.renderScale}");
        }
        
        // Check if HDR is enabled (important for bright lights)
        if (!urpAsset.supportsHDR && enableHDR)
        {
            Debug.LogWarning("URPVRSetupValidator: HDR is disabled. This may cause flashlight to look washed out.");
            Debug.Log("URPVRSetupValidator: Enable HDR manually in URP Asset settings for better lighting.");
        }
    }
    
    void ValidateMainLight(UniversalRenderPipelineAsset urpAsset)
    {
        Debug.Log($"<color=cyan>Main Light:</color> {(urpAsset.supportsMainLightShadows ? "✓" : "✗")} Shadows Supported");
    }
    
    void ValidateShadows(UniversalRenderPipelineAsset urpAsset)
    {
        Debug.Log($"<color=cyan>Shadows:</color>");
        Debug.Log($"  - Main Light Shadows: {urpAsset.supportsMainLightShadows}");
        Debug.Log($"  - Additional Light Shadows: {urpAsset.supportsAdditionalLightShadows}");
        
        if (urpAsset.shadowDistance < 30f)
        {
            Debug.LogWarning($"URPVRSetupValidator: Shadow distance is only {urpAsset.shadowDistance}m. Flashlight shadows may cut off early.");
            Debug.Log("  -> Recommend: 50-100m for underwater cave exploration");
        }
    }
    
    void ValidateRenderingFeatures(UniversalRenderPipelineAsset urpAsset)
    {
        Debug.Log($"<color=cyan>Rendering Features:</color>");
        Debug.Log($"  - Depth Texture: {urpAsset.supportsCameraDepthTexture}");
        Debug.Log($"  - Opaque Texture: {urpAsset.supportsCameraOpaqueTexture}");
        
        if (!urpAsset.supportsCameraDepthTexture)
        {
            Debug.LogWarning("URPVRSetupValidator: Depth Texture disabled. May affect volumetric effects.");
        }
    }
    
    [ContextMenu("Print Current Quality Level")]
    public void PrintQualityLevel()
    {
        Debug.Log($"Current Quality Level: {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
        Debug.Log($"Active Render Pipeline: {GraphicsSettings.currentRenderPipeline?.name ?? "None"}");
    }
    
    [ContextMenu("List All Lights in Scene")]
    public void ListSceneLights()
    {
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        
        Debug.Log($"=== Found {lights.Length} lights in scene ===");
        
        foreach (Light light in lights)
        {
            string status = light.enabled ? "✓" : "✗";
            Debug.Log($"{status} {light.gameObject.name}:");
            Debug.Log($"    Type: {light.type}");
            Debug.Log($"    Intensity: {light.intensity}");
            Debug.Log($"    Range: {light.range}");
            Debug.Log($"    Shadows: {light.shadows}");
            Debug.Log($"    RenderMode: {light.renderMode}");
        }
    }
}
