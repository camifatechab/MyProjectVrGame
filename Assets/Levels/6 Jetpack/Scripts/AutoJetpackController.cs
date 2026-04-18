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
    [SerializeField] private float armDownAngleThreshold = 120f;

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
    private bool isRecharging = false;

    [Header("Fuel Recharge System")]
    [Tooltip("Fuel recharged per second while grounded")]
    [SerializeField] private float fuelRechargeRate = 25f;

    [Tooltip("Enable fuel recharge when grounded")]
    [SerializeField] private bool enableRecharge = true;

    [Header("Surface Rules")]
    [Tooltip("Only these named floating islands count as recharge platforms.")]
    [SerializeField] private string[] rechargePlatformRoots =
    {
        "Floating_Island_4_Bioma2",
        "Floating_Island_4_Bioma2 (1)",
        "Floating_Island_3_Bioma2",
    };

    [Tooltip("Grounded contact on these surface prefixes triggers an immediate respawn.")]
    [SerializeField] private string[] hazardSurfacePrefixes =
    {
        "Lava_plane",
    };

    [SerializeField] private float groundSurfaceProbeDistance = 1.5f;
    [SerializeField] private float groundSurfaceProbeRadius = 0.2f;

    [Header("Air Resistance / Momentum")]
    [Tooltip("How quickly you slow down when not thrusting (lower = glide longer)")]
    [SerializeField] private float airDrag = 2f;

    [Tooltip("How quickly you slow down on the ground")]
    [SerializeField] private float groundDrag = 4f;

    [Header("Movement Integration")]
    [Tooltip("Reference to the ground movement controller - will be found automatically if not assigned")]
    [SerializeField] private MonoBehaviour moveProvider;

    [Header("Audio Manager")]
    [Tooltip("Reference to JetpackAudioManager - will be auto-created if not assigned")]
    [SerializeField] private JetpackAudioManager audioManager;

    [Header("Legacy Audio Settings (Deprecated - Use AudioManager)")]
    [Tooltip("Audio source for jetpack sound effects")]
    [SerializeField] private AudioSource jetpackAudioSource;

    [Tooltip("Sound to play while flying")]
    [SerializeField] private AudioClip flyingSound;

    [Tooltip("Volume of the flying sound (0-1)")]
    [SerializeField] private float flyingSoundVolume = 0.7f;

    [Tooltip("Time in seconds for sound to fade out when stopping")]
    [SerializeField] private float fadeOutDuration = 0.5f;

    // Audio state tracking
    private bool isFadingOut = false;
    private float fadeTimer = 0f;

    [Header("Collection Settings")]
    [Tooltip("Radius for crystal collection trigger")]
    [SerializeField] private float collectionRadius = 0.5f;

    [Header("Physics")]
    private float gravity = 9.81f;

    [Header("Debug")]
    [Tooltip("Logs per-second grip/arm/flight telemetry to the console. Leave OFF in normal play.")]
    [SerializeField] private bool verboseLogging = false;

    // Onboarding hint support
    public bool WasFiredThisSession { get; private set; } = false;
    // Set by RoverDriver.Dismount() via field access — blocks jetpack briefly after dismount
    private float postDismountCooldown = 0f;
    private bool externalFlightLock = false;


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

    // Landing detection
    private float previousVerticalVelocity = 0f;
    private RoverDriver roverDriver;
    private RoverPhysicsController roverPhysicsController;
    private PlayerRespawnManager playerRespawnManager;

    // Track previous grounded state
    private bool wasGrounded = true;
    private bool isGroundedOnRechargePlatform = false;
    private bool isGroundedOnHazardSurface = false;
    private string currentGroundSurfaceName = string.Empty;

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

        if (moveProvider == null)
        {
            GameObject moveObject = GameObject.Find("Move");
            if (moveObject != null)
            {
                moveProvider = moveObject.GetComponent<MonoBehaviour>();
                if (moveProvider != null && moveProvider.GetType().Name.Contains("Move"))
                    Debug.Log($"<color=cyan>✓ Found Movement Provider: {moveProvider.GetType().Name} on {moveObject.name}</color>");
            }
            if (moveProvider == null)
                Debug.LogWarning("No Movement Provider found. Ground movement won't be disabled during flight.");
        }

        roverDriver = FindAnyObjectByType<RoverDriver>();
        roverPhysicsController = FindAnyObjectByType<RoverPhysicsController>();
        playerRespawnManager = GetComponent<PlayerRespawnManager>();
        Debug.Log($"AutoJetpack Ready! Controllers found: Left={leftControllerTransform != null}, Right={rightControllerTransform != null}");

        if (jetpackAudioSource != null && flyingSound != null)
            Debug.Log("<color=green>✓ Jetpack Audio System Ready!</color>");
        else if (flyingSound == null)
            Debug.LogWarning("⚠ No flying sound assigned! Please assign an AudioClip in the Inspector.");

        Debug.Log($"<color=green>✓ Fuel System Initialized: {currentFuel}/{maxFuel}</color>");
    }

    void SetupAudioManager()
    {
        if (audioManager == null)
        {
            audioManager = GetComponent<JetpackAudioManager>();
            if (audioManager == null)
            {
                audioManager = gameObject.AddComponent<JetpackAudioManager>();
                Debug.Log("<color=cyan>✓ Created JetpackAudioManager component</color>");
            }
        }
        Debug.Log("<color=green>✓ JetpackAudioManager Ready!</color>");
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

        Debug.Log($"<color=green>✓ Collection trigger created with radius: {collectionRadius}m</color>");
    }

    void SetupAudioSource()
    {
        jetpackAudioSource = GetComponent<AudioSource>();
        if (jetpackAudioSource == null)
        {
            jetpackAudioSource = gameObject.AddComponent<AudioSource>();
            Debug.Log("<color=cyan>✓ Created AudioSource component</color>");
        }

        jetpackAudioSource.loop = true;
        jetpackAudioSource.playOnAwake = false;
        jetpackAudioSource.volume = flyingSoundVolume;
        jetpackAudioSource.clip = flyingSound;
        jetpackAudioSource.spatialBlend = 0f;
    }

    void UpdateAudioFade()
    {
        if (isFadingOut && jetpackAudioSource != null)
        {
            fadeTimer += Time.deltaTime;
            float fadeProgress = fadeTimer / fadeOutDuration;
            jetpackAudioSource.volume = Mathf.Lerp(flyingSoundVolume, 0f, fadeProgress);
            if (fadeProgress >= 1f)
            {
                jetpackAudioSource.Stop();
                jetpackAudioSource.volume = flyingSoundVolume;
                isFadingOut = false;
                Debug.Log("<color=green>🔇 Flying sound faded out</color>");
            }
        }
    }

    void StartAudioFadeOut()
    {
        if (jetpackAudioSource != null && jetpackAudioSource.isPlaying)
        {
            isFadingOut = true;
            fadeTimer = 0f;
            Debug.Log("<color=yellow>🔊 Starting audio fade out...</color>");
        }
    }

    void AutoFindControllers()
    {
        Transform cameraOffset = transform.Find("Camera Offset");
        if (cameraOffset != null)
        {
            leftControllerTransform = cameraOffset.Find("Left Controller");
            rightControllerTransform = cameraOffset.Find("Right Controller");

            if (leftControllerTransform != null) Debug.Log($"Found Left Controller: {leftControllerTransform.name}");
            else Debug.LogWarning("Could not find Left Controller!");

            if (rightControllerTransform != null) Debug.Log($"Found Right Controller: {rightControllerTransform.name}");
            else Debug.LogWarning("Could not find Right Controller!");
        }
        else
        {
            Debug.LogError("AutoJetpack: Could not find Camera Offset!");
        }
    }

    void InitializeXRDevices()
    {
        var leftHandDevices = new System.Collections.Generic.List<InputDevice>();
        var rightHandDevices = new System.Collections.Generic.List<InputDevice>();

        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftHandDevices);
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);

        if (leftHandDevices.Count > 0) { leftDevice = leftHandDevices[0]; Debug.Log($"Left Hand Device: {leftDevice.name}"); }
        if (rightHandDevices.Count > 0) { rightDevice = rightHandDevices[0]; Debug.Log($"Right Hand Device: {rightDevice.name}"); }
    }

    void Update()
    {
        if (!leftDevice.isValid || !rightDevice.isValid)
            InitializeXRDevices();

        UpdateGroundSurfaceState();
        HandleHazardSurfaceRespawn();

        if (externalFlightLock)
        {
            if (isFlying)
            {
                isFlying = false;
                if (HapticsManager.Instance != null) HapticsManager.Instance.StopJetpackVibration();
                if (audioManager != null) audioManager.StopThrust();
                else StartAudioFadeOut();
            }

            UpdateAudioFade();
            UpdateAudioManagerState();
            UpdateGroundMovementState();
            return;
        }

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
        bool leftGripPressed = IsGripPressed(leftDevice);
        bool rightGripPressed = IsGripPressed(rightDevice);
        bool bothGripsPressed = leftGripPressed && rightGripPressed;
        bool armsDown = AreArmsDown();

        if (verboseLogging && Time.frameCount % 60 == 0)
            Debug.Log($"[AutoJetpack] Left Grip: {leftGripPressed}, Right Grip: {rightGripPressed}, Arms Down: {armsDown}, Flying: {isFlying}, Fuel: {GetFuelPercentage():F1}%");

        bool inRover = (roverDriver != null && roverDriver.IsMounted)
            || (roverPhysicsController != null && roverPhysicsController.IsMounted);
        bool shouldFly = bothGripsPressed && armsDown && !isOutOfFuel && !inRover;

        if (postDismountCooldown > 0f) { postDismountCooldown -= Time.deltaTime; return; }
        if (shouldFly != isFlying)
        {
            isFlying = shouldFly;

            if (isFlying)
            {
                WasFiredThisSession = true;
                Debug.Log($"<color=cyan>★★★ Jetpack ACTIVATED ★★★ Fuel: {GetFuelPercentage():F1}%</color>");

                if (HapticsManager.Instance != null) HapticsManager.Instance.StartJetpackVibration();

                if (audioManager != null) audioManager.StartThrust();
                else if (jetpackAudioSource != null && flyingSound != null)
                {
                    if (isFadingOut) { isFadingOut = false; jetpackAudioSource.volume = flyingSoundVolume; }
                    if (!jetpackAudioSource.isPlaying) jetpackAudioSource.Play();
                }
            }
            else
            {
                string reason = isOutOfFuel ? "(OUT OF FUEL)" : "";
                Debug.Log($"<color=cyan>★★★ Jetpack DEACTIVATED ★★★ {reason}</color>");

                if (HapticsManager.Instance != null) HapticsManager.Instance.StopJetpackVibration();

                if (audioManager != null) audioManager.StopThrust();
                else StartAudioFadeOut();
            }
        }

        if (isFlying && isOutOfFuel)
        {
            isFlying = false;
            Debug.Log("<color=red>★★★ EMERGENCY SHUTDOWN - NO FUEL! ★★★</color>");
            if (HapticsManager.Instance != null) HapticsManager.Instance.StopJetpackVibration();
            if (audioManager != null) audioManager.StopThrust();
            else StartAudioFadeOut();
        }
    }

    void UpdateFuelSystem()
    {
        if (isFlying)
        {
            float consumption = fuelConsumptionRate * Time.deltaTime;
            Vector3 thrustDirection = GetThrustDirection();
            float upwardness = Vector3.Dot(thrustDirection, Vector3.up);
            if (upwardness < 0.7f) consumption *= glidingEfficiency;

            currentFuel -= consumption;

            if (currentFuel <= 0)
            {
                currentFuel = 0;
                if (!isOutOfFuel)
                {
                    isOutOfFuel = true;
                    Debug.Log("<color=red>⚠ OUT OF FUEL! ⚠</color>");
                    if (audioManager != null) audioManager.StopLowFuelWarning();
                }
            }

            float fuelPercentage = (currentFuel / maxFuel) * 100f;
            if (fuelPercentage <= lowFuelThreshold && !hasShownLowFuelWarning)
            {
                hasShownLowFuelWarning = true;
                Debug.Log($"<color=orange>⚠ LOW FUEL WARNING: {fuelPercentage:F1}% remaining! ⚠</color>");
                if (audioManager != null) audioManager.PlayLowFuelWarning();
            }
        }
        else
        {
            if (currentFuel > (maxFuel * lowFuelThreshold / 100f))
                hasShownLowFuelWarning = false;
        }
    }

    void UpdateFuelRecharge()
    {
        bool shouldRecharge = isGroundedOnRechargePlatform && !isFlying && enableRecharge && currentFuel < maxFuel;

        if (shouldRecharge)
        {
            if (!isRecharging) { isRecharging = true; Debug.Log("<color=cyan>⚡ FUEL RECHARGING...</color>"); }

            currentFuel = Mathf.Min(currentFuel + fuelRechargeRate * Time.deltaTime, maxFuel);

            if (currentFuel > 0 && isOutOfFuel)
            {
                isOutOfFuel = false;
                Debug.Log("<color=green>✓ FUEL RESTORED - Ready to fly!</color>");
            }

            if (hasShownLowFuelWarning && (currentFuel / maxFuel * 100f) > lowFuelThreshold)
            {
                hasShownLowFuelWarning = false;
                if (audioManager != null) audioManager.ResetLowFuelWarning();
            }

            if (currentFuel >= maxFuel && isRecharging)
                Debug.Log("<color=green>✓ FUEL TANK FULL!</color>");
        }
        else
        {
            if (isRecharging) isRecharging = false;
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

    public void SetExternalFlightLock(bool locked)
    {
        externalFlightLock = locked;

        if (locked)
            ForceStopFlightEffects();
    }

    private void OnDisable()
    {
        ForceStopFlightEffects();
    }

    private void ForceStopFlightEffects()
    {
        isFlying = false;
        isFadingOut = false;
        fadeTimer = 0f;
        velocity = Vector3.zero;

        if (HapticsManager.Instance != null)
            HapticsManager.Instance.StopJetpackVibration();

        if (audioManager != null)
            audioManager.StopAllAudio();

        if (jetpackAudioSource != null)
        {
            jetpackAudioSource.Stop();
            jetpackAudioSource.volume = flyingSoundVolume;
        }
    }

    void UpdateAudioManagerState()
    {
        if (audioManager == null) return;
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

        bool shouldEnableGroundMovement = externalFlightLock || (characterController.isGrounded && !isFlying);

        if (moveProvider.enabled != shouldEnableGroundMovement)
        {
            moveProvider.enabled = shouldEnableGroundMovement;
            string reason = externalFlightLock
                ? "JETPACK LOCKED"
                : (isFlying ? "FLYING" : (!characterController.isGrounded ? "IN AIR" : "GROUNDED"));
            Debug.Log($"<color=yellow>Ground Movement {(shouldEnableGroundMovement ? "ENABLED" : "DISABLED")} - {reason}</color>");
        }

        if (characterController.isGrounded != wasGrounded)
        {
            wasGrounded = characterController.isGrounded;
            Debug.Log($"<color=orange>Ground State: {(wasGrounded ? "LANDED" : "AIRBORNE")}</color>");
        }
    }

    bool IsGripPressed(InputDevice device)
    {
        if (!device.isValid) return false;

        bool gripButton = false;
        if (device.TryGetFeatureValue(CommonUsages.gripButton, out gripButton) && gripButton) return true;

        float grip = 0f;
        if (device.TryGetFeatureValue(CommonUsages.grip, out grip) && grip > 0.5f) return true;

        return false;
    }

    private void UpdateGroundSurfaceState()
    {
        isGroundedOnRechargePlatform = false;
        isGroundedOnHazardSurface = false;
        currentGroundSurfaceName = string.Empty;

        if (characterController == null)
            return;

        Vector3 feet = transform.position + characterController.center;
        feet.y -= Mathf.Max(0f, (characterController.height * 0.5f) - characterController.radius);

        Vector3 origin = feet + Vector3.up * 0.25f;
        float radius = Mathf.Min(groundSurfaceProbeRadius, characterController.radius * 0.95f);

        if (!Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit, groundSurfaceProbeDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return;

        currentGroundSurfaceName = hit.collider != null ? hit.collider.gameObject.name : string.Empty;
        isGroundedOnRechargePlatform = MatchesNamedRoot(hit.collider.transform, rechargePlatformRoots);
        isGroundedOnHazardSurface = MatchesSurfacePrefix(hit.collider.transform, hazardSurfacePrefixes);
    }

    private void HandleHazardSurfaceRespawn()
    {
        if (!isGroundedOnHazardSurface || playerRespawnManager == null)
            return;

        bool inRover = (roverDriver != null && roverDriver.IsMounted)
            || (roverPhysicsController != null && roverPhysicsController.IsMounted);

        if (inRover)
            return;

        playerRespawnManager.TriggerRespawn($"Touched hazard surface '{currentGroundSurfaceName}'");
    }

    private static bool MatchesNamedRoot(Transform surface, string[] validNames)
    {
        if (surface == null || validNames == null)
            return false;

        for (Transform current = surface; current != null; current = current.parent)
        {
            for (int i = 0; i < validNames.Length; i++)
            {
                if (!string.IsNullOrEmpty(validNames[i]) && current.name == validNames[i])
                    return true;
            }
        }

        return false;
    }

    private static bool MatchesSurfacePrefix(Transform surface, string[] prefixes)
    {
        if (surface == null || prefixes == null)
            return false;

        for (Transform current = surface; current != null; current = current.parent)
        {
            for (int i = 0; i < prefixes.Length; i++)
            {
                if (!string.IsNullOrEmpty(prefixes[i]) && current.name.StartsWith(prefixes[i]))
                    return true;
            }
        }

        return false;
    }

    bool AreArmsDown()
    {
        if (leftControllerTransform == null || rightControllerTransform == null) return false;

        float leftAngle = Vector3.Angle(leftControllerTransform.forward, Vector3.down);
        float rightAngle = Vector3.Angle(rightControllerTransform.forward, Vector3.down);

        if (verboseLogging && Time.frameCount % 60 == 0)
            Debug.Log($"[AutoJetpack] Arm Angles - Left: {leftAngle:F1}, Right: {rightAngle:F1} (Threshold: {armDownAngleThreshold})");

        return leftAngle < armDownAngleThreshold && rightAngle < armDownAngleThreshold;
    }

    Vector3 GetThrustDirection()
    {
        if (leftControllerTransform == null || rightControllerTransform == null) return Vector3.up;

        Vector3 averageDirection = (leftControllerTransform.forward + rightControllerTransform.forward).normalized;
        return -averageDirection;
    }

    void ApplyMovement()
    {
        if (isFlying)
        {
            Vector3 targetVelocity = GetThrustDirection() * maxUpwardVelocity;
            velocity = Vector3.MoveTowards(velocity, targetVelocity, thrustForce * Time.deltaTime);
            characterController.Move(velocity * Time.deltaTime);
        }
        else
        {
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, airDrag * Time.deltaTime);
            velocity.x = horizontalVelocity.x;
            velocity.z = horizontalVelocity.z;

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
}

/// <summary>
/// Simple component to make the collection trigger work with CrystalCollectible.
/// </summary>
public class PlayerCollectionTrigger : MonoBehaviour
{
    // Exists so the trigger can detect crystals.
    // Actual collection handled by CrystalCollectible.OnTriggerEnter()
}
