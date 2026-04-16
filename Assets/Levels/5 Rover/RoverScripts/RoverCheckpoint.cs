using UnityEngine;

/// <summary>
/// Marker component for rover respawn checkpoints.
/// Place on an empty GameObject on the road surface. The rover drives through the
/// trigger collider and this checkpoint becomes the active respawn point.
/// Position = respawn location. Forward (blue arrow) = respawn facing direction.
/// </summary>
public class RoverCheckpoint : MonoBehaviour
{
    [Tooltip("How far below this checkpoint the rover can drop before respawn triggers. " +
             "Use a large value (e.g. 500) for checkpoints before big jumps.")]
    public float fallDistance = 3f;

    [Tooltip("Small Y offset added to the respawn position so the rover doesn't clip into the road.")]
    public float respawnLift = 0.5f;

    public Vector3 RespawnPosition => transform.position + Vector3.up * respawnLift;
    public Quaternion RespawnRotation => Quaternion.LookRotation(
        Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized, Vector3.up);

    private void Reset()
    {
        ConfigureCollider();
    }

    private void Awake()
    {
        // Force-configure every time so it works regardless of scene serialization.
        ConfigureCollider();
        Debug.Log($"<color=#44ff88>[RoverCheckpoint] {name} ready at Y={transform.position.y:F1}, fallDistance={fallDistance}</color>");
    }

    private void ConfigureCollider()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null)
            box = gameObject.AddComponent<BoxCollider>();

        box.isTrigger = true;
        box.size = new Vector3(14f, 6f, 6f); // wide enough for any road, tall + deep enough to catch the rover
        box.center = new Vector3(0f, 2f, 0f); // lift center so it sits above the road surface
    }

    private void OnDrawGizmos()
    {
        // Checkpoint icon: green translucent box + forward arrow.
        Gizmos.color = new Color(0.1f, 0.9f, 0.3f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;

        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(0.1f, 0.9f, 0.3f, 0.8f);
            Gizmos.DrawWireCube(box.center, box.size);
        }

        // Respawn direction arrow.
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.9f);
        Vector3 pos = transform.position + Vector3.up * 0.5f;
        Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Gizmos.DrawLine(pos, pos + fwd * 4f);
        // Arrowhead
        Vector3 tip = pos + fwd * 4f;
        Vector3 right = Vector3.Cross(Vector3.up, fwd) * 0.6f;
        Gizmos.DrawLine(tip, tip - fwd * 0.8f + right);
        Gizmos.DrawLine(tip, tip - fwd * 0.8f - right);
    }
}
