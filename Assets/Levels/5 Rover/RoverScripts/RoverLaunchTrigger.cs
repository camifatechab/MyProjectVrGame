using UnityEngine;

/// <summary>
/// Simple launch trigger for the rover ramp jump.
/// Place on a GameObject with a trigger BoxCollider at the end of the ramp.
/// When the rover enters the trigger, its velocity is set to a calibrated launch vector
/// that produces a natural parabolic arc to the landing zone.
/// During flight the rover is kept stable (no spinning, heavier feel).
/// After landing, the rover is dampened so it doesn't bounce off the road.
/// </summary>
public class RoverLaunchTrigger : MonoBehaviour
{
    [Header("Launch")]
    [Tooltip("Minimum launch speed (m/s). If the rover is already going faster, its speed is preserved.")]
    public float minimumLaunchSpeed = 28f;

    [Tooltip("Launch pitch above horizontal (degrees). 18-22 works well for the volcano jump.")]
    public float launchPitchDegrees = 20f;

    [Tooltip("World-space forward direction for the launch. If zero, uses this trigger's forward.")]
    public Vector3 launchDirectionOverride = Vector3.zero;

    [Header("In-Flight Stability")]
    [Tooltip("Extra gravity multiplier during flight (0 = normal gravity, 0.4 = 40% heavier). " +
             "Makes the rover feel heavy without being a brick.")]
    public float flightExtraGravity = 0.4f;

    [Tooltip("Angular damping per physics step during flight (0.85 = strong, 0.95 = gentle). " +
             "Prevents spinning in the air.")]
    [Range(0.5f, 1f)]
    public float flightAngularDamping = 0.85f;

    [Header("Landing Dampener")]
    [Tooltip("How long after launch the dampener stays active waiting for landing (seconds).")]
    public float landingWindowDuration = 8f;

    [Tooltip("Extra downforce applied for a few seconds after touchdown to keep the rover planted.")]
    public float landingDownforce = 2000f;

    [Tooltip("How long after first ground contact the downforce stays active (seconds).")]
    public float landingDownforceDuration = 2f;

    [Tooltip("How much of the vertical bounce velocity to kill on landing (0 = none, 1 = all).")]
    [Range(0f, 1f)]
    public float bounceDamping = 0.85f;

    [Header("Cooldown")]
    [Tooltip("Seconds before the trigger can fire again (prevents double-launches).")]
    public float cooldown = 4f;

    private float lastLaunchTime = -999f;

    // Landing dampener state.
    private Rigidbody trackedRb;
    private RoverPhysicsController trackedController;
    private float landingWindowTimer;
    private float landingDownforceTimer;
    private bool waitingForLanding;
    private bool landingActive;

    private void Reset()
    {
        ConfigureCollider();
    }

    private void Awake()
    {
        ConfigureCollider();
        Debug.Log($"<color=#77ddff>[RoverLaunchTrigger] {name} ready at {transform.position}, dir={launchDirectionOverride}, speed={minimumLaunchSpeed}, pitch={launchPitchDegrees}</color>");
    }

    private void ConfigureCollider()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null)
            box = gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(14f, 8f, 6f);
        box.center = new Vector3(0f, 3f, 0f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (Time.time - lastLaunchTime < cooldown) return;

        RoverPhysicsController controller = other.GetComponentInParent<RoverPhysicsController>();
        if (controller == null) return;

        Rigidbody rb = controller.rb != null ? controller.rb : controller.GetComponent<Rigidbody>();
        if (rb == null) return;

        // Resolve horizontal launch direction.
        Vector3 forward;
        if (launchDirectionOverride.sqrMagnitude > 0.0001f)
            forward = Vector3.ProjectOnPlane(launchDirectionOverride, Vector3.up).normalized;
        else
            forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;

        // Build launch velocity: preserve the rover's current speed if it exceeds the minimum.
        float currentSpeed = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up).magnitude;
        float speed = Mathf.Max(currentSpeed, minimumLaunchSpeed);

        float pitch = launchPitchDegrees * Mathf.Deg2Rad;
        Vector3 launchDir = (forward * Mathf.Cos(pitch) + Vector3.up * Mathf.Sin(pitch)).normalized;

        rb.linearVelocity = launchDir * speed;
        rb.angularVelocity = Vector3.zero;
        rb.WakeUp();

        // Start tracking for in-flight stability + landing dampening.
        trackedRb = rb;
        trackedController = controller;
        landingWindowTimer = landingWindowDuration;
        landingDownforceTimer = 0f;
        waitingForLanding = true;
        landingActive = false;

        lastLaunchTime = Time.time;

        // Tell the respawn system not to trigger during the jump.
        RoverCheckpointRespawn respawn = controller.GetComponent<RoverCheckpointRespawn>();
        if (respawn != null)
            respawn.SuppressAirborneCheck(landingWindowDuration);

        Debug.Log($"<color=#77ddff>[RoverLaunchTrigger] Launched {controller.name} at {speed:F1} m/s, pitch {launchPitchDegrees:F0} deg</color>");
    }

    private void FixedUpdate()
    {
        if (trackedRb == null)
            return;

        // Phase 1: in-flight — waiting for the rover to land.
        if (waitingForLanding)
        {
            landingWindowTimer -= Time.fixedDeltaTime;

            if (landingWindowTimer <= 0f)
            {
                ClearTracking();
                return;
            }

            // --- In-flight stability ---
            // Kill spin so the rover stays flat and the player stays planted.
            trackedRb.angularVelocity *= flightAngularDamping;

            // Extra gravity so the rover feels heavy (not floaty).
            if (flightExtraGravity > 0f)
                trackedRb.AddForce(Physics.gravity * flightExtraGravity, ForceMode.Acceleration);

            // Check if any wheel has touched ground.
            if (trackedController != null && HasAnyWheelContact(trackedController))
            {
                // Touchdown! Kill the vertical bounce.
                Vector3 vel = trackedRb.linearVelocity;
                if (vel.y > 0f)
                {
                    vel.y *= (1f - bounceDamping);
                    trackedRb.linearVelocity = vel;
                }
                trackedRb.angularVelocity *= 0.2f;

                waitingForLanding = false;
                landingActive = true;
                landingDownforceTimer = landingDownforceDuration;

                Debug.Log($"<color=#88ff88>[RoverLaunchTrigger] Landing detected. Dampening active for {landingDownforceDuration:F1}s</color>");
            }

            return;
        }

        // Phase 2: post-touchdown downforce to keep the rover planted.
        if (landingActive)
        {
            landingDownforceTimer -= Time.fixedDeltaTime;

            if (landingDownforceTimer <= 0f)
            {
                ClearTracking();
                return;
            }

            // Apply extra downforce to stick the rover to the road.
            trackedRb.AddForce(Vector3.down * landingDownforce, ForceMode.Force);

            // Keep killing upward bounces.
            Vector3 vel = trackedRb.linearVelocity;
            if (vel.y > 1f)
            {
                vel.y *= (1f - bounceDamping);
                trackedRb.linearVelocity = vel;
            }
        }
    }

    private void ClearTracking()
    {
        trackedRb = null;
        trackedController = null;
        waitingForLanding = false;
        landingActive = false;
        landingWindowTimer = 0f;
        landingDownforceTimer = 0f;
    }

    private static bool HasAnyWheelContact(RoverPhysicsController controller)
    {
        return IsWheelGrounded(controller.wheelFL)
            || IsWheelGrounded(controller.wheelFR)
            || IsWheelGrounded(controller.wheelRL)
            || IsWheelGrounded(controller.wheelRR);
    }

    private static bool IsWheelGrounded(WheelCollider wheel)
    {
        return wheel != null && wheel.isGrounded;
    }

    private void OnDrawGizmos()
    {
        // Trigger volume.
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.color = new Color(0.3f, 0.85f, 1f, 0.2f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(0.3f, 0.85f, 1f, 0.85f);
            Gizmos.DrawWireCube(box.center, box.size);
        }

        // Launch arc preview.
        Gizmos.matrix = Matrix4x4.identity;
        Vector3 fwd = launchDirectionOverride.sqrMagnitude > 0.0001f
            ? Vector3.ProjectOnPlane(launchDirectionOverride, Vector3.up).normalized
            : Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;

        float pitch = launchPitchDegrees * Mathf.Deg2Rad;
        Vector3 launchDir = (fwd * Mathf.Cos(pitch) + Vector3.up * Mathf.Sin(pitch)).normalized;
        float speed = minimumLaunchSpeed;

        // Account for extra flight gravity in preview.
        Vector3 effectiveGravity = Physics.gravity * (1f + flightExtraGravity);

        // Draw parabolic arc preview.
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
        Vector3 vel = launchDir * speed;
        Vector3 pos = transform.position + Vector3.up * 1f;
        float dt = 0.1f;

        for (int i = 0; i < 60; i++)
        {
            Vector3 next = pos + vel * dt;
            vel += effectiveGravity * dt;
            Gizmos.DrawLine(pos, next);
            pos = next;
        }
    }
}
