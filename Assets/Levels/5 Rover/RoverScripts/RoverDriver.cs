using UnityEngine;
using UnityEngine.XR;
using Unity.XR.CoreUtils;
using System.Collections.Generic;

/// VR Rover — hand-vector steering wheel controls.
/// Right grip = gas. Left grip = brake/reverse. B = dismount.
public class RoverDriver : MonoBehaviour
{
    [Header("Seat")]
    public Transform seatAnchor;
    public float seatHeightBoost = 0.6f;

    [Header("Steering Wheel Visual")]
    public Transform steeringWheelMesh;
    private Quaternion wheelBaseRotation;

    [Header("Wheel Meshes (visual spin)")]
    public Transform[] wheelMeshes;
    public float wheelRadius = 0.35f;

    [Header("Movement")]
    public float maxForwardSpeed = 10f;
    public float maxReverseSpeed = 4f;
    public float acceleration    = 3f;
    public float brakingForce    = 4f;
    public float turnRate        = 55f;

    [Header("Steering")]
    public float steerDeadzone = 10f;
    public float steerMaxAngle = 80f;

    [Header("Mount")]
    public float mountRadius = 4f;

    [Header("Haptics")]
    public float idleRumble = 0.05f;
    public float maxRumble  = 0.25f;

    // --- runtime refs ---
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
    private float dismountHoldTimer = 0f;   // how long both grips held for dismount

    void Start()
    {
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

        // Force Rigidbody to kinematic — rover must never fall or be pushed by physics
        var rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }

        if (seatAnchor == null)
        {
            var go = new GameObject("SeatAnchor");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(-0.55f, 0.55f, -0.25f);
            seatAnchor = go.transform;
        }
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
            if (bothGrips && !gripsLastFrame && xrOrigin != null)
            {
                float dist = Vector3.Distance(xrOrigin.transform.position, transform.position);
                if (dist < mountRadius) Mount();
            }
        }
        else
        {
            // Dismount: hold both grips for 1.5s (deliberate, won't fire by accident)
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
            else
            {
                dismountHoldTimer = 0f;
            }

            float throttle = 0f;
            if (rightGrip && !leftGrip)      throttle =  1f;
            else if (leftGrip && !rightGrip) throttle = -1f;

            float steer = ComputeWheelSteering();
            Drive(throttle, steer);
            SpinWheels();
            UpdateHaptics(rightGrip);
        }

        gripsLastFrame = bothGrips;
    }

    // ── Hand-vector steering ──────────────────────────────────────────────────
    // Measures signed angle of (rightCtrl - leftCtrl) vector in rover XZ plane.
    // Delta from neutral on mount = wheel rotation = steer input.

    float ComputeWheelSteering()
    {
        if (leftCtrl == null || rightCtrl == null)
        {
            currentWheelAngle = Mathf.Lerp(currentWheelAngle, 0f, 4f * Time.deltaTime);
            UpdateWheelVisual(currentWheelAngle);
            return 0f;
        }

        float delta = Mathf.DeltaAngle(neutralAngle, GetHandAngle());
        currentWheelAngle = Mathf.Lerp(currentWheelAngle, delta, 15f * Time.deltaTime);
        UpdateWheelVisual(currentWheelAngle);

        float abs = Mathf.Abs(delta);
        if (abs < steerDeadzone) return 0f;
        float effective = delta > 0f ? abs - steerDeadzone : -(abs - steerDeadzone);
        return Mathf.Clamp(effective / (steerMaxAngle - steerDeadzone), -1f, 1f);
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

    // ── Driving ──────────────────────────────────────────────────────────────

    void Drive(float throttle, float steer)
    {
        smoothedSteer = Mathf.Lerp(smoothedSteer, steer, 3f * Time.deltaTime);

        if (throttle > 0.05f)
            currentSpeed = Mathf.MoveTowards(currentSpeed,  maxForwardSpeed, acceleration * Time.deltaTime);
        else if (throttle < -0.05f)
            currentSpeed = Mathf.MoveTowards(currentSpeed, -maxReverseSpeed, acceleration * Time.deltaTime);
        else
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakingForce * Time.deltaTime);

        float speedFactor   = 1f - (Mathf.Abs(currentSpeed) / maxForwardSpeed) * 0.5f;
        float effectiveTurn = turnRate * speedFactor;

        if (Mathf.Abs(currentSpeed) > 0.1f && Mathf.Abs(smoothedSteer) > 0.05f)
            transform.Rotate(0f, smoothedSteer * effectiveTurn * Mathf.Sign(currentSpeed) * Time.deltaTime, 0f, Space.World);

        transform.position += transform.forward * currentSpeed * Time.deltaTime;
    }

    void SpinWheels()
    {
        float spin = (currentSpeed / Mathf.Max(wheelRadius, 0.01f)) * Mathf.Rad2Deg * Time.deltaTime;
        foreach (Transform w in wheelMeshes)
            if (w != null) w.Rotate(Vector3.right, spin, Space.Self);
    }

    // ── Haptics ──────────────────────────────────────────────────────────────

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

    // ── Mount / Dismount ─────────────────────────────────────────────────────

    void Mount()
    {
        isMounted        = true;
        dismountCooldown = 2f;
        currentSpeed     = 0f;
        Debug.Log("[RoverDriver] Mounted!");

        if (xrOrigin != null && Camera.main != null)
        {
            Vector3 headOffset          = Camera.main.transform.position - xrOrigin.transform.position;
            xrOrigin.transform.position = seatAnchor.position - headOffset + Vector3.up * seatHeightBoost;
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
        if (jetpack        != null) jetpack.enabled        = false;
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
        if (jetpack        != null) jetpack.enabled        = true;
    }

    // ── XR helpers ───────────────────────────────────────────────────────────

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
}
