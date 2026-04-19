using System.Collections;
using UnityEngine;

/// <summary>
/// Drop on a floating platform.
/// Shows a pulsing ring (LineRenderer) + point light beacon.
/// When the player enters the trigger: next beacon activates, player gets gradual HP regen.
/// </summary>
public class PlatformBeacon : MonoBehaviour
{
    [Header("Ring")]
    public Color  beaconColor   = new Color(0.2f, 0.95f, 1f, 1f);
    public float  ringRadius    = 3.5f;
    public float  ringWidth     = 0.09f;
    public int    ringSegments  = 64;
    public float  ringYOffset   = 0.3f;
    public float  pulseSpeed    = 2.2f;
    public float  ringMinAlpha  = 0.18f;
    public float  ringMaxAlpha  = 0.88f;

    [Header("Point Light")]
    public float  lightRange    = 14f;

    [Header("Trigger Zone")]
    public float  triggerRadius = 9f;

    [Header("Health Reward")]
    public float  healPerSecond = 6f;
    public float  healDuration  = 5f;

    // ── Internals ────────────────────────────────────────────────
    private LineRenderer ring;
    private Light        pointLight;
    private bool         isActive;
    private BeaconSequenceManager manager;

    // ── Lifecycle ────────────────────────────────────────────────
    private void Awake()
    {
        manager = FindFirstObjectByType<BeaconSequenceManager>();
        Debug.Log($"<color=cyan>[PlatformBeacon] Awake on {gameObject.name}, manager={(manager != null ? "found" : "NULL")}</color>");
        BuildRing();
        BuildLight();
        EnsureTrigger();
        SetActive(false);
    }

    private void Update()
    {
        if (!isActive) return;

        float pulse = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI) + 1f) * 0.5f;

        if (ring != null)
        {
            Color c = beaconColor;
            c.a = Mathf.Lerp(ringMinAlpha, ringMaxAlpha, pulse);
            ring.startColor = c;
            ring.endColor   = c;
        }

        if (pointLight != null)
            pointLight.intensity = Mathf.Lerp(0.9f, 2.4f, pulse);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;

        bool isPlayer =
            other.GetComponentInParent<AutoJetpackController>()         != null ||
            other.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>()  != null;

        if (!isPlayer) return;

        // Gradual HP regen
        if (healPerSecond > 0f && healDuration > 0f)
        {
            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            if (health == null) health = FindFirstObjectByType<PlayerHealth>();
            if (health != null)
                StartCoroutine(GradualHeal(health));
        }

        Debug.Log($"<color=cyan>[PlatformBeacon] Reached → {gameObject.name}</color>");
        manager?.OnBeaconReached(this);
    }

    // ── Public API ───────────────────────────────────────────────
    public void SetActive(bool active)
    {
        isActive = active;
        if (ring       != null) ring.enabled       = active;
        if (pointLight != null) pointLight.enabled = active;
    }

    // ── Heal coroutine ───────────────────────────────────────────
    private IEnumerator GradualHeal(PlayerHealth health)
    {
        float elapsed = 0f;
        while (elapsed < healDuration)
        {
            elapsed += Time.deltaTime;
            health.Heal(healPerSecond * Time.deltaTime);
            yield return null;
        }
    }

    // ── Builders ─────────────────────────────────────────────────
    private void BuildRing()
    {
        GameObject go = new GameObject("PBcn_Ring");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.up * ringYOffset;

        ring = go.AddComponent<LineRenderer>();
        ring.useWorldSpace   = false;
        ring.loop            = true;
        ring.positionCount   = ringSegments;
        ring.widthMultiplier = ringWidth;
        ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ring.receiveShadows  = false;
        ring.alignment       = LineAlignment.View;

        Shader sh = Shader.Find("Universal Render Pipeline/Unlit")
                 ?? Shader.Find("Sprites/Default");
        Material mat = new Material(sh);
        mat.color = beaconColor;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", beaconColor);
        ring.material = mat;

        for (int i = 0; i < ringSegments; i++)
        {
            float angle = i / (float)ringSegments * Mathf.PI * 2f;
            ring.SetPosition(i, new Vector3(
                Mathf.Cos(angle) * ringRadius, 0f,
                Mathf.Sin(angle) * ringRadius));
        }
    }

    private void BuildLight()
    {
        GameObject go = new GameObject("PBcn_Light");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.up;

        pointLight           = go.AddComponent<Light>();
        pointLight.type      = LightType.Point;
        pointLight.color     = beaconColor;
        pointLight.range     = lightRange;
        pointLight.intensity = 1.5f;
        pointLight.shadows   = LightShadows.None;
    }

    private void EnsureTrigger()
    {
        SphereCollider col = gameObject.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius    = triggerRadius;
        col.center    = Vector3.up * (triggerRadius * 0.5f);
    }
}
