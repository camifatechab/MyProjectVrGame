using UnityEngine;

/// <summary>
/// "Deep Dweller" - A subtle, unsettling presence in the deep water.
/// Rarely appears, far away, barely visible. When you look directly - just gone.
/// No dramatic effects. Just... was it ever there?
/// </summary>
public class DeepDweller : MonoBehaviour
{
    [Header("Depth Restriction")]
    [Tooltip("Only appears when player is below this depth")]
    public float minimumDepth = -10f;
    [Tooltip("More likely to appear the deeper you go")]
    public float maximumDepth = -18f;
    
    [Header("Appearance Timing")]
    [Tooltip("Minimum seconds between appearances")]
    public float minHiddenTime = 25f;
    [Tooltip("Maximum seconds between appearances")]
    public float maxHiddenTime = 50f;
    [Tooltip("How long it watches before silently relocating")]
    public float watchDuration = 8f;
    
    [Header("Positioning")]
    [Tooltip("How far away it appears")]
    public float watchDistance = 22f;
    [Tooltip("Angle from player's view (peripheral vision)")]
    public float peripheralAngle = 55f;
    [Tooltip("If player looks within this angle, it disappears")]
    public float spottedAngle = 25f;
    
    [Header("Subtle Eyes (Optional)")]
    public Light leftEye;
    public Light rightEye;
    public float eyeGlow = 0.8f;
    public Color eyeColor = new Color(0.4f, 0.6f, 1f, 1f); // Pale blue, ghostly
    
    [Header("Audio")]
    public AudioSource subtleSound;
    [Range(0f, 0.3f)] public float maxVolume = 0.15f;
    
    [Header("References")]
    public Transform player;
    
    // State
    private enum DwellerState { Hidden, Watching }
    private DwellerState state = DwellerState.Hidden;
    private float stateTimer;
    private Renderer[] renderers;
    
    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
        
        // Auto-find player
        if (player == null)
        {
            GameObject xrRig = GameObject.Find("XR Origin (XR Rig)");
            if (xrRig != null)
            {
                Camera cam = xrRig.GetComponentInChildren<Camera>();
                player = cam != null ? cam.transform : xrRig.transform;
            }
            else if (Camera.main != null)
            {
                player = Camera.main.transform;
            }
        }
        
        // Setup eyes
        SetupEyes();
        
        // Start hidden
        Hide();
        stateTimer = Random.Range(5f, 15f); // First appearance sooner
        
        Debug.Log("DeepDweller: Waiting in the deep...");
    }
    
    void SetupEyes()
    {
        if (leftEye != null)
        {
            leftEye.color = eyeColor;
            leftEye.intensity = 0f;
            leftEye.range = 3f;
        }
        if (rightEye != null)
        {
            rightEye.color = eyeColor;
            rightEye.intensity = 0f;
            rightEye.range = 3f;
        }
    }
    
    void Update()
    {
        if (player == null) return;
        
        stateTimer -= Time.deltaTime;
        
        switch (state)
        {
            case DwellerState.Hidden:
                UpdateHidden();
                break;
                
            case DwellerState.Watching:
                UpdateWatching();
                break;
        }
    }
    
    void UpdateHidden()
    {
        // Check if it's time to appear AND player is deep enough
        if (stateTimer <= 0f)
        {
            if (IsPlayerDeepEnough())
            {
                Appear();
            }
            else
            {
                // Player not deep enough, check again soon
                stateTimer = 3f;
            }
        }
    }
    
    void UpdateWatching()
    {
        // Face the player (slowly, subtly)
        FacePlayer();
        
        // Check if player spotted us
        if (IsPlayerLookingAtMe())
        {
            // Silently vanish - no drama, no sound, just gone
            Hide();
            stateTimer = Random.Range(minHiddenTime, maxHiddenTime);
            return;
        }
        
        // Time to relocate
        if (stateTimer <= 0f)
        {
            Hide();
            stateTimer = Random.Range(minHiddenTime * 0.5f, maxHiddenTime * 0.7f);
        }
        
        // Update subtle audio
        UpdateAudio();
    }
    
    bool IsPlayerDeepEnough()
    {
        return player.position.y <= minimumDepth;
    }
    
    void Appear()
    {
        // Choose position in peripheral vision, at depth
        Vector3 playerForward = player.forward;
        playerForward.y = 0;
        playerForward.Normalize();
        
        // Random side
        float side = Random.value > 0.5f ? 1f : -1f;
        float angle = peripheralAngle + Random.Range(5f, 20f);
        
        Vector3 offsetDir = Quaternion.Euler(0, angle * side, 0) * playerForward;
        
        // Position
        Vector3 newPos = player.position + offsetDir * watchDistance;
        
        // Match player depth or slightly deeper
        newPos.y = Mathf.Min(player.position.y - Random.Range(0f, 3f), maximumDepth);
        newPos.y = Mathf.Clamp(newPos.y, maximumDepth, minimumDepth);
        
        transform.position = newPos;
        
        // Show
        SetVisible(true);
        state = DwellerState.Watching;
        stateTimer = watchDuration;
    }
    
    void Hide()
    {
        SetVisible(false);
        state = DwellerState.Hidden;
    }
    
    void SetVisible(bool visible)
    {
        // Toggle renderers
        foreach (Renderer rend in renderers)
        {
            if (rend != null)
            {
                rend.enabled = visible;
            }
        }
        
        // Toggle eyes
        if (leftEye != null) leftEye.intensity = visible ? eyeGlow : 0f;
        if (rightEye != null) rightEye.intensity = visible ? eyeGlow : 0f;
    }
    
    void FacePlayer()
    {
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y *= 0.2f; // Minimal vertical tilt
        
        if (toPlayer.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(toPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 0.5f * Time.deltaTime);
        }
    }
    
    bool IsPlayerLookingAtMe()
    {
        Vector3 toDweller = transform.position - player.position;
        float angle = Vector3.Angle(player.forward, toDweller);
        return angle < spottedAngle;
    }
    
    void UpdateAudio()
    {
        if (subtleSound == null) return;
        
        // Very quiet, barely there
        float distance = Vector3.Distance(transform.position, player.position);
        float volume = Mathf.Lerp(maxVolume, 0f, distance / (watchDistance * 1.2f));
        
        subtleSound.volume = volume;
    }
    
    void OnDrawGizmosSelected()
    {
        // Depth zone
        Gizmos.color = new Color(0f, 0f, 0.5f, 0.3f);
        Gizmos.DrawCube(
            new Vector3(transform.position.x, (minimumDepth + maximumDepth) / 2f, transform.position.z),
            new Vector3(50f, Mathf.Abs(maximumDepth - minimumDepth), 50f)
        );
        
        if (player != null)
        {
            // Watch distance
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(player.position, watchDistance);
            
            // Line to player
            Gizmos.color = state == DwellerState.Watching ? Color.white : Color.gray;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
}
