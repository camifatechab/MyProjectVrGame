using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Rebuilds the jetpack fuel UI in the rover UI visual style.
/// Attach to the world-space VR UI canvas.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class AutoFuelUISetup : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private Vector2 gaugePosition = new(188f, -84f);
    [SerializeField] private Vector2 gaugeSize = new(220f, 124f);
    [SerializeField] private bool isSetupComplete;

    [Header("Theme")]
    [SerializeField] private Color panelTint = new(0.04f, 0.09f, 0.14f, 0.84f);
    [SerializeField] private Color glowColor = new(0.43f, 0.92f, 0.95f, 1f);
    [SerializeField] private Color titleColor = new(0.67f, 0.83f, 0.9f, 1f);
    [SerializeField] private Color textColor = new(0.92f, 0.97f, 1f, 1f);
    [SerializeField] private Color mutedTextColor = new(0.67f, 0.83f, 0.9f, 1f);
    [SerializeField] private Color barFrameColor = new(0.12f, 0.2f, 0.28f, 1f);
    [SerializeField] private Color barBackgroundColor = new(0.05f, 0.09f, 0.12f, 0.62f);

    private GameObject fuelGaugePanel;

    private void Start()
    {
        if (!isSetupComplete)
            SetupFuelUI();
    }

    [ContextMenu("Setup Fuel UI")]
    private void SetupFuelUI()
    {
        Transform existing = transform.Find("FuelGaugePanel");
        if (existing != null)
            DestroyImmediate(existing.gameObject);

        fuelGaugePanel = CreateNode("FuelGaugePanel", transform, gaugePosition, gaugeSize);
        CanvasGroup panelCanvasGroup = fuelGaugePanel.AddComponent<CanvasGroup>();
        Image panelRootImage = fuelGaugePanel.AddComponent<Image>();
        panelRootImage.color = new Color(1f, 1f, 1f, 0f);

        Image panelGlow = CreateImage("Glow", fuelGaugePanel.transform, Vector2.zero, gaugeSize + new Vector2(16f, 16f), new Color(glowColor.r, glowColor.g, glowColor.b, 0.14f));
        Image panelBackground = CreateImage("BackgroundPanel", fuelGaugePanel.transform, Vector2.zero, gaugeSize, panelTint);
        Image accentLine = CreateImage("AccentLine", fuelGaugePanel.transform, new Vector2(18f, 42f), new Vector2(130f, 3f), glowColor);

        TextMeshProUGUI titleText = CreateText("TitleText", fuelGaugePanel.transform, new Vector2(34f, 24f), new Vector2(126f, 18f), 11f, FontStyles.Bold, titleColor);
        titleText.text = "JETPACK FUEL";
        titleText.alignment = TextAlignmentOptions.Left;

        Image barFrame = CreateImage("FuelBarFrame", fuelGaugePanel.transform, new Vector2(-74f, -4f), new Vector2(36f, 76f), barFrameColor);
        Image fuelBarBackground = CreateImage("FuelBarBackground", barFrame.transform, Vector2.zero, new Vector2(28f, 62f), barBackgroundColor);

        Image fuelBarFill = CreateImage("FuelBarFill", fuelBarBackground.transform, Vector2.zero, new Vector2(20f, 54f), glowColor);
        fuelBarFill.type = Image.Type.Filled;
        fuelBarFill.fillMethod = Image.FillMethod.Vertical;
        fuelBarFill.fillOrigin = (int)Image.OriginVertical.Bottom;
        fuelBarFill.fillAmount = 1f;

        TextMeshProUGUI fuelText = CreateText("FuelPercentageText", fuelGaugePanel.transform, new Vector2(42f, 2f), new Vector2(120f, 30f), 19f, FontStyles.Bold, textColor);
        fuelText.text = "100%";
        fuelText.alignment = TextAlignmentOptions.Left;

        TextMeshProUGUI statusText = CreateText("StatusText", fuelGaugePanel.transform, new Vector2(42f, -22f), new Vector2(120f, 16f), 10f, FontStyles.Normal, mutedTextColor);
        statusText.text = "READY";
        statusText.alignment = TextAlignmentOptions.Left;

        TextMeshProUGUI hintText = CreateText("HintText", fuelGaugePanel.transform, new Vector2(42f, -42f), new Vector2(128f, 18f), 7.5f, FontStyles.Normal, mutedTextColor);
        hintText.text = "LAND TO RECHARGE";
        hintText.alignment = TextAlignmentOptions.Left;

        JetpackFuelUI fuelUi = fuelGaugePanel.AddComponent<JetpackFuelUI>();
        AssignFuelUiReferences(fuelUi, fuelBarFill, fuelBarBackground, fuelText, statusText, panelBackground, panelGlow, accentLine, barFrame, titleText, panelCanvasGroup);

        isSetupComplete = true;
    }

    private static void AssignFuelUiReferences(
        JetpackFuelUI fuelUi,
        Image fuelBarFill,
        Image fuelBarBackground,
        TextMeshProUGUI fuelText,
        TextMeshProUGUI statusText,
        Image panelBackground,
        Image panelGlow,
        Image accentLine,
        Image barFrame,
        TextMeshProUGUI titleText,
        CanvasGroup panelCanvasGroup)
    {
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var type = typeof(JetpackFuelUI);
        type.GetField("fuelBarFill", flags)?.SetValue(fuelUi, fuelBarFill);
        type.GetField("fuelBarBackground", flags)?.SetValue(fuelUi, fuelBarBackground);
        type.GetField("fuelText", flags)?.SetValue(fuelUi, fuelText);
        type.GetField("statusText", flags)?.SetValue(fuelUi, statusText);
        type.GetField("panelBackground", flags)?.SetValue(fuelUi, panelBackground);
        type.GetField("panelGlow", flags)?.SetValue(fuelUi, panelGlow);
        type.GetField("accentLine", flags)?.SetValue(fuelUi, accentLine);
        type.GetField("barFrame", flags)?.SetValue(fuelUi, barFrame);
        type.GetField("titleText", flags)?.SetValue(fuelUi, titleText);
        type.GetField("panelCanvasGroup", flags)?.SetValue(fuelUi, panelCanvasGroup);
    }

    private static GameObject CreateNode(string name, Transform parent, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject node = new(name, typeof(RectTransform), typeof(CanvasRenderer));
        node.transform.SetParent(parent, false);

        RectTransform rect = node.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        return node;
    }

    private static Image CreateImage(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject node = CreateNode(name, parent, anchoredPosition, size);
        Image image = node.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, float fontSize, FontStyles fontStyle, Color color)
    {
        GameObject node = CreateNode(name, parent, anchoredPosition, size);
        TextMeshProUGUI text = node.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    [ContextMenu("Remove Fuel UI")]
    private void RemoveFuelUI()
    {
        Transform existing = transform.Find("FuelGaugePanel");
        if (existing != null)
        {
            DestroyImmediate(existing.gameObject);
            isSetupComplete = false;
        }
    }
}
