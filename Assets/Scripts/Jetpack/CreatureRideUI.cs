using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

/// <summary>
/// Rover-themed prompt and cyan interaction feedback for mounting/dismounting the creature.
/// Keeps the creature ride prompts visually aligned with the Mount Rover UI.
/// </summary>
public class CreatureRideUI : MonoBehaviour
{
    [Header("Legacy Scene References")]
    public Canvas rideCanvas;
    public Image backgroundPanel;
    public TextMeshProUGUI promptText;

    [Header("Prompt Copy")]
    public string mountTitle = "Mount Creature";
    public string nearCreaturePrompt = "Grip to ride";
    public string dismountTitle = "Exit Creature";
    public string finalDismountPrompt = "Press both grips to get out";

    [Header("Theme")]
    [SerializeField] private RoverTheme theme;
    [SerializeField] private bool faceCamera = true;

    [Header("Prompt Detection")]
    public float displayDistance = 8f;

    [Header("Prompt Layout")]
    [SerializeField] private Vector2 panelSize = new(210f, 72f);
    [SerializeField] private float canvasScale = 0.0018f;
    [SerializeField] private float sideOffset = 0.18f;
    [SerializeField] private float verticalOffset = 0.1f;

    [Header("VR Comfort")]
    [SerializeField] private float mountPromptDistance = 1.4f;
    [SerializeField] private float dismountPromptDistance = 1.2f;
    [SerializeField] private float mountEyeLineOffset = -0.08f;
    [SerializeField] private float dismountEyeLineOffset = -0.14f;
    [SerializeField] private float maxVerticalComfortShift = 0.08f;
    [SerializeField] private float promptMoveSpeed = 10f;

    [Header("Cyan Feedback")]
    [SerializeField] private float feedbackHeight = 0.42f;
    [SerializeField] private float feedbackRangeFar = 2.4f;
    [SerializeField] private float feedbackRangeNear = 3.6f;
    [SerializeField] private float feedbackUndersideOffset = 0.08f;

    private RideableCreature creature;
    private FloatingIslandIntroSequence introSequence;
    private Transform playerCamera;
    private CanvasGroup canvasGroup;
    private RectTransform panelRect;
    private Image runtimeBackgroundImage;
    private Image runtimeGlowImage;
    private Image runtimeAccentImage;
    private Image runtimePointerImage;
    private TextMeshProUGUI titleLabel;
    private TextMeshProUGUI subtitleLabel;
    private Light feedbackLight;
    private Transform feedbackVisualRoot;
    private Renderer[] feedbackRenderers;
    private Material feedbackMaterial;
    private MaterialPropertyBlock feedbackPropertyBlock;
    private Vector3 feedbackBaseScale = Vector3.one;
    private float currentAlpha;
    private bool initialized;
    private bool feedbackAnchorResolved;
    private Vector3 feedbackAnchorLocalPosition;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private enum PromptMode
    {
        Hidden,
        Mount,
        Dismount
    }

    private void Awake()
    {
        EnsureInitialized();
    }

    private void Start()
    {
        EnsureInitialized();
    }

    private void Update()
    {
        EnsureInitialized();

        if (creature == null)
            return;

        if (playerCamera == null)
            ResolvePlayerCamera();

        if (playerCamera == null)
            return;

        PromptMode promptMode = ResolvePromptMode();
        bool visible = promptMode != PromptMode.Hidden;
        float promptFadeSpeed = theme != null ? theme.promptFadeSpeed : 6f;

        currentAlpha = Mathf.MoveTowards(currentAlpha, visible ? 1f : 0f, promptFadeSpeed * Time.deltaTime);
        UpdatePromptCopy(promptMode);
        ApplyTheme(currentAlpha);
        UpdatePromptPlacement(visible, promptMode);
        UpdateFeedbackLight(visible, promptMode);

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        creature = FindFirstObjectByType<RideableCreature>();
        introSequence = FindFirstObjectByType<FloatingIslandIntroSequence>();

        if (theme == null)
            theme = Resources.Load<RoverTheme>("RoverKit/DefaultRoverTheme");

        ResolvePlayerCamera();
        EnsureRuntimePrompt();
        EnsureFeedbackVisuals();
        EnsureFeedbackLight();
        DisableLegacyPromptGraphics();
        ApplyTheme(0f);

        initialized = true;
    }

    private PromptMode ResolvePromptMode()
    {
        if (ShouldShowFinalDismountPrompt())
            return PromptMode.Dismount;

        if (creature != null && !creature.IsPlayerMounted && creature.canPlayerMount && playerCamera != null)
        {
            float distance = Vector3.Distance(playerCamera.position, creature.transform.position);
            if (distance <= displayDistance)
                return PromptMode.Mount;
        }

        return PromptMode.Hidden;
    }

    private bool ShouldShowFinalDismountPrompt()
    {
        if (creature == null || !creature.IsPlayerMounted || creature.IsFlying || creature.IsParking)
            return false;

        if (introSequence != null && introSequence.IsWaitingForFinalDismount)
            return true;

        return creature.TotalWaypoints > 0 && creature.CurrentWaypointIndex >= creature.TotalWaypoints - 1;
    }

    private void ResolvePlayerCamera()
    {
        var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin != null && xrOrigin.Camera != null)
        {
            playerCamera = xrOrigin.Camera.transform;
            return;
        }

        Camera foundCamera = Camera.main;
        if (foundCamera == null && xrOrigin != null)
            foundCamera = xrOrigin.GetComponentInChildren<Camera>(true);

        if (foundCamera != null)
            playerCamera = foundCamera.transform;
    }

    private void DisableLegacyPromptGraphics()
    {
        if (backgroundPanel != null)
            backgroundPanel.gameObject.SetActive(false);

        if (promptText != null && (backgroundPanel == null || promptText.gameObject != backgroundPanel.gameObject))
            promptText.gameObject.SetActive(false);
    }

    private void EnsureRuntimePrompt()
    {
        if (rideCanvas == null)
            rideCanvas = GetComponent<Canvas>();

        if (rideCanvas == null)
            rideCanvas = gameObject.AddComponent<Canvas>();

        rideCanvas.renderMode = RenderMode.WorldSpace;
        rideCanvas.overrideSorting = true;
        rideCanvas.sortingOrder = 40;
        rideCanvas.pixelPerfect = false;

        RectTransform canvasRect = rideCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = panelSize;
        canvasRect.localScale = Vector3.one * canvasScale;
        canvasRect.localRotation = Quaternion.identity;

        canvasGroup = rideCanvas.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = rideCanvas.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;

        if (panelRect != null)
            return;

        panelRect = CreateRect("PromptPanel", canvasRect, panelSize, Vector2.zero);
        runtimeGlowImage = CreateImage("Glow", panelRect, panelSize + new Vector2(18f, 18f), new Color(0.43f, 0.92f, 0.95f, 0.16f));
        runtimeBackgroundImage = CreateImage("Background", panelRect, panelSize, new Color(0.02f, 0.08f, 0.12f, 0.88f));
        runtimeAccentImage = CreateImage("Accent", panelRect, new Vector2(132f, 4f), new Color(0.75f, 0.95f, 1f, 1f));
        runtimeAccentImage.rectTransform.anchoredPosition = new Vector2(0f, 18f);

        runtimePointerImage = CreateImage("Pointer", panelRect, new Vector2(12f, 12f), new Color(0.75f, 0.95f, 1f, 1f));
        runtimePointerImage.rectTransform.anchoredPosition = new Vector2(0f, -40f);
        runtimePointerImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        titleLabel = CreateText("Title", panelRect, new Vector2(0f, 6f), new Vector2(180f, 22f), 12f, FontStyles.Bold);
        subtitleLabel = CreateText("Subtitle", panelRect, new Vector2(0f, -12f), new Vector2(180f, 16f), 6.5f, FontStyles.Normal);
    }

    private void EnsureFeedbackLight()
    {
        if (creature == null || feedbackLight != null)
            return;

        Transform existingLight = creature.transform.Find("CreatureInteractionLight");
        if (existingLight != null)
        {
            feedbackLight = existingLight.GetComponent<Light>();
            return;
        }

        GameObject lightObject = new("CreatureInteractionLight");
        lightObject.transform.SetParent(creature.transform, false);
        lightObject.transform.localPosition = ResolveFeedbackAnchorLocalPosition() + Vector3.up * feedbackHeight;

        feedbackLight = lightObject.AddComponent<Light>();
        feedbackLight.type = LightType.Point;
        feedbackLight.range = feedbackRangeFar;
        feedbackLight.shadows = LightShadows.None;
        feedbackLight.intensity = 0f;
        feedbackLight.enabled = false;
    }

    private void EnsureFeedbackVisuals()
    {
        if (creature == null || feedbackVisualRoot != null)
            return;

        feedbackPropertyBlock ??= new MaterialPropertyBlock();

        Transform existingRoot = creature.transform.Find("CreatureInteractionVisualRoot");
        if (existingRoot != null)
        {
            feedbackVisualRoot = existingRoot;
            feedbackRenderers = feedbackVisualRoot.GetComponentsInChildren<Renderer>(true);
            feedbackBaseScale = feedbackVisualRoot.localScale;
            return;
        }

        if (feedbackMaterial == null)
            feedbackMaterial = CreateFeedbackMaterial();

        GameObject root = new("CreatureInteractionVisualRoot");
        root.transform.SetParent(creature.transform, false);
        root.transform.localPosition = ResolveFeedbackAnchorLocalPosition();
        root.transform.localRotation = Quaternion.identity;
        feedbackVisualRoot = root.transform;

        Renderer outerGlow = CreateFeedbackOrb(
            "OuterGlow",
            feedbackVisualRoot,
            Vector3.zero,
            new Vector3(0.7f, 0.16f, 0.7f));
        Renderer innerGlow = CreateFeedbackOrb(
            "InnerGlow",
            feedbackVisualRoot,
            new Vector3(0f, 0.04f, 0f),
            new Vector3(0.36f, 0.1f, 0.36f));

        feedbackRenderers = new[] { outerGlow, innerGlow };
        feedbackBaseScale = feedbackVisualRoot.localScale;
        feedbackVisualRoot.gameObject.SetActive(false);
    }

    private void UpdatePromptCopy(PromptMode promptMode)
    {
        if (titleLabel == null || subtitleLabel == null)
            return;

        if (promptMode == PromptMode.Dismount)
        {
            titleLabel.text = dismountTitle;
            subtitleLabel.text = finalDismountPrompt;
            return;
        }

        titleLabel.text = mountTitle;
        subtitleLabel.text = nearCreaturePrompt;
    }

    private void ApplyTheme(float alpha)
    {
        Color primary = theme != null ? theme.primaryGlow : new Color(0.43f, 0.92f, 0.95f, 1f);
        Color panel = theme != null ? theme.panelTint : new Color(0.02f, 0.08f, 0.12f, 0.92f);
        Color text = theme != null ? theme.textColor : new Color(0.95f, 0.99f, 1f, 1f);
        Color muted = theme != null ? theme.mutedTextColor : new Color(0.72f, 0.9f, 0.96f, 1f);

        if (canvasGroup != null)
            canvasGroup.alpha = alpha;

        if (runtimeBackgroundImage != null)
        {
            Color c = panel;
            c.a *= alpha;
            runtimeBackgroundImage.color = c;
        }

        if (runtimeGlowImage != null)
        {
            Color c = primary;
            c.a = 0.14f * alpha;
            runtimeGlowImage.color = c;
        }

        if (runtimeAccentImage != null)
        {
            Color c = primary;
            c.a = 0.95f * alpha;
            runtimeAccentImage.color = c;
        }

        if (runtimePointerImage != null)
        {
            Color c = primary;
            c.a = 0.85f * alpha;
            runtimePointerImage.color = c;
        }

        if (titleLabel != null)
        {
            Color c = text;
            c.a *= alpha;
            titleLabel.color = c;
        }

        if (subtitleLabel != null)
        {
            Color c = muted;
            c.a *= alpha;
            subtitleLabel.color = c;
        }

        if (panelRect != null)
            panelRect.gameObject.SetActive(alpha > 0.001f);
    }

    private void UpdatePromptPlacement(bool visible, PromptMode promptMode)
    {
        if (rideCanvas == null || playerCamera == null || creature == null)
            return;

        float bobAmplitude = visible && theme != null ? theme.bobAmplitude * 0.2f : 0f;
        float bobFrequency = theme != null ? theme.bobFrequency : 1f;
        float yOffset = visible ? Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude : 0f;
        Vector3 forward = Vector3.ProjectOnPlane(playerCamera.forward, Vector3.up);
        if (forward.sqrMagnitude <= 0.0001f)
            forward = playerCamera.forward;
        if (forward.sqrMagnitude <= 0.0001f)
            forward = Vector3.forward;
        forward.Normalize();

        Vector3 right = Vector3.Cross(playerCamera.up, forward).normalized;
        if (right.sqrMagnitude <= 0.0001f)
            right = playerCamera.right.normalized;

        float comfortDistance = promptMode == PromptMode.Dismount ? dismountPromptDistance : mountPromptDistance;
        float eyeLineOffset = promptMode == PromptMode.Dismount ? dismountEyeLineOffset : mountEyeLineOffset;
        Vector3 targetPosition = playerCamera.position
            + forward * comfortDistance
            + playerCamera.up * (eyeLineOffset + yOffset);

        if (promptMode == PromptMode.Mount)
        {
            Vector3 toAnchor = GetPromptAnchorPosition() - playerCamera.position;
            if (toAnchor.sqrMagnitude > 0.0001f)
            {
                Vector3 toAnchorDir = toAnchor.normalized;
                float horizontalShift = Mathf.Clamp(Vector3.Dot(toAnchorDir, right) * sideOffset * 2f, -sideOffset, sideOffset);
                float verticalShift = Mathf.Clamp(Vector3.Dot(toAnchorDir, playerCamera.up) * verticalOffset, -maxVerticalComfortShift, maxVerticalComfortShift);
                targetPosition += right * horizontalShift + playerCamera.up * verticalShift;
            }
        }

        float followLerp = 1f - Mathf.Exp(-promptMoveSpeed * Time.deltaTime);
        rideCanvas.transform.position = Vector3.Lerp(rideCanvas.transform.position, targetPosition, followLerp);

        if (faceCamera)
        {
            Vector3 toCamera = playerCamera.position - rideCanvas.transform.position;
            if (toCamera.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(-toCamera.normalized, playerCamera.up);
                rideCanvas.transform.rotation = Quaternion.Slerp(rideCanvas.transform.rotation, targetRotation, followLerp);
            }
        }
    }

    private void UpdateFeedbackLight(bool visible, PromptMode promptMode)
    {
        if (feedbackLight == null || creature == null)
            return;

        if (!visible)
        {
            feedbackLight.enabled = false;
            feedbackLight.intensity = 0f;
            UpdateFeedbackVisuals(false, 0f, 0f);
            return;
        }

        float proximity = 1f;
        if (promptMode == PromptMode.Mount && playerCamera != null)
        {
            float nearDistance = Mathf.Max(2f, displayDistance * 0.5f);
            float farDistance = Mathf.Max(displayDistance, nearDistance + 0.1f);
            float playerDistance = Vector3.Distance(playerCamera.position, creature.transform.position);
            proximity = 1f - Mathf.InverseLerp(nearDistance, farDistance, playerDistance);
        }

        float pulseSpeed = Mathf.Lerp(
            theme != null ? theme.beaconPulseFar : 1.1f,
            theme != null ? theme.beaconPulseNear : 2.6f,
            proximity);
        float pulse = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;

        feedbackLight.enabled = true;
        feedbackLight.color = theme != null ? theme.primaryGlow : new Color(0.43f, 0.92f, 0.95f, 1f);
        feedbackLight.intensity = Mathf.Lerp(
            theme != null ? theme.beaconLightFar : 1.2f,
            theme != null ? theme.beaconLightNear : 3.8f,
            Mathf.Lerp(proximity, 1f, pulse * 0.35f));
        feedbackLight.range = Mathf.Lerp(feedbackRangeFar, feedbackRangeNear, proximity);

        Vector3 localAnchor = ResolveFeedbackAnchorLocalPosition() + Vector3.up * feedbackHeight;
        float bobAmplitude = visible && theme != null ? theme.bobAmplitude : 0f;
        float bobFrequency = theme != null ? theme.bobFrequency : 1f;
        float yOffset = Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
        feedbackLight.transform.localPosition = localAnchor + Vector3.up * yOffset;

        UpdateFeedbackVisuals(true, proximity, pulse);
    }

    private Vector3 GetPromptAnchorPosition()
    {
        Vector3 anchor = creature.transform.TransformPoint(creature.seatOffset);
        anchor += Vector3.up * 0.12f;
        return anchor;
    }

    private Vector3 ResolveFeedbackAnchorLocalPosition()
    {
        if (feedbackAnchorResolved)
            return feedbackAnchorLocalPosition;

        Renderer[] renderers = creature != null ? creature.GetComponentsInChildren<Renderer>(true) : null;
        bool hasBounds = false;
        Bounds bounds = default;

        if (renderers != null)
        {
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
        }

        if (hasBounds)
        {
            Vector3 worldAnchor = new Vector3(bounds.center.x, bounds.min.y - feedbackUndersideOffset, bounds.center.z);
            feedbackAnchorLocalPosition = creature.transform.InverseTransformPoint(worldAnchor);
        }
        else
        {
            feedbackAnchorLocalPosition = creature.seatOffset + Vector3.down * 0.5f;
        }

        feedbackAnchorResolved = true;
        return feedbackAnchorLocalPosition;
    }

    private void UpdateFeedbackVisuals(bool visible, float proximity, float pulse)
    {
        if (feedbackVisualRoot == null || feedbackRenderers == null || feedbackRenderers.Length == 0)
            return;

        if (!visible)
        {
            feedbackVisualRoot.gameObject.SetActive(false);
            return;
        }

        feedbackVisualRoot.gameObject.SetActive(true);

        float glowStrength = Mathf.Lerp(
            theme != null ? theme.beaconGlowFar : 0.45f,
            theme != null ? theme.beaconGlowNear : 1.55f,
            Mathf.Lerp(proximity, 1f, pulse * 0.35f));
        float scaleMultiplier = Mathf.Lerp(
            theme != null ? theme.beaconScaleFar : 1f,
            theme != null ? theme.beaconScaleNear : 1.14f,
            pulse * Mathf.Max(0.2f, proximity));
        float bobAmplitude = visible && theme != null ? theme.bobAmplitude : 0f;
        float bobFrequency = theme != null ? theme.bobFrequency : 1f;
        float yOffset = Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;

        feedbackVisualRoot.localPosition = ResolveFeedbackAnchorLocalPosition() + Vector3.up * yOffset;
        feedbackVisualRoot.localScale = feedbackBaseScale * scaleMultiplier;
        ApplyFeedbackGlow(theme != null ? theme.primaryGlow : new Color(0.43f, 0.92f, 0.95f, 1f), glowStrength);
    }

    private void ApplyFeedbackGlow(Color glowColor, float glowStrength)
    {
        if (feedbackRenderers == null || feedbackPropertyBlock == null)
            return;

        Color baseColor = Color.Lerp(Color.black, glowColor, Mathf.Clamp01(glowStrength));
        baseColor.a = Mathf.Clamp01(0.1f + glowStrength * 0.2f);
        Color emissionColor = glowColor * Mathf.LinearToGammaSpace(Mathf.Max(0.4f, glowStrength * 1.1f));

        for (int i = 0; i < feedbackRenderers.Length; i++)
        {
            Renderer target = feedbackRenderers[i];
            if (target == null)
                continue;

            target.GetPropertyBlock(feedbackPropertyBlock);
            feedbackPropertyBlock.SetColor(BaseColorId, baseColor);
            feedbackPropertyBlock.SetColor(EmissionColorId, emissionColor);
            target.SetPropertyBlock(feedbackPropertyBlock);
        }
    }

    private Material CreateFeedbackMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material material = new(shader);
        material.enableInstancing = true;

        if (shader.name == "Universal Render Pipeline/Unlit")
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.SetFloat("_Cull", 0f);
            material.renderQueue = 3000;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        material.SetColor(BaseColorId, new Color(0.43f, 0.92f, 0.95f, 0.18f));
        return material;
    }

    private Renderer CreateFeedbackOrb(string name, Transform parent, Vector3 localPosition, Vector3 localScale)
    {
        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = name;
        orb.transform.SetParent(parent, false);
        orb.transform.localPosition = localPosition;
        orb.transform.localRotation = Quaternion.identity;
        orb.transform.localScale = localScale;

        Collider orbCollider = orb.GetComponent<Collider>();
        if (orbCollider != null)
            Destroy(orbCollider);

        Renderer renderer = orb.GetComponent<Renderer>();
        renderer.sharedMaterial = feedbackMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        return renderer;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 size, Vector2 anchoredPosition)
    {
        GameObject target = new(name);
        target.transform.SetParent(parent, false);

        RectTransform rect = target.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        rect.localScale = Vector3.one;
        return rect;
    }

    private static Image CreateImage(string name, Transform parent, Vector2 size, Color color)
    {
        RectTransform rect = CreateRect(name, parent, size, Vector2.zero);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, float fontSize, FontStyles fontStyle)
    {
        RectTransform rect = CreateRect(name, parent, size, anchoredPosition);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.richText = false;
        return label;
    }
}
