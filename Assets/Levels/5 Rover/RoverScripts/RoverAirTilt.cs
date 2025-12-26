using UnityEngine;

public class RoverAirTilt : MonoBehaviour
{
    [Header("Tilt Settings")]
    public float airbornePitch = 10f;   // degrees downward
    public float tiltSpeed = 5f;

    [Header("Ground Check")]
    public Rigidbody roverRB;
    public float groundedVelocityThreshold = 0.5f;

    private Quaternion startRotation;
    private bool isAirborne;

    void Start()
    {
        startRotation = transform.localRotation;
    }

    void Update()
    {
        // Simple airborne check (vertical velocity)
        isAirborne = Mathf.Abs(roverRB.linearVelocity.y) > groundedVelocityThreshold;

        Quaternion targetRotation;

        if (isAirborne)
        {
            // Nose down (X-axis pitch)
            targetRotation = startRotation * Quaternion.Euler(-airbornePitch, 0f, 0f);
        }
        else
        {
            // Back to normal
            targetRotation = startRotation;
        }

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRotation,
            tiltSpeed * Time.deltaTime
        );
    }
}