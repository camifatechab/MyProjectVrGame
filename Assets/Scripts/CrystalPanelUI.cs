using UnityEngine;
using UnityEngine.UI;

public class CrystalPanelUI : MonoBehaviour
{
    [Header("UI Text References")]
    [SerializeField] private Text crystalsText;
    [SerializeField] private Text remainingText;

    [Header("Color Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color completeColor = Color.green;
    [SerializeField] private int warningThreshold = 2; // Change color when this many or fewer remain

    private CrystalManager crystalManager;

    void Start()
    {
        // Get reference to CrystalManager singleton
        crystalManager = CrystalManager.Instance;
        
        if (crystalManager == null)
        {
            Debug.LogError("CrystalPanelUI: CrystalManager instance not found!");
            enabled = false;
            return;
        }

        // Initial update
        UpdateUI();
    }

    void Update()
    {
        // Update UI every frame to reflect real-time changes
        if (crystalManager != null)
        {
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        int collected = crystalManager.CollectedCrystals;
        int total = crystalManager.TotalCrystals;
        int remaining = crystalManager.RemainingCrystals;

        // Update crystals text
        if (crystalsText != null)
        {
            if (remaining == 0)
            {
                crystalsText.text = $"COMPLETE! {collected}/{total}";
                crystalsText.color = completeColor;
            }
            else
            {
                crystalsText.text = $"Crystals: {collected}/{total}";
                crystalsText.color = normalColor;
            }
        }

        // Update remaining text with color coding
        if (remainingText != null)
        {
            remainingText.text = $"Remaining: {remaining}";

            // Color code based on remaining count
            if (remaining == 0)
            {
                remainingText.color = completeColor;
            }
            else if (remaining <= warningThreshold)
            {
                remainingText.color = warningColor;
            }
            else
            {
                remainingText.color = normalColor;
            }
        }
    }
}
