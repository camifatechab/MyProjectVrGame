using UnityEngine;
using UnityEngine.XR;
using InputDevice = UnityEngine.XR.InputDevice;

/// <summary>
/// VR laser shooter for the right hand controller.
/// Attach to the Right Controller GameObject.
/// Fires a continuous laser beam while the right trigger is held.
/// </summary>
public class LaserShooter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Auto-found on parent if left empty.")]
    [SerializeField] private AutoJetpackController jetpackController;

    [Tooltip("Laser fires from here. Auto-created at controller tip if left empty.")]
    [SerializeField] private Transform laserOrigin;

    [Header("Laser Settings")]
    [SerializeField] private float laserRange = 50f;
    [SerializeField] private Color laserColor = new Color(0f, 1f, 0.8f);
    [SerializeField] private float laserWidth = 0.05f;

    [Header("Haptics")]
    [SerializeField] private float rhythmBurstIntensity = 0.6f;
    [SerializeField] private float rhythmBurstDuration  = 0.04f;
    [SerializeField] private float rhythmPauseTime      = 0.08f;
    [SerializeField] private float killHapticIntensity  = 0.8f;
    [SerializeField] private float killHapticDuration   = 0.3f;

    [Header("Vignette")]
    [Tooltip("Auto-found on Main Camera if left empty.")]
    [SerializeField] private LaserVignetteEffect vignetteEffect;

    [Header("Dev / Testing")]
    [Tooltip("Uncheck to fire without needing to activate the jetpack first.")]
    [SerializeField] private bool requireFlying = true;

    [Header("Audio")]
    [SerializeField] private AudioClip fireSound;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip emptyClickSound;
    [SerializeField] private AudioClip travelWhooshSound;
    private AudioSource gunAudio;
    private float hitSoundCooldown = 0f;
    private bool  emptyClickedLastFrame = false;

    // --- Private ---
    private LineRenderer    lineRenderer;
    private InputDevice     rightDevice;
    private ParticleSystem  impactParticle;
    private GameObject      readyDot;

    private bool  isLaserActive  = false;
    private float rhythmTimer    = 0f;
    private bool  rhythmInBurst  = true;

    // ─────────────────────────────────────────────
    // LIFECYCLE
    // ─────────────────────────────────────────────

    void Start()
    {
        SetupLineRenderer();
        SetupImpactParticle();
        SetupReadyIndicator();
        SetupAudio();
        InitializeXRDevice();
        AutoFindReferences();
    }

void Update()
    {
        if (!rightDevice.isValid)
            InitializeXRDevice();

        bool armed      = IsArmed();
        bool triggerHeld = IsTriggerHeld();
        bool shouldFire = armed && triggerHeld;

        // Empty click when not armed but trigger pressed
        if (triggerHeld && !armed && !emptyClickedLastFrame)
        {
            if (gunAudio != null && emptyClickSound != null)
                gunAudio.PlayOneShot(emptyClickSound, 0.8f);
        }
        emptyClickedLastFrame = triggerHeld && !armed;

        hitSoundCooldown -= Time.deltaTime;

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
        return !requireFlying || (jetpackController != null && jetpackController.IsFlying());
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

    // ─────────────────────────────────────────────
    // LASER
    // ─────────────────────────────────────────────

void FireLaser()
    {
        if (!isLaserActive)
        {
            isLaserActive        = true;
            lineRenderer.enabled = true;
            rhythmTimer          = 0f;
            rhythmInBurst        = true;
            // Fire sound on activation
            if (gunAudio != null && fireSound != null)
                gunAudio.PlayOneShot(fireSound, 1f);
            // Start looping whoosh
            if (gunAudio != null && travelWhooshSound != null)
            {
                gunAudio.clip   = travelWhooshSound;
                gunAudio.loop   = true;
                gunAudio.volume = 0.5f;
                gunAudio.Play();
            }
        }

        if (vignetteEffect != null)
            vignetteEffect.SetActive(true);

        // Rhythmic haptics
        rhythmTimer += Time.deltaTime;
        float interval = rhythmInBurst ? rhythmBurstDuration : rhythmPauseTime;
        if (rhythmTimer >= interval)
        {
            rhythmTimer   = 0f;
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

            LaserTarget target = hit.collider.GetComponent<LaserTarget>()
                              ?? hit.collider.GetComponentInParent<LaserTarget>();
            if (target != null)
            {
                // Hit sound (throttled so it doesn't spam every frame)
                if (hitSoundCooldown <= 0f && gunAudio != null && hitSound != null)
                {
                    gunAudio.PlayOneShot(hitSound, 0.7f);
                    hitSoundCooldown = 0.15f;
                }
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
        if (!isLaserActive) return;
        isLaserActive        = false;
        lineRenderer.enabled = false;
        impactParticle.Stop();
        // Stop whoosh loop
        if (gunAudio != null && gunAudio.isPlaying)
            gunAudio.Stop();
        if (vignetteEffect != null)
            vignetteEffect.SetActive(false);
    }

    public bool IsLaserActive() => isLaserActive;

    // ─────────────────────────────────────────────
    // SETUP
    // ─────────────────────────────────────────────

    void SetupLineRenderer()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.startWidth    = laserWidth;
        lineRenderer.endWidth      = laserWidth * 0.4f;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.enabled       = false;

        Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        Material mat = new Material(sh);
        mat.SetColor("_BaseColor", laserColor);
        mat.SetColor("_Color",     laserColor);
        lineRenderer.material   = mat;
        lineRenderer.startColor = laserColor;
        lineRenderer.endColor   = new Color(laserColor.r, laserColor.g, laserColor.b, 0.3f);
        lineRenderer.SetPosition(0, Vector3.zero);
        lineRenderer.SetPosition(1, Vector3.forward * 0.1f);

        Debug.Log("<color=cyan>✓ LaserShooter: LineRenderer ready</color>");
    }

    void SetupImpactParticle()
    {
        GameObject pfxGO = new GameObject("LaserImpactParticle");
        pfxGO.transform.SetParent(transform);
        pfxGO.transform.localPosition = new Vector3(0f, 0f, 0.1f);

        impactParticle = pfxGO.AddComponent<ParticleSystem>();

        var main = impactParticle.main;
        main.loop            = false;
        main.playOnAwake     = false;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(6f, 16f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
        main.maxParticles    = 150;
        main.startColor      = new ParticleSystem.MinMaxGradient(Color.yellow, new Color(1f, 0.3f, 0f));
        main.gravityModifier = 0.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = impactParticle.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 60) });

        var shape = impactParticle.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.15f;

        var col = impactParticle.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.yellow,              0f),
                new GradientColorKey(new Color(1f, 0.15f, 0f), 0.5f),
                new GradientColorKey(new Color(0.3f,0.3f,0.3f),1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f,  0f),
                new GradientAlphaKey(0.8f,0.4f),
                new GradientAlphaKey(0f,  1f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        var pfxRenderer = impactParticle.GetComponent<ParticleSystemRenderer>();
        pfxRenderer.material = new Material(sh);

        pfxGO.SetActive(false);
        Debug.Log("<color=cyan>✓ LaserShooter: ImpactParticle ready</color>");
    }

    void SetupReadyIndicator()
    {
        readyDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        readyDot.name = "LaserReadyDot";
        readyDot.transform.SetParent(transform);
        readyDot.transform.localPosition = new Vector3(0f, 0f, 0.12f);
        readyDot.transform.localScale    = Vector3.one * 0.015f;
        Destroy(readyDot.GetComponent<Collider>());

        Shader sh  = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        Material m = new Material(sh);
        m.SetColor("_BaseColor", laserColor);
        m.SetColor("_Color",     laserColor);
        readyDot.GetComponent<Renderer>().material = m;

        readyDot.SetActive(false);
        Debug.Log("<color=cyan>✓ LaserShooter: ReadyDot ready</color>");
    }

    void InitializeXRDevice()
    {
        var devices = new System.Collections.Generic.List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
        if (devices.Count > 0)
        {
            rightDevice = devices[0];
            Debug.Log("<color=cyan>✓ LaserShooter: Right XR device found — " + rightDevice.name + "</color>");
        }
    }

    void AutoFindReferences()
    {
        if (jetpackController == null)
        {
            jetpackController = GetComponentInParent<AutoJetpackController>();
            if (jetpackController == null)
                Debug.LogWarning("[LaserShooter] AutoJetpackController not found. Assign manually or disable requireFlying.");
        }

        if (laserOrigin == null)
        {
            Transform existing = transform.Find("LaserOrigin");
            if (existing != null)
            {
                laserOrigin = existing;
            }
            else
            {
                GameObject go = new GameObject("LaserOrigin");
                go.transform.SetParent(transform);
                go.transform.localPosition = new Vector3(0f, 0f, 0.1f);
                go.transform.localRotation = Quaternion.identity;
                laserOrigin = go.transform;
            }
        }

        if (vignetteEffect == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                vignetteEffect = cam.GetComponent<LaserVignetteEffect>()
                              ?? cam.gameObject.AddComponent<LaserVignetteEffect>();
            }
        }
    }

void SetupAudio()
    {
        gunAudio = GetComponent<AudioSource>();
        if (gunAudio == null)
            gunAudio = gameObject.AddComponent<AudioSource>();
        gunAudio.spatialBlend = 0f; // 2D
        gunAudio.playOnAwake  = false;
    }

}
