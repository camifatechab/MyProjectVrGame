using UnityEngine;
using UnityEngine.XR;
using Unity.XR.CoreUtils;
using System.Collections.Generic;

/// VR Rover — kinematic movement + ComputePenetration wall correction.
public class RoverDriver : MonoBehaviour
{
    [Header("Seat")]
    public Transform seatAnchor;
    public float seatHeightBoost = 0.8f;

    [Header("Steering Wheel Visual")]
    public Transform steeringWheelMesh;
    private Quaternion wheelBaseRotation;

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

    [Header("Mount")]
    public float mountRadius = 4f;

    [Header("Haptics")]
    public float idleRumble = 0.05f;
    public float maxRumble  = 0.25f;

    // --- runtime refs ---
    // Rigidbody removed — GroundSnap handles vertical movement
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

    // --- collision ---
    // FIX 4: exclude dragon layer (13) AND XR layer (2) from wall checks
    // FIX 2/3: use fixed local half-extents computed once, not world-space renderer bounds
    private int     wallMask;
    private Vector3 castHalfExt;   // local-space half-extents for BoxCast
    private Vector3 castCenter;    // local offset from pivot to collider center

void Start()
    {
        boxCol = GetComponent<BoxCollider>();

        wallMask = ~((1 << 13) | (1 << 2));

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

        // Snap to ground immediately on start
        GroundSnap();
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
            if (bothGrips && dismountCooldown <= 0f)
            {
                dismountHoldTimer += Time.deltaTime;
                if (dismountHoldTimer >= 0.5f)
                {
                    dismountHoldTimer = 0f;
                    Dismount();
                    gripsLastFrame = bothGrips;
                    return;
                }
            }
            else { dismountHoldTimer = 0f; }

            float throttle = 0f;
            if (rightGrip && !leftGrip)      throttle =  1f;
            else if (leftGrip && !rightGrip) throttle = -1f;

            // FIX 6: pass Time.deltaTime explicitly so smoothing is frame-rate independent
            float steer = ComputeWheelSteering(Time.deltaTime);
            Drive(throttle, steer, Time.deltaTime);
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

        // FIX 2: BoxCast from COLLIDER CENTER in world space with LOCAL half-extents
        // These are correct regardless of rover rotation
        Vector3 worldCenter = transform.TransformPoint(castCenter);
        Vector3 moveDir     = transform.forward * Mathf.Sign(currentSpeed);
        float   moveDist    = Mathf.Abs(currentSpeed) * dt;

        if (Physics.BoxCast(worldCenter, castHalfExt, moveDir,
                out RaycastHit hit, transform.rotation,
                moveDist, wallMask, QueryTriggerInteraction.Ignore)
            && hit.transform != null
            && hit.transform != transform
            && !hit.transform.IsChildOf(transform))
        {
            float safe = Mathf.Max(0f, hit.distance - 0.02f);
            moveDist     = safe;
            currentSpeed = 0f;
        }

        transform.position += moveDir * moveDist;
    }

    // ── Depenetration ─────────────────────────────────────────────────────────
    // FIX 3: no more renderer bounds — uses boxCol directly, no per-frame allocation
    void Depenetrate()
    {
        if (boxCol == null) return;

        Vector3 worldCenter = transform.TransformPoint(castCenter);

        // FIX 5: reuse castHalfExt — no GetComponentsInChildren allocation every frame
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
                transform.position += dir * (dist + 0.001f);
                float into = Vector3.Dot(transform.forward * currentSpeed, -dir);
                if (into > 0f) currentSpeed = 0f;
            }
        }
    }

    // ── Wheels ────────────────────────────────────────────────────────────────

    void SpinWheels()
    {
        float spin = (currentSpeed / Mathf.Max(wheelRadius, 0.01f)) * Mathf.Rad2Deg * Time.deltaTime;
        foreach (Transform w in wheelMeshes)
            if (w != null) w.Rotate(Vector3.right, spin, Space.Self);
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
        isMounted        = true;
        dismountCooldown = 2f;
        currentSpeed     = 0f;
        Debug.Log("[RoverDriver] Mounted!");

        // Disable jetpack and force isFlying = false so effects stop immediately
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

        // Give jetpack a 1-second cooldown so dismount grips don't trigger flight
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
    }


void GroundSnap()
    {
        int groundMask = ~((1 << 13) | (1 << 2));
        Vector3 origin  = transform.position + Vector3.up * 3f;

        // RaycastAll so we skip our own BoxCollider and find the actual ground
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 10f,
            groundMask, QueryTriggerInteraction.Ignore);

        // Find closest hit that is NOT this rover or its children
        RaycastHit best = default;
        float bestDist  = float.MaxValue;
        bool  found     = false;

        foreach (var h in hits)
        {
            if (h.transform == transform) continue;
            if (h.transform.IsChildOf(transform)) continue;
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

            if (diff > -0.05f)
            {
                // On ground or uphill — snap directly
                transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
                verticalVelocity   = 0f;
            }
            else
            {
                // Falling — apply gravity, clamp when we reach surface
                verticalVelocity += Physics.gravity.y * Time.deltaTime;
                float newY = transform.position.y + verticalVelocity * Time.deltaTime;
                if (newY <= targetY) { newY = targetY; verticalVelocity = 0f; }
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            }

            // Tilt to terrain normal, preserve yaw
            float   yaw = transform.eulerAngles.y;
            Vector3 fwd = Vector3.ProjectOnPlane(
                Quaternion.Euler(0f, yaw, 0f) * Vector3.forward, best.normal).normalized;
            if (fwd.sqrMagnitude > 0.01f)
            {
                Quaternion target  = Quaternion.LookRotation(fwd, best.normal);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, 12f * Time.deltaTime);
            }
        }
        else
        {
            // No ground found — fall with terminal velocity cap
            verticalVelocity    = Mathf.Max(verticalVelocity + Physics.gravity.y * Time.deltaTime, -20f);
            transform.position += Vector3.up * verticalVelocity * Time.deltaTime;
        }
    }
}
