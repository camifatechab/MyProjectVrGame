using UnityEngine;
using UnityEngine.XR;
using InputDevice = UnityEngine.XR.InputDevice;

/// <summary>
/// Buzz Lightyear-style laser shooter for the right hand.
/// Attach to the Right Controller GameObject.
///
/// FIRE CONDITIONS:
///   1. requireFlying = false  OR  player IsFlying()
///   2. Right trigger held > 0.5
///   3. Wrist aimed forward/upward (not pointing at floor)
///
/// IMPROVEMENTS:
///   - Impact spark particle at raycast hit point
///   - Haptic rumble while firing + kill burst via HapticsManager
///   - Ready indicator (glowing dot) when armed but not firing
///   - requireFlying inspector toggle for easy testing
/// </summary>
public class LaserShooter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("XR Origin root with AutoJetpackController. Auto-found if left empty.")]
    [SerializeField] private AutoJetpackController jetpackController;

    [Tooltip("Laser fires from here. Auto-created at controller tip if left empty.")]
    [SerializeField] private Transform laserOrigin;

    [Header("Laser Settings")]
    [SerializeField] private float laserRange = 50f;
    [SerializeField] private Color laserColor = new Color(0f, 1f, 0.8f);
    [SerializeField] private float laserWidth = 0.05f;

    [Header("Wrist Aim")]
    [Tooltip("Angle from straight-down. 90=horizontal. Higher = more lenient.")]
    [SerializeField] private float aimAngleThreshold = 70f;

    [Header("Haptics")]
    [SerializeField] private float firingHapticIntensity = 0.2f;
    [SerializeField] private float firingHapticDuration = 0.05f;
    [SerializeField] private float hapticInterval = 0.05f;
    [SerializeField] private float killHapticIntensity = 0.8f;
    [SerializeField] private float killHapticDuration = 0.3f;

        [Header("Vignette")]
    [Tooltip("Auto-found on Main Camera if left empty.")]
    [SerializeField] private LaserVignetteEffect vignetteEffect;

    [Header("Rhythmic Haptics")]
    [SerializeField] private float rhythmBurstIntensity = 0.6f;
    [SerializeField] private float rhythmBurstDuration  = 0.04f;
    [SerializeField] private float rhythmPauseTime      = 0.08f;
    private float rhythmTimer = 0f;
    private bool  rhythmInBurst = true;

[Header("Dev / Testing")]
    [Tooltip("Uncheck to fire without needing to activate the jetpack first")]
    [SerializeField] private bool requireFlying = true;

    // Private - laser beam
    private LineRenderer lineRenderer;
    private InputDevice rightDevice;
    private bool isLaserActive = false;

    // Private - haptics
    private float hapticTimer = 0f;

    // Private - impact particle
    private ParticleSystem impactParticle;

    // Private - ready indicator
    private GameObject readyDot;

    void Start()
    {
        SetupLineRenderer();
        SetupImpactParticle();
        SetupReadyIndicator();
        InitializeXRDevice();
        AutoFindReferences();
    }

    // ─────────────────────────────────────────────
    // SETUP
    // ─────────────────────────────────────────────

void SetupLineRenderer()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.startWidth = laserWidth;
        lineRenderer.endWidth = laserWidth * 0.4f;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.enabled = false;

        Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (urpUnlit == null) urpUnlit = Shader.Find("Unlit/Color");
        if (urpUnlit == null) urpUnlit = Shader.Find("Sprites/Default");

        Material laserMat = new Material(urpUnlit);
        laserMat.SetColor("_BaseColor", laserColor);
        laserMat.SetColor("_Color", laserColor);
        lineRenderer.material = laserMat;
        lineRenderer.startColor = laserColor;
        lineRenderer.endColor = new Color(laserColor.r, laserColor.g, laserColor.b, 0.3f);

        // Set valid non-zero initial positions — prevents Invalid AABB errors
        lineRenderer.SetPosition(0, Vector3.zero);
        lineRenderer.SetPosition(1, Vector3.forward * 0.1f);

        Debug.Log("<color=cyan>\u2713 LaserShooter beam ready | Shader: " + urpUnlit?.name + "</color>");
    }

void SetupImpactParticle()
    {
        GameObject pfxGO = new GameObject("LaserImpactParticle");
        pfxGO.transform.SetParent(transform);
        pfxGO.transform.localPosition = new Vector3(0f, 0f, 0.1f);
        pfxGO.transform.localRotation = Quaternion.identity;

        impactParticle = pfxGO.AddComponent<ParticleSystem>();

        var main = impactParticle.main;
        main.loop         = false;
        main.playOnAwake  = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
        main.startSpeed    = new ParticleSystem.MinMaxCurve(4f, 10f);
        main.startSize     = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.maxParticles  = 80;
        main.startColor    = new ParticleSystem.MinMaxGradient(Color.yellow, new Color(1f, 0.3f, 0f));
        main.gravityModifier = 0.4f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = impactParticle.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });

        var shape = impactParticle.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.05f;

        // Fade out over lifetime
        var col = impactParticle.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.yellow, 0f), new GradientColorKey(new Color(1f,0.2f,0f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        // Shrink over lifetime
        var sizeLife = impactParticle.sizeOverLifetime;
        sizeLife.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var pfxRenderer = impactParticle.GetComponent<ParticleSystemRenderer>();
        pfxRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (urpUnlit == null) urpUnlit = Shader.Find("Sprites/Default");
        Material pfxMat = new Material(urpUnlit);
        pfxMat.SetColor("_BaseColor", Color.yellow);
        pfxMat.SetColor("_Color", Color.yellow);
        pfxRenderer.material = pfxMat;

        pfxGO.SetActive(false);
        Debug.Log("<color=cyan>✓ LaserShooter impact particle ready</color>");
    }

    void SetupReadyIndicator()
    {
        readyDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        readyDot.name = "LaserReadyDot";
        readyDot.transform.SetParent(transform);
        readyDot.transform.localPosition = new Vector3(0f, 0f, 0.12f);
        readyDot.transform.localScale = Vector3.one * 0.015f;

        Destroy(readyDot.GetComponent<Collider>());

        Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (urpUnlit == null) urpUnlit = Shader.Find("Sprites/Default");
        Material dotMat = new Material(urpUnlit);
        dotMat.SetColor("_BaseColor", laserColor);
        dotMat.SetColor("_Color", laserColor);
        readyDot.GetComponent<Renderer>().material = dotMat;

        readyDot.SetActive(false);
        Debug.Log("<color=cyan>✓ LaserShooter ready indicator created</color>");
    }

    void InitializeXRDevice()
    {
        var devices = new System.Collections.Generic.List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
        if (devices.Count > 0)
        {
            rightDevice = devices[0];
            Debug.Log("<color=cyan>✓ LaserShooter: Right device found: " + rightDevice.name + "</color>");
        }
    }

void AutoFindReferences()
    {
        if (jetpackController == null)
        {
            jetpackController = GetComponentInParent<AutoJetpackController>();
            if (jetpackController != null)
                Debug.Log("<color=cyan>✓ LaserShooter: Found AutoJetpackController on parent</color>");
            else
                Debug.LogWarning("[LaserShooter] AutoJetpackController not found. Assign manually or disable requireFlying.");
        }

        if (laserOrigin == null)
        {
            Transform existing = transform.Find("LaserOrigin");
            laserOrigin = existing != null ? existing : CreateLaserOrigin();
            Debug.Log("<color=cyan>✓ LaserShooter: LaserOrigin ready</color>");
        }

        if (vignetteEffect == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                vignetteEffect = cam.gameObject.GetComponent<LaserVignetteEffect>();
                if (vignetteEffect == null)
                    vignetteEffect = cam.gameObject.AddComponent<LaserVignetteEffect>();
                Debug.Log("<color=cyan>✓ LaserShooter: LaserVignetteEffect ready on camera</color>");
            }
        }
    }

    Transform CreateLaserOrigin()
    {
        GameObject origin = new GameObject("LaserOrigin");
        origin.transform.SetParent(transform);
        origin.transform.localPosition = new Vector3(0f, 0f, 0.1f);
        origin.transform.localRotation = Quaternion.identity;
        return origin.transform;
    }

    // ─────────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────────

    void Update()
    {
        if (!rightDevice.isValid) InitializeXRDevice();

        bool armed = IsArmed();
        bool shouldFire = armed && IsTriggerHeld();

        if (readyDot != null)
            readyDot.SetActive(armed && !shouldFire);

        if (shouldFire)
            FireLaser();
        else
            StopLaser();
    }

    // ─────────────────────────────────────────────
    // FIRE CONDITIONS
    // ─────────────────────────────────────────────

bool IsArmed()
    {
        bool flyingOk = !requireFlying || (jetpackController != null && jetpackController.IsFlying());
        return flyingOk;
    }

    bool IsTriggerHeld()
    {
        if (!rightDevice.isValid) return false;
        float val = 0f;
        if (rightDevice.TryGetFeatureValue(CommonUsages.trigger, out val))
            return val > 0.5f;
        bool btn = false;
        rightDevice.TryGetFeatureValue(CommonUsages.triggerButton, out btn);
        return btn;
    }

bool IsWristAimed()
    {
        return true; // Removed — no wrist angle restriction
    }

    // ─────────────────────────────────────────────
    // LASER
    // ─────────────────────────────────────────────

void FireLaser()
    {
        if (!isLaserActive)
        {
            isLaserActive = true;
            lineRenderer.enabled = true;
            rhythmTimer   = 0f;
            rhythmInBurst = true;
            Debug.Log("<color=green>\u26a1 LASER ON</color>");
        }

        // Vignette on
        if (vignetteEffect != null) vignetteEffect.SetActive(true);

        // Rhythmic haptic pattern: burst -> pause -> burst
        rhythmTimer += Time.deltaTime;
        float rhythmInterval = rhythmInBurst ? rhythmBurstDuration : rhythmPauseTime;
        if (rhythmTimer >= rhythmInterval)
        {
            rhythmTimer = 0f;
            rhythmInBurst = !rhythmInBurst;
            if (rhythmInBurst && HapticsManager.Instance != null)
                HapticsManager.Instance.PulseRight(rhythmBurstIntensity, rhythmBurstDuration);
        }

        Vector3 origin    = laserOrigin.position;
        Vector3 direction = laserOrigin.forward;
        lineRenderer.SetPosition(0, origin);

        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, laserRange))
        {
            lineRenderer.SetPosition(1, hit.point);

            impactParticle.transform.position = hit.point;
            if (!impactParticle.gameObject.activeSelf)
                impactParticle.gameObject.SetActive(true);
            if (!impactParticle.isPlaying)
                impactParticle.Play();

            LaserTarget target = hit.collider.GetComponent<LaserTarget>();
            if (target != null)
            {
                bool wasAlive = !target.IsDead();
                target.OnLaserHit();

                if (wasAlive && target.IsDead() && HapticsManager.Instance != null)
                    HapticsManager.Instance.PulseRight(killHapticIntensity, killHapticDuration);
            }
        }
        else
        {
            lineRenderer.SetPosition(1, origin + direction * laserRange);
            if (impactParticle.gameObject.activeSelf)
                impactParticle.gameObject.SetActive(false);
        }
    }

void StopLaser()
    {
        if (isLaserActive)
        {
            isLaserActive = false;
            lineRenderer.enabled = false;
            impactParticle.Stop();
            hapticTimer = 0f;
            if (vignetteEffect != null) vignetteEffect.SetActive(false);
            Debug.Log("<color=yellow>\u26a1 LASER OFF</color>");
        }
    }

    public bool IsLaserActive() => isLaserActive;
}
