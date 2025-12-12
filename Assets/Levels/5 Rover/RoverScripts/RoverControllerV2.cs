using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class RoverControllerV2 : MonoBehaviour
{
    [Header("Rover Configuration")]
    [Tooltip("The maximum base torque applied to the driving wheels in Nm.")]
    public float maxMotorTorque = 300f;
    [Tooltip("The maximum steering angle applied to the steering wheels in degrees.")]
    public float maxSteeringAngle = 30f;
    [Tooltip("The speed at which the steering angle is smoothed (higher = faster turn response).")]
    public float steerSmoothness = 10f;
    [Tooltip("The maximum braking torque applied to all wheels in Nm.")]
    public float maxBrakeTorque = 500f;

    [Header("Boost System (Mapped to 'B' Button)")]
    [Tooltip("Additional torque applied when boost is active.")]
    public float maxBoostTorque = 500f;
    [Tooltip("Total duration of boost charge when full.")]
    public float boostDuration = 2.0f;
    [Tooltip("Rate at which the boost gauge recharges per second (when not boosting).")]
    public float boostRechargeRate = 0.5f;

    [Header("ATV Suspension Bobbing (Visual Feedback)")]
    [Tooltip("Maximum vertical offset for the bobbing effect.")]
    public float bobbingAmplitude = 0.05f;
    [Tooltip("Frequency multiplier for the vertical bobbing (higher = more frantic movement).")]
    public float bobbingFrequency = 15f;

    [Header("Wheel Assignments")]
    [Tooltip("WheelColliders that will be used for steering (e.g., front wheels).")]
    public List<WheelCollider> steeringWheels;
    [Tooltip("WheelColliders that will receive motor torque (e.g., rear or all wheels).")]
    public List<WheelCollider> drivingWheels;
    [Tooltip("Visual meshes associated with the WheelColliders (optional, but highly recommended).")]
    public List<Transform> wheelMeshes;

    [Header("Current Input Values (Set via XR Inputs)")]
    [Tooltip("Input value for steering (-1 = full left, 1 = full right).")]
    [Range(-1f, 1f)]
    public float SteerInput;
    [Tooltip("Input value for acceleration (0 to 1).")]
    [Range(0f, 1f)]
    public float ThrottleInput;
    [Tooltip("Input value for braking (0 to 1).")]
    [Range(0f, 1f)]
    public float BrakeInput;
    [Tooltip("Input value for reverse/deceleration (0 to -1).")]
    [Range(-1f, 0f)]
    public float ReverseInput;
    [Tooltip("Input value for activating the boost (0 = off, 1 = on).")]
    [Range(0f, 1f)]
    public float BoostInput;

    // Internal State
    private Rigidbody rb;
    private const float MIN_REVERSE_SPEED = -0.1f; // Speed threshold to allow reverse
    private float currentBoostCharge = 2.0f; // Starts full
    private float currentSmoothedSteerAngle;
    private Vector3 originalLocalPosition;

    /// <summary>
    /// Normalized value (0 to 1) for UI display of the boost gauge.
    /// </summary>
    public float CurrentBoostChargeNormalized => currentBoostCharge / boostDuration;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Adjust the center of mass to be lower to prevent the rover from tipping over easily.
        rb.centerOfMass = new Vector3(0f, -0.5f, 0f);
        currentBoostCharge = boostDuration; // Initialize boost to full
        // Store the original position of the rover mesh/transform for the bobbing effect
        originalLocalPosition = transform.localPosition;
    }

    void Update()
    {
        // Visual effects (like bobbing) should run in Update for frame-rate consistency
        HandleATVSuspensionBob();
    }

    void FixedUpdate()
    {
        // Physics calculations should be done in FixedUpdate
        HandleBoost();
        HandleMovement();
        UpdateWheelVisuals();
    }

    /// <summary>
    /// Applies a procedural vertical and rotational bobbing effect based on speed.
    /// This gives the visual impression of rugged ATV suspension travel.
    /// </summary>
    private void HandleATVSuspensionBob()
    {
        // Calculate current horizontal speed for bobbing intensity
        Vector3 flatVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        float speed = flatVelocity.magnitude;

        // Base the bobbing intensity on speed (only bob if moving)
        float currentAmplitude = bobbingAmplitude * Mathf.Clamp01(speed / 5f);
        float currentFrequency = bobbingFrequency * Mathf.Clamp01(speed / 5f);

        // Calculate the vertical offset using a sine wave
        float verticalOffset = Mathf.Sin(Time.time * currentFrequency) * currentAmplitude;

        // Apply the offset to the local position's Y axis
        // We restore the original X and Z position to prevent drifting
        transform.localPosition = new Vector3(
            originalLocalPosition.x,
            originalLocalPosition.y + verticalOffset,
            originalLocalPosition.z
        );

        // Optional: Apply a slight rotational tilt based on steering input
        float maxTilt = 2.0f; // Max degrees of tilt
        float targetTilt = SteerInput * maxTilt;

        // Smooth the tilt using Lerp
        Quaternion targetRotation = Quaternion.Euler(
            transform.localRotation.eulerAngles.x,
            transform.localRotation.eulerAngles.y,
            -targetTilt // Negative Z-axis rotation tilts the rover sideways
        );

        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * 5f);
    }

    /// <summary>
    /// Manages the boost charge, consumption, and recharge cycle.
    /// </summary>
    private void HandleBoost()
    {
        // Boost is active if input is high AND charge is greater than zero
        bool isBoosting = BoostInput > 0.5f && currentBoostCharge > 0;

        if (isBoosting)
        {
            // Consume boost
            currentBoostCharge -= Time.deltaTime;
        }
        else
        {
            // Recharge boost, but don't exceed max duration
            currentBoostCharge = Mathf.Min(boostDuration, currentBoostCharge + Time.deltaTime * boostRechargeRate);
        }

        // Clamp to valid range
        currentBoostCharge = Mathf.Clamp(currentBoostCharge, 0f, boostDuration);
    }

    /// <summary>
    /// Applies motor torque, steering, and braking forces to the WheelColliders.
    /// </summary>
    private void HandleMovement()
    {
        float combinedMotorInput = ThrottleInput + ReverseInput;
        bool isBoosting = BoostInput > 0.5f && currentBoostCharge > 0;

        // Calculate the total motor torque, adding boost if active
        float motorPower = combinedMotorInput * maxMotorTorque;
        if (isBoosting)
        {
            motorPower += maxBoostTorque; // Additive boost torque
        }

        // --- 1. Steering (Smoothed) ---
        float targetSteeringAngle = SteerInput * maxSteeringAngle;

        // Smoothly move the current steering angle towards the target input
        currentSmoothedSteerAngle = Mathf.Lerp(
            currentSmoothedSteerAngle,
            targetSteeringAngle,
            Time.deltaTime * steerSmoothness
        );

        foreach (WheelCollider wheel in steeringWheels)
        {
            wheel.steerAngle = currentSmoothedSteerAngle;
        }

        // --- 2. Braking Logic ---
        float currentSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        float currentBrakeTorque = BrakeInput * maxBrakeTorque;

        // Auto-brake/Handbrake logic when switching directions
        if ((combinedMotorInput > 0 && currentSpeed < MIN_REVERSE_SPEED) ||
             (combinedMotorInput < 0 && currentSpeed > 0.1f))
        {
            // Stronger brake to stop quickly before switching direction
            currentBrakeTorque = Mathf.Max(currentBrakeTorque, maxBrakeTorque * 2.5f);
        }
        else if (combinedMotorInput == 0 && BrakeInput == 0)
        {
            // Apply a small brake to simulate rolling resistance/parking brake when idle
            currentBrakeTorque = Mathf.Max(currentBrakeTorque, 100f);
        }

        // --- 3. Motor Torque and Final Application ---
        foreach (WheelCollider wheel in drivingWheels)
        {
            // Apply braking force
            wheel.brakeTorque = currentBrakeTorque;

            // Apply motor torque only if the brake is not fully active.
            if (BrakeInput < 0.1f) // Allowing a slight input tolerance
            {
                wheel.motorTorque = motorPower;
            }
            else
            {
                wheel.motorTorque = 0;
            }
        }
    }

    /// <summary>
    /// Updates the position and rotation of the visual wheel meshes to match the WheelColliders.
    /// </summary>
    private void UpdateWheelVisuals()
    {
        // Simple validation to ensure we don't hit an index error if lists aren't matched
        List<WheelCollider> allWheels = new List<WheelCollider>();
        allWheels.AddRange(steeringWheels);
        allWheels.AddRange(drivingWheels);

        for (int i = 0; i < wheelMeshes.Count && i < allWheels.Count; i++)
        {
            WheelCollider wheel = allWheels[i];
            Transform mesh = wheelMeshes[i];

            if (wheel != null && mesh != null)
            {
                Vector3 pos;
                Quaternion rot;
                wheel.GetWorldPose(out pos, out rot);
                mesh.position = pos;
                mesh.rotation = rot;
            }
        }
    }
}
