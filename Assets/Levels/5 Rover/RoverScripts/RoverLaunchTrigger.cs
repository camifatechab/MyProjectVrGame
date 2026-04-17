using UnityEngine;

/// <summary>
/// Launch trigger that can solve a deterministic ballistic jump to a landing target.
/// </summary>
public class RoverLaunchTrigger : MonoBehaviour
{
    [Header("Launch")]
    [Tooltip("Minimum fallback launch speed (m/s) when target solve is unavailable.")]
    public float minimumLaunchSpeed = 28f;

    [Tooltip("Launch pitch above horizontal (degrees) used for ballistic solve.")]
    public float launchPitchDegrees = 20f;

    [Tooltip("Fallback world-space forward direction when target solve is unavailable. If zero, uses this trigger's forward.")]
    public Vector3 launchDirectionOverride = Vector3.zero;

    [Header("Launch Target")]
    [Tooltip("Optional landing target. If assigned, launch velocity is solved from rover COM to this point.")]
    public Transform landingTarget;

    [Tooltip("Vertical offset added to the landing target point for tuning.")]
    public float landingTargetYOffset = 0.75f;

    [Tooltip("If true, force-sets the landing target's RoverCheckpoint as the active respawn checkpoint at the moment of launch.")]
    public bool forceActivateLandingCheckpoint = true;

    [Header("Jump Window")]
    [Tooltip("Maximum scripted launch duration (seconds).")]
    public float landingWindowDuration = 8f;

    [Header("Cooldown")]
    [Tooltip("Seconds before the trigger can fire again (prevents double-launches).")]
    public float cooldown = 4f;

    private float lastLaunchTime = -999f;
    private RoverPhysicsController trackedController;
    private float scriptedWindowTimer;

    // Precomputed at startup from a fixed reference point so every launch
    // uses the exact same velocity regardless of the rover's incoming speed.
    private Vector3 precomputedLaunchVelocity;
    private bool hasPrecomputedVelocity;

    private void Reset()
    {
        ConfigureCollider();
    }

    private void Awake()
    {
        PrecomputeLaunchVelocity();
        Debug.Log($"<color=#77ddff>[RoverLaunchTrigger] {name} ready at {transform.position}, target={(landingTarget != null ? landingTarget.name : "none")}, pitch={launchPitchDegrees:F1}, precomputed={hasPrecomputedVelocity}</color>");
    }

    /// <summary>
    /// Solves the ballistic velocity once from the trigger's own center to the landing target.
    /// Because the origin is fixed, every launch produces an identical arc no matter the rover's entry speed.
    /// </summary>
    private void PrecomputeLaunchVelocity()
    {
        hasPrecomputedVelocity = false;
        precomputedLaunchVelocity = Vector3.zero;

        if (landingTarget == null)
            return;

        // Use the trigger collider's world-space center as the fixed launch origin.
        BoxCollider box = GetComponent<BoxCollider>();
        Vector3 origin = box != null
            ? transform.TransformPoint(box.center)
            : transform.position;

        Vector3 targetPoint = landingTarget.position + Vector3.up * landingTargetYOffset;
        hasPrecomputedVelocity = TrySolveBallisticVelocity(
            origin, targetPoint, launchPitchDegrees, Physics.gravity.magnitude,
            out precomputedLaunchVelocity);

        if (hasPrecomputedVelocity)
            Debug.Log($"<color=#77ddff>[RoverLaunchTrigger] Precomputed launch velocity: {precomputedLaunchVelocity.magnitude:F2} m/s toward {landingTarget.name}</color>");
        else
            Debug.LogWarning($"[RoverLaunchTrigger] Could not precompute ballistic toward {landingTarget.name} — will fall back to runtime solve");
    }

    private void ConfigureCollider()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null)
            box = gameObject.AddComponent<BoxCollider>();

        box.isTrigger = true;
        box.size = new Vector3(8f, 3f, 1.2f);
        box.center = new Vector3(0f, 1.4f, 0f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || Time.time - lastLaunchTime < cooldown)
            return;

        RoverPhysicsController controller = other.GetComponentInParent<RoverPhysicsController>();
        if (controller == null)
            return;

        Rigidbody rb = controller.rb != null ? controller.rb : controller.GetComponent<Rigidbody>();
        if (rb == null)
            return;

        Vector3 launchVelocity;
        bool solved;

        if (hasPrecomputedVelocity)
        {
            // Always use the same precomputed velocity — arc is identical every launch.
            launchVelocity = precomputedLaunchVelocity;
            solved = true;
        }
        else if (landingTarget != null)
        {
            // Fallback: solve at runtime from the rover's current COM.
            Vector3 targetPoint = landingTarget.position + Vector3.up * landingTargetYOffset;
            solved = TrySolveBallisticVelocity(
                rb.worldCenterOfMass,
                targetPoint,
                launchPitchDegrees,
                Physics.gravity.magnitude,
                out launchVelocity);
        }
        else
        {
            launchVelocity = Vector3.zero;
            solved = false;
        }

        if (!solved)
            launchVelocity = BuildFallbackLaunchVelocity(rb);

        rb.linearVelocity = launchVelocity;
        rb.angularVelocity = Vector3.zero;
        rb.WakeUp();

        trackedController = controller;
        scriptedWindowTimer = Mathf.Max(landingWindowDuration, Time.fixedDeltaTime);
        trackedController.BeginScriptedLaunch(scriptedWindowTimer);
        lastLaunchTime = Time.time;

        RoverCheckpointRespawn respawn = controller.GetComponent<RoverCheckpointRespawn>();
        if (respawn != null)
            respawn.SuppressAirborneCheck(landingWindowDuration);

        if (forceActivateLandingCheckpoint && landingTarget != null && respawn != null)
        {
            RoverCheckpoint cp = landingTarget.GetComponent<RoverCheckpoint>();
            if (cp == null) cp = landingTarget.GetComponentInParent<RoverCheckpoint>();
            if (cp != null)
            {
                respawn.activeCheckpoint = cp;
                Debug.Log($"<color=#77ddff>[RoverLaunchTrigger] Force-activated checkpoint: {cp.name}</color>");
            }
        }

        Debug.Log($"<color=#77ddff>[RoverLaunchTrigger] Launched {controller.name} at {launchVelocity.magnitude:F2} m/s ({(solved ? "target solve" : "fallback")})</color>");
    }

    private void FixedUpdate()
    {
        if (trackedController == null)
            return;

        scriptedWindowTimer -= Time.fixedDeltaTime;
        if (scriptedWindowTimer <= 0f || !trackedController.IsScriptedLaunchActive)
        {
            trackedController.EndScriptedLaunch();
            ClearTracking();
        }
    }

    private void ClearTracking()
    {
        trackedController = null;
        scriptedWindowTimer = 0f;
    }

    public static bool TrySolveBallisticVelocity(
        Vector3 origin,
        Vector3 target,
        float pitchDegrees,
        float gravityMagnitude,
        out Vector3 launchVelocity)
    {
        launchVelocity = Vector3.zero;

        if (gravityMagnitude <= 0.0001f)
            return false;

        Vector3 toTarget = target - origin;
        Vector3 planar = Vector3.ProjectOnPlane(toTarget, Vector3.up);
        float horizontalDistance = planar.magnitude;
        if (horizontalDistance <= 0.0001f)
            return false;

        float pitchRadians = pitchDegrees * Mathf.Deg2Rad;
        float cosPitch = Mathf.Cos(pitchRadians);
        if (cosPitch <= 0.0001f)
            return false;

        float tanPitch = Mathf.Tan(pitchRadians);
        float verticalOffset = toTarget.y;
        float denominator = 2f * cosPitch * cosPitch * (horizontalDistance * tanPitch - verticalOffset);
        if (denominator <= 0.0001f)
            return false;

        float speedSquared = gravityMagnitude * horizontalDistance * horizontalDistance / denominator;
        if (speedSquared <= 0f || float.IsNaN(speedSquared) || float.IsInfinity(speedSquared))
            return false;

        float speed = Mathf.Sqrt(speedSquared);
        Vector3 forward = planar / horizontalDistance;
        launchVelocity = forward * (speed * cosPitch) + Vector3.up * (speed * Mathf.Sin(pitchRadians));
        return IsFinite(launchVelocity);
    }

    private void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.color = new Color(0.3f, 0.85f, 1f, 0.2f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(0.3f, 0.85f, 1f, 0.85f);
            Gizmos.DrawWireCube(box.center, box.size);
        }

        Gizmos.matrix = Matrix4x4.identity;
        Vector3 start = transform.position + Vector3.up * 1f;
        Vector3 previewVelocity = BuildFallbackPreviewVelocity();

        if (landingTarget != null)
        {
            Vector3 previewTarget = landingTarget.position + Vector3.up * landingTargetYOffset;
            if (TrySolveBallisticVelocity(start, previewTarget, launchPitchDegrees, Physics.gravity.magnitude, out Vector3 solvedVelocity))
                previewVelocity = solvedVelocity;

            Gizmos.color = new Color(0.4f, 1f, 0.4f, 0.8f);
            Gizmos.DrawSphere(previewTarget, 0.45f);
        }

        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
        Vector3 vel = previewVelocity;
        Vector3 pos = start;
        float dt = 0.1f;

        for (int i = 0; i < 60; i++)
        {
            Vector3 next = pos + vel * dt;
            vel += Physics.gravity * dt;
            Gizmos.DrawLine(pos, next);
            pos = next;
        }
    }

    private Vector3 BuildFallbackLaunchVelocity(Rigidbody rb)
    {
        float currentSpeed = rb == null ? 0f : Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up).magnitude;
        float speed = Mathf.Max(currentSpeed, minimumLaunchSpeed);
        return BuildFallbackDirection() * speed;
    }

    private Vector3 BuildFallbackPreviewVelocity()
    {
        return BuildFallbackDirection() * minimumLaunchSpeed;
    }

    private Vector3 BuildFallbackDirection()
    {
        Vector3 forward = launchDirectionOverride.sqrMagnitude > 0.0001f
            ? Vector3.ProjectOnPlane(launchDirectionOverride, Vector3.up).normalized
            : Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;

        float pitch = launchPitchDegrees * Mathf.Deg2Rad;
        return (forward * Mathf.Cos(pitch) + Vector3.up * Mathf.Sin(pitch)).normalized;
    }

    private static bool IsFinite(Vector3 value)
    {
        return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
