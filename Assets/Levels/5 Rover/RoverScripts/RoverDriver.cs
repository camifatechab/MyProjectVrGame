using UnityEngine;
using UnityEngine.XR;
using Unity.XR.CoreUtils;
using System.Collections.Generic;

/// VR Rover — Mario Kart style controls.
/// Right grip = gas. Left grip = brake/reverse.
/// Tilt both controllers left/right = steer.
/// B button = dismount.
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
    public float acceleration    = 3f;   // slow build-up = heavy feel
    public float brakingForce    = 4f;   // slow to stop
    public float turnRate        = 55f;

    [Header("Steering (tilt)")]
    [Tooltip("Controller roll degrees before steering starts")]
    public float tiltDeadzone  = 10f;
    public float tiltMaxAngle  = 40f;

    [Header("Mount")]
    public float mountRadius = 4f;

    [Header("Arms Extended Check")]
    [Tooltip("How far forward (meters) controllers must be from the headset to count as extended")]
    public float armsForwardMin = 0.15f;
    [Tooltip("Show debug gizmos for arm check")]
    public bool  debugArms = false;

    [Header("Haptics")]
    public float idleRumble  = 0.05f;
    public float maxRumble   = 0.25f;

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
    private bool  isMounted        = false;
    private bool  gripsLastFrame   = false;
    private float currentSpeed     = 0f;
    private float visualSteer      = 0f;
    private float smoothedSteer    = 0f;   // lerped steer for heavy feel
    private float dismountCooldown = 0f;
    private float rumbleTimer      = 0f;

    // ─────────────────────────────────────────────────────────────────────────

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

        // Auto-find steering wheel
        if (steeringWheelMesh == null)
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
                if (t.name.ToLower().Contains("steering"))
                    { steeringWheelMesh = t; break; }

        if (steeringWheelMesh != null)
            wheelBaseRotation = steeringWheelMesh.localRotation;

        // Auto-find wheels: cylinder_ + dot suffix + offset from center
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
            Debug.Log($"[RoverDriver] Wheels: {string.Join(", ", System.Array.ConvertAll(wheelMeshes, w => w.name))}");
        }

        if (seatAnchor == null)
        {
            var go = new GameObject("SeatAnchor");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(-0.23f, 0.55f, 0.1f);
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
        bool bButton   = GetButton(rightDevice, CommonUsages.secondaryButton);

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
            if (bButton && dismountCooldown <= 0f)
            {
                Dismount();
                gripsLastFrame = bothGrips;
                return;
            }

            // Gas = right grip, Brake/reverse = left grip
            bool  armed    = ArmsExtended();

            float throttle = 0f;
            if (armed)
            {
                if (rightGrip && !leftGrip)      throttle =  1f;
                else if (leftGrip && !rightGrip) throttle = -1f;
                // both grips = coast (throttle stays 0)
            }

            float steer = armed ? ComputeTiltSteering() : 0f;
            Drive(throttle, steer);
            SpinWheels();
            UpdateHaptics(rightGrip);
        }

        gripsLastFrame = bothGrips;
    }

    // ── Arms extended check ─────────────────────────────────────────────────
    // Returns true when BOTH controllers are in front of the headset by armsForwardMin.
    // "In front" = positive dot product with head's forward direction.

    bool ArmsExtended()
    {
        if (Camera.main == null || leftCtrl == null || rightCtrl == null) return false;

        Transform head    = Camera.main.transform;
        Vector3   headPos = head.position;
        Vector3   fwd     = head.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.001f) fwd = transform.forward;
        fwd.Normalize();

        float leftDot  = Vector3.Dot(leftCtrl.position  - headPos, fwd);
        float rightDot = Vector3.Dot(rightCtrl.position - headPos, fwd);

        if (debugArms)
            Debug.Log($"[RoverDriver] Arms fwd — L:{leftDot:F2} R:{rightDot:F2} (need >{armsForwardMin})");

        return leftDot > armsForwardMin && rightDot > armsForwardMin;
    }

    // ── Tilt steering ─────────────────────────────────────────────────────────
    // Average roll of both controllers in world space → steer value -1..1

    float ComputeTiltSteering()
    {
        if (leftCtrl == null || rightCtrl == null) return 0f;

        float leftRoll  = GetControllerRoll(leftCtrl);
        float rightRoll = GetControllerRoll(rightCtrl);
        float avgRoll   = (leftRoll + rightRoll) * 0.5f;

        // Visual wheel follows tilt
        visualSteer = Mathf.Lerp(visualSteer, avgRoll, 8f * Time.deltaTime);
        if (steeringWheelMesh != null)
            steeringWheelMesh.localRotation = wheelBaseRotation *
                Quaternion.AngleAxis(visualSteer * (90f / tiltMaxAngle), Vector3.up);

        float abs = Mathf.Abs(avgRoll);
        if (abs < tiltDeadzone) return 0f;
        float effective = avgRoll > 0f ? abs - tiltDeadzone : -(abs - tiltDeadzone);
        return Mathf.Clamp(effective / (tiltMaxAngle - tiltDeadzone), -1f, 1f);
    }

    // Roll = rotation around the controller's forward axis (Z in world XZ plane)
    float GetControllerRoll(Transform ctrl)
    {
        // Project controller's local right vector onto world XZ plane, measure tilt
        Vector3 right = ctrl.right;
        right.y = 0f;
        if (right.sqrMagnitude < 0.001f) return 0f;
        right.Normalize();

        // Compare against rover's right vector to get signed roll
        float dot   = Vector3.Dot(ctrl.up, Vector3.up);
        float cross = ctrl.up.x * transform.forward.z - ctrl.up.z * transform.forward.x;
        // Use controller's up.y drop as tilt signal
        float tilt  = ctrl.forward.y * 90f; // -90..90 degrees
        return tilt;
    }

    // ── Driving ──────────────────────────────────────────────────────────────

void Drive(float throttle, float steer)
    {
        // Smooth steer input — heavy vehicle responds slowly
        smoothedSteer = Mathf.Lerp(smoothedSteer, steer, 3f * Time.deltaTime);

        // Speed
        if (throttle > 0.05f)
            currentSpeed = Mathf.MoveTowards(currentSpeed,  maxForwardSpeed, acceleration * Time.deltaTime);
        else if (throttle < -0.05f)
            currentSpeed = Mathf.MoveTowards(currentSpeed, -maxReverseSpeed, acceleration * Time.deltaTime);
        else
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakingForce * Time.deltaTime);

        // Turn rate drops at high speed (wide turns) — heavy vehicle behaviour
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
    // Continuous low rumble while driving, scales with speed

    void UpdateHaptics(bool gasHeld)
    {
        if (!gasHeld || Mathf.Abs(currentSpeed) < 0.2f)
        {
            // Stop rumble
            SendHaptic(leftDevice,  0f, 0f);
            SendHaptic(rightDevice, 0f, 0f);
            rumbleTimer = 0f;
            return;
        }

        rumbleTimer -= Time.deltaTime;
        if (rumbleTimer > 0f) return;
        rumbleTimer = 0.08f; // pulse every 80ms

        float intensity = Mathf.Lerp(idleRumble, maxRumble, Mathf.Abs(currentSpeed) / maxForwardSpeed);
        SendHaptic(leftDevice,  intensity, 0.08f);
        SendHaptic(rightDevice, intensity, 0.08f);
    }

    void SendHaptic(InputDevice d, float amplitude, float duration)
    {
        if (d.isValid)
            d.SendHapticImpulse(0, amplitude, duration);
    }

    // ── Mount / Dismount ─────────────────────────────────────────────────────

    void Mount()
    {
        isMounted        = true;
        dismountCooldown = 1f;
        currentSpeed     = 0f;
        Debug.Log("[RoverDriver] Mounted!");

        if (xrOrigin != null && Camera.main != null)
        {
            Vector3 headOffset          = Camera.main.transform.position - xrOrigin.transform.position;
            xrOrigin.transform.position = seatAnchor.position - headOffset + Vector3.up * seatHeightBoost;
            xrOrigin.transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            xrOrigin.transform.SetParent(transform, worldPositionStays: true);
        }

        if (locomotionProviders != null)
            foreach (var p in locomotionProviders) if (p != null) p.enabled = false;

        if (charController != null) charController.enabled = false;
        if (jetpack        != null) jetpack.enabled        = false;
    }

    void Dismount()
    {
        isMounted    = false;
        currentSpeed = 0f;
        Debug.Log("[RoverDriver] Dismounted!");

        // Kill haptics on exit
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
