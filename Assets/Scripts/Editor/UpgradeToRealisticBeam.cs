using UnityEngine;
using UnityEditor;

/// <summary>
/// Upgrades to the realistic beam with full inspector control
/// </summary>
public class UpgradeToRealisticBeam : EditorWindow
{
    [MenuItem("Tools/Upgrade to Realistic Beam")]
    public static void ShowWindow()
    {
        if (EditorUtility.DisplayDialog(
            "Upgrade to Realistic Beam",
            "This will replace SimpleVolumetricBeam with RealisticVolumetricBeam.\n\n" +
            "You'll get:\n" +
            "• More realistic appearance\n" +
            "• Full inspector control\n" +
            "• Gradient falloff\n" +
            "• Adjustable transparency\n" +
            "• Edge softness\n" +
            "• Optional underwater tint\n" +
            "• Optional pulse effect\n\n" +
            "Continue?",
            "Upgrade",
            "Cancel"))
        {
            PerformUpgrade();
        }
    }

    static void PerformUpgrade()
    {
        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("  UPGRADING TO REALISTIC BEAM");
        Debug.Log("═══════════════════════════════════════════");
        
        GameObject spotlight = GameObject.Find("Spotlight");
        if (spotlight == null)
        {
            Debug.LogError("✗ Spotlight not found!");
            return;
        }
        
        // Remove old SimpleVolumetricBeam
        SimpleVolumetricBeam oldBeam = spotlight.GetComponent<SimpleVolumetricBeam>();
        if (oldBeam != null)
        {
            DestroyImmediate(oldBeam);
            Debug.Log("✓ Removed SimpleVolumetricBeam");
        }
        
        // Make sure V-Light is disabled
        Component vlight = spotlight.GetComponent("VLight");
        if (vlight != null)
        {
            var enabledProp = vlight.GetType().GetProperty("enabled");
            if (enabledProp != null)
            {
                enabledProp.SetValue(vlight, false);
            }
        }
        
        // Add RealisticVolumetricBeam
        RealisticVolumetricBeam newBeam = spotlight.GetComponent<RealisticVolumetricBeam>();
        if (newBeam == null)
        {
            newBeam = spotlight.AddComponent<RealisticVolumetricBeam>();
            Debug.Log("✓ Added RealisticVolumetricBeam");
        }
        
        // Configure for realistic underwater appearance
        Light light = spotlight.GetComponent<Light>();
        SerializedObject so = new SerializedObject(newBeam);
        
        // Set realistic defaults
        SerializedProperty spotlightProp = so.FindProperty("spotlight");
        if (spotlightProp != null)
        {
            spotlightProp.objectReferenceValue = light;
        }
        
        SerializedProperty colorProp = so.FindProperty("beamColor");
        if (colorProp != null)
        {
            // Slight cyan/white for underwater
            colorProp.colorValue = new Color(0.9f, 0.95f, 1f, 0.2f);
        }
        
        SerializedProperty transparencyProp = so.FindProperty("beamTransparency");
        if (transparencyProp != null)
        {
            transparencyProp.floatValue = 0.2f; // See-through
        }
        
        SerializedProperty falloffProp = so.FindProperty("falloffStrength");
        if (falloffProp != null)
        {
            falloffProp.floatValue = 1.2f; // Nice fade
        }
        
        SerializedProperty glowProp = so.FindProperty("sourceGlow");
        if (glowProp != null)
        {
            glowProp.floatValue = 1.5f; // Bright at source
        }
        
        SerializedProperty softnessProp = so.FindProperty("edgeSoftness");
        if (softnessProp != null)
        {
            softnessProp.floatValue = 0.3f; // Soft edges
        }
        
        SerializedProperty underwaterProp = so.FindProperty("useUnderwaterTint");
        if (underwaterProp != null)
        {
            underwaterProp.boolValue = true;
        }
        
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(spotlight);
        
        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("<color=green>✓ UPGRADE COMPLETE!</color>");
        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("");
        Debug.Log("SELECT SPOTLIGHT to see all controls in Inspector:");
        Debug.Log("• Beam Transparency (adjust visibility)");
        Debug.Log("• Falloff Strength (fade near→far)");
        Debug.Log("• Source Glow (brightness at flashlight)");
        Debug.Log("• Edge Softness (hard→soft edges)");
        Debug.Log("• Underwater Tint (cyan tint)");
        Debug.Log("• Enable Pulse (optional organic feel)");
        Debug.Log("");
        
        EditorUtility.DisplayDialog(
            "Upgrade Complete!",
            "RealisticVolumetricBeam is now active!\n\n" +
            "SELECT SPOTLIGHT in hierarchy to see all settings.\n\n" +
            "Recommended:\n" +
            "• Beam Transparency: 0.15-0.25\n" +
            "• Falloff Strength: 1.0-1.5\n" +
            "• Edge Softness: 0.3-0.5\n\n" +
            "Test in Play mode and adjust!",
            "Got it!");
        
        // Select the spotlight so user sees the Inspector
        Selection.activeGameObject = spotlight;
    }
}
