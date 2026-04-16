using UnityEngine;

/// <summary>
/// Scripted ramp-jump helper.
/// <para>
/// Place this component on an invisible trigger box at the END of the rover ramp. When the
/// rover body enters the trigger the launcher: (a) applies a calibrated velocity impulse
/// (forward + up) so the rover is guaranteed to clear the gap to the landing platform,
/// (b) temporarily suspends <see cref="RoverRoadFailSafe"/> so the rover is allowed to be
/// airborne without getting teleported back to the last safe road segment, and (c) logs a
/// clear trace to make mid-jump debugging easy.
/// </para>
/// <para>
/// A second trigger collider (<see cref="landingTrigger"/>) placed on or just above the
/// landing platform registers the successful landing: it resumes the failsafe, clears any
/// leftover angular velocity and marks the landing pose as a new safe checkpoint.
/// </para>
/// <para>
/// The launch trigger collider MUST be marked <c>isTrigger</c>. The landing trigger should
/// also be <c>isTrigger</c> but the landing platform itself should be solid ground (non-trigger
/// collider) whose name starts with <c>Platform_</c>, <c>Base_Road_</c>, or <c>Terrain</c> so
/// the failsafe's safe-surface probes accept it.
/// </para>
/// </summary>
[DisallowMultipleComponent]
public class RoverRampJumpLauncher : MonoBehaviour
{
    [Header("Target Rover")]
    [Tooltip("Name of the rover GameObject expected to enter the launch trigger. Matched by root name/tag.")]
    public string expectedRoverName = "Rover_PhysicsTest";

    [Tooltip("Accept any Rigidbody that carries a RoverPhysicsController component.")]
    public bool acceptAnyRover = true;

    [Header("Launch Impulse")]
    [Tooltip("Target horizontal speed (m/s) the rover should have on leaving the ramp. 30–40 m/s works well for the volcano jump.")]
    public float launchSpeed = 34f;

    [Tooltip("Pitch above horizontal (degrees) used to build the launch direction. 15–25° is the sweet spot for the volcano jump.")]
    public float launchPitchDegrees = 18f;

    [Tooltip("World-space forward direction used for the launch. If zero this launcher will use its own transform.forward (project on ground plane).")]
    public Vector3 launchDirectionOverride = Vector3.zero;

    [Tooltip("Guarantee the rover has at least this vertical velocity after launch (m/s). Prevents a slow roll-off from producing a weak jump.")]
    public float minimumVerticalKick = 8f;

    [Tooltip("Multiplier applied to the rover's horizontal velocity BEFORE we add the boost. 1 keeps current speed, 0 resets to zero first.")]
    [Range(0f, 1f)] public float preserveHorizontalFactor = 0.4f;

    [Header("Failsafe Suspension")]
    [Tooltip("Seconds of failsafe suspension after launch. Must cover the entire expected flight time + a small margin.")]
    public float failsafeSuspensionSeconds = 6f;

    [Header("Landing")]
    [Tooltip("Optional second trigger zone that marks the rover as safely landed.")]
    public Collider landingTrigger;

    [Tooltip("If true, the landing trigger will mark the rover's current pose as the new safe checkpoint.")]
    public bool landingMarksSafePose = true;

    [Header("Cooldown / Debug")]
    [Tooltip("Minimum seconds between two consecutive launches (prevents double-firing if the rover tumbles back into the trigger).")]
    public float relaunchCooldown = 3f;

    [Tooltip("Draw the launch and landing volumes in the editor.")]
    public bool drawGizmos = true;

    private float lastLaunchTime = -999f;
    private bool hasLandingSubscription;

    private void Reset()
    {
        // Try to auto-assign a trigger collider on this object.
        Collider self = GetComponent<Collider>();
        if (self != null && !self.isTrigger)
            self.isTrigger = true;
    }

    private void OnEnable()
    {
        EnsureLandingSubscription();
    }

    private void OnDisable()
    {
        if (hasLandingSubscription && landingTrigger != null)
        {
            LandingProxy proxy = landingTrigger.GetComponent<LandingProxy>();
            if (proxy != null) proxy.owner = null;
        }
        hasLandingSubscription = false;
    }

    private void EnsureLandingSubscription()
    {
        if (landingTrigger == null || hasLandingSubscription) return;
        LandingProxy proxy = landingTrigger.GetComponent<LandingProxy>();
        if (proxy == null) proxy = landingTrigger.gameObject.AddComponent<LandingProxy>();
        proxy.owner = this;
        hasLandingSubscription = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryLaunchFromCollider(other);
    }

    private void TryLaunchFromCollider(Collider other)
    {
        if (Time.time - lastLaunchTime < relaunchCooldown)
            return;

        RoverPhysicsController controller = ResolveController(other);
        if (controller == null)
            return;

        Rigidbody rb = controller.rb != null ? controller.rb : controller.GetComponent<Rigidbody>();
        if (rb == null)
            return;

        ApplyLaunch(rb, controller);
    }

    private RoverPhysicsController ResolveController(Collider other)
    {
        if (other == null) return null;

        RoverPhysicsController controller = other.GetComponentInParent<RoverPhysicsController>();
        if (controller == null)
            return null;

        if (!acceptAnyRover &&
            !string.IsNullOrEmpty(expectedRoverName) &&
            controller.gameObject.name != expectedRoverName)
        {
            return null;
        }

        return controller;
    }

    private void ApplyLaunch(Rigidbody rb, RoverPhysicsController controller)
    {
        Vector3 forward = ResolveLaunchForward(rb);
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        forward.Normalize();

        float pitch = Mathf.Clamp(launchPitchDegrees, -45f, 60f) * Mathf.Deg2Rad;
        Vector3 launchDir = (forward * Mathf.Cos(pitch) + Vector3.up * Mathf.Sin(pitch)).normalized;

        Vector3 currentV = rb.linearVelocity;
        Vector3 horizontal = Vector3.ProjectOnPlane(currentV, Vector3.up) * Mathf.Clamp01(preserveHorizontalFactor);
        Vector3 newVelocity = horizontal + launchDir * Mathf.Max(launchSpeed, 0.1f);

        // Guarantee the rover clears the ground so the next fixed step does not instantly find
        // a road collider below it and trip the failsafe "we were never unsupported" path.
        if (newVelocity.y < minimumVerticalKick)
            newVelocity.y = minimumVerticalKick;

        rb.linearVelocity = newVelocity;
        rb.angularVelocity = Vector3.zero;

        // Wake the rigidbody to make sure the fresh velocity is applied this physics step.
        rb.WakeUp();

        RoverRoadFailSafe failsafe = controller.GetComponent<RoverRoadFailSafe>();
        if (failsafe != null)
            failsafe.SuspendFailsafe(failsafeSuspensionSeconds, "ramp jump launch");

        lastLaunchTime = Time.time;

        Debug.Log($"<color=#77ddff>[RampJumpLauncher] Launched {controller.name}. Dir={launchDir:F2} Speed={launchSpeed:F1} Pitch={launchPitchDegrees:F1}° Suspend={failsafeSuspensionSeconds:F1}s NewVelocity={newVelocity:F2}</color>");
    }

    private Vector3 ResolveLaunchForward(Rigidbody rb)
    {
        if (launchDirectionOverride.sqrMagnitude > 0.0001f)
            return Vector3.ProjectOnPlane(launchDirectionOverride, Vector3.up);

        Vector3 selfForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (selfForward.sqrMagnitude > 0.0001f)
            return selfForward;

        // Final fallback: use the rover's own heading.
        return Vector3.ProjectOnPlane(rb.transform.forward, Vector3.up);
    }

    internal void HandleLanding(Collider other)
    {
        RoverPhysicsController controller = ResolveController(other);
        if (controller == null) return;

        RoverRoadFailSafe failsafe = controller.GetComponent<RoverRoadFailSafe>();
        if (failsafe == null) return;

        failsafe.ResumeFailsafe();
        if (landingMarksSafePose)
            failsafe.MarkCurrentPoseSafe();

        Rigidbody rb = controller.rb != null ? controller.rb : controller.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Kill any residual spin so the landing feels stable.
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log($"<color=#88ff88>[RampJumpLauncher] Landing confirmed for {controller.name}. Failsafe resumed.</color>");
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        // Launch volume.
        Collider self = GetComponent<Collider>();
        if (self != null)
        {
            Gizmos.color = new Color(0.3f, 0.85f, 1f, 0.22f);
            Gizmos.matrix = transform.localToWorldMatrix;
            if (self is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = new Color(0.3f, 0.85f, 1f, 0.9f);
                Gizmos.DrawWireCube(box.center, box.size);
            }
        }

        // Launch vector preview.
        Gizmos.matrix = Matrix4x4.identity;
        float pitch = launchPitchDegrees * Mathf.Deg2Rad;
        Vector3 forward = launchDirectionOverride.sqrMagnitude > 0.0001f
            ? launchDirectionOverride.normalized
            : transform.forward;
        Vector3 dir = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
        Vector3 launchDir = (dir * Mathf.Cos(pitch) + Vector3.up * Mathf.Sin(pitch)).normalized;

        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
        Gizmos.DrawLine(transform.position, transform.position + launchDir * Mathf.Max(4f, launchSpeed * 0.4f));

        // Landing volume.
        if (landingTrigger != null)
        {
            Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.22f);
            Gizmos.matrix = landingTrigger.transform.localToWorldMatrix;
            if (landingTrigger is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.9f);
                Gizmos.DrawWireCube(box.center, box.size);
            }
        }
    }

    /// <summary>Internal helper forwarded OnTriggerEnter from the landing collider back to the launcher.</summary>
    internal class LandingProxy : MonoBehaviour
    {
        internal RoverRampJumpLauncher owner;

        private void OnTriggerEnter(Collider other)
        {
            if (owner != null) owner.HandleLanding(other);
        }
    }
}
