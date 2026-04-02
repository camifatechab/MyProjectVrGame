using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RoverRoadFailSafe : MonoBehaviour
{
    public Rigidbody rb;
    public RoverPhysicsController controller;
    public float safeProbeHeight = 2.2f;
    public float safeProbeDistance = 6f;
    public float safeProbeRadius = 0.7f;
    public float safePositionLift = 0.45f;
    public float offRoadResetDelay = 0.1f;
    public float unsupportedResetDelay = 0.35f;
    public float fallBelowSafeDistance = 2.5f;
    public float minimumFallSpeed = 1.5f;
    public string[] safeColliderPrefixes = { "Road_", "Base_Road_" };
    public string[] supportColliderPrefixes = { "Shelf_", "Guide_" };
    public string[] instantResetTriggerNames = { "Ocean" };

    private Vector3 lastSafePosition;
    private Quaternion lastSafeRotation;
    private bool hasSafePosition;
    private float offRoadTimer;

    private void Reset()
    {
        rb = GetComponent<Rigidbody>();
        controller = GetComponent<RoverPhysicsController>();
    }

    private void Awake()
    {
        rb ??= GetComponent<Rigidbody>();
        controller ??= GetComponent<RoverPhysicsController>();
        lastSafePosition = transform.position + Vector3.up * safePositionLift;
        lastSafeRotation = transform.rotation;
        hasSafePosition = true;
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        if (TryGetSafeHit(out RaycastHit hit))
        {
            lastSafePosition = hit.point + Vector3.up * safePositionLift;
            lastSafeRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized, Vector3.up);
            hasSafePosition = true;
            offRoadTimer = 0f;
            return;
        }

        if (IsSupportedByRecoverySurface())
        {
            ResetToSafePosition();
            return;
        }

        if (!hasSafePosition)
            return;

        bool movingDown = rb.linearVelocity.y < -minimumFallSpeed;
        bool fellTooLow = transform.position.y < lastSafePosition.y - fallBelowSafeDistance;

        if (!movingDown && !fellTooLow)
        {
            offRoadTimer = 0f;
            return;
        }

        offRoadTimer += Time.fixedDeltaTime;

        if (offRoadTimer >= offRoadResetDelay && fellTooLow && movingDown)
        {
            ResetToSafePosition();
            return;
        }

        if (offRoadTimer >= unsupportedResetDelay && fellTooLow)
        {
            ResetToSafePosition();
        }
    }

    private bool TryGetSafeHit(out RaycastHit hit)
    {
        Vector3 origin = transform.position + Vector3.up * safeProbeHeight;
        if (Physics.SphereCast(origin, safeProbeRadius, Vector3.down, out hit, safeProbeDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            string colliderName = hit.collider != null ? hit.collider.name : string.Empty;
            for (int i = 0; i < safeColliderPrefixes.Length; i++)
            {
                if (colliderName.StartsWith(safeColliderPrefixes[i]))
                    return true;
            }
        }

        return false;
    }

    private bool IsSupportedByRecoverySurface()
    {
        Vector3 origin = transform.position + Vector3.up * safeProbeHeight;
        if (Physics.SphereCast(origin, safeProbeRadius, Vector3.down, out RaycastHit hit, safeProbeDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            string colliderName = hit.collider != null ? hit.collider.name : string.Empty;
            for (int i = 0; i < supportColliderPrefixes.Length; i++)
            {
                if (colliderName.StartsWith(supportColliderPrefixes[i]))
                    return true;
            }
        }

        return false;
    }

    private void ResetToSafePosition()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = lastSafePosition;
        rb.rotation = lastSafeRotation;
        offRoadTimer = 0f;

        if (controller != null)
        {
            controller.SetInput(0f, 0f, 1f);
        }
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
}
