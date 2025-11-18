using UnityEngine;

public class SimplePlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float verticalSpeed = 2f;
    
    [Header("Gravity Settings")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private bool applyGravity = true;
    
    private Transform playerTransform;
    private Transform cameraTransform;
    private float verticalVelocity = 0f;
    private CharacterController characterController;
    
private void Awake()
    {
        playerTransform = transform;
        characterController = GetComponent<CharacterController>();
        
        // Fix CharacterController so player stands on ground properly
        if (characterController != null)
        {
            // Set height to 1.8m (average human height)
            characterController.height = 1.8f;
            // Center at half height so bottom touches ground at y=0
            characterController.center = new Vector3(0f, characterController.height / 2f, 0f);
            characterController.radius = 0.3f; // Wider for better stability
        }
        
        // Find the main camera
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraTransform = mainCamera.transform;
        }
    }
    
    private void Update()
    {
        HandleMovement();
    }
    
    private void HandleMovement()
    {
        Vector3 movement = Vector3.zero;
        
        // Get keyboard input
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null)
        {
            // Forward/Backward (W/S)
            if (keyboard.wKey.isPressed)
            {
                movement += cameraTransform.forward;
            }
            if (keyboard.sKey.isPressed)
            {
                movement -= cameraTransform.forward;
            }
            
            // Left/Right (A/D)
            if (keyboard.aKey.isPressed)
            {
                movement -= cameraTransform.right;
            }
            if (keyboard.dKey.isPressed)
            {
                movement += cameraTransform.right;
            }
            
            // Up/Down (Space/Shift) - only when not applying gravity
            if (!applyGravity)
            {
                if (keyboard.spaceKey.isPressed)
                {
                    movement += Vector3.up * verticalSpeed;
                }
                if (keyboard.leftShiftKey.isPressed)
                {
                    movement -= Vector3.up * verticalSpeed;
                }
            }
        }
        
        // Flatten horizontal movement (remove vertical component from forward/right)
        if (applyGravity)
        {
            movement.y = 0f;
        }
        
        // Normalize horizontal movement
        if (movement.magnitude > 0.1f)
        {
            movement = movement.normalized;
        }
        
        // Apply gravity when enabled
        if (applyGravity && characterController != null)
        {
            if (characterController.isGrounded && verticalVelocity < 0)
            {
                verticalVelocity = -2f; // Keep grounded
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }
            
            movement.y = verticalVelocity;
            
            // Move with CharacterController
            characterController.Move(movement * moveSpeed * Time.deltaTime);
        }
        else
        {
            // Free swimming - direct transform movement
            if (movement != Vector3.zero)
            {
                playerTransform.Translate(movement * moveSpeed * Time.deltaTime, Space.World);
            }
        }
    }
    
    // Called by water trigger
    public void EnableSwimming()
    {
        applyGravity = false;
        verticalVelocity = 0f;
        Debug.Log("Swimming mode enabled - Free 3D movement");
    }
    
    // Called by water trigger
    public void DisableSwimming()
    {
        applyGravity = true;
        Debug.Log("Land mode enabled - Gravity applied");
    }
}