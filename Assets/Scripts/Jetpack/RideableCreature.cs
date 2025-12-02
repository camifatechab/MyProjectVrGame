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
    private bool isTransitioning = false;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private FlyingCreaturePatrol patrolScript;
    private MonoBehaviour jetpackController;
    private float hapticTimer;
    private int currentFlightWaypointIndex = 0;
    
    // XR Controllers for haptics
    private ActionBasedController leftController;
    private ActionBasedController rightController;
    
    // XR Input Devices for direct grip detection
    private InputDevice leftDevice;
    private InputDevice rightDevice;
    private bool wasGripPressed = false;
    private Canvas uiCanvas;


    
    // Player state before mounting
    private Vector3 playerOriginalPosition;
    private Transform originalParent;
    private float idleBobOffset;
    private Vector3 idleBasePosition;

    
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
        
        // Hide UI canvas
        if (uiCanvas == null)
        {
            var canvases = FindObjectsOfType<Canvas>();
            foreach (var canvas in canvases)
            {
                if (canvas.gameObject.name.Contains("UI") || canvas.gameObject.name.Contains("Canvas"))
                {
                    uiCanvas = canvas;
                    break;
                }
            }
        }
        if (uiCanvas != null)
            uiCanvas.enabled = false;
        
        // Disable normal patrol
        if (patrolScript != null)
            patrolScript.enabled = false;
        
        // Parent player to creature
        if (xrOrigin != null)
        {
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
                yield return null;
            }
            
            xrOrigin.localPosition = targetLocalPos;
        }
        
        isPlayerMounted = true;
        isTransitioning = false;
        currentFlightWaypointIndex = 0;
        hapticTimer = 0f;
        
        OnPlayerMounted?.Invoke();
        Debug.Log("Player mounted creature!");
    }
    
private void UpdateMountedState()
    {
        // Refresh devices if not valid
        if (!leftDevice.isValid || !rightDevice.isValid)
        {
            RefreshInputDevices();
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
        
        // Follow flight path if defined
        if (flightPathWaypoints.Count > 0)
        {
            FollowFlightPath();
        }
        
        // Check for dismount input using direct grip detection
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
    
    private void FollowFlightPath()
    {
        if (currentFlightWaypointIndex >= flightPathWaypoints.Count)
        {
            // Reached end of flight path
            DismountCreature();
            return;
        }
        
        Transform targetWaypoint = flightPathWaypoints[currentFlightWaypointIndex];
        if (targetWaypoint == null)
        {
            currentFlightWaypointIndex++;
            return;
        }
        
        Vector3 direction = (targetWaypoint.position - transform.position).normalized;
        transform.position += direction * rideSpeed * Time.deltaTime;
        
        // Smooth rotation toward target
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 2f * Time.deltaTime);
        }
        
        // Check if reached waypoint
        float distance = Vector3.Distance(transform.position, targetWaypoint.position);
        if (distance < 3f)
        {
            currentFlightWaypointIndex++;
        }
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
        
        // Unparent player
        if (xrOrigin != null)
        {
            Vector3 dismountPosition = transform.position + Vector3.up * 2f + transform.forward * 2f;
            
            xrOrigin.SetParent(originalParent);
            
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
