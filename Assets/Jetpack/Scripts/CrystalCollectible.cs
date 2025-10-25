using UnityEngine;

/// <summary>
/// Simple collectible script for crystals/spaceship parts.
/// Attach to each collectible object.
/// </summary>
public class CrystalCollectible : MonoBehaviour
{
    [Header("Collection Settings")]
    [Tooltip("Optional audio clip to play when collected")]
    public AudioClip collectionSound;
    
    [Tooltip("Volume of collection sound (0-1)")]
    [Range(0f, 1f)]
    public float soundVolume = 0.7f;
    
    [Header("Visual Feedback")]
    [Tooltip("Should the crystal rotate while idle?")]
    public bool rotateWhileIdle = true;
    
    [Tooltip("Rotation speed")]
    public float rotationSpeed = 30f;
    
    private bool isCollected = false;
    private AudioSource audioSource;

    void Start()
    {
        // Register with manager
        if (CrystalManager.Instance != null)
        {
            CrystalManager.Instance.RegisterCrystal(this);
        }
        else
        {
            Debug.LogWarning("CrystalManager not found in scene! Please add one.");
        }
        
        // Setup audio source if sound is provided
        if (collectionSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = collectionSound;
            audioSource.volume = soundVolume;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
        }
    }

    void Update()
    {
        // Simple idle rotation
        if (rotateWhileIdle && !isCollected)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if player collected it
        if (!isCollected && other.CompareTag("MainCamera"))
        {
            Collect();
        }
    }

void Collect()
    {
        if (isCollected) return; // Prevent double collection
        
        isCollected = true;
        
        // STEP 1: Hide visual IMMEDIATELY (instant feedback!)
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }
        
        // STEP 2: Disable collider IMMEDIATELY (prevent double collection)
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }
        
        // STEP 3: Play sound (after gem is hidden)
        if (audioSource != null && collectionSound != null)
        {
            audioSource.Play();
        }
        
        // STEP 4: Notify manager (updates UI immediately)
        if (CrystalManager.Instance != null)
        {
            CrystalManager.Instance.OnCrystalCollected(this);
        }
        
        // STEP 5: Destroy after sound finishes (cleanup)
        float destroyDelay = (collectionSound != null) ? collectionSound.length : 0f;
        Destroy(gameObject, destroyDelay);
    }
}
