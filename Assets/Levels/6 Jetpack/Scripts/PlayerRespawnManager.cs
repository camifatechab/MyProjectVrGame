using UnityEngine;

/// <summary>
/// Manages player respawn when falling below WP01 or running out of fuel.
/// Attach to the XR Origin (XR Rig) alongside AutoJetpackController.
/// </summary>
public class PlayerRespawnManager : MonoBehaviour
{
    [Header("Respawn Settings")]
    [Tooltip("Reference to WP01 - will be found automatically if not assigned")]
    [SerializeField] private Transform wp01Transform;
    
    [Tooltip("How far below WP01 before triggering respawn")]
    [SerializeField] private float fallThreshold = 15f;
    
    [Tooltip("Offset above WP01 to respawn at (so player spawns above platform)")]
    [SerializeField] private Vector3 respawnOffset = new Vector3(0f, 2f, 0f);
    
    [Tooltip("Time to wait before allowing respawn again (prevents spam)")]
    [SerializeField] private float respawnCooldown = 2f;
    
    [Header("Respawn Conditions")]
    [Tooltip("Respawn when falling below WP01")]
    [SerializeField] private bool respawnOnFall = true;
    
    [Tooltip("Respawn when out of fuel AND falling")]
    [SerializeField] private bool respawnOnNoFuelFall = true;
    
    [Tooltip("How long player must be out of fuel before respawn")]
    [SerializeField] private float outOfFuelGracePeriod = 3f;
    
    [Header("Audio Feedback")]
    [Tooltip("Sound to play on respawn")]
    [SerializeField] private AudioClip respawnSound;
    
    [Tooltip("Volume of respawn sound")]
    [SerializeField] private float respawnSoundVolume = 0.8f;
    
    // References
    private AutoJetpackController jetpackController;
    private CharacterController characterController;
    private AudioSource audioSource;
    
    // State tracking
    private float failHeight;
    private float lastRespawnTime = -999f;
    private float outOfFuelTimer = 0f;
    private bool isRespawning = false;
    
    // Stats
    private int respawnCount = 0;
    
    void Start()
    {
        // Get references
        jetpackController = GetComponent<AutoJetpackController>();
        characterController = GetComponent<CharacterController>();
        
        if (jetpackController == null)
        {
            Debug.LogError("[PlayerRespawnManager] AutoJetpackController not found on this GameObject!");
        }
        
        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
        
        // Auto-find WP01 if not assigned
        if (wp01Transform == null)
        {
            GameObject wp01 = GameObject.Find("WP01_Liftoff");
            if (wp01 != null)
            {
                wp01Transform = wp01.transform;
                Debug.Log($"<color=green>✓ PlayerRespawnManager: Found WP01 at Y={wp01Transform.position.y}</color>");
            }
            else
            {
                Debug.LogError("[PlayerRespawnManager] WP01_Liftoff not found! Please assign manually.");
            }
        }
        
        // Calculate fail height
        if (wp01Transform != null)
        {
            failHeight = wp01Transform.position.y - fallThreshold;
            Debug.Log($"<color=cyan>✓ PlayerRespawnManager Ready!</color>\n" +
                      $"  WP01 Height: {wp01Transform.position.y}\n" +
                      $"  Fail Height: {failHeight}\n" +
                      $"  Respawn Position: {GetRespawnPosition()}");
        }
    }
    
    void Update()
    {
        if (isRespawning || wp01Transform == null) return;
        
        // Check cooldown
        if (Time.time - lastRespawnTime < respawnCooldown) return;
        
        bool shouldRespawn = false;
        string respawnReason = "";
        
        // Check fall condition
        if (respawnOnFall && transform.position.y < failHeight)
        {
            shouldRespawn = true;
            respawnReason = $"Fell below threshold (Y={transform.position.y:F1} < {failHeight:F1})";
        }
        
        // Check out of fuel + falling condition
        if (respawnOnNoFuelFall && jetpackController != null)
        {
            if (jetpackController.IsOutOfFuel() && !characterController.isGrounded)
            {
                outOfFuelTimer += Time.deltaTime;
                
                if (outOfFuelTimer >= outOfFuelGracePeriod && transform.position.y < wp01Transform.position.y)
                {
                    shouldRespawn = true;
                    respawnReason = $"Out of fuel and falling for {outOfFuelTimer:F1}s";
                }
            }
            else
            {
                outOfFuelTimer = 0f;
            }
        }
        
        // Execute respawn
        if (shouldRespawn)
        {
            TriggerRespawn(respawnReason);
        }
    }
    
    void LateUpdate()
    {
        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("<color=magenta>[DEBUG] R key pressed - forcing respawn</color>");
            ForceRespawn();
        }
        #endif
    }
    
    public void TriggerRespawn(string reason = "Manual")
    {
        if (isRespawning) return;
        
        isRespawning = true;
        lastRespawnTime = Time.time;
        respawnCount++;
        
        Debug.Log($"<color=yellow>↻ RESPAWNING #{respawnCount}</color>\n  Reason: {reason}");
        
        ExecuteRespawn();
        isRespawning = false;
    }
    
    private void ExecuteRespawn()
    {
        Vector3 respawnPos = GetRespawnPosition();
        
        if (characterController != null)
        {
            characterController.enabled = false;
        }
        
        transform.position = respawnPos;
        
        if (characterController != null)
        {
            characterController.enabled = true;
        }
        
        if (jetpackController != null)
        {
            jetpackController.RefillFuelFull();
        }
        
        outOfFuelTimer = 0f;
        
        if (respawnSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(respawnSound, respawnSoundVolume);
        }
        
        Debug.Log($"<color=green>✓ Respawned at {respawnPos}</color>");
    }
    
    private Vector3 GetRespawnPosition()
    {
        if (wp01Transform != null)
        {
            return wp01Transform.position + respawnOffset;
        }
        return transform.position;
    }
    
    public void ForceRespawn()
    {
        lastRespawnTime = -999f;
        TriggerRespawn("Force Respawn");
    }
    
    public int GetRespawnCount() => respawnCount;
    public float GetFailHeight() => failHeight;
    public bool IsInDanger() => transform.position.y < failHeight || (jetpackController != null && jetpackController.IsOutOfFuel() && !characterController.isGrounded);
    
    /// <summary>
    /// Draw respawn threshold plane in Scene view (editor only)
    /// </summary>
    void OnDrawGizmos()
    {
        float gizmoFailHeight = failHeight;
        Vector3 gizmoCenter = Vector3.zero;
        
        // If not in play mode, calculate from WP01
        if (wp01Transform == null)
        {
            GameObject wp01 = GameObject.Find("WP01_Liftoff");
            if (wp01 != null)
            {
                gizmoFailHeight = wp01.transform.position.y - fallThreshold;
                gizmoCenter = new Vector3(wp01.transform.position.x, gizmoFailHeight, wp01.transform.position.z);
            }
        }
        else
        {
            gizmoCenter = new Vector3(wp01Transform.position.x, gizmoFailHeight, wp01Transform.position.z);
        }
        
        // Draw yellow wireframe plane
        Gizmos.color = new Color(1f, 0.9f, 0f, 0.8f);
        
        float planeSize = 100f;
        Vector3 p1 = gizmoCenter + new Vector3(-planeSize, 0, -planeSize);
        Vector3 p2 = gizmoCenter + new Vector3(planeSize, 0, -planeSize);
        Vector3 p3 = gizmoCenter + new Vector3(planeSize, 0, planeSize);
        Vector3 p4 = gizmoCenter + new Vector3(-planeSize, 0, planeSize);
        
        // Draw perimeter
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);
        
        // Draw diagonals
        Gizmos.DrawLine(p1, p3);
        Gizmos.DrawLine(p2, p4);
        
        // Draw center cross
        Gizmos.DrawLine(gizmoCenter + Vector3.left * planeSize, gizmoCenter + Vector3.right * planeSize);
        Gizmos.DrawLine(gizmoCenter + Vector3.back * planeSize, gizmoCenter + Vector3.forward * planeSize);
        
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(gizmoCenter, "RESPAWN THRESHOLD");
        #endif
    }
}
