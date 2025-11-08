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

        // Determine which depth zone we're in and interpolate fog SMOOTHLY
        if (currentDepth >= waterSurfaceY)
        {
            // Above water - use surface fog (still thick enough to hide bottom)
            RenderSettings.fogColor = surfaceFogColor;
            RenderSettings.fogDensity = surfaceFogDensity * 0.5f; // Slightly lighter above water
        }
        else if (currentDepth >= midDepthY)
        {
            // Surface to mid-depth transition - SMOOTH
            float t = Mathf.InverseLerp(waterSurfaceY, midDepthY, currentDepth);
            t = Mathf.SmoothStep(0, 1, t); // Smooth interpolation for gradual transition
            RenderSettings.fogColor = Color.Lerp(surfaceFogColor, midDepthFogColor, t);
            RenderSettings.fogDensity = Mathf.Lerp(surfaceFogDensity, midDepthFogDensity, t);
        }
        else if (currentDepth >= deepZoneY)
        {
            // Mid-depth to deep zone transition - SMOOTH
            float t = Mathf.InverseLerp(midDepthY, deepZoneY, currentDepth);
            t = Mathf.SmoothStep(0, 1, t); // Smooth interpolation
            RenderSettings.fogColor = Color.Lerp(midDepthFogColor, deepZoneFogColor, t);
            RenderSettings.fogDensity = Mathf.Lerp(midDepthFogDensity, deepZoneFogDensity, t);
        }
        else if (currentDepth >= caveFloorY)
        {
            // Deep zone to cave floor transition - SMOOTH
            float t = Mathf.InverseLerp(deepZoneY, caveFloorY, currentDepth);
            t = Mathf.SmoothStep(0, 1, t); // Smooth interpolation
            RenderSettings.fogColor = Color.Lerp(deepZoneFogColor, caveFloorFogColor, t);
            RenderSettings.fogDensity = Mathf.Lerp(deepZoneFogDensity, caveFloorFogDensity, t);
        }
        else
        {
            // Below cave floor
            RenderSettings.fogColor = caveFloorFogColor;
            RenderSettings.fogDensity = caveFloorFogDensity;
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