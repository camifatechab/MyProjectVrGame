using UnityEngine;
using System.Collections;

public class RespawnableItem : MonoBehaviour
{
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Rigidbody rb;

    public float respawnDelay = 5f; // How long to wait before respawning (optional)
    public float distanceThreshold = 10f; // Max allowed distance from initial position before respawn

    private void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (Vector3.Distance(transform.position, initialPosition) > distanceThreshold)
        {
            StartCoroutine(RespawnAfterDelay());
        }
    }

    private System.Collections.IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);

        // Reset position and velocity
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.position = initialPosition;
        transform.rotation = initialRotation;
    }

    public void ForceRespawn()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.position = initialPosition;
        transform.rotation = initialRotation;
    }
}
