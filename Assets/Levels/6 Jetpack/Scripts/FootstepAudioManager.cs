using UnityEngine;

/// <summary>
/// Footstep audio that syncs with actual player movement.
/// Uses distance traveled to trigger steps - faster movement = faster footsteps.
/// </summary>
public class FootstepAudioManager : MonoBehaviour
{
    [Header("=== AUDIO CLIP ===")]
    [Tooltip("Single footstep sound (or array for variation)")]
    public AudioClip[] footstepClips;
    
    [Header("=== VOLUME ===")]
    [Range(0f, 1f)]
    public float volume = 0.4f;
    
    [Range(0f, 0.2f)]
    public float volumeVariation = 0.05f;
    
    [Header("=== PITCH ===")]
    public float pitchMin = 0.95f;
    public float pitchMax = 1.05f;
    
    [Header("=== STRIDE SETTINGS ===")]
    [Tooltip("Distance in meters per footstep (average human stride ~0.7m)")]
    public float strideLength = 0.65f;
    
    [Tooltip("Minimum speed to play footsteps (m/s)")]
    public float minSpeed = 0.3f;
    
    [Header("=== GROUND DETECTION ===")]
    public float groundCheckDistance = 1.5f;
    public LayerMask groundLayer = ~0;
    
    [Header("=== REFERENCES ===")]
    public Transform playerTransform;
    public CharacterController characterController;
    
    // Private
    private AudioSource audioSource;
    private Vector3 lastPosition;
    private float distanceTraveled = 0f;
    private bool isGrounded = true;
    private int lastClipIndex = -1;
    
    private void Start()
    {
        // Find player
        if (playerTransform == null)
        {
            var xrOrigin = FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null)
                playerTransform = xrOrigin.transform;
            else
                playerTransform = transform;
        }
        
        // Find CharacterController
        if (characterController == null)
        {
            characterController = playerTransform.GetComponent<CharacterController>();
        }
        
        // Create audio source
        GameObject audioObj = new GameObject("FootstepAudio");
        audioObj.transform.SetParent(transform);
        audioSource = audioObj.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        
        lastPosition = playerTransform.position;
        
        Debug.Log("FootstepAudioManager: Ready - stride-based footsteps");
    }
    
    private void Update()
    {
        // Check grounded
        if (characterController != null)
        {
            isGrounded = characterController.isGrounded;
        }
        else
        {
            isGrounded = Physics.Raycast(playerTransform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance, groundLayer);
        }
        
        // Calculate horizontal distance moved this frame
        Vector3 currentPos = playerTransform.position;
        Vector3 delta = new Vector3(
            currentPos.x - lastPosition.x,
            0f, // Ignore vertical movement
            currentPos.z - lastPosition.z
        );
        
        float distanceThisFrame = delta.magnitude;
        float speed = distanceThisFrame / Time.deltaTime;
        
        lastPosition = currentPos;
        
        // Only accumulate distance if grounded and moving
        if (isGrounded && speed >= minSpeed)
        {
            distanceTraveled += distanceThisFrame;
            
            // Play footstep when stride distance reached
            if (distanceTraveled >= strideLength)
            {
                PlayFootstep();
                distanceTraveled = 0f;
            }
        }
        else
        {
            // Reset when stopped or airborne
            distanceTraveled = 0f;
        }
    }
    
    private void PlayFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;
        
        // Pick a different clip than last time if possible
        int clipIndex;
        if (footstepClips.Length > 1)
        {
            do
            {
                clipIndex = Random.Range(0, footstepClips.Length);
            } while (clipIndex == lastClipIndex);
            lastClipIndex = clipIndex;
        }
        else
        {
            clipIndex = 0;
        }
        
        AudioClip clip = footstepClips[clipIndex];
        if (clip == null) return;
        
        // Slight variations for natural feel
        audioSource.pitch = Random.Range(pitchMin, pitchMax);
        float vol = volume + Random.Range(-volumeVariation, volumeVariation);
        
        audioSource.PlayOneShot(clip, Mathf.Clamp01(vol));
    }
    
    public void SetStrideLength(float length)
    {
        strideLength = Mathf.Max(0.1f, length);
    }
    
    public float GetDistanceTraveled() => distanceTraveled;
    public bool IsGrounded() => isGrounded;
}
