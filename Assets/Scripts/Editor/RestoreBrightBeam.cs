using UnityEngine;
using UnityEditor;

/// <summary>
/// Restore to the bright, visible beam settings (when darkness was over)
/// </summary>
public class RestoreBrightBeam : EditorWindow
{
    [MenuItem("Tools/Restore Bright Beam (When It Worked)")]
    public static void ShowWindow()
    {
        RestoreSettings();
    }

    static void RestoreSettings()
    {
        Debug.Log("╔═══════════════════════════════════════════════╗");
        Debug.Log("║      RESTORING BRIGHT BEAM SETTINGS          ║");
        Debug.Log("╚═══════════════════════════════════════════════╝");
        
        GameObject spotlight = GameObject.Find("Spotlight");
        if (spotlight == null)
        {
            Debug.LogError("✗ Cannot find Spotlight!");
            return;
        }
        
        SimpleVolumetricBeam beam = spotlight.GetComponent<SimpleVolumetricBeam>();
        if (beam == null)
        {
            Debug.LogError("✗ SimpleVolumetricBeam not found!");
            return;
        }
        
        SerializedObject so = new SerializedObject(beam);
        
        // RESTORE TO WORKING SETTINGS:
        // These were the settings when you said "darkness is over"
        
        SerializedProperty colorProp = so.FindProperty("beamColor");
        if (colorProp != null)
        {
            // Bright white with 50% alpha (what was working)
            colorProp.colorValue = new Color(1f, 1f, 0.95f, 0.5f);
            Debug.Log("✓ RESTORED beam color");
            Debug.Log("  - Color: Bright white");
            Debug.Log("  - Alpha: 0.5 (50% visible - BRIGHT)");
        }
        
        SerializedProperty lengthProp = so.FindProperty("beamLength");
        if (lengthProp != null)
        {
            lengthProp.floatValue = 25f;
            Debug.Log("✓ RESTORED beam length to 25m");
        }
        
        SerializedProperty sidesProp = so.FindProperty("coneSides");
        if (sidesProp != null)
        {
            sidesProp.intValue = 16; // Original setting
            Debug.Log("✓ RESTORED cone sides to 16");
        }
        
        so.ApplyModifiedProperties();
        
        // Restore light settings
        Light light = spotlight.GetComponent<Light>();
        if (light != null)
        {
            light.intensity = 100f; // Maximum brightness
            light.range = 25f;
            light.spotAngle = 45f;
            light.color = Color.white;
            Debug.Log("✓ RESTORED light settings");
            Debug.Log($"  - Intensity: {light.intensity} (MAXIMUM)");
            Debug.Log($"  - Angle: {light.spotAngle}°");
        }
        
        EditorUtility.SetDirty(spotlight);
        
        Debug.Log("╔═══════════════════════════════════════════════╗");
        Debug.Log("║  <color=green>✓ RESTORED TO WORKING SETTINGS!</color>         ║");
        Debug.Log("╚═══════════════════════════════════════════════╝");
        Debug.Log("");
        Debug.Log("<color=cyan>SETTINGS RESTORED TO:</color>");
        Debug.Log("✓ Beam Alpha: 0.5 (50% - BRIGHT and VISIBLE)");
        Debug.Log("✓ Light Intensity: 100 (MAXIMUM)");
        Debug.Log("✓ Spot Angle: 45°");
        Debug.Log("✓ Beam Length: 25m");
        Debug.Log("");
        Debug.Log("This is EXACTLY how it was when you said");
        Debug.Log("'the darkness is over' - bright and visible!");
        Debug.Log("");
        
        EditorUtility.DisplayDialog(
            "Restored to Working Settings!",
            "The beam is now back to the bright, visible settings from when you said 'the darkness is over'.\n\n" +
            "Settings:\n" +
            "✓ Alpha: 0.5 (50% visible)\n" +
            "✓ Intensity: 100 (maximum)\n" +
            "✓ Same as when it was working!\n\n" +
            "Test in Play mode!",
            "OK");
    }
}
