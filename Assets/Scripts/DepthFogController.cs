using UnityEngine;

public class DepthFogController : MonoBehaviour
{
    [Header("Depth Zones")]
    [Tooltip("Y position of water surface")]
    public float waterSurfaceY = 47.665f;
    
    [Tooltip("Y position where mid-depth starts")]
    public float midDepthY = 30f;
    
    [Tooltip("Y position where deep zone starts")]
    public float deepZoneY = 10f;
    
    [Tooltip("Y position of cave floor")]
    public float caveFloorY = -23.52f;

    
    [Header("Ambient Light Control")]
    [Tooltip("Ambient light intensity at surface (bright)")]
    [Range(0f, 2f)]
    public float surfaceAmbientIntensity = 1.0f;
    
    [Tooltip("Ambient light intensity at cave floor (pitch black)")]
    [Range(0f, 2f)]
    public float caveFloorAmbientIntensity = 0.0f;
[Header("Fog Colors - All Based on Surface Blue")]
    [Tooltip("Surface/shallow water - same as your water color")]
    public Color surfaceFogColor = new Color(0.08f, 0.18f, 0.35f, 1f); // Your dark navy blue
    
    [Tooltip("Mid-depth - darker version of surface blue")]
    public Color midDepthFogColor = new Color(0.05f, 0.12f, 0.25f, 1f); // Darker navy
    
    [Tooltip("Deep zone - much darker blue")]
    public Color deepZoneFogColor = new Color(0.02f, 0.06f, 0.15f, 1f); // Very dark blue
    
    [Tooltip("Cave floor - almost black with blue hint")]
    public Color caveFloorFogColor = new Color(0.01f, 0.02f, 0.08f, 1f); // Nearly black

    [Header("Fog Density - MUCH THICKER")]
    [Tooltip("Surface fog density - thick so bottom is NOT visible")]
    [Range(0.01f, 0.2f)]
    public float surfaceFogDensity = 0.04f;
    
    [Tooltip("Mid-depth fog density")]
    [Range(0.01f, 0.2f)]
    public float midDepthFogDensity = 0.06f;
    
    [Tooltip("Deep zone fog density")]
    [Range(0.01f, 0.2f)]
    public float deepZoneFogDensity = 0.08f;
    
    [Tooltip("Cave floor fog density - maximum darkness")]
    [Range(0.01f, 0.2f)]
    public float caveFloorFogDensity = 0.12f;

    [Header("References")]
    [Tooltip("The camera to track depth (usually Main Camera)")]
    public Transform playerCamera;

    private void Start()
    {
        // Enable fog
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared; // Exponential fog for underwater feel

        // Find camera if not assigned
        if (playerCamera == null)
        {
            playerCamera = Camera.main.transform;
        }

        if (playerCamera == null)
        {
            Debug.LogError("DepthFogController: No camera found! Please assign playerCamera.");
            enabled = false;
        }
    }

private void Update()
    {
        if (playerCamera == null) return;

        float currentDepth = playerCamera.position.y;
        float normalizedDepth = 0f;

        // Calculate normalized depth (0 = surface, 1 = cave floor)
        if (currentDepth >= waterSurfaceY)
        {
            normalizedDepth = 0f;
        }
        else if (currentDepth <= caveFloorY)
        {
            normalizedDepth = 1f;
        }
        else
        {
            // Inverse lerp from surface to cave floor
            normalizedDepth = Mathf.InverseLerp(waterSurfaceY, caveFloorY, currentDepth);
        }

        // EXPONENTIAL fog density growth - this makes the difference!
        // At surface (0): very light fog
        // At cave floor (1): extremely thick fog
        float baseDensity = 0.02f; // Increased for more atmosphere
        float maxDensity = 0.25f; // Balanced for darkness while flashlight still works
        float fogDensity = Mathf.Lerp(baseDensity, maxDensity, normalizedDepth * normalizedDepth * normalizedDepth);

        // Smooth color transition from bright blue to nearly black
        Color currentFogColor;
        if (currentDepth >= waterSurfaceY)
        {
            currentFogColor = surfaceFogColor;
        }
        else if (currentDepth >= midDepthY)
        {
            float t = Mathf.InverseLerp(waterSurfaceY, midDepthY, currentDepth);
            t = Mathf.SmoothStep(0, 1, t);
            currentFogColor = Color.Lerp(surfaceFogColor, midDepthFogColor, t);
        }
        else if (currentDepth >= deepZoneY)
        {
            float t = Mathf.InverseLerp(midDepthY, deepZoneY, currentDepth);
            t = Mathf.SmoothStep(0, 1, t);
            currentFogColor = Color.Lerp(midDepthFogColor, deepZoneFogColor, t);
        }
        else if (currentDepth >= caveFloorY)
        {
            float t = Mathf.InverseLerp(deepZoneY, caveFloorY, currentDepth);
            t = Mathf.SmoothStep(0, 1, t);
            currentFogColor = Color.Lerp(deepZoneFogColor, caveFloorFogColor, t);
        }
        else
        {
            currentFogColor = caveFloorFogColor;
        }

        // Apply fog settings
        RenderSettings.fogColor = currentFogColor;
        RenderSettings.fogDensity = fogDensity;

        // CRITICAL: Control ambient light intensity - this makes objects actually dark!
        float ambientIntensity = Mathf.Lerp(surfaceAmbientIntensity, caveFloorAmbientIntensity, normalizedDepth * normalizedDepth);
        RenderSettings.ambientIntensity = ambientIntensity;
        
        // Also darken ambient light color at depth
        Color ambientColor = Color.Lerp(new Color(0.3f, 0.3f, 0.3f), Color.black, normalizedDepth * normalizedDepth);
        RenderSettings.ambientLight = ambientColor;
        
        // CRITICAL: Kill skybox contribution at cave floor
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientSkyColor = ambientColor;
        RenderSettings.ambientEquatorColor = ambientColor;
        RenderSettings.ambientGroundColor = ambientColor;
        
        // CRITICAL: Disable reflection probes at depth
        float reflectionIntensity = Mathf.Lerp(1.0f, 0.0f, normalizedDepth * normalizedDepth);
        RenderSettings.reflectionIntensity = reflectionIntensity;
        RenderSettings.defaultReflectionResolution = 16; // Minimal for performance

        // Debug in cave zone
        if (currentDepth < deepZoneY && Time.frameCount % 60 == 0)
        {
            Debug.Log($"Depth: {currentDepth:F1}m | Fog Density: {fogDensity:F3} | Normalized: {normalizedDepth:F2}");
        }
    }

    private void OnDisable()
    {
        // Optional: Reset fog when script is disabled
        // RenderSettings.fog = false;
    }

    // Visualize depth zones in Scene view
    private void OnDrawGizmos()
    {
        Gizmos.color = surfaceFogColor;
        Gizmos.DrawWireCube(new Vector3(0, waterSurfaceY, 0), new Vector3(100, 0.5f, 100));

        Gizmos.color = midDepthFogColor;
        Gizmos.DrawWireCube(new Vector3(0, midDepthY, 0), new Vector3(100, 0.5f, 100));

        Gizmos.color = deepZoneFogColor;
        Gizmos.DrawWireCube(new Vector3(0, deepZoneY, 0), new Vector3(100, 0.5f, 100));

        Gizmos.color = caveFloorFogColor;
        Gizmos.DrawWireCube(new Vector3(0, caveFloorY, 0), new Vector3(100, 0.5f, 100));
    }
}