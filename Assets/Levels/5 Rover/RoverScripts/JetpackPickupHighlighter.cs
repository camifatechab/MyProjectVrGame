using UnityEngine;

/// <summary>
/// Lightweight pulsing ring to signal that the world jetpack can be grabbed.
/// </summary>
public class JetpackPickupHighlighter : MonoBehaviour
{
    [Header("Ring")]
    public float radius = 0.85f;
    public float width = 0.04f;
    public float yOffset = 0.05f;
    public int segments = 48;
    public Color ringColor = new Color(0.2f, 0.95f, 1f, 0.85f);
    public float pulseSpeed = 2.5f;
    public float pulseMinAlpha = 0.18f;
    public float pulseMaxAlpha = 0.85f;
    public float lightHeight = 0.45f;
    public float lightRange = 2.8f;
    public float lightIntensityMin = 0.8f;
    public float lightIntensityMax = 2.1f;

    private LineRenderer ring;
    private Light feedbackLight;
    private bool isHighlighted;

    private void Awake()
    {
        EnsureRing();
        SetHighlighted(false);
    }

    private void Update()
    {
        if (!isHighlighted || ring == null)
            return;

        float pulse = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI) + 1f) * 0.5f;
        Color color = ringColor;
        color.a = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, pulse);
        ring.startColor = color;
        ring.endColor = color;

        if (feedbackLight != null)
        {
            feedbackLight.intensity = Mathf.Lerp(lightIntensityMin, lightIntensityMax, pulse);
            feedbackLight.color = ringColor;
        }
    }

    public void SetHighlighted(bool highlighted)
    {
        isHighlighted = highlighted;
        if (ring != null)
            ring.gameObject.SetActive(highlighted);

        if (feedbackLight != null)
            feedbackLight.enabled = highlighted;
    }

    private void EnsureRing()
    {
        Transform ringTransform = transform.Find("JetpackHighlightRing");
        if (ringTransform == null)
        {
            GameObject ringObject = new GameObject("JetpackHighlightRing");
            ringTransform = ringObject.transform;
            ringTransform.SetParent(transform, false);
            ringTransform.localPosition = Vector3.up * yOffset;
        }

        ring = ringTransform.GetComponent<LineRenderer>();
        if (ring == null)
            ring = ringTransform.gameObject.AddComponent<LineRenderer>();

        ring.useWorldSpace = false;
        ring.loop = true;
        ring.positionCount = Mathf.Max(segments, 12);
        ring.widthMultiplier = width;
        ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ring.receiveShadows = false;
        ring.alignment = LineAlignment.View;

        if (ring.material == null)
        {
            Material material = new Material(Shader.Find("Sprites/Default"));
            material.color = ringColor;
            ring.material = material;
        }

        for (int i = 0; i < ring.positionCount; i++)
        {
            float t = i / (float)ring.positionCount;
            float angle = t * Mathf.PI * 2f;
            ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }

        EnsureFeedbackLight();
    }

    private void EnsureFeedbackLight()
    {
        Transform lightTransform = transform.Find("JetpackHighlightLight");
        if (lightTransform == null)
        {
            GameObject lightObject = new GameObject("JetpackHighlightLight");
            lightTransform = lightObject.transform;
            lightTransform.SetParent(transform, false);
            lightTransform.localPosition = Vector3.up * lightHeight;
        }

        feedbackLight = lightTransform.GetComponent<Light>();
        if (feedbackLight == null)
            feedbackLight = lightTransform.gameObject.AddComponent<Light>();

        feedbackLight.type = LightType.Point;
        feedbackLight.range = lightRange;
        feedbackLight.shadows = LightShadows.None;
        feedbackLight.color = ringColor;
        feedbackLight.intensity = lightIntensityMin;
        feedbackLight.enabled = false;
    }
}
