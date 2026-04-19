using UnityEngine;

/// <summary>
/// Place on any checkpoint volume (needs a Collider set to Is Trigger).
/// Updates the player's respawn point when entered.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CheckpointTrigger : MonoBehaviour
{
    [Tooltip("Where the player respawns. Defaults to this object's position if left empty.")]
    public Transform spawnPosition;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerHealth health = other.GetComponent<PlayerHealth>()
                           ?? other.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        Vector3 pos = spawnPosition != null ? spawnPosition.position : transform.position;
        health.SetCheckpoint(pos);
        Debug.Log($"[Checkpoint] Active: {gameObject.name} → {pos}");
    }
}
