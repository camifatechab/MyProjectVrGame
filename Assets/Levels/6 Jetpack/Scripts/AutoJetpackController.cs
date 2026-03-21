using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using InputDevice = UnityEngine.XR.InputDevice;

/// <summary>
/// FULLY AUTOMATIC VR Jetpack Controller
/// Hold BOTH grip buttons with arms down to fly upward!
/// </summary>
public class AutoJetpackController : MonoBehaviour
{
    [Header("Flight Settings")]
    [SerializeField] private float thrustForce = 15f;
    [SerializeField] private float maxUpwardVelocity = 10f;
    [SerializeField] private float armDownAngleThreshold = 120f;

    [Header("Fuel System")]
    [SerializeField] private float maxFuel = 100f;
    [SerializeField] private float currentFuel = 100f;
    [SerializeField] private float fuelConsumptionRate = 10f;
    [SerializeField] private float lowFuelThreshold = 25f;
    [SerializeField] private float glidingEfficiency = 0.5f;

    private bool isOutOfFuel = false;
    private bool hasShownLowFuelWarning = false;
    private bool isRecharging = false;

    [Header("Fuel Recharge System")]
    [SerializeField] private float fuelRechargeRate = 25f;
    [SerializeField] private bool enableRecharge = true;

    [Header("Air Resistance / Momentum")]
    [SerializeField] private float airDrag = 2f;
    [SerializeField] private float groundDrag = 4f;

    [Header("Movement Integration")]
    [SerializeField] private MonoBehaviour moveProvider;

    [Header("Audio Manager")]
    [SerializeField] private JetpackAudioManager audioManager;

    [Header("Legacy Audio Settings")]
    [SerializeField] private AudioSource jetpackAudioSource;
    [SerializeField] private AudioClip flyingSound;
    [SerializeField] private float flyingSoundVolume = 0.7f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    private bool isFadingOut = false;
    private float fadeTimer = 0f;

    [Header("Collection Settings")]
    [SerializeField] private float collectionRadius = 0.5f;

    // Physics
    private float gravity = 9.81f;
    private CharacterController characterController;
    private Vector3 velocity;

    // State
    private bool isFlying = false;
    private bool gripsWerePressedLastFrame = false;
    private float postDismountCooldown = 0f;
    private RoverDriver roverDriver;

    private Transform leftControllerTransform;
    private Transform rightControllerTransform;

    private InputDevice leftDevice;
    private InputDevice rightDevice;

    private GameObject collectionTrigger;

    private float previousVerticalVelocity = 0f;
    private bool wasGrounded = true;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.3f;
            characterController.center = new Vector3(0, 0.9f, 0);
        }

        SetupAudioManager();
        SetupAudioSource();

        currentFuel = maxFuel;

        SetupCollectionTrigger();
        AutoFindControllers();
        InitializeXRDevices();

        roverDriver = FindAnyObjectByType<RoverDriver>();

        if (moveProvider == null)
        {
            GameObject moveObject = GameObject.Find("Move");
            if (moveObject != null)
                moveProvider = moveObject.GetComponent<MonoBehaviour>();
        }

        Debug.Log($"AutoJetpack Ready! Controllers: L={leftControllerTransform != null} R={rightControllerTransform != null}");
    }

    void SetupAudioManager()
    {
        if (audioManager == null)
        {
            audioManager = GetComponent<JetpackAudioManager>();
            if (audioManager == null)
                audioManager = gameObject.AddComponent<JetpackAudioManager>();
        }
    }

    void SetupCollectionTrigger()
    {
        collectionTrigger = new GameObject("CollectionTrigger");
        collectionTrigger.transform.SetParent(transform);
        collectionTrigger.transform.localPosition = Vector3.zero;
        SphereCollider trigger = collectionTrigger.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = collectionRadius;
        collectionTrigger.AddComponent<PlayerCollectionTrigger>();
        collectionTrigger.tag = "MainCamera";
    }

    void SetupAudioSource()
    {
        jetpackAudioSource = GetComponent<AudioSource>();
        if (jetpackAudioSource == null)
            jetpackAudioSource = gameObject.AddComponent<AudioSource>();
        jetpackAudioSource.loop = true;
        jetpackAudioSource.playOnAwake = false;
        jetpackAudioSource.volume = flyingSoundVolume;
        jetpackAudioSource.clip = flyingSound;
        jetpackAudioSource.spatialBlend = 0f;
    }

    void AutoFindControllers()
    {
        Transform cameraOffset = transform.Find("Camera Offset");
        if (cameraOffset != null)
        {
            leftControllerTransform  = cameraOffset.Find("Left Controller");
            rightControllerTransform = cameraOffset.Find("Right Controller");
        }
    }

    void InitializeXRDevices()
    {
        var left  = new System.Collections.Generic.List<InputDevice>();
        var right = new System.Collections.Generic.List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand,  left);
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, right);
        if (left.Count  > 0) leftDevice  = left[0];
        if (right.Count > 0) rightDevice = right[0];
    }

    void Update()
    {
        if (!leftDevice.isValid || !rightDevice.isValid)
            InitializeXRDevices();

        CheckJetpackActivation();
        UpdateFuelSystem();
        UpdateFuelRecharge();
        UpdateAudioFade();
        UpdateAudioManagerState();
        ApplyMovement();
        CheckLanding();
        UpdateGroundMovementState();
    }

    void CheckJetpackActivation()
    {
        bool leftGripPressed  = IsGripPressed(leftDevice);
        bool rightGripPressed = IsGripPressed(rightDevice);
        bool bothGripsPressed = leftGripPressed && rightGripPressed;
        bool gripsJustPressed = bothGripsPressed && !gripsWerePressedLastFrame;

        if (postDismountCooldown > 0f)
            postDismountCooldown -= Time.deltaTime;

        bool armsDown = AreArmsDown();
        bool inRover  = roverDriver != null && roverDriver.IsMounted;

        // Only start on a fresh grip press
        if (gripsJustPressed && armsDown && !isOutOfFuel && !inRover && postDismountCooldown <= 0f)
            isFlying = true;

        // Stop when grips release or in rover
        if (!bothGripsPressed || inRover)
            isFlying = false;

        gripsWerePressedLastFrame = bothGripsPressed;
    }

    void UpdateFuelSystem()
    {
        if (!isFlying)
        {
            if (currentFuel > (maxFuel * lowFuelThreshold / 100f))
                hasShownLowFuelWarning = false;
            return;
        }

        float consumption = fuelConsumptionRate * Time.deltaTime;
        if (Vector3.Dot(GetThrustDirection(), Vector3.up) < 0.7f)
            consumption *= glidingEfficiency;

        currentFuel -= consumption;
        if (currentFuel <= 0)
        {
            currentFuel = 0;
            if (!isOutOfFuel)
            {
                isOutOfFuel = true;
                if (audioManager != null) audioManager.StopLowFuelWarning();
            }
        }

        float pct = (currentFuel / maxFuel) * 100f;
        if (pct <= lowFuelThreshold && !hasShownLowFuelWarning)
        {
            hasShownLowFuelWarning = true;
            if (audioManager != null) audioManager.PlayLowFuelWarning();
        }
    }

    void UpdateFuelRecharge()
    {
        bool should = characterController.isGrounded && !isFlying && enableRecharge && currentFuel < maxFuel;
        if (should)
        {
            isRecharging = true;
            currentFuel  = Mathf.Min(currentFuel + fuelRechargeRate * Time.deltaTime, maxFuel);
            if (currentFuel > 0 && isOutOfFuel) isOutOfFuel = false;
            if (hasShownLowFuelWarning && (currentFuel / maxFuel * 100f) > lowFuelThreshold)
            {
                hasShownLowFuelWarning = false;
                if (audioManager != null) audioManager.ResetLowFuelWarning();
            }
        }
        else { isRecharging = false; }
    }

    void UpdateAudioFade()
    {
        if (!isFadingOut || jetpackAudioSource == null) return;
        fadeTimer += Time.deltaTime;
        jetpackAudioSource.volume = Mathf.Lerp(flyingSoundVolume, 0f, fadeTimer / fadeOutDuration);
        if (fadeTimer >= fadeOutDuration)
        {
            jetpackAudioSource.Stop();
            jetpackAudioSource.volume = flyingSoundVolume;
            isFadingOut = false;
        }
    }

    void UpdateAudioManagerState()
    {
        if (audioManager != null)
            audioManager.UpdatePlayerState(transform.position.y, velocity.magnitude);
    }

    void CheckLanding()
    {
        if (characterController.isGrounded && !wasGrounded)
        {
            if (HapticsManager.Instance != null) HapticsManager.Instance.PulseLanding(previousVerticalVelocity);
            if (audioManager != null) audioManager.PlayLanding(previousVerticalVelocity);
        }
        previousVerticalVelocity = velocity.y;
    }

    void UpdateGroundMovementState()
    {
        if (moveProvider == null) return;
        bool shouldEnable = characterController.isGrounded && !isFlying;
        if (moveProvider.enabled != shouldEnable) moveProvider.enabled = shouldEnable;
        if (characterController.isGrounded != wasGrounded) wasGrounded = characterController.isGrounded;
    }

    void ApplyMovement()
    {
        if (isFlying)
        {
            Vector3 target = GetThrustDirection() * maxUpwardVelocity;
            velocity = Vector3.MoveTowards(velocity, target, thrustForce * Time.deltaTime);
            characterController.Move(velocity * Time.deltaTime);
        }
        else
        {
            Vector3 horiz = new Vector3(velocity.x, 0, velocity.z);
            horiz = Vector3.MoveTowards(horiz, Vector3.zero, airDrag * Time.deltaTime);
            velocity.x = horiz.x;
            velocity.z = horiz.z;

            if (!characterController.isGrounded)
            {
                velocity.y -= gravity * Time.deltaTime;
                characterController.Move(velocity * Time.deltaTime);
            }
            else
            {
                velocity = Vector3.zero;
            }
        }
    }

    bool IsGripPressed(InputDevice device)
    {
        if (!device.isValid) return false;
        if (device.TryGetFeatureValue(CommonUsages.gripButton, out bool btn) && btn) return true;
        if (device.TryGetFeatureValue(CommonUsages.grip, out float grip) && grip > 0.5f) return true;
        return false;
    }

    bool AreArmsDown()
    {
        if (leftControllerTransform == null || rightControllerTransform == null) return false;
        float l = Vector3.Angle(leftControllerTransform.forward,  Vector3.down);
        float r = Vector3.Angle(rightControllerTransform.forward, Vector3.down);
        return l < armDownAngleThreshold && r < armDownAngleThreshold;
    }

    Vector3 GetThrustDirection()
    {
        if (leftControllerTransform == null || rightControllerTransform == null) return Vector3.up;
        return -(leftControllerTransform.forward + rightControllerTransform.forward).normalized;
    }

    // Public API
    public bool IsFlying()           => isFlying;
    public float GetCurrentFuel()    => currentFuel;
    public float GetMaxFuel()        => maxFuel;
    public float GetFuelPercentage() => (currentFuel / maxFuel) * 100f;
    public bool IsLowOnFuel()        => GetFuelPercentage() <= lowFuelThreshold;
    public bool IsOutOfFuel()        => isOutOfFuel;

    public void RefillFuel(float amount) { currentFuel = Mathf.Min(currentFuel + amount, maxFuel); isOutOfFuel = false; }
    public void RefillFuelFull()         { currentFuel = maxFuel; isOutOfFuel = false; hasShownLowFuelWarning = false; }
}

/// <summary>Exists so crystals can detect the collection trigger.</summary>
public class PlayerCollectionTrigger : MonoBehaviour { }
