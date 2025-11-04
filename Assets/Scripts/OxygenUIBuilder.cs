using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OxygenUIBuilder : MonoBehaviour
{
    void Start()
    {
        CreateOxygenUI();
    }
    
    void CreateOxygenUI()
    {
        // Find canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in scene!");
            return;
        }
        
        // Check if oxygen panel already exists
        Transform existingPanel = canvas.transform.Find("OxygenPanel");
        if (existingPanel != null)
        {
            Debug.Log("Oxygen UI already exists");
            return;
        }
        
        // Create main panel
        GameObject panel = new GameObject("OxygenPanel");
        panel.transform.SetParent(canvas.transform, false);
        
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(0, 0);
        panelRect.pivot = new Vector2(0, 0);
        panelRect.anchoredPosition = new Vector2(20, 20); // Bottom left
        panelRect.sizeDelta = new Vector2(300, 100);
        
        // Add background image
        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Dark semi-transparent background
        
        // Create oxygen text
        GameObject textObj = new GameObject("OxygenText");
        textObj.transform.SetParent(panel.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0.5f);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = new Vector2(0, 15);
        textRect.sizeDelta = new Vector2(-20, -20);
        
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Oxygen: 100%";
        text.fontSize = 24;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        
        // Create bar container (background)
        GameObject barContainer = new GameObject("OxygenBarContainer");
        barContainer.transform.SetParent(panel.transform, false);
        
        RectTransform barContainerRect = barContainer.AddComponent<RectTransform>();
        barContainerRect.anchorMin = new Vector2(0.1f, 0.1f);
        barContainerRect.anchorMax = new Vector2(0.9f, 0.4f);
        barContainerRect.pivot = new Vector2(0.5f, 0.5f);
        barContainerRect.anchoredPosition = Vector2.zero;
        barContainerRect.sizeDelta = Vector2.zero;
        
        Image barContainerImg = barContainer.AddComponent<Image>();
        barContainerImg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f); // Dark background for bar
        
        // Create oxygen bar (green fill)
        GameObject bar = new GameObject("OxygenBar");
        bar.transform.SetParent(barContainer.transform, false);
        
        RectTransform barRect = bar.AddComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0, 0);
        barRect.anchorMax = new Vector2(1, 1);
        barRect.pivot = new Vector2(0, 0.5f);
        barRect.anchoredPosition = Vector2.zero;
        barRect.sizeDelta = Vector2.zero;
        
        Image barImg = bar.AddComponent<Image>();
        barImg.color = new Color(0, 1, 0, 1); // Bright green
        barImg.type = Image.Type.Filled;
        barImg.fillMethod = Image.FillMethod.Horizontal;
        barImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        barImg.fillAmount = 1.0f;
        
        // Add OxygenSystemUI component to panel
        OxygenSystemUI oxygenSystem = panel.AddComponent<OxygenSystemUI>();
        
        // Use reflection to set private fields
        var oxygenSystemType = typeof(OxygenSystemUI);
        var barFillField = oxygenSystemType.GetField("oxygenBarFill", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var textField = oxygenSystemType.GetField("oxygenText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var bgField = oxygenSystemType.GetField("panelBackground", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (barFillField != null) barFillField.SetValue(oxygenSystem, barImg);
        if (textField != null) textField.SetValue(oxygenSystem, text);
        if (bgField != null) bgField.SetValue(oxygenSystem, panelBg);
        
        Debug.Log("Oxygen UI created successfully!");
    }
}