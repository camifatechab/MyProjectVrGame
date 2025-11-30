using UnityEngine;

/// <summary>
/// Handles underwater atmosphere effects:
/// - Breathing audio loop when in water
/// - Heartbeat that fades in at depth
/// - Flashlight flicker that intensifies with depth
/// </summary>
public class UnderwaterAtmosphere : MonoBehaviour
{
    [Header("=== AUDIO CLIPS ===")]
    [Tooltip("Breathing/regulator loop audio")]
    public AudioClip breathingClip;
    
    [Tooltip("Heartbeat loop audio")]
    public AudioClip heartbeatClip;
    
    [Header("=== BREATHING SETTINGS ===")]
    [Tooltip("Volume for breathing (keep low to not overwhelm)")]
    [Range(0f, 1f)]
    public float breathingVolume = 0.35f;
    
    [Header("=== HEARTBEAT SETTINGS ===")]
    [Tooltip("Maximum heartbeat volume at deepest point")]
    [Range(0f, 1f)]
    public float maxHeartbeatVolume = 0.6f;
    
    [Tooltip("How fast heartbeat fades in/out (seconds)")]
    public float heartbeatFadeSpeed = 2f;
    
    [Header("=== HEARTBEAT PROXIMITY ===")]
    [Tooltip("Speed up heartbeat when near spaceship parts")]
    public bool enableProximityHeartbeat = true;
    
    [Tooltip("Distance at which heartbeat starts speeding up")]
    public float proximityDistance = 15f;
    
    [Tooltip("Maximum pitch multiplier when very close (1.5 = 50% faster)")]
    public float maxProximityPitch = 1.5f;
    
    [Tooltip("Tag used for spaceship parts")]
    public string spaceshipPartTag = "SpaceshipPart";

    
    [Header("=== FLASHLIGHT FLICKER SETTINGS ===")]
    [Tooltip("Reference to VRFlashlightController (auto-finds if empty)")]
    public VRFlashlightController flashlightController;
    
    [Tooltip("Minimum time between flickers at surface")]
    public float minFlickerInterval = 4f;
    
    [Tooltip("Maximum time between flickers at surface")]
    public float maxFlickerInterval = 8f;
    
    [Tooltip("Minimum time between flickers at max depth")]
    public float minFlickerIntervalDeep = 0.5f;
    
    [Tooltip("Maximum time between flickers at max depth")]
    public float maxFlickerIntervalDeep = 2f;
    
    [Tooltip("How long each flicker lasts")]
    public float flickerDuration = 0.08f;
    
    [Tooltip("Chance of double-flicker at depth (0-1)")]
    [Range(0f, 1f)]
    public float doubleFlickerChance = 0.3f;
    
    [Header("=== DEPTH SETTINGS ===")]
    [Tooltip("Y position where effects START (entering deep zone)")]
    public float effectStartY = 10f;
    
    [Tooltip("Y position where effects are at MAXIMUM (cave floor)")]
    public float effectMaxY = -20f;
    
    [Tooltip("Camera/player transform to track depth (auto-finds Main Camera)")]
    public Transform playerTransform;
    
    // Private references
    private AudioSource breathingSource;
    private AudioSource heartbeatSource;
    private Light spotlightRef;
    private float originalSpotlightIntensity;
    
    // State tracking
    private bool isInWater = false;
    private float currentHeartbeatVolume = 0f;
    private float targetHeartbeatVolume = 0f;
    private float nextFlickerTime = 0f;
    private bool isFlickering = false;
    private float flickerEndTime = 0f;
    private int flickerCount = 0;
    private int flickersRemaining = 0;
    
    // Proximity tracking
    private GameObject[] spaceshipParts;
    private float proximityFactor = 0f;

    
    void Start()
    {
        // Find player camera if not assigned
        if (playerTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                playerTransform = mainCam.transform;
                Debug.Log("UnderwaterAtmosphere: Found Main Camera for depth tracking");
            }
        }
        
        // Find flashlight controller if not assigned
        if (flashlightController == null)
        {
            flashlightController = FindObjectOfType<VRFlashlightController>();
            if (flashlightController != null)
            {
                Debug.Log("UnderwaterAtmosphere: Found VRFlashlightController");
            }
        }
        
        // Create audio sources
        CreateAudioSources();
        
        // Set initial flicker time
        nextFlickerTime = Time.time + Random.Range(minFlickerInterval, maxFlickerInterval);
        
        // Find spaceship parts for proximity heartbeat
        if (enableProximityHeartbeat)
        {
            spaceshipParts = GameObject.FindGameObjectsWithTag(spaceshipPartTag);
            Debug.Log($"UnderwaterAtmosphere: Found {spaceshipParts.Length} spaceship parts for proximity heartbeat");
        }
        
        Debug.Log("UnderwaterAtmosphere: Initialized - Effects start at Y=" + effectStartY + ", max at Y=" + effectMaxY);
    }
    
    void CreateAudioSources()
    {
        // Create breathing audio source
        GameObject breathingObj = new GameObject("BreathingAudio");
        breathingObj.transform.SetParent(transform);
        breathingSource = breathingObj.AddComponent<AudioSource>();
        breathingSource.clip = breathingClip;
        breathingSource.loop = true;
        breathingSource.playOnAwake = false;
        breathingSource.spatialBlend = 0f; // 2D sound - always same volume
        breathingSource.volume = 0f;
        breathingSource.priority = 64; // Medium-high priority
        
        // Create heartbeat audio source
        GameObject heartbeatObj = new GameObject("HeartbeatAudio");
        heartbeatObj.transform.SetParent(transform);
        heartbeatSource = heartbeatObj.AddComponent<AudioSource>();
        heartbeatSource.clip = heartbeatClip;
        heartbeatSource.loop = true;
        heartbeatSource.playOnAwake = false;
        heartbeatSource.spatialBlend = 0f; // 2D sound
        heartbeatSource.volume = 0f;
        heartbeatSource.priority = 32; // High priority
        
        Debug.Log("UnderwaterAtmosphere: Audio sources created");
    }
    
    
    void Update()
    {
        if (playerTransform == null) return;
        
        // Keep trying to find flashlight if not found yet
        if (flashlightController == null)
        {
            flashlightController = FindObjectOfType<VRFlashlightController>();
            if (flashlightController != null)
            {
                Debug.Log("UnderwaterAtmosphere: Found VRFlashlightController (in Update)");
            }
        }
        
        float currentY = playerTransform.position.y;
        
        // Calculate depth factor (0 = at effectStartY or above, 1 = at effectMaxY or below)
        float depthFactor = CalculateDepthFactor(currentY);
        
        // Update breathing
        UpdateBreathing();
        
        // Update heartbeat based on depth
        UpdateHeartbeat(depthFactor);
        
        // Update flashlight flicker based on depth
        UpdateFlashlightFlicker(depthFactor);
    }
    
    float CalculateDepthFactor(float currentY)
    {
        if (currentY >= effectStartY)
        {
            return 0f; // Above or at start - no effects
        }
        else if (currentY <= effectMaxY)
        {
            return 1f; // At or below max depth - full effects
        }
        else
        {
            // Interpolate between start and max
            return Mathf.InverseLerp(effectStartY, effectMaxY, currentY);
        }
    }
    
    void UpdateBreathing()
    {
        if (breathingSource == null || breathingClip == null) return;
        
        if (isInWater)
        {
            // Start playing if not already
            if (!breathingSource.isPlaying)
            {
                breathingSource.Play();
                Debug.Log("UnderwaterAtmosphere: Started breathing audio");
            }
            
            // Fade in breathing
            breathingSource.volume = Mathf.MoveTowards(breathingSource.volume, breathingVolume, Time.deltaTime * 2f);
        }
        else
        {
            // Fade out breathing
            breathingSource.volume = Mathf.MoveTowards(breathingSource.volume, 0f, Time.deltaTime * 2f);
            
            // Stop if fully faded
            if (breathingSource.volume <= 0.01f && breathingSource.isPlaying)
            {
                breathingSource.Stop();
                Debug.Log("UnderwaterAtmosphere: Stopped breathing audio");
            }
        }
    }
    
    void UpdateHeartbeat(float depthFactor)
    {
        if (heartbeatSource == null || heartbeatClip == null) return;
        
        // Calculate proximity to nearest spaceship part
        proximityFactor = 0f;
        if (enableProximityHeartbeat && spaceshipParts != null && playerTransform != null)
        {
            float nearestDistance = float.MaxValue;
            foreach (GameObject part in spaceshipParts)
            {
                if (part != null && part.activeInHierarchy)
                {
                    float dist = Vector3.Distance(playerTransform.position, part.transform.position);
                    if (dist < nearestDistance)
                    {
                        nearestDistance = dist;
                    }
                }
            }
            
            // Calculate proximity factor (1 = very close, 0 = far away)
            if (nearestDistance < proximityDistance)
            {
                proximityFactor = 1f - (nearestDistance / proximityDistance);
            }
        }
        
        // Only play heartbeat when in water AND at depth
        if (isInWater && depthFactor > 0.1f)
        {
            // Target volume scales with depth AND proximity
            float volumeBoost = proximityFactor * 0.3f; // Up to 30% louder when close
            targetHeartbeatVolume = Mathf.Min(1f, maxHeartbeatVolume * depthFactor + volumeBoost);
            
            // Pitch increases when near spaceship parts (faster heartbeat = more excitement)
            float targetPitch = 1f + (proximityFactor * (maxProximityPitch - 1f));
            heartbeatSource.pitch = Mathf.Lerp(heartbeatSource.pitch, targetPitch, Time.deltaTime * 3f);
            
            // Start playing if not already
            if (!heartbeatSource.isPlaying)
            {
                heartbeatSource.Play();
                Debug.Log("UnderwaterAtmosphere: Started heartbeat at depth factor " + depthFactor.ToString("F2"));
            }
        }
        else
        {
            targetHeartbeatVolume = 0f;
            // Reset pitch when not playing
            heartbeatSource.pitch = Mathf.Lerp(heartbeatSource.pitch, 1f, Time.deltaTime * 3f);
        }
        
        // Smooth fade to target volume
        currentHeartbeatVolume = Mathf.MoveTowards(currentHeartbeatVolume, targetHeartbeatVolume, 
            (maxHeartbeatVolume / heartbeatFadeSpeed) * Time.deltaTime);
        heartbeatSource.volume = currentHeartbeatVolume;
        
        // Stop if fully faded out
        if (currentHeartbeatVolume <= 0.01f && heartbeatSource.isPlaying && targetHeartbeatVolume == 0f)
        {
            heartbeatSource.Stop();
            heartbeatSource.pitch = 1f; // Reset pitch
            Debug.Log("UnderwaterAtmosphere: Stopped heartbeat");
        }
    }

    
    private bool hasLoggedFlickerStatus = false;
    
    void UpdateFlashlightFlicker(float depthFactor)
    {
        // Debug: Log status once when at depth
        if (depthFactor > 0.1f && !hasLoggedFlickerStatus)
        {
            hasLoggedFlickerStatus = true;
            Debug.Log($"UnderwaterAtmosphere: Flicker check - depthFactor={depthFactor:F2}, flashlightController={(flashlightController != null)}, spotlight={(flashlightController?.spotlight != null)}, spotlightActive={(flashlightController?.spotlight?.gameObject.activeInHierarchy)}");
        }
        
        // Only flicker when at depth
        if (depthFactor <= 0.1f || flashlightController == null)
        {
            return;
        }
        
        // Get spotlight reference if we don't have it
        if (spotlightRef == null && flashlightController.spotlight != null)
        {
            spotlightRef = flashlightController.spotlight;
            originalSpotlightIntensity = spotlightRef.intensity;
            Debug.Log($"UnderwaterAtmosphere: Got spotlight reference, intensity={originalSpotlightIntensity}");
        }
        
        if (spotlightRef == null || !spotlightRef.gameObject.activeInHierarchy) return;
        
        // Handle active flicker
        if (isFlickering)
        {
            if (Time.time >= flickerEndTime)
            {
                // End this flicker
                spotlightRef.intensity = originalSpotlightIntensity;
                flickersRemaining--;
                
                if (flickersRemaining > 0)
                {
                    // Do another flicker in the sequence
                    StartSingleFlicker();
                }
                else
                {
                    // Flicker sequence complete
                    isFlickering = false;
                    ScheduleNextFlicker(depthFactor);
                }
            }
            return;
        }
        
        // Check if it's time for a new flicker
        if (Time.time >= nextFlickerTime)
        {
            StartFlickerSequence(depthFactor);
        }
        
        // Store current intensity for restoration (in case it changed due to depth system)
        if (!isFlickering)
        {
            originalSpotlightIntensity = spotlightRef.intensity;
        }
    }
    
    void StartFlickerSequence(float depthFactor)
    {
        isFlickering = true;
        
        // Determine number of flickers (more likely to be multiple at depth)
        float doubleChance = doubleFlickerChance * depthFactor;
        
        if (Random.value < doubleChance * 0.3f && depthFactor > 0.8f)
        {
            flickersRemaining = 3; // Triple flicker at extreme depth
        }
        else if (Random.value < doubleChance)
        {
            flickersRemaining = 2; // Double flicker
        }
        else
        {
            flickersRemaining = 1; // Single flicker
        }
        
        flickerCount++;
        StartSingleFlicker();
        
        // Debug.Log($"UnderwaterAtmosphere: Flicker sequence started ({flickersRemaining} flickers) at depth {depthFactor:F2}");
    }
    
    void StartSingleFlicker()
    {
        // Dim or turn off the light briefly
        float flickerIntensity = Random.Range(0f, originalSpotlightIntensity * 0.3f);
        spotlightRef.intensity = flickerIntensity;
        
        // Schedule end of this flicker
        float thisDuration = flickerDuration * Random.Range(0.5f, 1.5f);
        flickerEndTime = Time.time + thisDuration;
        
        // Small gap between multi-flickers
        if (flickersRemaining > 1)
        {
            flickerEndTime += Random.Range(0.05f, 0.15f);
        }
    }
    
    void ScheduleNextFlicker(float depthFactor)
    {
        // Interpolate interval based on depth
        float minInterval = Mathf.Lerp(minFlickerInterval, minFlickerIntervalDeep, depthFactor);
        float maxInterval = Mathf.Lerp(maxFlickerInterval, maxFlickerIntervalDeep, depthFactor);
        
        nextFlickerTime = Time.time + Random.Range(minInterval, maxInterval);
    }
    
    // Called by WaterTriggerZone or similar
    public void OnEnterWater()
    {
        isInWater = true;
        Debug.Log("UnderwaterAtmosphere: Player entered water");
    }
    
    public void OnExitWater()
    {
        isInWater = false;
        Debug.Log("UnderwaterAtmosphere: Player exited water");
    }
    
    // Auto-detect water entry via trigger (backup method)
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<Camera>() != null)
        {
            OnEnterWater();
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<Camera>() != null)
        {
            OnExitWater();
        }
    }
    
    // Public method to manually set water state (for testing or other triggers)
    public void SetInWater(bool inWater)
    {
        if (inWater)
            OnEnterWater();
        else
            OnExitWater();
    }
    
    // For debugging in inspector
    void OnValidate()
    {
        if (effectStartY < effectMaxY)
        {
            Debug.LogWarning("UnderwaterAtmosphere: effectStartY should be HIGHER than effectMaxY (remember Y goes down as you descend)");
        }
    }
}
