using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleSwimmingMovement : MonoBehaviour
{
    [Header("Swimming Settings")]
    [SerializeField] private float underwaterSpeed = 5f;
    [SerializeField] private float surfaceSpeed = 8f;
    [SerializeField] private float sprintMultiplier = 1.8f;
    [SerializeField] private float verticalSpeed = 3.5f;
    
    [Header("Physics Settings")]
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float maxSpeed = 12f;
    [SerializeField] private AnimationCurve accelerationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Surface Detection")]
    [SerializeField] private float surfaceThreshold = 0.5f; // Distance from water surface to enable freestyle
    [SerializeField] private LayerMask waterLayer;
    [SerializeField] private float waterSurfaceY = 10f; // Y position of water surface
    
    [Header("Boost Settings")]
    [SerializeField] private float boostSpeed = 15f;
    [SerializeField] private float boostDuration = 1.5f;
    [SerializeField] private float boostCooldown = 3f;
    [SerializeField] private KeyCode boostKey = KeyCode.LeftControl;
    
    [Header("Camera Effects")]
    [SerializeField] private float maxCameraRoll = 5f;
    [SerializeField] private float cameraRollSpeed = 3f;
    
    private Transform playerTransform;
    private Transform cameraTransform;
    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 targetVelocity = Vector3.zero;
    private float currentSpeed = 0f;
    private bool isBoostActive = false;
    private float boostTimer = 0f;
    private float boostCooldownTimer = 0f;
    private float currentCameraRoll = 0f;
    private bool isNearSurface = false;
    
    private void Awake()
    {
        playerTransform = transform;
        
        // Find the main camera
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraTransform = mainCamera.transform;
        }
    }
    
    private void Update()
    {
        UpdateBoostTimer();
        CheckSurfaceProximity();
        HandleMovement();
        ApplyCameraEffects();
    }
    
    private void CheckSurfaceProximity()
    {
        // Check if player is near the water surface
        float distanceToSurface = Mathf.Abs(playerTransform.position.y - waterSurfaceY);
        isNearSurface = distanceToSurface < surfaceThreshold;
    }
    
    private void UpdateBoostTimer()
    {
        if (isBoostActive)
        {
            boostTimer -= Time.deltaTime;
            if (boostTimer <= 0)
            {
                isBoostActive = false;
            }
        }
        
        if (boostCooldownTimer > 0)
        {
            boostCooldownTimer -= Time.deltaTime;
        }
    }
    
    private void HandleMovement()
    {
        Vector3 inputDirection = Vector3.zero;
        bool isSprinting = false;
        
        // Get keyboard input
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null)
        {
            // Forward/Backward (W/S)
            if (keyboard.wKey.isPressed)
            {
                if (isNearSurface)
                {
                    // Freestyle swimming on surface - horizontal forward movement
                    Vector3 forward = cameraTransform.forward;
                    forward.y = 0; // Keep horizontal
                    inputDirection += forward.normalized;
                }
                else
                {
                    // Underwater swimming - follow camera direction
                    inputDirection += cameraTransform.forward;
                }
            }
            if (keyboard.sKey.isPressed)
            {
                if (isNearSurface)
                {
                    Vector3 forward = cameraTransform.forward;
                    forward.y = 0;
                    inputDirection -= forward.normalized;
                }
                else
                {
                    inputDirection -= cameraTransform.forward;
                }
            }
            
            // Left/Right (A/D)
            if (keyboard.aKey.isPressed)
            {
                inputDirection -= cameraTransform.right;
            }
            if (keyboard.dKey.isPressed)
            {
                inputDirection += cameraTransform.right;
            }
            
            // Up/Down (Space/Shift) - disabled when near surface for freestyle
            if (!isNearSurface)
            {
                if (keyboard.spaceKey.isPressed)
                {
                    inputDirection += Vector3.up;
                }
                if (keyboard.leftShiftKey.isPressed)
                {
                    inputDirection -= Vector3.up;
                }
            }
            else
            {
                // On surface, Space makes you jump/breach slightly
                if (keyboard.spaceKey.wasPressedThisFrame)
                {
                    currentVelocity += Vector3.up * 3f;
                }
            }
            
            // Sprint (Left Shift when not going down)
            if (keyboard.leftShiftKey.isPressed && !keyboard.wKey.isPressed)
            {
                isSprinting = false;
            }
            else if (keyboard.leftShiftKey.isPressed)
            {
                isSprinting = true;
            }
            
            // Boost (Left Control)
            if (keyboard.leftCtrlKey.wasPressedThisFrame && boostCooldownTimer <= 0 && !isBoostActive)
            {
                ActivateBoost();
            }
        }
        
        // Normalize input to prevent faster diagonal movement
        if (inputDirection.magnitude > 1f)
        {
            inputDirection.Normalize();
        }
        
        // Calculate target velocity
        float baseSpeed = isNearSurface ? surfaceSpeed : underwaterSpeed;
        
        if (isBoostActive)
        {
            baseSpeed = boostSpeed;
        }
        else if (isSprinting)
        {
            baseSpeed *= sprintMultiplier;
        }
        
        targetVelocity = inputDirection * baseSpeed;
        
        // Smooth acceleration/deceleration
        if (inputDirection.magnitude > 0.1f)
        {
            // Accelerating
            float accelFactor = accelerationCurve.Evaluate(currentSpeed / maxSpeed);
            currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, 
                acceleration * accelFactor * Time.deltaTime);
        }
        else
        {
            // Decelerating
            currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, 
                deceleration * Time.deltaTime);
        }
        
        // Clamp to max speed
        if (currentVelocity.magnitude > maxSpeed)
        {
            currentVelocity = currentVelocity.normalized * maxSpeed;
        }
        
        // Apply movement
        if (currentVelocity.magnitude > 0.01f)
        {
            playerTransform.Translate(currentVelocity * Time.deltaTime, Space.World);
        }
        
        currentSpeed = currentVelocity.magnitude;
    }
    
    private void ActivateBoost()
    {
        isBoostActive = true;
        boostTimer = boostDuration;
        boostCooldownTimer = boostCooldown;
        
        // Add immediate forward boost
        Vector3 boostDirection = cameraTransform.forward;
        if (isNearSurface)
        {
            boostDirection.y = 0;
            boostDirection.Normalize();
        }
        currentVelocity += boostDirection * boostSpeed * 0.5f;
    }
    
    private void ApplyCameraEffects()
    {
        if (cameraTransform == null) return;
        
        // Calculate desired camera roll based on strafe direction
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        float targetRoll = 0f;
        
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed)
            {
                targetRoll = maxCameraRoll;
            }
            else if (keyboard.dKey.isPressed)
            {
                targetRoll = -maxCameraRoll;
            }
        }
        
        // Smooth camera roll
        currentCameraRoll = Mathf.Lerp(currentCameraRoll, targetRoll, Time.deltaTime * cameraRollSpeed);
        
        // Apply roll to camera
        Vector3 currentEuler = cameraTransform.localEulerAngles;
        cameraTransform.localEulerAngles = new Vector3(
            currentEuler.x,
            currentEuler.y,
            currentCameraRoll
        );
    }
    
    // Public methods for debugging
    public bool IsNearSurface() => isNearSurface;
    public bool IsBoostActive() => isBoostActive;
    public float GetCurrentSpeed() => currentSpeed;
    public string GetSwimmingMode() => isNearSurface ? "Freestyle (Surface)" : "Underwater";
}