using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages all crystals in the scene.
/// Tracks collection progress and can trigger events when all collected.
/// </summary>
public class CrystalManager : MonoBehaviour
{
    // Singleton pattern for easy access
    public static CrystalManager Instance { get; private set; }
    
    [Header("Crystal Tracking")]
    private List<CrystalCollectible> allCrystals = new List<CrystalCollectible>();
    private int collectedCount = 0;
    
    [Header("Debug")]
    [Tooltip("Show collection messages in console")]
    public bool showDebugMessages = true;
    
    // Properties for easy access
    public int TotalCrystals => allCrystals.Count;
    public int CollectedCrystals => collectedCount;
    public int RemainingCrystals => TotalCrystals - CollectedCrystals;
    public bool AllCollected => RemainingCrystals == 0 && TotalCrystals > 0;
    
    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple CrystalManagers found! Destroying duplicate.");
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Register a crystal with the manager (called by CrystalCollectible on Start)
    /// </summary>
    public void RegisterCrystal(CrystalCollectible crystal)
    {
        if (!allCrystals.Contains(crystal))
        {
            allCrystals.Add(crystal);
            
            if (showDebugMessages)
            {
                Debug.Log($"Crystal registered. Total crystals: {TotalCrystals}");
            }
        }
    }
    
    /// <summary>
    /// Called when a crystal is collected
    /// </summary>
    public void OnCrystalCollected(CrystalCollectible crystal)
    {
        if (allCrystals.Contains(crystal))
        {
            collectedCount++;
            
            if (showDebugMessages)
            {
                Debug.Log($"Crystal collected! Progress: {CollectedCrystals}/{TotalCrystals} ({RemainingCrystals} remaining)");
            }
            
            // Check if all collected
            if (AllCollected)
            {
                OnAllCrystalsCollected();
            }
        }
    }
    
    /// <summary>
    /// Called when all crystals have been collected
    /// </summary>
    void OnAllCrystalsCollected()
    {
        if (showDebugMessages)
        {
            Debug.Log("🎉 ALL CRYSTALS COLLECTED! Mission Complete!");
        }
        
        // TODO: Add your completion logic here
        // Examples:
        // - Show victory UI
        // - Play completion sound
        // - Enable spaceship repair
        // - Unlock next level
    }
    
    /// <summary>
    /// Reset collection progress (useful for testing)
    /// </summary>
    public void ResetProgress()
    {
        collectedCount = 0;
        
        if (showDebugMessages)
        {
            Debug.Log("Crystal collection progress reset.");
        }
    }
    
    // Display info in Unity Editor
    void OnGUI()
    {
        if (showDebugMessages && Application.isPlaying)
        {
            // Top-left corner display
            GUI.Box(new Rect(10, 10, 200, 60), "");
            GUI.Label(new Rect(20, 20, 180, 20), $"Crystals: {CollectedCrystals}/{TotalCrystals}");
            GUI.Label(new Rect(20, 40, 180, 20), $"Remaining: {RemainingCrystals}");
        }
    }
}
