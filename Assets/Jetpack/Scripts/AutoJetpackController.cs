using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using InputDevice = UnityEngine.XR.InputDevice;

/// <summary>
/// FULLY AUTOMATIC VR Jetpack Controller
/// No setup required - just attach and fly!
/// Hold BOTH grip buttons with arms down to fly upward!
/// Automatically disables left joystick movement during flight AND when in air!
/// NOW WITH CRYSTAL COLLECTION SUPPORT!
/// </summary>
public class AutoJetpackController : MonoBehaviour
{
    [Header("Flight Settings")]
    [SerializeField] private float thrustForce = 15f;
    [SerializeField] private float maxUpwardVelocity = 10f;
    [SerializeField] private float armDownAngleThreshold = 90f;
    
    
    [Header("Fuel System")]
    [Tooltip("Maximum fuel capacity")]
    [SerializeField] private float maxFuel = 100f;
    
    [Tooltip("Current fuel amount")]
    [SerializeField] private float currentFuel = 100f;
    
    [Tooltip("Fuel consumed per second while flying")]
    [SerializeField] private float fuelConsumptionRate = 10f;
    
    [Tooltip("Fuel percentage when low fuel warning starts")]
    [SerializeField] private float lowFuelThreshold = 25f;
    
    [Tooltip("Bonus fuel efficiency when gliding (multiplier)")]
    [SerializeField] private float glidingEfficiency = 0.5f;
    
    // Fuel system state
    private bool isOutOfFuel = false;
    private bool hasShownLowFuelWarning = false;
    
[Header("Air Resistance / Momentum")]
    [Tooltip("How quickly you slow down when not thrusting (lower = glide longer)")]
    [SerializeField] private float airDrag = 2f;
    
    [Tooltip("How quickly you slow down on the ground")]
    [SerializeField] private float groundDrag = 4f;
    
    [Header("Movement Integration")]
    [Tooltip("Reference to the ground movement controller - will be found automatically if not assigned")]
    [SerializeField] private MonoBehaviour moveProvider;
    
    [Header("Collection Settings")]
    [Tooltip("Radius for crystal collection trigger")]
    [SerializeField] private float collectionRadius = 0.5f;
    
    [Header("Physics")]
    private float gravity = 9.81f;
    
    private CharacterController characterController;
    private Vector3 velocity;
    private bool isFlying = false;
    
    private Transform leftControllerTransform;
    private Transform rightControllerTransform;
    
    // Input devices
    private InputDevice leftDevice;
    private InputDevice rightDevice;
    
    // Collection trigger
    private GameObject collectionTrigger;
    
    // Track previous grounded state
    private bool wasGrounded = true;
    
void Start()
    {
        // Get or add CharacterController
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.3f;
            characterController.center = new Vector3(0, 0.9f, 0);
        }
        
        // Initialize fuel
        currentFuel = maxFuel;
        
        // Create collection trigger
        SetupCollectionTrigger();
        
        // Auto-find controllers
        AutoFindControllers();
        
        // Initialize XR devices
        InitializeXRDevices();
        
        // Auto-find movement provider if not assigned
        if (moveProvider == null)
        {
            GameObject moveObject = GameObject.Find("Move");
            if (moveObject != null)
            {
                moveProvider = moveObject.GetComponent<MonoBehaviour>();
                
                if (moveProvider != null && moveProvider.GetType().Name.Contains("Move"))
                {
                    Debug.Log($"<color=cyan>✓ Found Movement Provider: {moveProvider.GetType().Name} on {moveObject.name}</color>");
                }
            }
            
            if (moveProvider == null)
            {
                Debug.LogWarning("No Movement Provider found. Ground movement won't be disabled during flight.");
            }
        }
        
        Debug.Log($"AutoJetpack Ready! Controllers found: Left={leftControllerTransform != null}, Right={rightControllerTransform != null}");
        Debug.Log($"<color=green>✓ Fuel System Initialized: {currentFuel}/{maxFuel}</color>");
    }
    
    void SetupCollectionTrigger()
    {
        // Create a child GameObject for collection detection
        collectionTrigger = new GameObject("CollectionTrigger");
        collectionTrigger.transform.SetParent(transform);
        collectionTrigger.transform.localPosition = Vector3.zero;
        
        // Add sphere collider for collection
        SphereCollider trigger = collectionTrigger.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = collectionRadius;
        
        // Add the collection component
        collectionTrigger.AddComponent<PlayerCollectionTrigger>();
        
        // Set the MainCamera tag so crystals can detect it
        collectionTrigger.tag = "MainCamera";
        
        Debug.Log($"<color=green>✓ Collection trigger created with radius: {collectionRadius}m</color>");
    }
    
    void AutoFindControllers()
    {
        Transform cameraOffset = transform.Find("Camera Offset");
        if (cameraOffset != null)
        {
            leftControllerTransform = cameraOffset.Find("Left Controller");
            rightControllerTransform = cameraOffset.Find("Right Controller");
            
            if (leftControllerTransform != null)
                Debug.Log($"Found Left Controller: {leftControllerTransform.name}");
            else
                Debug.LogWarning("Could not find Left Controller!");
                
            if (rightControllerTransform != null)
                Debug.Log($"Found Right Controller: {rightControllerTransform.name}");
            else
                Debug.LogWarning("Could not find Right Controller!");
        }
        else
        {
            Debug.LogError("AutoJetpack: Could not find Camera Offset!");
        }
    }
    
    void InitializeXRDevices()
    {
        // Get XR input devices
        var leftHandDevices = new System.Collections.Generic.List<InputDevice>();
        var rightHandDevices = new System.Collections.Generic.List<InputDevice>();
        
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftHandDevices);
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);
        
        if (leftHandDevices.Count > 0)
        {
            leftDevice = leftHandDevices[0];
            Debug.Log($"Left Hand Device: {leftDevice.name}");
        }
        
        if (rightHandDevices.Count > 0)
        {
            rightDevice = rightHandDevices[0];
            Debug.Log($"Right Hand Device: {rightDevice.name}");
        }
    }
    
void Update()
    {
        // Re-initialize devices if they become invalid
        if (!leftDevice.isValid || !rightDevice.isValid)
        {
            InitializeXRDevices();
        }
        
        CheckJetpackActivation();
        UpdateFuelSystem();
        ApplyMovement();
        UpdateGroundMovementState();
    }
    
void CheckJetpackActivation()
    {
        // Try multiple button types to maximize compatibility
        bool leftGripPressed = IsGripPressed(leftDevice);
        bool rightGripPressed = IsGripPressed(rightDevice);
        
        bool bothGripsPressed = leftGripPressed && rightGripPressed;
        
        // Check arm angles
        bool armsDown = AreArmsDown();
        
        // Debug output every second
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[AutoJetpack] Left Grip: {leftGripPressed}, Right Grip: {rightGripPressed}, Arms Down: {armsDown}, Flying: {isFlying}, Fuel: {GetFuelPercentage():F1}%");
        }
        
        // Can't fly if out of fuel!
        bool shouldFly = bothGripsPressed && armsDown && !isOutOfFuel;
        
        if (shouldFly != isFlying)
        {
            isFlying = shouldFly;
            
            if (isFlying)
            {
                Debug.Log($"<color=cyan>★★★ Jetpack ACTIVATED ★★★ Fuel: {GetFuelPercentage():F1}%</color>");
            }
            else
            {
                string reason = isOutOfFuel ? "(OUT OF FUEL)" : "";
                Debug.Log($"<color=cyan>★★★ Jetpack DEACTIVATED ★★★ {reason}</color>");
            }
        }
        
        // Auto-deactivate if fuel runs out mid-flight
        if (isFlying && isOutOfFuel)
        {
            isFlying = false;
            Debug.Log("<color=red>★★★ EMERGENCY SHUTDOWN - NO FUEL! ★★★</color>");
        }
    }


    void UpdateFuelSystem()
    {
        if (isFlying)
        {
            // Calculate fuel consumption
            float consumption = fuelConsumptionRate * Time.deltaTime;
            
            // Bonus efficiency when mostly gliding (not thrusting straight up)
            Vector3 thrustDirection = GetThrustDirection();
            float upwardness = Vector3.Dot(thrustDirection, Vector3.up);
            if (upwardness < 0.7f) // Not thrusting directly upward
            {
                consumption *= glidingEfficiency;
            }
            
            // Consume fuel
            currentFuel -= consumption;
            
            // Check if out of fuel
            if (currentFuel <= 0)
            {
                currentFuel = 0;
                if (!isOutOfFuel)
                {
                    isOutOfFuel = true;
                    Debug.Log("<color=red>⚠ OUT OF FUEL! ⚠</color>");
                }
            }
            
            // Low fuel warning
            float fuelPercentage = (currentFuel / maxFuel) * 100f;
            if (fuelPercentage <= lowFuelThreshold && !hasShownLowFuelWarning)
            {
                hasShownLowFuelWarning = true;
                Debug.Log($"<color=orange>⚠ LOW FUEL WARNING: {fuelPercentage:F1}% remaining! ⚠</color>");
            }
        }
        else
        {
            // Reset warning when not flying and fuel is above threshold
            if (!isFlying && currentFuel > (maxFuel * lowFuelThreshold / 100f))
            {
                hasShownLowFuelWarning = false;
            }
        }
    }
    
    public void RefillFuel(float amount)
    {
        currentFuel = Mathf.Min(currentFuel + amount, maxFuel);
        isOutOfFuel = false;
        Debug.Log($"<color=cyan>✓ Fuel Refilled! Current: {currentFuel:F1}/{maxFuel}</color>");
    }
    
    public void RefillFuelFull()
    {
        currentFuel = maxFuel;
        isOutOfFuel = false;
        hasShownLowFuelWarning = false;
        Debug.Log("<color=green>✓ FUEL TANK FULL!</color>");
    }
    
    // Public getters for UI
    public float GetCurrentFuel() => currentFuel;
    public float GetMaxFuel() => maxFuel;
    public float GetFuelPercentage() => (currentFuel / maxFuel) * 100f;
    public bool IsLowOnFuel() => GetFuelPercentage() <= lowFuelThreshold;
    public bool IsOutOfFuel() => isOutOfFuel;
    public bool IsFlying() => isFlying;

    
    void UpdateGroundMovementState()
    {
        if (moveProvider == null) return;
        
        // CRITICAL FIX: Only enable ground movement when ACTUALLY GROUNDED
        // Disable it when flying OR in the air (falling/gliding)
        bool shouldEnableGroundMovement = characterController.isGrounded && !isFlying;
        
        // Only update if state changed (avoid spam)
        if (moveProvider.enabled != shouldEnableGroundMovement)
        {
            moveProvider.enabled = shouldEnableGroundMovement;
            
            string reason = "";
            if (isFlying)
                reason = "FLYING";
            else if (!characterController.isGrounded)
                reason = "IN AIR";
            else
                reason = "GROUNDED";
            
            Debug.Log($"<color=yellow>Ground Movement {(shouldEnableGroundMovement ? "ENABLED" : "DISABLED")} - {reason}</color>");
        }
        
        // Track grounded state changes
        if (characterController.isGrounded != wasGrounded)
        {
            wasGrounded = characterController.isGrounded;
            Debug.Log($"<color=orange>Ground State: {(wasGrounded ? "LANDED" : "AIRBORNE")}</color>");
        }
    }
    
    bool IsGripPressed(InputDevice device)
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
        
        // Try trigger as fallback
        float trigger = 0f;
        if (device.TryGetFeatureValue(CommonUsages.trigger, out trigger) && trigger > 0.5f)
            return true;
        
        return false;
    }
    
    bool AreArmsDown()
    {
        if (leftControllerTransform == null || rightControllerTransform == null)
            return false;
        
        // Check if controllers are pointing down
        float leftAngle = Vector3.Angle(leftControllerTransform.forward, Vector3.down);
        float rightAngle = Vector3.Angle(rightControllerTransform.forward, Vector3.down);
        
        // Debug output every second
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[AutoJetpack] Arm Angles - Left: {leftAngle:F1}, Right: {rightAngle:F1} (Threshold: {armDownAngleThreshold})");
        }
        
        return leftAngle < armDownAngleThreshold && rightAngle < armDownAngleThreshold;
    }

    Vector3 GetThrustDirection()
    {
        if (leftControllerTransform == null || rightControllerTransform == null)
            return Vector3.up; // Default to up if controllers not found
        
        // Get the forward direction of both controllers
        Vector3 leftDirection = leftControllerTransform.forward;
        Vector3 rightDirection = rightControllerTransform.forward;
        
        // Average the two directions (Iron Man uses both hands!)
        Vector3 averageDirection = (leftDirection + rightDirection).normalized;
        
        // Negate because controller forward points away from palm
        // When palms point down (arms down), we want to go UP
        Vector3 thrustDirection = -averageDirection;
        
        return thrustDirection;
    }

    
    void ApplyMovement()
    {
        if (isFlying)
        {
            // Get the direction from both hands (Iron Man style!)
            Vector3 thrustDirection = GetThrustDirection();
            
            // Apply thrust in the direction hands are pointing
            Vector3 targetVelocity = thrustDirection * maxUpwardVelocity;
            
            // Smoothly accelerate towards target velocity
            velocity = Vector3.MoveTowards(velocity, targetVelocity, thrustForce * Time.deltaTime);
            
            // Debug output when flying
            if (Time.frameCount % 30 == 0)
            {
                Debug.Log($"<color=green>FLYING! Speed: {velocity.magnitude:F2} m/s, Direction: {thrustDirection}, Height: {transform.position.y:F2} m</color>");
            }
        }
        else
        {
            // MOMENTUM PRESERVATION - Keep moving but slow down!
            // Horizontal air resistance (drag)
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, airDrag * Time.deltaTime);
            
            // Apply horizontal velocity back
            velocity.x = horizontalVelocity.x;
            velocity.z = horizontalVelocity.z;
            
            // Apply gravity to vertical movement
            if (!characterController.isGrounded)
            {
                velocity.y -= gravity * Time.deltaTime;
            }
            else
            {
                velocity.y = -2f;
                // Also stop horizontal movement when grounded
                velocity.x = Mathf.MoveTowards(velocity.x, 0, groundDrag * Time.deltaTime);
                velocity.z = Mathf.MoveTowards(velocity.z, 0, groundDrag * Time.deltaTime);
            }
        }
        
        // Move the character in 3D!
        characterController.Move(velocity * Time.deltaTime);
    }
}

/// <summary>
/// Simple component to make the collection trigger work with CrystalCollectible
/// This just needs to exist on the trigger object
/// </summary>
public class PlayerCollectionTrigger : MonoBehaviour
{
    // This component doesn't need any code!
    // It just exists so the trigger can detect crystals
    // The actual collection is handled by CrystalCollectible.OnTriggerEnter()
}
