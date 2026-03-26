using UnityEngine;

/// Attach to Rover_Cami_Trial.
/// Creates a pulsing ground ring + proximity particle burst to signal the player to interact.
/// Everything is tweakable in the Inspector.
public class RoverInteractIndicator : MonoBehaviour
{
    [Header("Ground Ring")]
    public Color ringColor         = new Color(0.2f, 0.8f, 1f, 0.6f);
    public float ringRadius        = 2.0f;
    public float ringWidth         = 0.08f;
    public float pulseSpeed        = 1.8f;
    [Range(0f, 1f)]
    public float pulseMinAlpha     = 0.15f;
    [Range(0f, 1f)]
    public float pulseMaxAlpha     = 0.75f;
    public float ringYOffset       = 0.05f;   // height above ground

    [Header("Proximity Particles")]
    public bool  showParticles     = true;
    public Color particleColor     = new Color(0.2f, 0.9f, 1f, 1f);
    public float particleRadius    = 1.8f;
    public int   particleCount     = 12;
    public float particleSize      = 0.08f;
    public float particleRiseSpeed = 0.6f;
    public float particleLifetime  = 1.2f;

    [Header("Behaviour")]
    public float proximityRange    = 4.0f;   // player must be within this to see particles
    public bool  hideWhenMounted   = true;

    // --- runtime ---
    private RoverDriver    rover;
    private Transform      player;
    private LineRenderer   ring;
    private ParticleSystem particles;
    private bool           wasVisible = true;

    void Start()
    {
        rover  = GetComponent<RoverDriver>();
        var xr = FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
        if (xr != null) player = xr.transform;

        BuildRing();
        BuildParticles();
    }

    void Update()
    {
        bool mounted = rover != null && rover.IsMounted;
        bool visible = !(hideWhenMounted && mounted);

        if (visible != wasVisible)
        {
            ring.gameObject.SetActive(visible);
            if (particles != null) particles.gameObject.SetActive(visible);
            wasVisible = visible;
        }

        if (!visible) return;

        // Pulse ring alpha
        float t     = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, t);
        Color c     = ringColor;
        c.a         = alpha;
        ring.startColor = c;
        ring.endColor   = c;

        // Proximity particles
        if (particles != null && showParticles && player != null)
        {
            float dist = Vector3.Distance(
                new Vector3(player.position.x, 0, player.position.z),
                new Vector3(transform.position.x, 0, transform.position.z));

            var em = particles.emission;
            em.enabled = dist < proximityRange;
        }
    }

    void BuildRing()
    {
        var go = new GameObject("InteractRing");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.up * ringYOffset;

        ring = go.AddComponent<LineRenderer>();
        ring.useWorldSpace    = false;
        ring.loop             = true;
        ring.widthMultiplier  = ringWidth;
        ring.positionCount    = 64;
        ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ring.receiveShadows   = false;

        // Use Sprites/Default shader — always available, supports transparency
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = ringColor;
        ring.material = mat;

        // Build circle points
        for (int i = 0; i < 64; i++)
        {
            float angle = i / 64f * Mathf.PI * 2f;
            ring.SetPosition(i, new Vector3(
                Mathf.Cos(angle) * ringRadius,
                0f,
                Mathf.Sin(angle) * ringRadius));
        }
    }

    void BuildParticles()
    {
        if (!showParticles) return;

        var go = new GameObject("InteractParticles");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.up * 0.1f;

        particles = go.AddComponent<ParticleSystem>();

        var main         = particles.main;
        main.loop        = true;
        main.playOnAwake = true;
        main.startLifetime  = particleLifetime;
        main.startSpeed     = particleRiseSpeed;
        main.startSize      = particleSize;
        main.startColor     = particleColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles   = particleCount * 2;

        var em       = particles.emission;
        em.rateOverTime = particleCount / particleLifetime;

        var shape        = particles.shape;
        shape.enabled    = true;
        shape.shapeType  = ParticleSystemShapeType.Circle;
        shape.radius     = particleRadius;
        shape.radiusThickness = 0.1f;

        // Velocity — rise upward
        var vel          = particles.velocityOverLifetime;
        vel.enabled      = true;
        vel.space        = ParticleSystemSimulationSpace.Local;
        var upCurve      = AnimationCurve.Linear(0, 1, 1, 0);
        vel.y            = new ParticleSystem.MinMaxCurve(particleRiseSpeed, upCurve);

        // Fade out over lifetime
        var col          = particles.colorOverLifetime;
        col.enabled      = true;
        var gradient     = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(particleColor, 0f), new GradientColorKey(particleColor, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color        = gradient;

        var renderer     = particles.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.material.color = particleColor;
    }
}
