using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RoverEdgeRecoveryAssist : MonoBehaviour
{
    public Rigidbody rb;
    public RoverPhysicsController controller;
    public float minimumThrottleForAssist = 0.35f;
    public float stuckForwardSpeedThreshold = 1.6f;
    public float inwardAssistAcceleration = 7.5f;
    public float forwardAssistAcceleration = 4f;
    public float outwardVelocityCancel = 3f;
    public string guideNamePrefix = "Guide_";

    private Vector3 guideNormalSum;
    private int guideContactCount;

    private void Reset()
    {
        rb = GetComponent<Rigidbody>();
        controller = GetComponent<RoverPhysicsController>();
    }

    private void Awake()
    {
        rb ??= GetComponent<Rigidbody>();
        controller ??= GetComponent<RoverPhysicsController>();
    }

    private void FixedUpdate()
    {
        if (rb == null || controller == null || guideContactCount == 0)
        {
            guideNormalSum = Vector3.zero;
            guideContactCount = 0;
            return;
        }

        float throttle = Mathf.Clamp01(controller.throttleInput);
        float forwardSpeed = Mathf.Max(0f, Vector3.Dot(rb.linearVelocity, transform.forward));
        float stuck01 = 1f - Mathf.Clamp01(forwardSpeed / Mathf.Max(stuckForwardSpeedThreshold, 0.01f));

        if (throttle >= minimumThrottleForAssist && stuck01 > 0f)
        {
            Vector3 averageNormal = guideNormalSum / guideContactCount;
            Vector3 inward = Vector3.ProjectOnPlane(averageNormal, Vector3.up).normalized;

            if (inward.sqrMagnitude > 0.0001f)
            {
                rb.AddForce(inward * (inwardAssistAcceleration * stuck01), ForceMode.Acceleration);
                rb.AddForce(transform.forward * (forwardAssistAcceleration * stuck01), ForceMode.Acceleration);

                float outwardSpeed = Vector3.Dot(rb.linearVelocity, -inward);
                if (outwardSpeed > 0f)
                {
                    rb.AddForce(inward * (outwardSpeed * outwardVelocityCancel), ForceMode.Acceleration);
                }
            }
        }

        guideNormalSum = Vector3.zero;
        guideContactCount = 0;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.collider == null || !collision.collider.name.StartsWith(guideNamePrefix))
            return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            guideNormalSum += collision.GetContact(i).normal;
            guideContactCount++;
        }
    }
}
