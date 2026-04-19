using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows "Hold both grips to exit" in the rover cockpit whenever the player is mounted.
/// Attach to any child of the rover GameObject that has RoverUIBinder in its hierarchy.
/// Automatically hides after the jetpack is picked up at CP06.
/// </summary>
public class RoverDismountPrompt : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private Vector2 panelSize = new(210f, 72f);
    [SerializeField] private float canvasScale = 0.002f;
    [SerializeField] private float lateralOffset = -0.38f;
    [SerializeField] private float forwardOffset = 0.05f;
    [SerializeField] private float verticalOffset = -0.08f;
    [SerializeField] private float inwardYawDegrees = 22f;
    [SerializeField] private float fadeSpeed = 6f;

    [Header("Copy")]
    [SerializeField] private string titleText = "Exit Rover";
    [SerializeField] private string subtitleText = "Hold both grips to get out";

    [Header("Behaviour")]
    [SerializeField] private bool hideAfterJetpackPickup = true;

    private Canvas worldCanvas;
    private CanvasGroup canvasGroup;
    private RectTransform panelRect;
    private Image backgroundImage, glowImage, accentImage;
    private TextMeshProUGUI titleLabel, subtitleLabel;
    private Transform promptRoot;
    private float currentAlpha;
    private bool initialized;

    private RoverUIBinder binder;
    private CP06JetpackSequence sequence;

    private void Awake() => EnsurePrompt();

    private void LateUpdate()
    {
        if (!initialized)
            EnsurePrompt();

        binder ??= GetComponentInParent<RoverUIBinder>();
        if (hideAfterJetpackPickup)
            sequence ??= FindFirstObjectByType<CP06JetpackSequence>();

        if (binder == null)
            return;

        bool visible = binder.IsMounted;
        if (hideAfterJetpackPickup && sequence != null && sequence.PickupComplete)
            visible = false;

        currentAlpha = Mathf.MoveTowards(currentAlpha, visible ? 1f : 0f, fadeSpeed * Time.deltaTime);
        ApplyTheme(currentAlpha);
        UpdatePlacement(visible);

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;
    }

    private void EnsurePrompt()
    {
        if (promptRoot == null)
        {
            Transform existing = transform.Find("RoverDismountPromptRoot");
            promptRoot = existing != null ? existing : CreateRoot();
        }

        if (worldCanvas != null)
        {
            initialized = true;
            return;
        }

        GameObject canvasGO = new("RoverDismountPromptCanvas");
        canvasGO.transform.SetParent(promptRoot, false);

        worldCanvas = canvasGO.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.overrideSorting = true;
        worldCanvas.sortingOrder = 41;
        worldCanvas.pixelPerfect = false;

        canvasGroup = canvasGO.AddComponent<CanvasGroup>();

        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = panelSize;
        canvasRect.localScale = Vector3.one * canvasScale;

        panelRect = CreateRect("Panel", canvasRect, panelSize, Vector2.zero);
        glowImage = CreateImage("Glow", panelRect, panelSize + new Vector2(18f, 18f), new Color(0.43f, 0.92f, 0.95f, 0.16f));
        backgroundImage = CreateImage("Background", panelRect, panelSize, new Color(0.02f, 0.08f, 0.12f, 0.88f));
        accentImage = CreateImage("Accent", panelRect, new Vector2(132f, 4f), new Color(0.75f, 0.95f, 1f, 1f));
        accentImage.rectTransform.anchoredPosition = new Vector2(0f, 18f);

        titleLabel = CreateText("Title", panelRect, new Vector2(0f, 6f), new Vector2(180f, 22f), 12f, FontStyles.Bold);
        subtitleLabel = CreateText("Subtitle", panelRect, new Vector2(0f, -12f), new Vector2(180f, 16f), 6.5f, FontStyles.Normal);

        if (titleLabel != null) titleLabel.text = titleText;
        if (subtitleLabel != null) subtitleLabel.text = subtitleText;

        initialized = true;
    }

    private Transform CreateRoot()
    {
        GameObject root = new("RoverDismountPromptRoot");
        root.transform.SetParent(transform, false);
        return root.transform;
    }

    private void UpdatePlacement(bool visible)
    {
        if (promptRoot == null || binder == null)
            return;

        Transform anchor = binder.DashboardAnchor != null ? binder.DashboardAnchor
                         : binder.PromptAnchor != null ? binder.PromptAnchor
                         : binder.SeatAnchor;

        float bobY = 0f;
        if (visible && binder.Theme != null)
            bobY = Mathf.Sin(Time.time * binder.Theme.bobFrequency * Mathf.PI * 2f) * binder.Theme.bobAmplitude * 0.2f;

        Vector3 localOffset = new(lateralOffset, verticalOffset + bobY, forwardOffset);

        if (anchor != null)
            promptRoot.position = anchor.TransformPoint(localOffset);

        if (binder.PlayerCamera != null && visible)
        {
            Vector3 toCamera = binder.PlayerCamera.transform.position - promptRoot.position;
            if (toCamera.sqrMagnitude > 0.0001f)
            {
                Quaternion lookAt = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
                Quaternion tilt = Quaternion.AngleAxis(inwardYawDegrees * Mathf.Sign(lateralOffset), Vector3.up);
                promptRoot.rotation = lookAt * tilt;
            }
        }
        else if (anchor != null)
        {
            promptRoot.rotation = anchor.rotation * Quaternion.Euler(0f, inwardYawDegrees * Mathf.Sign(lateralOffset), 0f);
        }
    }

    private void ApplyTheme(float alpha)
    {
        RoverTheme theme = binder?.Theme;
        Color primary = theme != null ? theme.primaryGlow : new Color(0.43f, 0.92f, 0.95f, 1f);
        Color panel = theme != null ? theme.panelTint : new Color(0.02f, 0.08f, 0.12f, 0.92f);
        Color text = theme != null ? theme.textColor : new Color(0.95f, 0.99f, 1f, 1f);
        Color muted = theme != null ? theme.mutedTextColor : new Color(0.72f, 0.9f, 0.96f, 1f);

        if (canvasGroup != null)
            canvasGroup.alpha = alpha;

        if (backgroundImage != null) { Color c = panel; c.a *= alpha; backgroundImage.color = c; }
        if (glowImage != null) { Color c = primary; c.a = 0.14f * alpha; glowImage.color = c; }
        if (accentImage != null) { Color c = primary; c.a = 0.95f * alpha; accentImage.color = c; }
        if (titleLabel != null) { Color c = text; c.a *= alpha; titleLabel.color = c; }
        if (subtitleLabel != null) { Color c = muted; c.a *= alpha; subtitleLabel.color = c; }

        if (panelRect != null)
            panelRect.gameObject.SetActive(alpha > 0.001f);
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 size, Vector2 anchoredPosition)
    {
        GameObject go = new(name);
        go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
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
        Image img = rect.gameObject.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 anchoredPos, Vector2 size, float fontSize, FontStyles style)
    {
        RectTransform rect = CreateRect(name, parent, size, anchoredPos);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.richText = false;
        return label;
    }
}
