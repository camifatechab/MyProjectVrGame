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
    // AudioSource no longer needed - using PlayClipAtPoint instead

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
        
        // Note: No need to setup AudioSource - we use PlayClipAtPoint which handles it automatically!
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
        
        // STEP 3: Play sound (after gem is hidden) - Use PlayClipAtPoint for reliability!
        if (collectionSound != null)
        {
            // PlayClipAtPoint creates a temporary AudioSource that auto-destroys after playing
            // This ensures sound plays completely even if GameObject is destroyed
            AudioSource.PlayClipAtPoint(collectionSound, transform.position, soundVolume);
            Debug.Log($"<color=cyan>♪ Crystal collection sound playing at {transform.position}</color>");
        }
        else
        {
            Debug.LogWarning("<color=yellow>No collection sound assigned to crystal!</color>");
        }
        
        // STEP 4: Notify manager (updates UI immediately)
        if (CrystalManager.Instance != null)
        {
            CrystalManager.Instance.OnCrystalCollected(this);
        }
        
        // STEP 5: Destroy immediately (sound plays independently via PlayClipAtPoint)
        Destroy(gameObject, 0.1f); // Small delay to ensure all callbacks complete
    }
}
