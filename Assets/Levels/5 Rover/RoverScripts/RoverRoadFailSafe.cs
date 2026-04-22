using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class RoverRoadFailSafe : MonoBehaviour
{
    private const float MaxResponsiveOffRoadResetDelay = 0.15f;
    private const float MaxResponsiveUnsupportedResetDelay = 0.35f;
    private const float DefaultUnsupportedHorizontalResetDistance = 1.75f;
    private static readonly Vector3[] ProbePattern =
    {
        Vector3.zero,
        Vector3.right,
        Vector3.left,
        Vector3.forward,
        Vector3.back,
        new Vector3(1f, 0f, 1f),
        new Vector3(1f, 0f, -1f),
        new Vector3(-1f, 0f, 1f),
        new Vector3(-1f, 0f, -1f),
    };

    public Rigidbody rb;
    public RoverPhysicsController controller;
    public float safeProbeHeight = 2.2f;
    public float safeProbeDistance = 6f;
    public float safeProbeRadius = 0.7f;
    public float safePositionLift = 0.45f;
    public float offRoadResetDelay = 0.4f;
    public float unsupportedResetDelay = 0.6f;
    public float unsupportedHorizontalResetDistance = DefaultUnsupportedHorizontalResetDistance;
    public float fallBelowSafeDistance = 4f;
    public float minimumFallSpeed = 1.5f;
    public float resetFeedbackDuration = 0.9f;
    public string[] safeColliderPrefixes = { "Road_", "Base_Road_", "Edge_Road", "Edge_Placeholder_", "Terrain", "Platform_" };
    public string[] instantResetTriggerNames = { "Ocean" };
    [Tooltip("Prefixes of colliders to convert to triggers (guide rails only — NOT road edges).")]
    public string[] passableColliderPrefixes = { "Guide_", "Shelf_" };

    public bool IsResetWarningActive { get; private set; }
    public bool IsResetting { get; private set; }
    public bool CanReturnToCheckpoint => hasSafePosition || HasRespawnPoint();
    public bool CanReturnToRoverStart => hasRoverStartPose;
    public bool IsSuspended => suspendTimer > 0f;

    private Vector3 lastSafePosition;
    private Quaternion lastSafeRotation;
    private bool hasSafePosition;
    private Vector3 roverStartPosition;
    private Quaternion roverStartRotation;
    private bool hasRoverStartPose;
    private float offRoadTimer;
    private float resetFeedbackTimer;
    private float suspendTimer;
    private string suspendReason;

    private void Reset()
    {
        rb = GetComponent<Rigidbody>();
        controller = GetComponent<RoverPhysicsController>();
    }

    private PlayerHealth cachedPlayerHealth;
    private RoverStuckDiagnostics diagnostics;

    private void Awake()
    {
        rb ??= GetComponent<Rigidbody>();
        controller ??= GetComponent<RoverPhysicsController>();
        diagnostics ??= GetComponent<RoverStuckDiagnostics>();
        offRoadResetDelay = Mathf.Clamp(offRoadResetDelay, 0.02f, MaxResponsiveOffRoadResetDelay);
        unsupportedResetDelay = Mathf.Clamp(unsupportedResetDelay, offRoadResetDelay, MaxResponsiveUnsupportedResetDelay);
        unsupportedHorizontalResetDistance = Mathf.Max(0.5f, unsupportedHorizontalResetDistance);
        safeColliderPrefixes = EnsureRequiredPrefixes(safeColliderPrefixes, "Road_", "Base_Road_", "Edge_Road", "Edge_Placeholder_", "Terrain", "Platform_");
        passableColliderPrefixes = EnsureRequiredPrefixes(passableColliderPrefixes, "Guide_", "Shelf_");
        roverStartPosition = transform.position;
        roverStartRotation = transform.rotation;
        hasRoverStartPose = true;
        lastSafePosition = transform.position + Vector3.up * safePositionLift;
        lastSafeRotation = transform.rotation;
        hasSafePosition = true;
        MakeBarriersPassable();
    }

    private void MakeBarriersPassable()
    {
        if (passableColliderPrefixes == null || passableColliderPrefixes.Length == 0)
            return;

        Collider[] allColliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
        foreach (Collider col in allColliders)
        {
            if (col == null) continue;
            string colName = col.name;
            for (int i = 0; i < passableColliderPrefixes.Length; i++)
            {
                if (colName.StartsWith(passableColliderPrefixes[i]))
                {
                    col.isTrigger = true;
                    break;
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        if (controller != null && controller.IsMountStabilizing)
            return;

        if (resetFeedbackTimer > 0f)
        {
            resetFeedbackTimer = Mathf.Max(0f, resetFeedbackTimer - Time.fixedDeltaTime);
            IsResetting = resetFeedbackTimer > 0f;
        }
        else
        {
            IsResetting = false;
        }

        // Honor explicit external suspensions (e.g. scripted ramp jumps).
        if (suspendTimer > 0f)
        {
            suspendTimer = Mathf.Max(0f, suspendTimer - Time.fixedDeltaTime);
            // Clear warning state so UI does not flash mid-jump.
            IsResetWarningActive = false;
            offRoadTimer = 0f;
            // Still refresh safe-pose opportunistically if we happen to touch safe ground during the jump.
            if (TryGetSafeHit(out RaycastHit suspendedHit))
            {
                lastSafePosition = suspendedHit.point + Vector3.up * safePositionLift;
                lastSafeRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized, Vector3.up);
                hasSafePosition = true;
            }
            return;
        }

        if (TryGetSafeHit(out RaycastHit hit))
        {
            lastSafePosition = hit.point + Vector3.up * safePositionLift;
            lastSafeRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized, Vector3.up);
            hasSafePosition = true;
            offRoadTimer = 0f;
            IsResetWarningActive = false;
            return;
        }

        if (!hasSafePosition)
        {
            IsResetWarningActive = false;
            return;
        }

        bool movingDown = rb.linearVelocity.y < -minimumFallSpeed;
        bool fellTooLow = transform.position.y < lastSafePosition.y - fallBelowSafeDistance;
        float unsupportedHorizontalDistance = Vector3.ProjectOnPlane(transform.position - lastSafePosition, Vector3.up).magnitude;
        bool driftedOffRoad = unsupportedHorizontalDistance >= unsupportedHorizontalResetDistance;

        if (!movingDown && !fellTooLow && !driftedOffRoad)
        {
            offRoadTimer = 0f;
            IsResetWarningActive = false;
            return;
        }

        IsResetWarningActive = true;
        offRoadTimer += Time.fixedDeltaTime;

        if (offRoadTimer >= offRoadResetDelay && fellTooLow && movingDown)
        {
            diagnostics?.ReportFailSafeIntervention(
                "Fell below the last safe surface while moving downward.",
                transform.position,
                lastSafePosition,
                rb.linearVelocity);
            ResetToSafePosition();
            return;
        }

        if (driftedOffRoad && offRoadTimer >= unsupportedResetDelay)
        {
            diagnostics?.ReportFailSafeIntervention(
                "Moved too far away from the last recognized safe surface.",
                transform.position,
                lastSafePosition,
                rb.linearVelocity);
            ResetToSafePosition();
        }
    }

    private bool TryGetSafeHit(out RaycastHit hit)
    {
        return TryGetHitWithPrefixes(safeColliderPrefixes, out hit);
    }

    private void ResetToSafePosition()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Use checkpoint respawn if available, otherwise fall back to last safe road position
        if (cachedPlayerHealth == null)
            cachedPlayerHealth = FindFirstObjectByType<PlayerHealth>();

        if (cachedPlayerHealth != null && cachedPlayerHealth.respawnPoint != null)
        {
            rb.position = cachedPlayerHealth.respawnPoint.position;
            rb.rotation = Quaternion.LookRotation(
                Vector3.ProjectOnPlane(cachedPlayerHealth.respawnPoint.forward, Vector3.up).normalized,
                Vector3.up);
        }
        else
        {
            rb.position = lastSafePosition;
            rb.rotation = lastSafeRotation;
        }

        offRoadTimer = 0f;
        ApplyResetFeedback();

        if (controller != null)
        {
            controller.SetInput(0f, 0f, 1f);
        }
    }

    private void ResetToRoverStartPosition()
    {
        if (!hasRoverStartPose || rb == null)
            return;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = roverStartPosition;
        rb.rotation = roverStartRotation;

        offRoadTimer = 0f;
        ApplyResetFeedback();

        if (controller != null)
            controller.SetInput(0f, 0f, 1f);
    }

    private void ApplyResetFeedback()
    {
        resetFeedbackTimer = resetFeedbackDuration;
        IsResetting = true;
        IsResetWarningActive = false;
    }

    public bool ReturnToLastCheckpointFromUi()
    {
        if (!CanReturnToCheckpoint || rb == null)
            return false;

        ResetToSafePosition();
        return true;
    }

    public bool ReturnToRoverStartFromUi()
    {
        if (!CanReturnToRoverStart || rb == null)
            return false;

        ResetToRoverStartPosition();
        return true;
    }

    /// <summary>
    /// Temporarily pause the failsafe (e.g. during scripted ramp jumps). While suspended the
    /// rover is allowed to be airborne and far from any Road_* surface without being teleported back.
    /// Calling this while already suspended extends the suspension (takes the max of the two timers).
    /// </summary>
    public void SuspendFailsafe(float duration, string reason = null)
    {
        if (duration <= 0f) return;
        suspendTimer = Mathf.Max(suspendTimer, duration);
        suspendReason = reason;
        IsResetWarningActive = false;
        offRoadTimer = 0f;
        Debug.Log($"<color=yellow>[RoverRoadFailSafe] Suspended for {duration:F2}s ({reason ?? "no reason"}).</color>");
    }

    /// <summary>End any active suspension immediately.</summary>
    public void ResumeFailsafe()
    {
        suspendTimer = 0f;
        suspendReason = null;
    }

    /// <summary>
    /// Force the failsafe to treat the rover's current pose as the new last-known-safe pose.
    /// Useful after a successful scripted landing so the rover never gets kicked back to the ramp.
    /// </summary>
    public void MarkCurrentPoseSafe()
    {
        lastSafePosition = transform.position + Vector3.up * safePositionLift;
        lastSafeRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized, Vector3.up);
        hasSafePosition = true;
        offRoadTimer = 0f;
        IsResetWarningActive = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
            return;

        string colliderName = other.name;
        for (int i = 0; i < instantResetTriggerNames.Length; i++)
        {
            if (colliderName.StartsWith(instantResetTriggerNames[i]))
            {
                ResetToSafePosition();
                return;
            }
        }
    }

    private bool TryGetHitWithPrefixes(string[] prefixes, out RaycastHit bestHit)
    {
        bestHit = default;
        bool found = false;
        float bestDistance = float.MaxValue;

        foreach (Vector3 origin in EnumerateProbeOrigins())
        {
            RaycastHit[] hits = Physics.SphereCastAll(origin, safeProbeRadius, Vector3.down, safeProbeDistance, ~0, QueryTriggerInteraction.Ignore);
            for (int hitIndex = 0; hitIndex < hits.Length; hitIndex++)
            {
                RaycastHit hit = hits[hitIndex];
                if (ShouldIgnoreProbeHit(hit.collider))
                    continue;

                string colliderName = hit.collider != null ? hit.collider.name : string.Empty;
                for (int i = 0; i < prefixes.Length; i++)
                {
                    if (!colliderName.StartsWith(prefixes[i]))
                        continue;

                    if (hit.distance < bestDistance)
                    {
                        bestHit = hit;
                        bestDistance = hit.distance;
                        found = true;
                    }

                    break;
                }
            }
        }

        return found;
    }

    private System.Collections.Generic.IEnumerable<Vector3> EnumerateProbeOrigins()
    {
        Vector3 baseOrigin = transform.position + Vector3.up * safeProbeHeight;
        yield return baseOrigin;

        Collider footprintCollider = GetFootprintCollider();
        if (footprintCollider == null)
            yield break;

        Bounds bounds = footprintCollider.bounds;
        float lateralExtent = Mathf.Max(0f, bounds.extents.x - safeProbeRadius);
        float longitudinalExtent = Mathf.Max(0f, bounds.extents.z - safeProbeRadius);

        if (lateralExtent <= 0.01f && longitudinalExtent <= 0.01f)
            yield break;

        Vector3 footprintCenter = new Vector3(bounds.center.x, transform.position.y, bounds.center.z) + Vector3.up * safeProbeHeight;
        Vector3 rightOffset = transform.right * lateralExtent;
        Vector3 forwardOffset = transform.forward * longitudinalExtent;

        for (int i = 1; i < ProbePattern.Length; i++)
        {
            Vector3 pattern = ProbePattern[i];
            yield return footprintCenter + rightOffset * pattern.x + forwardOffset * pattern.z;
        }
    }

    private Collider GetFootprintCollider()
    {
        if (controller != null && controller.bodyCollider != null && controller.bodyCollider.enabled)
            return controller.bodyCollider;

        Collider localCollider = GetComponent<Collider>();
        return localCollider != null && localCollider.enabled ? localCollider : null;
    }

    private bool ShouldIgnoreProbeHit(Collider colliderHit)
    {
        if (colliderHit == null)
            return true;

        Transform hitTransform = colliderHit.transform;
        return hitTransform == transform || hitTransform.IsChildOf(transform);
    }

    private static string[] EnsureRequiredPrefixes(string[] source, params string[] required)
    {
        HashSet<string> prefixes = new HashSet<string>();

        if (source != null)
        {
            for (int i = 0; i < source.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(source[i]))
                    prefixes.Add(source[i]);
            }
        }

        if (required != null)
        {
            for (int i = 0; i < required.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(required[i]))
                    prefixes.Add(required[i]);
            }
        }

        string[] merged = new string[prefixes.Count];
        prefixes.CopyTo(merged);
        return merged;
    }

    private bool HasRespawnPoint()
    {
        if (cachedPlayerHealth == null)
            cachedPlayerHealth = FindFirstObjectByType<PlayerHealth>();

        return cachedPlayerHealth != null && cachedPlayerHealth.respawnPoint != null;
    }
}
