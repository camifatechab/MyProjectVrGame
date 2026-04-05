using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoverDashboardController : MonoBehaviour
{
    [Header("Dashboard References")]
    [SerializeField] private Transform panelRoot;

    [Header("Fallback Layout")]
    [SerializeField] private Vector2 panelSize = new(220f, 104f);
    [SerializeField] private float canvasScale = 0.00145f;
    [SerializeField] private Vector3 fallbackDashboardOffset = Vector3.zero;

    private Canvas worldCanvas;
    private CanvasGroup canvasGroup;
    private Image backgroundImage;
    private Image glowImage;
    private Image accentImage;
    private Image healthBarBack;
    private Image healthBarFill;
    private TextMeshProUGUI healthLabel;
    private TextMeshProUGUI statusLabel;
    private TextMeshProUGUI speedValueLabel;
    private TextMeshProUGUI speedUnitLabel;
    private bool initialized;
    private float currentAlpha;
    private float currentHealthNormalized = 1f;
    private float confirmationTimer;
    private bool wasMounted;

    private enum DashboardState
    {
        Normal,
        Confirmation,
        ResetWarning,
        Repositioning
    }

    private void Awake()
    {
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        EnsureRuntimePanel();
        initialized = true;
    }

    public void Present(RoverUIBinder binder)
    {
        if (!initialized)
            Awake();

        RoverTheme theme = binder.Theme;
        bool visible = binder.IsMounted;
        bool justMounted = binder.IsMounted && !wasMounted;
        if (justMounted)
            confirmationTimer = theme != null ? theme.mountConfirmDuration : 1.8f;

        if (confirmationTimer > 0f)
            confirmationTimer = Mathf.Max(0f, confirmationTimer - Time.deltaTime);

        float fadeSpeed = theme != null ? theme.dashboardFadeSpeed : 5f;
        currentAlpha = Mathf.MoveTowards(currentAlpha, visible ? 1f : 0f, fadeSpeed * Time.deltaTime);
        DashboardState state = ResolveState(binder, confirmationTimer > 0f);

        UpdatePlacement(binder, theme);
        UpdateCopy(binder, theme, state);
        ApplyTheme(theme, currentAlpha, state);

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;

        wasMounted = binder.IsMounted;
    }

    private void EnsureRuntimePanel()
    {
        if (panelRoot == null || panelRoot.parent != transform)
        {
            Transform existingRoot = transform.Find("PanelRoot");
            if (existingRoot != null)
            {
                panelRoot = existingRoot;
            }
            else
            {
                GameObject root = new("PanelRoot");
                root.transform.SetParent(transform, false);
                root.transform.localPosition = Vector3.zero;
                panelRoot = root.transform;
            }
        }

        if (worldCanvas != null)
            return;

        GameObject canvasObject = new("DashboardCanvas");
        canvasObject.transform.SetParent(panelRoot, false);

        worldCanvas = canvasObject.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.overrideSorting = true;
        worldCanvas.sortingOrder = 35;
        worldCanvas.pixelPerfect = false;

        canvasGroup = canvasObject.AddComponent<CanvasGroup>();

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = panelSize;
        canvasRect.localScale = Vector3.one * canvasScale;
        canvasRect.localRotation = Quaternion.identity;

        RectTransform panelRect = CreateRect("Panel", canvasRect, panelSize, Vector2.zero);
        glowImage = CreateImage("Glow", panelRect, panelSize + new Vector2(16f, 16f), new Color(0.8f, 0.84f, 0.9f, 0.1f));
        backgroundImage = CreateImage("Background", panelRect, panelSize, new Color(0.06f, 0.09f, 0.11f, 0.9f));
        accentImage = CreateImage("Accent", panelRect, new Vector2(132f, 4f), new Color(0.76f, 0.8f, 0.85f, 1f));
        accentImage.rectTransform.anchoredPosition = new Vector2(0f, 34f);

        healthLabel = CreateText("HealthLabel", panelRect, new Vector2(-62f, -40f), new Vector2(24f, 12f), 6.5f, FontStyles.Normal);
        healthLabel.alignment = TextAlignmentOptions.MidlineLeft;
        healthLabel.text = "HP";

        healthBarBack = CreateImage("HealthBarBack", panelRect, new Vector2(104f, 4f), new Color(1f, 1f, 1f, 0.08f));
        healthBarBack.rectTransform.anchoredPosition = new Vector2(14f, -40f);

        healthBarFill = CreateImage("HealthBarFill", healthBarBack.rectTransform, new Vector2(104f, 4f), new Color(0.76f, 0.8f, 0.85f, 1f));
        healthBarFill.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        healthBarFill.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        healthBarFill.rectTransform.pivot = new Vector2(0f, 0.5f);
        healthBarFill.rectTransform.anchoredPosition = Vector2.zero;

        statusLabel = CreateText("Status", panelRect, new Vector2(0f, 18f), new Vector2(180f, 18f), 8f, FontStyles.Normal);
        speedValueLabel = CreateText("SpeedValue", panelRect, new Vector2(0f, -2f), new Vector2(180f, 34f), 22f, FontStyles.Bold);
        speedUnitLabel = CreateText("SpeedUnit", panelRect, new Vector2(0f, -22f), new Vector2(120f, 14f), 7f, FontStyles.Normal);
    }

    private void UpdatePlacement(RoverUIBinder binder, RoverTheme theme)
    {
        if (panelRoot == null)
            return;

        Transform anchor = binder.DashboardAnchor != null ? binder.DashboardAnchor : binder.SeatAnchor;
        Vector3 offset = theme != null && theme.dashboardOffset != Vector3.zero
            ? theme.dashboardOffset
            : fallbackDashboardOffset;

        if (anchor != null)
            panelRoot.position = anchor.position + offset;

        if (binder.PlayerCamera != null)
        {
            Vector3 toCamera = binder.PlayerCamera.transform.position - panelRoot.position;
            if (toCamera.sqrMagnitude > 0.0001f)
                panelRoot.rotation = Quaternion.LookRotation(-toCamera.normalized, binder.PlayerCamera.transform.up);
        }
    }

    private string GetStatusLabel(RoverTheme theme, DashboardState state, RoverUIBinder binder)
    {
        switch (state)
        {
            case DashboardState.Confirmation:
                return theme != null ? theme.mountConfirmTitle : "Rover Linked";
            case DashboardState.Repositioning:
                return theme != null ? theme.dashboardRepositioningLabel : "REPOSITIONING";
            case DashboardState.ResetWarning:
                return theme != null ? theme.dashboardResetWarningLabel : "RESET IMMINENT";
            default:
                return theme != null ? theme.dashboardStatusLabel : "DRIVE READY";
        }
    }

    private DashboardState ResolveState(RoverUIBinder binder, bool showConfirmation)
    {
        if (showConfirmation)
            return DashboardState.Confirmation;

        if (binder.IsRepositioning)
            return DashboardState.Repositioning;

        if (binder.IsResetWarningActive)
            return DashboardState.ResetWarning;

        return DashboardState.Normal;
    }

    private void UpdateCopy(RoverUIBinder binder, RoverTheme theme, DashboardState state)
    {
        float speedKmh = Mathf.Abs(binder.ForwardSpeed) * 3.6f;
        currentHealthNormalized = binder.HealthNormalized;

        if (statusLabel != null)
            statusLabel.text = GetStatusLabel(theme, state, binder);

        if (speedValueLabel != null)
            speedValueLabel.text = Mathf.RoundToInt(speedKmh).ToString("00");

        if (speedUnitLabel != null)
            speedUnitLabel.text = theme != null ? theme.dashboardSpeedUnit : "km/h";

        if (healthBarFill != null)
        {
            Vector2 size = healthBarFill.rectTransform.sizeDelta;
            size.x = Mathf.Lerp(12f, 104f, currentHealthNormalized);
            healthBarFill.rectTransform.sizeDelta = size;
        }
    }

    private void ApplyTheme(RoverTheme theme, float alpha, DashboardState state)
    {
        bool showConfirmation = state == DashboardState.Confirmation;
        bool showWarning = state == DashboardState.ResetWarning || state == DashboardState.Repositioning;

        Color panel = showConfirmation
            ? theme != null ? theme.mountConfirmPanelTint : new Color(0.16f, 0.18f, 0.2f, 0.9f)
            : theme != null ? theme.dashboardPanelTint : new Color(0.06f, 0.09f, 0.11f, 0.9f);
        Color accent = showConfirmation
            ? theme != null ? theme.mountConfirmAccent : new Color(0.7f, 0.73f, 0.77f, 1f)
            : showWarning
                ? theme != null ? theme.dashboardWarningAccent : new Color(1f, 0.76f, 0.29f, 1f)
                : theme != null ? theme.dashboardAccent : new Color(0.76f, 0.8f, 0.85f, 1f);
        Color text = showConfirmation
            ? theme != null ? theme.mountConfirmTextColor : new Color(0.93f, 0.94f, 0.96f, 1f)
            : showWarning
                ? theme != null ? theme.dashboardWarningTextColor : new Color(0.99f, 0.95f, 0.86f, 1f)
                : theme != null ? theme.dashboardTextColor : new Color(0.95f, 0.97f, 0.99f, 1f);
        Color muted = showConfirmation
            ? theme != null ? theme.mountConfirmMutedTextColor : new Color(0.72f, 0.75f, 0.79f, 1f)
            : showWarning
                ? theme != null ? theme.dashboardWarningTextColor : new Color(0.99f, 0.95f, 0.86f, 1f)
                : theme != null ? theme.dashboardMutedTextColor : new Color(0.63f, 0.68f, 0.73f, 1f);

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
            Color c = accent;
            c.a = 0.1f * alpha;
            glowImage.color = c;
        }

        if (accentImage != null)
        {
            Color c = accent;
            c.a = 0f;
            accentImage.color = c;
        }

        if (healthBarFill != null)
        {
            Color c = GetHealthColor(theme, alpha);
            healthBarFill.color = c;
        }

        if (healthBarBack != null)
        {
            Color c = panel;
            c.a = 0.32f * alpha;
            healthBarBack.color = c;
        }

        if (statusLabel != null)
        {
            Color c = muted;
            c.a *= alpha;
            statusLabel.color = c;
        }

        if (healthLabel != null)
        {
            Color c = muted;
            c.a *= alpha;
            healthLabel.color = c;
        }

        if (speedUnitLabel != null)
        {
            Color c = muted;
            c.a *= alpha;
            speedUnitLabel.color = c;
        }

        if (speedValueLabel != null)
        {
            Color c = text;
            c.a *= alpha;
            speedValueLabel.color = c;
        }
    }

    private Color GetHealthColor(RoverTheme theme, float alpha)
    {
        Color target = currentHealthNormalized > 0.6f
            ? new Color(0.78f, 0.81f, 0.85f, 1f)
            : currentHealthNormalized > 0.3f
                ? new Color(1f, 0.76f, 0.29f, 1f)
                : new Color(1f, 0.38f, 0.32f, 1f);
        target.a *= alpha;
        return target;
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
