using UnityEngine;

/// <summary>
/// Keeps the rover stable when airborne (no wheels on ground).
/// Hard-locks the rotation so the rover always faces forward with only a gentle
/// pitch that follows the velocity arc — like a roller coaster car on a parabola.
/// No tumbling, no spinning, no roll. VR-friendly.
/// Attach to the same GameObject as RoverPhysicsController.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class RoverAirborneStabilizer : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;
    public RoverPhysicsController controller;

    [Header("Pitch Follow")]
    [Tooltip("How quickly the nose follows the velocity arc (degrees/sec). " +
             "Higher = more responsive tilt. 60 feels natural.")]
    public float pitchFollowSpeed = 60f;

    [Tooltip("Max nose-down pitch in degrees. Limits how far the rover tilts on descent.")]
    public float maxPitchDown = 20f;

    [Tooltip("Max nose-up pitch in degrees.")]
    public float maxPitchUp = 12f;

    private bool isAirborne;
    private float lockedYaw;
    private float currentPitch;

    private void Awake()
    {
        rb ??= GetComponent<Rigidbody>();
        controller ??= GetComponent<RoverPhysicsController>();
    }

    private void FixedUpdate()
    {
        if (rb == null || controller == null)
            return;

        bool wheelsGrounded = IsWheelGrounded(controller.wheelFL)
                           || IsWheelGrounded(controller.wheelFR)
                           || IsWheelGrounded(controller.wheelRL)
                           || IsWheelGrounded(controller.wheelRR);

        if (wheelsGrounded)
        {
            if (isAirborne)
                isAirborne = false;
            return;
        }

        // --- Airborne ---

        if (!isAirborne)
        {
            // Just left the ground — capture heading and current pitch.
            isAirborne = true;
            lockedYaw = transform.eulerAngles.y;
            currentPitch = 0f;
        }

        // Kill ALL angular velocity — no physics rotation allowed.
        rb.angularVelocity = Vector3.zero;

        // Calculate desired pitch from velocity direction.
        float targetPitch = 0f;
        if (rb.linearVelocity.sqrMagnitude > 4f)
        {
            // Negative asin = going down = positive pitch (nose down).
            targetPitch = -Mathf.Asin(Mathf.Clamp(rb.linearVelocity.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
            targetPitch = Mathf.Clamp(targetPitch, -maxPitchUp, maxPitchDown);
        }

        // Smoothly move toward target pitch.
        currentPitch = Mathf.MoveTowards(currentPitch, targetPitch, pitchFollowSpeed * Time.fixedDeltaTime);

        // Hard-set the rotation: locked yaw, smooth pitch, zero roll.
        Quaternion targetRotation = Quaternion.Euler(currentPitch, lockedYaw, 0f);
        rb.MoveRotation(targetRotation);
    }

    private static bool IsWheelGrounded(WheelCollider wheel)
    {
        return wheel != null && wheel.isGrounded;
    }
}
