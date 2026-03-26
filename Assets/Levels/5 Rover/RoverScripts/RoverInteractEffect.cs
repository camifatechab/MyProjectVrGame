using UnityEngine;

/// Pulsing ground ring + proximity particle burst for the rover mount point.
/// Attach to Rover_Cami_Trial. No dependencies needed.
public class RoverInteractEffect : MonoBehaviour
{
    [Header("Ring")]
    public float ringRadius      = 2.0f;
    public float ringBaseAlpha   = 0.35f;
    public float pulseSpeedFar   = 1.2f;
    public float pulseSpeedNear  = 3.5f;
    public Color ringColor       = new Color(0.2f, 0.9f, 1f, 1f); // cyan

    [Header("Proximity")]
    public float nearDistance    = 3.5f;

    // runtime
    private GameObject   ringObj;
    private MeshRenderer ringRenderer;
    private Material     ringMat;
    private GameObject   particleObj;
    private ParticleSystem particles;
    private Transform    player;
    private RoverDriver  driver;
    private float        pulseT;

    void Start()
    {
        driver = GetComponent<RoverDriver>();
        var xrOrigin = FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin != null) player = xrOrigin.transform;

        BuildRing();
        BuildParticles();
    }

    void Update()
    {
        if (driver != null && driver.IsMounted)
        {
            ringObj.SetActive(false);
            particleObj.SetActive(false);
            return;
        }

        ringObj.SetActive(true);

        float dist = player != null
            ? Vector3.Distance(transform.position, player.position)
            : float.MaxValue;

        bool isNear = dist < nearDistance;

        // Pulse speed
        float speed = isNear ? pulseSpeedNear : pulseSpeedFar;
        pulseT += Time.deltaTime * speed;
        float pulse = (Mathf.Sin(pulseT) + 1f) * 0.5f; // 0..1

        // Ring alpha + scale
        float alpha = Mathf.Lerp(ringBaseAlpha * 0.4f, ringBaseAlpha, pulse);
        float scale = Mathf.Lerp(0.85f, 1.05f, pulse);
        ringMat.color = new Color(ringColor.r, ringColor.g, ringColor.b, alpha);
        ringObj.transform.localScale = Vector3.one * ringRadius * 2f * scale;

        // Particles only when near
        if (isNear && !particles.isPlaying)  particles.Play();
        if (!isNear && particles.isPlaying)  particles.Stop();
    }

    void BuildRing()
    {
        ringObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ringObj.name = "RoverRingEffect";
        ringObj.transform.SetParent(transform, false);
        ringObj.transform.localPosition = new Vector3(0f, -0.42f, 0f);
        ringObj.transform.localScale    = Vector3.one * ringRadius * 2f;

        // Remove collider
        Destroy(ringObj.GetComponent<Collider>());

        // Flat disc — squish Y
        ringObj.transform.localScale = new Vector3(ringRadius * 2f, 0.02f, ringRadius * 2f);

        // Material — Unlit transparent
        ringMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        ringMat.SetFloat("_Surface", 1);           // transparent
        ringMat.SetFloat("_Blend", 0);             // alpha
        ringMat.SetFloat("_AlphaClip", 0);
        ringMat.enableInstancing = true;
        ringMat.renderQueue = 3000;
        ringMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        ringMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        ringMat.SetInt("_ZWrite", 0);
        ringMat.color = ringColor;
        ringMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        ringRenderer = ringObj.GetComponent<MeshRenderer>();
        ringRenderer.material = ringMat;
        ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ringRenderer.receiveShadows    = false;
    }

    void BuildParticles()
    {
        particleObj = new GameObject("RoverParticleEffect");
        particleObj.transform.SetParent(transform, false);
        particleObj.transform.localPosition = new Vector3(0f, 0.1f, 0f);

        particles = particleObj.AddComponent<ParticleSystem>();

        var main = particles.main;
        main.loop           = true;
        main.startLifetime  = 1.2f;
        main.startSpeed     = 1.5f;
        main.startSize      = 0.08f;
        main.startColor     = new Color(0.2f, 0.9f, 1f, 0.9f);
        main.maxParticles   = 40;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = particles.emission;
        emission.rateOverTime = 20f;

        // Emit from a circle on the ground
        var shape = particles.shape;
        shape.enabled    = true;
        shape.shapeType  = ParticleSystemShapeType.Circle;
        shape.radius     = ringRadius * 0.9f;
        shape.rotation   = new Vector3(0f, 0f, 0f);

        // Drift upward
        var vel = particles.velocityOverLifetime;
        vel.enabled = true;
        vel.space   = ParticleSystemSimulationSpace.Local;
        vel.y       = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);

        // Fade out
        var col = particles.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(new Color(0.2f, 0.9f, 1f), 0f),
                    new GradientColorKey(new Color(0.2f, 0.9f, 1f), 1f) },
            new[] { new GradientAlphaKey(0.9f, 0f),
                    new GradientAlphaKey(0f,   1f) });
        col.color = grad;

        particles.Stop();
    }
}
