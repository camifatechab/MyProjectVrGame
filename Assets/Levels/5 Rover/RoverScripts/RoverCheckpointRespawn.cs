using UnityEngine;

/// <summary>
/// Respawn system for the rover. Attach to the rover GameObject (the one with the Rigidbody).
/// Primary detection: if no wheel is touching an actual ROAD surface for <c>maxOffRoadTime</c> seconds, respawn.
/// This catches both airborne falls AND landing on non-road surfaces (terrain, volcano mesh, etc.).
/// The check is suppressed during ramp jumps via <see cref="SuppressOffRoadCheck"/>.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class RoverCheckpointRespawn : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The rover's Rigidbody. Auto-assigned if left empty.")]
    public Rigidbody rb;

    [Tooltip("Optional: the RoverPhysicsController to zero its input after respawn.")]
    public RoverPhysicsController controller;

    [Header("Active Checkpoint (read-only)")]
    [Tooltip("The checkpoint the rover will respawn at. Updated automatically as the rover drives through checkpoint triggers.")]
    public RoverCheckpoint activeCheckpoint;

    [Header("Off-Road Detection")]
    [Tooltip("Seconds with no wheel on a road surface before respawn. " +
             "Detects both airborne AND grounded-on-wrong-surface.")]
    public float maxOffRoadTime = 2.5f;

    [Header("Y-Fall Safety Net")]
    [Tooltip("If enabled, also respawns when the rover drops below checkpoint Y minus its fallDistance.")]
    public bool useYFallback = true;

    // Off-road tracking.
    private float offRoadTimer;
    private float suppressionTimer;

    private void Reset()
    {
        rb = GetComponent<Rigidbody>();
        controller = GetComponent<RoverPhysicsController>();
    }

    private void Awake()
    {
        rb ??= GetComponent<Rigidbody>();
        controller ??= GetComponent<RoverPhysicsController>();
        Debug.Log($"<color=#44ff88>[RoverCheckpointRespawn] Ready on {name}. rb={rb != null}, controller={controller != null}</color>");
    }

    /// <summary>
    /// Call this to temporarily suppress the off-road respawn check (e.g. during a ramp jump).
    /// </summary>
    public void SuppressOffRoadCheck(float duration)
    {
        suppressionTimer = duration;
        offRoadTimer = 0f;
    }

    // Keep old name working so LaunchTrigger doesn't break.
    public void SuppressAirborneCheck(float duration) => SuppressOffRoadCheck(duration);

    private void FixedUpdate()
    {
        if (activeCheckpoint == null || rb == null)
            return;

        // --- Y-based fall detection (safety net) ---
        if (useYFallback)
        {
            float fallThreshold = activeCheckpoint.transform.position.y - activeCheckpoint.fallDistance;
            if (transform.position.y < fallThreshold)
            {
                Debug.Log($"<color=#ff8844>[RoverCheckpointRespawn] Y-fall detected (rover Y={transform.position.y:F1}, threshold={fallThreshold:F1})</color>");
                Respawn();
                return;
            }
        }

        // --- Primary: off-road detection (no wheel on a road surface) ---
        if (controller == null || maxOffRoadTime <= 0f)
            return;

        // Count down suppression (active during ramp jumps).
        if (suppressionTimer > 0f)
        {
            suppressionTimer -= Time.fixedDeltaTime;
            offRoadTimer = 0f;
            return;
        }

        bool anyWheelOnRoad = IsWheelOnRoad(controller.wheelFL)
                           || IsWheelOnRoad(controller.wheelFR)
                           || IsWheelOnRoad(controller.wheelRL)
                           || IsWheelOnRoad(controller.wheelRR);

        if (anyWheelOnRoad)
        {
            offRoadTimer = 0f;
        }
        else
        {
            offRoadTimer += Time.fixedDeltaTime;

            if (offRoadTimer >= maxOffRoadTime)
            {
                Debug.Log($"<color=#ff8844>[RoverCheckpointRespawn] Off-road for {maxOffRoadTime:F1}s — respawning</color>");
                Respawn();
                return;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
            return;

        RoverCheckpoint checkpoint = other.GetComponent<RoverCheckpoint>();
        if (checkpoint == null)
            checkpoint = other.GetComponentInParent<RoverCheckpoint>();

        if (checkpoint != null)
        {
            activeCheckpoint = checkpoint;
            Debug.Log($"<color=#44ff88>[RoverCheckpointRespawn] Checkpoint activated: {checkpoint.name}</color>");
        }
    }

    private void Respawn()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = activeCheckpoint.RespawnPosition;
        rb.rotation = activeCheckpoint.RespawnRotation;

        offRoadTimer = 0f;
        suppressionTimer = Mathf.Max(suppressionTimer, 2f); // 2s grace — off-road check won't fire immediately after respawn

        if (controller != null)
            controller.SetInput(0f, 0f, 1f);

        Debug.Log($"<color=#ff8844>[RoverCheckpointRespawn] Respawned at {activeCheckpoint.name}</color>");
    }

    /// <summary>
    /// Checks if a wheel is grounded on an actual road segment (not terrain or other geometry).
    /// Road segments are named: Road_*, Base_Road_*, Edge_Road_*.
    /// </summary>
    private static bool IsWheelOnRoad(WheelCollider wheel)
    {
        if (wheel == null || !wheel.isGrounded)
            return false;

        WheelHit hit;
        if (!wheel.GetGroundHit(out hit))
            return false;

        string name = hit.collider.gameObject.name;
        return name.StartsWith("Road") || name.StartsWith("Base_Road") || name.StartsWith("Edge_Road");
    }
}
