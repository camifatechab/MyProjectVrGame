using UnityEngine;

/// <summary>
/// Plays heartbeat audio when the DeepDweller squid is watching.
/// Heartbeat speeds up and gets louder based on squid distance.
/// </summary>
public class SquidHeartbeat : MonoBehaviour
{
    [Header("=== AUDIO ===")]
    [Tooltip("Heartbeat audio clip")]
    public AudioClip heartbeatClip;
    
    [Header("=== VOLUME ===")]
    [Tooltip("Maximum heartbeat volume when squid is close")]
    [Range(0f, 1f)]
    public float maxVolume = 0.7f;
    
    [Tooltip("Minimum volume when squid is far but watching")]
    [Range(0f, 1f)]
    public float minVolume = 0.2f;
    
    [Tooltip("How fast volume fades in/out")]
    public float volumeFadeSpeed = 2f;
    
    [Header("=== PITCH (SPEED) ===")]
    [Tooltip("Normal heartbeat pitch when squid is far")]
    public float minPitch = 0.9f;
    
    [Tooltip("Fast heartbeat pitch when squid is close")]
    public float maxPitch = 1.6f;
    
    [Tooltip("How fast pitch changes")]
    public float pitchChangeSpeed = 3f;
    
    [Header("=== DISTANCE ===")]
    [Tooltip("Distance at which heartbeat is loudest/fastest")]
    public float closeDistance = 8f;
    
    [Tooltip("Distance at which heartbeat is quietest/slowest")]
    public float farDistance = 25f;
    
    [Header("=== SPACESHIP PARTS ===")]
    [Tooltip("Enable heartbeat when near spaceship parts")]
    public bool enableSpaceshipHeartbeat = true;
    
    [Tooltip("Distance to spaceship part that triggers heartbeat")]
    public float spaceshipProximityDistance = 12f;
    
    [Tooltip("Tag used for spaceship parts")]
    public string spaceshipPartTag = "SpaceshipPart";
    
    [Header("=== REFERENCES ===")]
    [Tooltip("DeepDweller squid (auto-finds if empty)")]
    public DeepDweller deepDweller;
    
    [Tooltip("Player transform (auto-finds Main Camera if empty)")]
    public Transform player;
    
    // Private
    private AudioSource audioSource;
    private float targetVolume = 0f;
    private float targetPitch = 1f;
    private float currentVolume = 0f;
    private float currentPitch = 1f;
    private GameObject[] spaceshipParts;
    
    
void Start()
    {
        // Create audio source
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = heartbeatClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound - always same volume
        audioSource.volume = 0f;
        audioSource.priority = 32; // High priority
        
        // Auto-find DeepDweller
        if (deepDweller == null)
        {
            deepDweller = FindObjectOfType<DeepDweller>();
            if (deepDweller != null)
            {
                Debug.Log("SquidHeartbeat: Found DeepDweller");
            }
        }
        
        // Auto-find player
        if (player == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                player = cam.transform;
                Debug.Log("SquidHeartbeat: Found Main Camera as player");
            }
        }
        
        // Find spaceship parts
        spaceshipParts = GameObject.FindGameObjectsWithTag(spaceshipPartTag);
        Debug.Log($"SquidHeartbeat: Found {spaceshipParts.Length} spaceship parts");
        
        Debug.Log("SquidHeartbeat: Initialized");
    }
    
void Update()
    {
        if (player == null || heartbeatClip == null) return;
        
        // Check if squid is watching
        bool squidIsWatching = deepDweller != null && deepDweller.IsWatching;
        
        // Check proximity to spaceship parts
        float nearestPartDistance = float.MaxValue;
        if (enableSpaceshipHeartbeat && spaceshipParts != null)
        {
            foreach (GameObject part in spaceshipParts)
            {
                if (part != null && part.activeInHierarchy)
                {
                    float dist = Vector3.Distance(player.position, part.transform.position);
                    if (dist < nearestPartDistance)
                    {
                        nearestPartDistance = dist;
                    }
                }
            }
        }
        bool nearSpaceshipPart = nearestPartDistance < spaceshipProximityDistance;
        
        // Trigger heartbeat if squid watching OR near spaceship part
        if (squidIsWatching || nearSpaceshipPart)
        {
            float intensity = 0f;
            
            if (squidIsWatching)
            {
                // Calculate intensity based on squid distance
                float squidDistance = Vector3.Distance(player.position, deepDweller.transform.position);
                float squidIntensity = 1f - Mathf.InverseLerp(closeDistance, farDistance, squidDistance);
                intensity = Mathf.Max(intensity, squidIntensity);
            }
            
            if (nearSpaceshipPart)
            {
                // Calculate intensity based on part distance
                float partIntensity = 1f - Mathf.InverseLerp(0f, spaceshipProximityDistance, nearestPartDistance);
                intensity = Mathf.Max(intensity, partIntensity);
            }
            
            intensity = Mathf.Clamp01(intensity);
            
            // Set targets based on intensity
            targetVolume = Mathf.Lerp(minVolume, maxVolume, intensity);
            targetPitch = Mathf.Lerp(minPitch, maxPitch, intensity);
            
            // Start playing if not already
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
                if (squidIsWatching)
                    Debug.Log("SquidHeartbeat: Squid is watching!");
                else
                    Debug.Log($"SquidHeartbeat: Near spaceship part! Distance: {nearestPartDistance:F1}m");
            }
        }
        else
        {
            // Nothing triggering - fade out
            targetVolume = 0f;
            targetPitch = minPitch;
        }
        
        // Smooth transitions
        currentVolume = Mathf.MoveTowards(currentVolume, targetVolume, volumeFadeSpeed * Time.deltaTime);
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, pitchChangeSpeed * Time.deltaTime);
        
        audioSource.volume = currentVolume;
        audioSource.pitch = currentPitch;
        
        // Stop if fully faded out
        if (currentVolume <= 0.01f && audioSource.isPlaying && targetVolume == 0f)
        {
            audioSource.Stop();
        }
    }
}
