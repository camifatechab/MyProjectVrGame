using UnityEngine;

/// <summary>
/// Audio controller for the alien squid creature.
/// Creates atmospheric tension with proximity-based heartbeat and occasional roars.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SquidAudio : MonoBehaviour
{
    [Header("Audio Clips")]
    [Tooltip("Roar/growl sounds")]
    public AudioClip[] roarClips;
    
    [Tooltip("Heartbeat sound for tension")]
    public AudioClip heartbeatClip;
    
    [Tooltip("Ambient idle sounds (optional)")]
    public AudioClip[] ambientClips;
    
    [Header("Roar Settings")]
    [Tooltip("Minimum time between roars")]
    public float minRoarInterval = 15f;
    [Tooltip("Maximum time between roars")]
    public float maxRoarInterval = 45f;
    [Range(0f, 1f)]
    public float roarVolume = 0.7f;
    
    [Header("Heartbeat Settings")]
    [Tooltip("Distance at which heartbeat starts")]
    public float heartbeatStartDistance = 30f;
    [Tooltip("Distance at which heartbeat is loudest")]
    public float heartbeatMaxDistance = 5f;
    [Range(0f, 1f)]
    public float maxHeartbeatVolume = 0.8f;
    
    [Header("3D Audio")]
    public float maxAudioDistance = 50f;
    
    private AudioSource mainAudioSource;
    private AudioSource heartbeatSource;
    private Transform playerTransform;
    private float nextRoarTime;
    
    void Awake()
    {
        mainAudioSource = GetComponent<AudioSource>();
        SetupMainAudioSource();
        CreateHeartbeatSource();
    }
    
    void Start()
    {
        // Find the player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        
        nextRoarTime = Time.time + Random.Range(minRoarInterval, maxRoarInterval);
    }
    
    void SetupMainAudioSource()
    {
        mainAudioSource.spatialBlend = 1f;
        mainAudioSource.rolloffMode = AudioRolloffMode.Linear;
        mainAudioSource.maxDistance = maxAudioDistance;
        mainAudioSource.minDistance = 2f;
        mainAudioSource.playOnAwake = false;
    }
    
    void CreateHeartbeatSource()
    {
        // Create separate audio source for heartbeat loop
        GameObject heartbeatObj = new GameObject("SquidHeartbeatAudio");
        heartbeatObj.transform.SetParent(transform);
        heartbeatObj.transform.localPosition = Vector3.zero;
        
        heartbeatSource = heartbeatObj.AddComponent<AudioSource>();
        heartbeatSource.spatialBlend = 1f;
        heartbeatSource.rolloffMode = AudioRolloffMode.Linear;
        heartbeatSource.maxDistance = heartbeatStartDistance;
        heartbeatSource.minDistance = 1f;
        heartbeatSource.loop = true;
        heartbeatSource.playOnAwake = false;
        heartbeatSource.volume = 0f;
        
        if (heartbeatClip != null)
        {
            heartbeatSource.clip = heartbeatClip;
            heartbeatSource.Play();
        }
    }
    
    void Update()
    {
        UpdateHeartbeat();
        UpdateRoars();
    }
    
    void UpdateHeartbeat()
    {
        if (playerTransform == null || heartbeatSource == null || heartbeatClip == null) return;
        
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        
        if (distance < heartbeatStartDistance)
        {
            // Calculate volume based on proximity (closer = louder)
            float t = 1f - Mathf.InverseLerp(heartbeatMaxDistance, heartbeatStartDistance, distance);
            heartbeatSource.volume = Mathf.Lerp(0f, maxHeartbeatVolume, t);
            
            // Speed up heartbeat when very close
            heartbeatSource.pitch = Mathf.Lerp(0.8f, 1.3f, t);
            
            if (!heartbeatSource.isPlaying)
            {
                heartbeatSource.Play();
            }
        }
        else
        {
            heartbeatSource.volume = 0f;
        }
    }
    
    void UpdateRoars()
    {
        if (Time.time >= nextRoarTime)
        {
            PlayRoar();
            nextRoarTime = Time.time + Random.Range(minRoarInterval, maxRoarInterval);
        }
    }
    
    public void PlayRoar()
    {
        if (roarClips == null || roarClips.Length == 0) return;
        
        AudioClip clip = roarClips[Random.Range(0, roarClips.Length)];
        mainAudioSource.pitch = Random.Range(0.85f, 1.15f);
        mainAudioSource.PlayOneShot(clip, roarVolume);
    }
    
    /// <summary>
    /// Call this when squid detects/aggros on player
    /// </summary>
    public void PlayAggroRoar()
    {
        if (roarClips == null || roarClips.Length == 0) return;
        
        AudioClip clip = roarClips[Random.Range(0, roarClips.Length)];
        mainAudioSource.pitch = Random.Range(1.0f, 1.2f);
        mainAudioSource.PlayOneShot(clip, 1f);
    }
}
