using UnityEngine;

/// <summary>
/// Detects when the rover is physically stuck (player pressing throttle but not moving)
/// and applies a small pop-free impulse automatically.
/// Attach to the same GameObject as RoverPhysicsController.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class RoverStuckDetector : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Auto-assigned from this GameObject if left empty.")]
    public Rigidbody rb;
    public RoverPhysicsController controller;

    [Header("Detection")]
    [Tooltip("Speed (m/s) below which the rover is considered stuck.")]
    public float stuckSpeedThreshold = 0.3f;

    [Tooltip("How long (s) the rover must be stuck + throttle pressed before auto-unstick fires.")]
    public float stuckDuration = 1.5f;

    [Tooltip("Minimum |throttle| to treat as 'player is trying to move'.")]
    public float minThrottleToDetect = 0.15f;

    [Tooltip("Maximum brake input allowed while checking for stuck (high brake = intentional stop).")]
    public float maxBrakeToDetect = 0.1f;

    [Header("Unstick Impulse")]
    [Tooltip("Upward impulse (N·s) — lifts the rover over the edge.")]
    public float upwardImpulse = 7f;

    [Tooltip("Forward impulse (N·s) along the rover's facing direction.")]
    public float forwardImpulse = 4f;

    [Header("Cooldown")]
    [Tooltip("Seconds before the detector can fire again after an unstick.")]
    public float cooldown = 3f;

    // ── state ──────────────────────────────────────────────────────────────
    private float stuckTimer;
    private float cooldownTimer;

    private void Reset()
    {
        rb         = GetComponent<Rigidbody>();
        controller = GetComponent<RoverPhysicsController>();
    }

    private void Awake()
    {
        rb         ??= GetComponent<Rigidbody>();
        controller ??= GetComponent<RoverPhysicsController>();
        Debug.Log($"<color=#ffaa44>[RoverStuckDetector] Ready on {name}.</color>");
    }

    private void FixedUpdate()
    {
        if (rb == null || controller == null)
            return;

        // Never fire during scripted launches or while not mounted.
        if (controller.IsScriptedLaunchActive || !controller.IsMounted)
        {
            stuckTimer = 0f;
            return;
        }

        // Count down cooldown.
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.fixedDeltaTime;
            stuckTimer     = 0f;
            return;
        }

        float speed        = rb.linearVelocity.magnitude;
        float throttleAbs  = Mathf.Abs(controller.throttleInput);
        float brake        = controller.brakeInput;

        bool playerTrying  = throttleAbs >= minThrottleToDetect && brake <= maxBrakeToDetect;
        bool movingSlowly  = speed < stuckSpeedThreshold;

        if (playerTrying && movingSlowly)
        {
            stuckTimer += Time.fixedDeltaTime;

            if (stuckTimer >= stuckDuration)
            {
                Unstick();
                stuckTimer    = 0f;
                cooldownTimer = cooldown;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }

    private void Unstick()
    {
        // Project forward onto the horizontal plane so the impulse is level.
        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (flatForward.sqrMagnitude < 0.001f) flatForward = Vector3.forward;
        flatForward.Normalize();

        // Respect throttle direction (could be reversing).
        float dir = Mathf.Sign(controller.throttleInput);

        Vector3 impulse = Vector3.up * upwardImpulse
                        + flatForward * (forwardImpulse * dir);

        rb.angularVelocity = Vector3.zero;
        rb.AddForce(impulse, ForceMode.Impulse);

        Debug.Log($"<color=#ffaa44>[RoverStuckDetector] Unstick fired on {name} " +
                  $"(speed={rb.linearVelocity.magnitude:F2} m/s, " +
                  $"throttle={controller.throttleInput:F2})</color>");
    }
}
