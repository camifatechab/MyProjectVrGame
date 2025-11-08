using UnityEngine;

public class DepthFogController : MonoBehaviour
{
    [Header("Depth Zones")]
    [Tooltip("Y position of water surface")]
    public float waterSurfaceY = 47.665f;
    
    [Tooltip("Y position where mid-depth starts")]
    public float midDepthY = 25f;
    
    [Tooltip("Y position where deep zone starts")]
    public float deepZoneY = 0f;
    
    [Tooltip("Y position of cave floor")]
    public float caveFloorY = -23.52f;

    [Header("Fog Settings - Surface")]
    public Color surfaceFogColor = new Color(0.5f, 0.7f, 0.9f, 1f); // Light blue
    public float surfaceFogDensity = 0.005f;

    [Header("Fog Settings - Mid Depth")]
    public Color midDepthFogColor = new Color(0.2f, 0.4f, 0.6f, 1f); // Darker blue
    public float midDepthFogDensity = 0.02f;

    [Header("Fog Settings - Deep Zone")]
    public Color deepZoneFogColor = new Color(0.05f, 0.15f, 0.25f, 1f); // Very dark blue
    public float deepZoneFogDensity = 0.05f;

    [Header("Fog Settings - Cave Floor")]
    public Color caveFloorFogColor = new Color(0.02f, 0.05f, 0.08f, 1f); // Nearly black
    public float caveFloorFogDensity = 0.08f;

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

        // Determine which depth zone we're in and interpolate fog
        if (currentDepth >= waterSurfaceY)
        {
            // Above water - minimal fog
            RenderSettings.fogColor = surfaceFogColor;
            RenderSettings.fogDensity = surfaceFogDensity * 0.3f; // Even lighter above water
        }
        else if (currentDepth >= midDepthY)
        {
            // Surface to mid-depth transition
            float t = Mathf.InverseLerp(waterSurfaceY, midDepthY, currentDepth);
            RenderSettings.fogColor = Color.Lerp(surfaceFogColor, midDepthFogColor, t);
            RenderSettings.fogDensity = Mathf.Lerp(surfaceFogDensity, midDepthFogDensity, t);
        }
        else if (currentDepth >= deepZoneY)
        {
            // Mid-depth to deep zone transition
            float t = Mathf.InverseLerp(midDepthY, deepZoneY, currentDepth);
            RenderSettings.fogColor = Color.Lerp(midDepthFogColor, deepZoneFogColor, t);
            RenderSettings.fogDensity = Mathf.Lerp(midDepthFogDensity, deepZoneFogDensity, t);
        }
        else if (currentDepth >= caveFloorY)
        {
            // Deep zone to cave floor transition
            float t = Mathf.InverseLerp(deepZoneY, caveFloorY, currentDepth);
            RenderSettings.fogColor = Color.Lerp(deepZoneFogColor, caveFloorFogColor, t);
            RenderSettings.fogDensity = Mathf.Lerp(deepZoneFogDensity, caveFloorFogDensity, t);
        }
        else
        {
            // Below cave floor (shouldn't happen, but just in case)
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
