// Updated
using System.Collections;

using UnityEngine;

/// <summary>
/// Manages audio transitions when the player goes underwater.
/// Smoothly fades between surface ambient sounds and underwater ambience.
/// Non-destructive - works alongside existing AmbientSound components.
/// </summary>
public class UnderwaterAudioTransition : MonoBehaviour
{
    [Header("Water Detection")]
    [Tooltip("Y position of the water surface")]
    public float waterSurfaceY = 47.665f;
    
    [Tooltip("Offset ABOVE water surface0.0ftrigger splash early (feet detection)")]
    public float splashTriggerOffset = 0.8f;
    
    [Tooltip("Offset below camera to check (prevents flickering at exact surface)")]
    public float detectionOffset = 0.2f;

    [Header("Audio Settings")]
    [Tooltip("The underwater ambience audio clip")]
    public AudioClip underwaterAmbienceClip;
    
    [Tooltip("Volume of underwater ambience")]
    [Range(0f, 1f)]
    public float underwaterVolume = 0.8f;
    
    [Tooltip("How long the fade transition takes (seconds)")]
    [Range(0.1f, 3f)]
    public float fadeDuration = 1.0f;

    [Header("Splash & Effects")]
    [Tooltip("Splash sound when entering water")]
    public AudioClip splashEnterClip;
    
    [Tooltip("Gurgle/bubble sound when submerging")]
    public AudioClip gurgleClip;
    
    [Tooltip("Volume for splash sounds")]
    [Range(0f, 1f)]
    public float splashVolume = 0.7f;
    
    [Tooltip("Volume for gurgle sound")]
    [Range(0f, 1f)]
    public float gurgleVolume = 0.5f;
    
    [Tooltip("Minimum vertical speed to trigger splash (m/s)")]
    public float minSplashSpeed = 0.5f;

    [Header("Surface Audio Control")]
    [Tooltip("Reference to the WaterSurface GameObject (will find AmbientSound on it)")]
    public GameObject waterSurfaceObject;

    [Header("Debug")]
    [Tooltip("Show debug messages in console")]
    public bool debugMode = false;

    // Private variables
    private AudioSource underwaterAudioSource;
    private AudioSource surfaceAudioSource;
    private bool isUnderwater = false;
    private float currentFadeTime = 0f;
    private float targetSurfaceVolume = 0f;
    private float targetUnderwaterVolume = 0f;
    private float originalSurfaceVolume = 1f;
    private Vector3 lastPosition;
    
    private bool wasAboveWater = true;
    private Transform playerHead;
    private Transform playerBody; // XR Origin for body/feet position
    private bool wasUnderwater = false;
    private bool hasSplashed = false;
    private bool bodyInWater = false;

private float lastFrameTime;

    IEnumerator DelayedSetup()
    {
        // Wait one frame for AmbientSound to create its AudioSource
        yield return null;
        
        // Now find the surface audio source
        FindSurfaceAudioSource();
    }

void Start()
    {
        // Find the player's head (Main Camera) for underwater audio transition
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            playerHead = mainCamera.transform;
            if (debugMode) Debug.Log("[UnderwaterAudio] Found player head: " + playerHead.name);
        }
        else
        {
            Debug.LogError("[UnderwaterAudio] No Main Camera found! Please tag your VR camera as MainCamera.");
        }
        
        // Find the XR Origin (player body/feet position) for splash detection
        GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
        if (xrOrigin != null)
        {
            playerBody = xrOrigin.transform;
            if (debugMode) Debug.Log("[UnderwaterAudio] Found player body (XR Origin): " + playerBody.name);
        }
        else
        {
            Debug.LogWarning("[UnderwaterAudio] XR Origin not found by name. Using camera position for splash detection.");
            playerBody = playerHead; // Fallback to head if body not found
        }
        
        // Create audio source for underwater ambience
        underwaterAudioSource = gameObject.AddComponent<AudioSource>();
        underwaterAudioSource.clip = underwaterAmbienceClip;
        underwaterAudioSource.loop = true;
        underwaterAudioSource.playOnAwake = false;
        underwaterAudioSource.volume = 0f;
        underwaterAudioSource.spatialBlend = 0f; // 2D sound
        
        // Initialize position tracking for velocity calculation
        lastPosition = transform.position;
        lastFrameTime = Time.time;
        
        // Initialize state based on starting position
        float startY = transform.position.y;
        wasAboveWater = startY > waterSurfaceY;
        isUnderwater = (startY - detectionOffset) < waterSurfaceY;
        
        if (debugMode) Debug.Log($"[UnderwaterAudio] Starting at Y: {startY}, wasAboveWater: {wasAboveWater}, isUnderwater: {isUnderwater}");
        
        // Delay finding the surface audio source to let AmbientSound create it first
        StartCoroutine(DelayedSetup());
        
        // Start underwater audio (at zero volume initially, will adjust based on starting state)
        if (underwaterAudioSource.clip != null)
        {
            underwaterAudioSource.Play();
            
            // If starting underwater, set volume immediately
            if (isUnderwater)
            {
                underwaterAudioSource.volume = underwaterVolume;
                targetUnderwaterVolume = underwaterVolume;
                targetSurfaceVolume = 0f;
                if (debugMode) Debug.Log("[UnderwaterAudio] Started underwater - setting volumes immediately");
            }
            else
            {
                underwaterAudioSource.volume = 0f;
                if (debugMode) Debug.Log("[UnderwaterAudio] Underwater audio source initialized at 0 volume");
            }
        }
        else
        {
            Debug.LogWarning("[UnderwaterAudio] No underwater ambience clip assigned!");
        }
    }

void Update()
    {
        if (playerHead == null || playerBody == null) return;

        float currentBodyY = playerBody.position.y;
        
        // Check if BODY/FEET just crossed the water surface this frame
        bool bodyTouchingWater = currentBodyY < waterSurfaceY;
        
        // INSTANT splash detection: Check if we crossed the water surface THIS frame
        // This catches fast-falling players by checking the previous frame position
        if (!hasSplashed)
        {
            // If we just crossed from above water to below water this frame
            if (bodyTouchingWater && !bodyInWater)
            {
                PlaySplashSound();
                hasSplashed = true; // Mark that splash has played - NEVER reset this!
                if (debugMode) Debug.Log($"[UnderwaterAudio] SPLASH! Body Y: {currentBodyY}, Water Y: {waterSurfaceY}");
            }
            // ALSO check if we're falling fast and about to cross (predictive splash)
            else if (!bodyInWater && currentBodyY > waterSurfaceY)
            {
                // Calculate velocity (how fast we're falling)
                float velocity = (currentBodyY - lastPosition.y) / Time.deltaTime;
                
                // If falling downward and will cross water surface in next frame
                if (velocity < -0.5f) // Falling down (negative velocity)
                {
                    float distanceToWater = currentBodyY - waterSurfaceY;
                    float timeToImpact = distanceToWater / Mathf.Abs(velocity);
                    
                    // If we'll hit water in less than one frame, play splash NOW
                    if (timeToImpact < Time.deltaTime * 2f)
                    {
                        PlaySplashSound();
                        hasSplashed = true;
                        if (debugMode) Debug.Log($"[UnderwaterAudio] PREDICTIVE SPLASH! Body Y: {currentBodyY}, Velocity: {velocity}, Time to impact: {timeToImpact}");
                    }
                }
            }
        }
        
        // Update body in water state and position tracking
        bodyInWater = bodyTouchingWater;
        lastPosition = playerBody.position;

        // Check if HEAD is underwater (for audio transition)
        bool headIsUnderwater = playerHead.position.y < waterSurfaceY;

        // Handle ambience transition based on HEAD position
        if (headIsUnderwater != wasUnderwater)
        {
            StartCoroutine(TransitionAmbience(headIsUnderwater));
            wasUnderwater = headIsUnderwater;
            if (debugMode) Debug.Log($"[UnderwaterAudio] Head underwater changed to: {headIsUnderwater}");
        }
    }

    /// <summary>
    /// Plays the splash sound effect
    /// </summary>
    void PlaySplashSound()
    {
        if (splashEnterClip != null)
        {
            AudioSource.PlayClipAtPoint(splashEnterClip, transform.position, splashVolume);
            if (debugMode) Debug.Log("[UnderwaterAudio] Splash!");
        }
    }

    /// <summary>
    /// Plays the gurgle sound effect when submerging
    /// </summary>
    void PlayGurgleSound()
    {
        if (gurgleClip != null)
        {
            AudioSource.PlayClipAtPoint(gurgleClip, transform.position, gurgleVolume);
            if (debugMode) Debug.Log("[UnderwaterAudio] Gurgle!");
        }
    }

/// <summary>
    /// Smoothly transitions between surface and underwater ambience
    /// </summary>
    IEnumerator TransitionAmbience(bool enteringWater)
    {
        float elapsedTime = 0f;
        float startUnderwaterVol = underwaterAudioSource != null ? underwaterAudioSource.volume : 0f;
        float startSurfaceVol = surfaceAudioSource != null ? surfaceAudioSource.volume : originalSurfaceVolume;
        
        float targetUnderwaterVol = enteringWater ? underwaterVolume : 0f;
        float targetSurfaceVol = enteringWater ? 0f : originalSurfaceVolume;
        
        if (debugMode) Debug.Log($"[UnderwaterAudio] Starting transition - Entering: {enteringWater}, Underwater: {startUnderwaterVol}->{targetUnderwaterVol}, Surface: {startSurfaceVol}->{targetSurfaceVol}");
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;
            
            if (underwaterAudioSource != null)
            {
                underwaterAudioSource.volume = Mathf.Lerp(startUnderwaterVol, targetUnderwaterVol, t);
            }
            
            if (surfaceAudioSource != null)
            {
                surfaceAudioSource.volume = Mathf.Lerp(startSurfaceVol, targetSurfaceVol, t);
            }
            
            yield return null;
        }
        
        // Ensure final volumes are set
        if (underwaterAudioSource != null) underwaterAudioSource.volume = targetUnderwaterVol;
        if (surfaceAudioSource != null) surfaceAudioSource.volume = targetSurfaceVol;
        
        if (debugMode) Debug.Log($"[UnderwaterAudio] Transition complete - Underwater: {targetUnderwaterVol}, Surface: {targetSurfaceVol}");
    }


    /// <summary>
    /// Finds and caches the surface audio source from the AmbientSound component
    /// </summary>
    void FindSurfaceAudioSource()
    {
        if (waterSurfaceObject == null)
        {
            // Try to find WaterSurface by name
            waterSurfaceObject = GameObject.Find("WaterSurface");
            if (waterSurfaceObject == null)
            {
                Debug.LogWarning("[UnderwaterAudio] Could not find WaterSurface GameObject. Please assign it manually.");
                return;
            }
        }

        // Get the AmbientSound component
        AmbientSound ambientSound = waterSurfaceObject.GetComponent<AmbientSound>();
        if (ambientSound != null)
        {
            // Get the audio source from AmbientSound
            surfaceAudioSource = waterSurfaceObject.GetComponent<AudioSource>();
            if (surfaceAudioSource != null)
            {
                originalSurfaceVolume = surfaceAudioSource.volume;
                // Initialize to current state - if starting underwater, target should be 0
                targetSurfaceVolume = originalSurfaceVolume;
                if (debugMode) Debug.Log($"[UnderwaterAudio] Found surface audio source. Original volume: {originalSurfaceVolume}");
            }
            else
            {
                Debug.LogWarning("[UnderwaterAudio] AmbientSound found but no AudioSource component!");
            }
        }
        else
        {
            Debug.LogWarning("[UnderwaterAudio] No AmbientSound component found on WaterSurface!");
        }
    }

    /// <summary>
    /// Helper to manually refresh the surface audio source reference
    /// </summary>
    public void RefreshSurfaceAudioSource()
    {
        FindSurfaceAudioSource();
    }

    // Visual helper in Scene view
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Draw water surface line
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
        Vector3 surfacePos = new Vector3(transform.position.x, waterSurfaceY, transform.position.z);
        Gizmos.DrawWireCube(surfacePos, new Vector3(10f, 0.1f, 10f));

        // Draw detection point
        Gizmos.color = isUnderwater ? Color.cyan : Color.yellow;
        Gizmos.DrawWireSphere(transform.position - new Vector3(0, detectionOffset, 0), 0.2f);
    }
}
