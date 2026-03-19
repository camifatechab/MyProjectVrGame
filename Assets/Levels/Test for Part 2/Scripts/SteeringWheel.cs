/*using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SteeringWheel : MonoBehaviour
{
    [Header("Steering Settings")]
    public float maxSteeringAngle = 90f;
    public float returnSpeed = 5f;

    private XRGrabInteractable grabInteractable;
    private Transform grabbingHand;

    private float currentAngle = 0f;

    void Awake()
    {
        /*grabInteractable = GetComponentInChildren<XRGrabInteractable>();

        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);

        grabInteractable = GetComponentInChildren<XRGrabInteractable>();

    if (grabInteractable != null)
    {
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        grabbingHand = args.interactorObject.transform;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        grabbingHand = null;
    }

    void Update()
    {
        if (grabbingHand != null)
        {
            RotateWithHand();
        }
        else
        {
            ReturnToCenter();
        }
    }

    void RotateWithHand()
    {
        Vector3 localHandPos = transform.InverseTransformPoint(grabbingHand.position);

        // Use X and Z for steering wheel plane
        float angle = Mathf.Atan2(localHandPos.x, localHandPos.z) * Mathf.Rad2Deg;

        currentAngle = Mathf.Clamp(angle, -maxSteeringAngle, maxSteeringAngle);

        transform.localRotation = Quaternion.Euler(0, 0, -currentAngle);
    }

    void ReturnToCenter()
    {
        currentAngle = Mathf.Lerp(currentAngle, 0f, Time.deltaTime * returnSpeed);
        transform.localRotation = Quaternion.Euler(0, 0, -currentAngle);
    }

    public float GetSteeringPercent()
    {
        return currentAngle / maxSteeringAngle;
    }
}*/

/*using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SteeringWheel : MonoBehaviour
{
    public float maxSteeringAngle = 90f;
    public float returnSpeed = 5f;

    private XRGrabInteractable grabInteractable;
    private Transform grabbingHand;

    private float currentAngle = 0f;

    void Awake()
    {
        grabInteractable = GetComponentInChildren<XRGrabInteractable>();

        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrab);
            grabInteractable.selectExited.AddListener(OnRelease);
        }
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        grabbingHand = args.interactorObject.transform;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        grabbingHand = null;
    }

    void LateUpdate()
    {
        if (grabbingHand != null)
            RotateWithHand();
        else
            ReturnToCenter();
    }

    void RotateWithHand()
    {
        Vector3 localHandPos = transform.InverseTransformPoint(grabbingHand.position);

        float angle = Mathf.Atan2(localHandPos.x, localHandPos.z) * Mathf.Rad2Deg;

        currentAngle = Mathf.Clamp(angle, -maxSteeringAngle, maxSteeringAngle);

        transform.localRotation = Quaternion.Euler(0, 0, -currentAngle);
    }

    void ReturnToCenter()
    {
        currentAngle = Mathf.Lerp(currentAngle, 0f, Time.deltaTime * returnSpeed);
        transform.localRotation = Quaternion.Euler(0, 0, -currentAngle);
    }

    public float GetSteeringPercent()
    {
        return currentAngle / maxSteeringAngle;
    }
}*/

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SteeringWheel : MonoBehaviour
{
    public float maxSteeringAngle = 90f;
    public float returnSpeed = 5f;

    private XRGrabInteractable grabInteractable;
    private Transform grabbingHand;

    private float currentAngle = 0f;

    private Vector3 lastHandDirection;
    public Transform wheelVisual;

    void Awake()
    {
        grabInteractable = GetComponentInChildren<XRGrabInteractable>();

        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrab);
            grabInteractable.selectExited.AddListener(OnRelease);
        }
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        grabbingHand = args.interactorObject.transform;

        Vector3 dir = grabbingHand.position - transform.position;
        lastHandDirection = dir.normalized;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        grabbingHand = null;
    }

    void Update()
    {
        if (grabbingHand != null)
            RotateWithHand();
        else
            ReturnToCenter();
    }

    void RotateWithHand()
    {
        Vector3 localHandPos = transform.InverseTransformPoint(grabbingHand.position);

        float angle = Mathf.Atan2(localHandPos.y, localHandPos.x) * Mathf.Rad2Deg;

        //currentAngle = Mathf.Clamp(angle, -maxSteeringAngle, maxSteeringAngle);
        float targetAngle = Mathf.Clamp(angle, -maxSteeringAngle, maxSteeringAngle);
        currentAngle = Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime * 15f);

        transform.localRotation = Quaternion.Euler(0f, 0f, -currentAngle);
        //transform.localRotation = Quaternion.Euler(0f, 0f, currentAngle);
        //float angle = Mathf.Atan2(localHandPos.y, localHandPos.x) * Mathf.Rad2Deg;

        Debug.Log(currentAngle);
    }

    void ReturnToCenter()
    {
        currentAngle = Mathf.Lerp(currentAngle, 0f, Time.deltaTime * returnSpeed);
        transform.localRotation = Quaternion.Euler(0, 0, -currentAngle);
    }

    public float GetSteeringPercent()
    {
        return currentAngle / maxSteeringAngle;
    }
}