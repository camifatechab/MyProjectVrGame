using UnityEngine;

/// <summary>
/// Creates fog-based transition when player passes through cloud layer.
/// Attach to empty GameObject in scene.
/// </summary>
public class CloudTransition : MonoBehaviour
{
    [Header("Transition Heights")]
    public float cloudBottomY = 80f;
    public float cloudTopY = 95f;
    
    [Header("Fog - Below Clouds")]
    public Color belowCloudsFogColor = new Color(0.7f, 0.85f, 1f, 1f);
    public float belowCloudsFogDensity = 0.0f; // No fog below clouds - keep colors bright
    
    [Header("Fog - Inside Clouds")]
    public Color insideCloudsFogColor = Color.white;
    public float insideCloudsFogDensity = 0.15f;
    
    [Header("Fog - Above Clouds (Paradise)")]
    public Color aboveCloudsFogColor = new Color(0.2f, 0.1f, 0.3f, 1f);
    public float aboveCloudsFogDensity = 0.005f;
    
    [Header("References")]
    public Transform playerTransform;
    
    [Header("Settings")]
    public float transitionSpeed = 3f;
    
    public enum CloudZone { BelowClouds, InsideClouds, AboveClouds }
    public CloudZone CurrentZone { get; private set; }
    
    private float targetFogDensity;
    private Color targetFogColor;
    private bool hasReachedParadise = false;
    
    // Store original fog settings to restore on disable
    private Color originalFogColor;
    private float originalFogDensity;
    private bool originalFogEnabled;
    private FogMode originalFogMode;

    
    public System.Action<CloudZone> OnZoneChanged;
    public System.Action OnFirstParadiseEntry;
    
    private void OnEnable()
    {
        // Save original fog settings
        originalFogColor = RenderSettings.fogColor;
        originalFogDensity = RenderSettings.fogDensity;
        originalFogEnabled = RenderSettings.fog;
        originalFogMode = RenderSettings.fogMode;
    }
    
    private void OnDisable()
    {
        // Restore original fog settings
        RenderSettings.fogColor = originalFogColor;
        RenderSettings.fogDensity = originalFogDensity;
        RenderSettings.fog = originalFogEnabled;
        RenderSettings.fogMode = originalFogMode;
    }
    
    
private void Start()
    {
        if (playerTransform == null)
        {
            var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null)
                playerTransform = xrOrigin.Camera.transform;
            else
                playerTransform = Camera.main?.transform;
        }
        
        if (playerTransform == null)
        {
            Debug.LogError("CloudTransition: No player transform found!");
            enabled = false;
            return;
        }
        
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        // Don't enable fog here - UpdateZone will handle it based on player height
        UpdateZone(true);
    }
    
    private void Update()
    {
        if (playerTransform == null) return;
        
        UpdateZone(false);
        
        RenderSettings.fogDensity = Mathf.Lerp(
            RenderSettings.fogDensity, 
            targetFogDensity, 
            transitionSpeed * Time.deltaTime
        );
        
        RenderSettings.fogColor = Color.Lerp(
            RenderSettings.fogColor, 
            targetFogColor, 
            transitionSpeed * Time.deltaTime
        );
    }
    
    private void UpdateZone(bool immediate)
    {
        float playerY = playerTransform.position.y;
        CloudZone newZone;
        
        if (playerY < cloudBottomY)
        {
            newZone = CloudZone.BelowClouds;
            targetFogDensity = belowCloudsFogDensity;
            targetFogColor = belowCloudsFogColor;
            RenderSettings.fog = false; // Disable fog below clouds for vibrant colors
        }
        else if (playerY > cloudTopY)
        {
            newZone = CloudZone.AboveClouds;
            targetFogDensity = aboveCloudsFogDensity;
            targetFogColor = aboveCloudsFogColor;
            RenderSettings.fog = true; // Enable fog above clouds
            RenderSettings.fog = true; // Enable fog above clouds
            
            if (!hasReachedParadise)
            {
                hasReachedParadise = true;
                OnFirstParadiseEntry?.Invoke();
                Debug.Log("CloudTransition: First time reaching paradise!");
            }
        }
        else
        {
            newZone = CloudZone.InsideClouds;
            RenderSettings.fog = true; // Enable fog inside clouds
            float progress = (playerY - cloudBottomY) / (cloudTopY - cloudBottomY);
            float intensity = Mathf.Sin(progress * Mathf.PI);
            targetFogDensity = Mathf.Lerp(belowCloudsFogDensity, insideCloudsFogDensity, intensity);
            targetFogColor = Color.Lerp(belowCloudsFogColor, insideCloudsFogColor, intensity);
        }
        
        if (newZone != CurrentZone)
        {
            CurrentZone = newZone;
            OnZoneChanged?.Invoke(newZone);
            Debug.Log($"CloudTransition: Entered {newZone}");
        }
        
        if (immediate)
        {
            RenderSettings.fogDensity = targetFogDensity;
            RenderSettings.fogColor = targetFogColor;
        }
    }
}
