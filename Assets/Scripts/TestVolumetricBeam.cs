using UnityEngine;

/// <summary>
/// Simple test to verify if V-Light volumetric beam is visible
/// Attach to any GameObject and press T in Play mode to test
/// </summary>
public class TestVolumetricBeam : MonoBehaviour
{
    [Header("Test Key")]
    [SerializeField] private KeyCode testKey = KeyCode.T;
    
    [Header("Auto Test")]
    [SerializeField] private bool runTestOnStart = true;
    
    void Start()
    {
        if (runTestOnStart)
        {
            Invoke("RunTest", 2f);
        }
    }
    
    void Update()
    {
        if (Input.GetKeyDown(testKey))
        {
            RunTest();
        }
    }
    
    public void RunTest()
    {
        Debug.Log("════════════════════════════════════════");
        Debug.Log("     VOLUMETRIC BEAM TEST");
        Debug.Log("════════════════════════════════════════");
        
        // Find the spotlight
        GameObject spotlight = GameObject.Find("Spotlight");
        if (spotlight == null)
        {
            Debug.LogError("✗ Spotlight not found!");
            return;
        }
        
        Debug.Log($"✓ Spotlight found at: {spotlight.transform.position}");
        
        // Check Light component
        Light light = spotlight.GetComponent<Light>();
        if (light == null)
        {
            Debug.LogError("✗ No Light component!");
            return;
        }
        
        Debug.Log($"✓ Light: {(light.enabled ? "ON" : "OFF")}, Type: {light.type}");
        Debug.Log($"  Intensity: {light.intensity}, Range: {light.range}m, Angle: {light.spotAngle}°");
        
        // Check VLight component
        Component vlight = spotlight.GetComponent("VLight");
        if (vlight == null)
        {
            Debug.LogError("✗ No VLight component!");
            return;
        }
        
        Debug.Log("✓ VLight component present");
        
        // Check if VLight is enabled
        System.Type vlightType = vlight.GetType();
        var enabledProp = vlightType.GetProperty("enabled");
        if (enabledProp != null)
        {
            bool vlightEnabled = (bool)enabledProp.GetValue(vlight);
            Debug.Log($"  VLight enabled: {vlightEnabled}");
        }
        
        // Check fog (required for volumetric visibility)
        Debug.Log($"✓ Fog: {(RenderSettings.fog ? "ENABLED" : "DISABLED")}");
        if (RenderSettings.fog)
        {
            Debug.Log($"  Fog Density: {RenderSettings.fogDensity}");
            Debug.Log($"  Fog Color: {RenderSettings.fogColor}");
        }
        
        // Check Camera for rendering
        Camera cam = spotlight.GetComponent<Camera>();
        if (cam != null)
        {
            Debug.Log($"✓ Camera present: {(cam.enabled ? "ENABLED" : "DISABLED")}");
        }
        
        // Check for mesh renderer (V-Light creates this)
        MeshRenderer[] renderers = spotlight.GetComponentsInChildren<MeshRenderer>();
        Debug.Log($"✓ Found {renderers.Length} mesh renderer(s) under Spotlight");
        
        foreach (var renderer in renderers)
        {
            Debug.Log($"  - {renderer.gameObject.name}: {(renderer.enabled ? "visible" : "hidden")}");
        }
        
        Debug.Log("════════════════════════════════════════");
        Debug.Log("WHAT YOU SHOULD SEE:");
        Debug.Log("- A cone-shaped beam of light");
        Debug.Log("- More visible in deeper/darker areas");
        Debug.Log("- Illuminating fog particles");
        Debug.Log("");
        Debug.Log("IF YOU DON'T SEE THE BEAM:");
        Debug.Log("1. Increase VLight intensity to 100+");
        Debug.Log("2. Make sure you're in darker water");
        Debug.Log("3. Check fog density is at least 0.01");
        Debug.Log($"4. Press {testKey} to run test again");
        Debug.Log("════════════════════════════════════════");
    }
}
