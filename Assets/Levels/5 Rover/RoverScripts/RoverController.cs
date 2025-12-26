using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

[RequireComponent(typeof(Rigidbody))]
public class RoverController : MonoBehaviour
{
    [Header("Driving Settings")]
    public float acceleration = 15f;
    public float brakeForce = 20f;
    public float maxSpeed = 10f;
    public float turnSpeed = 45f;

    [Header("Input Actions")]
    public InputActionProperty accelerateAction; // Right trigger
    public InputActionProperty brakeAction;      // Left trigger
    public InputActionProperty steerAction;      // Left joystick (Vector2)
    public InputActionProperty boostAction; // Assign to B button

    [Header("Audio")]
    public AudioSource engineSource;
    public AudioSource brakeSource;
    public AudioClip engineLoop;
    public AudioClip brakeClip;
    public AudioClip boostSound;
    [Range(0.5f, 2f)] public float enginePitchMin = 0.8f;
    [Range(0.5f, 2f)] public float enginePitchMax = 1.5f;
    public float fadeSpeed = 2f;
    public float maxEngineVolume = 0.6f;

    public AudioClip mudEnterSound;
    public AudioSource sfxSource; // optional separate source for one-shots

    [Header("Boost Settings")]
    public float boostForce = 30f;
    public float boostDuration = 2f;
    public float boostCooldown = 5f;
    public float boostMeter = 100f;   // 0–100
    public float boostUsageRate = 50f; // how fast it drains
    public float boostRegenPerBattery = 33.3f;

    [Header("Boost UI")]
    public Slider boostSlider;
    public float boostUISmoothSpeed = 3f;
    private float targetBoostValue;
    private bool boosting = false;
    private bool canBoost => boostMeter > 0f;

    private bool wasBoostHeld = false;

    private Rigidbody rb;
    private bool isBraking;
    private float targetEngineVolume;

    [Header("Input Highlight Objects")]
    public Renderer forwardArrow;
    public Renderer backwardArrow;
    public Renderer leftArrow;
    public Renderer rightArrow;
    public Renderer boostButton;
    public Material normalMaterial;
    public Material highlightMaterial;

    // --- TERRAIN SLOWDOWN ---
    /*private float defaultAcceleration;
    private float defaultMaxSpeed;
    private Coroutine terrainResetCoroutine;*/
    [Header("Terrain Slowdown Settings")]
    public float slowMultiplier = 0.5f;   // how much to slow down
    private float terrainMultiplier = 1f; // current terrain speed factor
    private float basePitch = 1f; // Default engine pitch multiplier


    [Header("Visuals")]
    public RoverSuspensionTilt suspensionTilt; // drag the body object here

    public WheelCollider wheelFL;
    public WheelCollider wheelFR;
    public WheelCollider wheelRL;
    public WheelCollider wheelRR;

    public float motorTorque = 1500f;
    public float steerAngle = 35f;

    private float throttleInput;
    private float steeringInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (boostSlider)
        {
            boostSlider.value = boostMeter / 100f;
            targetBoostValue = boostSlider.value;
        }

        if (engineSource && engineLoop)
        {
            engineSource.clip = engineLoop;
            engineSource.loop = true;
            engineSource.volume = 0f; // start silent
            engineSource.Play();
        }
    }

    void FixedUpdate()
    {
        float accel = accelerateAction.action?.ReadValue<float>() ?? 0f;
        float brake = brakeAction.action?.ReadValue<float>() ?? 0f;
        Vector2 steer = steerAction.action?.ReadValue<Vector2>() ?? Vector2.zero;

        throttleInput = accel;
        steeringInput = steer.x;

        ApplyWheelForces(throttleInput, steeringInput);

        // --- Forward acceleration (affected by terrain slowdown)---
        /*if (accel > 0.1f && rb.linearVelocity.magnitude < maxSpeed)
            rb.AddForce(transform.forward * accel * acceleration, ForceMode.Acceleration);*/
        if (accel > 0.1f && rb.linearVelocity.magnitude < maxSpeed * terrainMultiplier)
            rb.AddForce(transform.forward * accel * acceleration * terrainMultiplier, ForceMode.Acceleration);

        // --- Brake or Reverse ---
        if (brake > 0.1f)
        {
            if (rb.linearVelocity.magnitude > 0.5f)
                rb.AddForce(-transform.forward * brake * brakeForce, ForceMode.Acceleration); // Brake
            else
                rb.AddForce(-transform.forward * brake * acceleration, ForceMode.Acceleration); // Reverse

            if (!isBraking && brakeSource && brakeClip)
            {
                brakeSource.PlayOneShot(brakeClip);
                isBraking = true;
            }
        }
        else isBraking = false;

        // --- Steering ---
        float turn = steer.x * turnSpeed * Time.fixedDeltaTime;
        transform.Rotate(0f, turn, 0f);

        // after calculating accel, steer, and updating rb:
        if (suspensionTilt != null)
        {
            // steer.x is -1..1 from the left stick (already used for turning)
            suspensionTilt.turnInput = steer.x;

            // accel is 0..1 for forward; brake is 0..1 for braking
            // we want accelInput in -1..+1 where positive = accelerate, negative = braking
            float accelInputValue = 0f;
            if (accel > 0.1f) accelInputValue = Mathf.Clamp01(accel);     // forward
            else if (brake > 0.1f) accelInputValue = -Mathf.Clamp01(brake); // braking as negative

            suspensionTilt.accelInput = accelInputValue;

            // speed: horizontal speed magnitude (ignore vertical)
            suspensionTilt.speed = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
        }

        // --- Featherable Boost ---
        bool boostHeld = /*(Keyboard.current != null && Keyboard.current.bKey.isPressed) || */boostAction.action.IsPressed();

        if (boostHeld && canBoost)
        {
            boosting = true;
            rb.AddForce(transform.forward * boostForce, ForceMode.Acceleration);
            boostMeter -= boostUsageRate * Time.fixedDeltaTime;
            boostMeter = Mathf.Max(boostMeter, 0f);
        }
        else boosting = false;

        UpdateBoostUI();

        // --- Engine Sound ---
        if (engineSource)
        {
            float speedPercent = Mathf.Clamp01(rb.linearVelocity.magnitude / maxSpeed);

            // Lower pitch slightly based on terrain slowdown
            float terrainPitchFactor = Mathf.Lerp(0.6f, 1f, terrainMultiplier);

            // Base pitch adjusted by terrain factor
            engineSource.pitch = Mathf.Lerp(enginePitchMin, enginePitchMax, speedPercent) * terrainPitchFactor;


            // Volume also drops slightly when slowed
            float accelOrBrake = Mathf.Max(accel, brake);
            targetEngineVolume = Mathf.Lerp(0.1f, maxEngineVolume * terrainMultiplier, Mathf.Max(speedPercent, accelOrBrake));

            // Smoothly fade volume
            engineSource.volume = Mathf.MoveTowards(engineSource.volume, targetEngineVolume, fadeSpeed * Time.fixedDeltaTime);
        }

        // --- Highlight Inputs ---
        UpdateInputHighlights(accel, brake, steer, boostHeld);
    }
    void Update()
    {
        if (boostSlider)
        {
            boostSlider.value = Mathf.Lerp(boostSlider.value, targetBoostValue, boostUISmoothSpeed * Time.deltaTime);
        }
    }
    public void UpdateBoostUI()
    {
        if (boostSlider)
            boostSlider.value = boostMeter / 100f;

        if (!boostSlider) return;

        targetBoostValue = boostMeter / 100f;
    }

    //Added 26/10/2025
    // --- Slow Terrain System ---
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("SlowTerrain"))
        {
            terrainMultiplier = slowMultiplier;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("SlowTerrain"))
        {
            terrainMultiplier = 1f;
        }
    }

    void UpdateInputHighlights(float accel, float brake, Vector2 steer, bool boostHeld)
    {
        // Forward
        if (forwardArrow)
            forwardArrow.material = (accel > 0.1f) ? highlightMaterial : normalMaterial;

        // Backward / Reverse
        if (backwardArrow)
            backwardArrow.material = (brake > 0.1f) ? highlightMaterial : normalMaterial;

        // Left
        if (leftArrow)
            leftArrow.material = (steer.x < -0.1f) ? highlightMaterial : normalMaterial;

        // Right
        if (rightArrow)
            rightArrow.material = (steer.x > 0.1f) ? highlightMaterial : normalMaterial;

        // Boost
        if (boostButton)
            boostButton.material = boostHeld ? highlightMaterial : normalMaterial;
    }

    // Called by SlowTerrain.cs
    public void ApplyTerrainSlowdown(float multiplier, float duration)
    {
        StopAllCoroutines(); // Stop any previous slowdown coroutine
        StartCoroutine(SlowdownRoutine(multiplier, duration));
    }

    private IEnumerator SlowdownRoutine(float multiplier, float duration)
    {
        terrainMultiplier = multiplier;
        yield return new WaitForSeconds(duration);
        terrainMultiplier = 1f; // Return to normal speed
    }
    /*private IEnumerator Boost()
    {
    /*if (boosting || !canBoost)
    yield break;

boosting = true;
float elapsed = 0f;

while (elapsed < boostDuration && boostMeter > 0f)
{
    rb.AddForce(transform.forward * boostForce, ForceMode.Acceleration);
    boostMeter -= boostUsageRate * Time.deltaTime;

    UpdateBoostUI();

    elapsed += Time.deltaTime;
    yield return null;
}

boosting = false;

        if (boosting || !canBoost)
        yield break;

        boosting = true;

        // Play boost sound once
        if (engineSource && !engineSource.isPlaying)
            engineSource.Play(); // optional, if you have a separate source for boost use that

        if (boostSound)
            AudioSource.PlayClipAtPoint(boostSound, transform.position, 1f);

        while (boostMeter > 0f)
        {
            // Apply boost acceleration
            rb.AddForce(transform.forward * boostForce, ForceMode.Acceleration);

            // Drain the boost meter
            boostMeter -= boostUsageRate * Time.deltaTime;

            // Clamp so it never goes negative
            boostMeter = Mathf.Max(boostMeter, 0f);

            // Update the UI smoothly
            UpdateBoostUI();

            // Stop boosting if the meter hits zero
            if (boostMeter <= 0.01f)
                break;

            yield return null;
        }

        // Once boost meter is empty, stop boosting
        boosting = false;
        yield return new WaitForSeconds(boostCooldown);
        boostMeter = Mathf.Clamp(boostMeter, 0f, 100f);
        UpdateBoostUI();*/
    public void PlayMudEnterSound(Vector3 position)
    {
        if (mudEnterSound)
        {
            if (sfxSource)
                sfxSource.PlayOneShot(mudEnterSound, 0.8f);
            else
                AudioSource.PlayClipAtPoint(mudEnterSound, position, 0.8f);
        }
    }

    void ApplyWheelForces(float throttle, float steering)
    {
        // Steering (front wheels)
        wheelFL.steerAngle = steering * steerAngle;
        wheelFR.steerAngle = steering * steerAngle;

        // Motor (all-wheel drive feel)
        wheelFL.motorTorque = throttle * motorTorque;
        wheelFR.motorTorque = throttle * motorTorque;
        wheelRL.motorTorque = throttle * motorTorque;
        wheelRR.motorTorque = throttle * motorTorque;
    }
}