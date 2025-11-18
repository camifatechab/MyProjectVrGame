using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// VR UI Panel for displaying crystal collection progress.
/// Connects to CrystalManager to show real-time updates.
/// Similar to JetpackFuelUI - inspector-editable and simple.
/// </summary>
public class VRCrystalPanel : MonoBehaviour
{
    [Header("UI Text References")]
    [Tooltip("Text showing 'Crystals: X/Y'")]
    [SerializeField] private Text crystalsText;
    
    [Tooltip("Text showing 'Remaining: X'")]
    [SerializeField] private Text remainingText;
    
    [Header("Display Settings")]
    [Tooltip("Color for remaining text when crystals remain")]
    [SerializeField] private Color normalColor = Color.white;
    
    [Tooltip("Color for remaining text when all collected")]
    [SerializeField] private Color completedColor = Color.green;
    
    [Tooltip("Color for crystals text when all collected")]
    [SerializeField] private Color crystalsCompletedColor = new Color(0.3f, 1f, 0.3f);
    
    private CrystalManager crystalManager;

    void Start()
    {
        // Find the CrystalManager in the scene
        crystalManager = CrystalManager.Instance;
        
        if (crystalManager == null)
        {
            Debug.LogError("VRCrystalPanel: CrystalManager not found! Make sure there's a CrystalManager in the scene.");
        }
        
        // Initial update
        UpdateDisplay();
    }

    void Update()
    {
        // Update display every frame to show real-time changes
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        if (crystalManager == null) return;
        
        // Update crystals text
        if (crystalsText != null)
        {
            crystalsText.text = $"Crystals: {crystalManager.CollectedCrystals}/{crystalManager.TotalCrystals}";
            
            // Change color if all collected
            if (crystalManager.AllCollected)
            {
                crystalsText.color = crystalsCompletedColor;
            }
            else
            {
                crystalsText.color = normalColor;
            }
        }
        
        // Update remaining text
        if (remainingText != null)
        {
            remainingText.text = $"Remaining: {crystalManager.RemainingCrystals}";
            
            // Change color based on completion
            if (crystalManager.AllCollected)
            {
                remainingText.color = completedColor;
            }
            else if (crystalManager.RemainingCrystals <= 2)
            {
                // Yellow when close to completion
                remainingText.color = Color.yellow;
            }
            else
            {
                remainingText.color = normalColor;
            }
        }
    }
}
