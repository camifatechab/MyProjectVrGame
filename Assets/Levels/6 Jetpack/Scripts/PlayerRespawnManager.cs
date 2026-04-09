using System.Linq;
using UnityEngine;

/// <summary>
/// Manages player respawn when falling below the tracked ride waypoint or running out of fuel.
/// Attach to the XR Origin (XR Rig) alongside AutoJetpackController.
/// </summary>
public class PlayerRespawnManager : MonoBehaviour
{
    [Header("Respawn Settings")]
    [Tooltip("Reference to the current respawn waypoint. Auto-found if empty.")]
    [SerializeField] private Transform wp01Transform;

    [Tooltip("How far below the tracked waypoint before triggering respawn")]
    [SerializeField] private float fallThreshold = 15f;

    [Tooltip("Offset above the waypoint to respawn at")]
    [SerializeField] private Vector3 respawnOffset = new Vector3(0f, 2f, 0f);

    [Tooltip("Time to wait before allowing respawn again (prevents spam)")]
    [SerializeField] private float respawnCooldown = 2f;

    [Header("Respawn Conditions")]
    [Tooltip("Respawn when falling below the tracked waypoint")]
    [SerializeField] private bool respawnOnFall = true;

    [Tooltip("Respawn when out of fuel AND falling")]
    [SerializeField] private bool respawnOnNoFuelFall = true;

    [Tooltip("How long player must be out of fuel before respawn")]
    [SerializeField] private float outOfFuelGracePeriod = 3f;

    [Header("Waypoint Tracking")]
    [Tooltip("Update respawn to the most recently reached ride waypoint")]
    [SerializeField] private bool followRideWaypointProgress = true;

    [Header("Audio Feedback")]
    [Tooltip("Sound to play on respawn")]
    [SerializeField] private AudioClip respawnSound;

    [Tooltip("Volume of respawn sound")]
    [SerializeField] private float respawnSoundVolume = 0.8f;

    private AutoJetpackController jetpackController;
    private CharacterController characterController;
    private AudioSource audioSource;
    private RideableCreature rideableCreature;

    private float failHeight;
    private float lastRespawnTime = -999f;
    private float outOfFuelTimer = 0f;
    private bool isRespawning = false;
    private bool respawnEnabled = true;
    private int respawnCount = 0;

    void Start()
    {
        jetpackController = GetComponent<AutoJetpackController>();
        characterController = GetComponent<CharacterController>();
        rideableCreature = FindFirstObjectByType<RideableCreature>();

        if (jetpackController == null)
            Debug.LogError("[PlayerRespawnManager] AutoJetpackController not found on this GameObject!");

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        if (wp01Transform == null)
        {
            wp01Transform = ResolveRespawnWaypoint();
            if (wp01Transform != null)
            {
                Debug.Log($"<color=green>✓ PlayerRespawnManager: Found respawn waypoint '{wp01Transform.name}' at Y={wp01Transform.position.y}</color>");
            }
            else
            {
                Debug.LogError("[PlayerRespawnManager] Could not find a respawn waypoint. Please assign one manually.");
            }
        }

        RefreshFailHeight();

        if (wp01Transform != null)
        {
            Debug.Log($"<color=cyan>✓ PlayerRespawnManager Ready!</color>\n" +
                      $"  Waypoint: {wp01Transform.name}\n" +
                      $"  Fail Height: {failHeight}\n" +
                      $"  Respawn Position: {GetRespawnPosition()}");
        }
    }

    void Update()
    {
        if (!respawnEnabled)
            return;

        UpdateTrackedRespawnWaypoint();

        if (isRespawning || wp01Transform == null)
            return;

        if (Time.time - lastRespawnTime < respawnCooldown)
            return;

        bool shouldRespawn = false;
        string respawnReason = "";

        if (respawnOnFall && transform.position.y < failHeight)
        {
            shouldRespawn = true;
            respawnReason = $"Fell below threshold (Y={transform.position.y:F1} < {failHeight:F1})";
        }

        if (respawnOnNoFuelFall && jetpackController != null && characterController != null)
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

        if (shouldRespawn)
            TriggerRespawn(respawnReason);
    }

    void LateUpdate()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.R))
            ForceRespawn();
#endif
    }

    public void TriggerRespawn(string reason = "Manual")
    {
        if (!respawnEnabled || isRespawning)
            return;

        isRespawning = true;
        lastRespawnTime = Time.time;
        respawnCount++;

        Debug.Log($"<color=yellow>↻ RESPAWNING #{respawnCount}</color>\n  Reason: {reason}");

        ExecuteRespawn();
        isRespawning = false;
    }

    public void SetRespawnWaypoint(Transform waypoint)
    {
        if (waypoint == null)
            return;

        wp01Transform = waypoint;
        RefreshFailHeight();
    }

    public void ForceRespawn()
    {
        lastRespawnTime = -999f;
        TriggerRespawn("Force Respawn");
    }

    public void SetRespawnEnabled(bool enabled)
    {
        respawnEnabled = enabled;
        outOfFuelTimer = 0f;
    }

    public int GetRespawnCount() => respawnCount;
    public float GetFailHeight() => failHeight;

    public bool IsInDanger()
    {
        if (!respawnEnabled)
            return false;

        bool outOfFuelAndAirborne = jetpackController != null &&
            characterController != null &&
            jetpackController.IsOutOfFuel() &&
            !characterController.isGrounded;

        return transform.position.y < failHeight || outOfFuelAndAirborne;
    }

    private void ExecuteRespawn()
    {
        Vector3 respawnPos = GetRespawnPosition();

        if (characterController != null)
            characterController.enabled = false;

        transform.position = respawnPos;

        if (characterController != null)
            characterController.enabled = true;

        if (jetpackController != null)
            jetpackController.RefillFuelFull();

        outOfFuelTimer = 0f;

        if (respawnSound != null && audioSource != null)
            audioSource.PlayOneShot(respawnSound, respawnSoundVolume);

        Debug.Log($"<color=green>✓ Respawned at {respawnPos}</color>");
    }

    private Vector3 GetRespawnPosition()
    {
        if (wp01Transform != null)
            return wp01Transform.position + respawnOffset;

        return transform.position;
    }

    private void UpdateTrackedRespawnWaypoint()
    {
        if (!followRideWaypointProgress)
            return;

        if (rideableCreature == null)
            rideableCreature = FindFirstObjectByType<RideableCreature>();

        if (rideableCreature == null || !rideableCreature.IsFlying)
            return;

        Transform currentWaypoint = rideableCreature != null ? rideableCreature.CurrentWaypoint : null;
        if (currentWaypoint != null &&
            currentWaypoint.name.StartsWith("WP") &&
            currentWaypoint != wp01Transform)
            SetRespawnWaypoint(currentWaypoint);
    }

    private void RefreshFailHeight()
    {
        if (wp01Transform != null)
            failHeight = wp01Transform.position.y - fallThreshold;
    }

    private Transform ResolveRespawnWaypoint()
    {
        GameObject exact = GameObject.Find("WP01_Liftoff");
        if (exact != null)
            return exact.transform;

        Transform[] allTransforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        Transform wp01 = allTransforms.FirstOrDefault(t => t != null && t.name.StartsWith("WP01_"));
        if (wp01 != null)
            return wp01;

        Transform flightPath = allTransforms.FirstOrDefault(t => t != null && t.name == "FlightPath");
        if (flightPath == null)
            return null;

        return flightPath.Cast<Transform>()
            .Where(t => t != null && t.name.StartsWith("WP"))
            .OrderBy(t => t.name)
            .FirstOrDefault();
    }

    void OnDrawGizmos()
    {
        float gizmoFailHeight = failHeight;
        Vector3 gizmoCenter = Vector3.zero;

        if (wp01Transform == null)
        {
            GameObject wp01 = GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Select(t => t.gameObject)
                .FirstOrDefault(go => go.name.StartsWith("WP01_"));
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

        Gizmos.color = new Color(1f, 0.9f, 0f, 0.8f);

        float planeSize = 100f;
        Vector3 p1 = gizmoCenter + new Vector3(-planeSize, 0, -planeSize);
        Vector3 p2 = gizmoCenter + new Vector3(planeSize, 0, -planeSize);
        Vector3 p3 = gizmoCenter + new Vector3(planeSize, 0, planeSize);
        Vector3 p4 = gizmoCenter + new Vector3(-planeSize, 0, planeSize);

        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);
        Gizmos.DrawLine(p1, p3);
        Gizmos.DrawLine(p2, p4);
        Gizmos.DrawLine(gizmoCenter + Vector3.left * planeSize, gizmoCenter + Vector3.right * planeSize);
        Gizmos.DrawLine(gizmoCenter + Vector3.back * planeSize, gizmoCenter + Vector3.forward * planeSize);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(gizmoCenter, "RESPAWN THRESHOLD");
#endif
    }
}
