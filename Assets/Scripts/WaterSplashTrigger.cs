using UnityEngine;

/// <summary>
/// Simple trigger-based water splash detector.
/// Place this on an invisible trigger collider at the water surface.
/// Plays a splash sound ONCE when the player enters the trigger.
/// </summary>
public class WaterSplashTrigger : MonoBehaviour
{
    [Header("Splash Audio")]
    [Tooltip("Splash sound to play on water entry")]
    public AudioClip splashSound;
    
    [Tooltip("Volume of the splash sound")]
    [Range(0f, 1f)]
    public float splashVolume = 0.7f;
    
    [Header("Detection Settings")]
    [Tooltip("Tag of the player object (usually 'Player' or leave empty for any object)")]
    public string playerTag = "Player";
    
    [Tooltip("Use tag filtering? If false, any object triggers the splash")]
    public bool useTagFiltering = false;
    
    [Header("Debug")]
    [Tooltip("Show debug messages")]
    public bool debugMode = true;
    
    // Private variables
    private bool hasSplashed = false;
    
    void OnTriggerEnter(Collider other)
    {
        // Only play splash once
        if (hasSplashed) return;
        
        // Check if this is the player (if tag filtering is enabled)
        if (useTagFiltering && !other.CompareTag(playerTag))
        {
            if (debugMode) Debug.Log($"[WaterSplash] Ignored collision with: {other.name} (wrong tag)");
            return;
        }
        
        // Play the splash sound
        if (splashSound != null)
        {
            AudioSource.PlayClipAtPoint(splashSound, transform.position, splashVolume);
            hasSplashed = true;
            
            if (debugMode) Debug.Log($"[WaterSplash] SPLASH! Triggered by: {other.name}");
        }
        else
        {
            Debug.LogWarning("[WaterSplash] No splash sound assigned!");
        }
    }
    
    // Optional: Reset splash if you want to test multiple times
    public void ResetSplash()
    {
        hasSplashed = false;
        if (debugMode) Debug.Log("[WaterSplash] Splash reset - ready to trigger again");
    }
    
    // Draw the trigger area in the Scene view
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f); // Light blue semi-transparent
        
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            
            // Draw outline
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.8f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
