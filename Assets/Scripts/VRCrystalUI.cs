using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VRCrystalUI : MonoBehaviour
{
    [SerializeField] private Text crystalsText;
    [SerializeField] private Text remainingText;
    private CrystalManager crystalManager;
    private Canvas canvas;

    void Start()
    {
        // Find the Crystal Manager
        crystalManager = FindFirstObjectByType<CrystalManager>();
        
        // Get and configure the Canvas for VR
        canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
        }

        // Create UI elements if they don't exist
        CreateUIElements();

        // Initial update
        UpdateUI();
    }

void CreateUIElements()
    {
        // Only create UI elements if they're not assigned in Inspector
        if (crystalsText != null && remainingText != null)
        {
            return; // UI elements already assigned, skip creation
        }

        // Create Panel
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(transform, false);
        
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 1);
        panelRect.anchoredPosition = new Vector2(50, -50);
        panelRect.sizeDelta = new Vector2(400, 150);
        
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f);

        // Create Crystals Text
        GameObject crystalsTextObj = new GameObject("CrystalsText");
        crystalsTextObj.transform.SetParent(panel.transform, false);
        
        RectTransform crystalsRect = crystalsTextObj.AddComponent<RectTransform>();
        crystalsRect.anchorMin = new Vector2(0.5f, 0.5f);
        crystalsRect.anchorMax = new Vector2(0.5f, 0.5f);
        crystalsRect.pivot = new Vector2(0.5f, 0.5f);
        crystalsRect.anchoredPosition = new Vector2(0, 20);
        crystalsRect.sizeDelta = new Vector2(350, 50);
        
        crystalsText = crystalsTextObj.AddComponent<Text>();
        crystalsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        crystalsText.fontSize = 32;
        crystalsText.alignment = TextAnchor.MiddleCenter;
        crystalsText.color = Color.white;

        // Create Remaining Text
        GameObject remainingTextObj = new GameObject("RemainingText");
        remainingTextObj.transform.SetParent(panel.transform, false);
        
        RectTransform remainingRect = remainingTextObj.AddComponent<RectTransform>();
        remainingRect.anchorMin = new Vector2(0.5f, 0.5f);
        remainingRect.anchorMax = new Vector2(0.5f, 0.5f);
        remainingRect.pivot = new Vector2(0.5f, 0.5f);
        remainingRect.anchoredPosition = new Vector2(0, -30);
        remainingRect.sizeDelta = new Vector2(350, 50);
        
        remainingText = remainingTextObj.AddComponent<Text>();
        remainingText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        remainingText.fontSize = 28;
        remainingText.alignment = TextAnchor.MiddleCenter;
        remainingText.color = new Color(0.8f, 0.8f, 1f);
    }

    void Update()
    {
        // Update UI every frame
        UpdateUI();
    }

    void UpdateUI()
    {
        if (crystalManager == null) return;

        if (crystalsText != null)
        {
            crystalsText.text = $"Crystals: {crystalManager.CollectedCrystals}/{crystalManager.TotalCrystals}";
        }

        if (remainingText != null)
        {
            remainingText.text = $"Remaining: {crystalManager.RemainingCrystals}";
        }
    }
}