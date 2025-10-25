using UnityEngine;
using UnityEngine.XR;
using InputDevice = UnityEngine.XR.InputDevice;

/// <summary>
/// FULLY AUTOMATIC VR Jetpack Controller
/// No setup required - just attach and fly!
/// Hold BOTH grip buttons with arms down to fly upward!
/// </summary>
public class AutoJetpackController : MonoBehaviour
{
    [Header("Flight Settings")]
    [SerializeField] private float thrustForce = 15f;
    [SerializeField] private float maxUpwardVelocity = 10f;
    [SerializeField] private float armDownAngleThreshold = 90f;
    
    [Header("Air Resistance / Momentum")]
    [Tooltip("How quickly you slow down when not thrusting (lower = glide longer)")]
    [SerializeField] private float airDrag = 2f;
    
    [Tooltip("How quickly you slow down on the ground")]
    [SerializeField] private float groundDrag = 4f;
    
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
        
        // Auto-find controllers
        AutoFindControllers();
        
        // Initialize XR devices
        InitializeXRDevices();
        
        Debug.Log($"AutoJetpack Ready! Controllers found: Left={leftControllerTransform != null}, Right={rightControllerTransform != null}");
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
        ApplyMovement();
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
            Debug.Log($"[AutoJetpack] Left Grip: {leftGripPressed}, Right Grip: {rightGripPressed}, Arms Down: {armsDown}, Flying: {isFlying}");
        }
        
        // Activate jetpack
        bool shouldFly = bothGripsPressed && armsDown;
        
        if (shouldFly != isFlying)
        {
            isFlying = shouldFly;
            Debug.Log($"<color=cyan>★★★ Jetpack {(isFlying ? "ACTIVATED" : "DEACTIVATED")} ★★★</color>");
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
