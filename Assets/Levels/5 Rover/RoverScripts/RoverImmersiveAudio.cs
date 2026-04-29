using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RoverPhysicsController))]
public class RoverImmersiveAudio : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private RoverPhysicsController controller;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform audioAnchor;

    [Header("Clips")]
    [SerializeField] private AudioClip idleLoop;
    [SerializeField] private AudioClip engineLoop;
    [SerializeField] private AudioClip offroadLoop;
    [SerializeField] private AudioClip suspensionRattleLoop;
    [SerializeField] private AudioClip brakeClip;
    [SerializeField] private AudioClip boostClip;
    [SerializeField] private AudioClip landingClip;

    [Header("Mix")]
    [SerializeField] [Range(0f, 1f)] private float interiorSpatialBlend = 1f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 28f;
    [SerializeField] private float masterVolume = 1f;
    [SerializeField] private float fadeSpeed = 4f;

    [Header("Idle + Engine")]
    [SerializeField] private float idleBaseVolume = 0.18f;
    [SerializeField] private float idleMaxVolume = 0.42f;
    [SerializeField] private float engineMaxVolume = 0.6f;
    [SerializeField] private float enginePitchMin = 0.88f;
    [SerializeField] private float enginePitchMax = 1.35f;

    [Header("Surface Layers")]
    [SerializeField] private float offroadMaxVolume = 0.38f;
    [SerializeField] private float offroadPitchMin = 0.92f;
    [SerializeField] private float offroadPitchMax = 1.18f;
    [SerializeField] private float suspensionMaxVolume = 0.28f;
    [SerializeField] private float suspensionPitchMin = 0.95f;
    [SerializeField] private float suspensionPitchMax = 1.2f;

    [Header("Triggers")]
    [SerializeField] private float brakeSpeedThreshold = 2.5f;
    [SerializeField] private float brakeCooldown = 0.45f;
    [SerializeField] private float curveBrakeSpeedThreshold = 5f;
    [SerializeField] [Range(0f, 1f)] private float curveSteerThreshold = 0.38f;
    [SerializeField] private float curveBrakeRetriggerDelay = 0.7f;
    [SerializeField] private bool landingOnlyOnArmedScriptedJump = true;
    [SerializeField] private float landingAirTimeThreshold = 0.18f;
    [SerializeField] private float landingImpactThreshold = 3f;
    [SerializeField] private bool playBoostOnScriptedLaunch;

    [Header("Landing Feel")]
    [SerializeField] [Range(0f, 1f)] private float landingSpatialBlend = 0.82f;
    [SerializeField] private float landingMinDistance = 4.5f;
    [SerializeField] private float landingMaxDistance = 34f;
    [SerializeField] private float landingVolumeBoost = 1.35f;
    [SerializeField] private float landingPitchMin = 0.97f;
    [SerializeField] private float landingPitchMax = 1.03f;

    private AudioSource idleSource;
    private AudioSource engineSource;
    private AudioSource offroadSource;
    private AudioSource suspensionSource;
    private AudioSource oneShotSource;
    private AudioSource landingSource;

    private bool wasGrounded;
    private bool wasBraking;
    private bool wasCurving;
    private bool lastScriptedLaunchActive;
    private float airborneTimer;
    private float hardestLandingVelocity;
    private float brakeCooldownTimer;
    private float curveBrakeTimer;
    private float smoothSlip;
    private float smoothJolt;
    private Vector3 previousVelocity;
    private bool scriptedLandingArmed;
    private float landingRetriggerBlockTimer;

    private void Reset()
    {
        controller = GetComponent<RoverPhysicsController>();
        rb = GetComponent<Rigidbody>();
    }

    private void Awake()
    {
        controller ??= GetComponent<RoverPhysicsController>();
        rb ??= GetComponent<Rigidbody>();
    }

    private void Start()
    {
        ResolveAnchor();
        EnsureAudioSources();
        ApplyClips();

        wasGrounded = controller != null && controller.IsGrounded;
        previousVelocity = rb != null ? rb.linearVelocity : Vector3.zero;
        lastScriptedLaunchActive = controller != null && controller.IsScriptedLaunchActive;
    }

    private void OnValidate()
    {
        controller ??= GetComponent<RoverPhysicsController>();
        rb ??= GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (controller == null || rb == null)
            return;

        ResolveAnchor();
        EnsureAudioSources();
        ApplyClips();

        bool grounded = controller.IsGrounded;
        float speed01 = controller.SpeedNormalized;
        float throttle01 = Mathf.Clamp01(Mathf.Abs(controller.throttleInput));
        float brake01 = Mathf.Clamp01(controller.brakeInput);
        float steer01 = Mathf.Clamp01(Mathf.Abs(controller.steerInput));
        float drive01 = Mathf.Clamp01(Mathf.Max(speed01, throttle01 * 0.9f));

        float slip01 = SampleSlip();
        float jolt01 = SampleJolt();
        float offroad01 = Mathf.Clamp01(speed01 * 0.55f + slip01 * 0.85f + jolt01 * 0.45f);
        float suspension01 = Mathf.Clamp01(speed01 * 0.2f + slip01 * 0.55f + jolt01);

        UpdateLoopSource(
            idleSource,
            idleLoop,
            Mathf.Lerp(idleMaxVolume, idleBaseVolume, drive01) * masterVolume,
            Mathf.Lerp(0.96f, 1.05f, drive01));

        UpdateLoopSource(
            engineSource,
            engineLoop,
            engineMaxVolume * drive01 * masterVolume,
            Mathf.Lerp(enginePitchMin, enginePitchMax, drive01));

        UpdateLoopSource(
            offroadSource,
            offroadLoop,
            grounded ? offroadMaxVolume * offroad01 * masterVolume : 0f,
            Mathf.Lerp(offroadPitchMin, offroadPitchMax, speed01));

        UpdateLoopSource(
            suspensionSource,
            suspensionRattleLoop,
            grounded ? suspensionMaxVolume * suspension01 * masterVolume : 0f,
            Mathf.Lerp(suspensionPitchMin, suspensionPitchMax, suspension01));

        HandleBrake(brake01, steer01, grounded);
        HandleLanding(grounded);
        HandleBoostTrigger();

        brakeCooldownTimer = Mathf.Max(0f, brakeCooldownTimer - Time.deltaTime);
        curveBrakeTimer = Mathf.Max(0f, curveBrakeTimer - Time.deltaTime);
        landingRetriggerBlockTimer = Mathf.Max(0f, landingRetriggerBlockTimer - Time.deltaTime);
        previousVelocity = rb.linearVelocity;
        wasGrounded = grounded;
        wasBraking = brake01 > 0.55f;
        wasCurving = steer01 > curveSteerThreshold;
        lastScriptedLaunchActive = controller.IsScriptedLaunchActive;
    }

    public void TriggerBoost(float volumeScale = 1f)
    {
        PlayOneShot(boostClip, Mathf.Clamp01(volumeScale));
    }

    public void ArmLandingForCurrentJump()
    {
        scriptedLandingArmed = true;
    }

    public void PlayScriptedLanding(float impactSpeed)
    {
        bool allowLandingSound = !landingOnlyOnArmedScriptedJump || scriptedLandingArmed;
        if (!allowLandingSound)
            return;

        float clampedImpact = Mathf.Max(impactSpeed, landingImpactThreshold + 2f);
        float volumeScale = Mathf.InverseLerp(landingImpactThreshold, landingImpactThreshold + 8f, clampedImpact);
        PlayLandingShot(Mathf.Lerp(0.78f, 1f, volumeScale));

        scriptedLandingArmed = false;
        landingRetriggerBlockTimer = 0.2f;
        airborneTimer = 0f;
        hardestLandingVelocity = 0f;
    }

    private void HandleBrake(float brake01, float steer01, bool grounded)
    {
        if (!grounded)
            return;

        float forwardSpeed = Mathf.Abs(controller.ForwardSpeed);
        bool brakingNow = brake01 > 0.55f;
        bool curvingNow = steer01 > curveSteerThreshold && forwardSpeed >= curveBrakeSpeedThreshold;

        if (brakingNow && !wasBraking && brakeCooldownTimer <= 0f && forwardSpeed >= brakeSpeedThreshold)
        {
            brakeCooldownTimer = brakeCooldown;
            PlayOneShot(brakeClip, Mathf.Lerp(0.55f, 1f, brake01));
            return;
        }

        if (!brakingNow && curvingNow && (!wasCurving || curveBrakeTimer <= 0f))
        {
            float steerStrength = Mathf.InverseLerp(curveSteerThreshold, 1f, steer01);
            float speedStrength = Mathf.InverseLerp(curveBrakeSpeedThreshold, Mathf.Max(curveBrakeSpeedThreshold + 0.01f, controller.maxForwardSpeed), forwardSpeed);
            float volumeScale = Mathf.Clamp01(0.35f + steerStrength * 0.4f + speedStrength * 0.25f);
            curveBrakeTimer = curveBrakeRetriggerDelay;
            PlayOneShot(brakeClip, volumeScale);
        }
    }

    private void HandleLanding(bool grounded)
    {
        if (!grounded)
        {
            airborneTimer += Time.deltaTime;
            hardestLandingVelocity = Mathf.Min(hardestLandingVelocity, rb.linearVelocity.y);
            return;
        }

        if (wasGrounded)
        {
            airborneTimer = 0f;
            hardestLandingVelocity = 0f;
            return;
        }

        if (landingRetriggerBlockTimer > 0f)
        {
            airborneTimer = 0f;
            hardestLandingVelocity = 0f;
            scriptedLandingArmed = false;
            return;
        }

        float impactSpeed = Mathf.Abs(hardestLandingVelocity);
        bool allowLandingSound = !landingOnlyOnArmedScriptedJump || scriptedLandingArmed;
        if (allowLandingSound && airborneTimer >= landingAirTimeThreshold && impactSpeed >= landingImpactThreshold)
        {
            float volumeScale = Mathf.InverseLerp(landingImpactThreshold, landingImpactThreshold + 8f, impactSpeed);
            PlayLandingShot(Mathf.Lerp(0.7f, 0.92f, volumeScale));
        }

        scriptedLandingArmed = false;
        airborneTimer = 0f;
        hardestLandingVelocity = 0f;
    }

    private void HandleBoostTrigger()
    {
        if (!playBoostOnScriptedLaunch || controller == null)
            return;

        if (controller.IsScriptedLaunchActive && !lastScriptedLaunchActive)
            TriggerBoost();
    }

    private float SampleSlip()
    {
        float totalSlip = 0f;
        int groundedWheels = 0;

        AccumulateWheelSlip(controller.wheelFL, ref totalSlip, ref groundedWheels);
        AccumulateWheelSlip(controller.wheelFR, ref totalSlip, ref groundedWheels);
        AccumulateWheelSlip(controller.wheelRL, ref totalSlip, ref groundedWheels);
        AccumulateWheelSlip(controller.wheelRR, ref totalSlip, ref groundedWheels);

        float rawSlip = groundedWheels > 0 ? totalSlip / groundedWheels : 0f;
        rawSlip = Mathf.Clamp01(rawSlip * 1.35f);
        smoothSlip = Mathf.MoveTowards(smoothSlip, rawSlip, Time.deltaTime * 4.5f);
        return smoothSlip;
    }

    private float SampleJolt()
    {
        Vector3 velocity = rb.linearVelocity;
        float verticalDelta = Mathf.Abs(velocity.y - previousVelocity.y);
        float angularJolt = new Vector2(rb.angularVelocity.x, rb.angularVelocity.z).magnitude;
        float rawJolt = Mathf.Clamp01(verticalDelta * 0.18f + angularJolt * 0.08f);
        smoothJolt = Mathf.MoveTowards(smoothJolt, rawJolt, Time.deltaTime * 5.5f);
        return smoothJolt;
    }

    private static void AccumulateWheelSlip(WheelCollider wheel, ref float totalSlip, ref int groundedWheels)
    {
        if (wheel == null || !wheel.isGrounded || !wheel.GetGroundHit(out WheelHit hit))
            return;

        groundedWheels++;
        totalSlip += Mathf.Abs(hit.forwardSlip) + Mathf.Abs(hit.sidewaysSlip) * 0.8f;
    }

    private void UpdateLoopSource(AudioSource source, AudioClip clip, float targetVolume, float targetPitch)
    {
        if (source == null)
            return;

        if (source.clip != clip)
        {
            source.clip = clip;
            if (clip == null)
            {
                source.Stop();
                return;
            }
        }

        if (clip == null)
            return;

        if (!source.isPlaying)
            source.Play();

        source.volume = Mathf.MoveTowards(source.volume, targetVolume, fadeSpeed * Time.deltaTime);
        source.pitch = Mathf.MoveTowards(source.pitch, targetPitch, Time.deltaTime * 3f);

        if (source.volume <= 0.001f && targetVolume <= 0.001f)
            source.volume = 0f;
    }

    private void PlayOneShot(AudioClip clip, float volumeScale)
    {
        if (clip == null || oneShotSource == null)
            return;

        oneShotSource.PlayOneShot(clip, volumeScale * masterVolume);
    }

    private void PlayLandingShot(float volumeScale)
    {
        if (landingClip == null)
            return;

        AudioSource source = landingSource != null ? landingSource : oneShotSource;
        if (source == null)
            return;

        source.pitch = Random.Range(landingPitchMin, landingPitchMax);
        source.PlayOneShot(landingClip, volumeScale * landingVolumeBoost * masterVolume);
    }

    private void ResolveAnchor()
    {
        if (controller != null && controller.seatAnchor != null)
        {
            audioAnchor = controller.seatAnchor;
            return;
        }

        if (audioAnchor == null)
            audioAnchor = transform;
    }

    private void EnsureAudioSources()
    {
        if (audioAnchor == null)
            return;

        Transform root = FindOrCreateChild(audioAnchor, "ImmersiveAudio");

        idleSource = EnsureSource(root, "Idle", true);
        engineSource = EnsureSource(root, "Engine", true);
        offroadSource = EnsureSource(root, "Offroad", true);
        suspensionSource = EnsureSource(root, "Suspension", true);
        oneShotSource = EnsureSource(root, "OneShots", false);
        landingSource = EnsureSource(root, "Landing", false);
        ConfigureLandingSource(landingSource);
    }

    private void ApplyClips()
    {
        AssignLoopClip(idleSource, idleLoop);
        AssignLoopClip(engineSource, engineLoop);
        AssignLoopClip(offroadSource, offroadLoop);
        AssignLoopClip(suspensionSource, suspensionRattleLoop);
    }

    private static void AssignLoopClip(AudioSource source, AudioClip clip)
    {
        if (source == null || source.clip == clip)
            return;

        source.clip = clip;
        if (clip == null)
            source.Stop();
    }

    private AudioSource EnsureSource(Transform parent, string childName, bool loop)
    {
        Transform child = FindOrCreateChild(parent, childName);
        AudioSource source = child.GetComponent<AudioSource>();
        bool created = false;
        if (source == null)
        {
            source = child.gameObject.AddComponent<AudioSource>();
            created = true;
        }

        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = interiorSpatialBlend;
        source.panStereo = 0f;
        source.spread = 360f;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.dopplerLevel = 0.05f;
        source.priority = 96;
        if (created)
        {
            source.volume = 0f;
            source.pitch = 1f;
        }
        return source;
    }

    private void ConfigureLandingSource(AudioSource source)
    {
        if (source == null)
            return;

        source.spatialBlend = landingSpatialBlend;
        source.panStereo = 0f;
        source.spread = 360f;
        source.minDistance = landingMinDistance;
        source.maxDistance = landingMaxDistance;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.dopplerLevel = 0.01f;
        source.priority = 72;
    }

    private static Transform FindOrCreateChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
            return child;

        GameObject childObject = new(childName);
        child = childObject.transform;
        child.SetParent(parent, false);
        child.localPosition = Vector3.zero;
        child.localRotation = Quaternion.identity;
        child.localScale = Vector3.one;
        return child;
    }
}
