using UnityEngine;

/// <summary>
/// Centralized audio manager for all jetpack-related sounds.
/// Handles ignition, thrust, wind, landing, and fuel warning audio.
/// </summary>
public class JetpackAudioManager : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip ignitionSound;
    [SerializeField] private AudioClip thrustLoopSound;
    [SerializeField] private AudioClip boostSound;
    [SerializeField] private AudioClip lowFuelWarningSound;
    [SerializeField] private AudioClip landingSoftSound;
    [SerializeField] private AudioClip landingHardSound;
    [SerializeField] private AudioClip platformTouchdownSound;
    
    [Header("Wind Audio")]
    [SerializeField] private AudioClip windHighAltitudeSound;
    [SerializeField] private AudioClip windRushingSpeedSound;
    
    [Header("Volume Settings")]
    [SerializeField] private float masterVolume = 1f;
    [SerializeField] private float ignitionVolume = 0.8f;
    [SerializeField] private float thrustVolume = 0.7f;
    [SerializeField] private float boostVolume = 0.9f;
    [Header("Warning Settings")]
    [SerializeField] private float warningVolume = 5f;
    [SerializeField] private float warningRepeatInterval = 2f;
    [SerializeField] private float landingVolume = 0.8f;
    [SerializeField] private float windVolume = 0.5f;
    
    [Header("Wind Settings")]
    [SerializeField] private float windAltitudeThreshold = 50f;
    [SerializeField] private float windSpeedThreshold = 5f;
    [SerializeField] private float maxWindAltitude = 150f;
    [SerializeField] private float maxWindSpeed = 15f;
    
    [Header("Landing Settings")]
    [SerializeField] private float hardLandingVelocity = 8f;
    [SerializeField] private float softLandingVelocity = 2f;
    
    [Header("Fade Settings")]
    [SerializeField] private float thrustFadeInDuration = 0.2f;
    [SerializeField] private float thrustFadeOutDuration = 0.5f;
    [SerializeField] private float windFadeDuration = 1f;
    
    // Audio Sources
    private AudioSource ignitionSource;
    private AudioSource thrustSource;
    private AudioSource boostSource;
    private AudioSource warningSource;
    private AudioSource landingSource;
    private AudioSource windAltitudeSource;
    private AudioSource windSpeedSource;
    
    // State tracking
    private bool isThrusting = false;
    private bool isFadingThrust = false;
    private float thrustFadeTimer = 0f;
    private float thrustTargetVolume = 0f;
    private float thrustStartVolume = 0f;
    
    private bool hasPlayedLowFuelWarning = false;
    private bool isLowFuelWarningActive = false;
    private float warningRepeatTimer = 0f;
    private float lastLandingTime = 0f;
    private const float LANDING_COOLDOWN = 0.5f;
    
    // References
    private float currentAltitude = 0f;
    private float currentSpeed = 0f;
    private float groundLevel = 0f;
    
    private void Awake()
    {
        SetupAudioSources();
    }
    
    private void Start()
    {
        // Store ground level (approximate)
        groundLevel = transform.position.y;
        Debug.Log("<color=green>✓ JetpackAudioManager initialized</color>");
    }
    
    private void SetupAudioSources()
    {
        // Create audio sources for each sound type
        ignitionSource = CreateAudioSource("Ignition", false);
        thrustSource = CreateAudioSource("Thrust", true);
        boostSource = CreateAudioSource("Boost", false);
        warningSource = CreateAudioSource("Warning", false);
        landingSource = CreateAudioSource("Landing", false);
        windAltitudeSource = CreateAudioSource("WindAltitude", true);
        windSpeedSource = CreateAudioSource("WindSpeed", true);
        
        // Assign clips
        if (ignitionSound != null) ignitionSource.clip = ignitionSound;
        if (thrustLoopSound != null) thrustSource.clip = thrustLoopSound;
        if (boostSound != null) boostSource.clip = boostSound;
        if (lowFuelWarningSound != null) warningSource.clip = lowFuelWarningSound;
        if (windHighAltitudeSound != null) windAltitudeSource.clip = windHighAltitudeSound;
        if (windRushingSpeedSound != null) windSpeedSource.clip = windRushingSpeedSound;
    }
    
    private AudioSource CreateAudioSource(string name, bool loop)
    {
        GameObject audioObj = new GameObject($"Audio_{name}");
        audioObj.transform.SetParent(transform);
        audioObj.transform.localPosition = Vector3.zero;
        
        AudioSource source = audioObj.AddComponent<AudioSource>();
        source.loop = loop;
        source.playOnAwake = false;
        source.spatialBlend = 0f; // 2D sound (follows player)
        source.volume = 0f;
        
        return source;
    }
    
    private void Update()
    {
        UpdateThrustFade();
        UpdateWindAudio();
        UpdateWarningSound();
    }
    
    #region Thrust Audio
    
    /// <summary>
    /// Call when jetpack is activated
    /// </summary>
    public void PlayIgnition()
    {
        if (ignitionSource != null && ignitionSound != null)
        {
            ignitionSource.volume = ignitionVolume * masterVolume;
            ignitionSource.PlayOneShot(ignitionSound);
            Debug.Log("<color=cyan>🔊 Jetpack Ignition</color>");
        }
    }
    
    /// <summary>
    /// Start the thrust loop sound with fade in
    /// </summary>
    public void StartThrust()
    {
        if (isThrusting) return;
        
        isThrusting = true;
        
        // Play ignition first
        PlayIgnition();
        
        // Start thrust loop with fade in
        if (thrustSource != null && thrustLoopSound != null)
        {
            thrustSource.volume = 0f;
            thrustSource.Play();
            
            // Start fade in
            isFadingThrust = true;
            thrustFadeTimer = 0f;
            thrustStartVolume = 0f;
            thrustTargetVolume = thrustVolume * masterVolume;
            
            Debug.Log("<color=cyan>🔊 Thrust Loop Starting</color>");
        }
    }
    
    /// <summary>
    /// Stop the thrust loop sound with fade out
    /// </summary>
    public void StopThrust()
    {
        if (!isThrusting) return;
        
        isThrusting = false;
        
        // Start fade out
        if (thrustSource != null && thrustSource.isPlaying)
        {
            isFadingThrust = true;
            thrustFadeTimer = 0f;
            thrustStartVolume = thrustSource.volume;
            thrustTargetVolume = 0f;
            
            Debug.Log("<color=cyan>🔇 Thrust Loop Stopping</color>");
        }
    }
    
    private void UpdateThrustFade()
    {
        if (!isFadingThrust || thrustSource == null) return;
        
        float fadeDuration = thrustTargetVolume > thrustStartVolume ? thrustFadeInDuration : thrustFadeOutDuration;
        thrustFadeTimer += Time.deltaTime;
        float t = Mathf.Clamp01(thrustFadeTimer / fadeDuration);
        
        thrustSource.volume = Mathf.Lerp(thrustStartVolume, thrustTargetVolume, t);
        
        if (t >= 1f)
        {
            isFadingThrust = false;
            
            // Stop audio source if faded out completely
            if (thrustTargetVolume <= 0f)
            {
                thrustSource.Stop();
            }
        }
    }
    
    #endregion
    
    #region Boost Audio
    
    /// <summary>
    /// Play boost sound effect
    /// </summary>
    public void PlayBoost()
    {
        if (boostSource != null && boostSound != null)
        {
            boostSource.volume = boostVolume * masterVolume;
            boostSource.PlayOneShot(boostSound);
            Debug.Log("<color=magenta>🚀 Boost!</color>");
        }
    }
    
    #endregion
    
    #region Warning Audio
    
    /// <summary>
    /// Play low fuel warning sound (starts repeating alarm)
    /// </summary>
    public void PlayLowFuelWarning()
    {
        if (isLowFuelWarningActive) return; // Already active
        
        if (warningSource == null)
        {
            Debug.LogError("Warning AudioSource is null!");
            return;
        }
        
        if (lowFuelWarningSound == null)
        {
            Debug.LogError("Low Fuel Warning Sound clip is not assigned! Please assign it in the Inspector.");
            return;
        }
        
        isLowFuelWarningActive = true;
        warningRepeatTimer = warningRepeatInterval; // Play immediately on next update
        
        // Play first warning immediately
        warningSource.volume = warningVolume * masterVolume;
        warningSource.PlayOneShot(lowFuelWarningSound);
        
        Debug.Log($"<color=orange>⚠ LOW FUEL WARNING STARTED! Volume: {warningSource.volume} - Will repeat every {warningRepeatInterval}s</color>");
    }
    
    /// <summary>
    /// Update warning sound - handles repeating alarm
    /// </summary>
    private void UpdateWarningSound()
    {
        if (!isLowFuelWarningActive) return;
        if (warningSource == null || lowFuelWarningSound == null) return;
        
        warningRepeatTimer += Time.deltaTime;
        
        if (warningRepeatTimer >= warningRepeatInterval)
        {
            warningRepeatTimer = 0f;
            warningSource.volume = warningVolume * masterVolume;
            warningSource.PlayOneShot(lowFuelWarningSound);
            Debug.Log("<color=orange>⚠ LOW FUEL WARNING (repeat)</color>");
        }
    }
    
    /// <summary>
    /// Stop warning immediately (when fuel reaches 0 or refueled)
    /// </summary>
    public void StopLowFuelWarning()
    {
        isLowFuelWarningActive = false;
        warningRepeatTimer = 0f;
    }
    
    /// <summary>
    /// Reset warning state (call when fuel is refilled above threshold)
    /// </summary>
    public void ResetLowFuelWarning()
    {
        if (isLowFuelWarningActive)
        {
            Debug.Log("<color=green>✓ Low fuel warning stopped</color>");
        }
        
        hasPlayedLowFuelWarning = false;
        isLowFuelWarningActive = false;
        warningRepeatTimer = 0f;
    }
    
    #endregion
    
    #region Landing Audio
    
    /// <summary>
    /// Play appropriate landing sound based on velocity
    /// </summary>
    public void PlayLanding(float landingVelocity, bool isPlatform = false)
    {
        // Cooldown to prevent multiple landing sounds
        if (Time.time - lastLandingTime < LANDING_COOLDOWN) return;
        lastLandingTime = Time.time;
        
        float absVelocity = Mathf.Abs(landingVelocity);
        
        // Don't play sound for very soft landings
        if (absVelocity < softLandingVelocity) return;
        
        AudioClip clipToPlay = null;
        
        if (isPlatform && platformTouchdownSound != null)
        {
            clipToPlay = platformTouchdownSound;
        }
        else if (absVelocity >= hardLandingVelocity && landingHardSound != null)
        {
            clipToPlay = landingHardSound;
        }
        else if (landingSoftSound != null)
        {
            clipToPlay = landingSoftSound;
        }
        
        if (clipToPlay != null && landingSource != null)
        {
            landingSource.volume = landingVolume * masterVolume;
            landingSource.PlayOneShot(clipToPlay);
            
            string landingType = isPlatform ? "Platform" : (absVelocity >= hardLandingVelocity ? "Hard" : "Soft");
            Debug.Log($"<color=yellow>🦶 {landingType} Landing (velocity: {absVelocity:F1})</color>");
        }
    }
    
    #endregion
    
    #region Wind Audio
    
    /// <summary>
    /// Update wind audio based on altitude and speed
    /// </summary>
    public void UpdatePlayerState(float altitude, float speed, float groundY = 0f)
    {
        currentAltitude = altitude - groundY;
        currentSpeed = speed;
    }
    
    private void UpdateWindAudio()
    {
        // Altitude-based wind
        if (windAltitudeSource != null && windHighAltitudeSound != null)
        {
            if (currentAltitude > windAltitudeThreshold)
            {
                float altitudeT = Mathf.InverseLerp(windAltitudeThreshold, maxWindAltitude, currentAltitude);
                float targetVolume = altitudeT * windVolume * masterVolume;
                
                if (!windAltitudeSource.isPlaying)
                {
                    windAltitudeSource.Play();
                }
                
                windAltitudeSource.volume = Mathf.Lerp(windAltitudeSource.volume, targetVolume, Time.deltaTime / windFadeDuration);
            }
            else
            {
                windAltitudeSource.volume = Mathf.Lerp(windAltitudeSource.volume, 0f, Time.deltaTime / windFadeDuration);
                
                if (windAltitudeSource.volume < 0.01f && windAltitudeSource.isPlaying)
                {
                    windAltitudeSource.Stop();
                }
            }
        }
        
        // Speed-based wind
        if (windSpeedSource != null && windRushingSpeedSound != null)
        {
            if (currentSpeed > windSpeedThreshold)
            {
                float speedT = Mathf.InverseLerp(windSpeedThreshold, maxWindSpeed, currentSpeed);
                float targetVolume = speedT * windVolume * masterVolume;
                
                if (!windSpeedSource.isPlaying)
                {
                    windSpeedSource.Play();
                }
                
                windSpeedSource.volume = Mathf.Lerp(windSpeedSource.volume, targetVolume, Time.deltaTime / windFadeDuration);
            }
            else
            {
                windSpeedSource.volume = Mathf.Lerp(windSpeedSource.volume, 0f, Time.deltaTime / windFadeDuration);
                
                if (windSpeedSource.volume < 0.01f && windSpeedSource.isPlaying)
                {
                    windSpeedSource.Stop();
                }
            }
        }
    }
    
    #endregion
    
    #region Utility
    
    /// <summary>
    /// Stop all audio immediately
    /// </summary>
    public void StopAllAudio()
    {
        isThrusting = false;
        isFadingThrust = false;
        
        if (ignitionSource != null) ignitionSource.Stop();
        if (thrustSource != null) thrustSource.Stop();
        if (boostSource != null) boostSource.Stop();
        if (warningSource != null) warningSource.Stop();
        if (landingSource != null) landingSource.Stop();
        if (windAltitudeSource != null) windAltitudeSource.Stop();
        if (windSpeedSource != null) windSpeedSource.Stop();
    }
    
    /// <summary>
    /// Set master volume for all sounds
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
    }
    
    public bool IsThrusting => isThrusting;
    
    #endregion
}
