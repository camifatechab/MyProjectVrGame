using UnityEngine;
using UnityEditor;

/// <summary>
/// PERFECT BALANCE - Visible beam that's also see-through and realistic
/// </summary>
public class PerfectBeamBalance : EditorWindow
{
    [MenuItem("Tools/Perfect Beam Balance")]
    public static void ShowWindow()
    {
        ApplyPerfectBalance();
    }

    static void ApplyPerfectBalance()
    {
        Debug.Log("╔═══════════════════════════════════════════════╗");
        Debug.Log("║      APPLYING PERFECT BEAM BALANCE           ║");
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
        
        // THE PERFECT BALANCE:
        // - Alpha 0.28 = Visible but still see-through
        // - Soft white-blue color for underwater realism
        // - Emission helps it glow subtly
        
        SerializedProperty colorProp = so.FindProperty("beamColor");
        if (colorProp != null)
        {
            // Soft blue-white with BALANCED alpha (28% visible)
            colorProp.colorValue = new Color(0.9f, 0.95f, 1.0f, 0.28f);
            Debug.Log("✓ Set BALANCED beam color");
            Debug.Log("  - Color: Soft blue-white (underwater tint)");
            Debug.Log("  - Alpha: 0.28 (28% - visible but see-through)");
        }
        
        SerializedProperty lengthProp = so.FindProperty("beamLength");
        if (lengthProp != null)
        {
            lengthProp.floatValue = 28f;
            Debug.Log("✓ Set beam length to 28m");
        }
        
        SerializedProperty sidesProp = so.FindProperty("coneSides");
        if (sidesProp != null)
        {
            sidesProp.intValue = 24; // Smooth edges
            Debug.Log("✓ Set cone sides to 24 (smooth)");
        }
        
        so.ApplyModifiedProperties();
        
        // Light settings for perfect balance
        Light light = spotlight.GetComponent<Light>();
        if (light != null)
        {
            light.intensity = 65f; // Strong enough to see, not overpowering
            light.range = 28f;
            light.spotAngle = 38f; // Realistic flashlight angle
            light.color = new Color(0.98f, 0.98f, 1.0f); // Cool white
            Debug.Log("✓ Configured light");
            Debug.Log($"  - Intensity: {light.intensity} (balanced)");
            Debug.Log($"  - Angle: {light.spotAngle}° (realistic)");
        }
        
        EditorUtility.SetDirty(spotlight);
        
        Debug.Log("╔═══════════════════════════════════════════════╗");
        Debug.Log("║  <color=green>✓✓✓ PERFECT BALANCE ACHIEVED! ✓✓✓</color>        ║");
        Debug.Log("╚═══════════════════════════════════════════════╝");
        Debug.Log("");
        Debug.Log("<color=cyan>THE SWEET SPOT:</color>");
        Debug.Log("✓ VISIBLE: 28% alpha - you can clearly see it");
        Debug.Log("✓ SEE-THROUGH: Not solid - view isn't blocked");
        Debug.Log("✓ REALISTIC: Soft blue-white underwater tint");
        Debug.Log("✓ FOCUSED: 38° beam angle like real flashlight");
        Debug.Log("");
        Debug.Log("<color=yellow>This is the GOLDILOCKS setting:</color>");
        Debug.Log("- Not too dark (you can see it)");
        Debug.Log("- Not too bright (you can see through it)");
        Debug.Log("- Just right! 🎯");
        Debug.Log("");
        
        EditorUtility.DisplayDialog(
            "Perfect Balance Applied!",
            "The beam is now:\n\n" +
            "✓ VISIBLE (28% opacity)\n" +
            "✓ SEE-THROUGH (not blocking view)\n" +
            "✓ REALISTIC (underwater light color)\n" +
            "✓ FOCUSED (38° angle)\n\n" +
            "This is the sweet spot!\n\n" +
            "Test in Play mode NOW!",
            "Let's Go!");
    }
}
