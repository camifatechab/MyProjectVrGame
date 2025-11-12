using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Automated setup script for VR flashlight system.
/// Attach this to your Right Controller and click "Auto-Setup Flashlight" in the Inspector.
/// This will configure all components automatically.
/// </summary>
public class VRFlashlightAutoSetup : MonoBehaviour
{
    [Header("Auto-Detection")]
    [Tooltip("The flashlight prefab (will search if not assigned)")]
    public GameObject flashlightPrefab;
    
    [Header("Component References (Auto-assigned)")]
    public VRFlashlightController flashlightController;
    public VRLightOptimizer lightOptimizer;
    public Light spotlight;
    public VolumetricLightBeam volumetricBeam;
    
    [Header("Setup Options")]
    [Tooltip("VR intensity multiplier for light optimizer")]
    [Range(1.5f, 5f)]
    public float vrIntensityBoost = 2.5f;
    
    [Tooltip("Enable fill light for VR")]
    public bool useVRFillLight = true;
    
    [ContextMenu("Auto-Setup Flashlight")]
    public void AutoSetupFlashlight()
    {
        Debug.Log("=== VR Flashlight Auto-Setup Started ===");
        
        // Step 1: Find flashlight prefab
        if (!FindFlashlightPrefab())
        {
            Debug.LogError("AutoSetup FAILED: Could not find flashlight prefab!");
            return;
        }
        
        // Step 2: Find or add VRFlashlightController
        if (!SetupFlashlightController())
        {
            Debug.LogError("AutoSetup FAILED: Could not setup flashlight controller!");
            return;
        }
        
        // Step 3: Find spotlight
        if (!FindSpotlight())
        {
            Debug.LogError("AutoSetup FAILED: Could not find spotlight!");
            return;
        }
        
        // Step 4: Add VRLightOptimizer to spotlight
        if (!SetupLightOptimizer())
        {
            Debug.LogError("AutoSetup FAILED: Could not setup light optimizer!");
            return;
        }
        
        // Step 5: Find or verify volumetric beam
        FindVolumetricBeam();
        
        Debug.Log("=== VR Flashlight Auto-Setup COMPLETE ===");
        Debug.Log("✓ Flashlight Controller configured");
        Debug.Log("✓ Light Optimizer attached to spotlight");
        Debug.Log($"✓ VR Intensity Boost: {vrIntensityBoost}x");
        Debug.Log($"✓ Fill Light: {(useVRFillLight ? "Enabled" : "Disabled")}");
        Debug.Log("\nNext: Enter Play mode and test in VR!");
    }
    
    bool FindFlashlightPrefab()
    {
        if (flashlightPrefab != null)
        {
            Debug.Log($"✓ Using assigned flashlight: {flashlightPrefab.name}");
            return true;
        }
        
        // Search in children
        foreach (Transform child in transform)
        {
            if (child.name.Contains("Flashlight"))
            {
                flashlightPrefab = child.gameObject;
                Debug.Log($"✓ Found flashlight: {flashlightPrefab.name}");
                return true;
            }
        }
        
        Debug.LogError("✗ No flashlight prefab found in children!");
        Debug.LogError("  Make sure FlashlightPrefab is a child of this GameObject");
        return false;
    }
    
    bool SetupFlashlightController()
    {
        // Check if controller already exists on this GameObject
        flashlightController = GetComponent<VRFlashlightController>();
        
        if (flashlightController == null)
        {
            Debug.Log("Adding VRFlashlightController component...");
            flashlightController = gameObject.AddComponent<VRFlashlightController>();
        }
        
        if (flashlightController != null)
        {
            // Assign references
            flashlightController.flashlightPrefab = flashlightPrefab;
            
            Debug.Log("✓ VRFlashlightController configured");
            return true;
        }
        
        return false;
    }
    
    bool FindSpotlight()
    {
        if (flashlightPrefab == null) return false;
        
        // Find spotlight in flashlight hierarchy
        Light[] lights = flashlightPrefab.GetComponentsInChildren<Light>(true);
        
        foreach (Light light in lights)
        {
            if (light.type == LightType.Spot)
            {
                spotlight = light;
                Debug.Log($"✓ Found spotlight: {spotlight.gameObject.name}");
                Debug.Log($"  Current intensity: {spotlight.intensity}");
                Debug.Log($"  Current range: {spotlight.range}");
                return true;
            }
        }
        
        Debug.LogError("✗ No spotlight found in flashlight!");
        return false;
    }
    
    bool SetupLightOptimizer()
    {
        if (spotlight == null) return false;
        
        // Check if optimizer already exists
        lightOptimizer = spotlight.GetComponent<VRLightOptimizer>();
        
        if (lightOptimizer == null)
        {
            Debug.Log("Adding VRLightOptimizer to spotlight...");
            lightOptimizer = spotlight.gameObject.AddComponent<VRLightOptimizer>();
        }
        
        if (lightOptimizer != null)
        {
            // Configure optimizer
            lightOptimizer.targetLight = spotlight;
            lightOptimizer.vrIntensityMultiplier = vrIntensityBoost;
            lightOptimizer.useVRFillLight = useVRFillLight;
            lightOptimizer.enableShadows = true;
            lightOptimizer.shadowResolution = UnityEngine.Rendering.LightShadowResolution.High;
            
            Debug.Log("✓ VRLightOptimizer configured on spotlight");
            Debug.Log($"  VR Boost: {vrIntensityBoost}x");
            Debug.Log($"  Fill Light: {useVRFillLight}");
            return true;
        }
        
        return false;
    }
    
    void FindVolumetricBeam()
    {
        if (spotlight == null) return;
        
        volumetricBeam = spotlight.GetComponent<VolumetricLightBeam>();
        
        if (volumetricBeam != null)
        {
            Debug.Log("✓ Volumetric beam found");
            Debug.Log($"  Beam length: {volumetricBeam.beamLength}");
        }
        else
        {
            Debug.LogWarning("⚠ No volumetric beam component found");
            Debug.LogWarning("  This is optional but recommended for visual effect");
        }
    }
    
    [ContextMenu("Print Current Setup")]
    public void PrintCurrentSetup()
    {
        Debug.Log("=== Current VR Flashlight Setup ===");
        Debug.Log($"Flashlight Prefab: {(flashlightPrefab != null ? flashlightPrefab.name : "NOT FOUND")}");
        Debug.Log($"Flashlight Controller: {(flashlightController != null ? "✓ Found" : "✗ Missing")}");
        Debug.Log($"Spotlight: {(spotlight != null ? spotlight.gameObject.name : "NOT FOUND")}");
        Debug.Log($"Light Optimizer: {(lightOptimizer != null ? "✓ Found" : "✗ Missing")}");
        Debug.Log($"Volumetric Beam: {(volumetricBeam != null ? "✓ Found" : "⚠ Not Found")}");
        
        if (spotlight != null)
        {
            Debug.Log($"\nSpotlight Settings:");
            Debug.Log($"  Intensity: {spotlight.intensity}");
            Debug.Log($"  Range: {spotlight.range}");
            Debug.Log($"  Spot Angle: {spotlight.spotAngle}");
            Debug.Log($"  Shadows: {spotlight.shadows}");
        }
        
        if (lightOptimizer != null)
        {
            Debug.Log($"\nLight Optimizer Settings:");
            Debug.Log($"  VR Intensity Multiplier: {lightOptimizer.vrIntensityMultiplier}x");
            Debug.Log($"  Use VR Fill Light: {lightOptimizer.useVRFillLight}");
        }
    }
    
    [ContextMenu("Test VR Detection")]
    public void TestVRDetection()
    {
        bool vrActive = UnityEngine.XR.XRSettings.enabled && UnityEngine.XR.XRSettings.isDeviceActive;
        Debug.Log($"VR Status: {(vrActive ? "ACTIVE" : "Inactive")}");
        Debug.Log($"  XR Enabled: {UnityEngine.XR.XRSettings.enabled}");
        Debug.Log($"  Device Active: {UnityEngine.XR.XRSettings.isDeviceActive}");
        
        if (vrActive)
        {
            Debug.Log($"  Device Name: {UnityEngine.XR.XRSettings.loadedDeviceName}");
        }
    }
}
