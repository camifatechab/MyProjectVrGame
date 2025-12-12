using UnityEngine;

public class WindZoneSimple : MonoBehaviour
{
    public Vector3 forceDirection = Vector3.right;
    public float forceStrength = 50f;

    private void OnTriggerStay(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null)
        {
            rb.AddForce(forceDirection.normalized * forceStrength, ForceMode.Acceleration);
        }
    }
}