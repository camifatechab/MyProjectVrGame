using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class RoverDashboardController : MonoBehaviour
{
    [Header("Dashboard References")]
    [SerializeField] private Transform panelRoot;

    [Header("Fallback Layout")]
    [SerializeField] private Vector2 panelSize = new(216f, 94f);
    [SerializeField] private float canvasScale = 0.00145f;
    [SerializeField] private Vector3 fallbackDashboardOffset = Vector3.zero;

    private Canvas worldCanvas;
    private CanvasGroup canvasGroup;
    private Image backgroundImage;
    private Image glowImage;
    private Image accentImage;
    private Image radioCardImage;
    private Image radioCardOutline;
    private Image healthBarBack;
    private Image healthBarFill;
    private Image compassAxisVertical;
    private Image compassAxisHorizontal;
    private Image compassNeedlePrimary;
    private Image compassNeedleSecondary;
    private Image compassCenterDot;
    private TextMeshProUGUI healthLabel;
    private TextMeshProUGUI compassNorthLabel;
    private TextMeshProUGUI compassEastLabel;
    private TextMeshProUGUI compassSouthLabel;
    private TextMeshProUGUI compassWestLabel;
    private TextMeshProUGUI radioValueLabel;
    private TextMeshProUGUI radioToggleHintLabel;
    private TextMeshProUGUI radioNextHintLabel;
    private TextMeshProUGUI statusLabel;
    private TextMeshProUGUI speedValueLabel;
    private TextMeshProUGUI speedUnitLabel;
    private RectTransform compassRoot;
    private Transform radioControlsRoot;
    private RoverDashboardRadioButton radioToggleButton;
    private RoverDashboardRadioButton radioNextButton;
    private bool initialized;
    private float currentAlpha;
    private float currentHealthNormalized = 1f;
    private float confirmationTimer;
    private bool wasMounted;
    private float currentHeadingAngle;
    private float radioPressFeedback;

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
        EnsureRadioControls(binder);
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
        glowImage = CreateImage("Glow", panelRect, panelSize + new Vector2(10f, 10f), new Color(0.8f, 0.84f, 0.9f, 0.1f));
        backgroundImage = CreateImage("Background", panelRect, panelSize, new Color(0.06f, 0.09f, 0.11f, 0.9f));
        accentImage = CreateImage("Accent", panelRect, new Vector2(124f, 2f), new Color(0.76f, 0.8f, 0.85f, 1f));
        accentImage.rectTransform.anchoredPosition = new Vector2(0f, 30f);

        RectTransform radioCardRect = CreateRect("RadioCard", panelRect, new Vector2(66f, 34f), new Vector2(-58f, 6f));
        radioCardImage = CreateImage("RadioCardFill", radioCardRect, new Vector2(66f, 34f), new Color(0.09f, 0.13f, 0.16f, 0.82f));
        radioCardOutline = CreateImage("RadioCardOutline", radioCardRect, new Vector2(68f, 36f), new Color(0.76f, 0.8f, 0.85f, 0.18f));

        healthLabel = CreateText("HealthLabel", panelRect, new Vector2(-86f, -29f), new Vector2(22f, 12f), 6f, FontStyles.Normal);
        healthLabel.alignment = TextAlignmentOptions.MidlineLeft;
        healthLabel.text = "HP";

        healthBarBack = CreateImage("HealthBarBack", panelRect, new Vector2(92f, 4f), new Color(1f, 1f, 1f, 0.08f));
        healthBarBack.rectTransform.anchoredPosition = new Vector2(-25f, -29f);

        healthBarFill = CreateImage("HealthBarFill", healthBarBack.rectTransform, new Vector2(92f, 4f), new Color(0.76f, 0.8f, 0.85f, 1f));
        healthBarFill.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        healthBarFill.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        healthBarFill.rectTransform.pivot = new Vector2(0f, 0.5f);
        healthBarFill.rectTransform.anchoredPosition = Vector2.zero;

        compassRoot = CreateRect("CompassRoot", panelRect, new Vector2(30f, 30f), new Vector2(74f, -25f));
        compassAxisVertical = CreateImage("CompassAxisVertical", compassRoot, new Vector2(1.1f, 20f), new Color(1f, 1f, 1f, 0.08f));
        compassAxisHorizontal = CreateImage("CompassAxisHorizontal", compassRoot, new Vector2(20f, 1.1f), new Color(1f, 1f, 1f, 0.08f));

        compassNeedlePrimary = CreateImage("CompassNeedlePrimary", compassRoot, new Vector2(1.4f, 18f), new Color(0.76f, 0.8f, 0.85f, 1f));
        compassNeedlePrimary.rectTransform.anchoredPosition = Vector2.zero;
        compassNeedlePrimary.rectTransform.pivot = new Vector2(0.5f, 0.72f);

        compassNeedleSecondary = CreateImage("CompassNeedleSecondary", compassRoot, new Vector2(1.4f, 10f), new Color(1f, 1f, 1f, 0.18f));
        compassNeedleSecondary.rectTransform.anchoredPosition = Vector2.zero;
        compassNeedleSecondary.rectTransform.pivot = new Vector2(0.5f, 0.28f);

        compassCenterDot = CreateImage("CompassCenterDot", compassRoot, new Vector2(3f, 3f), new Color(0.76f, 0.8f, 0.85f, 1f));

        compassNorthLabel = CreateText("CompassNorth", compassRoot, new Vector2(0f, 11f), new Vector2(10f, 10f), 4.8f, FontStyles.Normal);
        compassEastLabel = CreateText("CompassEast", compassRoot, new Vector2(10f, 0f), new Vector2(10f, 10f), 4.8f, FontStyles.Normal);
        compassSouthLabel = CreateText("CompassSouth", compassRoot, new Vector2(0f, -11f), new Vector2(10f, 10f), 4.8f, FontStyles.Normal);
        compassWestLabel = CreateText("CompassWest", compassRoot, new Vector2(-10f, 0f), new Vector2(10f, 10f), 4.8f, FontStyles.Normal);

        compassNorthLabel.text = "N";
        compassEastLabel.text = "E";
        compassSouthLabel.text = "S";
        compassWestLabel.text = "W";

        radioValueLabel = CreateText("RadioValue", panelRect, new Vector2(-58f, 16f), new Vector2(52f, 12f), 5.4f, FontStyles.Normal);
        radioValueLabel.alignment = TextAlignmentOptions.Center;
        radioToggleHintLabel = CreateText("RadioToggleHint", panelRect, new Vector2(-66f, -6f), new Vector2(20f, 12f), 5.4f, FontStyles.Normal);
        radioToggleHintLabel.alignment = TextAlignmentOptions.Center;
        radioToggleHintLabel.text = "ON";
        radioNextHintLabel = CreateText("RadioNextHint", panelRect, new Vector2(-48f, -6f), new Vector2(24f, 12f), 5.4f, FontStyles.Normal);
        radioNextHintLabel.alignment = TextAlignmentOptions.Center;
        radioNextHintLabel.text = "NEXT";

        statusLabel = CreateText("Status", panelRect, new Vector2(0f, 24f), new Vector2(108f, 14f), 6.9f, FontStyles.Normal);
        statusLabel.alignment = TextAlignmentOptions.Center;
        speedValueLabel = CreateText("SpeedValue", panelRect, new Vector2(-4f, 1f), new Vector2(98f, 30f), 21f, FontStyles.Bold);
        speedUnitLabel = CreateText("SpeedUnit", panelRect, new Vector2(-4f, -15f), new Vector2(64f, 12f), 6.2f, FontStyles.Normal);
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

        currentHeadingAngle = GetHeadingAngle(binder.transform.forward);
        radioPressFeedback = 0f;

        if (radioValueLabel != null)
            radioValueLabel.text = binder.RadioDisplayText;

        if (radioToggleButton != null)
            radioPressFeedback = Mathf.Max(radioPressFeedback, radioToggleButton.PressedStrength);

        if (radioNextButton != null)
            radioPressFeedback = Mathf.Max(radioPressFeedback, radioNextButton.PressedStrength);

        if (healthBarFill != null)
        {
            Vector2 size = healthBarFill.rectTransform.sizeDelta;
            size.x = Mathf.Lerp(10f, 92f, currentHealthNormalized);
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

        if (radioCardImage != null)
        {
            Color c = panel;
            c.r += 0.02f;
            c.g += 0.02f;
            c.b += 0.02f;
            c.a = Mathf.Lerp(0.58f, 0.74f, radioPressFeedback) * alpha;
            radioCardImage.color = c;
        }

        if (radioCardOutline != null)
        {
            Color c = accent;
            c.a = Mathf.Lerp(0.14f, 0.52f, radioPressFeedback) * alpha;
            radioCardOutline.color = c;
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

        if (radioValueLabel != null)
        {
            Color c = binderRadioColor(text, muted, alpha, showWarning);
            radioValueLabel.color = c;
        }

        if (radioToggleHintLabel != null)
        {
            Color c = muted;
            c.a *= alpha;
            radioToggleHintLabel.color = c;
        }

        if (radioNextHintLabel != null)
        {
            Color c = muted;
            c.a *= alpha;
            radioNextHintLabel.color = c;
        }

        ApplyCompassTheme(text, muted, accent, alpha);

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

    private Color binderRadioColor(Color text, Color muted, float alpha, bool showWarning)
    {
        Color target = muted;
        if (radioValueLabel != null && !string.IsNullOrWhiteSpace(radioValueLabel.text))
        {
            if (!radioValueLabel.text.Contains("OFF") && !radioValueLabel.text.Contains("EMPTY"))
                target = showWarning ? text : new Color(text.r * 0.95f, text.g * 0.98f, text.b, 1f);
        }

        target.a *= alpha;
        return target;
    }

    private void EnsureRadioControls(RoverUIBinder binder)
    {
        if (panelRoot == null)
            return;

        if (radioControlsRoot == null)
        {
            Transform existing = panelRoot.Find("RadioControlsRoot");
            if (existing != null)
            {
                radioControlsRoot = existing;
            }
            else
            {
                GameObject root = new("RadioControlsRoot");
                radioControlsRoot = root.transform;
                radioControlsRoot.SetParent(panelRoot, false);
                radioControlsRoot.localPosition = new Vector3(-0.081f, -0.0005f, -0.003f);
                radioControlsRoot.localRotation = Quaternion.identity;
                radioControlsRoot.localScale = Vector3.one;
            }
        }
        else
        {
            radioControlsRoot.localPosition = new Vector3(-0.081f, -0.0005f, -0.003f);
            radioControlsRoot.localRotation = Quaternion.identity;
            radioControlsRoot.localScale = Vector3.one;
        }

        if (radioToggleButton == null)
            radioToggleButton = CreateRadioButton("PowerButton", new Vector3(-0.011f, 0f, 0f), "ON", RoverDashboardRadioButton.ButtonAction.ToggleRadio);

        if (radioNextButton == null)
            radioNextButton = CreateRadioButton("NextButton", new Vector3(0.011f, 0f, 0f), "NEXT", RoverDashboardRadioButton.ButtonAction.NextTrack);

        if (radioControlsRoot != null)
            radioControlsRoot.gameObject.SetActive(binder != null && binder.IsMounted);

        radioToggleButton.transform.localPosition = new Vector3(-0.0115f, 0f, 0f);
        radioNextButton.transform.localPosition = new Vector3(0.0115f, 0f, 0f);
        DestroyButtonLabel(radioToggleButton.transform);
        DestroyButtonLabel(radioNextButton.transform);

        radioToggleButton.Bind(binder, RoverDashboardRadioButton.ButtonAction.ToggleRadio);
        radioNextButton.Bind(binder, RoverDashboardRadioButton.ButtonAction.NextTrack);
    }

    private RoverDashboardRadioButton CreateRadioButton(string name, Vector3 localPosition, string label, RoverDashboardRadioButton.ButtonAction action)
    {
        GameObject root = new(name);
        root.transform.SetParent(radioControlsRoot, false);
        root.transform.localPosition = localPosition;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.size = new Vector3(0.018f, 0.0105f, 0.009f);

        GameObject baseObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseObject.name = "Base";
        baseObject.transform.SetParent(root.transform, false);
        baseObject.transform.localPosition = Vector3.zero;
        baseObject.transform.localScale = new Vector3(0.0155f, 0.007f, 0.006f);
        Destroy(baseObject.GetComponent<Collider>());

        GameObject capObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        capObject.name = "Cap";
        capObject.transform.SetParent(root.transform, false);
        capObject.transform.localPosition = new Vector3(0f, 0f, 0.0022f);
        capObject.transform.localScale = new Vector3(0.0125f, 0.0046f, 0.0026f);
        Destroy(capObject.GetComponent<Collider>());

        root.AddComponent<XRSimpleInteractable>();
        RoverDashboardRadioButton button = root.AddComponent<RoverDashboardRadioButton>();

        button.Bind(null, action);

        return button;
    }

    private static void DestroyButtonLabel(Transform buttonTransform)
    {
        if (buttonTransform == null)
            return;

        Transform footer = buttonTransform.Find("FooterLabel");
        if (footer != null)
            Destroy(footer.gameObject);

        Transform cap = buttonTransform.Find("Cap");
        if (cap != null)
        {
            Transform legacyLabel = cap.Find("Label");
            if (legacyLabel != null)
                Destroy(legacyLabel.gameObject);
        }
    }

    private static TextMeshPro CreateWorldLabel(string name, Transform parent, string text, float zOffset, float fontSize)
    {
        GameObject labelObject = new(name);
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.localPosition = new Vector3(0f, 0f, zOffset);
        labelObject.transform.localRotation = Quaternion.identity;
        labelObject.transform.localScale = Vector3.one * 0.01f;

        TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.text = text;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        return label;
    }

    private void ApplyCompassTheme(Color text, Color muted, Color accent, float alpha)
    {
        if (compassAxisVertical != null)
        {
            Color c = muted;
            c.a = 0.16f * alpha;
            compassAxisVertical.color = c;
        }

        if (compassAxisHorizontal != null)
        {
            Color c = muted;
            c.a = 0.16f * alpha;
            compassAxisHorizontal.color = c;
        }

        if (compassNeedlePrimary != null)
        {
            compassNeedlePrimary.rectTransform.localRotation = Quaternion.Euler(0f, 0f, currentHeadingAngle);
            Color c = accent;
            c.a = 0.95f * alpha;
            compassNeedlePrimary.color = c;
        }

        if (compassNeedleSecondary != null)
        {
            compassNeedleSecondary.rectTransform.localRotation = Quaternion.Euler(0f, 0f, currentHeadingAngle);
            Color c = muted;
            c.a = 0.3f * alpha;
            compassNeedleSecondary.color = c;
        }

        if (compassCenterDot != null)
        {
            Color c = accent;
            c.a = 0.9f * alpha;
            compassCenterDot.color = c;
        }

        ApplyCompassLabelColor(compassNorthLabel, GetCompassLabelColor(0, text, muted, alpha));
        ApplyCompassLabelColor(compassEastLabel, GetCompassLabelColor(1, text, muted, alpha));
        ApplyCompassLabelColor(compassSouthLabel, GetCompassLabelColor(2, text, muted, alpha));
        ApplyCompassLabelColor(compassWestLabel, GetCompassLabelColor(3, text, muted, alpha));
    }

    private Color GetCompassLabelColor(int index, Color text, Color muted, float alpha)
    {
        int activeIndex = GetCardinalIndex(currentHeadingAngle);
        Color target = index == activeIndex ? text : muted;
        target.a *= alpha;
        return target;
    }

    private static void ApplyCompassLabelColor(TextMeshProUGUI label, Color color)
    {
        if (label != null)
            label.color = color;
    }

    private static int GetCardinalIndex(float headingAngle)
    {
        float normalized = Mathf.Repeat(headingAngle, 360f);
        return Mathf.RoundToInt(normalized / 90f) % 4;
    }

    private static float GetHeadingAngle(Vector3 forward)
    {
        Vector2 flatForward = new Vector2(forward.x, forward.z);
        if (flatForward.sqrMagnitude < 0.0001f)
            return 0f;

        float angle = Mathf.Atan2(flatForward.x, flatForward.y) * Mathf.Rad2Deg;
        if (angle < 0f)
            angle += 360f;

        return -angle;
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
