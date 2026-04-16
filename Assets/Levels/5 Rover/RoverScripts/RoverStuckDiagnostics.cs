using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class RoverStuckDiagnostics : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;
    public RoverPhysicsController controller;
    public Collider roverBodyCollider;

    [Header("Detection")]
    public bool diagnosticsEnabled = true;
    public float minimumThrottle = 0.35f;
    public float stuckForwardSpeedThreshold = 1f;
    public float stuckDetectionTime = 0.35f;
    public float reportCooldown = 1f;
    public float contactMemory = 0.2f;
    public float minimumBlockerScore = 0.35f;

    [Header("Reporting")]
    public bool logToConsole = true;
    public bool drawGizmos = true;
    public bool pauseEditorOnStuck = true;
    public bool selectOffendingCollider = true;

    [Header("Latest Report")]
    [TextArea(4, 10)] public string latestReport;
    public string lastOtherColliderName;
    public string lastOtherColliderPath;
    public string lastThisColliderName;
    public Vector3 lastWorldPoint;
    public Vector3 lastRoverLocalPoint;
    public Vector3 lastBodyLocalPoint;
    public Vector3 lastWorldNormal;
    public float lastBlockerScore;
    public float lastForwardSpeed;
    public float lastThrottle;
    public bool lastGrounded;

    private ContactSnapshot latestCandidate;
    private float stuckTimer;
    private float nextReportTime;
    private bool hasLatestCandidate;

    private struct ContactSnapshot
    {
        public Collider thisCollider;
        public Collider otherCollider;
        public Vector3 point;
        public Vector3 normal;
        public float score;
        public float time;
    }

    private void Reset()
    {
        rb = GetComponent<Rigidbody>();
        controller = GetComponent<RoverPhysicsController>();
        roverBodyCollider = controller != null && controller.bodyCollider != null
            ? controller.bodyCollider
            : GetComponent<Collider>();
    }

    private void Awake()
    {
        rb ??= GetComponent<Rigidbody>();
        controller ??= GetComponent<RoverPhysicsController>();
        roverBodyCollider ??= controller != null && controller.bodyCollider != null
            ? controller.bodyCollider
            : GetComponent<Collider>();
    }

    private void FixedUpdate()
    {
        if (!diagnosticsEnabled || rb == null || controller == null)
        {
            ClearTransientState();
            return;
        }

        bool hasFreshCandidate = hasLatestCandidate && Time.time - latestCandidate.time <= contactMemory;
        if (!hasFreshCandidate || !IsAttemptingToMoveButBlocked())
        {
            stuckTimer = 0f;
            return;
        }

        stuckTimer += Time.fixedDeltaTime;
        if (stuckTimer < stuckDetectionTime || Time.time < nextReportTime)
            return;

        EmitCollisionReport(latestCandidate);
        nextReportTime = Time.time + reportCooldown;
    }

    private void OnCollisionEnter(Collision collision)
    {
        CaptureContact(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        CaptureContact(collision);
    }

    public void ReportFailSafeIntervention(string reason, Vector3 currentPosition, Vector3 safePosition, Vector3 velocity)
    {
        if (!diagnosticsEnabled)
            return;

        StringBuilder builder = new StringBuilder(256);
        builder.Append("[RoverStuckDiagnostics] Fail-safe intervened: ");
        builder.Append(reason);
        builder.Append(" current=");
        builder.Append(FormatVector(currentPosition));
        builder.Append(" safe=");
        builder.Append(FormatVector(safePosition));
        builder.Append(" velocity=");
        builder.Append(FormatVector(velocity));

        if (hasLatestCandidate && latestCandidate.otherCollider != null)
        {
            builder.Append(" lastCollider=");
            builder.Append(latestCandidate.otherCollider.name);
            builder.Append(" lastContact=");
            builder.Append(FormatVector(latestCandidate.point));
        }

        latestReport = builder.ToString();

        if (logToConsole)
            Debug.LogWarning(latestReport, gameObject);

#if UNITY_EDITOR
        if (pauseEditorOnStuck)
            EditorApplication.isPaused = true;
#endif
    }

    private void CaptureContact(Collision collision)
    {
        if (!diagnosticsEnabled || collision == null || collision.collider == null)
            return;

        Transform otherTransform = collision.collider.transform;
        if (otherTransform == transform || otherTransform.IsChildOf(transform))
            return;

        Vector3 desiredDirection = GetDesiredMotionDirection();
        bool found = false;
        ContactSnapshot best = default;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            float score = EvaluateBlockerScore(contact.normal, desiredDirection);
            if (score < minimumBlockerScore)
                continue;

            if (!found || score > best.score)
            {
                best = new ContactSnapshot
                {
                    thisCollider = contact.thisCollider,
                    otherCollider = contact.otherCollider,
                    point = contact.point,
                    normal = contact.normal,
                    score = score,
                    time = Time.time,
                };
                found = true;
            }
        }

        if (found)
        {
            latestCandidate = best;
            hasLatestCandidate = true;
        }
    }

    private bool IsAttemptingToMoveButBlocked()
    {
        if (controller == null || rb == null)
            return false;

        float throttle = Mathf.Abs(controller.throttleInput);
        if (throttle < minimumThrottle)
            return false;

        float forwardSpeed = Mathf.Abs(Vector3.Dot(rb.linearVelocity, transform.forward));
        return forwardSpeed <= stuckForwardSpeedThreshold;
    }

    private Vector3 GetDesiredMotionDirection()
    {
        float throttle = controller != null ? controller.throttleInput : 0f;
        Vector3 planarForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (planarForward.sqrMagnitude <= 0.0001f)
            planarForward = Vector3.forward;

        return throttle < 0f ? -planarForward.normalized : planarForward.normalized;
    }

    private void EmitCollisionReport(ContactSnapshot snapshot)
    {
        Collider otherCollider = snapshot.otherCollider;
        lastOtherColliderName = otherCollider != null ? otherCollider.name : "(unknown)";
        lastOtherColliderPath = otherCollider != null ? GetHierarchyPath(otherCollider.transform) : string.Empty;
        lastThisColliderName = snapshot.thisCollider != null ? snapshot.thisCollider.name : "(unknown)";
        lastWorldPoint = snapshot.point;
        lastRoverLocalPoint = transform.InverseTransformPoint(snapshot.point);
        lastBodyLocalPoint = roverBodyCollider != null
            ? roverBodyCollider.transform.InverseTransformPoint(snapshot.point)
            : lastRoverLocalPoint;
        lastWorldNormal = snapshot.normal;
        lastBlockerScore = snapshot.score;
        lastForwardSpeed = rb != null ? Vector3.Dot(rb.linearVelocity, transform.forward) : 0f;
        lastThrottle = controller != null ? controller.throttleInput : 0f;
        lastGrounded = IsAnyWheelGrounded();

        StringBuilder builder = new StringBuilder(512);
        builder.Append("[RoverStuckDiagnostics] Possible blocker detected. collider=");
        builder.Append(lastOtherColliderName);
        builder.Append(" path=");
        builder.Append(lastOtherColliderPath);
        builder.Append(" roverCollider=");
        builder.Append(lastThisColliderName);
        builder.Append(" worldPoint=");
        builder.Append(FormatVector(lastWorldPoint));
        builder.Append(" roverLocalPoint=");
        builder.Append(FormatVector(lastRoverLocalPoint));
        builder.Append(" bodyLocalPoint=");
        builder.Append(FormatVector(lastBodyLocalPoint));
        builder.Append(" normal=");
        builder.Append(FormatVector(lastWorldNormal));
        builder.Append(" blockerScore=");
        builder.Append(lastBlockerScore.ToString("F3"));
        builder.Append(" throttle=");
        builder.Append(lastThrottle.ToString("F2"));
        builder.Append(" forwardSpeed=");
        builder.Append(lastForwardSpeed.ToString("F2"));
        builder.Append(" grounded=");
        builder.Append(lastGrounded ? "true" : "false");

        latestReport = builder.ToString();

        if (logToConsole)
            Debug.LogWarning(latestReport, otherCollider != null ? otherCollider.gameObject : gameObject);

#if UNITY_EDITOR
        if (selectOffendingCollider && otherCollider != null)
        {
            Selection.activeGameObject = otherCollider.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();
        }

        if (pauseEditorOnStuck)
            EditorApplication.isPaused = true;
#endif
    }

    private void ClearTransientState()
    {
        hasLatestCandidate = false;
        stuckTimer = 0f;
    }

    private bool IsAnyWheelGrounded()
    {
        if (controller == null)
            return false;

        return IsWheelGrounded(controller.wheelFL)
            || IsWheelGrounded(controller.wheelFR)
            || IsWheelGrounded(controller.wheelRL)
            || IsWheelGrounded(controller.wheelRR);
    }

    private static bool IsWheelGrounded(WheelCollider wheel)
    {
        return wheel != null && wheel.isGrounded;
    }

    private static float EvaluateBlockerScore(Vector3 contactNormal, Vector3 desiredDirection)
    {
        Vector3 normal = contactNormal.sqrMagnitude > 0.0001f ? contactNormal.normalized : Vector3.up;
        float supportAmount = Vector3.Dot(normal, Vector3.up);
        if (supportAmount > 0.6f)
            return 0f;

        Vector3 planarDesired = Vector3.ProjectOnPlane(desiredDirection, Vector3.up);
        if (planarDesired.sqrMagnitude > 0.0001f)
            planarDesired.Normalize();

        float directionalBlock = planarDesired.sqrMagnitude > 0.0001f
            ? Mathf.Max(0f, Vector3.Dot(normal, -planarDesired))
            : 0f;
        float overheadBlock = Mathf.Max(0f, -Vector3.Dot(normal, Vector3.up));
        return directionalBlock + overheadBlock * 0.75f;
    }

    private static string GetHierarchyPath(Transform current)
    {
        if (current == null)
            return string.Empty;

        StringBuilder builder = new StringBuilder(current.name);
        while (current.parent != null)
        {
            current = current.parent;
            builder.Insert(0, '/');
            builder.Insert(0, current.name);
        }

        return builder.ToString();
    }

    private static string FormatVector(Vector3 value)
    {
        return "(" + value.x.ToString("F3") + ", " + value.y.ToString("F3") + ", " + value.z.ToString("F3") + ")";
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos || string.IsNullOrEmpty(lastOtherColliderName))
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(lastWorldPoint, 0.18f);
        Gizmos.DrawLine(lastWorldPoint, lastWorldPoint + lastWorldNormal.normalized * 1.4f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(lastWorldPoint, 0.28f);
    }
}
