using UnityEngine;

/// <summary>
/// Respawn system for the rover. Attach to the rover GameObject (the one with the Rigidbody).
/// When the rover drives through a <see cref="RoverCheckpoint"/> trigger, that checkpoint
/// becomes the active respawn point. If the rover falls below the active checkpoint's Y
/// minus its <c>fallDistance</c>, the rover is instantly teleported back.
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

    private void Reset()
    {
        rb = GetComponent<Rigidbody>();
        controller = GetComponent<RoverPhysicsController>();
    }

    private void Awake()
    {
        rb ??= GetComponent<Rigidbody>();
        controller ??= GetComponent<RoverPhysicsController>();
    }

    private void FixedUpdate()
    {
        if (activeCheckpoint == null || rb == null)
            return;

        float fallThreshold = activeCheckpoint.transform.position.y - activeCheckpoint.fallDistance;

        if (transform.position.y < fallThreshold)
        {
            Respawn();
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

        if (controller != null)
            controller.SetInput(0f, 0f, 1f);

        Debug.Log($"<color=#ff8844>[RoverCheckpointRespawn] Respawned at {activeCheckpoint.name}</color>");
    }
}
