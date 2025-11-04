using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OxygenManager : MonoBehaviour
{
    [Header("Oxygen Settings")]
    public float maxOxygen = 100f;
    public float oxygenDepletionRate = 5f;
    public float oxygenRefillRate = 20f;
    
    [Header("UI Settings")]
    public Vector2 uiPosition = new Vector2(120, -50);
    public Vector2 uiSize = new Vector2(300, 100);
    public Color barColor = Color.green;
    
    private float currentOxygen;
    private bool isUnderwater = false;
    
    private GameObject oxygenPanel;
    private Image oxygenBarFill;
    private Text oxygenText;
    
    void Start()
    {
        currentOxygen = maxOxygen;
        CreateOxygenUI();
    }
    
void CreateOxygenUI()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found!");
            return;
        }
        
        // Create main panel - matching jetpack style
        oxygenPanel = new GameObject("OxygenPanelNew");
        oxygenPanel.transform.SetParent(canvas.transform, false);
        oxygenPanel.layer = 5;
        
        RectTransform panelRect = oxygenPanel.AddComponent<RectTransform>();
        panelRect.anchoredPosition = new Vector2(-200, -100); // Bottom right area
        panelRect.sizeDelta = new Vector2(250, 100);
        
        Image panelBg = oxygenPanel.AddComponent<Image>();
        panelBg.color = new Color(0.3f, 0.3f, 0.3f, 0.85f); // Gray background like jetpack
        
        // Create vertical green bar on the LEFT side
        GameObject barContainer = new GameObject("OxygenBarContainer");
        barContainer.transform.SetParent(oxygenPanel.transform, false);
        barContainer.layer = 5;
        
        RectTransform barContainerRect = barContainer.AddComponent<RectTransform>();
        barContainerRect.anchorMin = new Vector2(0, 0);
        barContainerRect.anchorMax = new Vector2(0, 1);
        barContainerRect.pivot = new Vector2(0, 0.5f);
        barContainerRect.anchoredPosition = new Vector2(20, 0);
        barContainerRect.sizeDelta = new Vector2(50, -20); // Vertical bar 50px wide
        
        Image barContainerBg = barContainer.AddComponent<Image>();
        barContainerBg.color = new Color(0.1f, 0.1f, 0.1f, 1f); // Dark background
        
        // Create the green fill bar (vertical)
        GameObject barFill = new GameObject("OxygenBarFill");
        barFill.transform.SetParent(barContainer.transform, false);
        barFill.layer = 5;
        
        RectTransform barFillRect = barFill.AddComponent<RectTransform>();
        barFillRect.anchorMin = new Vector2(0, 0);
        barFillRect.anchorMax = new Vector2(1, 1);
        barFillRect.anchoredPosition = Vector2.zero;
        barFillRect.sizeDelta = new Vector2(-6, -6);
        
        oxygenBarFill = barFill.AddComponent<Image>();
        oxygenBarFill.color = new Color(0, 0.8f, 0, 1f); // Bright green
        oxygenBarFill.type = Image.Type.Filled;
        oxygenBarFill.fillMethod = Image.FillMethod.Vertical; // VERTICAL fill
        oxygenBarFill.fillOrigin = (int)Image.OriginVertical.Bottom; // Fill from bottom
        oxygenBarFill.fillAmount = 1.0f;
        
        // Create text on the RIGHT side - just "Oxygen: X/5"
        GameObject textObj = new GameObject("OxygenText");
        textObj.transform.SetParent(oxygenPanel.transform, false);
        textObj.layer = 5;
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.35f, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(-20, 0);
        
        oxygenText = textObj.AddComponent<Text>();
        oxygenText.text = "Oxygen: 5/5";
        oxygenText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        oxygenText.fontSize = 20;
        oxygenText.color = Color.white;
        oxygenText.alignment = TextAnchor.MiddleRight;
        oxygenText.fontStyle = FontStyle.Bold;
        
        Debug.Log("Jetpack-style Oxygen UI created!");
    }
    
    void Update()
    {
        if (isUnderwater)
        {
            currentOxygen -= oxygenDepletionRate * Time.deltaTime;
            currentOxygen = Mathf.Max(0, currentOxygen);
            
            if (currentOxygen <= 0)
            {
                OnOxygenDepleted();
            }
        }
        else
        {
            currentOxygen += oxygenRefillRate * Time.deltaTime;
            currentOxygen = Mathf.Min(maxOxygen, currentOxygen);
        }
        
        UpdateUI();
    }
    
void UpdateUI()
    {
        if (oxygenBarFill != null)
        {
            oxygenBarFill.fillAmount = currentOxygen / maxOxygen;
        }
        
        if (oxygenText != null)
        {
            int current = Mathf.FloorToInt(currentOxygen / 20f); // Convert to 0-5 scale
            int max = Mathf.FloorToInt(maxOxygen / 20f);
            oxygenText.text = $"Oxygen: {current}/{max}";
        }
    }
    
    void OnOxygenDepleted()
    {
        Debug.Log("Out of oxygen!");
        // Add effects here
    }
    
    public void SetUnderwater(bool underwater)
    {
        isUnderwater = underwater;
        Debug.Log($"Underwater status: {underwater}");
    }
}