using UnityEngine;

/// <summary>
/// Adds ambient bubble sounds to particle systems or bubble objects.
/// Attach to any bubble emitter for underwater ambiance.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BubbleAudio : MonoBehaviour
{
    [Header("Audio Settings")]
    [Tooltip("Bubble sound clips - will randomly select from these")]
    public AudioClip[] bubbleClips;
    
    [Tooltip("Volume range for variety")]
    [Range(0f, 1f)] public float minVolume = 0.3f;
    [Range(0f, 1f)] public float maxVolume = 0.6f;
    
    [Tooltip("Pitch range for variety")]
    public float minPitch = 0.8f;
    public float maxPitch = 1.2f;
    
    [Header("Timing")]
    [Tooltip("Minimum time between bubble sounds")]
    public float minInterval = 0.5f;
    [Tooltip("Maximum time between bubble sounds")]
    public float maxInterval = 2f;
    
    [Header("Continuous Mode")]
    [Tooltip("Use looping ambient sound instead of random intervals")]
    public bool useLoopingAmbient = false;
    
    [Header("3D Audio")]
    [Tooltip("How far the sound can be heard")]
    public float maxDistance = 20f;
    
    private AudioSource audioSource;
    private float nextPlayTime;
    
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        SetupAudioSource();
    }
    
    void SetupAudioSource()
    {
        audioSource.spatialBlend = 1f; // Full 3D
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.maxDistance = maxDistance;
        audioSource.minDistance = 1f;
        audioSource.playOnAwake = false;
        
        if (useLoopingAmbient && bubbleClips != null && bubbleClips.Length > 0)
        {
            audioSource.clip = bubbleClips[0];
            audioSource.loop = true;
            audioSource.volume = (minVolume + maxVolume) / 2f;
            audioSource.Play();
        }
    }
    
    void Update()
    {
        if (useLoopingAmbient) return;
        
        if (Time.time >= nextPlayTime)
        {
            PlayRandomBubble();
            nextPlayTime = Time.time + Random.Range(minInterval, maxInterval);
        }
    }
    
    void PlayRandomBubble()
    {
        if (bubbleClips == null || bubbleClips.Length == 0) return;
        
        AudioClip clip = bubbleClips[Random.Range(0, bubbleClips.Length)];
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.volume = Random.Range(minVolume, maxVolume);
        audioSource.PlayOneShot(clip);
    }
    
    /// <summary>
    /// Call this from particle system or animation events
    /// </summary>
    public void PlayBubbleSound()
    {
        PlayRandomBubble();
    }
}
