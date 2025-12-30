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
    
    [Header("Platform Parking")]
    [Tooltip("Tag used to identify landing platforms")]
    public string platformTag = "LandingPlatform";
    
    [Tooltip("Maximum distance to search for platforms")]
    public float platformSearchRadius = 200f;
    
    [Tooltip("Height offset above platform for landing")]
    public float landingHeightOffset = 3f;
    
    [Tooltip("Speed when flying to platform")]
    public float parkingSpeed = 20f;
    
    [Header("Audio")]
    [Tooltip("Reference to CreatureAudioManager - will be auto-created if not assigned")]
    public CreatureAudioManager audioManager;
    
    
[Header("References")]
    public Transform playerCamera;
    public Transform xrOrigin;
    
    // Runtime state
    private bool isPlayerMounted = false;
    private bool isFlying = false;
    private bool isTransitioning = false;
    private bool isParking = false;
        private Transform targetPlatform;
    private Vector3 landingPosition;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private FlyingCreaturePatrol patrolScript;
    private MonoBehaviour jetpackController;
    private float hapticTimer;
    private int currentFlightWaypointIndex = 0;
    private bool isReversePath = false;
    private float splineT = 0f;
    
    // XR Controllers for haptics
    private ActionBasedController leftController;
    private ActionBasedController rightController;
    
    // XR Input Devices for direct grip detection
    private InputDevice leftDevice;
    private InputDevice rightDevice;
    private bool wasGripPressed = false;
    private float dismountCooldown = 0f;
    private const float DISMOUNT_COOLDOWN_TIME = 1.5f;

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
    private Quaternion mountedRotation;
    
    public bool IsPlayerMounted => isPlayerMounted;
    public bool IsFlying => isFlying;
    public bool IsParking => isParking;
    public int CurrentWaypointIndex => currentFlightWaypointIndex;
    public int TotalWaypoints => flightPathWaypoints.Count;
    public float FlightProgress => flightPathWaypoints.Count > 1 ? (float)currentFlightWaypointIndex / (flightPathWaypoints.Count - 1) : 0f;
    public bool IsReversePath => isReversePath;
    
    
    // Events
    public System.Action OnPlayerMounted;
    public System.Action OnPlayerDismounted;
    
    private void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        idleBasePosition = transform.position;
        idleBobOffset = Random.Range(0f, Mathf.PI * 2f);
        
        if (startIdle)
        {
            var patrol = GetComponent<FlyingCreaturePatrol>();
            if (patrol != null)
                patrol.enabled = false;
        }
        
        patrolScript = GetComponent<FlyingCreaturePatrol>();
        
        if (xrOrigin == null)
        {
            var origin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (origin != null)
            {
                xrOrigin = origin.transform;
                playerCamera = origin.Camera?.transform;
            }
        }
        
        SetupAudioManager();
        
FindXRControllers();
        FindJetpackController();
        
        moveProvider = FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement.ContinuousMoveProvider>();
        turnProvider = FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning.ContinuousTurnProvider>();
        snapTurnProvider = FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning.SnapTurnProvider>();
        
        var canvases = FindObjectsOfType<Canvas>();
        foreach (var canvas in canvases)
        {
            // Find VR UI Canvas specifically (fuel/gems), not CreatureRideUI
            if (canvas.gameObject.name.Contains("VR UI") || canvas.gameObject.name.Contains("Fuel"))
            {
                uiCanvas = canvas;
                break;
            }
        }

        
        if (flightPathWaypoints.Count == 0)
        {
            AutoFindWaypoints();
        }
    }

    private void SetupAudioManager()
    {
        if (audioManager == null)
        {
            audioManager = GetComponent<CreatureAudioManager>();
            if (audioManager == null)
            {
                audioManager = gameObject.AddComponent<CreatureAudioManager>();
                Debug.Log("<color=cyan>✓ Created CreatureAudioManager</color>");
            }
        }
        Debug.Log("<color=green>✓ CreatureAudioManager Ready!</color>");
    }
    
    
private void AutoFindWaypoints()
    {
        GameObject waypointParent = GameObject.Find("FlightPath");
        if (waypointParent == null)
            waypointParent = GameObject.Find("FlightWaypoints");
        
        if (waypointParent == null)
        {
            Debug.LogWarning("RideableCreature: No FlightPath or FlightWaypoints found in scene!");
            return;
        }
        
        var waypoints = new List<Transform>();
        foreach (Transform child in waypointParent.transform)
        {
            waypoints.Add(child);
        }
        
        waypoints.Sort((a, b) => a.name.CompareTo(b.name));
        
        flightPathWaypoints.Clear();
        flightPathWaypoints.AddRange(waypoints);
        
        Debug.Log($"RideableCreature: Auto-found {flightPathWaypoints.Count} flight waypoints");
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
        
        var leftHandDevices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftHandDevices);
        if (leftHandDevices.Count > 0)
            leftDevice = leftHandDevices[0];
        
        var rightHandDevices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);
        if (rightHandDevices.Count > 0)
            rightDevice = rightHandDevices[0];
    }
    
    private void FindJetpackController()
    {
        if (xrOrigin != null)
        {
            var controllers = xrOrigin.GetComponentsInChildren<MonoBehaviour>();
            foreach (var controller in controllers)
            {
                if (controller.GetType().Name == "AutoJetpackController")
                {
                    jetpackController = controller;
                    return;
                }
            }
        }
    }
    
    private void Update()
    {
        if (isTransitioning) return;
        
        if (!isPlayerMounted && Input.GetKeyDown(KeyCode.M))
        {
            MountCreature();
            return;
        }
        
        if (isPlayerMounted)
        {
            UpdateMountedState();
        }
        else
        {
            {
                CheckForMountInput();
                
                if (startIdle && idleBobAmplitude > 0f)
                {
                    float bob = Mathf.Sin((Time.time * idleBobSpeed) + idleBobOffset) * idleBobAmplitude;
                    transform.position = idleBasePosition + new Vector3(0f, bob, 0f);
                }
            }
        }
    }

    
    private void CheckForMountInput()
    {
        if (playerCamera == null) return;
        
        // Cooldown after dismount to prevent immediate remount
        if (dismountCooldown > 0f)
        {
            dismountCooldown -= Time.deltaTime;
            return;
        }
        
        if (!leftDevice.isValid || !rightDevice.isValid)
            RefreshInputDevices();
        
        float distance = Vector3.Distance(transform.position, playerCamera.position);
        
        if (distance <= mountDistance)
        {
            bool gripPressed = IsGripPressed(leftDevice) || IsGripPressed(rightDevice);
            if (Input.GetKeyDown(KeyCode.E)) gripPressed = true;
            
            if (gripPressed && !wasGripPressed)
            {
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
        var leftHandDevices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftHandDevices);
        if (leftHandDevices.Count > 0)
            leftDevice = leftHandDevices[0];
        
        var rightHandDevices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);
        if (rightHandDevices.Count > 0)
            rightDevice = rightHandDevices[0];
    }
    
    private bool IsGripPressed(InputDevice device)
    {
        if (!device.isValid) return false;
        
        bool gripButton = false;
        if (device.TryGetFeatureValue(CommonUsages.gripButton, out gripButton) && gripButton)
            return true;
        
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
        
        if (xrOrigin != null)
        {
            playerOriginalPosition = xrOrigin.position;
            originalParent = xrOrigin.parent;
        }
        
        if (jetpackController != null)
            jetpackController.enabled = false;
        
        if (moveProvider != null) moveProvider.enabled = false;
        if (turnProvider != null) turnProvider.enabled = false;
        if (snapTurnProvider != null) snapTurnProvider.enabled = false;
        if (uiCanvas != null) uiCanvas.enabled = false;
        if (patrolScript != null) patrolScript.enabled = false;
        
        if (xrOrigin != null)
        {
            xrOrigin.SetParent(transform);
            
            Vector3 targetLocalPos = seatOffset;
            float elapsed = 0f;
            Vector3 startLocalPos = xrOrigin.localPosition;
            
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / transitionDuration;
                t = t * t * (3f - 2f * t);
                
                xrOrigin.localPosition = Vector3.Lerp(startLocalPos, targetLocalPos, t);
                xrOrigin.rotation = Quaternion.Euler(0, xrOrigin.eulerAngles.y, 0);
                yield return null;
            }
            
            xrOrigin.localPosition = targetLocalPos;
            mountedRotation = xrOrigin.rotation;
        }
        
        isPlayerMounted = true;
        isFlying = true;
        
        // Play mount audio
        if (audioManager != null)
        {
            audioManager.PlayMount();
            audioManager.StartFlightAmbient();
        }
        
        if (isReversePath)
            currentFlightWaypointIndex = flightPathWaypoints.Count - 2;
        else
            currentFlightWaypointIndex = 0;
        
        splineT = 0f;
        hapticTimer = 0f;
        lastCreaturePosition = transform.position;
        
        OnPlayerMounted?.Invoke();
        isTransitioning = false;
    }
    
    private void UpdateMountedState()
    {
        if (!leftDevice.isValid || !rightDevice.isValid)
            RefreshInputDevices();
        
        if (xrOrigin != null)
        {
            float rideBob = Mathf.Sin(Time.time * idleBobSpeed) * idleBobAmplitude;
            Vector3 bobbedSeatOffset = seatOffset + new Vector3(0f, rideBob, 0f);
            xrOrigin.localPosition = bobbedSeatOffset;
            float creatureYaw = transform.eulerAngles.y;
            xrOrigin.rotation = Quaternion.Euler(0, creatureYaw, 0);
        }
        
        if (enableHaptics)
        {
            hapticTimer += Time.deltaTime;
            if (hapticTimer >= hapticInterval)
            {
                hapticTimer = 0f;
                SendHapticPulse();
            }
        }
        
        // Handle parking movement (overrides normal flight)
        if (isParking)
        {
            UpdateParkingMovement();
        }
        // Follow flight path if currently flying (and not parking)
        else if (isFlying)
        {
            // Check for grip to trigger parking
            bool gripPressed = IsGripPressed(leftDevice) || IsGripPressed(rightDevice);
            if (Input.GetKeyDown(KeyCode.P)) gripPressed = true;
            
            if (gripPressed && !wasGripPressed)
            {
                TryParkAtNearestPlatform();
            }
            wasGripPressed = gripPressed;
            
            if (flightPathWaypoints.Count > 0 && !isParking)
            {
                FollowFlightPath();
            }
        }
        
        // Only allow dismount when NOT flying and NOT parking
        if (!isFlying && !isParking)
        {
            bool gripPressed = IsGripPressed(leftDevice) || IsGripPressed(rightDevice);
            if (Input.GetKeyDown(KeyCode.E)) gripPressed = true;
            
            if (gripPressed && !wasGripPressed)
            {
                DismountCreature();
            }
            
            wasGripPressed = gripPressed;
        }
    }
    
    private void FollowFlightPath()
    {
        if (flightPathWaypoints.Count < 2)
        {
            EndFlightReturnToIdle();
            return;
        }
        
        int p0, p1, p2, p3;
        
        if (isReversePath)
        {
            p1 = Mathf.Clamp(currentFlightWaypointIndex, 0, flightPathWaypoints.Count - 1);
            p2 = Mathf.Clamp(currentFlightWaypointIndex - 1, 0, flightPathWaypoints.Count - 1);
            p0 = Mathf.Clamp(currentFlightWaypointIndex + 1, 0, flightPathWaypoints.Count - 1);
            p3 = Mathf.Clamp(currentFlightWaypointIndex - 2, 0, flightPathWaypoints.Count - 1);
        }
        else
        {
            p0 = Mathf.Clamp(currentFlightWaypointIndex - 1, 0, flightPathWaypoints.Count - 1);
            p1 = Mathf.Clamp(currentFlightWaypointIndex, 0, flightPathWaypoints.Count - 1);
            p2 = Mathf.Clamp(currentFlightWaypointIndex + 1, 0, flightPathWaypoints.Count - 1);
            p3 = Mathf.Clamp(currentFlightWaypointIndex + 2, 0, flightPathWaypoints.Count - 1);
        }
        
        if (flightPathWaypoints[p1] == null || flightPathWaypoints[p2] == null)
        {
            if (isReversePath)
                currentFlightWaypointIndex--;
            else
                currentFlightWaypointIndex++;
            return;
        }
        
        Vector3 pos0 = flightPathWaypoints[p0] != null ? flightPathWaypoints[p0].position : flightPathWaypoints[p1].position;
        Vector3 pos1 = flightPathWaypoints[p1].position;
        Vector3 pos2 = flightPathWaypoints[p2].position;
        Vector3 pos3 = flightPathWaypoints[p3] != null ? flightPathWaypoints[p3].position : flightPathWaypoints[p2].position;
        
        float clampedT = Mathf.Clamp01(splineT);
        Vector3 newPosition = CatmullRom(pos0, pos1, pos2, pos3, clampedT);
        
        Vector3 direction = (newPosition - transform.position).normalized;
        if (direction.sqrMagnitude > 0.001f)
        {
            Vector3 flatDirection = new Vector3(direction.x, 0, direction.z).normalized;
            if (flatDirection.sqrMagnitude > 0.001f)
            {
                float targetYAngle = Mathf.Atan2(flatDirection.x, flatDirection.z) * Mathf.Rad2Deg;
                float currentY = transform.eulerAngles.y;
                float newY = Mathf.LerpAngle(currentY, targetYAngle, 3f * Time.deltaTime);
                
                float turnDelta = Mathf.DeltaAngle(currentY, targetYAngle);
                float bankAngle = Mathf.Clamp(turnDelta * 0.2f, -20f, 20f);
                
                transform.rotation = Quaternion.Euler(61f, newY, bankAngle);
            }
        }
        
        transform.position = newPosition;
        
        float segmentLength = Vector3.Distance(pos1, pos2);
        if (segmentLength < 0.1f) segmentLength = 0.1f;
        
        float verticalDiff = pos2.y - transform.position.y;
        float speedMultiplier = 1f;
        if (verticalDiff < -5f)
            speedMultiplier = 1.6f;
        else if (verticalDiff > 10f)
            speedMultiplier = 0.7f;
        
        float currentSpeed = rideSpeed * speedMultiplier;
        splineT += (currentSpeed / segmentLength) * Time.deltaTime;
        
        if (splineT >= 1f)
        {
            splineT = splineT - 1f;
            
            if (isReversePath)
            {
                currentFlightWaypointIndex--;
                if (currentFlightWaypointIndex <= 0)
                {
                    EndFlightReturnToIdle();
                    return;
                }
            }
            else
            {
                currentFlightWaypointIndex++;
                if (currentFlightWaypointIndex >= flightPathWaypoints.Count - 1)
                {
                    EndFlightReturnToIdle();
                    return;
                }
            }
        }
    }

    private void EndFlightReturnToIdle()
    {
        isFlying = false;
        isReversePath = !isReversePath;
        idleBasePosition = transform.position;
        startIdle = true;
    }


    #region Platform Parking
    
    private void TryParkAtNearestPlatform()
    {
        Transform nearest = FindNearestPlatform();
        
        if (nearest != null)
        {
            StartParking(nearest);
        }
        else
        {
            Debug.Log("RideableCreature: No landing platforms found within range");
        }
    }
    
    private Transform FindNearestPlatform()
    {
        GameObject[] platforms = GameObject.FindGameObjectsWithTag(platformTag);
        
        if (platforms.Length == 0)
        {
            Debug.LogWarning("RideableCreature: No objects with tag '" + platformTag + "' found. Tag your platforms!");
            return null;
        }
        
        Transform nearest = null;
        float nearestDistance = platformSearchRadius;
        
        foreach (GameObject platform in platforms)
        {
            float distance = Vector3.Distance(transform.position, platform.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = platform.transform;
            }
        }
        
        if (nearest != null)
        {
            Debug.Log("RideableCreature: Found platform '" + nearest.name + "' at " + nearestDistance.ToString("F1") + "m");
        }
        
        return nearest;
    }
    
    private void StartParking(Transform platform)
    {
        isParking = true;
        targetPlatform = platform;
        
        // Find the TOP surface of the platform to land ON it, not inside it
        // Use raycast from above to find the actual walkable surface
        Collider platformCollider = platform.GetComponent<Collider>();
        if (platformCollider != null)
        {
            // Cast a ray from above the platform down to find the surface
            Vector3 rayOrigin = new Vector3(platform.position.x, platformCollider.bounds.max.y + 50f, platform.position.z);
            RaycastHit hit;
            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 100f))
            {
                // Land on the hit surface plus offset for creature height
                landingPosition = hit.point + Vector3.up * landingHeightOffset;
            }
            else
            {
                // Fallback: use platform's Y position (ground level) plus offset
                landingPosition = new Vector3(
                    platform.position.x,
                    platform.position.y + landingHeightOffset,
                    platform.position.z
                );
            }
        }
        else
        {
            landingPosition = platform.position + Vector3.up * landingHeightOffset;
        }
        
        Debug.Log("RideableCreature: Parking at " + targetPlatform.name + " landing Y=" + landingPosition.y);
    }
    
    private void UpdateParkingMovement()
    {
        if (targetPlatform == null)
        {
            isParking = false;
            return;
        }
        
        Vector3 direction = (landingPosition - transform.position);
        float distance = direction.magnitude;
        
        if (distance > 1.5f)
        {
            direction.Normalize();
            transform.position += direction * parkingSpeed * Time.deltaTime;
            
            if (direction.sqrMagnitude > 0.001f)
            {
                Vector3 flatDir = new Vector3(direction.x, 0, direction.z).normalized;
                if (flatDir.sqrMagnitude > 0.001f)
                {
                    float targetYaw = Mathf.Atan2(flatDir.x, flatDir.z) * Mathf.Rad2Deg;
                    float currentYaw = transform.eulerAngles.y;
                    float newYaw = Mathf.LerpAngle(currentYaw, targetYaw, 3f * Time.deltaTime);
                    transform.rotation = Quaternion.Euler(61f, newYaw, 0f);
                }
            }
        }
        else
        {
            CompleteLanding();
        }
    }
    
    private void CompleteLanding()
    {
        Debug.Log("RideableCreature: Landed on " + targetPlatform.name + " - Creature parked, player dismounting");
        
        // Play landing audio
        if (audioManager != null)
        {
            audioManager.PlayLanding();
            audioManager.PlayCallExcited();
            audioManager.StopFlightAmbient();
        }
        
        isParking = false;
        isFlying = false;
        
        // Update idle position to this platform
        idleBasePosition = transform.position;
        startIdle = true;
        
        // Auto-dismount the player onto the platform
        if (isPlayerMounted)
        {
            StartCoroutine(DismountOnPlatform());
        }
        
        targetPlatform = null;
    }
    
    
private System.Collections.IEnumerator DismountOnPlatform()
    {
        isTransitioning = true;
        
        // Dismount player onto the platform
        if (xrOrigin != null)
        {
            Vector3 dismountPos = transform.position + transform.right * 2f;
            dismountPos.y = transform.position.y - landingHeightOffset + 1f;
            
            xrOrigin.SetParent(originalParent);
            
            float elapsed = 0f;
            Vector3 startPos = xrOrigin.position;
            
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / transitionDuration;
                t = t * t * (3f - 2f * t);
                xrOrigin.position = Vector3.Lerp(startPos, dismountPos, t);
                yield return null;
            }
            
            xrOrigin.position = dismountPos;
        }
        
        // Re-enable player controls
        if (jetpackController != null) jetpackController.enabled = true;
        if (moveProvider != null) moveProvider.enabled = true;
        if (turnProvider != null) turnProvider.enabled = true;
        if (snapTurnProvider != null) snapTurnProvider.enabled = true;
        if (uiCanvas != null) uiCanvas.enabled = true;
        
        isPlayerMounted = false;
        dismountCooldown = DISMOUNT_COOLDOWN_TIME;
        
        // Play dismount audio
        if (audioManager != null)
        {
            audioManager.PlayDismount();
        }
        
        OnPlayerDismounted?.Invoke();
        isTransitioning = false;
        
        Debug.Log("RideableCreature: Player dismounted, creature waiting on platform");
    }
    

    

    

    
    #endregion

    
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
            leftController.SendHapticImpulse(hapticIntensity, hapticDuration);
        
        if (rightController != null)
            rightController.SendHapticImpulse(hapticIntensity, hapticDuration);
        
        // Sync wing flap sound with haptic pulse
        if (audioManager != null && audioManager.SyncWingFlapWithHaptics)
        {
            audioManager.PlayWingFlap();
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
        
        if (xrOrigin != null)
        {
            Vector3 dismountPos = transform.position + transform.right * 2f;
            dismountPos.y = transform.position.y;
            
            xrOrigin.SetParent(originalParent);
            
            float elapsed = 0f;
            Vector3 startPos = xrOrigin.position;
            
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / transitionDuration;
                t = t * t * (3f - 2f * t);
                
                xrOrigin.position = Vector3.Lerp(startPos, dismountPos, t);
                yield return null;
            }
            
            xrOrigin.position = dismountPos;
        }
        
        if (jetpackController != null)
            jetpackController.enabled = true;
        
        if (moveProvider != null) moveProvider.enabled = true;
        if (turnProvider != null) turnProvider.enabled = true;
        if (snapTurnProvider != null) snapTurnProvider.enabled = true;
        if (uiCanvas != null) uiCanvas.enabled = true;
        
        isPlayerMounted = false;
        isFlying = false;
        dismountCooldown = DISMOUNT_COOLDOWN_TIME; // Prevent immediate remount
        
        // Play dismount audio
        if (audioManager != null)
        {
            audioManager.PlayDismount();
            audioManager.StopFlightAmbient();
        }

        
        OnPlayerDismounted?.Invoke();
        isTransitioning = false;
    }
}
