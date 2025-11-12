using UnityEngine;
using UnityEditor;

/// <summary>
/// Makes the volumetric beam realistic - soft, subtle, see-through
/// </summary>
public class MakeBeamRealistic : EditorWindow
{
    [MenuItem("Tools/Make Beam Realistic")]
    public static void ShowWindow()
    {
        MakeRealistic();
    }

    static void MakeRealistic()
    {
        Debug.Log("╔═══════════════════════════════════════════════╗");
        Debug.Log("║      MAKING BEAM REALISTIC                   ║");
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
        
        // REALISTIC SETTINGS:
        // - Lower alpha for see-through effect
        // - Slightly blue-white tint (like real underwater light)
        // - Longer, thinner beam
        
        SerializedProperty colorProp = so.FindProperty("beamColor");
        if (colorProp != null)
        {
            // Soft white-blue with LOW alpha (15% visible - subtle!)
            colorProp.colorValue = new Color(0.95f, 0.98f, 1.0f, 0.15f);
            Debug.Log("✓ Set realistic beam color (subtle, 15% alpha)");
        }
        
        SerializedProperty lengthProp = so.FindProperty("beamLength");
        if (lengthProp != null)
        {
            lengthProp.floatValue = 30f;
            Debug.Log("✓ Set beam length to 30m (longer)");
        }
        
        SerializedProperty sidesProp = so.FindProperty("coneSides");
        if (sidesProp != null)
        {
            sidesProp.intValue = 24; // More sides = smoother
            Debug.Log("✓ Set cone sides to 24 (smoother)");
        }
        
        so.ApplyModifiedProperties();
        
        // Adjust light for realism
        Light light = spotlight.GetComponent<Light>();
        if (light != null)
        {
            light.intensity = 40f; // Bright but not overpowering
            light.range = 30f;
            light.spotAngle = 35f; // Narrower, more focused
            light.color = new Color(1f, 0.98f, 0.95f); // Slightly warm white
            Debug.Log("✓ Configured light for realism");
            Debug.Log($"  - Intensity: {light.intensity}");
            Debug.Log($"  - Angle: {light.spotAngle}° (focused beam)");
        }
        
        EditorUtility.SetDirty(spotlight);
        
        Debug.Log("╔═══════════════════════════════════════════════╗");
        Debug.Log("║  <color=green>✓ BEAM NOW REALISTIC!</color>                     ║");
        Debug.Log("╚═══════════════════════════════════════════════╝");
        Debug.Log("");
        Debug.Log("<color=cyan>REALISTIC SETTINGS APPLIED:</color>");
        Debug.Log("✓ Subtle transparency (15% - you can see through it)");
        Debug.Log("✓ Soft blue-white color (underwater light effect)");
        Debug.Log("✓ Narrower focused beam (35° angle)");
        Debug.Log("✓ Moderate intensity (not overpowering)");
        Debug.Log("");
        Debug.Log("The beam should now:");
        Debug.Log("- Be see-through like real light");
        Debug.Log("- Enhance visibility without blocking view");
        Debug.Log("- Look natural in underwater environment");
        Debug.Log("");
        
        EditorUtility.DisplayDialog(
            "Beam Made Realistic!",
            "The volumetric beam now has:\n\n" +
            "✓ Subtle transparency (15%)\n" +
            "✓ Soft underwater light color\n" +
            "✓ Focused narrow beam (35°)\n" +
            "✓ Natural intensity\n\n" +
            "Test in Play mode!",
            "OK");
    }
}
