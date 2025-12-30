using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to refresh crystal particles on all crystals
/// </summary>
public class CrystalParticlesRefresher : EditorWindow
{
    [MenuItem("Tools/Refresh All Crystal Particles")]
    static void RefreshAllCrystals()
    {
        CrystalParticles[] allCrystals = FindObjectsByType<CrystalParticles>(FindObjectsSortMode.None);
        
        int count = 0;
        foreach (var crystal in allCrystals)
        {
            crystal.RefreshParticles();
            count++;
        }
        
        Debug.Log($"<color=green>✓ Refreshed particles on {count} crystals</color>");
    }
    
    [MenuItem("Tools/Refresh Selected Crystal Particles")]
    static void RefreshSelectedCrystals()
    {
        int count = 0;
        foreach (GameObject obj in Selection.gameObjects)
        {
            CrystalParticles cp = obj.GetComponent<CrystalParticles>();
            if (cp != null)
            {
                cp.RefreshParticles();
                count++;
            }
        }
        
        if (count > 0)
            Debug.Log($"<color=green>✓ Refreshed particles on {count} selected crystals</color>");
        else
            Debug.LogWarning("No CrystalParticles components found on selected objects");
    }
}
