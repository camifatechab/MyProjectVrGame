using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

/// <summary>
/// Allows player to mount and ride a flying creature.
/// Attach to the rideable creature (Sky_flyB).
/// </summary>
public class RideableCreature : MonoBehaviour
{
    [Header("Mount Settings")]
    [Tooltip("Distance player must be within to mount")]
    public float mountDistance = 3f;
    
    [Tooltip("Where player sits relative to creature")]
    public Vector3 seatOffset = new Vector3(0f, 1f, 0f);
    
    [Tooltip("How long the mount/dismount transition takes")]
    public float transitionDuration = 0.5f;
    
    [Header("Flight Path")]
    [Tooltip("Waypoints for scenic flight. If empty, creature continues normal patrol.")]
    public List<Transform> flightPathWaypoints = new List<Transform>();
    
    [Tooltip("Speed while carrying player")]
    public float rideSpeed = 12f;
    
    [Tooltip("Return to start position after dismount")]
    public bool returnToStartAfterDismount = true;
    
    [Header("Haptic Feedback")]
    [Tooltip("Enable haptic pulses synced to wing flaps")]
    public bool enableHaptics = true;
    
    [Tooltip("Time between haptic pulses (wing flap rhythm)")]
    public float hapticInterval = 0.8f;
    
    [Tooltip("Haptic pulse intensity (0-1)")]
    [Range(0f, 1f)]
    public float hapticIntensity = 0.3f;
    
    [Tooltip("Haptic pulse duration")]
    public float hapticDuration = 0.1f;
    
    [Header("Idle State")]
    [Tooltip("Creature starts idle and waits for player")]
    public bool startIdle = true;
    
    [Tooltip("Gentle bobbing while idle")]
    public float idleBobAmplitude = 0.3f;
    
    [Tooltip("Idle bob speed")]
    public float idleBobSpeed = 1f;
    
    
[Header("Visual Indicator")]
    [Tooltip("Show prompt when player is near")]
    public bool showMountPrompt = true;
    
    [Tooltip("UI text to show mount prompt")]
    public string mountPromptText = "Grip to Ride";
    
    [Header("References")]
    public Transform playerCamera;
    public Transform xrOrigin;
    
    // Runtime state
    private bool isPlayerMounted = false;
    private bool isFlying = false; // True during flight path, false when idle on creature
    private bool isTransitioning = false;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private FlyingCreaturePatrol patrolScript;
    private MonoBehaviour jetpackController;
    private float hapticTimer;
    private int currentFlightWaypointIndex = 0;
    private float splineT = 0f; // Progress along current spline segment (0-1)-1)0;
    
    // XR Controllers for haptics
    private ActionBasedController leftController;
    private ActionBasedController rightController;
    
    // XR Input Devices for direct grip detection
    private InputDevice leftDevice;
    private InputDevice rightDevice;
    private bool wasGripPressed = false;
    private Canvas uiCanvas;
    private UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement.ContinuousMoveProvider moveProvider;
    private UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning.ContinuousTurnProvider turnProvider;
    private UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning.SnapTurnProvider snapTurnProvider;



    
    // Player state before mounting
    private Vector3 playerOriginalPosition;
    private Transform originalParent;
    private float idleBobOffset;
    private Vector3 idleBasePosition;
    private Vector3 lastCreaturePosition;
    private Quaternion mountedRotation; // Locked rotation during ride (prevents dizziness)


    
    public bool IsPlayerMounted => isPlayerMounted;
    
    // Events
    public System.Action OnPlayerMounted;
    public System.Action OnPlayerDismounted;
    
    private void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        idleBasePosition = transform.position;
        idleBobOffset = Random.Range(0f, Mathf.PI * 2f);
        
        // If starting idle, disable patrol
        if (startIdle)
        {
            var patrol = GetComponent<FlyingCreaturePatrol>();
            if (patrol != null)
                patrol.enabled = false;
        }

        
        patrolScript = GetComponent<FlyingCreaturePatrol>();
        
        // Auto-find XR components
        if (xrOrigin == null)
        {
            var origin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (origin != null)
            {
                xrOrigin = origin.transform;
                playerCamera = origin.Camera?.transform;
            }
        }
        
        // Find XR controllers for haptics
        FindXRControllers();
        
        // Find jetpack controller to disable during ride
        FindJetpackController();
        
        // Cache locomotion providers (avoid FindObjectOfType in Update)
        moveProvider = FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement.ContinuousMoveProvider>();
        turnProvider = FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning.ContinuousTurnProvider>();
        snapTurnProvider = FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning.SnapTurnProvider>();
        
        // Cache UI canvas
        var canvases = FindObjectsOfType<Canvas>();
        foreach (var canvas in canvases)
        {
            if (canvas.gameObject.name.Contains("UI") || canvas.gameObject.name.Contains("Canvas"))
            {
                uiCanvas = canvas;
                break;
            }
        }
            // Auto-find flight waypoints if not assigned
        if (flightPathWaypoints.Count == 0)
        {
            AutoFindWaypoints();
        }
    }

private void AutoFindWaypoints()
    {
        // Look for waypoint parent objects - try FlightPath first, then FlightWaypoints
        GameObject waypointParent = GameObject.Find("FlightPath");
        if (waypointParent == null)
            waypointParent = GameObject.Find("FlightWaypoints");
        
        if (waypointParent == null)
        {
            Debug.LogWarning("RideableCreature: No FlightPath or FlightWaypoints found in scene!");
            return;
        }
        
        // Get all waypoints sorted by name (WP01, WP02, etc.)
        var waypoints = new System.Collections.Generic.List<Transform>();
        foreach (Transform child in waypointParent.transform)
        {
            waypoints.Add(child);
        }
        
        // Sort by name to ensure correct order
        waypoints.Sort((a, b) => a.name.CompareTo(b.name));
        
        flightPathWaypoints.Clear();
        flightPathWaypoints.AddRange(waypoints);
        
        Debug.Log($"RideableCreature: Auto-found {flightPathWaypoints.Count} flight waypoints from {waypointParent.name}");
    }

    
private void FindXRControllers()
    {
        var controllers = FindObjectsOfType<ActionBasedController>();
        foreach (var controller in controllers)
        {
            string name = controller.gameObject.name.ToLower();
            if (name.Contains("left"))
                leftController = controller;
            else if (name.Contains("right"))
                rightController = controller;
        }
        
        // Also get InputDevices for direct grip detection
        var leftHandDevices = new System.Collections.Generic.List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftHandDevices);
        if (leftHandDevices.Count > 0)
        {
            leftDevice = leftHandDevices[0];
            Debug.Log("RideableCreature: Found left hand device");
        }
        
        var rightHandDevices = new System.Collections.Generic.List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);
        if (rightHandDevices.Count > 0)
        {
            rightDevice = rightHandDevices[0];
            Debug.Log("RideableCreature: Found right hand device");
        }
    }
    
private void FindJetpackController()
    {
        // Look for AutoJetpackController specifically
        if (xrOrigin != null)
        {
            // Try to find by component type directly
            var controllers = xrOrigin.GetComponentsInChildren<MonoBehaviour>();
            foreach (var controller in controllers)
            {
                if (controller.GetType().Name == "AutoJetpackController")
                {
                    jetpackController = controller;
                    Debug.Log("RideableCreature: Found AutoJetpackController");
                    return;
                }
            }
        }
    }
    
private void Update()
    {
        if (isTransitioning) return;
        
        // Desktop testing shortcut - press M to mount from anywhere
        if (!isPlayerMounted && Input.GetKeyDown(KeyCode.M))
        {
            Debug.Log("[Desktop Test] M pressed - force mounting creature");
            MountCreature();
            return;
        }
        
        if (isPlayerMounted)
        {
            UpdateMountedState();
        }
        else
        {
            CheckForMountInput();
            
            // Gentle idle bobbing while waiting
            if (startIdle && idleBobAmplitude > 0f)
            {
                float bob = Mathf.Sin((Time.time * idleBobSpeed) + idleBobOffset) * idleBobAmplitude;
                transform.position = idleBasePosition + new Vector3(0f, bob, 0f);
            }
        }
    }
    
private void CheckForMountInput()
    {
        if (playerCamera == null)
        {
            return;
        }
        
        // Refresh devices if not valid (VR devices may not be ready at Start)
        if (!leftDevice.isValid || !rightDevice.isValid)
        {
            RefreshInputDevices();
        }
        
        float distance = Vector3.Distance(transform.position, playerCamera.position);
        
        if (distance <= mountDistance)
        {
            // Use direct grip detection like AutoJetpackController
            bool gripPressed = IsGripPressed(leftDevice) || IsGripPressed(rightDevice);
            
            // Fallback to keyboard for testing
            if (Input.GetKeyDown(KeyCode.E))
            {
                gripPressed = true;
            }
            
            // Detect grip press (not held)
            if (gripPressed && !wasGripPressed)
            {
                Debug.Log("Grip pressed - mounting creature!");
                MountCreature();
            }
            
            wasGripPressed = gripPressed;
        }
        else
        {
            wasGripPressed = false;
        }
    }
    
    private void RefreshInputDevices()
    {
        var leftHandDevices = new System.Collections.Generic.List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftHandDevices);
        if (leftHandDevices.Count > 0)
            leftDevice = leftHandDevices[0];
        
        var rightHandDevices = new System.Collections.Generic.List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);
        if (rightHandDevices.Count > 0)
            rightDevice = rightHandDevices[0];
    }
    
    private bool IsGripPressed(InputDevice device)
    {
        if (!device.isValid)
            return false;
        
        // Try grip button (binary)
        bool gripButton = false;
        if (device.TryGetFeatureValue(CommonUsages.gripButton, out gripButton) && gripButton)
            return true;
        
        // Try grip (analog)
        float grip = 0f;
        if (device.TryGetFeatureValue(CommonUsages.grip, out grip) && grip > 0.5f)
            return true;
        
        return false;
    }
    
    private void MountCreature()
    {
        if (isPlayerMounted || isTransitioning) return;
        
        StartCoroutine(MountTransition());
    }
    
    private System.Collections.IEnumerator MountTransition()
    {
        isTransitioning = true;
        
        // Store player's original state
        if (xrOrigin != null)
        {
            playerOriginalPosition = xrOrigin.position;
            originalParent = xrOrigin.parent;
        }
        
        // Disable jetpack
        if (jetpackController != null)
            jetpackController.enabled = false;
        
        // Disable locomotion providers (already cached in Start)
        if (moveProvider != null)
            moveProvider.enabled = false;
        if (turnProvider != null)
            turnProvider.enabled = false;
        if (snapTurnProvider != null)
            snapTurnProvider.enabled = false;
        
        // Hide UI canvas (already cached in Start)
        if (uiCanvas != null)
            uiCanvas.enabled = false;
        
        // Disable normal patrol
        if (patrolScript != null)
            patrolScript.enabled = false;
        
        // Parent player to creature for reliable following
        if (xrOrigin != null)
        {
            playerOriginalPosition = xrOrigin.position;
            originalParent = xrOrigin.parent;
            
            // Parent to creature
            xrOrigin.SetParent(transform);
            
            // Smoothly move player to seat position
            Vector3 targetLocalPos = seatOffset;
            float elapsed = 0f;
            Vector3 startLocalPos = xrOrigin.localPosition;
            
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / transitionDuration;
                t = t * t * (3f - 2f * t); // Smoothstep
                
                xrOrigin.localPosition = Vector3.Lerp(startLocalPos, targetLocalPos, t);
                // Keep player upright during transition
                xrOrigin.rotation = Quaternion.Euler(0, xrOrigin.eulerAngles.y, 0);
                yield return null;
            }
            
            xrOrigin.localPosition = targetLocalPos;
            
            // Store initial rotation - will be locked during entire ride
            mountedRotation = xrOrigin.rotation;
        }
        
        isPlayerMounted = true;
        isFlying = true; // Start flight when mounted
        currentFlightWaypointIndex = 0;
        splineT = 0f;
        hapticTimer = 0f;
        lastCreaturePosition = transform.position;
        
        Debug.Log($"Player mounted! isFlying={isFlying}, waypoints={flightPathWaypoints.Count}");
        
        OnPlayerMounted?.Invoke();
        isTransitioning = false; // Allow Update to run!
    }
    
private void UpdateMountedState()
    {
        // Refresh devices if not valid
        if (!leftDevice.isValid || !rightDevice.isValid)
        {
            RefreshInputDevices();
        }
        
        // Lock player to creature - facing creature's forward, but staying upright
        if (xrOrigin != null)
        {
            // Add bobbing motion that matches the creature's flying animation
            // This syncs the player with the creature's visual movement to prevent motion sickness
            float rideBob = Mathf.Sin(Time.time * idleBobSpeed) * idleBobAmplitude;
            Vector3 bobbedSeatOffset = seatOffset + new Vector3(0f, rideBob, 0f);
            
            // Keep player at seat offset with matching bob
            xrOrigin.localPosition = bobbedSeatOffset;
            
            // Face creature's forward direction but stay upright (like riding a dragon)
            float creatureYaw = transform.eulerAngles.y;
            xrOrigin.rotation = Quaternion.Euler(0, creatureYaw, 0);
        }
        
        // Handle haptic feedback
        if (enableHaptics)
        {
            hapticTimer += Time.deltaTime;
            if (hapticTimer >= hapticInterval)
            {
                hapticTimer = 0f;
                SendHapticPulse();
            }
        }
        
        // Follow flight path if currently flying
        if (isFlying)
        {
            if (flightPathWaypoints.Count > 0)
            {
                FollowFlightPath();
            }
            else
            {
                Debug.LogWarning("isFlying is true but no waypoints! Count: " + flightPathWaypoints.Count);
            }
        }
        
        // Only allow dismount when NOT flying (creature is idle)
        if (!isFlying)
        {
            bool gripPressed = IsGripPressed(leftDevice) || IsGripPressed(rightDevice);
            
            // Fallback to keyboard
            if (Input.GetKeyDown(KeyCode.E))
                gripPressed = true;
            
            // Detect grip press (not held)
            if (gripPressed && !wasGripPressed)
            {
                Debug.Log("Grip pressed - dismounting creature!");
                DismountCreature();
            }
            
            wasGripPressed = gripPressed;
        }
    }
    
private void FollowFlightPath()
    {
        if (flightPathWaypoints.Count < 2)
        {
            Debug.LogWarning("FollowFlightPath: Not enough waypoints!");
            EndFlightReturnToIdle();
            return;
        }
        
        // Get the 4 points needed for Catmull-Rom spline
        int p0 = Mathf.Clamp(currentFlightWaypointIndex - 1, 0, flightPathWaypoints.Count - 1);
        int p1 = Mathf.Clamp(currentFlightWaypointIndex, 0, flightPathWaypoints.Count - 1);
        int p2 = Mathf.Clamp(currentFlightWaypointIndex + 1, 0, flightPathWaypoints.Count - 1);
        int p3 = Mathf.Clamp(currentFlightWaypointIndex + 2, 0, flightPathWaypoints.Count - 1);
        
        if (flightPathWaypoints[p1] == null || flightPathWaypoints[p2] == null)
        {
            Debug.LogWarning($"FollowFlightPath: Null waypoint at p1={p1} or p2={p2}");
            currentFlightWaypointIndex++;
            return;
        }
        
        // Calculate segment length for consistent speed
        float segmentLength = Vector3.Distance(flightPathWaypoints[p1].position, flightPathWaypoints[p2].position);
        if (segmentLength < 0.1f) segmentLength = 0.1f;
        
        // Variable speed based on vertical direction - faster when diving!
        Vector3 nextPos = flightPathWaypoints[p2].position;
        float verticalDiff = nextPos.y - transform.position.y;
        float speedMultiplier = 1f;
        if (verticalDiff < -5f)
            speedMultiplier = 1.6f; // Diving
        else if (verticalDiff > 10f)
            speedMultiplier = 0.7f; // Climbing
        
        float currentSpeed = rideSpeed * speedMultiplier;
        
        // Advance along spline based on speed
        splineT += (currentSpeed / segmentLength) * Time.deltaTime;
        
        // Debug every ~1 second
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"Flight: wp={currentFlightWaypointIndex}, t={splineT:F2}, pos={transform.position}, target={nextPos}");
        }
        
        // Move to next segment when t >= 1
        if (splineT >= 1f)
        {
            splineT = 0f;
            currentFlightWaypointIndex++;
            Debug.Log($"Moving to waypoint {currentFlightWaypointIndex}");
            
            // Check if we've reached the end - return to idle, don't auto-dismount
            if (currentFlightWaypointIndex >= flightPathWaypoints.Count - 1)
            {
                EndFlightReturnToIdle();
                return;
            }
        }
        
        // Calculate smooth position using Catmull-Rom spline
        Vector3 pos0 = flightPathWaypoints[p0] != null ? flightPathWaypoints[p0].position : flightPathWaypoints[p1].position;
        Vector3 pos1 = flightPathWaypoints[p1].position;
        Vector3 pos2 = flightPathWaypoints[p2].position;
        Vector3 pos3 = flightPathWaypoints[p3] != null ? flightPathWaypoints[p3].position : flightPathWaypoints[p2].position;
        
        Vector3 newPosition = CatmullRom(pos0, pos1, pos2, pos3, splineT);
        
        // Smooth rotation - calculate direction from movement
        Vector3 direction = (newPosition - transform.position).normalized;
        if (direction.sqrMagnitude > 0.001f)
        {
            Vector3 flatDirection = new Vector3(direction.x, 0, direction.z).normalized;
            if (flatDirection.sqrMagnitude > 0.001f)
            {
                float targetYAngle = Mathf.Atan2(flatDirection.x, flatDirection.z) * Mathf.Rad2Deg;
                float currentY = transform.eulerAngles.y;
                float newY = Mathf.LerpAngle(currentY, targetYAngle, 0.8f * Time.deltaTime);
                
                // Calculate gentle banking based on turn rate
                float turnDelta = Mathf.DeltaAngle(currentY, targetYAngle);
                float bankAngle = Mathf.Clamp(turnDelta * 0.2f, -20f, 20f);
                
                transform.rotation = Quaternion.Euler(61f, newY, bankAngle);
            }
        }
        
        transform.position = newPosition;
    }

private void EndFlightReturnToIdle()
    {
        Debug.Log("Flight complete - creature returning to idle. Press grip or E to dismount.");
        isFlying = false;
        
        // Reset flight path index for potential future rides
        currentFlightWaypointIndex = 0;
        splineT = 0f;
        
        // Return creature to idle position and rotation
        transform.position = idleBasePosition;
        transform.rotation = Quaternion.Euler(60.9f, 0f, 0f); // Original idle rotation
        
        // Resume idle bobbing
        startIdle = true;
    }

    
    // Catmull-Rom spline interpolation - creates smooth curves through points
    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }
    
    private void SendHapticPulse()
    {
        if (leftController != null)
        {
            leftController.SendHapticImpulse(hapticIntensity, hapticDuration);
        }
        
        if (rightController != null)
        {
            rightController.SendHapticImpulse(hapticIntensity, hapticDuration);
        }
    }
    
    private void DismountCreature()
    {
        if (!isPlayerMounted || isTransitioning) return;
        
        StartCoroutine(DismountTransition());
    }
    
    private System.Collections.IEnumerator DismountTransition()
    {
        isTransitioning = true;
        
        // Unparent and move player to safe dismount position
        if (xrOrigin != null)
        {
            // Unparent first
            xrOrigin.SetParent(originalParent);
            
            Vector3 dismountPosition = transform.position + Vector3.up * 2f + Vector3.forward * 2f;
            
            // Smoothly move player to dismount position
            float elapsed = 0f;
            Vector3 startPos = xrOrigin.position;
            
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / transitionDuration;
                t = t * t * (3f - 2f * t);
                
                xrOrigin.position = Vector3.Lerp(startPos, dismountPosition, t);
                yield return null;
            }
            
            xrOrigin.position = dismountPosition;
        }
        
        // Re-enable jetpack
        if (jetpackController != null)
            jetpackController.enabled = true;
        
        // Re-enable locomotion providers (joystick movement)
        if (moveProvider != null)
            moveProvider.enabled = true;
        if (turnProvider != null)
            turnProvider.enabled = true;
        if (snapTurnProvider != null)
            snapTurnProvider.enabled = true;
        
        // Show UI canvas again
        if (uiCanvas != null)
            uiCanvas.enabled = true;
        
        isPlayerMounted = false;
        isTransitioning = false;
        
        OnPlayerDismounted?.Invoke();
        Debug.Log("Player dismounted creature!");
        
        // Return creature to start or resume patrol
        if (returnToStartAfterDismount)
        {
            StartCoroutine(ReturnToStart());
        }
        else if (patrolScript != null)
        {
            patrolScript.enabled = true;
        }
    }
    
    private System.Collections.IEnumerator ReturnToStart()
    {
        float elapsed = 0f;
        float returnDuration = 3f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / returnDuration;
            t = t * t * (3f - 2f * t);
            
            transform.position = Vector3.Lerp(startPos, startPosition, t);
            transform.rotation = Quaternion.Slerp(startRot, startRotation, t);
            
            yield return null;
        }
        
        transform.position = startPosition;
        transform.rotation = startRotation;
        
        // Resume patrol
        if (patrolScript != null)
            patrolScript.enabled = true;
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw mount radius
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, mountDistance);
        
        // Draw seat position
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.TransformPoint(seatOffset), 0.3f);
        
        // Draw flight path
        if (flightPathWaypoints.Count > 0)
        {
            Gizmos.color = Color.magenta;
            for (int i = 0; i < flightPathWaypoints.Count; i++)
            {
                if (flightPathWaypoints[i] == null) continue;
                
                Gizmos.DrawWireSphere(flightPathWaypoints[i].position, 1f);
                
                if (i > 0 && flightPathWaypoints[i - 1] != null)
                {
                    Gizmos.DrawLine(flightPathWaypoints[i - 1].position, flightPathWaypoints[i].position);
                }
            }
            
            // Line from creature to first waypoint
            if (flightPathWaypoints[0] != null)
            {
                Gizmos.color = new Color(1f, 0f, 1f, 0.5f);
                Gizmos.DrawLine(transform.position, flightPathWaypoints[0].position);
            }
        }
    }
}
