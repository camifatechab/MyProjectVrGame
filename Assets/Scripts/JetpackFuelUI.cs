using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Rover-themed VR fuel HUD for the jetpack.
/// Keeps the fuel logic simple while matching the rover UI palette and panel styling.
/// </summary>
public class JetpackFuelUI : MonoBehaviour
{
    [Header("Auto-Setup (Leave Empty)")]
    [Tooltip("Will automatically find the AutoJetpackController")]
    [SerializeField] private AutoJetpackController jetpackController;

    [Header("UI References")]
    [SerializeField] private Image fuelBarFill;
    [SerializeField] private Image fuelBarBackground;
    [SerializeField] private TextMeshProUGUI fuelText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Image panelBackground;
    [SerializeField] private Image panelGlow;
    [SerializeField] private Image accentLine;
    [SerializeField] private Image barFrame;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private CanvasGroup panelCanvasGroup;

    [Header("Visibility")]
    [Tooltip("Keep the fuel HUD hidden until the jetpack has actually been fired once this session.")]
    [SerializeField] private bool showOnlyAfterFirstUse = true;
    [Tooltip("Hide the HUD again whenever the jetpack is externally locked or disabled.")]
    [SerializeField] private bool hideWhenUnavailable = true;

    [Header("Visual Settings")]
    [SerializeField] private Color panelTint = new(0.04f, 0.09f, 0.14f, 0.84f);
    [SerializeField] private Color glowColor = new(0.43f, 0.92f, 0.95f, 1f);
    [SerializeField] private Color titleColor = new(0.67f, 0.83f, 0.9f, 1f);
    [SerializeField] private Color textColor = new(0.92f, 0.97f, 1f, 1f);
    [SerializeField] private Color mutedTextColor = new(0.67f, 0.83f, 0.9f, 1f);
    [SerializeField] private Color frameColor = new(0.12f, 0.2f, 0.28f, 1f);
    [SerializeField] private Color fullFuelColor = new(0.43f, 0.92f, 0.95f, 1f);
    [SerializeField] private Color mediumFuelColor = new(1f, 0.76f, 0.29f, 1f);
    [SerializeField] private Color lowFuelColor = new(1f, 0.38f, 0.32f, 1f);
    [SerializeField] private float smoothSpeed = 5f;

    [Header("Warning Settings")]
    [SerializeField] private float flashSpeed = 3f;
    [SerializeField] private float criticalThreshold = 10f;

    private float targetFillAmount = 1f;
    private float currentFillAmount = 1f;
    private bool isFlashing;
    private bool isVisible;

    private void Start()
    {
        if (jetpackController == null)
        {
            jetpackController = FindFirstObjectByType<AutoJetpackController>();
            if (jetpackController == null)
                Debug.LogError("JetpackFuelUI: Could not find AutoJetpackController!");
        }

        if (fuelBarFill == null)
            Debug.LogWarning("JetpackFuelUI: Fuel Bar Fill not assigned!");
        if (fuelText == null)
            Debug.LogWarning("JetpackFuelUI: Fuel Text not assigned!");
        if (statusText == null)
            Debug.LogWarning("JetpackFuelUI: Status Text not assigned!");

        ApplyStaticTheme();
        UpdateVisibility(force: true);
    }

    private void Update()
    {
        if (jetpackController == null)
            return;

        UpdateVisibility();
        UpdateFuelDisplay();
        UpdateStatusDisplay();
    }

    private void UpdateFuelDisplay()
    {
        float fuelPercentage = jetpackController.GetFuelPercentage();
        targetFillAmount = fuelPercentage / 100f;
        currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, smoothSpeed * Time.deltaTime);

        Color targetColor;
        if (fuelPercentage > 50f)
        {
            float t = (fuelPercentage - 50f) / 50f;
            targetColor = Color.Lerp(mediumFuelColor, fullFuelColor, t);
            isFlashing = false;
        }
        else if (fuelPercentage > 25f)
        {
            targetColor = mediumFuelColor;
            isFlashing = false;
        }
        else
        {
            targetColor = lowFuelColor;
            isFlashing = fuelPercentage <= criticalThreshold;
            if (isFlashing)
            {
                float flash = Mathf.PingPong(Time.time * flashSpeed, 1f);
                targetColor = Color.Lerp(lowFuelColor * 0.45f, lowFuelColor, flash);
            }
        }

        if (fuelBarFill != null)
        {
            fuelBarFill.fillAmount = currentFillAmount;
            fuelBarFill.color = targetColor;
        }

        if (barFrame != null)
            barFrame.color = Color.Lerp(frameColor, targetColor, 0.3f);

        if (panelGlow != null)
        {
            Color glow = glowColor;
            glow.a = isFlashing ? 0.22f : Mathf.Lerp(0.08f, 0.16f, 1f - targetFillAmount);
            panelGlow.color = glow;
        }

        if (fuelText != null)
        {
            fuelText.text = $"{fuelPercentage:F0}%";
            if (isFlashing)
            {
                float textFlash = Mathf.PingPong(Time.time * flashSpeed, 1f);
                fuelText.color = Color.Lerp(lowFuelColor * 0.75f, textColor, textFlash);
            }
            else
            {
                fuelText.color = textColor;
            }
        }
    }

    private void UpdateStatusDisplay()
    {
        if (statusText == null)
            return;

        if (jetpackController.IsOutOfFuel())
        {
            statusText.text = "NO FUEL";
            float flash = Mathf.PingPong(Time.time * flashSpeed, 1f);
            statusText.color = Color.Lerp(lowFuelColor * 0.45f, lowFuelColor, flash);
            return;
        }

        if (jetpackController.IsLowOnFuel())
        {
            statusText.text = "LOW FUEL";
            statusText.color = mediumFuelColor;
            return;
        }

        statusText.text = "READY";
        statusText.color = glowColor;
    }

    private void ApplyStaticTheme()
    {
        if (panelBackground != null)
            panelBackground.color = panelTint;

        if (panelGlow != null)
        {
            Color glow = glowColor;
            glow.a = 0.12f;
            panelGlow.color = glow;
        }

        if (accentLine != null)
            accentLine.color = glowColor;

        if (fuelBarBackground != null)
            fuelBarBackground.color = new Color(0.05f, 0.09f, 0.12f, 0.62f);

        if (barFrame != null)
            barFrame.color = frameColor;

        if (titleText != null)
        {
            titleText.text = "JETPACK FUEL";
            titleText.color = titleColor;
        }

        if (fuelText != null)
            fuelText.color = textColor;

        if (statusText != null)
            statusText.color = mutedTextColor;
    }

    private void UpdateVisibility(bool force = false)
    {
        if (panelCanvasGroup == null)
            panelCanvasGroup = GetComponent<CanvasGroup>();

        bool shouldShow = true;

        if (showOnlyAfterFirstUse)
            shouldShow = jetpackController != null && jetpackController.WasFiredThisSession;

        if (hideWhenUnavailable && jetpackController != null)
            shouldShow &= jetpackController.isActiveAndEnabled;

        if (!force && shouldShow == isVisible)
            return;

        isVisible = shouldShow;

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = shouldShow ? 1f : 0f;
            panelCanvasGroup.interactable = shouldShow;
            panelCanvasGroup.blocksRaycasts = shouldShow;
        }
        else
        {
            CanvasRenderer[] renderers = GetComponentsInChildren<CanvasRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].SetAlpha(shouldShow ? 1f : 0f);
        }
    }

    public void SetJetpackController(AutoJetpackController controller)
    {
        jetpackController = controller;
        UpdateVisibility(force: true);
    }
}
