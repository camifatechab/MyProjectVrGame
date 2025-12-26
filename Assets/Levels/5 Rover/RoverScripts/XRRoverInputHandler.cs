using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// This script handles reading raw input data from the XR Input System
/// and translating it into the separated input values required by the RoverController.
/// This acts as the bridge between the VR controller actions and the vehicle physics.
/// </summary>
public class XRRoverInputHandler : MonoBehaviour
{
    [Header("Controller Target")]
    [Tooltip("The RoverController script to feed the processed inputs into.")]
    public RoverControllerV2 targetRoverController;

    [Header("Input Action References (Drag Actions Here)")]
    [Tooltip("The Vector2 Action for Joystick movement (X=Steer, Y=Throttle/Reverse).")]
    public InputActionReference moveAction;

    [Tooltip("The Float Action for the Brake Trigger (e.g., Right Hand Grip/Trigger).")]
    public InputActionReference brakeAction;

    [Tooltip("The Button Action for activating Boost (e.g., Primary or Secondary Button like 'B').")]
    public InputActionReference boostAction;

    private void OnEnable()
    {
        // Subscribe to the actions when the script is enabled
        if (moveAction != null) moveAction.action.Enable();
        if (brakeAction != null) brakeAction.action.Enable();
        if (boostAction != null) boostAction.action.Enable();
    }

    private void OnDisable()
    {
        // Unsubscribe from the actions when the script is disabled
        if (moveAction != null) moveAction.action.Disable();
        if (brakeAction != null) brakeAction.action.Disable();
        if (boostAction != null) boostAction.action.Disable();
    }

    void Update()
    {
        if (targetRoverController == null)
        {
            Debug.LogError("XR Rover Input Handler is missing a reference to the Target Rover Controller!");
            return;
        }

        // 1. Handle Steering (X-Axis of Joystick) and Throttle/Reverse (Y-Axis)
        if (moveAction != null && moveAction.action.enabled)
        {
            Vector2 moveValue = moveAction.action.ReadValue<Vector2>();

            // X-Axis = Steer Input
            targetRoverController.SteerInput = moveValue.x;

            // Y-Axis Processing: Split into separate Throttle (forward) and Reverse (backward)
            if (moveValue.y > 0)
            {
                targetRoverController.ThrottleInput = moveValue.y;
                targetRoverController.ReverseInput = 0f;
            }
            else
            {
                targetRoverController.ThrottleInput = 0f;
                targetRoverController.ReverseInput = moveValue.y; // This will be a negative value
            }
        }

        // 2. Handle Braking (Trigger Pressure)
        if (brakeAction != null && brakeAction.action.enabled)
        {
            // Use ReadValue<float>() for the analog trigger press
            targetRoverController.BrakeInput = brakeAction.action.ReadValue<float>();
        }

        // 3. Handle Boost (Button Press)
        if (boostAction != null && boostAction.action.enabled)
        {
            // Use ReadValue<float>() for button press (0 or 1)
            targetRoverController.BoostInput = boostAction.action.ReadValue<float>();
        }
    }
}