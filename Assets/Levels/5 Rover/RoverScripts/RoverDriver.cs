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
    public float initialDriveSpeed = 2.5f;
    public float timeToFullSpeed   = 1.75f;

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
    public float steeringRumble = 0.07f;
    public float roughTerrainRumble = 0.05f;
    public float impactRumble = 0.35f;
    public float impactDuration = 0.12f;

    // --- runtime refs ---
    private BoxCollider           boxCol;
    private XROrigin              xrOrigin;
    private AutoJetpackController jetpack;
    private FlightSmokeTrail      flightSmokeTrail;
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
    private float impactCooldown    = 0f;
    private float dismountHoldTimer = 0f;
    private float verticalVelocity  = 0f;
    private Vector3 currentGroundNormal = Vector3.up;
    private float driveHoldTimer = 0f;
    private float lastDriveSign = 0f;
    private bool hasGroundSupport = true;

    // Cached pivot offsets from rover root (set at mount time)
    private Vector3 pivotLeftLocalPos;
    private Vector3 pivotRightLocalPos;
    private Vector3 rearLeftLocalPos;
    private Vector3 rearRightLocalPos;
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

    void ConfigureCollisionBody()
    {
        if (boxCol == null)
        {
            castHalfExt = new Vector3(0.96f, 0.54f, 1.32f);
            castCenter  = new Vector3(0f, 0.45f, 0.1f);
            return;
        }

        Vector3 originalSize = boxCol.size;
        Vector3 originalCenter = boxCol.center;

        // Use a shallower chassis collider so ramps contact the wheel probes before the body acts like a wall.
        boxCol.center = originalCenter + new Vector3(0f, originalSize.y * 0.22f, 0f);
        boxCol.size = new Vector3(
            originalSize.x * 0.92f,
            originalSize.y * 0.58f,
            originalSize.z * 0.82f);

        castHalfExt = new Vector3(
            boxCol.size.x * transform.lossyScale.x * 0.5f,
            boxCol.size.y * transform.lossyScale.y * 0.5f,
            boxCol.size.z * transform.lossyScale.z * 0.5f);
        castCenter = boxCol.center;
    }

    void Start()
    {
        boxCol = GetComponent<BoxCollider>();
        wallMask = ~((1 << 13) | (1 << 2) | (1 << 8));
        ConfigureCollisionBody();

        xrOrigin = FindAnyObjectByType<XROrigin>();
        jetpack  = FindAnyObjectByType<AutoJetpackController>();
        flightSmokeTrail = FindAnyObjectByType<FlightSmokeTrail>();

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
            pivotLeftLocalPos = GetProbeLocalPosition(frontWheelLeft);
        }
        if (frontWheelRight != null)
        {
            pivotRightLocalPos = GetProbeLocalPosition(frontWheelRight);
        }
        pivotOffsetsSet = true;

        Transform rearLeft = FindWheelMesh("rearleft");
        if (rearLeft != null)
            rearLeftLocalPos = GetProbeLocalPosition(rearLeft);

        Transform rearRight = FindWheelMesh("rearright");
        if (rearRight != null)
            rearRightLocalPos = GetProbeLocalPosition(rearRight);
    }

    Vector3 GetProbeLocalPosition(Transform target)
    {
        if (target == null)
            return Vector3.zero;

        Renderer targetRenderer = target.GetComponentInChildren<Renderer>();
        if (targetRenderer != null)
            return transform.InverseTransformPoint(targetRenderer.bounds.center);

        return transform.InverseTransformPoint(target.position);
    }

    Transform FindWheelMesh(string compactName)
    {
        if (wheelMeshes == null)
            return null;

        foreach (Transform wheel in wheelMeshes)
        {
            if (wheel == null)
                continue;

            string normalizedName = wheel.name.ToLower().Replace("_", "").Replace(" ", "");
            if (normalizedName.Contains(compactName))
                return wheel;
        }

        return null;
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
        impactCooldown = Mathf.Max(0f, impactCooldown - Time.deltaTime);

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
            UpdateHaptics(throttle, steer);
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

        float throttleSign = Mathf.Abs(throttle) > 0.05f ? Mathf.Sign(throttle) : 0f;
        if (throttleSign != 0f)
        {
            if (!Mathf.Approximately(throttleSign, lastDriveSign))
                driveHoldTimer = 0f;

            driveHoldTimer += dt;
            lastDriveSign = throttleSign;

            float hold01 = timeToFullSpeed <= 0.01f
                ? 1f
                : Mathf.Clamp01(driveHoldTimer / timeToFullSpeed);

            float launchSpeed = throttleSign > 0f
                ? Mathf.Min(initialDriveSpeed, maxForwardSpeed)
                : Mathf.Min(initialDriveSpeed, maxReverseSpeed);

            float topSpeed = throttleSign > 0f ? maxForwardSpeed : maxReverseSpeed;
            float targetAbsSpeed = Mathf.Lerp(launchSpeed, topSpeed, hold01);
            float targetSpeed = targetAbsSpeed * throttleSign;

            if (Mathf.Sign(currentSpeed) == throttleSign && Mathf.Abs(currentSpeed) > targetAbsSpeed)
                targetSpeed = currentSpeed;

            float appliedAcceleration = acceleration * (hasGroundSupport ? 1f : 0.35f);
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, appliedAcceleration * dt);
        }
        else
        {
            driveHoldTimer = 0f;
            lastDriveSign = 0f;

            if (hasGroundSupport)
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakingForce * dt);
        }

        float speedFactor   = 1f - (Mathf.Abs(currentSpeed) / maxForwardSpeed) * 0.5f;
        float effectiveTurn = turnRate * speedFactor;
        if (Mathf.Abs(currentSpeed) > 0.1f && Mathf.Abs(smoothedSteer) > 0.02f)
            transform.Rotate(0f, smoothedSteer * effectiveTurn * Mathf.Sign(currentSpeed) * dt, 0f, Space.World);

        if (Mathf.Abs(currentSpeed) < 0.001f) return;

        Vector3 moveDir = transform.forward * Mathf.Sign(currentSpeed);
        Vector3 slopeProbeDir = Vector3.ProjectOnPlane(moveDir, currentGroundNormal);
        if (slopeProbeDir.sqrMagnitude < 0.0001f)
            slopeProbeDir = moveDir;
        else
            slopeProbeDir.Normalize();

        float moveDist = Mathf.Abs(currentSpeed) * dt;
        float climbLift = 0f;

        if (TryGetFrontGroundTargetY(slopeProbeDir, castHalfExt.z + moveDist + 0.25f, out float targetY))
        {
            float rise = targetY - transform.position.y;
            if (rise > 0.02f && rise <= stepHeight)
            {
                climbLift = rise;
                transform.position += Vector3.up * climbLift;
            }
        }

        MoveWithCollisionResponse(moveDir, moveDist, climbLift);
    }

    void MoveWithCollisionResponse(Vector3 moveDir, float moveDist, float climbLift)
    {
        if (moveDist <= 0.0001f)
            return;

        if (!TryGetBlockingHit(moveDir, moveDist + 0.05f, transform.position, out RaycastHit hit))
        {
            transform.position += moveDir * moveDist;
            return;
        }

        if (climbLift > 0.0001f)
        {
            Vector3 liftedPosition = transform.position + Vector3.up * climbLift;
            if (!TryGetBlockingHit(moveDir, moveDist + 0.05f, liftedPosition, out _))
            {
                transform.position = liftedPosition + moveDir * moveDist;
                currentSpeed *= 0.95f;
                return;
            }
        }

        float safeDist = Mathf.Max(0f, hit.distance - 0.05f);
        if (safeDist > 0.0001f)
            transform.position += moveDir * safeDist;

        float remainingDist = Mathf.Max(0f, moveDist - safeDist);
        Vector3 slideDir = Vector3.ProjectOnPlane(moveDir, hit.normal).normalized;

        if (remainingDist > 0.0001f && slideDir.sqrMagnitude > 0.0001f && hit.normal.y < 0.35f)
            transform.position += slideDir * (remainingDist * 0.5f);

        if (Vector3.Dot(moveDir, -hit.normal) > 0.2f)
        {
            currentSpeed *= hit.normal.y > 0.2f ? 0.85f : 0.6f;
            TriggerImpactHaptics(hit.normal.y > 0.2f ? 0.55f : 1f);
        }
    }

    bool TryGetBlockingHit(Vector3 moveDir, float castDistance, Vector3 referencePosition, out RaycastHit bestHit)
    {
        bestHit = default;
        if (boxCol == null)
            return false;

        Vector3 blockingCastCenter = castCenter + Vector3.up * (castHalfExt.y * 0.35f);
        Vector3 blockingCastHalfExt = new Vector3(
            castHalfExt.x * 0.95f,
            Mathf.Max(castHalfExt.y * 0.55f, 0.05f),
            castHalfExt.z * 0.95f);

        Vector3 worldCenter = referencePosition + transform.rotation * blockingCastCenter;
        RaycastHit[] hits = Physics.BoxCastAll(
            worldCenter,
            blockingCastHalfExt,
            moveDir,
            transform.rotation,
            castDistance,
            wallMask,
            QueryTriggerInteraction.Ignore);

        float bestDistance = float.MaxValue;
        bool found = false;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.collider == boxCol) continue;
            if (hit.transform == transform) continue;
            if (hit.transform.IsChildOf(transform)) continue;
            if (hit.normal.y >= 0.35f) continue;

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                bestHit = hit;
                found = true;
            }
        }

        return found;
    }

    // ── Step Climbing ─────────────────────────────────────────────────────────

    void ClimbStep()
    {
        if (Mathf.Abs(currentSpeed) < 0.01f) return;

        Vector3 rawMoveDir = transform.forward * Mathf.Sign(currentSpeed);
        Vector3 moveDir = Vector3.ProjectOnPlane(rawMoveDir, currentGroundNormal);
        if (moveDir.sqrMagnitude < 0.0001f)
            moveDir = rawMoveDir;
        else
            moveDir.Normalize();

        float checkDist = Mathf.Max(castHalfExt.z + 0.35f, 0.8f);
        if (!TryGetFrontGroundTargetY(moveDir, checkDist, out float targetY))
            return;

        float rise = targetY - transform.position.y;
        if (rise <= 0.02f || rise > stepHeight)
            return;

        float lift = Mathf.Min(rise, Mathf.Max(stepHeight * 0.3f, Mathf.Abs(currentSpeed) * Time.deltaTime * 0.5f));
        transform.position += Vector3.up * lift;
    }

    bool TryGetFrontGroundTargetY(Vector3 moveDir, float checkDist, out float targetY)
    {
        targetY = 0f;
        int groundMask = ~((1 << 13) | (1 << 2));

        Vector3[] probeLocalPoints;
        if (pivotOffsetsSet)
        {
            probeLocalPoints = new[]
            {
                castCenter + Vector3.forward * castHalfExt.z,
                new Vector3(pivotLeftLocalPos.x, castCenter.y, pivotLeftLocalPos.z),
                new Vector3(pivotRightLocalPos.x, castCenter.y, pivotRightLocalPos.z)
            };
        }
        else
        {
            probeLocalPoints = new[] { castCenter + Vector3.forward * castHalfExt.z };
        }

        float bestRise = float.MinValue;
        bool found = false;

        foreach (Vector3 localProbe in probeLocalPoints)
        {
            Vector3 probeWorld = transform.TransformPoint(localProbe) + moveDir * checkDist;
            Vector3 rayOrigin = probeWorld + Vector3.up * (stepHeight + 1f);

            if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, stepHeight + 2f, groundMask, QueryTriggerInteraction.Ignore))
                continue;

            if (hit.transform == transform)
                continue;

            if (hit.transform.IsChildOf(transform))
                continue;

            if (hit.normal.y < 0.35f)
                continue;

            float rise = hit.point.y - transform.position.y;
            if (rise > bestRise)
            {
                bestRise = rise;
                targetY = hit.point.y;
                found = true;
            }
        }

        return found;
    }

    bool TryGetGroundSupport(out float targetY, out Vector3 supportNormal)
    {
        targetY = 0f;
        supportNormal = Vector3.up;

        int groundMask = ~((1 << 13) | (1 << 2));
        float bestY = float.MinValue;
        Vector3 normalSum = Vector3.zero;
        int normalCount = 0;
        bool found = false;

        Vector3[] probeLocalPoints = pivotOffsetsSet
            ? new[]
            {
                new Vector3(pivotLeftLocalPos.x, castCenter.y, pivotLeftLocalPos.z),
                new Vector3(pivotRightLocalPos.x, castCenter.y, pivotRightLocalPos.z),
                new Vector3(rearLeftLocalPos.x, castCenter.y, rearLeftLocalPos.z),
                new Vector3(rearRightLocalPos.x, castCenter.y, rearRightLocalPos.z),
                castCenter
            }
            : new[] { castCenter };

        foreach (Vector3 localProbe in probeLocalPoints)
        {
            Vector3 origin = transform.TransformPoint(localProbe) + Vector3.up * 8f;
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 20f, groundMask, QueryTriggerInteraction.Ignore);

            RaycastHit bestHit = default;
            float closestDistance = float.MaxValue;
            bool hitFound = false;

            foreach (RaycastHit hit in hits)
            {
                if (hit.transform == transform) continue;
                if (hit.transform.IsChildOf(transform)) continue;
                if (hit.normal.y < 0.4f) continue;
                if (hit.distance >= closestDistance) continue;

                closestDistance = hit.distance;
                bestHit = hit;
                hitFound = true;
            }

            if (!hitFound)
                continue;

            found = true;
            if (bestHit.point.y > bestY)
                bestY = bestHit.point.y;

            normalSum += bestHit.normal;
            normalCount++;
        }

        if (!found)
            return false;

        targetY = bestY;
        if (normalCount > 0)
            supportNormal = (normalSum / normalCount).normalized;

        return true;
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
                if (into > 0f && dir.y < 0.1f)
                {
                    currentSpeed *= 0.7f;
                    TriggerImpactHaptics(0.75f);
                }
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

    void UpdateHaptics(float throttle, float steer)
    {
        float speed01 = Mathf.Clamp01(Mathf.Abs(currentSpeed) / Mathf.Max(maxForwardSpeed, 0.01f));
        float throttle01 = Mathf.Clamp01(Mathf.Abs(throttle));
        float steer01 = Mathf.Clamp01(Mathf.Abs(steer));
        float terrain01 = Mathf.Clamp01(1f - currentGroundNormal.y);

        if (speed01 < 0.02f && throttle01 < 0.01f && steer01 < 0.05f)
        {
            StopDriveHaptics();
            return;
        }

        rumbleTimer -= Time.deltaTime;
        if (rumbleTimer > 0f) return;

        rumbleTimer = 0.08f;

        float cruise = Mathf.Lerp(idleRumble, maxRumble, speed01);
        float throttleBoost = idleRumble * 0.5f * throttle01;
        float terrainBoost = roughTerrainRumble * terrain01;
        float steerBoost = steeringRumble * steer01;

        float leftIntensity = cruise + throttleBoost + terrainBoost + (steer < -0.05f ? steerBoost : steerBoost * 0.35f);
        float rightIntensity = cruise + throttleBoost + terrainBoost + (steer > 0.05f ? steerBoost : steerBoost * 0.35f);

        if (!hasGroundSupport)
        {
            leftIntensity *= 0.6f;
            rightIntensity *= 0.6f;
        }

        SendHaptic(leftDevice, Mathf.Clamp01(leftIntensity), 0.08f);
        SendHaptic(rightDevice, Mathf.Clamp01(rightIntensity), 0.08f);
    }

    void StopDriveHaptics()
    {
        SendHaptic(leftDevice, 0f, 0f);
        SendHaptic(rightDevice, 0f, 0f);
        rumbleTimer = 0f;
    }

    void TriggerImpactHaptics(float intensityScale)
    {
        if (!isMounted || impactCooldown > 0f)
            return;

        impactCooldown = impactDuration;

        float amplitude = Mathf.Clamp01(impactRumble * Mathf.Clamp01(intensityScale));
        SendHaptic(leftDevice, amplitude, impactDuration);
        SendHaptic(rightDevice, amplitude, impactDuration);
    }

    void SendHaptic(InputDevice d, float amplitude, float duration)
    {
        if (d.isValid) d.SendHapticImpulse(0, amplitude, duration);
    }

    // ── Mount / Dismount ──────────────────────────────────────────────────────

    void Mount()
    {
        SetJetpackMountedState(mounted: true);

        isMounted        = true;
        dismountCooldown = 2f;
        currentSpeed     = 0f;
        Debug.Log("[RoverDriver] Mounted!");

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

        StopDriveHaptics();

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
        SetJetpackMountedState(mounted: false);
    }

    void SetJetpackMountedState(bool mounted)
    {
        if (jetpack == null)
            return;

        jetpack.SetExternalFlightLock(mounted);

        var flyingField = typeof(AutoJetpackController).GetField("isFlying",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        if (mounted)
        {
            if (flyingField != null)
                flyingField.SetValue(jetpack, false);

            if (flightSmokeTrail != null)
                flightSmokeTrail.ClearTrail();

            if (HapticsManager.Instance != null)
                HapticsManager.Instance.StopJetpackVibration();

            jetpack.enabled = false;
            return;
        }

        jetpack.enabled = true;
        jetpack.SetExternalFlightLock(false);

        var cooldownField = typeof(AutoJetpackController).GetField("postDismountCooldown",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (cooldownField != null)
            cooldownField.SetValue(jetpack, 1.0f);
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
        if (TryGetGroundSupport(out float targetY, out Vector3 supportNormal))
        {
            hasGroundSupport = true;
            currentGroundNormal = supportNormal;
            if (Mathf.Abs(currentSpeed) > 0.01f)
            {
                Vector3 snapProbeDir = Vector3.ProjectOnPlane(transform.forward * Mathf.Sign(currentSpeed), currentGroundNormal);
                if (snapProbeDir.sqrMagnitude < 0.0001f)
                    snapProbeDir = transform.forward * Mathf.Sign(currentSpeed);
                else
                    snapProbeDir.Normalize();

                if (TryGetFrontGroundTargetY(snapProbeDir, castHalfExt.z + 0.15f, out float frontTargetY))
                {
                    float forwardRise = frontTargetY - targetY;
                    if (forwardRise > 0.02f && forwardRise <= stepHeight)
                        targetY = frontTargetY;
                }
            }

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
            hasGroundSupport = false;
            currentGroundNormal = Vector3.up;
            verticalVelocity    = Mathf.Max(verticalVelocity + Physics.gravity.y * Time.deltaTime, -20f);
            transform.position += Vector3.up * verticalVelocity * Time.deltaTime;
        }
    }
}
