using UnityEngine;

/// <summary>
/// Controls smooth skybox blending based on altitude or flight progress.
/// Requires a material using the Skybox/BlendedCubemap shader.
/// </summary>
public class SkyboxBlendController : MonoBehaviour
{
    [Header("Skybox Material")]
    [Tooltip("Material using Skybox/BlendedCubemap shader")]
    public Material blendedSkyboxMaterial;
    
    [Header("Blend Mode")]
    public BlendMode blendMode = BlendMode.Altitude;
    
    public enum BlendMode
    {
        Altitude,       // Blend based on height
        FlightProgress, // Blend based on waypoint progress
        Manual          // Control via script
    }
    
    [Header("Altitude Settings")]
    [Tooltip("Height where skybox A is fully shown (blend = 0)")]
    public float lowAltitude = 0f;
    
    [Tooltip("Height where skybox B is fully shown (blend = 1)")]
    public float highAltitude = 500f;
    
    [Tooltip("Transform to track altitude (player or creature)")]
    public Transform altitudeTarget;
    
    [Header("Transition Settings")]
    [Tooltip("How fast the blend transitions (higher = faster)")]
    public float transitionSpeed = 1f;
    
    [Tooltip("Use smooth step curve for more natural transition")]
    public bool useSmoothStep = true;
    
    [Tooltip("Add slight delay before transitioning back")]
    public float transitionDelay = 0.5f;
    
    [Header("Flight Progress Settings")]
    [Tooltip("Reference to RideableCreature for waypoint progress")]
    public RideableCreature rideableCreature;
    
    [Tooltip("Waypoint index where blend starts (0 = beginning)")]
    public int blendStartWaypoint = 3;
    
    [Tooltip("Waypoint index where blend completes")]
    public int blendEndWaypoint = 7;
    
    [Header("Visual Polish")]
    [Tooltip("Add subtle fog increase during transition")]
    public bool useFogTransition = true;
    
    [Tooltip("Fog density at low altitude")]
    public float lowAltitudeFog = 0.001f;
    
    [Tooltip("Fog density at high altitude")]
    public float highAltitudeFog = 0.0005f;
    
    [Tooltip("Fog color at low altitude")]
    public Color lowAltitudeFogColor = new Color(0.5f, 0.6f, 0.7f);
    
    [Tooltip("Fog color at high altitude")]
    public Color highAltitudeFogColor = new Color(0.8f, 0.85f, 1f);
    
    // Runtime
    private float currentBlend = 0f;
    private float targetBlend = 0f;
    private float delayTimer = 0f;
    private float lastTargetBlend = 0f;
    
    // Property name in shader
    private static readonly int BlendProperty = Shader.PropertyToID("_Blend");
    
    /// <summary>
    /// Current blend value (0 = Skybox A, 1 = Skybox B)
    /// </summary>
    public float CurrentBlend => currentBlend;
    
    /// <summary>
    /// Manually set the target blend value (only works in Manual mode)
    /// </summary>
    public void SetTargetBlend(float value)
    {
        if (blendMode == BlendMode.Manual)
        {
            targetBlend = Mathf.Clamp01(value);
        }
    }
    
    /// <summary>
    /// Force immediate blend (no transition)
    /// </summary>
    public void SetBlendImmediate(float value)
    {
        currentBlend = Mathf.Clamp01(value);
        targetBlend = currentBlend;
        ApplyBlend();
    }
    
    private void Start()
    {
        // Auto-find altitude target if not set
        if (altitudeTarget == null)
        {
            // Try to find player/creature
            var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null)
            {
                altitudeTarget = xrOrigin.transform;
            }
        }
        
        // Auto-find RideableCreature if not set
        if (rideableCreature == null)
        {
            rideableCreature = FindObjectOfType<RideableCreature>();
        }
        
        // Initialize fog settings
        if (useFogTransition)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
        }
        
        // Set initial blend
        ApplyBlend();
        
        Debug.Log($"SkyboxBlendController initialized. Mode: {blendMode}, Material: {(blendedSkyboxMaterial != null ? blendedSkyboxMaterial.name : "NULL")}");
    }
    
    private void Update()
    {
        // Calculate target blend based on mode
        switch (blendMode)
        {
            case BlendMode.Altitude:
                UpdateAltitudeBlend();
                break;
                
            case BlendMode.FlightProgress:
                UpdateFlightProgressBlend();
                break;
                
            case BlendMode.Manual:
                // Target set externally via SetTargetBlend()
                break;
        }
        
        // Handle transition delay (prevents flickering when hovering at threshold)
        if (Mathf.Abs(targetBlend - lastTargetBlend) > 0.01f)
        {
            delayTimer = transitionDelay;
            lastTargetBlend = targetBlend;
        }
        
        if (delayTimer > 0f)
        {
            delayTimer -= Time.deltaTime;
        }
        
        // Smooth transition to target
        if (delayTimer <= 0f || Mathf.Abs(currentBlend - targetBlend) > 0.1f)
        {
            float speed = transitionSpeed * Time.deltaTime;
            currentBlend = Mathf.MoveTowards(currentBlend, targetBlend, speed);
        }
        
        ApplyBlend();
    }
    
    private void UpdateAltitudeBlend()
    {
        if (altitudeTarget == null) return;
        
        float altitude = altitudeTarget.position.y;
        float rawBlend = Mathf.InverseLerp(lowAltitude, highAltitude, altitude);
        
        if (useSmoothStep)
        {
            // Smooth step for more natural feel
            rawBlend = rawBlend * rawBlend * (3f - 2f * rawBlend);
        }
        
        targetBlend = rawBlend;
    }
    
private void UpdateFlightProgressBlend()
    {
        if (rideableCreature == null) return;
        
        // Only blend while flying
        if (!rideableCreature.IsFlying)
        {
            // Keep current blend when not flying
            return;
        }
        
        int currentWP = rideableCreature.CurrentWaypointIndex;
        int totalWP = rideableCreature.TotalWaypoints;
        bool isReverse = rideableCreature.IsReversePath;
        
        // Calculate progress through blend zone
        float rawBlend;
        
        if (isReverse)
        {
            // Reverse: blend from 1 back to 0 as we go from end to start
            // When at high waypoint (near end), blend = 1
            // When at low waypoint (near start), blend = 0
            if (currentWP >= blendEndWaypoint)
                rawBlend = 1f;
            else if (currentWP <= blendStartWaypoint)
                rawBlend = 0f;
            else
                rawBlend = Mathf.InverseLerp(blendStartWaypoint, blendEndWaypoint, currentWP);
        }
        else
        {
            // Forward: blend from 0 to 1 as we progress through waypoints
            if (currentWP <= blendStartWaypoint)
                rawBlend = 0f;
            else if (currentWP >= blendEndWaypoint)
                rawBlend = 1f;
            else
                rawBlend = Mathf.InverseLerp(blendStartWaypoint, blendEndWaypoint, currentWP);
        }
        
        if (useSmoothStep)
        {
            rawBlend = rawBlend * rawBlend * (3f - 2f * rawBlend);
        }
        
        targetBlend = rawBlend;
    }
    
    private void ApplyBlend()
    {
        // Apply to material
        if (blendedSkyboxMaterial != null)
        {
            blendedSkyboxMaterial.SetFloat(BlendProperty, currentBlend);
        }
        
        // Apply fog transition
        if (useFogTransition)
        {
            RenderSettings.fogDensity = Mathf.Lerp(lowAltitudeFog, highAltitudeFog, currentBlend);
            RenderSettings.fogColor = Color.Lerp(lowAltitudeFogColor, highAltitudeFogColor, currentBlend);
        }
    }
    
    private void OnValidate()
    {
        // Apply changes in editor
        if (blendedSkyboxMaterial != null)
        {
            blendedSkyboxMaterial.SetFloat(BlendProperty, currentBlend);
        }
    }
    
    private void OnDestroy()
    {
        // Reset blend to 0 when destroyed (so material doesn't stay blended)
        if (blendedSkyboxMaterial != null)
        {
            blendedSkyboxMaterial.SetFloat(BlendProperty, 0f);
        }
    }
}
