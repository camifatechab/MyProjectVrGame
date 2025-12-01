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

    //Added 24/10/2025
    /*void Awake()
    {
        defaultAcceleration = acceleration;
        defaultMaxSpeed = maxSpeed;
    }*/

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
        /*float accel = accelerateAction.action?.ReadValue<float>() ?? 0f;
        float brake = brakeAction.action?.ReadValue<float>() ?? 0f;
        Vector2 steer = steerAction.action?.ReadValue<Vector2>() ?? Vector2.zero;

        // Forward acceleration
        if (accel > 0.1f && rb.linearVelocity.magnitude < maxSpeed)
            rb.AddForce(transform.forward * accel * acceleration, ForceMode.Acceleration);

        // Reverse / brake
        if (brake > 0.1f)
        {
            rb.AddForce(-transform.forward * brake * brakeForce, ForceMode.Acceleration);

            if (!isBraking && brakeSource && brakeClip)
            {
                brakeSource.PlayOneShot(brakeClip);
                isBraking = true;
            }
        }
        else
        {
            isBraking = false;
        }

        // Steering (left stick X-axis)
        float turn = steer.x * turnSpeed * Time.fixedDeltaTime;
        transform.Rotate(0f, turn, 0f);

        //Featherable boost 
        bool boostHeld = false;

        // --- Boost Input ---
        if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame) // For PC testing
        {
            StartCoroutine(Boost());
        }
        else if (boostAction.action.WasPressedThisFrame())
        {
            StartCoroutine(Boost());
        }
        if (!boosting)
        {
            // For PC testing
            if ((Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame) ||
                boostAction.action.WasPressedThisFrame())
            {
                if (canBoost)
                    StartCoroutine(Boost());
            }
        }

        bool boostPressed = false;

        // Check both PC key and Quest controller input
        if (Keyboard.current != null && Keyboard.current.bKey.isPressed)
            boostPressed = true;
        else if (boostAction.action.IsPressed())
            boostPressed = true;

        if (boostPressed && canBoost && !boosting)
        {
            StartCoroutine(Boost());
        }


        // Engine sound pitch scaling with speed
        if (engineSource)
        {
            float speedPercent = Mathf.Clamp01(rb.linearVelocity.magnitude / maxSpeed);
            float accelOrBrake = Mathf.Max(accel, brake);

        // Smooth pitch based on speed
        engineSource.pitch = Mathf.Lerp(enginePitchMin, enginePitchMax, speedPercent);

        // Target volume increases with acceleration/speed
        targetEngineVolume = Mathf.Lerp(0.1f, maxEngineVolume, Mathf.Max(speedPercent, accelOrBrake));

        // Smoothly fade towards target volume
        engineSource.volume = Mathf.MoveTowards(engineSource.volume, targetEngineVolume, fadeSpeed * Time.fixedDeltaTime);
        //engineSource.pitch = Mathf.Lerp(enginePitchMin, enginePitchMax, speedPercent);
        }
    }*/

        //Last update: 22/10/2025
        /*float accel = accelerateAction.action?.ReadValue<float>() ?? 0f;
        float brake = brakeAction.action?.ReadValue<float>() ?? 0f;
        Vector2 steer = steerAction.action?.ReadValue<Vector2>() ?? Vector2.zero;

        // Forward acceleration
        if (accel > 0.1f && rb.linearVelocity.magnitude < maxSpeed)
            rb.AddForce(transform.forward * accel * acceleration, ForceMode.Acceleration);

        // Reverse / brake
        if (brake > 0.1f)
        {
            rb.AddForce(-transform.forward * brake * brakeForce, ForceMode.Acceleration);

            if (!isBraking && brakeSource && brakeClip)
            {
                brakeSource.PlayOneShot(brakeClip);
                isBraking = true;
            }
        }
        else isBraking = false;

        // Steering (left stick X-axis)
        float turn = steer.x * turnSpeed * Time.fixedDeltaTime;
        transform.Rotate(0f, turn, 0f);

        // --- FEATHERABLE BOOST ---
        bool boostHeld = false;

        // For PC and Meta Quest
        if (Keyboard.current != null && Keyboard.current.bKey.isPressed)
            boostHeld = true;
        else if (boostAction.action.IsPressed())
            boostHeld = true;

        if (boostHeld && canBoost)
        {
            boosting = true;
            rb.AddForce(transform.forward * boostForce, ForceMode.Acceleration);

            // Drain meter
            boostMeter -= boostUsageRate * Time.fixedDeltaTime;
            boostMeter = Mathf.Max(boostMeter, 0f);
        }
        else
        {
            boosting = false;
        }

        UpdateBoostUI();

        // Engine sound pitch scaling with speed
        if (engineSource)
        {
            float speedPercent = Mathf.Clamp01(rb.linearVelocity.magnitude / maxSpeed);
            float accelOrBrake = Mathf.Max(accel, brake);
            engineSource.pitch = Mathf.Lerp(enginePitchMin, enginePitchMax, speedPercent);
            targetEngineVolume = Mathf.Lerp(0.1f, maxEngineVolume, Mathf.Max(speedPercent, accelOrBrake));
            engineSource.volume = Mathf.MoveTowards(engineSource.volume, targetEngineVolume, fadeSpeed * Time.fixedDeltaTime);
        }*/

        float accel = accelerateAction.action?.ReadValue<float>() ?? 0f;
        float brake = brakeAction.action?.ReadValue<float>() ?? 0f;
        Vector2 steer = steerAction.action?.ReadValue<Vector2>() ?? Vector2.zero;

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
        /*if (engineSource)
        {
            float speedPercent = Mathf.Clamp01(rb.linearVelocity.magnitude / maxSpeed);
            float accelOrBrake = Mathf.Max(accel, brake);
            engineSource.pitch = Mathf.Lerp(enginePitchMin, enginePitchMax, speedPercent);
            targetEngineVolume = Mathf.Lerp(0.1f, maxEngineVolume, Mathf.Max(speedPercent, accelOrBrake));
            engineSource.volume = Mathf.MoveTowards(engineSource.volume, targetEngineVolume, fadeSpeed * Time.fixedDeltaTime);
        }*/
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

    //Added 24/10/2025
    /*public void ApplyTerrainSlowdown(float speedMultiplier, float duration)
    {
        // Cancel any existing reset coroutine
        if (terrainResetCoroutine != null)
            StopCoroutine(terrainResetCoroutine);

        // Reduce speed and acceleration temporarily
        acceleration = defaultAcceleration * speedMultiplier;
        maxSpeed = defaultMaxSpeed * speedMultiplier;

        // Reset after duration
        terrainResetCoroutine = StartCoroutine(ResetTerrainEffect(duration));
    }*/

    //Added 24/10/2025
    /*private IEnumerator ResetTerrainEffect(float delay)
    {
        yield return new WaitForSeconds(delay);
        acceleration = defaultAcceleration;
        maxSpeed = defaultMaxSpeed;
        terrainResetCoroutine = null;
    }*/

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

}

/*using System.Collections;
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
    public InputActionProperty boostAction;      // B button

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

    [Header("Boost Settings")]
    public float boostForce = 30f;
    public float boostUsageRate = 50f; // how fast it drains
    public float boostMeter = 100f;    // 0–100
    public float boostRegenPerBattery = 33.3f;

    [Header("Boost UI")]
    public Slider boostSlider;
    public float boostUISmoothSpeed = 3f;
    private float targetBoostValue;

    [Header("Steering Wheel (Optional)")]
    /*public Transform steeringWheel;
    public float maxSteerAngle = 45f;
    public SteeringWheelController steeringWheelController;

    [Header("Visuals")]
    public WheelVisuals wheelVisuals;

    private bool boosting = false;
    private bool canBoost => boostMeter > 0f;

    private Rigidbody rb;
    private bool isBraking;
    private float targetEngineVolume;
    private float currentSteerInput;

    // For terrain modifiers
    private float originalAcceleration;
    private float originalMaxSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.maxAngularVelocity = 5f;

        originalAcceleration = acceleration;
        originalMaxSpeed = maxSpeed;

        if (boostSlider)
        {
            boostSlider.value = boostMeter / 100f;
            targetBoostValue = boostSlider.value;
        }

        if (engineSource && engineLoop)
        {
            engineSource.clip = engineLoop;
            engineSource.loop = true;
            engineSource.volume = 0f;
            engineSource.Play();
        }
    }

    void FixedUpdate()
    {
        float accel = accelerateAction.action?.ReadValue<float>() ?? 0f;
        float brake = brakeAction.action?.ReadValue<float>() ?? 0f;

        // Handle steering input
        /*if (steeringWheel != null)
        {
            float steerAngle = steeringWheel.localEulerAngles.z;
            if (steerAngle > 180f) steerAngle -= 360f;
            currentSteerInput = Mathf.Clamp(steerAngle / maxSteerAngle, -1f, 1f);
        }
        else
        {
            Vector2 steer = steerAction.action?.ReadValue<Vector2>() ?? Vector2.zero;
            currentSteerInput = steer.x;
        }
        if (steeringWheelController != null)
        {
            currentSteerInput = steeringWheelController.GetSteerInput();
        }
        else
        {
            Vector2 steer = steerAction.action?.ReadValue<Vector2>() ?? Vector2.zero;
            currentSteerInput = steer.x;
        }

        // Update steering visuals
        if (wheelVisuals != null)
        {
            wheelVisuals.UpdateWheelVisuals(currentSteerInput);
            wheelVisuals.SpinWheels(rb.linearVelocity.magnitude);
        }

        // Movement
        if (accel > 0.1f && rb.linearVelocity.magnitude < maxSpeed)
            rb.AddForce(transform.forward * accel * acceleration, ForceMode.Acceleration);

        if (brake > 0.1f)
        {
            rb.AddForce(-transform.forward * brake * brakeForce, ForceMode.Acceleration);

            if (!isBraking && brakeSource && brakeClip)
            {
                brakeSource.PlayOneShot(brakeClip);
                isBraking = true;
            }
        }
        else
        {
            isBraking = false;
        }

        // Steering
        float turn = currentSteerInput * turnSpeed * Time.fixedDeltaTime;
        transform.Rotate(0f, turn, 0f);

        // --- Boost Input ---
        bool boostPressed = false;
        if (Keyboard.current != null && Keyboard.current.bKey.isPressed)
            boostPressed = true;
        else if (boostAction.action.IsPressed())
            boostPressed = true;

        if (boostPressed && canBoost && !boosting)
        {
            StartCoroutine(Boost());
        }

        // Engine audio scaling
        if (engineSource)
        {
            float speedPercent = Mathf.Clamp01(rb.linearVelocity.magnitude / maxSpeed);
            float accelOrBrake = Mathf.Max(accel, brake);

            engineSource.pitch = Mathf.Lerp(enginePitchMin, enginePitchMax, speedPercent);
            targetEngineVolume = Mathf.Lerp(0.1f, maxEngineVolume, Mathf.Max(speedPercent, accelOrBrake));
            engineSource.volume = Mathf.MoveTowards(engineSource.volume, targetEngineVolume, fadeSpeed * Time.fixedDeltaTime);
        }
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
        if (!boostSlider) return;
        targetBoostValue = boostMeter / 100f;
    }

    private IEnumerator Boost()
    {
        boosting = true;

        if (boostSound)
            AudioSource.PlayClipAtPoint(boostSound, transform.position, 0.8f);

        while ((Keyboard.current != null && Keyboard.current.bKey.isPressed || boostAction.action.IsPressed()) && boostMeter > 0f)
        {
            rb.AddForce(transform.forward * boostForce, ForceMode.Acceleration);
            boostMeter -= boostUsageRate * Time.deltaTime;
            boostMeter = Mathf.Max(boostMeter, 0f);
            UpdateBoostUI();
            yield return null;
        }

        boosting = false;
        UpdateBoostUI();
    }

    // --- Swamp slow-down ---
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("SlowTerrain"))
        {
            acceleration *= 0.5f;
            maxSpeed *= 0.6f;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("SlowTerrain"))
        {
            acceleration = originalAcceleration;
            maxSpeed = originalMaxSpeed;
        }
    }
}*/