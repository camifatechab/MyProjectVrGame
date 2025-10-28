using UnityEngine;
using UnityEngine.UI;

public class CrystalCounterUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject crystalPanel;
    [SerializeField] private Text crystalsText;
    [SerializeField] private Text remainingText;
    
    [Header("Settings")]
    [SerializeField] private int totalCrystals = 5;
    
    private int collectedCrystals = 0;

    void Start()
    {
        // Initial update
        UpdateUI();
    }

    public void CollectCrystal()
    {
        collectedCrystals++;
        UpdateUI();
        
        // Check if all crystals collected
        if (collectedCrystals >= totalCrystals)
        {
            OnAllCrystalsCollected();
        }
    }

    private void UpdateUI()
    {
        if (crystalsText != null)
        {
            crystalsText.text = $"Crystals: {collectedCrystals}/{totalCrystals}";
        }
        
        if (remainingText != null)
        {
            int remaining = totalCrystals - collectedCrystals;
            remainingText.text = $"Remaining: {remaining}";
            
            // Optional: Change color when getting close to completion
            if (remaining <= 2)
            {
                remainingText.color = Color.yellow;
            }
            if (remaining == 0)
            {
                remainingText.color = Color.green;
            }
        }
    }

    private void OnAllCrystalsCollected()
    {
        Debug.Log("All crystals collected!");
        
        if (crystalsText != null)
        {
            crystalsText.color = Color.green;
            crystalsText.text = $"COMPLETE! {collectedCrystals}/{totalCrystals}";
        }
    }

    // Public method to reset the counter
    public void ResetCounter()
    {
        collectedCrystals = 0;
        
        if (crystalsText != null)
        {
            crystalsText.color = Color.white;
        }
        
        if (remainingText != null)
        {
            remainingText.color = Color.white;
        }
        
        UpdateUI();
    }

    // Public getter for collected count
    public int GetCollectedCount()
    {
        return collectedCrystals;
    }

    // Public getter for total count
    public int GetTotalCount()
    {
        return totalCrystals;
    }
}
