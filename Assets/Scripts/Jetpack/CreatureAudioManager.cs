using UnityEngine;

/// <summary>
/// Spatial audio manager for flying creature.
/// All sounds are 3D positioned on the creature for immersion.
/// </summary>
public class CreatureAudioManager : MonoBehaviour
{
    [Header("=== ONE-SHOT CLIPS ===")]
    public AudioClip mountSound;
    public AudioClip dismountSound;
    public AudioClip wingFlapSound;
    public AudioClip wingFlapFastSound;
    public AudioClip creatureCallFriendly;
    public AudioClip creatureCallExcited;
    public AudioClip creatureLandingSound;
    
    [Header("=== LOOPING CLIPS ===")]
    public AudioClip flightAmbientSound;
    public AudioClip creatureBreathingSound;
    public AudioClip flyingWingFlapLoop;
    
    [Header("=== VOLUME ===")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float mountVolume = 0.8f;
    [Range(0f, 1f)] public float wingFlapVolume = 0.7f;
    [Range(0f, 1f)] public float callVolume = 1f;
    [Range(0f, 1f)] public float landingVolume = 0.9f;
    [Range(0f, 1f)] public float ambientVolume = 0.4f;
    [Range(0f, 1f)] public float breathingVolume = 0.25f;
    [Range(0f, 1f)] public float flyingWingVolume = 0.5f;
    
    [Header("=== 3D SPATIAL SETTINGS ===")]
    [Tooltip("0 = 2D, 1 = full 3D")]
    [Range(0f, 1f)] public float spatialBlend = 1f;
    public float minDistance = 1f;
    public float maxDistance = 50f;
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;
    
    [Header("=== FADE SETTINGS ===")]
    public float ambientFadeInDuration = 1.5f;
    public float ambientFadeOutDuration = 2f;
    
    [Header("=== WING FLAP SYNC ===")]
    public bool syncWingFlapWithHaptics = true;
    
    // Audio Sources - all positioned on creature
    private AudioSource callSource;          // For creature calls (plays full clip)
    private AudioSource oneShotSource;       // For mount/dismount/landing
    private AudioSource wingFlapSource;      // For individual wing flaps
    private AudioSource flightAmbientSource; // Looping wind/flight
    private AudioSource breathingSource;     // Looping breathing
    private AudioSource flyingWingLoopSource;// Looping wing flaps while flying
    
    // State
    private bool isFlying = false;
    private bool isPlayerMounted = false;
    private float ambientFadeTimer = 0f;
    private float ambientTargetVolume = 0f;
    private float ambientStartVolume = 0f;
    private bool isFadingAmbient = false;
    
    public bool IsFlying => isFlying;
    public bool SyncWingFlapWithHaptics => syncWingFlapWithHaptics;
    
    private void Awake()
    {
        SetupAudioSources();
    }
    
    private void Start()
    {
        Debug.Log("<color=green>✓ CreatureAudioManager: Spatial audio ready</color>");
    }
    
    private void SetupAudioSources()
    {
        // Call source - dedicated for creature calls so they play fully
        callSource = CreateSpatialAudioSource("CreatureCall");
        callSource.priority = 32; // High priority so it doesn't get cut
        
        // One-shot for mount/dismount/landing
        oneShotSource = CreateSpatialAudioSource("CreatureOneShot");
        oneShotSource.priority = 64;
        
        // Wing flap source for individual flaps
        wingFlapSource = CreateSpatialAudioSource("WingFlap");
        wingFlapSource.priority = 100;
        
        // Flight ambient (looping)
        flightAmbientSource = CreateSpatialAudioSource("FlightAmbient");
        flightAmbientSource.loop = true;
        flightAmbientSource.priority = 128;
        if (flightAmbientSound != null) flightAmbientSource.clip = flightAmbientSound;
        
        // Breathing (looping)
        breathingSource = CreateSpatialAudioSource("Breathing");
        breathingSource.loop = true;
        breathingSource.priority = 140;
        if (creatureBreathingSound != null) breathingSource.clip = creatureBreathingSound;
        
        // Flying wing loop (looping while in flight)
        flyingWingLoopSource = CreateSpatialAudioSource("FlyingWingLoop");
        flyingWingLoopSource.loop = true;
        flyingWingLoopSource.priority = 110;
        if (flyingWingFlapLoop != null) flyingWingLoopSource.clip = flyingWingFlapLoop;
    }
    
    private AudioSource CreateSpatialAudioSource(string name)
    {
        GameObject audioObj = new GameObject($"Audio_{name}");
        audioObj.transform.SetParent(transform);
        audioObj.transform.localPosition = Vector3.zero;
        
        AudioSource source = audioObj.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = spatialBlend;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.rolloffMode = rolloffMode;
        source.dopplerLevel = 0.5f;
        source.spread = 60f;
        source.volume = 0f;
        
        return source;
    }
    
    private void Update()
    {
        UpdateAmbientFade();
    }
    
    #region Mount/Dismount
    
    public void PlayMount()
    {
        if (oneShotSource != null && mountSound != null)
        {
            oneShotSource.PlayOneShot(mountSound, mountVolume * masterVolume);
        }
        
        isPlayerMounted = true;
        
        // Play friendly call when mounting
        PlayCallFriendly();
        
        Debug.Log("<color=cyan>🦅 Mount sound + call</color>");
    }
    
    public void PlayDismount()
    {
        if (oneShotSource != null && dismountSound != null)
        {
            oneShotSource.PlayOneShot(dismountSound, mountVolume * masterVolume);
        }
        
        isPlayerMounted = false;
        
        Debug.Log("<color=cyan>🦅 Dismount sound</color>");
    }
    
    #endregion
    
    #region Creature Calls
    
    /// <summary>
    /// Play friendly call - uses dedicated source so it plays completely
    /// </summary>
    public void PlayCallFriendly()
    {
        if (callSource != null && creatureCallFriendly != null)
        {
            // Stop any current call first
            callSource.Stop();
            
            // Play the full clip
            callSource.clip = creatureCallFriendly;
            callSource.volume = callVolume * masterVolume;
            callSource.Play();
            
            Debug.Log($"<color=yellow>🦅 Creature friendly call - duration: {creatureCallFriendly.length}s</color>");
        }
    }
    
    /// <summary>
    /// Play excited call - uses dedicated source so it plays completely
    /// </summary>
    public void PlayCallExcited()
    {
        if (callSource != null && creatureCallExcited != null)
        {
            callSource.Stop();
            callSource.clip = creatureCallExcited;
            callSource.volume = callVolume * masterVolume;
            callSource.Play();
            
            Debug.Log($"<color=yellow>🦅 Creature excited call - duration: {creatureCallExcited.length}s</color>");
        }
    }
    
    #endregion
    
    #region Wing Flaps
    
    /// <summary>
    /// Play single wing flap (synced with haptics)
    /// </summary>
    public void PlayWingFlap()
    {
        if (wingFlapSource != null && wingFlapSound != null)
        {
            // Slight pitch variation for natural feel
            wingFlapSource.pitch = Random.Range(0.95f, 1.05f);
            wingFlapSource.PlayOneShot(wingFlapSound, wingFlapVolume * masterVolume);
        }
    }
    
    /// <summary>
    /// Play fast wing flap
    /// </summary>
    public void PlayWingFlapFast()
    {
        AudioClip clip = wingFlapFastSound != null ? wingFlapFastSound : wingFlapSound;
        if (wingFlapSource != null && clip != null)
        {
            wingFlapSource.pitch = Random.Range(1.0f, 1.1f);
            wingFlapSource.PlayOneShot(clip, wingFlapVolume * masterVolume);
        }
    }
    
    #endregion
    
    #region Landing
    
    public void PlayLanding()
    {
        if (oneShotSource != null && creatureLandingSound != null)
        {
            oneShotSource.PlayOneShot(creatureLandingSound, landingVolume * masterVolume);
            Debug.Log("<color=yellow>🦅 Creature landing</color>");
        }
    }
    
    #endregion
    
    #region Flight Ambient & Wing Loop
    
    /// <summary>
    /// Start all flight sounds (ambient wind + continuous wing flaps)
    /// </summary>
    public void StartFlightAmbient()
    {
        if (isFlying) return;
        isFlying = true;
        
        // Start flight ambient with fade
        if (flightAmbientSource != null && flightAmbientSound != null)
        {
            flightAmbientSource.volume = 0f;
            flightAmbientSource.Play();
            
            isFadingAmbient = true;
            ambientFadeTimer = 0f;
            ambientStartVolume = 0f;
            ambientTargetVolume = ambientVolume * masterVolume;
        }
        
        // Start breathing
        if (breathingSource != null && creatureBreathingSound != null)
        {
            breathingSource.volume = breathingVolume * masterVolume;
            breathingSource.Play();
        }
        
        // Start continuous wing flap loop while flying
        if (flyingWingLoopSource != null && flyingWingFlapLoop != null)
        {
            flyingWingLoopSource.volume = flyingWingVolume * masterVolume;
            flyingWingLoopSource.Play();
        }
        
        Debug.Log("<color=cyan>🌬️ Flight audio started (ambient + wing loop)</color>");
    }
    
    /// <summary>
    /// Stop flight sounds - wing flaps continue spatially if player dismounts mid-air
    /// </summary>
    public void StopFlightAmbient()
    {
        if (!isFlying) return;
        isFlying = false;
        
        // Fade out ambient
        if (flightAmbientSource != null && flightAmbientSource.isPlaying)
        {
            isFadingAmbient = true;
            ambientFadeTimer = 0f;
            ambientStartVolume = flightAmbientSource.volume;
            ambientTargetVolume = 0f;
        }
        
        // Stop breathing
        if (breathingSource != null)
        {
            breathingSource.Stop();
        }
        
        // If player dismounted, keep wing loop going (spatial 3D) so they hear it from outside
        // Only stop if creature lands
        // The wing loop continues - creature keeps flying
        
        Debug.Log("<color=cyan>🌬️ Flight ambient stopping (wing loop may continue)</color>");
    }
    
    /// <summary>
    /// Completely stop all flight sounds including wing loop (call when creature lands)
    /// </summary>
    public void StopAllFlightSounds()
    {
        isFlying = false;
        
        if (flightAmbientSource != null) flightAmbientSource.Stop();
        if (breathingSource != null) breathingSource.Stop();
        if (flyingWingLoopSource != null) flyingWingLoopSource.Stop();
        
        isFadingAmbient = false;
        
        Debug.Log("<color=cyan>🦅 All flight sounds stopped</color>");
    }
    
    private void UpdateAmbientFade()
    {
        if (!isFadingAmbient || flightAmbientSource == null) return;
        
        float fadeDuration = ambientTargetVolume > ambientStartVolume ? ambientFadeInDuration : ambientFadeOutDuration;
        ambientFadeTimer += Time.deltaTime;
        float t = Mathf.Clamp01(ambientFadeTimer / fadeDuration);
        
        flightAmbientSource.volume = Mathf.Lerp(ambientStartVolume, ambientTargetVolume, t);
        
        if (t >= 1f)
        {
            isFadingAmbient = false;
            if (ambientTargetVolume <= 0f)
            {
                flightAmbientSource.Stop();
            }
        }
    }
    
    #endregion
    
    #region Utility
    
    public void StopAllAudio()
    {
        isFlying = false;
        isPlayerMounted = false;
        isFadingAmbient = false;
        
        if (callSource != null) callSource.Stop();
        if (oneShotSource != null) oneShotSource.Stop();
        if (wingFlapSource != null) wingFlapSource.Stop();
        if (flightAmbientSource != null) flightAmbientSource.Stop();
        if (breathingSource != null) breathingSource.Stop();
        if (flyingWingLoopSource != null) flyingWingLoopSource.Stop();
    }
    
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
    }
    
    /// <summary>
    /// Update spatial settings at runtime
    /// </summary>
    public void SetSpatialBlend(float blend)
    {
        spatialBlend = Mathf.Clamp01(blend);
        
        if (callSource != null) callSource.spatialBlend = spatialBlend;
        if (oneShotSource != null) oneShotSource.spatialBlend = spatialBlend;
        if (wingFlapSource != null) wingFlapSource.spatialBlend = spatialBlend;
        if (flightAmbientSource != null) flightAmbientSource.spatialBlend = spatialBlend;
        if (breathingSource != null) breathingSource.spatialBlend = spatialBlend;
        if (flyingWingLoopSource != null) flyingWingLoopSource.spatialBlend = spatialBlend;
    }
    
    #endregion
}
