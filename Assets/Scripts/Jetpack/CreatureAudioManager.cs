using UnityEngine;

/// <summary>
/// Centralized audio manager for flying creature sounds.
/// Handles mount/dismount, wing flaps, ambient flight, and creature calls.
/// </summary>
public class CreatureAudioManager : MonoBehaviour
{
    [Header("One-Shot Audio Clips")]
    [SerializeField] private AudioClip mountSound;
    [SerializeField] private AudioClip dismountSound;
    [SerializeField] private AudioClip wingFlapSound;
    [SerializeField] private AudioClip wingFlapFastSound;
    [SerializeField] private AudioClip creatureCallFriendly;
    [SerializeField] private AudioClip creatureCallExcited;
    [SerializeField] private AudioClip creatureLandingSound;
    
    [Header("Looping Audio Clips")]
    [SerializeField] private AudioClip flightAmbientSound;
    [SerializeField] private AudioClip creatureBreathingSound;
    
    [Header("Volume Settings")]
    [SerializeField] private float masterVolume = 1f;
    [SerializeField] private float mountVolume = 1f;
    [SerializeField] private float wingFlapVolume = 0.8f;
    [SerializeField] private float callVolume = 0.9f;
    [SerializeField] private float landingVolume = 1f;
    [SerializeField] private float ambientVolume = 0.5f;
    [SerializeField] private float breathingVolume = 0.3f;
    
    [Header("Fade Settings")]
    [SerializeField] private float ambientFadeInDuration = 1f;
    [SerializeField] private float ambientFadeOutDuration = 1.5f;
    
    [Header("Wing Flap Sync")]
    [Tooltip("If true, wing flap sound plays automatically with haptic interval")]
    [SerializeField] private bool syncWingFlapWithHaptics = true;
    
    // Audio Sources
    private AudioSource oneShotSource;
    private AudioSource flightAmbientSource;
    private AudioSource breathingSource;
    
    // State tracking
    private bool isFlying = false;
    private bool isFadingAmbient = false;
    private float ambientFadeTimer = 0f;
    private float ambientTargetVolume = 0f;
    private float ambientStartVolume = 0f;
    
    private void Awake()
    {
        SetupAudioSources();
    }
    
    private void Start()
    {
        Debug.Log("<color=green>✓ CreatureAudioManager initialized</color>");
    }
    
    private void SetupAudioSources()
    {
        // One-shot source for mount, dismount, wing flaps, calls
        oneShotSource = CreateAudioSource("CreatureOneShot", false);
        
        // Looping sources
        flightAmbientSource = CreateAudioSource("FlightAmbient", true);
        breathingSource = CreateAudioSource("Breathing", true);
        
        // Assign looping clips
        if (flightAmbientSound != null) flightAmbientSource.clip = flightAmbientSound;
        if (creatureBreathingSound != null) breathingSource.clip = creatureBreathingSound;
    }
    
    private AudioSource CreateAudioSource(string name, bool loop)
    {
        GameObject audioObj = new GameObject($"Audio_{name}");
        audioObj.transform.SetParent(transform);
        audioObj.transform.localPosition = Vector3.zero;
        
        AudioSource source = audioObj.AddComponent<AudioSource>();
        source.loop = loop;
        source.playOnAwake = false;
        source.spatialBlend = 0.5f; // Partial 3D for immersion
        source.volume = 0f;
        
        return source;
    }
    
    private void Update()
    {
        UpdateAmbientFade();
    }
    
    #region Mount/Dismount
    
    /// <summary>
    /// Play mount sound when player gets on creature
    /// </summary>
    public void PlayMount()
    {
        if (oneShotSource != null && mountSound != null)
        {
            oneShotSource.volume = mountVolume * masterVolume;
            oneShotSource.PlayOneShot(mountSound);
            Debug.Log("<color=cyan>🦅 Mount sound played</color>");
        }
        
        // Play friendly call when mounting
        PlayCallFriendly();
    }
    
    /// <summary>
    /// Play dismount sound when player gets off creature
    /// </summary>
    public void PlayDismount()
    {
        if (oneShotSource != null && dismountSound != null)
        {
            oneShotSource.volume = mountVolume * masterVolume;
            oneShotSource.PlayOneShot(dismountSound);
            Debug.Log("<color=cyan>🦅 Dismount sound played</color>");
        }
    }
    
    #endregion
    
    #region Wing Flaps
    
    /// <summary>
    /// Play wing flap sound (call this synced with haptic pulses)
    /// </summary>
    public void PlayWingFlap()
    {
        if (oneShotSource != null && wingFlapSound != null)
        {
            oneShotSource.volume = wingFlapVolume * masterVolume;
            oneShotSource.PlayOneShot(wingFlapSound);
        }
    }
    
    /// <summary>
    /// Play fast wing flap sound (for rapid flight)
    /// </summary>
    public void PlayWingFlapFast()
    {
        AudioClip clip = wingFlapFastSound != null ? wingFlapFastSound : wingFlapSound;
        
        if (oneShotSource != null && clip != null)
        {
            oneShotSource.volume = wingFlapVolume * masterVolume;
            oneShotSource.PlayOneShot(clip);
        }
    }
    
    #endregion
    
    #region Creature Calls
    
    /// <summary>
    /// Play friendly creature call (mounting, idle)
    /// </summary>
    public void PlayCallFriendly()
    {
        if (oneShotSource != null && creatureCallFriendly != null)
        {
            oneShotSource.volume = callVolume * masterVolume;
            oneShotSource.PlayOneShot(creatureCallFriendly);
            Debug.Log("<color=yellow>🦅 Creature friendly call</color>");
        }
    }
    
    /// <summary>
    /// Play excited creature call (reaching destination, paradise zone)
    /// </summary>
    public void PlayCallExcited()
    {
        if (oneShotSource != null && creatureCallExcited != null)
        {
            oneShotSource.volume = callVolume * masterVolume;
            oneShotSource.PlayOneShot(creatureCallExcited);
            Debug.Log("<color=yellow>🦅 Creature excited call!</color>");
        }
    }
    
    #endregion
    
    #region Landing
    
    /// <summary>
    /// Play creature landing sound
    /// </summary>
    public void PlayLanding()
    {
        if (oneShotSource != null && creatureLandingSound != null)
        {
            oneShotSource.volume = landingVolume * masterVolume;
            oneShotSource.PlayOneShot(creatureLandingSound);
            Debug.Log("<color=yellow>🦅 Creature landing</color>");
        }
    }
    
    #endregion
    
    #region Flight Ambient
    
    /// <summary>
    /// Start flight ambient sounds (call when player mounts and starts flying)
    /// </summary>
    public void StartFlightAmbient()
    {
        if (isFlying) return;
        
        isFlying = true;
        
        // Start flight ambient with fade in
        if (flightAmbientSource != null && flightAmbientSound != null)
        {
            flightAmbientSource.volume = 0f;
            flightAmbientSource.Play();
            
            isFadingAmbient = true;
            ambientFadeTimer = 0f;
            ambientStartVolume = 0f;
            ambientTargetVolume = ambientVolume * masterVolume;
            
            Debug.Log("<color=cyan>🌬️ Flight ambient starting</color>");
        }
        
        // Start breathing
        if (breathingSource != null && creatureBreathingSound != null)
        {
            breathingSource.volume = breathingVolume * masterVolume;
            breathingSource.Play();
        }
    }
    
    /// <summary>
    /// Stop flight ambient sounds (call when player dismounts or lands)
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
            
            Debug.Log("<color=cyan>🌬️ Flight ambient stopping</color>");
        }
        
        // Stop breathing
        if (breathingSource != null)
        {
            breathingSource.Stop();
        }
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
    
    /// <summary>
    /// Stop all creature audio
    /// </summary>
    public void StopAllAudio()
    {
        isFlying = false;
        isFadingAmbient = false;
        
        if (oneShotSource != null) oneShotSource.Stop();
        if (flightAmbientSource != null) flightAmbientSource.Stop();
        if (breathingSource != null) breathingSource.Stop();
    }
    
    /// <summary>
    /// Set master volume
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
    }
    
    public bool IsFlying => isFlying;
    public bool SyncWingFlapWithHaptics => syncWingFlapWithHaptics;
    
    #endregion
}
