using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoverMountPromptController : MonoBehaviour
{
    [Header("Prompt References")]
    [SerializeField] private Transform promptRoot;

    [Header("Fallback Copy")]
    [SerializeField] private string fallbackTitle = "Mount Rover";
    [SerializeField] private string fallbackSubtitle = "Grip to ride";
    [SerializeField] private bool faceCamera = true;

    [Header("Fallback Layout")]
    [SerializeField] private Vector2 panelSize = new(210f, 72f);
    [SerializeField] private float canvasScale = 0.0018f;
    [SerializeField] private float sideOffset = 0.52f;
    [SerializeField] private float verticalOffset = 0.52f;
    private Canvas worldCanvas;
    private CanvasGroup canvasGroup;
    private RectTransform panelRect;
    private Image backgroundImage;
    private Image glowImage;
    private Image accentImage;
    private Image pointerImage;
    private TextMeshProUGUI titleLabel;
    private TextMeshProUGUI subtitleLabel;
    private Vector3 baseLocalPosition;
    private bool initialized;
    private float currentAlpha;

    private void Awake()
    {
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        EnsureRuntimePrompt();
        baseLocalPosition = promptRoot.localPosition;
        initialized = true;
    }

    public void Present(RoverUIBinder binder)
    {
        if (!initialized)
            Awake();

        RoverTheme theme = binder.Theme;
        bool showPrompt = !binder.IsMounted &&
            (binder.CurrentState == RoverUIState.PlayerNearby || binder.CurrentState == RoverUIState.ReadyToMount);
        bool visible = showPrompt;
        float targetAlpha = visible ? 1f : 0f;
        float fadeSpeed = theme != null ? theme.promptFadeSpeed : 6f;

        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
        ApplyTheme(theme, currentAlpha, false);
        UpdateCopy(theme);

        if (promptRoot != null)
        {
            Transform anchor = binder.PromptAnchor != null ? binder.PromptAnchor : binder.SeatAnchor;
            Vector3 offset = theme != null ? theme.promptOffset : new Vector3(0f, 0.2f, 0f);
            float bobAmplitude = visible && theme != null ? theme.bobAmplitude * 0.2f : 0f;
            float bobFrequency = theme != null ? theme.bobFrequency : 1f;
            float yOffset = visible ? Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude : 0f;

            if (anchor != null)
            {
                Vector3 towardPlayer = Vector3.zero;
                if (binder.PlayerCamera != null)
                {
                    towardPlayer = binder.PlayerCamera.transform.position - anchor.position;
                    towardPlayer.y = 0f;
                    if (towardPlayer.sqrMagnitude > 0.0001f)
                        towardPlayer = towardPlayer.normalized * sideOffset;
                }

                promptRoot.position = anchor.position + offset + towardPlayer + Vector3.up * (verticalOffset + yOffset);
            }
            else
            {
                promptRoot.localPosition = baseLocalPosition + offset + Vector3.up * (verticalOffset + yOffset);
            }
        }

        if (faceCamera && binder.PlayerCamera != null && promptRoot != null && visible)
        {
            Vector3 toCamera = binder.PlayerCamera.transform.position - promptRoot.position;
            if (toCamera.sqrMagnitude > 0.0001f)
                promptRoot.rotation = Quaternion.LookRotation(-toCamera.normalized, binder.PlayerCamera.transform.up);
        }

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;
    }

    private void EnsureRuntimePrompt()
    {
        if (promptRoot == null || promptRoot.parent != transform)
        {
            Transform existingPrompt = transform.Find("PromptRoot");
            if (existingPrompt != null)
            {
                promptRoot = existingPrompt;
            }
            else
            {
                GameObject root = new("PromptRoot");
                root.transform.SetParent(transform, false);
                root.transform.localPosition = new Vector3(0f, 0.2f, 0f);
                promptRoot = root.transform;
            }
        }

        if (worldCanvas != null)
            return;

        GameObject canvasObject = new("PromptCanvas");
        canvasObject.transform.SetParent(promptRoot, false);

        worldCanvas = canvasObject.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.overrideSorting = true;
        worldCanvas.sortingOrder = 40;
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

    private void UpdateCopy(RoverTheme theme)
    {
        if (titleLabel != null)
            titleLabel.text = theme != null ? theme.mountTitle : fallbackTitle;

        if (subtitleLabel != null)
            subtitleLabel.text = theme != null ? theme.mountSubtitle : fallbackSubtitle;
    }

    private void ApplyTheme(RoverTheme theme, float alpha, bool showConfirmation)
    {
        Color primary = showConfirmation
            ? theme != null ? theme.mountConfirmAccent : new Color(0.72f, 0.75f, 0.79f, 1f)
            : theme != null ? theme.primaryGlow : new Color(0.43f, 0.92f, 0.95f, 1f);
        Color panel = showConfirmation
            ? theme != null ? theme.mountConfirmPanelTint : new Color(0.16f, 0.18f, 0.2f, 0.9f)
            : theme != null ? theme.panelTint : new Color(0.02f, 0.08f, 0.12f, 0.92f);
        Color text = showConfirmation
            ? theme != null ? theme.mountConfirmTextColor : new Color(0.93f, 0.94f, 0.96f, 1f)
            : theme != null ? theme.textColor : new Color(0.95f, 0.99f, 1f, 1f);
        Color muted = showConfirmation
            ? theme != null ? theme.mountConfirmMutedTextColor : new Color(0.72f, 0.75f, 0.79f, 1f)
            : theme != null ? theme.mutedTextColor : new Color(0.72f, 0.9f, 0.96f, 1f);

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
            c.a = (showConfirmation ? 0.08f : 0.14f) * alpha;
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
            c.a = (showConfirmation ? 0.18f : 0.85f) * alpha;
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
}
