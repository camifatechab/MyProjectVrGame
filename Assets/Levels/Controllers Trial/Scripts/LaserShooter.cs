using System;
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

    [Tooltip("Optional rover controller. When mounted, the laser is allowed even if not flying.")]
    [SerializeField] private RoverPhysicsController roverController;

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

    [Header("Hit Feedback")]
    [SerializeField] private Color hitLaserColor = new Color(1f, 0.3f, 0f);

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
    private GameObject      hitMarkerRoot;
    private float           hitMarkerTimer;
    private const float     HitMarkerDuration = 0.12f;

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
        SetupHitMarker();
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

        if (hitMarkerTimer > 0f)
        {
            hitMarkerTimer -= Time.deltaTime;
            if (hitMarkerTimer <= 0f && hitMarkerRoot != null)
                hitMarkerRoot.SetActive(false);
        }

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
        if (!requireFlying)
            return true;

        if (roverController != null && roverController.IsMounted)
            return true;

        return jetpackController != null && jetpackController.IsFlying();
    }

    bool IsTriggerHeld()
    {
        if (!rightDevice.isValid) return false;

        float val = 0f;
        if (rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out val))
            return val > 0.5f;

        bool btn = false;
        rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out btn);
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

        bool hittingTarget = false;

        if (TryGetLaserHit(origin, direction, out RaycastHit hit))
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
                hittingTarget = true;
                hitMarkerTimer = HitMarkerDuration;
                if (hitMarkerRoot != null) hitMarkerRoot.SetActive(true);

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
            else
            {
                // Check for the final crystal
                CrystalDestructible crystal = hit.collider.GetComponent<CrystalDestructible>()
                                           ?? hit.collider.GetComponentInParent<CrystalDestructible>();
                if (crystal != null && !crystal.IsDead())
                {
                    hittingTarget = true;
                    hitMarkerTimer = HitMarkerDuration;
                    if (hitMarkerRoot != null) hitMarkerRoot.SetActive(true);

                    if (hitSoundCooldown <= 0f && gunAudio != null && hitSound != null)
                    {
                        gunAudio.PlayOneShot(hitSound, 0.7f);
                        hitSoundCooldown = 0.15f;
                    }
                    crystal.OnLaserHit();
                    if (crystal.IsDead() && HapticsManager.Instance != null)
                        HapticsManager.Instance.PulseRight(killHapticIntensity, killHapticDuration);
                }
            }
        }
        else
        {
            lineRenderer.SetPosition(1, origin + direction * laserRange);
            if (impactParticle.gameObject.activeSelf)
                impactParticle.gameObject.SetActive(false);
        }

        UpdateLaserColor(hittingTarget);
    }

    bool TryGetLaserHit(Vector3 origin, Vector3 direction, out RaycastHit validHit)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, laserRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, static (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (ShouldIgnoreHit(hit.collider))
                continue;

            validHit = hit;
            return true;
        }

        validHit = default;
        return false;
    }

    bool ShouldIgnoreHit(Collider collider)
    {
        if (collider == null)
            return true;

        Transform hitTransform = collider.transform;
        if (hitTransform == transform || hitTransform.IsChildOf(transform))
            return true;

        if (roverController != null && roverController.IsMounted)
        {
            RoverPhysicsController hitRover = collider.GetComponentInParent<RoverPhysicsController>();
            if (hitRover != null && hitRover == roverController)
                return true;
        }

        return false;
    }

void StopLaser()
    {
        if (!isLaserActive) return;
        isLaserActive        = false;
        lineRenderer.enabled = false;
        UpdateLaserColor(false);
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

        if (roverController == null)
            roverController = FindFirstObjectByType<RoverPhysicsController>();

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

    void SetupHitMarker()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        hitMarkerRoot = new GameObject("HitMarker");
        hitMarkerRoot.transform.SetParent(cam.transform);
        hitMarkerRoot.transform.localPosition = new Vector3(0f, 0f, 0.4f);
        hitMarkerRoot.transform.localRotation = Quaternion.identity;

        Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        Material markerMat = new Material(sh);
        markerMat.SetColor("_BaseColor", Color.white);
        markerMat.SetColor("_Color", Color.white);

        float gap = 0.003f;
        float armEnd = 0.012f;
        float width = 0.0015f;

        MakeHitMarkerLine(markerMat, new Vector3(gap, gap, 0f), new Vector3(armEnd, armEnd, 0f), width);
        MakeHitMarkerLine(markerMat, new Vector3(-gap, gap, 0f), new Vector3(-armEnd, armEnd, 0f), width);
        MakeHitMarkerLine(markerMat, new Vector3(gap, -gap, 0f), new Vector3(armEnd, -armEnd, 0f), width);
        MakeHitMarkerLine(markerMat, new Vector3(-gap, -gap, 0f), new Vector3(-armEnd, -armEnd, 0f), width);

        hitMarkerRoot.SetActive(false);
    }

    void MakeHitMarkerLine(Material mat, Vector3 start, Vector3 end, float width)
    {
        GameObject lineGO = new GameObject("HitArm");
        lineGO.transform.SetParent(hitMarkerRoot.transform, false);

        LineRenderer lr = lineGO.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.startWidth = width;
        lr.endWidth = width;
        lr.startColor = Color.white;
        lr.endColor = Color.white;
        lr.material = mat;
    }

    void UpdateLaserColor(bool hittingTarget)
    {
        Color c = hittingTarget ? hitLaserColor : laserColor;
        lineRenderer.startColor = c;
        lineRenderer.endColor = new Color(c.r, c.g, c.b, 0.3f);
        lineRenderer.material.SetColor("_BaseColor", c);
        lineRenderer.material.SetColor("_Color", c);
    }

}
