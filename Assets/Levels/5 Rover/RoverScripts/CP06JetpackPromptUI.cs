using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.XR.CoreUtils;

/// <summary>
/// World-space prompt flow for the CP06 transition.
/// Uses the same cyan/glow panel language as the rover prompts.
/// </summary>
public class CP06JetpackPromptUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CP06JetpackSequence sequence;
    [SerializeField] private AutoJetpackController jetpackController;

    [Header("World Prompt Placement")]
    [SerializeField] private float canvasScale = 0.00215f;
    [SerializeField] private Vector2 panelSize = new(320f, 108f);
    [SerializeField] private float sideOffset = 0.7f;
    [SerializeField] private float promptMoveSpeed = 8f;
    [SerializeField] private Vector3 grabJetpackOffset = new(0f, 1.15f, 0f);
    [SerializeField] private Vector3 useJetpackOffset = new(0f, 1.2f, 0f);
    [SerializeField] private Vector3 volcanoGoalOffset = new(0f, 1.3f, 0f);

    [Header("Prompt Copy")]
    [SerializeField] private string grabJetpackTitle = "Pick Up Jetpack";
    [SerializeField] private string grabJetpackSubtitle = "Grab the glowing jetpack";
    [SerializeField] private string grabJetpackDetail = "It unlocks flight for the volcano section.";
    [SerializeField] private string useJetpackTitle = "Use Jetpack";
    [SerializeField] private string useJetpackSubtitle = "Hold both grips and point both hands down";
    [SerializeField] private string useJetpackDetail = "Land on floating islands to recharge.";
    [SerializeField] private string volcanoGoalTitle = "Volcano Goal";
    [SerializeField] private string volcanoGoalSubtitle = "Reach the floating islands and targets";
    [SerializeField] private string volcanoGoalDetail = "Lava sends you back to CP6.";

    [Header("Timing")]
    [SerializeField] private float fadeSpeed = 6f;
    [SerializeField] private float useJetpackInstructionDuration = 8f;

    private Canvas worldCanvas;
    private CanvasGroup canvasGroup;
    private RectTransform panelRect;
    private Image glowImage;
    private Image backgroundImage;
    private Image accentImage;
    private Image pointerImage;
    private TextMeshProUGUI titleLabel;
    private TextMeshProUGUI subtitleLabel;
    private TextMeshProUGUI detailLabel;
    private float currentAlpha;
    private bool sawPickupComplete;
    private float useJetpackInstructionUntil;
    private Transform playerCamera;
    private Transform promptRoot;
    private PromptMode lastLoggedMode = PromptMode.Hidden;
    private enum PromptMode
    {
        Hidden,
        GrabJetpack,
        UseJetpack,
        VolcanoGoal
    }

    private void Awake()
    {
        EnsureRuntimePrompt();
    }

    private void OnEnable()
    {
        EnsureRuntimePrompt();
    }

    private void Update()
    {
        ResolveRuntimeReferences();
        EnsureRuntimePrompt();

        if (sequence == null)
            return;

        if (sequence.PickupComplete && !sawPickupComplete)
        {
            sawPickupComplete = true;
            useJetpackInstructionUntil = Time.time + useJetpackInstructionDuration;
        }

        PromptMode mode = ResolvePromptMode();
        bool visible = mode != PromptMode.Hidden;
        currentAlpha = Mathf.MoveTowards(currentAlpha, visible ? 1f : 0f, fadeSpeed * Time.deltaTime);

        UpdateCopy(mode);
        ApplyTheme(currentAlpha);
        LogModeChange(mode);
    }

    private void ResolveRuntimeReferences()
    {
        sequence ??= FindFirstObjectByType<CP06JetpackSequence>();
        jetpackController ??= FindFirstObjectByType<AutoJetpackController>();
        ResolvePlayerCamera();
    }

    private void ResolvePlayerCamera()
    {
        if (playerCamera != null)
            return;

        XROrigin xrOrigin = FindFirstObjectByType<XROrigin>();
        if (xrOrigin != null && xrOrigin.Camera != null)
        {
            playerCamera = xrOrigin.Camera.transform;
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
            playerCamera = mainCamera.transform;
    }

    private PromptMode ResolvePromptMode()
    {
        if (sequence == null || !sequence.SequenceUnlocked)
            return PromptMode.Hidden;

        if (!sequence.PickupComplete)
        {
            return sequence.rover != null && sequence.rover.IsMounted
                ? PromptMode.Hidden
                : PromptMode.GrabJetpack;
        }

        if (Time.time <= useJetpackInstructionUntil)
            return PromptMode.UseJetpack;

        return PromptMode.VolcanoGoal;
    }

    private void EnsureRuntimePrompt()
    {
        if (promptRoot != null && worldCanvas != null && panelRect != null)
            return;

        Transform existingCanvas = transform.Find("CP06JetpackPromptRuntime");
        GameObject canvasObject;

        if (existingCanvas != null)
        {
            canvasObject = existingCanvas.gameObject;
        }
        else
        {
            canvasObject = new GameObject("CP06JetpackPromptRuntime");
            canvasObject.transform.SetParent(transform, false);
        }

        promptRoot = canvasObject.transform;
        promptRoot.localScale = Vector3.one;

        worldCanvas = canvasObject.GetComponent<Canvas>();
        if (worldCanvas == null)
            worldCanvas = canvasObject.AddComponent<Canvas>();

        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.overrideSorting = true;
        worldCanvas.sortingOrder = 45;
        worldCanvas.pixelPerfect = false;

        canvasGroup = canvasObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = canvasObject.AddComponent<CanvasGroup>();

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = panelSize;
        canvasRect.localScale = Vector3.one * canvasScale;

        panelRect = CreateRect("Panel", canvasRect, panelSize, Vector2.zero);
        glowImage = CreateImage("Glow", panelRect, panelSize + new Vector2(18f, 18f), new Color(0.43f, 0.92f, 0.95f, 0.16f));
        backgroundImage = CreateImage("Background", panelRect, panelSize, new Color(0.02f, 0.08f, 0.12f, 0.88f));
        accentImage = CreateImage("Accent", panelRect, new Vector2(210f, 5f), new Color(0.75f, 0.95f, 1f, 1f));
        accentImage.rectTransform.anchoredPosition = new Vector2(0f, 47f);

        pointerImage = CreateImage("Pointer", panelRect, new Vector2(12f, 12f), new Color(0.75f, 0.95f, 1f, 1f));
        pointerImage.rectTransform.anchoredPosition = new Vector2(0f, -76f);
        pointerImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        titleLabel = CreateText("Title", panelRect, new Vector2(0f, 18f), new Vector2(280f, 28f), 16f, FontStyles.Bold);
        subtitleLabel = CreateText("Subtitle", panelRect, new Vector2(0f, -6f), new Vector2(280f, 22f), 9f, FontStyles.Normal);
        detailLabel = CreateText("Detail", panelRect, new Vector2(0f, -34f), new Vector2(280f, 28f), 7.2f, FontStyles.Normal);
        detailLabel.textWrappingMode = TextWrappingModes.Normal;
        detailLabel.overflowMode = TextOverflowModes.Overflow;

        Debug.Log("<color=#77ddff>[CP06JetpackPromptUI] Prompt runtime created.</color>");
    }

    private void UpdateCopy(PromptMode mode)
    {
        if (titleLabel == null || subtitleLabel == null || detailLabel == null)
            return;

        switch (mode)
        {
            case PromptMode.GrabJetpack:
                titleLabel.text = grabJetpackTitle;
                subtitleLabel.text = grabJetpackSubtitle;
                detailLabel.text = grabJetpackDetail;
                break;
            case PromptMode.UseJetpack:
                titleLabel.text = useJetpackTitle;
                subtitleLabel.text = useJetpackSubtitle;
                detailLabel.text = useJetpackDetail;
                break;
            case PromptMode.VolcanoGoal:
                titleLabel.text = volcanoGoalTitle;
                subtitleLabel.text = volcanoGoalSubtitle;
                detailLabel.text = volcanoGoalDetail;
                break;
            default:
                titleLabel.text = string.Empty;
                subtitleLabel.text = string.Empty;
                detailLabel.text = string.Empty;
                break;
        }
    }

    private void LateUpdate()
    {
        ResolveRuntimeReferences();
        EnsureRuntimePrompt();

        if (promptRoot == null)
            return;

        ResolvePlayerCamera();
        PromptMode mode = ResolvePromptMode();
        if (mode == PromptMode.Hidden || playerCamera == null)
            return;

        Transform anchor = ResolveAnchor(mode);
        if (anchor == null)
            return;

        Vector3 targetPosition = anchor.position + ResolveOffset(mode);

        Vector3 towardPlayer = playerCamera.position - targetPosition;
        towardPlayer.y = 0f;
        if (towardPlayer.sqrMagnitude > 0.0001f)
            targetPosition += towardPlayer.normalized * sideOffset;

        promptRoot.position = Vector3.Lerp(promptRoot.position, targetPosition, promptMoveSpeed * Time.deltaTime);

        Vector3 lookVector = playerCamera.position - promptRoot.position;
        if (lookVector.sqrMagnitude > 0.0001f)
            promptRoot.rotation = Quaternion.LookRotation(-lookVector.normalized, playerCamera.up);
    }

    private Transform ResolveAnchor(PromptMode mode)
    {
        if (sequence != null && sequence.jetpackPickup != null)
            return sequence.jetpackPickup.transform;

        return sequence != null ? sequence.transform : null;
    }

    private Vector3 ResolveOffset(PromptMode mode)
    {
        switch (mode)
        {
            case PromptMode.GrabJetpack:
                return grabJetpackOffset;
            case PromptMode.UseJetpack:
                return useJetpackOffset;
            case PromptMode.VolcanoGoal:
                return volcanoGoalOffset;
            default:
                return Vector3.zero;
        }
    }

    private void ApplyTheme(float alpha)
    {
        Color primary = new(0.43f, 0.92f, 0.95f, 1f);
        Color panel = new(0.02f, 0.08f, 0.12f, 0.92f);
        Color text = new(0.95f, 0.99f, 1f, 1f);
        Color muted = new(0.72f, 0.9f, 0.96f, 1f);

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

        if (detailLabel != null)
        {
            Color c = muted;
            c.a = 0.9f * alpha;
            detailLabel.color = c;
        }

        if (panelRect != null)
            panelRect.gameObject.SetActive(alpha > 0.001f);
    }

    private void LogModeChange(PromptMode mode)
    {
        if (mode == lastLoggedMode)
            return;

        lastLoggedMode = mode;
        Debug.Log($"<color=#77ddff>[CP06JetpackPromptUI] Mode: {mode}</color>");
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
