using UnityEngine;

/// <summary>
/// Saves the player's respawn position when they pass through this trigger zone.
/// Works alongside PlayerHealth.cs which reads lastCheckpoint for respawning.
/// </summary>
public class Checkpoint : MonoBehaviour
{
    [Header("Respawn Point")]
    [Tooltip("Where the player respawns. Defaults to this object's position if empty.")]
    public Transform respawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        // Accept XR Origin or anything tagged Player
        if (other.CompareTag("Player") || other.name.Contains("XR Origin"))
        {
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health == null) health = other.GetComponentInParent<PlayerHealth>();
            if (health == null) health = FindFirstObjectByType<PlayerHealth>();

            if (health != null)
            {
                Vector3 point = respawnPoint != null ? respawnPoint.position : transform.position;
                health.SetCheckpoint(point);
                Debug.Log($"<color=cyan>✓ Checkpoint '{name}' saved at {point}</color>");
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.8f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
