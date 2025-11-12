using UnityEngine;
using UnityEditor;

/// <summary>
/// Makes the volumetric beam brighter and more visible
/// </summary>
public class BrightenVolumetricBeam : EditorWindow
{
    [MenuItem("Tools/Brighten Volumetric Beam")]
    public static void ShowWindow()
    {
        BrightenBeam();
    }

    static void BrightenBeam()
    {
        Debug.Log("═══════════════════════════════════════");
        Debug.Log("  BRIGHTENING VOLUMETRIC BEAM");
        Debug.Log("═══════════════════════════════════════");
        
        GameObject spotlight = GameObject.Find("Spotlight");
        if (spotlight == null)
        {
            Debug.LogError("✗ Spotlight not found!");
            return;
        }
        
        // Get SimpleVolumetricBeam
        SimpleVolumetricBeam beam = spotlight.GetComponent<SimpleVolumetricBeam>();
        if (beam == null)
        {
            Debug.LogError("✗ SimpleVolumetricBeam not found! Run 'Switch to Simple Beam' first.");
            return;
        }
        
        SerializedObject so = new SerializedObject(beam);
        
        // Set brighter beam color with more alpha
        SerializedProperty beamColorProp = so.FindProperty("beamColor");
        if (beamColorProp != null)
        {
            // Bright white with good visibility
            beamColorProp.colorValue = new Color(1f, 1f, 0.95f, 0.35f);
            Debug.Log("✓ Set beam color to bright white (alpha: 0.35)");
        }
        
        // Increase beam length for better visibility
        SerializedProperty beamLengthProp = so.FindProperty("beamLength");
        if (beamLengthProp != null)
        {
            beamLengthProp.floatValue = 25f;
            Debug.Log("✓ Set beam length to 25m");
        }
        
        so.ApplyModifiedProperties();
        
        // Also boost the light intensity
        Light light = spotlight.GetComponent<Light>();
        if (light != null)
        {
            light.intensity = 50f;
            Debug.Log("✓ Boosted light intensity to 50");
        }
        
        EditorUtility.SetDirty(spotlight);
        
        Debug.Log("═══════════════════════════════════════");
        Debug.Log("<color=green>✓ BEAM BRIGHTENED!</color>");
        Debug.Log("═══════════════════════════════════════");
        Debug.Log("Test in Play mode - beam should be much more visible!");
        
        EditorUtility.DisplayDialog("Beam Brightened!", 
            "The volumetric beam is now brighter.\n\nTest in Play mode!",
            "OK");
    }
}
