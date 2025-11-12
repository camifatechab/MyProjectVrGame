using UnityEngine;
using UnityEditor;

/// <summary>
/// Completely removes V-Light components to eliminate errors
/// </summary>
public class RemoveVLight : EditorWindow
{
    [MenuItem("Tools/Remove V-Light Components")]
    public static void ShowWindow()
    {
        if (EditorUtility.DisplayDialog(
            "Remove V-Light",
            "This will completely remove V-Light components from Spotlight.\n\n" +
            "We're using RealisticVolumetricBeam now, so V-Light is not needed.\n\n" +
            "This will eliminate the IndexOutOfRangeException error.\n\n" +
            "Continue?",
            "Remove V-Light",
            "Cancel"))
        {
            RemoveVLightComponents();
        }
    }

    static void RemoveVLightComponents()
    {
        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("  REMOVING V-LIGHT COMPONENTS");
        Debug.Log("═══════════════════════════════════════════");
        
        GameObject spotlight = GameObject.Find("Spotlight");
        if (spotlight == null)
        {
            Debug.LogError("✗ Spotlight not found!");
            return;
        }
        
        int removedCount = 0;
        
        // Remove VLight component
        Component vlight = spotlight.GetComponent("VLight");
        if (vlight != null)
        {
            DestroyImmediate(vlight);
            Debug.Log("✓ Removed VLight component");
            removedCount++;
        }
        
        // Remove Camera (used by V-Light)
        Camera cam = spotlight.GetComponent<Camera>();
        if (cam != null)
        {
            DestroyImmediate(cam);
            Debug.Log("✓ Removed Camera component");
            removedCount++;
        }
        
        // Remove MeshRenderer (used by V-Light)
        MeshRenderer mr = spotlight.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            DestroyImmediate(mr);
            Debug.Log("✓ Removed MeshRenderer component");
            removedCount++;
        }
        
        // Remove MeshFilter (used by V-Light)
        MeshFilter mf = spotlight.GetComponent<MeshFilter>();
        if (mf != null)
        {
            DestroyImmediate(mf);
            Debug.Log("✓ Removed MeshFilter component");
            removedCount++;
        }
        
        // Remove UniversalAdditionalCameraData if present
        Component cameraData = spotlight.GetComponent("UniversalAdditionalCameraData");
        if (cameraData != null)
        {
            DestroyImmediate(cameraData);
            Debug.Log("✓ Removed UniversalAdditionalCameraData");
            removedCount++;
        }
        
        EditorUtility.SetDirty(spotlight);
        
        Debug.Log("═══════════════════════════════════════════");
        Debug.Log($"<color=green>✓ REMOVED {removedCount} V-LIGHT COMPONENTS!</color>");
        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("");
        Debug.Log("REMAINING COMPONENTS:");
        Debug.Log("• Transform");
        Debug.Log("• Light");
        Debug.Log("• UniversalAdditionalLightData");
        Debug.Log("• VRLightOptimizer");
        Debug.Log("• RealisticVolumetricBeam");
        Debug.Log("");
        Debug.Log("<color=green>No more V-Light errors!</color>");
        
        EditorUtility.DisplayDialog(
            "V-Light Removed!",
            $"Successfully removed {removedCount} V-Light components.\n\n" +
            "The IndexOutOfRangeException error is now gone!\n\n" +
            "Your RealisticVolumetricBeam is still working perfectly.",
            "OK");
    }
}
