using UnityEngine;
using UnityEngine.XR;
using Unity.XR.CoreUtils;
using System.Collections.Generic;

/// VR Rover — kinematic movement + ComputePenetration wall correction.
[DefaultExecutionOrder(-50)]
public class RoverDriver : MonoBehaviour
{
    [Header("Seat")]
    public Transform seatAnchor;
    public float seatHeightBoost = 0.8f;

    [Header("Steering Wheel Visual")]
    public Transform steeringWheelMesh;
    private Quaternion wheelBaseRotation;

    [Header("Front Wheel Steering Visual")]
    public Transform frontWheelLeft;
    public Transform frontWheelRight;
    public Transform frontAxle; // Axle_Front — used as rotation reference for steeringght;
    public float maxSteerVisualAngle = 30f;

    [Header("Wheel Meshes (visual spin)")]
    public Transform[] wheelMeshes;
    public float wheelRadius = 0.35f;

    [Header("Movement")]
    public float maxForwardSpeed = 10f;
    public float maxReverseSpeed = 4f;
    public float acceleration    = 6f;
    public float brakingForce    = 8f;
    public float turnRate        = 35f;

    [Header("Steering")]
    public float steerDeadzone = 8f;
    public float steerMaxAngle = 150f;

    [Header("Slope Climbing")]
    public float stepHeight = 0.6f;

    [Header("Mount")]
    public float mountRadius = 4f;

    [Header("Haptics")]
    public float idleRumble = 0.05f;
    public float maxRumble  = 0.25f;

    // --- runtime refs ---
    private BoxCollider           boxCol;
    private XROrigin              xrOrigin;
    private AutoJetpackController jetpack;
    private CharacterController   charController;
    private Transform             leftCtrl;
    private Transform             rightCtrl;
    private InputDevice           leftDevice;
    private InputDevice           rightDevice;
    private MonoBehaviour[]       locomotionProviders;

    // --- state ---
    public  bool  IsMounted         => isMounted;
    private bool  isMounted         = false;
    private bool  gripsLastFrame    = false;
    private float currentSpeed      = 0f;
    private float neutralAngle      = 0f;
    private float currentWheelAngle = 0f;
    private float smoothedSteer     = 0f;
    private float dismountCooldown  = 0f;
    private float rumbleTimer       = 0f;
    private float dismountHoldTimer = 0f;
    private float verticalVelocity  = 0f;

    // Cached pivot offsets from rover root (set at mount time)
    private Vector3 pivotLeftLocalPos;
    private Vector3 pivotRightLocalPos;
    private bool    pivotOffsetsSet = false;
    private Transform frontLeftSteerTarget;
    private Transform frontRightSteerTarget;
    private Transform frontSteerGroup;
    private Quaternion frontLeftSteerBaseRotation = Quaternion.identity;
    private Quaternion frontRightSteerBaseRotation = Quaternion.identity;
    private Quaternion frontSteerGroupBaseRotation = Quaternion.identity;
    private Vector3 frontLeftSteerBaseEuler;
    private Vector3 frontRightSteerBaseEuler;

    // --- collision ---
    private int     wallMask;
    private Vector3 castHalfExt;
    private Vector3 castCenter;

    void Start()
    {
        boxCol = GetComponent<BoxCollider>();
        wallMask = ~((1 << 13) | (1 << 2) | (1 << 8));

        if (boxCol != null)
        {
            castHalfExt = new Vector3(
                boxCol.size.x * transform.lossyScale.x * 0.5f,
                boxCol.size.y * transform.lossyScale.y * 0.5f,
                boxCol.size.z * transform.lossyScale.z * 0.5f);
            castCenter = boxCol.center;
        }
        else
        {
            castHalfExt = new Vector3(0.96f, 0.54f, 1.32f);
            castCenter  = new Vector3(0f, 0.45f, 0.1f);
        }

        xrOrigin = FindAnyObjectByType<XROrigin>();
        jetpack  = FindAnyObjectByType<AutoJetpackController>();

        if (xrOrigin != null)
        {
            charController = xrOrigin.GetComponent<CharacterController>();
            foreach (Transform t in xrOrigin.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Left Controller")  leftCtrl  = t;
                if (t.name == "Right Controller") rightCtrl = t;
            }
            var providers = new List<MonoBehaviour>();
            foreach (var comp in xrOrigin.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (comp == null) continue;
                string n = comp.GetType().Name;
                if (n.Contains("Turn") || n.Contains("Move") || n.Contains("Locomotion") ||
                    n.Contains("Teleport") || n.Contains("Climb") || n.Contains("Grab"))
                    providers.Add(comp);
            }
            locomotionProviders = providers.ToArray();
        }

        if (steeringWheelMesh == null)
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
                if (t.name.ToLower().Contains("steering"))
                    { steeringWheelMesh = t; break; }

        if (steeringWheelMesh != null)
            wheelBaseRotation = steeringWheelMesh.localRotation;

        if (wheelMeshes == null || wheelMeshes.Length == 0)
        {
            var found = new List<Transform>();
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLower();
                if (n.StartsWith("cylinder_") && n.Contains(".") && t.localPosition.magnitude > 0.1f)
                    found.Add(t);
            }
            wheelMeshes = found.ToArray();
        }

        if (seatAnchor == null)
        {
            var go = new GameObject("SeatAnchor");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(-0.3f, 0.1f, 0.1f);
            seatAnchor = go.transform;
        }

        InitializeFrontSteeringTargets();

        // Cache pivot local positions relative to rover root at start
        CachePivotOffsets();

        GroundSnap();
    }

    void CachePivotOffsets()
    {
        if (frontWheelLeft != null)
        {
            // Get bounds center of the wheel mesh in local space of rover root
            Renderer rL = frontWheelLeft.GetComponentInChildren<Renderer>();
            if (rL != null)
                pivotLeftLocalPos = transform.InverseTransformPoint(rL.bounds.center);
            else
                pivotLeftLocalPos = transform.InverseTransformPoint(frontWheelLeft.position);
        }
        if (frontWheelRight != null)
        {
            Renderer rR = frontWheelRight.GetComponentInChildren<Renderer>();
            if (rR != null)
                pivotRightLocalPos = transform.InverseTransformPoint(rR.bounds.center);
            else
                pivotRightLocalPos = transform.InverseTransformPoint(frontWheelRight.position);
        }
        pivotOffsetsSet = true;
    }

    void InitializeFrontSteeringTargets()
    {
        frontSteerGroup = null;
        frontSteerGroupBaseRotation = Quaternion.identity;

        frontLeftSteerTarget = PrepareSteeringTarget(frontWheelLeft, "FrontLeft");
        frontRightSteerTarget = PrepareSteeringTarget(frontWheelRight, "FrontRight");

        if (frontLeftSteerTarget != null)
        {
            frontLeftSteerBaseRotation = frontLeftSteerTarget.localRotation;
            frontLeftSteerBaseEuler = frontLeftSteerTarget.localEulerAngles;
        }
        if (frontRightSteerTarget != null)
        {
            frontRightSteerBaseRotation = frontRightSteerTarget.localRotation;
            frontRightSteerBaseEuler = frontRightSteerTarget.localEulerAngles;
        }
    }

    void AttachSteerTargetToAxle(Transform steerTarget)
    {
        if (steerTarget == null || frontAxle == null) return;
        if (steerTarget.parent == frontAxle) return;

        steerTarget.SetParent(frontAxle, true);
    }

    Transform TryCreateSharedFrontSteerGroup()
    {
        if (frontWheelLeft == null || frontWheelRight == null) return null;
        if (frontWheelLeft.parent == null || frontWheelLeft.parent != frontWheelRight.parent) return null;

        Transform commonParent = frontWheelLeft.parent;
        if ((frontWheelLeft.localPosition - frontWheelRight.localPosition).sqrMagnitude > 0.000001f)
            return null;

        foreach (Transform child in commonParent)
        {
            if (child != null && child.name == "FrontSteerGroupRuntime")
                return child;
        }

        var steerGroup = new GameObject("FrontSteerGroupRuntime").transform;
        steerGroup.SetParent(commonParent, false);
        steerGroup.localPosition = frontAxle != null
            ? commonParent.InverseTransformPoint(frontAxle.position)
            : frontWheelLeft.localPosition;
        steerGroup.localRotation = Quaternion.identity;
        steerGroup.localScale = Vector3.one;

        ReparentToSteerGroup(frontWheelLeft, steerGroup);
        ReparentToSteerGroup(frontWheelRight, steerGroup);

        return steerGroup;
    }

    void ReparentToSteerGroup(Transform source, Transform steerGroup)
    {
        if (source == null || steerGroup == null) return;
        source.SetParent(steerGroup, true);
    }

    Transform PrepareSteeringTarget(Transform assignedTarget, string sideLabel)
    {
        if (assignedTarget == null) return null;

        Transform existingRuntimePivot = FindExistingRuntimeSteeringPivot(sideLabel);
        if (existingRuntimePivot != null)
            return existingRuntimePivot;

        return CreateRuntimeSteeringPivot(assignedTarget, sideLabel);
    }

    Transform FindExistingRuntimeSteeringPivot(string sideLabel)
    {
        Transform searchRoot = frontAxle != null ? frontAxle : transform;
        if (searchRoot == null) return null;

        string runtimePivotName = sideLabel + "_SteerPivotRuntime";
        foreach (Transform child in searchRoot)
        {
            if (child != null && child.name == runtimePivotName)
                return child;
        }

        return null;
    }

    bool IsTrackedWheelMesh(Transform candidate)
    {
        if (candidate == null || wheelMeshes == null) return false;

        foreach (Transform wheel in wheelMeshes)
        {
            if (wheel == candidate)
                return true;
        }

        return false;
    }

    Transform CreateRuntimeSteeringPivot(Transform wheelVisual, string sideLabel)
    {
        if (wheelVisual == null) return null;
        if (wheelVisual.parent != null && wheelVisual.parent.name == sideLabel + "_SteerPivotRuntime")
            return wheelVisual.parent;

        Transform originalParent = wheelVisual.parent;
        Transform pivotParent = frontAxle != null ? frontAxle : originalParent;
        if (pivotParent == null)
            return wheelVisual;

        Vector3 pivotWorldPosition = GetSteeringPivotWorldPosition(wheelVisual);

        var pivot = new GameObject(sideLabel + "_SteerPivotRuntime").transform;
        pivot.SetParent(pivotParent, false);
        pivot.position = pivotWorldPosition;
        pivot.rotation = pivotParent.rotation;
        pivot.localScale = Vector3.one;

        if (!IsTrackedWheelMesh(wheelVisual) && wheelVisual.childCount > 0)
        {
            var children = new List<Transform>();
            foreach (Transform child in wheelVisual)
                children.Add(child);

            foreach (Transform child in children)
                child.SetParent(pivot, true);
        }
        else
        {
            wheelVisual.SetParent(pivot, true);
        }

        return pivot;
    }

    Vector3 GetSteeringPivotWorldPosition(Transform steerRoot)
    {
        Transform wheelVisual = FindWheelVisualForSteerRoot(steerRoot);
        if (wheelVisual != null)
        {
            Renderer wheelRenderer = wheelVisual.GetComponent<Renderer>();
            if (wheelRenderer != null)
                return wheelRenderer.bounds.center;

            return wheelVisual.position;
        }

        Renderer anyRenderer = steerRoot.GetComponentInChildren<Renderer>();
        if (anyRenderer != null)
            return anyRenderer.bounds.center;

        return steerRoot.position;
    }

    Transform FindWheelVisualForSteerRoot(Transform steerRoot)
    {
        if (steerRoot == null) return null;
        if (IsTrackedWheelMesh(steerRoot)) return steerRoot;

        if (wheelMeshes != null)
        {
            foreach (Transform wheel in wheelMeshes)
            {
                if (wheel != null && wheel.IsChildOf(steerRoot))
                    return wheel;
            }
        }

        foreach (Transform child in steerRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child != null && child.name.ToLowerInvariant().Contains("wheel"))
                return child;
        }

        return null;
    }

    void Update()
    {
        RefreshDevices();
        dismountCooldown -= Time.deltaTime;

        bool leftGrip  = GetGrip(leftDevice);
        bool rightGrip = GetGrip(rightDevice);
        bool bothGrips = leftGrip && rightGrip;

        if (!isMounted)
        {
            GroundSnap();
            if (bothGrips && !gripsLastFrame && xrOrigin != null)
            {
                float dist = Vector3.Distance(xrOrigin.transform.position, transform.position);
                if (dist < mountRadius) Mount();
            }
        }
        else
        {
            // Dismount: must release both grips then re-hold for 1.5s
            if (bothGrips && !gripsLastFrame && dismountCooldown <= 0f)
            {
                dismountHoldTimer += Time.deltaTime;
                if (dismountHoldTimer >= 1.5f)
                {
                    dismountHoldTimer = 0f;
                    Dismount();
                    gripsLastFrame = bothGrips;
                    return;
                }
            }
            else if (!bothGrips) { dismountHoldTimer = 0f; }

            float throttle = 0f;
            if (rightGrip && !leftGrip)      throttle =  1f;
            else if (leftGrip && !rightGrip) throttle = -1f;

            float steer = ComputeWheelSteering(Time.deltaTime);
            Drive(throttle, steer, Time.deltaTime);
            ClimbStep();
            GroundSnap();
            Depenetrate();
            SpinWheels();
            UpdateHaptics(rightGrip);
        }

        gripsLastFrame = bothGrips;
    }

    // ── Steering ─────────────────────────────────────────────────────────────

    float ComputeWheelSteering(float dt)
    {
        if (leftCtrl == null || rightCtrl == null)
        {
            currentWheelAngle = Mathf.Lerp(currentWheelAngle, 0f, 4f * dt);
            UpdateWheelVisual(currentWheelAngle);
            return 0f;
        }

        float delta = Mathf.DeltaAngle(neutralAngle, GetHandAngle());
        currentWheelAngle = Mathf.Lerp(currentWheelAngle, delta, 15f * dt);
        UpdateWheelVisual(currentWheelAngle);

        float abs = Mathf.Abs(delta);
        if (abs < steerDeadzone) return 0f;
        float effective  = delta > 0f ? abs - steerDeadzone : -(abs - steerDeadzone);
        float normalized = Mathf.Clamp(effective / (steerMaxAngle - steerDeadzone), -1f, 1f);
        return Mathf.Sign(normalized) * Mathf.Sqrt(Mathf.Abs(normalized));
    }

    float GetHandAngle()
    {
        Vector3 dir   = rightCtrl.position - leftCtrl.position;
        Vector3 local = transform.InverseTransformDirection(dir);
        local.y = 0f;
        if (local.sqrMagnitude < 0.0001f) return neutralAngle;
        return Mathf.Atan2(local.z, local.x) * Mathf.Rad2Deg;
    }

    void UpdateWheelVisual(float angleDeg)
    {
        if (steeringWheelMesh == null) return;
        steeringWheelMesh.localRotation = wheelBaseRotation *
            Quaternion.AngleAxis(angleDeg, Vector3.up);
    }

    // ── Driving ───────────────────────────────────────────────────────────────

    void Drive(float throttle, float steer, float dt)
    {
        smoothedSteer = Mathf.Lerp(smoothedSteer, steer, 2f * dt);

        if (throttle > 0.05f)
            currentSpeed = Mathf.MoveTowards(currentSpeed,  maxForwardSpeed, acceleration * dt);
        else if (throttle < -0.05f)
            currentSpeed = Mathf.MoveTowards(currentSpeed, -maxReverseSpeed, acceleration * dt);
        else
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakingForce * dt);

        float speedFactor   = 1f - (Mathf.Abs(currentSpeed) / maxForwardSpeed) * 0.5f;
        float effectiveTurn = turnRate * speedFactor;
        if (Mathf.Abs(currentSpeed) > 0.1f && Mathf.Abs(smoothedSteer) > 0.02f)
            transform.Rotate(0f, smoothedSteer * effectiveTurn * Mathf.Sign(currentSpeed) * dt, 0f, Space.World);

        if (Mathf.Abs(currentSpeed) < 0.001f) return;

        Vector3 moveDir  = transform.forward * Mathf.Sign(currentSpeed);
        float   moveDist = Mathf.Abs(currentSpeed) * dt;
        transform.position += moveDir * moveDist;
    }

    // ── Step Climbing ─────────────────────────────────────────────────────────

    void ClimbStep()
    {
        if (Mathf.Abs(currentSpeed) < 0.01f) return;

        Vector3 moveDir    = transform.forward * Mathf.Sign(currentSpeed);
        float   checkDist  = 0.6f;
        int     groundMask = ~((1 << 13) | (1 << 2));

        Vector3 lowOrigin  = transform.position + Vector3.up * (stepHeight * 0.5f);
        bool    hitLow     = Physics.Raycast(lowOrigin, moveDir, checkDist, groundMask, QueryTriggerInteraction.Ignore);
        if (!hitLow) return;

        Vector3 highOrigin = transform.position + Vector3.up * (stepHeight * 1.8f);
        bool    hitHigh    = Physics.Raycast(highOrigin, moveDir, checkDist, groundMask, QueryTriggerInteraction.Ignore);
        if (hitHigh) return;

        transform.position += Vector3.up * stepHeight * 0.4f * Time.deltaTime * 60f;
    }

    // ── Depenetration ─────────────────────────────────────────────────────────

    void Depenetrate()
    {
        if (boxCol == null) return;

        Vector3 worldCenter = transform.TransformPoint(castCenter);

        Collider[] hits = Physics.OverlapBox(
            worldCenter, castHalfExt, transform.rotation,
            wallMask, QueryTriggerInteraction.Ignore);

        foreach (var col in hits)
        {
            if (col == boxCol) continue;
            if (col.transform == transform) continue;
            if (col.transform.IsChildOf(transform)) continue;

            if (Physics.ComputePenetration(
                    boxCol, transform.position, transform.rotation,
                    col,    col.transform.position, col.transform.rotation,
                    out Vector3 dir, out float dist))
            {
                if (dir.y > 0.15f) continue;
                transform.position += dir * (dist + 0.001f);
                float into = Vector3.Dot(transform.forward * currentSpeed, -dir);
                if (into > 0f && dir.y < 0.1f) currentSpeed = 0f;
            }
        }
    }

    // ── Wheels ────────────────────────────────────────────────────────────────

    void SpinWheels()
    {
        // Spin wheel meshes on X axis
        float spin = (currentSpeed / Mathf.Max(wheelRadius, 0.01f)) * Mathf.Rad2Deg * Time.deltaTime;
        foreach (Transform w in wheelMeshes)
            if (w != null) w.Rotate(Vector3.right, spin, Space.Self);
        // Pivot X and Y stay at 0 (pivot itself has no baked rotation)
        // Only Z changes with steering — wheel mesh children have the baked -89.98 X
        float targetAngle = smoothedSteer * maxSteerVisualAngle;
        ApplySteerVisual(frontLeftSteerTarget, frontLeftSteerBaseEuler, targetAngle);
        ApplySteerVisual(frontRightSteerTarget, frontRightSteerBaseEuler, targetAngle);
    }

    void ApplySteerVisual(Transform steerTarget, Vector3 baseEuler, float targetAngle)
    {
        if (steerTarget == null) return;
        steerTarget.localEulerAngles = new Vector3(baseEuler.x, baseEuler.y, baseEuler.z + targetAngle);
    }

    // ── Haptics ───────────────────────────────────────────────────────────────

    void UpdateHaptics(bool gasHeld)
    {
        if (!gasHeld || Mathf.Abs(currentSpeed) < 0.2f)
        {
            SendHaptic(leftDevice,  0f, 0f);
            SendHaptic(rightDevice, 0f, 0f);
            rumbleTimer = 0f;
            return;
        }
        rumbleTimer -= Time.deltaTime;
        if (rumbleTimer > 0f) return;
        rumbleTimer = 0.08f;
        float intensity = Mathf.Lerp(idleRumble, maxRumble, Mathf.Abs(currentSpeed) / maxForwardSpeed);
        SendHaptic(leftDevice,  intensity, 0.08f);
        SendHaptic(rightDevice, intensity, 0.08f);
    }

    void SendHaptic(InputDevice d, float amplitude, float duration)
    {
        if (d.isValid) d.SendHapticImpulse(0, amplitude, duration);
    }

    // ── Mount / Dismount ──────────────────────────────────────────────────────

    void Mount()
    {
        if (jetpack != null) jetpack.enabled = false;

        isMounted        = true;
        dismountCooldown = 2f;
        currentSpeed     = 0f;
        Debug.Log("[RoverDriver] Mounted!");

        if (jetpack != null)
        {
            jetpack.enabled = false;
            var flyingField = typeof(AutoJetpackController).GetField("isFlying",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (flyingField != null) flyingField.SetValue(jetpack, false);
        }

        if (xrOrigin != null)
        {
            xrOrigin.transform.position = seatAnchor.position;
            xrOrigin.transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            xrOrigin.transform.SetParent(transform, worldPositionStays: true);
        }

        if (leftCtrl != null && rightCtrl != null)
        {
            neutralAngle      = GetHandAngle();
            currentWheelAngle = 0f;
        }

        if (locomotionProviders != null)
            foreach (var p in locomotionProviders) if (p != null) p.enabled = false;
        if (charController != null) charController.enabled = false;
    }

    void Dismount()
    {
        isMounted         = false;
        currentSpeed      = 0f;
        dismountHoldTimer = 0f;
        Debug.Log("[RoverDriver] Dismounted!");

        SendHaptic(leftDevice,  0f, 0f);
        SendHaptic(rightDevice, 0f, 0f);

        if (xrOrigin != null)
        {
            xrOrigin.transform.SetParent(null, worldPositionStays: true);
            xrOrigin.transform.position = transform.position
                + transform.right * (2.5f * transform.lossyScale.x)
                + Vector3.up * 0.1f;
        }

        if (locomotionProviders != null)
            foreach (var p in locomotionProviders) if (p != null) p.enabled = true;
        if (charController != null) charController.enabled = true;

        if (jetpack != null)
        {
            jetpack.enabled = true;
            var field = typeof(AutoJetpackController).GetField("postDismountCooldown",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field != null) field.SetValue(jetpack, 1.0f);
        }
    }

    // ── XR helpers ────────────────────────────────────────────────────────────

    void RefreshDevices()
    {
        if (!leftDevice.isValid)
        {
            var list = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, list);
            if (list.Count > 0) leftDevice = list[0];
        }
        if (!rightDevice.isValid)
        {
            var list = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.RightHand, list);
            if (list.Count > 0) rightDevice = list[0];
        }
    }

    bool GetGrip(InputDevice d)
    {
        if (!d.isValid) return false;
        d.TryGetFeatureValue(CommonUsages.gripButton, out bool v);
        return v;
    }

    bool GetButton(InputDevice d, InputFeatureUsage<bool> usage)
    {
        if (!d.isValid) return false;
        d.TryGetFeatureValue(usage, out bool v);
        return v;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, mountRadius);
        if (seatAnchor != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(seatAnchor.position, Vector3.one * 0.3f);
        }
        // Draw cached pivot positions
        if (Application.isPlaying && pivotOffsetsSet)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.TransformPoint(pivotLeftLocalPos),  0.1f);
            Gizmos.DrawWireSphere(transform.TransformPoint(pivotRightLocalPos), 0.1f);
        }
    }

    void GroundSnap()
    {
        int groundMask = ~((1 << 13) | (1 << 2));
        Vector3 origin = transform.position + Vector3.up * 8f;

        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 20f,
            groundMask, QueryTriggerInteraction.Ignore);

        RaycastHit best  = default;
        float bestDist   = float.MaxValue;
        bool  found      = false;

        foreach (var h in hits)
        {
            if (h.transform == transform) continue;
            if (h.transform.IsChildOf(transform)) continue;
            if (h.normal.y < 0.4f) continue;
            if (h.distance < bestDist)
            {
                bestDist = h.distance;
                best     = h;
                found    = true;
            }
        }

        if (found)
        {
            float targetY = best.point.y;
            float diff    = targetY - transform.position.y;

            if (diff > -2f)
            {
                transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
                verticalVelocity   = 0f;
            }
            else
            {
                verticalVelocity = Mathf.Max(verticalVelocity + Physics.gravity.y * Time.deltaTime, -20f);
                float newY = transform.position.y + verticalVelocity * Time.deltaTime;
                if (newY <= targetY) { newY = targetY; verticalVelocity = 0f; }
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            }

            // Keep rover upright — baked 270deg mesh rotation makes any tilt catastrophic
            float yaw = transform.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
        else
        {
            verticalVelocity    = Mathf.Max(verticalVelocity + Physics.gravity.y * Time.deltaTime, -20f);
            transform.position += Vector3.up * verticalVelocity * Time.deltaTime;
        }
    }
}
