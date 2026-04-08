using UnityEngine;

public class RoverSeatBeaconController : MonoBehaviour
{
    [Header("Visual Targets")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Renderer[] emissiveRenderers;
    [SerializeField] private Light[] accentLights;

    [Header("Fallback Tuning")]
    [SerializeField] private Color fallbackGlow = new(0.43f, 0.92f, 0.95f, 1f);
    [SerializeField] private float defaultPulseFar = 1.1f;
    [SerializeField] private float defaultPulseNear = 2.1f;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private MaterialPropertyBlock propertyBlock;
    private Vector3 baseScale = Vector3.one;
    private Vector3 baseLocalPosition;
    private bool initialized;
    private Material runtimeMaterial;

    private void Awake()
    {
        propertyBlock ??= new MaterialPropertyBlock();
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        EnsureRuntimeVisuals();

        baseScale = visualRoot.localScale;
        baseLocalPosition = visualRoot.localPosition;
        initialized = true;
    }

    public void Present(RoverUIBinder binder)
    {
        if (!initialized)
            Awake();

        RoverTheme theme = binder.Theme;
        bool visible = binder.CurrentState == RoverUIState.PlayerNearby || binder.CurrentState == RoverUIState.ReadyToMount;
        float proximity = 0f;

        if (visible)
        {
            float nearDistance = Mathf.Max(binder.ReadyToMountDistance, 0.1f);
            float farDistance = Mathf.Max(binder.NearbyDistance, nearDistance + 0.1f);
            proximity = 1f - Mathf.InverseLerp(nearDistance, farDistance, binder.PlayerDistance);
        }

        float pulseSpeed = Mathf.Lerp(
            theme != null ? theme.beaconPulseFar : defaultPulseFar,
            theme != null ? theme.beaconPulseNear : defaultPulseNear,
            proximity);

        float pulse = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
        float glowStrength = visible
            ? Mathf.Lerp(
                theme != null ? theme.beaconGlowFar : 0.08f,
                theme != null ? theme.beaconGlowNear : 0.24f,
                pulse * proximity)
            : 0f;

        Color glowColor = theme != null ? theme.primaryGlow : fallbackGlow;
        ApplyGlow(glowColor, glowStrength);
        ApplyScaleAndBob(theme, visible, proximity, pulse);
        ApplyLights(theme, visible, proximity, pulse);
    }

    private void EnsureRuntimeVisuals()
    {
        if (visualRoot == null)
        {
            Transform existingRoot = transform.Find("BeaconVisualRoot");
            if (existingRoot != null)
            {
                visualRoot = existingRoot;
            }
            else
            {
                GameObject root = new("BeaconVisualRoot");
                root.transform.SetParent(transform, false);
                root.transform.localPosition = new Vector3(0f, 0.12f, 0f);
                visualRoot = root.transform;
            }
        }

        Transform existingHalo = visualRoot.Find("HaloGlow");
        if (existingHalo != null)
            existingHalo.gameObject.SetActive(false);

        if (emissiveRenderers != null && emissiveRenderers.Length > 0)
        {
            for (int i = 0; i < emissiveRenderers.Length; i++)
            {
                if (emissiveRenderers[i] != null)
                    emissiveRenderers[i].gameObject.SetActive(false);
            }
        }

        if ((emissiveRenderers == null || emissiveRenderers.Length == 0) && visualRoot.childCount == 0)
        {
            runtimeMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            runtimeMaterial.SetFloat("_Surface", 1f);
            runtimeMaterial.SetFloat("_Blend", 0f);
            runtimeMaterial.SetFloat("_AlphaClip", 0f);
            runtimeMaterial.renderQueue = 3000;
            runtimeMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            runtimeMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            runtimeMaterial.SetInt("_ZWrite", 0);
            runtimeMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            emissiveRenderers = new Renderer[0];
        }

        if ((accentLights == null || accentLights.Length == 0))
        {
            Light pointLight = GetComponentInChildren<Light>(true);
            if (pointLight == null)
            {
                GameObject lightObject = new("SeatBeaconLight");
                lightObject.transform.SetParent(visualRoot, false);
                lightObject.transform.localPosition = new Vector3(0f, 0.28f, 0f);
                pointLight = lightObject.AddComponent<Light>();
                pointLight.type = LightType.Point;
                pointLight.range = 3.4f;
                pointLight.shadows = LightShadows.None;
            }

            accentLights = new[] { pointLight };
        }

        Transform existingParticles = transform.Find("AmbientParticles");
        if (existingParticles != null)
        {
            existingParticles.gameObject.SetActive(false);
        }
    }

    private Renderer CreateOrb(string name, Vector3 localPosition, Vector3 localScale)
    {
        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = name;
        orb.transform.SetParent(visualRoot, false);
        orb.transform.localPosition = localPosition;
        orb.transform.localScale = localScale;

        Collider orbCollider = orb.GetComponent<Collider>();
        if (orbCollider != null)
            Destroy(orbCollider);

        Renderer renderer = orb.GetComponent<Renderer>();
        renderer.sharedMaterial = runtimeMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return renderer;
    }

    private void ApplyGlow(Color glowColor, float glowStrength)
    {
        if (emissiveRenderers == null || propertyBlock == null)
            return;

        for (int i = 0; i < emissiveRenderers.Length; i++)
        {
            Renderer target = emissiveRenderers[i];
            if (target == null)
                continue;

            target.GetPropertyBlock(propertyBlock);
            Color tintedColor = glowColor * Mathf.LinearToGammaSpace(Mathf.Max(0f, glowStrength));

            propertyBlock.SetColor(BaseColorId, Color.Lerp(Color.black, glowColor, glowStrength));
            propertyBlock.SetColor(EmissionColorId, tintedColor);
            target.SetPropertyBlock(propertyBlock);
        }
    }

    private void ApplyScaleAndBob(RoverTheme theme, bool visible, float proximity, float pulse)
    {
        if (visualRoot == null)
            return;

        float scaleMultiplier = visible
            ? Mathf.Lerp(
                theme != null ? theme.beaconScaleFar : 1f,
                theme != null ? theme.beaconScaleNear : 1.05f,
                pulse * proximity)
            : 1f;

        float bobAmplitude = visible && theme != null ? theme.bobAmplitude : 0f;
        float bobFrequency = theme != null ? theme.bobFrequency : 1f;
        float yOffset = visible ? Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude : 0f;

        visualRoot.localScale = baseScale * scaleMultiplier;
        visualRoot.localPosition = baseLocalPosition + Vector3.up * yOffset;
    }

    private void ApplyLights(RoverTheme theme, bool visible, float proximity, float pulse)
    {
        if (accentLights == null)
            return;

        float intensity = visible
            ? Mathf.Lerp(
                theme != null ? theme.beaconLightFar : 0.5f,
                theme != null ? theme.beaconLightNear : 2f,
                proximity)
            : 0f;

        Color lightColor = theme != null ? theme.primaryGlow : fallbackGlow;

        for (int i = 0; i < accentLights.Length; i++)
        {
            Light accent = accentLights[i];
            if (accent == null)
                continue;

            accent.enabled = visible;
            accent.color = lightColor;
            accent.intensity = intensity;
            accent.range = visible ? Mathf.Lerp(2.4f, 3.6f, proximity) : accent.range;
        }
    }

}
