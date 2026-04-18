using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Rover-side exit prompt for the CP06 transition, using the same visual language
/// as the existing rover mount prompt.
/// </summary>
public class CP06RoverExitPromptController : MonoBehaviour
{
    [Header("Prompt References")]
    [SerializeField] private Transform promptRoot;
    [SerializeField] private RoverUIBinder binder;
    [SerializeField] private CP06JetpackSequence sequence;

    [Header("Copy")]
    [SerializeField] private string title = "Exit Rover";
    [SerializeField] private string subtitle = "Hold both grips to get out";

    [Header("Timing")]
    [SerializeField] private float showDelayAfterCP06 = 1.5f;
    [SerializeField] private bool faceCamera = true;

    [Header("Layout")]
    [SerializeField] private Vector2 panelSize = new(210f, 72f);
    [SerializeField] private float canvasScale = 0.0018f;
    [Tooltip("Shift along the rover's LOCAL +X (right of driver). Negative = left of driver.")]
    [SerializeField] private float lateralOffset = -0.38f;
    [Tooltip("Shift along the rover's LOCAL +Z (forward). Keeps prompt slightly ahead of dashboard.")]
    [SerializeField] private float forwardOffset = 0.05f;
    [Tooltip("Shift along world up. Negative = below the dashboard anchor (toward eye line).")]
    [SerializeField] private float verticalOffset = -0.08f;
    [Tooltip("Tilt the panel toward the driver by this angle on the rover's local Y axis.")]
    [SerializeField] private float inwardYawDegrees = 22f;

    private Canvas worldCanvas;
    private CanvasGroup canvasGroup;
    private RectTransform panelRect;
    private Image backgroundImage;
    private Image glowImage;
    private Image accentImage;
    private Image pointerImage;
    private TextMeshProUGUI titleLabel;
    private TextMeshProUGUI subtitleLabel;
    private float currentAlpha;
    private bool initialized;
    private Vector3 baseLocalPosition;

    private void Awake()
    {
        EnsureRuntimePrompt();
    }

    private void LateUpdate()
    {
        if (!initialized)
            EnsureRuntimePrompt();

        binder ??= GetComponentInParent<RoverUIBinder>();
        sequence ??= FindFirstObjectByType<CP06JetpackSequence>();

        if (binder == null || sequence == null)
            return;

        RoverTheme theme = binder.Theme;
        bool visible = ShouldShowPrompt();
        float fadeSpeed = theme != null ? theme.promptFadeSpeed : 6f;
        currentAlpha = Mathf.MoveTowards(currentAlpha, visible ? 1f : 0f, fadeSpeed * Time.deltaTime);

        UpdateCopy();
        ApplyTheme(theme, currentAlpha);
        UpdatePlacement(visible);

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;
    }

    private bool ShouldShowPrompt()
    {
        if (!sequence.SequenceUnlocked || sequence.PickupComplete)
            return false;

        if (sequence.SequenceUnlockedAt < 0f || Time.time < sequence.SequenceUnlockedAt + showDelayAfterCP06)
            return false;

        return binder != null && binder.IsMounted;
    }

    private void EnsureRuntimePrompt()
    {
        if (promptRoot == null || promptRoot.parent != transform)
        {
            Transform existingPrompt = transform.Find("CP06ExitPromptRoot");
            if (existingPrompt != null)
            {
                promptRoot = existingPrompt;
            }
            else
            {
                GameObject root = new("CP06ExitPromptRoot");
                root.transform.SetParent(transform, false);
                root.transform.localPosition = new Vector3(0f, 0.2f, 0f);
                promptRoot = root.transform;
            }
        }

        if (worldCanvas != null)
        {
            initialized = true;
            return;
        }

        GameObject canvasObject = new("CP06ExitPromptCanvas");
        canvasObject.transform.SetParent(promptRoot, false);

        worldCanvas = canvasObject.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.overrideSorting = true;
        worldCanvas.sortingOrder = 41;
        worldCanvas.pixelPerfect = false;

        canvasGroup = canvasObject.AddComponent<CanvasGroup>();

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = panelSize;
        canvasRect.localScale = Vector3.one * canvasScale;
        canvasRect.localRotation = Quaternion.identity;

        panelRect = CreateRect("Panel", canvasRect, panelSize, Vector2.zero);
        glowImage = CreateImage("Glow", panelRect, panelSize + new Vector2(18f, 18f), new Color(0.43f, 0.92f, 0.95f, 0.16f));
        backgroundImage = CreateImage("Background", panelRect, panelSize, new Color(0.02f, 0.08f, 0.12f, 0.88f));
        accentImage = CreateImage("Accent", panelRect, new Vector2(132f, 4f), new Color(0.75f, 0.95f, 1f, 1f));
        accentImage.rectTransform.anchoredPosition = new Vector2(0f, 18f);

        pointerImage = CreateImage("Pointer", panelRect, new Vector2(12f, 12f), new Color(0.75f, 0.95f, 1f, 1f));
        pointerImage.rectTransform.anchoredPosition = new Vector2(0f, -40f);
        pointerImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        titleLabel = CreateText("Title", panelRect, new Vector2(0f, 6f), new Vector2(180f, 22f), 12f, FontStyles.Bold);
        subtitleLabel = CreateText("Subtitle", panelRect, new Vector2(0f, -12f), new Vector2(180f, 16f), 6.5f, FontStyles.Normal);

        baseLocalPosition = promptRoot.localPosition;
        initialized = true;
    }

    private void UpdateCopy()
    {
        if (titleLabel != null)
            titleLabel.text = title;

        if (subtitleLabel != null)
            subtitleLabel.text = subtitle;
    }

    private void UpdatePlacement(bool visible)
    {
        if (promptRoot == null || binder == null)
            return;

        // Prefer the dashboard anchor — it's a known-good glance position the driver is
        // already looking at. Fall back to prompt/seat anchor only if no dashboard exists.
        Transform anchor = binder.DashboardAnchor != null ? binder.DashboardAnchor
                         : binder.PromptAnchor != null ? binder.PromptAnchor
                         : binder.SeatAnchor;

        float bobAmplitude = visible && binder.Theme != null ? binder.Theme.bobAmplitude * 0.2f : 0f;
        float bobFrequency = binder.Theme != null ? binder.Theme.bobFrequency : 1f;
        float bobY = visible ? Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude : 0f;

        if (anchor != null)
        {
            // Build offset in the ROVER's local frame so the prompt always sits in the same
            // cockpit-relative spot regardless of where the rover is driving.
            Vector3 localOffset = new(lateralOffset, verticalOffset + bobY, forwardOffset);
            promptRoot.position = anchor.TransformPoint(localOffset);
        }
        else
        {
            promptRoot.localPosition = baseLocalPosition + new Vector3(lateralOffset, verticalOffset + bobY, forwardOffset);
        }

        if (faceCamera && binder.PlayerCamera != null && visible)
        {
            Vector3 toCamera = binder.PlayerCamera.transform.position - promptRoot.position;
            if (toCamera.sqrMagnitude > 0.0001f)
            {
                Quaternion lookAtDriver = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
                // Tilt the panel slightly inward toward the driver so it reads cleanly
                // from the seat rather than being perfectly perpendicular.
                Quaternion inwardTilt = Quaternion.AngleAxis(inwardYawDegrees * Mathf.Sign(lateralOffset), Vector3.up);
                promptRoot.rotation = lookAtDriver * inwardTilt;
            }
        }
        else if (anchor != null)
        {
            // When not facing camera, still inherit the rover's yaw so it sits flush with the dashboard.
            promptRoot.rotation = anchor.rotation * Quaternion.Euler(0f, inwardYawDegrees * Mathf.Sign(lateralOffset), 0f);
        }
    }

    private void ApplyTheme(RoverTheme theme, float alpha)
    {
        Color primary = theme != null ? theme.primaryGlow : new Color(0.43f, 0.92f, 0.95f, 1f);
        Color panel = theme != null ? theme.panelTint : new Color(0.02f, 0.08f, 0.12f, 0.92f);
        Color text = theme != null ? theme.textColor : new Color(0.95f, 0.99f, 1f, 1f);
        Color muted = theme != null ? theme.mutedTextColor : new Color(0.72f, 0.9f, 0.96f, 1f);

        if (canvasGroup != null)
            canvasGroup.alpha = alpha;

        if (backgroundImage != null)
        {
            Color c = panel;
            c.a *= alpha;
            backgroundImage.color = c;
        }

        if (glowImage != null)
        {
            Color c = primary;
            c.a = 0.14f * alpha;
            glowImage.color = c;
        }

        if (accentImage != null)
        {
            Color c = primary;
            c.a = 0.95f * alpha;
            accentImage.color = c;
        }

        if (pointerImage != null)
        {
            Color c = primary;
            c.a = 0.85f * alpha;
            pointerImage.color = c;
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
