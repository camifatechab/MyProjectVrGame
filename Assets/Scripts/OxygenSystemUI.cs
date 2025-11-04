using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OxygenSystemUI : MonoBehaviour
{
    [Header("Oxygen Settings")]
    [SerializeField] private float maxOxygen = 100f;
    [SerializeField] private float oxygenDepletionRate = 5f; // Per second
    [SerializeField] private float oxygenRefillRate = 20f; // Per second when surfaced
    
    [Header("UI References")]
    [SerializeField] private Image oxygenBarFill;
    [SerializeField] private TextMeshProUGUI oxygenText;
    [SerializeField] private Image panelBackground;
    
    [Header("UI Positioning")]
    [SerializeField] private Vector2 uiPosition = new Vector2(-200, -100);
    [SerializeField] private Vector2 uiSize = new Vector2(300, 80);
    
    private float currentOxygen;
    private bool isUnderwater = false;
    private RectTransform uiPanel;
    
    void Start()
    {
        currentOxygen = maxOxygen;
        
        // If UI elements aren't assigned, try to find them
        if (oxygenBarFill == null || oxygenText == null)
        {
            FindOrCreateUIElements();
        }
        
        UpdateUI();
    }
    
    void Update()
    {
        // Deplete or refill oxygen based on underwater status
        if (isUnderwater)
        {
            currentOxygen -= oxygenDepletionRate * Time.deltaTime;
            currentOxygen = Mathf.Max(0, currentOxygen);
            
            if (currentOxygen <= 0)
            {
                HandleOxygenDepleted();
            }
        }
        else
        {
            // Refill oxygen when surfaced
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
            int percentage = Mathf.RoundToInt((currentOxygen / maxOxygen) * 100f);
            oxygenText.text = $"Oxygen: {percentage}%";
        }
    }
    
    void HandleOxygenDepleted()
    {
        Debug.Log("Oxygen depleted! Player should be affected.");
        // Add your oxygen depletion effects here (damage, screen effects, etc.)
    }
    
    void FindOrCreateUIElements()
    {
        // Try to find existing UI elements first
        GameObject canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null)
        {
            Debug.LogError("Canvas not found in scene!");
            return;
        }
        
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        
        // Find or create oxygen panel
        Transform existingPanel = canvasObj.transform.Find("OxygenPanel");
        if (existingPanel != null)
        {
            uiPanel = existingPanel.GetComponent<RectTransform>();
            oxygenBarFill = existingPanel.Find("OxygenBarContainer/OxygenBar")?.GetComponent<Image>();
            oxygenText = existingPanel.Find("OxygenText")?.GetComponent<TextMeshProUGUI>();
        }
    }
    
    // Called by water trigger system
    public void SetUnderwater(bool underwater)
    {
        isUnderwater = underwater;
    }
    
    public float GetOxygenPercentage()
    {
        return (currentOxygen / maxOxygen) * 100f;
    }
    
    public bool IsOutOfOxygen()
    {
        return currentOxygen <= 0;
    }
    
    // Public method to restore oxygen (for power-ups, etc.)
    public void RestoreOxygen(float amount)
    {
        currentOxygen = Mathf.Min(maxOxygen, currentOxygen + amount);
        UpdateUI();
    }
}