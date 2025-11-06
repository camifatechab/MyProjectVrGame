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

    IEnumerator DelayedSetup()
    {
        // Wait one frame for AmbientSound to create its AudioSource
        yield return null;
        
        // Now find the surface audio source
        FindSurfaceAudioSource();
    }

    void Start()
    {
        // Create audio source for underwater ambience
        underwaterAudioSource = gameObject.AddComponent<AudioSource>();
        underwaterAudioSource.clip = underwaterAmbienceClip;
        underwaterAudioSource.loop = true;
        underwaterAudioSource.playOnAwake = false;
        underwaterAudioSource.volume = 0f;
        underwaterAudioSource.spatialBlend = 0f; // 2D sound
        
        // Delay finding the surface audio source to let AmbientSound create it first
        StartCoroutine(DelayedSetup());
        
        // Start underwater audio (at zero volume)
        if (underwaterAudioSource.clip != null)
        {
            underwaterAudioSource.Play();
            if (debugMode) Debug.Log("[UnderwaterAudio] Underwater audio source initialized and playing at 0 volume");
        }
        else
        {
            Debug.LogWarning("[UnderwaterAudio] No underwater ambience clip assigned!");
        }
    }

    void Update()
    {
        // Check if player is underwater
        float cameraY = transform.position.y;
        bool shouldBeUnderwater = (cameraY - detectionOffset) < waterSurfaceY;

        // State change detection
        if (shouldBeUnderwater != isUnderwater)
        {
            isUnderwater = shouldBeUnderwater;
            currentFadeTime = 0f;

            if (isUnderwater)
            {
                // Going underwater
                targetSurfaceVolume = 0f;
                targetUnderwaterVolume = underwaterVolume;
                if (debugMode) Debug.Log($"[UnderwaterAudio] Diving underwater at Y: {cameraY}");
            }
            else
            {
                // Surfacing
                targetSurfaceVolume = originalSurfaceVolume;
                targetUnderwaterVolume = 0f;
                if (debugMode) Debug.Log($"[UnderwaterAudio] Surfacing at Y: {cameraY}");
            }
        }

        // Smooth fade transition
        if (currentFadeTime < fadeDuration)
        {
            currentFadeTime += Time.deltaTime;
            float fadeProgress = Mathf.Clamp01(currentFadeTime / fadeDuration);
            
            // Smooth fade curve (ease in/out)
            fadeProgress = Mathf.SmoothStep(0f, 1f, fadeProgress);

            // Apply fade to both audio sources
            if (surfaceAudioSource != null)
            {
                float startSurfaceVol = isUnderwater ? originalSurfaceVolume : 0f;
                surfaceAudioSource.volume = Mathf.Lerp(startSurfaceVol, targetSurfaceVolume, fadeProgress);
            }

            if (underwaterAudioSource != null)
            {
                float startUnderwaterVol = isUnderwater ? 0f : underwaterVolume;
                underwaterAudioSource.volume = Mathf.Lerp(startUnderwaterVol, targetUnderwaterVolume, fadeProgress);
            }
        }
        else
        {
            // Ensure volumes are exactly at target after fade completes
            if (surfaceAudioSource != null)
            {
                surfaceAudioSource.volume = targetSurfaceVolume;
            }
            if (underwaterAudioSource != null)
            {
                underwaterAudioSource.volume = targetUnderwaterVolume;
            }
        }
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
