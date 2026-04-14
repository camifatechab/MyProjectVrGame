using System;
using UnityEngine;

/// <summary>
/// Makes a static mesh destructible by the player's laser gun.
/// Attach to obstacle meshes such as Hive_spireB so they block the rover
/// until shot down, then immediately stop blocking the path.
/// </summary>
public class DestructiblePlant : MonoBehaviour
{
    public event Action Destroyed;

    [Header("Plant Health")]
    [Tooltip("Hit points before the plant is destroyed. Plants are one-shot by default.")]
    public float maxHP = 1f;

    [Tooltip("Laser damage received per second. Keep this high for instant destruction.")]
    public float damagePerSecond = 9999f;

    [Header("Health Bar")]
    [Tooltip("Height of the floating health bar above the plant pivot.")]
    public float barOffsetY = 3f;

    [Tooltip("Width of the floating health bar.")]
    public float barWidth = 1.2f;

    [Header("Destruction Effects")]
    [Tooltip("Spawn plant debris particles on death.")]
    public bool spawnPlantDebris = true;

    [Header("Shoot Cue")]
    [Tooltip("Show a cyan marker so the player knows this plant is a shootable blocker.")]
    public bool showShootCue = true;

    [Tooltip("World height above the plant where the shoot cue should float.")]
    public float shootCueHeight = 2.2f;

    [Tooltip("Base size of the floating cue.")]
    public float shootCueSize = 1.8f;

    [Tooltip("Point light range for the cyan cue.")]
    public float shootCueLightRange = 6f;

    private LaserTarget laserTarget;
    private Collider[] colliders;
    private Renderer[] renderers;
    private Transform cueRoot;
    private Transform cueDiamond;
    private Transform cueBeam;
    private Light cueLight;
    private Material cueDiamondMaterial;
    private Material cueBeamMaterial;
    private Transform cueCamera;
    private float cuePulseOffset;
    private bool isDead;

    public bool IsDead => isDead;

    private void Start()
    {
        EnsureLaserTarget();
        EnsureCollider();
        colliders = GetComponentsInChildren<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
        cuePulseOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

        if (showShootCue)
            BuildShootCue();
    }

    private void Update()
    {
        UpdateShootCue();

        if (isDead || laserTarget == null)
            return;

        if (!laserTarget.IsDead())
            return;

        isDead = true;
        DisableColliders();

        if (spawnPlantDebris)
            SpawnPlantDebris();

        if (cueRoot != null)
            cueRoot.gameObject.SetActive(false);

        Destroyed?.Invoke();
        Debug.Log($"<color=green>[DestructiblePlant] {name} destroyed - path opening.</color>");
    }

    private void EnsureLaserTarget()
    {
        laserTarget = GetComponent<LaserTarget>();
        if (laserTarget == null)
            laserTarget = gameObject.AddComponent<LaserTarget>();

        laserTarget.maxHP = maxHP;
        laserTarget.damagePerSecond = damagePerSecond;
        laserTarget.barOffsetY = barOffsetY;
        laserTarget.barWidth = barWidth;
    }

    private void EnsureCollider()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
                col = gameObject.AddComponent<MeshCollider>();
            else
                col = gameObject.AddComponent<BoxCollider>();
        }

        if (col != null)
            col.isTrigger = false;
    }

    private void DisableColliders()
    {
        if (colliders == null)
            return;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = false;
        }
    }

    private void SpawnPlantDebris()
    {
        Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
        Vector3 center = transform.position + Vector3.up * 1.5f;

        SpawnBurst(center, "PlantDebris", 40,
            new Color(0.2f, 0.5f, 0.1f), new Color(0.4f, 0.7f, 0.2f),
            6f, 0.4f, 1.2f, 1.5f, shader);

        SpawnBurst(center, "PlantDust", 25,
            new Color(0.35f, 0.25f, 0.1f), new Color(0.5f, 0.4f, 0.2f),
            3f, 0.8f, 1.8f, -0.1f, shader);
    }

    private static void SpawnBurst(
        Vector3 position,
        string label,
        int count,
        Color startColor,
        Color endColor,
        float speed,
        float size,
        float life,
        float gravity,
        Shader shader)
    {
        GameObject effectObject = new(label);
        effectObject.transform.position = position;

        ParticleSystem particleSystem = effectObject.AddComponent<ParticleSystem>();
        var main = particleSystem.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = life;
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
        main.maxParticles = count;
        main.startColor = new ParticleSystem.MinMaxGradient(startColor, endColor);
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = particleSystem.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });

        var shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.6f;

        var colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;

        Gradient gradient = new();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(startColor, 0f),
                new GradientColorKey(endColor, 0.5f),
                new GradientColorKey(new Color(endColor.r * 0.3f, endColor.g * 0.3f, endColor.b * 0.3f), 1f),
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.6f, 0.5f),
                new GradientAlphaKey(0f, 1f),
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        Material material = new(shader);
        material.color = startColor;
        renderer.sharedMaterial = material;

        particleSystem.Play();
        Destroy(effectObject, life + 0.5f);
    }

    private void BuildShootCue()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");

        cueRoot = new GameObject("ShootCue").transform;
        cueRoot.SetParent(transform, true);

        cueDiamond = CreateCueQuad("Diamond", shader, new Color(0.45f, 0.95f, 1f, 0.88f));
        cueDiamond.SetParent(cueRoot, false);
        cueDiamond.localRotation = Quaternion.Euler(0f, 0f, 45f);

        cueBeam = CreateCueQuad("Beam", shader, new Color(0.3f, 0.85f, 1f, 0.3f));
        cueBeam.SetParent(cueRoot, false);
        cueBeam.localPosition = new Vector3(0f, -0.85f, 0f);

        cueDiamondMaterial = cueDiamond.GetComponent<MeshRenderer>().sharedMaterial;
        cueBeamMaterial = cueBeam.GetComponent<MeshRenderer>().sharedMaterial;

        cueLight = cueRoot.gameObject.AddComponent<Light>();
        cueLight.type = LightType.Point;
        cueLight.color = new Color(0.38f, 0.94f, 1f, 1f);
        cueLight.range = shootCueLightRange;
        cueLight.intensity = 1.8f;
        cueLight.shadows = LightShadows.None;

        UpdateCueAnchor();
        ApplyCuePulse(0f);
    }

    private Transform CreateCueQuad(string label, Shader shader, Color color)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = label;
        Collider quadCollider = quad.GetComponent<Collider>();
        if (quadCollider != null)
            Destroy(quadCollider);

        MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
        Material material = new(shader);
        material.color = color;
        renderer.sharedMaterial = material;

        return quad.transform;
    }

    private void UpdateShootCue()
    {
        if (cueRoot == null)
            return;

        if (isDead)
        {
            cueRoot.gameObject.SetActive(false);
            return;
        }

        cueCamera ??= ResolveCueCamera();
        UpdateCueAnchor();

        if (cueCamera != null)
        {
            Vector3 lookDirection = cueCamera.position - cueRoot.position;
            if (lookDirection.sqrMagnitude > 0.001f)
                cueRoot.rotation = Quaternion.LookRotation(lookDirection);
        }

        ApplyCuePulse(Time.time);
    }

    private void UpdateCueAnchor()
    {
        if (cueRoot == null)
            return;

        cueRoot.position = GetCueWorldPosition();
    }

    private Vector3 GetCueWorldPosition()
    {
        if (TryGetVisualBounds(out Bounds bounds))
            return new Vector3(bounds.center.x, bounds.max.y + shootCueHeight, bounds.center.z);

        return transform.position + Vector3.up * shootCueHeight;
    }

    private bool TryGetVisualBounds(out Bounds bounds)
    {
        bounds = default;
        bool found = false;

        if (renderers == null)
            return false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }

    private void ApplyCuePulse(float timeValue)
    {
        float pulse = 0.5f + 0.5f * Mathf.Sin((timeValue * 2.6f) + cuePulseOffset);
        float diamondScale = Mathf.Lerp(shootCueSize * 0.85f, shootCueSize * 1.12f, pulse);
        float beamHeight = Mathf.Lerp(1.5f, 2.4f, pulse);
        float alpha = Mathf.Lerp(0.65f, 0.95f, pulse);

        if (cueDiamond != null)
            cueDiamond.localScale = new Vector3(diamondScale, diamondScale, 1f);

        if (cueBeam != null)
            cueBeam.localScale = new Vector3(shootCueSize * 0.2f, beamHeight, 1f);

        if (cueDiamondMaterial != null)
        {
            Color color = cueDiamondMaterial.color;
            color.a = alpha;
            cueDiamondMaterial.color = color;
        }

        if (cueBeamMaterial != null)
        {
            Color color = cueBeamMaterial.color;
            color.a = alpha * 0.45f;
            cueBeamMaterial.color = color;
        }

        if (cueLight != null)
            cueLight.intensity = Mathf.Lerp(1.5f, 2.4f, pulse);
    }

    private static Transform ResolveCueCamera()
    {
        if (Camera.main != null)
            return Camera.main.transform;

        Camera anyCamera = FindFirstObjectByType<Camera>();
        return anyCamera != null ? anyCamera.transform : null;
    }
}
