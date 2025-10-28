using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple and clear VR Fuel UI for Jetpack
/// Automatically finds the jetpack controller
/// Displays vertical fuel bar with color coding and warnings
/// </summary>
public class JetpackFuelUI : MonoBehaviour
{
    [Header("Auto-Setup (Leave Empty)")]
    [Tooltip("Will automatically find the AutoJetpackController")]
    [SerializeField] private AutoJetpackController jetpackController;
    
    [Header("UI References (Assign These)")]
    [SerializeField] private Image fuelBarFill;
    [SerializeField] private Image fuelBarBackground;
    [SerializeField] private TextMeshProUGUI fuelText;
    [SerializeField] private TextMeshProUGUI statusText;
    
    [Header("Visual Settings")]
    [SerializeField] private Color fullFuelColor = new Color(0.2f, 1f, 0.3f); // Green
    [SerializeField] private Color mediumFuelColor = new Color(1f, 0.9f, 0.2f); // Yellow
    [SerializeField] private Color lowFuelColor = new Color(1f, 0.2f, 0.2f); // Red
    [SerializeField] private float smoothSpeed = 5f;
    
    [Header("Warning Settings")]
    [SerializeField] private float flashSpeed = 3f;
    [SerializeField] private float criticalThreshold = 10f;
    
    // Internal state
    private float targetFillAmount = 1f;
    private float currentFillAmount = 1f;
    private bool isFlashing = false;
    
    void Start()
    {
        // Auto-find jetpack controller
        if (jetpackController == null)
        {
            jetpackController = FindFirstObjectByType<AutoJetpackController>();
            
            if (jetpackController != null)
            {
                Debug.Log("<color=green>✓ JetpackFuelUI found AutoJetpackController!</color>");
            }
            else
            {
                Debug.LogError("JetpackFuelUI: Could not find AutoJetpackController!");
            }
        }
        
        // Validate UI components
        if (fuelBarFill == null)
            Debug.LogWarning("JetpackFuelUI: Fuel Bar Fill not assigned!");
        if (fuelText == null)
            Debug.LogWarning("JetpackFuelUI: Fuel Text not assigned!");
        if (statusText == null)
            Debug.LogWarning("JetpackFuelUI: Status Text not assigned!");
    }
    
    void Update()
    {
        if (jetpackController == null) return;
        
        UpdateFuelDisplay();
        UpdateStatusDisplay();
    }
    
    void UpdateFuelDisplay()
    {
        // Get fuel percentage from controller
        float fuelPercentage = jetpackController.GetFuelPercentage();
        targetFillAmount = fuelPercentage / 100f;
        
        // Smooth fill animation
        currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, smoothSpeed * Time.deltaTime);
        
        if (fuelBarFill != null)
        {
            fuelBarFill.fillAmount = currentFillAmount;
            
            // Color coding based on fuel level
            Color targetColor;
            if (fuelPercentage > 50f)
            {
                // Green to Yellow transition
                float t = (fuelPercentage - 50f) / 50f;
                targetColor = Color.Lerp(mediumFuelColor, fullFuelColor, t);
            }
            else if (fuelPercentage > 25f)
            {
                // Yellow (medium fuel)
                targetColor = mediumFuelColor;
            }
            else
            {
                // Red (low fuel)
                targetColor = lowFuelColor;
                
                // Flash when critically low
                if (fuelPercentage <= criticalThreshold)
                {
                    isFlashing = true;
                    float flash = Mathf.PingPong(Time.time * flashSpeed, 1f);
                    targetColor = Color.Lerp(lowFuelColor * 0.5f, lowFuelColor, flash);
                }
                else
                {
                    isFlashing = false;
                }
            }
            
            fuelBarFill.color = targetColor;
        }
        
        // Update text display
        if (fuelText != null)
        {
            fuelText.text = $"{fuelPercentage:F0}%";
            
            // Make text flash when critically low
            if (isFlashing)
            {
                float textFlash = Mathf.PingPong(Time.time * flashSpeed, 1f);
                fuelText.color = Color.Lerp(lowFuelColor * 0.7f, Color.white, textFlash);
            }
            else
            {
                fuelText.color = Color.white;
            }
        }
    }
    
    void UpdateStatusDisplay()
    {
        if (statusText == null) return;
        
        if (jetpackController.IsOutOfFuel())
        {
            statusText.text = "NO FUEL";
            statusText.color = lowFuelColor;
            
            // Flash "NO FUEL" text
            float flash = Mathf.PingPong(Time.time * flashSpeed, 1f);
            statusText.color = Color.Lerp(lowFuelColor * 0.5f, lowFuelColor, flash);
        }
        else if (jetpackController.IsLowOnFuel())
        {
            statusText.text = "⚠ LOW FUEL";
            statusText.color = mediumFuelColor;
        }
        else
        {
            statusText.text = "READY";
            statusText.color = fullFuelColor;
        }
    }
    
    // Public method to manually set jetpack reference (if needed)
    public void SetJetpackController(AutoJetpackController controller)
    {
        jetpackController = controller;
        Debug.Log("<color=cyan>✓ JetpackFuelUI manually assigned controller</color>");
    }
}
