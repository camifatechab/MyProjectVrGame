using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Automatically creates and sets up the Jetpack Fuel UI
/// Just attach this to your VR UI Canvas and it does everything!
/// </summary>
[RequireComponent(typeof(Canvas))]
public class AutoFuelUISetup : MonoBehaviour
{
    [Header("Auto-Setup Settings")]
    [Tooltip("Position of the fuel gauge (in canvas space)")]
    [SerializeField] private Vector2 gaugePosition = new Vector2(250, -100);
    
    [Tooltip("Size of the fuel gauge panel")]
    [SerializeField] private Vector2 gaugeSize = new Vector2(140, 200);
    
    [Header("Status")]
    [SerializeField] private bool isSetupComplete = false;
    
    private GameObject fuelGaugePanel;
    private JetpackFuelUI fuelUIScript;
    
    void Start()
    {
        if (!isSetupComplete)
        {
            SetupFuelUI();
        }
    }
    
    [ContextMenu("Setup Fuel UI")]
    void SetupFuelUI()
    {
        Debug.Log("<color=cyan>===== AUTO FUEL UI SETUP STARTING =====</color>");
        
        // Check if already exists
        Transform existing = transform.Find("FuelGaugePanel");
        if (existing != null)
        {
            Debug.LogWarning("FuelGaugePanel already exists! Removing old one...");
            DestroyImmediate(existing.gameObject);
        }
        
        // Create main panel
        fuelGaugePanel = CreateUIPanel("FuelGaugePanel", transform, gaugePosition, gaugeSize);
        
        // Create background
        GameObject background = CreateUIImage("Background", fuelGaugePanel.transform);
        SetupRectTransform(background, Vector2.zero, Vector2.zero, Vector2.one, Vector2.one, gaugeSize);
        Image bgImage = background.GetComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.5f); // Semi-transparent black
        
        // Create fuel bar background
        GameObject fuelBarBg = CreateUIImage("FuelBarBackground", fuelGaugePanel.transform);
        SetupRectTransform(fuelBarBg, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40, 120));
        fuelBarBg.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 10);
        Image barBgImage = fuelBarBg.GetComponent<Image>();
        barBgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Dark gray
        
        // Create fuel bar fill (child of background)
        GameObject fuelBarFill = CreateUIImage("FuelBarFill", fuelBarBg.transform);
        SetupRectTransform(fuelBarFill, new Vector2(5, 5), Vector2.zero, Vector2.one, new Vector2(-5, -5));
        Image fillImage = fuelBarFill.GetComponent<Image>();
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Vertical;
        fillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
        fillImage.fillAmount = 1f;
        fillImage.color = new Color(0.2f, 1f, 0.3f, 1f); // Bright green
        
        // Create fuel percentage text
        GameObject percentText = CreateTextMeshPro("FuelPercentageText", fuelGaugePanel.transform);
        SetupRectTransform(percentText, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100, 30));
        percentText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -80);
        TextMeshProUGUI percentTMP = percentText.GetComponent<TextMeshProUGUI>();
        percentTMP.text = "100%";
        percentTMP.fontSize = 24;
        percentTMP.alignment = TextAlignmentOptions.Center;
        percentTMP.color = Color.white;
        
        // Create status text
        GameObject statusText = CreateTextMeshPro("StatusText", fuelGaugePanel.transform);
        SetupRectTransform(statusText, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(120, 25));
        statusText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 85);
        TextMeshProUGUI statusTMP = statusText.GetComponent<TextMeshProUGUI>();
        statusTMP.text = "READY";
        statusTMP.fontSize = 18;
        statusTMP.alignment = TextAlignmentOptions.Center;
        statusTMP.color = Color.white;
        
        // Add and configure JetpackFuelUI script
        fuelUIScript = fuelGaugePanel.AddComponent<JetpackFuelUI>();
        
        // Use reflection to set private fields
        var fuelUIType = typeof(JetpackFuelUI);
        
        fuelUIType.GetField("fuelBarFill", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(fuelUIScript, fillImage);
        
        fuelUIType.GetField("fuelBarBackground", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(fuelUIScript, barBgImage);
        
        fuelUIType.GetField("fuelText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(fuelUIScript, percentTMP);
        
        fuelUIType.GetField("statusText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(fuelUIScript, statusTMP);
        
        isSetupComplete = true;
        
        Debug.Log("<color=green>✓✓✓ FUEL UI SETUP COMPLETE! ✓✓✓</color>");
        Debug.Log("<color=cyan>Hierarchy created:</color>");
        Debug.Log("  - FuelGaugePanel");
        Debug.Log("    - Background");
        Debug.Log("    - FuelBarBackground");
        Debug.Log("      - FuelBarFill");
        Debug.Log("    - FuelPercentageText");
        Debug.Log("    - StatusText");
    }
    
    GameObject CreateUIPanel(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        
        Image img = panel.GetComponent<Image>();
        img.color = new Color(1, 1, 1, 0); // Transparent
        
        return panel;
    }
    
    GameObject CreateUIImage(string name, Transform parent)
    {
        GameObject img = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        img.transform.SetParent(parent, false);
        return img;
    }
    
    GameObject CreateTextMeshPro(string name, Transform parent)
    {
        GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(parent, false);
        return textObj;
    }
    
    void SetupRectTransform(GameObject obj, Vector2 offsetMin, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMax, Vector2? sizeDelta = null)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        
        if (sizeDelta.HasValue)
        {
            rt.sizeDelta = sizeDelta.Value;
        }
    }
    
    [ContextMenu("Remove Fuel UI")]
    void RemoveFuelUI()
    {
        Transform existing = transform.Find("FuelGaugePanel");
        if (existing != null)
        {
            DestroyImmediate(existing.gameObject);
            isSetupComplete = false;
            Debug.Log("<color=yellow>Fuel UI removed</color>");
        }
        else
        {
            Debug.LogWarning("No Fuel UI found to remove");
        }
    }
}
