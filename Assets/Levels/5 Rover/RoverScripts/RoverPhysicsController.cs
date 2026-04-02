using UnityEngine;
using UnityEngine.XR;
using Unity.XR.CoreUtils;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class RoverPhysicsController : MonoBehaviour
{
    [Header("Physics")]
    public Rigidbody rb;
    public BoxCollider bodyCollider;
    public BoxCollider playerBlocker;
    public float maxForwardSpeed = 18f;
    public float maxReverseSpeed = 7f;
    public float motorTorque = 1600f;
    public float brakeTorque = 2600f;
    public float idleBrakeTorque = 120f;
    public float steerAngle = 28f;
    public float steerResponse = 120f;
    public float downforce = 80f;
    public float extraGravity = 10f;
    public Vector3 centerOfMassOffset = new Vector3(0f, -0.35f, 0f);
    public Vector3 bodyColliderCenter = new Vector3(0f, 0.28f, 0f);
    public Vector3 bodyColliderSize = new Vector3(0.95f, 0.45f, 0.85f);
    public Vector3 playerBlockerCenter = new Vector3(0f, 0.85f, 0.05f);
    public Vector3 playerBlockerSize = new Vector3(1.35f, 1.25f, 2.2f);

    [Header("Suspension")]
    public float wheelRadius = 0.35f;
    public float suspensionDistance = 0.16f;
    public float wheelColliderYOffset = -0.1f;
    public float suspensionSpring = 28000f;
    public float suspensionDamper = 6500f;
    public float suspensionTargetPosition = 0.3f;
    public float wheelDampingRate = 1.5f;
    public float wheelSubstepSpeedThreshold = 5f;
    public int wheelSubstepsBelowThreshold = 8;
    public int wheelSubstepsAboveThreshold = 12;
    public float forwardFrictionStiffness = 1.35f;
    public float sidewaysFrictionStiffness = 1.6f;

    [Header("Wheel Colliders")]
    public WheelCollider wheelFL;
    public WheelCollider wheelFR;
    public WheelCollider wheelRL;
    public WheelCollider wheelRR;

    [Header("Visuals")]
    public Transform steeringWheelMesh;
    public float steeringWheelVisualAngle = 160f;
    public Transform frontWheelLeft;
    public Transform frontWheelRight;
    public float maxSteerVisualAngle = 30f;
    public Transform[] wheelMeshes;
    public float steeringWheelVisualSign = 1f;
    public float frontSteerVisualSign = -1f;
    public float wheelSpinVisualSign = 1f;

    [Header("Mount")]
    public Transform seatAnchor;
    public float seatHeightBoost = 0f;
    public Vector3 seatLocalPosition = new Vector3(-0.3f, 0.94f, 0.1f);
    public float mountRadius = 4f;
    public float dismountHoldDuration = 0.75f;
    public float seatYawOffset = 0f;

    [Header("VR Steering")]
    public float steerDeadzone = 8f;
    public float steerMaxAngle = 150f;

    [Header("Speed Sensitive Steering")]
    public float steeringAssistTopSpeed = 12f;
    public float lowSpeedSteerAngleMultiplier = 1f;
    public float highSpeedSteerAngleMultiplier = 0.58f;
    public float lowSpeedSteerResponseMultiplier = 1f;
    public float highSpeedSteerResponseMultiplier = 0.72f;

    [Header("Mounted Ride Comfort")]
    public bool useMountedRideComfort = true;
    public float mountedSuspensionSpringMultiplier = 1f;
    public float mountedSuspensionDamperMultiplier = 0f;
    public float mountedWheelDampingMultiplier = 0f;
    public float mountedDownforceMultiplier = 1f;
    public float mountedExtraGravityMultiplier = 1f;
    public bool useMountedSeatStabilization = true;
    public float mountedSeatPositionSmooth = 14f;
    public float mountedSeatVerticalSmooth = 2.5f;
    public float mountedSeatYawSmooth = 12f;

    [Header("Runtime Input")]
    [Range(-1f, 1f)] public float throttleInput;
    [Range(-1f, 1f)] public float steerInput;
    [Range(0f, 1f)] public float brakeInput;
    public bool useKeyboardFallback;

    private readonly WheelCollider[] wheelColliders = new WheelCollider[4];
    private readonly float[] wheelSpinAngles = new float[4];
    private readonly Quaternion[] wheelBaseRotations = new Quaternion[4];

    private Quaternion steeringWheelBaseRotation;
    private Vector3 frontLeftBaseEuler;
    private Vector3 frontRightBaseEuler;
    private float currentSteerAngle;
    private Transform frontLeftSteerTarget;
    private Transform frontRightSteerTarget;
    private XROrigin xrOrigin;
    private AutoJetpackController jetpack;
    private CharacterController charController;
    private Transform leftCtrl;
    private Transform rightCtrl;
    private InputDevice leftDevice;
    private InputDevice rightDevice;
    private MonoBehaviour[] locomotionProviders;
    private bool isMounted;
    private bool gripsLastFrame;
    private float dismountCooldown;
    private float dismountHoldTimer;
    private bool dismountReadyAfterRelease;
    private float neutralAngle;
    private float currentWheelAngle;
    private bool lastRideComfortMountedState;
    private Transform mountedRigAnchor;
    private Vector3 SeatWorldPosition =>
        seatAnchor != null ? seatAnchor.position : transform.position;

    private Vector3 SeatForward
    {
        get
        {
            Vector3 forward = seatAnchor != null ? seatAnchor.forward : transform.forward;
            forward = Vector3.ProjectOnPlane(forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            return forward.normalized;
        }
    }

    public void SetInput(float throttle, float steer, float brake)
    {
        throttleInput = Mathf.Clamp(throttle, -1f, 1f);
        steerInput = Mathf.Clamp(steer, -1f, 1f);
        brakeInput = Mathf.Clamp01(brake);
    }

    private void Reset()
    {
        rb = GetComponent<Rigidbody>();
        bodyCollider = GetComponent<BoxCollider>();
        playerBlocker = FindChildCollider("PlayerBlocker");
        AutoResolveReferences();
    }

    private void Awake()
    {
        rb ??= GetComponent<Rigidbody>();
        bodyCollider ??= GetComponent<BoxCollider>();
        playerBlocker ??= FindChildCollider("PlayerBlocker");
        AutoResolveReferences();
        InitializeMounting();
        PrepareFrontSteeringVisuals();
        CacheVisualState();
        ConfigureRigidbody();
        ConfigureBodyCollider();
        ConfigurePlayerBlocker();
        AlignWheelCollidersToVisuals();
        ConfigureWheelColliders();
        ApplyRideComfortSettings(force: true);
    }

    private void OnValidate()
    {
        rb ??= GetComponent<Rigidbody>();
        bodyCollider ??= GetComponent<BoxCollider>();
        playerBlocker ??= FindChildCollider("PlayerBlocker");
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        if (isMounted)
        {
            ReadMountedVrInput();
        }
        else if (useKeyboardFallback)
        {
            ReadKeyboardFallback();
        }
        else
        {
            SetInput(0f, 0f, 1f);
        }

        ApplySteering();
        ApplyDrive();
        ApplyDownforce();
        ApplyExtraGravity();
        ApplyRideComfortSettings();
    }

    private void Update()
    {
        HandleMounting();
        UpdateVisuals(Time.deltaTime);
    }

    private void LateUpdate()
    {
        UpdateMountedRigAnchor(Time.deltaTime);
    }

    private void ApplySteering()
    {
        float speed01 = GetSteeringSpeedFactor();
        float steerAngleMultiplier = Mathf.Lerp(lowSpeedSteerAngleMultiplier, highSpeedSteerAngleMultiplier, speed01);
        float steerResponseMultiplier = Mathf.Lerp(lowSpeedSteerResponseMultiplier, highSpeedSteerResponseMultiplier, speed01);
        float targetSteerAngle = steerInput * steerAngle * steerAngleMultiplier;
        currentSteerAngle = Mathf.MoveTowards(
            currentSteerAngle,
            targetSteerAngle,
            steerResponse * steerResponseMultiplier * Time.fixedDeltaTime);

        if (wheelFL != null) wheelFL.steerAngle = currentSteerAngle;
        if (wheelFR != null) wheelFR.steerAngle = currentSteerAngle;
    }

    private float GetSteeringSpeedFactor()
    {
        float referenceSpeed = Mathf.Max(steeringAssistTopSpeed, 0.01f);
        float forwardSpeed = Mathf.Abs(Vector3.Dot(rb.linearVelocity, transform.forward));
        return Mathf.Clamp01(forwardSpeed / referenceSpeed);
    }

    private void ApplyDrive()
    {
        float speed = Vector3.Dot(rb.linearVelocity, transform.forward);
        float driveInput = Mathf.Clamp(throttleInput, -1f, 1f);
        float appliedBrake = Mathf.Clamp01(brakeInput) * brakeTorque;
        float appliedTorque = 0f;

        if (Mathf.Abs(driveInput) < 0.01f)
        {
            appliedBrake = Mathf.Max(appliedBrake, idleBrakeTorque);
        }
        else
        {
            bool tryingToReverseDirection =
                (driveInput > 0f && speed < -0.5f) ||
                (driveInput < 0f && speed > 0.5f);

            if (tryingToReverseDirection)
            {
                appliedBrake = Mathf.Max(appliedBrake, brakeTorque);
            }
            else
            {
                float speedLimit = driveInput > 0f ? maxForwardSpeed : maxReverseSpeed;
                if (Mathf.Abs(speed) < speedLimit)
                    appliedTorque = driveInput * motorTorque;
            }
        }

        ApplyWheelTorque(appliedTorque, appliedBrake);
    }

    private void ApplyWheelTorque(float appliedTorque, float appliedBrake)
    {
        ApplyToWheel(wheelFL, appliedTorque, appliedBrake);
        ApplyToWheel(wheelFR, appliedTorque, appliedBrake);
        ApplyToWheel(wheelRL, appliedTorque, appliedBrake);
        ApplyToWheel(wheelRR, appliedTorque, appliedBrake);
    }

    private static void ApplyToWheel(WheelCollider wheel, float torque, float brake)
    {
        if (wheel == null)
            return;

        wheel.motorTorque = torque;
        wheel.brakeTorque = brake;
    }

    private void ApplyDownforce()
    {
        float appliedDownforce = GetAppliedDownforce();
        if (appliedDownforce <= 0f)
            return;

        rb.AddForce(-transform.up * appliedDownforce * rb.linearVelocity.magnitude, ForceMode.Force);
    }

    private void ApplyExtraGravity()
    {
        float appliedExtraGravity = GetAppliedExtraGravity();
        if (appliedExtraGravity <= 0f)
            return;

        rb.AddForce(Physics.gravity * appliedExtraGravity, ForceMode.Acceleration);
    }

    private void UpdateVisuals(float dt)
    {
        UpdateSteeringVisuals();
        UpdateWheelSpinVisuals(dt);
    }

    private void UpdateSteeringVisuals()
    {
        if (steeringWheelMesh != null && steerAngle > 0.001f)
        {
            float steer01 = Mathf.Clamp(currentSteerAngle / steerAngle, -1f, 1f);
            steeringWheelMesh.localRotation = steeringWheelBaseRotation *
                Quaternion.AngleAxis(steer01 * steeringWheelVisualAngle * steeringWheelVisualSign, Vector3.up);
        }

        float visualSteerAngle = Mathf.Clamp(currentSteerAngle, -maxSteerVisualAngle, maxSteerVisualAngle) * frontSteerVisualSign;

        if (frontLeftSteerTarget != null)
            frontLeftSteerTarget.localEulerAngles = new Vector3(
                frontLeftBaseEuler.x,
                frontLeftBaseEuler.y,
                frontLeftBaseEuler.z + visualSteerAngle);

        if (frontRightSteerTarget != null)
            frontRightSteerTarget.localEulerAngles = new Vector3(
                frontRightBaseEuler.x,
                frontRightBaseEuler.y,
                frontRightBaseEuler.z + visualSteerAngle);
    }

    private void UpdateWheelSpinVisuals(float dt)
    {
        CacheWheelArray();

        for (int i = 0; i < wheelColliders.Length; i++)
        {
            WheelCollider wheel = wheelColliders[i];
            Transform mesh = i < wheelMeshes.Length ? wheelMeshes[i] : null;

            if (wheel == null || mesh == null)
                continue;

            wheelSpinAngles[i] += wheel.rpm * 6f * dt * wheelSpinVisualSign;
            mesh.localRotation = wheelBaseRotations[i] * Quaternion.AngleAxis(wheelSpinAngles[i], Vector3.right);
        }
    }

    private void ConfigureRigidbody()
    {
        rb.centerOfMass = centerOfMassOffset;
    }

    private void ConfigureBodyCollider()
    {
        if (bodyCollider == null)
            return;

        bodyCollider.center = bodyColliderCenter;
        bodyCollider.size = bodyColliderSize;
    }

    private void ConfigurePlayerBlocker()
    {
        if (playerBlocker == null)
            return;

        playerBlocker.center = playerBlockerCenter;
        playerBlocker.size = playerBlockerSize;
        playerBlocker.isTrigger = false;
    }

    private void AlignWheelCollidersToVisuals()
    {
        CacheWheelArray();

        for (int i = 0; i < wheelColliders.Length; i++)
        {
            WheelCollider wheel = wheelColliders[i];
            Transform mesh = (wheelMeshes != null && i < wheelMeshes.Length) ? wheelMeshes[i] : null;

            if (wheel == null || mesh == null)
                continue;

            wheel.transform.position = mesh.position + transform.up * wheelColliderYOffset;
            wheel.transform.rotation = transform.rotation;
            wheel.center = Vector3.zero;
        }
    }

    private void ConfigureWheelColliders()
    {
        CacheWheelArray();

        foreach (WheelCollider wheel in wheelColliders)
        {
            if (wheel == null)
                continue;

            wheel.radius = wheelRadius;
            wheel.suspensionDistance = suspensionDistance;
            wheel.ConfigureVehicleSubsteps(
                wheelSubstepSpeedThreshold,
                wheelSubstepsBelowThreshold,
                wheelSubstepsAboveThreshold);

            WheelFrictionCurve forward = wheel.forwardFriction;
            forward.stiffness = forwardFrictionStiffness;
            wheel.forwardFriction = forward;

            WheelFrictionCurve sideways = wheel.sidewaysFriction;
            sideways.stiffness = sidewaysFrictionStiffness;
            wheel.sidewaysFriction = sideways;
        }

        ApplyRideComfortSettings(force: true);
    }

    private void CacheVisualState()
    {
        steeringWheelBaseRotation = steeringWheelMesh != null
            ? steeringWheelMesh.localRotation
            : Quaternion.identity;

        frontLeftBaseEuler = frontLeftSteerTarget != null
            ? frontLeftSteerTarget.localEulerAngles
            : Vector3.zero;

        frontRightBaseEuler = frontRightSteerTarget != null
            ? frontRightSteerTarget.localEulerAngles
            : Vector3.zero;

        for (int i = 0; i < wheelBaseRotations.Length; i++)
        {
            Transform mesh = (wheelMeshes != null && i < wheelMeshes.Length) ? wheelMeshes[i] : null;
            wheelBaseRotations[i] = mesh != null ? mesh.localRotation : Quaternion.identity;
        }
    }

    private void CacheWheelArray()
    {
        wheelColliders[0] = wheelFL;
        wheelColliders[1] = wheelFR;
        wheelColliders[2] = wheelRL;
        wheelColliders[3] = wheelRR;
    }

    private void AutoResolveReferences()
    {
        if (wheelFL == null) wheelFL = FindWheelCollider("WC_FL");
        if (wheelFR == null) wheelFR = FindWheelCollider("WC_FR");
        if (wheelRL == null) wheelRL = FindWheelCollider("WC_RL");
        if (wheelRR == null) wheelRR = FindWheelCollider("WC_RR");

        if (steeringWheelMesh == null) steeringWheelMesh = FindDeepChild("steering wheel");
        if (frontWheelLeft == null) frontWheelLeft = FindDeepChild("FrontLeft_Pivot");
        if (frontWheelRight == null) frontWheelRight = FindDeepChild("FrontRight_Pivot");

        if (wheelMeshes == null || wheelMeshes.Length != 4)
        {
            wheelMeshes = new[]
            {
                FindDeepChild("Wheel_FrontLeft"),
                FindDeepChild("Wheel_FrontRight"),
                FindDeepChild("Wheel_RearLeft"),
                FindDeepChild("Wheel_RearRight")
            };
        }

        if (seatAnchor == null)
        {
            GameObject seatAnchorObject = new GameObject("SeatAnchor");
            seatAnchorObject.transform.SetParent(transform, false);
            seatAnchorObject.transform.localPosition = seatLocalPosition;
            seatAnchorObject.transform.localRotation = Quaternion.identity;
            seatAnchor = seatAnchorObject.transform;
        }
        else if (seatAnchor.name == "SeatAnchor")
        {
            seatAnchor.localPosition = seatLocalPosition;
            seatAnchor.localRotation = Quaternion.identity;
        }

        if (playerBlocker == null)
        {
            GameObject blockerObject = new GameObject("PlayerBlocker");
            Transform blockerTransform = blockerObject.transform;
            blockerTransform.SetParent(transform, false);
            blockerTransform.localPosition = Vector3.zero;
            blockerTransform.localRotation = Quaternion.identity;
            blockerTransform.localScale = Vector3.one;
            playerBlocker = blockerObject.AddComponent<BoxCollider>();
        }

        if (mountedRigAnchor == null)
        {
            Transform existingAnchor = transform.Find("MountedRigAnchor");
            if (existingAnchor != null)
            {
                mountedRigAnchor = existingAnchor;
            }
            else
            {
                GameObject anchorObject = new GameObject("MountedRigAnchor");
                mountedRigAnchor = anchorObject.transform;
                mountedRigAnchor.SetParent(transform, false);
            }
        }

        if (mountedRigAnchor != null)
        {
            mountedRigAnchor.localScale = Vector3.one;
            UpdateMountedRigAnchor(0f, snapImmediately: true);
        }
    }

    private void PrepareFrontSteeringVisuals()
    {
        frontLeftSteerTarget = PrepareSteerPivot(frontWheelLeft, wheelMeshes != null && wheelMeshes.Length > 0 ? wheelMeshes[0] : null, "FrontLeft");
        frontRightSteerTarget = PrepareSteerPivot(frontWheelRight, wheelMeshes != null && wheelMeshes.Length > 1 ? wheelMeshes[1] : null, "FrontRight");
    }

    private Transform PrepareSteerPivot(Transform sourcePivot, Transform wheelMesh, string sideName)
    {
        if (sourcePivot == null)
            return null;

        Transform existing = sourcePivot.parent != null ? sourcePivot.parent.Find(sideName + "_SteerPivotRuntime") : null;
        if (existing != null)
            return existing;

        if (wheelMesh == null)
            return sourcePivot;

        GameObject runtimePivotObject = new GameObject(sideName + "_SteerPivotRuntime");
        Transform runtimePivot = runtimePivotObject.transform;
        runtimePivot.SetParent(sourcePivot.parent, worldPositionStays: false);
        runtimePivot.position = wheelMesh.position;
        runtimePivot.rotation = sourcePivot.rotation;
        runtimePivot.localScale = sourcePivot.localScale;

        while (sourcePivot.childCount > 0)
        {
            Transform child = sourcePivot.GetChild(0);
            child.SetParent(runtimePivot, worldPositionStays: true);
        }

        return runtimePivot;
    }

    private WheelCollider FindWheelCollider(string objectName)
    {
        Transform child = FindDeepChild(objectName);
        return child != null ? child.GetComponent<WheelCollider>() : null;
    }

    private Transform FindDeepChild(string objectName)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName)
                return child;
        }

        return null;
    }

    private BoxCollider FindChildCollider(string objectName)
    {
        Transform child = FindDeepChild(objectName);
        return child != null ? child.GetComponent<BoxCollider>() : null;
    }

    private void ReadKeyboardFallback()
    {
        float throttle = 0f;
        if (Input.GetKey(KeyCode.W)) throttle += 1f;
        if (Input.GetKey(KeyCode.S)) throttle -= 1f;

        float steer = 0f;
        if (Input.GetKey(KeyCode.A)) steer -= 1f;
        if (Input.GetKey(KeyCode.D)) steer += 1f;

        throttleInput = Mathf.Clamp(throttle, -1f, 1f);
        steerInput = Mathf.Clamp(steer, -1f, 1f);
        brakeInput = Input.GetKey(KeyCode.Space) ? 1f : 0f;
    }

    private void InitializeMounting()
    {
        xrOrigin = FindAnyObjectByType<XROrigin>();
        jetpack = FindAnyObjectByType<AutoJetpackController>();

        if (xrOrigin == null)
            return;

        charController = xrOrigin.GetComponent<CharacterController>();
        foreach (Transform t in xrOrigin.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "Left Controller") leftCtrl = t;
            if (t.name == "Right Controller") rightCtrl = t;
        }

        var providers = new List<MonoBehaviour>();
        foreach (MonoBehaviour comp in xrOrigin.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (comp == null)
                continue;

            string name = comp.GetType().Name;
            if (name.Contains("Turn") || name.Contains("Move") || name.Contains("Locomotion") ||
                name.Contains("Teleport") || name.Contains("Climb") || name.Contains("Grab"))
            {
                providers.Add(comp);
            }
        }

        locomotionProviders = providers.ToArray();
    }

    private void HandleMounting()
    {
        RefreshDevices();
        dismountCooldown -= Time.deltaTime;

        bool leftGrip = GetGrip(leftDevice);
        bool rightGrip = GetGrip(rightDevice);
        bool bothGrips = leftGrip && rightGrip;

        if (!isMounted)
        {
            if (bothGrips && !gripsLastFrame && xrOrigin != null)
            {
                float distance = Vector3.Distance(xrOrigin.transform.position, transform.position);
                if (distance < mountRadius)
                    Mount();
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Dismount();
                gripsLastFrame = bothGrips;
                return;
            }

            if (bothGrips && dismountCooldown <= 0f)
            {
                dismountHoldTimer += Time.deltaTime;
                if (dismountHoldTimer >= dismountHoldDuration)
                {
                    Dismount();
                    gripsLastFrame = bothGrips;
                    return;
                }
            }
            else
            {
                dismountHoldTimer = 0f;
            }
        }

        gripsLastFrame = bothGrips;
    }

    private void Mount()
    {
        if (xrOrigin == null || seatAnchor == null)
            return;

        isMounted = true;
        dismountCooldown = 2f;
        dismountHoldTimer = 0f;
        dismountReadyAfterRelease = false;
        useKeyboardFallback = false;
        SetInput(0f, 0f, 1f);

        if (jetpack != null)
            jetpack.enabled = false;

        AlignRigToSeat();
        UpdateMountedRigAnchor(0f, snapImmediately: true);
        xrOrigin.transform.SetParent(
            useMountedSeatStabilization && mountedRigAnchor != null ? mountedRigAnchor : transform,
            worldPositionStays: true);

        if (locomotionProviders != null)
        {
            foreach (MonoBehaviour provider in locomotionProviders)
            {
                if (provider != null)
                    provider.enabled = false;
            }
        }

        if (charController != null)
            charController.enabled = false;

        if (leftCtrl != null && rightCtrl != null)
        {
            neutralAngle = GetHandAngle();
            currentWheelAngle = 0f;
        }
    }

    private void Dismount()
    {
        isMounted = false;
        dismountHoldTimer = 0f;
        dismountReadyAfterRelease = false;
        useKeyboardFallback = true;
        SetInput(0f, 0f, 1f);

        if (xrOrigin != null)
        {
            xrOrigin.transform.SetParent(null, worldPositionStays: true);
            xrOrigin.transform.position = transform.position
                + transform.right * (2.5f * transform.lossyScale.x)
                + Vector3.up * 0.1f;
        }

        if (locomotionProviders != null)
        {
            foreach (MonoBehaviour provider in locomotionProviders)
            {
                if (provider != null)
                    provider.enabled = true;
            }
        }

        if (charController != null)
            charController.enabled = true;

        if (jetpack != null)
            jetpack.enabled = true;
    }

    private void AlignRigToSeat()
    {
        if (xrOrigin == null)
            return;

        Transform cameraTransform = xrOrigin.Camera != null ? xrOrigin.Camera.transform : null;
        Vector3 desiredPosition = SeatWorldPosition;
        Vector3 desiredForward = Quaternion.Euler(0f, seatYawOffset, 0f) * SeatForward;

        if (cameraTransform == null)
        {
            xrOrigin.transform.position = desiredPosition;
            xrOrigin.transform.rotation = Quaternion.LookRotation(desiredForward, Vector3.up);
            return;
        }

        Vector3 currentForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);
        if (currentForward.sqrMagnitude < 0.0001f)
            currentForward = Vector3.ProjectOnPlane(xrOrigin.transform.forward, Vector3.up);

        float yawDelta = Vector3.SignedAngle(currentForward.normalized, desiredForward.normalized, Vector3.up);
        xrOrigin.transform.RotateAround(cameraTransform.position, Vector3.up, yawDelta);
        xrOrigin.transform.position += desiredPosition - cameraTransform.position;
    }

    private void RefreshDevices()
    {
        if (!leftDevice.isValid)
        {
            List<InputDevice> list = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, list);
            if (list.Count > 0) leftDevice = list[0];
        }

        if (!rightDevice.isValid)
        {
            List<InputDevice> list = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.RightHand, list);
            if (list.Count > 0) rightDevice = list[0];
        }
    }

    private static bool GetGrip(InputDevice device)
    {
        return device.isValid &&
               device.TryGetFeatureValue(CommonUsages.gripButton, out bool value) &&
               value;
    }

    private void ReadMountedVrInput()
    {
        bool leftGrip = GetGrip(leftDevice);
        bool rightGrip = GetGrip(rightDevice);

        float throttle = 0f;
        if (rightGrip && !leftGrip) throttle = 1f;
        else if (leftGrip && !rightGrip) throttle = -1f;

        float steer = ComputeWheelSteering(Time.fixedDeltaTime);
        SetInput(throttle, steer, 0f);
    }

    private float ComputeWheelSteering(float dt)
    {
        if (leftCtrl == null || rightCtrl == null)
        {
            currentWheelAngle = Mathf.Lerp(currentWheelAngle, 0f, 4f * dt);
            return 0f;
        }

        float delta = Mathf.DeltaAngle(neutralAngle, GetHandAngle());
        currentWheelAngle = Mathf.Lerp(currentWheelAngle, delta, 15f * dt);

        float abs = Mathf.Abs(delta);
        if (abs < steerDeadzone)
            return 0f;

        float effective = delta > 0f ? abs - steerDeadzone : -(abs - steerDeadzone);
        float normalized = Mathf.Clamp(effective / Mathf.Max(steerMaxAngle - steerDeadzone, 1f), -1f, 1f);
        return Mathf.Sign(normalized) * Mathf.Sqrt(Mathf.Abs(normalized));
    }

    private float GetHandAngle()
    {
        Vector3 dir = rightCtrl.position - leftCtrl.position;
        Vector3 local = transform.InverseTransformDirection(dir);
        local.y = 0f;

        if (local.sqrMagnitude < 0.0001f)
            return neutralAngle;

        return Mathf.Atan2(local.z, local.x) * Mathf.Rad2Deg;
    }

    private void ApplyRideComfortSettings(bool force = false)
    {
        bool mountedState = useMountedRideComfort && isMounted;
        if (!force && mountedState == lastRideComfortMountedState)
            return;

        float springMultiplier = mountedState ? mountedSuspensionSpringMultiplier : 1f;
        float damperMultiplier = mountedState ? mountedSuspensionDamperMultiplier : 1f;
        float wheelDampingMultiplier = mountedState ? mountedWheelDampingMultiplier : 1f;

        JointSpring spring = new JointSpring
        {
            spring = suspensionSpring * springMultiplier,
            damper = suspensionDamper * damperMultiplier,
            targetPosition = suspensionTargetPosition
        };

        CacheWheelArray();
        foreach (WheelCollider wheel in wheelColliders)
        {
            if (wheel == null)
                continue;

            wheel.suspensionSpring = spring;
            wheel.wheelDampingRate = wheelDampingRate * wheelDampingMultiplier;
        }

        lastRideComfortMountedState = mountedState;
    }

    private float GetAppliedDownforce()
    {
        return downforce * (useMountedRideComfort && isMounted ? mountedDownforceMultiplier : 1f);
    }

    private float GetAppliedExtraGravity()
    {
        return extraGravity * (useMountedRideComfort && isMounted ? mountedExtraGravityMultiplier : 1f);
    }

    private void UpdateMountedRigAnchor(float deltaTime, bool snapImmediately = false)
    {
        if (mountedRigAnchor == null)
            return;

        Vector3 targetPosition = SeatWorldPosition;
        Vector3 targetForward = Quaternion.Euler(0f, seatYawOffset, 0f) * SeatForward;
        if (targetForward.sqrMagnitude < 0.0001f)
            targetForward = transform.forward;

        Quaternion targetRotation = Quaternion.LookRotation(targetForward.normalized, Vector3.up);

        if (!useMountedSeatStabilization || !isMounted || snapImmediately || deltaTime <= 0f)
        {
            mountedRigAnchor.SetPositionAndRotation(targetPosition, targetRotation);
            return;
        }

        float horizontalLerp = 1f - Mathf.Exp(-Mathf.Max(0f, mountedSeatPositionSmooth) * deltaTime);
        float verticalLerp = 1f - Mathf.Exp(-Mathf.Max(0f, mountedSeatVerticalSmooth) * deltaTime);
        float rotationLerp = 1f - Mathf.Exp(-Mathf.Max(0f, mountedSeatYawSmooth) * deltaTime);

        Vector3 smoothedPosition = mountedRigAnchor.position;
        smoothedPosition.x = Mathf.Lerp(smoothedPosition.x, targetPosition.x, horizontalLerp);
        smoothedPosition.z = Mathf.Lerp(smoothedPosition.z, targetPosition.z, horizontalLerp);
        smoothedPosition.y = Mathf.Lerp(smoothedPosition.y, targetPosition.y, verticalLerp);
        mountedRigAnchor.position = smoothedPosition;
        mountedRigAnchor.rotation = Quaternion.Slerp(mountedRigAnchor.rotation, targetRotation, rotationLerp);
    }
}
