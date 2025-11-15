using UnityEngine;
using UnityEngine.Rendering;

public class DepthPostProcessing : MonoBehaviour
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

    [Header("Post-Processing Settings")]
    [Tooltip("The global Volume component")]
    public Volume globalVolume;
    
    [Tooltip("Exposure at surface (bright)")]
    [Range(-3f, 3f)]
    public float surfaceExposure = 0.5f;
    
    [Tooltip("Exposure at mid-depth")]
    [Range(-3f, 3f)]
    public float midDepthExposure = 0f;
    
    [Tooltip("Exposure at deep zone")]
    [Range(-3f, 3f)]
    public float deepExposure = -1f;
    
    [Tooltip("Exposure at cave floor (very dark)")]
    [Range(-3f, 3f)]
    public float caveFloorExposure = -1.8f; // Reduced from -2.5 so flashlight is visible

    [Header("References")]
    [Tooltip("The camera to track depth")]
    public Transform playerCamera;

    private VolumeProfile volumeProfile;

    private void Start()
    {
        // Find camera if not assigned
        if (playerCamera == null)
        {
            playerCamera = Camera.main.transform;
        }

        // Get or create volume
        if (globalVolume == null)
        {
            globalVolume = GetComponent<Volume>();
        }

        if (globalVolume != null && globalVolume.profile != null)
        {
            volumeProfile = globalVolume.profile;
            Debug.Log("DepthPostProcessing: Volume profile found");
        }
        else
        {
            Debug.LogWarning("DepthPostProcessing: No Volume profile assigned!");
        }
    }

    private void Update()
    {
        if (playerCamera == null || volumeProfile == null) return;

        float currentDepth = playerCamera.position.y;
        float targetExposure = CalculateExposure(currentDepth);

        // Apply exposure to volume
        ApplyExposure(targetExposure);
    }

    private float CalculateExposure(float depth)
    {
        if (depth >= waterSurfaceY)
        {
            return surfaceExposure;
        }
        else if (depth >= midDepthY)
        {
            float t = Mathf.InverseLerp(waterSurfaceY, midDepthY, depth);
            t = Mathf.SmoothStep(0, 1, t);
            return Mathf.Lerp(surfaceExposure, midDepthExposure, t);
        }
        else if (depth >= deepZoneY)
        {
            float t = Mathf.InverseLerp(midDepthY, deepZoneY, depth);
            t = Mathf.SmoothStep(0, 1, t);
            return Mathf.Lerp(midDepthExposure, deepExposure, t);
        }
        else if (depth >= caveFloorY)
        {
            float t = Mathf.InverseLerp(deepZoneY, caveFloorY, depth);
            t = Mathf.SmoothStep(0, 1, t);
            return Mathf.Lerp(deepExposure, caveFloorExposure, t);
        }
        else
        {
            return caveFloorExposure;
        }
    }

    private void ApplyExposure(float exposure)
    {
        // Try to get ColorAdjustments from URP
        if (volumeProfile.TryGet<UnityEngine.Rendering.Universal.ColorAdjustments>(out var colorAdjustments))
        {
            colorAdjustments.postExposure.value = exposure;
        }
    }
}
