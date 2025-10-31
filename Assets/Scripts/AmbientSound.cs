using UnityEngine;

/// <summary>
/// Simple 3D ambient sound system for VR
/// Perfect for waterfalls, wind, nature sounds, etc.
/// Attach to any GameObject and assign an AudioClip!
/// </summary>
public class AmbientSound : MonoBehaviour
{
    [Header("Audio Settings")]
    [Tooltip("The ambient sound to play (waterfall, wind, etc.)")]
    [SerializeField] private AudioClip ambientClip;
    
    [Tooltip("Volume of the ambient sound (0-1)")]
    [SerializeField] private float volume = 1.0f;
    
    [Header("3D Spatial Settings")]
    [Tooltip("Minimum distance - full volume inside this range")]
    [SerializeField] private float minDistance = 5f;
    
    [Tooltip("Maximum distance - no sound beyond this range")]
    [SerializeField] private float maxDistance = 30f;
    
    [Tooltip("How quickly sound fades with distance (default: Logarithmic)")]
    [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
    
    [Header("Playback Settings")]
    [Tooltip("Start playing automatically on scene start")]
    [SerializeField] private bool playOnAwake = true;
    
    [Tooltip("Loop the sound continuously")]
    [SerializeField] private bool loop = true;
    
    [Tooltip("Random pitch variation for natural feel (0 = none, 0.1 = slight variation)")]
    [SerializeField] private float pitchVariation = 0.05f;
    
    private AudioSource audioSource;
    
    void Start()
    {
        SetupAudioSource();
        
        if (playOnAwake && ambientClip != null)
        {
            PlaySound();
        }
    }
    
    void SetupAudioSource()
    {
        // Get or create audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            Debug.Log($"<color=cyan>✓ Created AudioSource on {gameObject.name}</color>");
        }
        
        // Configure for 3D spatial audio
        audioSource.clip = ambientClip;
        audioSource.volume = volume;
        audioSource.loop = loop;
        audioSource.playOnAwake = false;
        
        // CRITICAL for 3D audio in VR!
        audioSource.spatialBlend = 1.0f; // 1.0 = full 3D spatial audio
        
        // Distance settings
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.rolloffMode = rolloffMode;
        
        // Add slight pitch variation for natural feel
        if (pitchVariation > 0)
        {
            audioSource.pitch = 1.0f + Random.Range(-pitchVariation, pitchVariation);
        }
        
        // Doppler effect (subtle for ambient sounds)
        audioSource.dopplerLevel = 0.1f;
        
        Debug.Log($"<color=green>✓ Ambient Sound Setup Complete: {gameObject.name}</color>");
        Debug.Log($"   - Min Distance: {minDistance}m (full volume)");
        Debug.Log($"   - Max Distance: {maxDistance}m (silent)");
    }
    
    /// <summary>
    /// Start playing the ambient sound
    /// </summary>
    public void PlaySound()
    {
        if (audioSource != null && ambientClip != null && !audioSource.isPlaying)
        {
            audioSource.Play();
            Debug.Log($"<color=cyan>🔊 Playing ambient sound: {ambientClip.name}</color>");
        }
    }
    
    /// <summary>
    /// Stop playing the ambient sound
    /// </summary>
    public void StopSound()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log($"<color=yellow>🔇 Stopped ambient sound: {ambientClip.name}</color>");
        }
    }
    
    /// <summary>
    /// Adjust volume at runtime
    /// </summary>
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }
    
    // Draw gizmos to visualize sound range in editor
    void OnDrawGizmosSelected()
    {
        // Draw min distance (full volume sphere)
        Gizmos.color = new Color(0, 1, 0, 0.3f); // Green
        Gizmos.DrawWireSphere(transform.position, minDistance);
        
        // Draw max distance (silent sphere)
        Gizmos.color = new Color(1, 0, 0, 0.3f); // Red
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }
}
