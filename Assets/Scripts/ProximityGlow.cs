using UnityEngine;

public class ProximityGlow : MonoBehaviour
{
    [Header("Proximity Settings")]
    [Tooltip("Maximum distance for glow effect")]
    [Range(1f, 20f)]
    public float maxGlowDistance = 8f;
    
    [Tooltip("Distance where glow starts to activate")]
    [Range(0.5f, 10f)]
    public float minGlowDistance = 2f;
    
    [Header("Intensity Settings")]
    [Tooltip("Base light intensity when player is far")]
    public float baseIntensity = 3.5f;
    
    [Tooltip("Maximum light intensity when player is close")]
    public float maxIntensity = 8f;
    
    [Tooltip("How fast the glow changes (smoothing)")]
    [Range(0.1f, 5f)]
    public float glowSpeed = 2f;
    
    [Header("References")]
    public Light glowLight;
    
    [Tooltip("Tag of the player object (default: Player)")]
    public string playerTag = "Player";
    
    private Transform playerTransform;
    private float targetIntensity;
    private float currentIntensity;
    
    void Start()
    {
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            playerTransform = player.transform;
            Debug.Log($"ProximityGlow: Found player '{player.name}'");
        }
        else
        {
            // Fallback to main camera
            if (Camera.main != null)
            {
                playerTransform = Camera.main.transform;
                Debug.LogWarning($"ProximityGlow: No player with tag '{playerTag}', using Main Camera");
            }
            else
            {
                Debug.LogError($"ProximityGlow: No player or camera found!");
                enabled = false;
                return;
            }
        }
        
        // Find light - search children more thoroughly
        if (glowLight == null)
        {
            // Try to find by name first
            Transform glowChild = transform.Find("MushroomGlow");
            if (glowChild != null)
            {
                glowLight = glowChild.GetComponent<Light>();
                Debug.Log($"ProximityGlow: Found light by name 'MushroomGlow'");
            }
            
            // If still not found, search all children
            if (glowLight == null)
            {
                glowLight = GetComponentInChildren<Light>();
                if (glowLight != null)
                {
                    Debug.Log($"ProximityGlow: Found light '{glowLight.gameObject.name}' in children");
                }
            }
        }
        
        if (glowLight == null)
        {
            Debug.LogError($"ProximityGlow on {gameObject.name}: No Light found! Please assign manually.");
            enabled = false;
            return;
        }
        
        currentIntensity = baseIntensity;
        targetIntensity = baseIntensity;
        
        Debug.Log($"ProximityGlow initialized successfully on {gameObject.name}");
    }
    
    void Update()
    {
        if (playerTransform == null || glowLight == null) return;
        
        // Calculate distance to player
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        
        // Calculate glow intensity based on distance
        if (distance <= minGlowDistance)
        {
            // Player is very close - max glow
            targetIntensity = maxIntensity;
        }
        else if (distance >= maxGlowDistance)
        {
            // Player is far - base glow
            targetIntensity = baseIntensity;
        }
        else
        {
            // Player is in range - interpolate
            float normalizedDistance = (distance - minGlowDistance) / (maxGlowDistance - minGlowDistance);
            targetIntensity = Mathf.Lerp(maxIntensity, baseIntensity, normalizedDistance);
        }
        
        // Smoothly transition to target intensity
        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * glowSpeed);
        glowLight.intensity = currentIntensity;
    }
    
    // Optional: Draw gizmo to visualize range
    void OnDrawGizmosSelected()
    {
        // Draw min distance (max glow)
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, minGlowDistance);
        
        // Draw max distance (starts fading)
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, maxGlowDistance);
    }
}