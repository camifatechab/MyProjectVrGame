using UnityEngine;

/// <summary>
/// Runtime diagnostics for V-Light setup
/// Attach this to any GameObject and it will check your V-Light setup in Play mode
/// </summary>
public class VLightDiagnostics : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private bool runTestOnStart = true;
    [SerializeField] private KeyCode testKey = KeyCode.F9;
    
    void Start()
    {
        if (runTestOnStart)
        {
            Invoke("RunDiagnostics", 1f); // Wait 1 second for scene to initialize
        }
    }
    
    void Update()
    {
        if (Input.GetKeyDown(testKey))
        {
            RunDiagnostics();
        }
    }
    
    public void RunDiagnostics()
    {
        Debug.Log("╔══════════════════════════════════════════════╗");
        Debug.Log("║     V-LIGHT DIAGNOSTICS REPORT              ║");
        Debug.Log("╚══════════════════════════════════════════════╝");
        
        // Find Spotlight
        GameObject spotlight = GameObject.Find("Spotlight");
        if (spotlight == null)
        {
            Debug.LogError("✗ CRITICAL: Spotlight GameObject not found!");
            return;
        }
        Debug.Log("✓ Spotlight GameObject found");
        
        // Check Light component
        Light lightComp = spotlight.GetComponent<Light>();
        if (lightComp == null)
        {
            Debug.LogError("✗ CRITICAL: No Light component on Spotlight!");
            return;
        }
        Debug.Log($"✓ Light Component: Type={lightComp.type}, Enabled={lightComp.enabled}");
        Debug.Log($"  - Intensity: {lightComp.intensity}");
        Debug.Log($"  - Range: {lightComp.range}m");
        Debug.Log($"  - Spot Angle: {lightComp.spotAngle}°");
        Debug.Log($"  - Color: {lightComp.color}");
        
        // Check VLight component
        Component vlight = spotlight.GetComponent("VLight");
        if (vlight == null)
        {
            Debug.LogError("✗ CRITICAL: No VLight component on Spotlight!");
            Debug.Log("  → Add VLight component from V-Light asset package");
            return;
        }
        Debug.Log("✓ VLight Component found");
        
        // Check Camera component (required by V-Light)
        Camera cam = spotlight.GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogWarning("✗ WARNING: No Camera component (V-Light may need this)");
        }
        else
        {
            Debug.Log($"✓ Camera Component: Enabled={cam.enabled}");
        }
        
        // Check if Spotlight is child of Right Controller
        Transform parent = spotlight.transform.parent;
        if (parent != null && parent.parent != null)
        {
            Debug.Log($"✓ Spotlight hierarchy: {parent.parent.name}/{parent.name}/{spotlight.name}");
        }
        
        // Check Fog settings
        Debug.Log($"✓ Fog Enabled: {RenderSettings.fog}");
        Debug.Log($"  - Fog Density: {RenderSettings.fogDensity}");
        Debug.Log($"  - Fog Color: {RenderSettings.fogColor}");
        
        // Check if flashlight is active
        bool flashlightActive = spotlight.activeSelf && lightComp.enabled;
        if (flashlightActive)
        {
            Debug.Log("<color=green>✓ Flashlight is ACTIVE - you should see a beam!</color>");
        }
        else
        {
            Debug.Log("<color=yellow>⚠ Flashlight is INACTIVE - press grip button to activate</color>");
        }
        
        // Performance check
        int triangleCount = 0;
        MeshFilter[] meshes = spotlight.GetComponentsInChildren<MeshFilter>();
        foreach (var mf in meshes)
        {
            if (mf.sharedMesh != null)
            {
                triangleCount += mf.sharedMesh.triangles.Length / 3;
            }
        }
        Debug.Log($"✓ Spotlight child triangles: {triangleCount}");
        
        Debug.Log("╔══════════════════════════════════════════════╗");
        Debug.Log("║     END OF DIAGNOSTICS                       ║");
        Debug.Log("╚══════════════════════════════════════════════╝");
        Debug.Log($"Press {testKey} to run diagnostics again");
    }
}
