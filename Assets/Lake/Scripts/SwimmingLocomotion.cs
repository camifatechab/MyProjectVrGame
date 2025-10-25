using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class SwimmingLocomotion : MonoBehaviour
{
    [Header("References")]
    public XRNode leftControllerNode = XRNode.LeftHand;
    public XRNode rightControllerNode = XRNode.RightHand;
    public CharacterController characterController;
    
    [Header("Swimming Settings")]
    [Tooltip("Base swimming speed - higher = faster forward movement")]
    public float swimSpeed = 5.0f;
    
    [Tooltip("Surface swimming speed for freestyle")]
    public float surfaceSwimSpeed = 7.0f;
    
    [Tooltip("How fast you swim upward when pulling arms down")]
    public float upwardSwimSpeed = 6.0f;
    
    [Tooltip("How fast you dive down when pushing arms up")]
    public float downwardSwimSpeed = 5.5f;
    
    [Tooltip("Speed multiplier for frog-style forward swimming")]
    public float frogSwimSpeed = 6.5f;
    
    [Tooltip("Side-to-side movement speed")]
    public float strafeSpeed = 3.5f;
    
    [Tooltip("Minimum arm speed to register movement (lower = more sensitive)")]
    public float minArmSpeed = 0.12f;
    
    [Tooltip("How quickly movement responds to arm motion")]
    public float responseSpeed = 12.0f;
    
    [Tooltip("Drag when not moving arms (lower = drift more)")]
    public float waterDrag = 3.0f;
    
    [Tooltip("Maximum swim speed")]
    public float maxSwimSpeed = 10.0f;
    
    [Header("Surface Detection")]
    [Tooltip("Distance from water surface to enable freestyle swimming")]
    public float surfaceThreshold = 0.5f;
    
    [Tooltip("Y position of water surface")]
    public float waterSurfaceY = 10f;
    
    [Header("Boost Settings")]
    [Tooltip("Boost speed when both arms pull back quickly")]
    public float boostSpeed = 15f;
    
    [Tooltip("Minimum arm velocity to trigger boost")]
    public float boostThreshold = 1.5f;
    
    [Tooltip("Boost duration")]
    public float boostDuration = 0.8f;
    
    [Header("State")]
    public bool isSwimmingEnabled = false;
    
    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 targetVelocity = Vector3.zero;
    private bool isNearSurface = false;
    private bool isBoostActive = false;
    private float boostTimer = 0f;
    
    void Start()
    {
        // Auto-find CharacterController if not assigned
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }
    }
    
    void Update()
    {
        if (!isSwimmingEnabled)
        {
            return;
        }
        
        CheckSurfaceProximity();
        UpdateBoost();
        
        // Get controller velocities in world space
        Vector3 leftVelocity = GetControllerVelocity(leftControllerNode);
        Vector3 rightVelocity = GetControllerVelocity(rightControllerNode);
        
        // Check for boost (both arms pulling back fast)
        CheckForBoost(leftVelocity, rightVelocity);
        
        // Average both arms for combined movement
        Vector3 combinedArmVelocity = (leftVelocity + rightVelocity) * 0.5f;
        
        // Calculate swimming force
        Vector3 swimForce;
        if (isNearSurface)
        {
            swimForce = CalculateSurfaceSwimming(combinedArmVelocity);
        }
        else
        {
            swimForce = CalculateUnderwaterSwimming(combinedArmVelocity);
        }
        
        // Apply boost if active
        if (isBoostActive)
        {
            Transform cameraTransform = Camera.main.transform;
            Vector3 boostDirection = cameraTransform.forward;
            if (isNearSurface)
            {
                boostDirection.y = 0;
                boostDirection.Normalize();
            }
            swimForce += boostDirection * boostSpeed;
        }
        
        // Smooth movement response
        targetVelocity = swimForce;
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, Time.deltaTime * responseSpeed);
        
        // Apply water drag
        currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, Time.deltaTime * waterDrag);
        
        // Clamp to max speed
        if (currentVelocity.magnitude > maxSwimSpeed)
        {
            currentVelocity = currentVelocity.normalized * maxSwimSpeed;
        }
        
        // Move the character
        if (characterController != null && characterController.enabled)
        {
            characterController.Move(currentVelocity * Time.deltaTime);
        }
    }
    
void CheckSurfaceProximity()
    {
        // Check if player is NEAR the surface (above water, within threshold)
        float distanceToSurface = transform.position.y - waterSurfaceY;
        // Only enable surface mode when above water or very close to it (not deep underwater)
        isNearSurface = distanceToSurface > -0.3f && distanceToSurface < surfaceThreshold;
    }
    
    void CheckForBoost(Vector3 leftVel, Vector3 rightVel)
    {
        if (isBoostActive) return;
        
        Transform cameraTransform = Camera.main.transform;
        
        // Check if both hands are pulling back fast (freestyle stroke boost)
        float leftBackwardPull = -Vector3.Dot(leftVel, cameraTransform.forward);
        float rightBackwardPull = -Vector3.Dot(rightVel, cameraTransform.forward);
        
        // Both arms pulling back at boost threshold
        if (leftBackwardPull > boostThreshold && rightBackwardPull > boostThreshold)
        {
            ActivateBoost();
        }
    }
    
    void ActivateBoost()
    {
        isBoostActive = true;
        boostTimer = boostDuration;
    }
    
    void UpdateBoost()
    {
        if (isBoostActive)
        {
            boostTimer -= Time.deltaTime;
            if (boostTimer <= 0)
            {
                isBoostActive = false;
            }
        }
    }
    
    Vector3 GetControllerVelocity(XRNode node)
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        
        if (device.isValid)
        {
            Vector3 velocity;
            if (device.TryGetFeatureValue(CommonUsages.deviceVelocity, out velocity))
            {
                return velocity;
            }
        }
        
        return Vector3.zero;
    }
    
    Vector3 CalculateSurfaceSwimming(Vector3 armVelocity)
    {
        // Freestyle swimming on the surface - horizontal movement only
        if (armVelocity.magnitude < minArmSpeed)
        {
            return Vector3.zero;
        }
        
        Vector3 swimForce = Vector3.zero;
        Transform cameraTransform = Camera.main.transform;
        
        // Horizontal forward/backward movement
        Vector3 horizontalForward = cameraTransform.forward;
        horizontalForward.y = 0;
        horizontalForward.Normalize();
        
        // Backward arm pull for forward freestyle swimming
        float backwardPull = -Vector3.Dot(armVelocity, cameraTransform.forward);
        if (backwardPull > 0)
        {
            swimForce += horizontalForward * backwardPull * surfaceSwimSpeed;
        }
        
        // Forward push can also move forward (breaststroke)
        float forwardPush = Vector3.Dot(armVelocity, cameraTransform.forward);
        if (forwardPush > 0.3f)
        {
            swimForce += horizontalForward * forwardPush * surfaceSwimSpeed * 0.7f;
        }
        
        // Left/right strafing on surface
        float sideMovement = Vector3.Dot(armVelocity, cameraTransform.right);
        Vector3 horizontalRight = cameraTransform.right;
        horizontalRight.y = 0;
        horizontalRight.Normalize();
        swimForce += horizontalRight * sideMovement * strafeSpeed;
        
        // Slight upward/downward adjustment (treading water)
        float armDownMotion = -armVelocity.y;
        if (armDownMotion > 0.2f)
        {
            swimForce += Vector3.up * armDownMotion * 2f;
        }
        
        return swimForce;
    }
    
    Vector3 CalculateUnderwaterSwimming(Vector3 armVelocity)
    {
        // Full 3D underwater swimming
        if (armVelocity.magnitude < minArmSpeed)
        {
            return Vector3.zero;
        }
        
        Vector3 swimForce = Vector3.zero;
        Transform cameraTransform = Camera.main.transform;
        
        // === FROG-STYLE / BREASTSTROKE SWIMMING ===
        float forwardPush = Vector3.Dot(armVelocity, cameraTransform.forward);
        if (forwardPush > 0.3f)
        {
            swimForce += cameraTransform.forward * forwardPush * frogSwimSpeed;
        }
        
        // === FREESTYLE BACKWARD PULL ===
        float backwardPull = -Vector3.Dot(armVelocity, cameraTransform.forward);
        if (backwardPull > 0)
        {
            swimForce += cameraTransform.forward * backwardPull * swimSpeed;
        }
        
        // === UP/DOWN SWIMMING ===
        float armDownMotion = -armVelocity.y;
        
        if (armDownMotion > 0.2f)
        {
            // Pulling down = swim up
            swimForce += Vector3.up * armDownMotion * upwardSwimSpeed;
        }
        else if (armDownMotion < -0.2f)
        {
            // Pushing up = dive down
            swimForce += Vector3.up * armDownMotion * downwardSwimSpeed;
        }
        
        // === LEFT/RIGHT STRAFING ===
        float sideMovement = Vector3.Dot(armVelocity, cameraTransform.right);
        swimForce += cameraTransform.right * sideMovement * strafeSpeed;
        
        return swimForce;
    }
    
    public void EnableSwimming(bool enable)
    {
        isSwimmingEnabled = enable;
        
        if (!enable)
        {
            // Reset velocity when exiting water
            currentVelocity = Vector3.zero;
            targetVelocity = Vector3.zero;
            isBoostActive = false;
        }
        
        Debug.Log("Swimming locomotion " + (enable ? "enabled" : "disabled"));
    }
    
    // Public getters for debugging
    public bool IsNearSurface() => isNearSurface;
    public bool IsBoostActive() => isBoostActive;
    public float GetCurrentSpeed() => currentVelocity.magnitude;
    public string GetSwimmingMode() => isNearSurface ? "Freestyle (Surface)" : "Underwater";
}
