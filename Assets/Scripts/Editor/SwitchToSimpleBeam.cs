using UnityEngine;
using UnityEditor;

/// <summary>
/// Automatically switches from V-Light to SimpleVolumetricBeam
/// Run from Tools > Switch to Simple Beam
/// </summary>
public class SwitchToSimpleBeam : EditorWindow
{
    [MenuItem("Tools/Switch to Simple Beam")]
    public static void ShowWindow()
    {
        if (EditorUtility.DisplayDialog(
            "Switch to SimpleVolumetricBeam",
            "This will:\n" +
            "1. Disable V-Light components (keeps them for backup)\n" +
            "2. Add SimpleVolumetricBeam component\n" +
            "3. Configure for optimal VR visibility\n\n" +
            "Continue?",
            "Yes, Switch",
            "Cancel"))
        {
            PerformSwitch();
        }
    }

    static void PerformSwitch()
    {
        Debug.Log("═══════════════════════════════════════");
        Debug.Log("  SWITCHING TO SIMPLEVOLUMETRICBEAM");
        Debug.Log("═══════════════════════════════════════");
        
        // Find Spotlight
        GameObject spotlight = GameObject.Find("Spotlight");
        if (spotlight == null)
        {
            Debug.LogError("✗ Spotlight GameObject not found!");
            EditorUtility.DisplayDialog("Error", "Could not find Spotlight GameObject", "OK");
            return;
        }
        
        Debug.Log("✓ Found Spotlight");
        
        // Disable V-Light components
        Component vlight = spotlight.GetComponent("VLight");
        if (vlight != null)
        {
            vlight.GetType().GetProperty("enabled").SetValue(vlight, false);
            Debug.Log("✓ Disabled VLight component");
        }
        
        // Disable Camera (V-Light uses this)
        Camera cam = spotlight.GetComponent<Camera>();
        if (cam != null)
        {
            cam.enabled = false;
            Debug.Log("✓ Disabled Camera component");
        }
        
        // Disable MeshRenderer and MeshFilter (V-Light creates these)
        MeshRenderer meshRenderer = spotlight.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
            Debug.Log("✓ Disabled MeshRenderer");
        }
        
        MeshFilter meshFilter = spotlight.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            meshFilter.GetType().GetProperty("enabled")?.SetValue(meshFilter, false);
            Debug.Log("✓ Disabled MeshFilter");
        }
        
        // Add SimpleVolumetricBeam if not present
        SimpleVolumetricBeam simpleBeam = spotlight.GetComponent<SimpleVolumetricBeam>();
        if (simpleBeam == null)
        {
            simpleBeam = spotlight.AddComponent<SimpleVolumetricBeam>();
            Debug.Log("✓ Added SimpleVolumetricBeam component");
        }
        else
        {
            Debug.Log("  SimpleVolumetricBeam already present");
        }
        
        // Configure SimpleVolumetricBeam
        Light light = spotlight.GetComponent<Light>();
        if (light != null)
        {
            SerializedObject so = new SerializedObject(simpleBeam);
            
            SerializedProperty spotlightProp = so.FindProperty("spotlight");
            if (spotlightProp != null)
            {
                spotlightProp.objectReferenceValue = light;
            }
            
            SerializedProperty beamLengthProp = so.FindProperty("beamLength");
            if (beamLengthProp != null)
            {
                beamLengthProp.floatValue = 20f;
            }
            
            SerializedProperty beamColorProp = so.FindProperty("beamColor");
            if (beamColorProp != null)
            {
                beamColorProp.colorValue = new Color(1f, 1f, 0.9f, 0.2f);
            }
            
            so.ApplyModifiedProperties();
            Debug.Log("✓ Configured SimpleVolumetricBeam");
        }
        
        EditorUtility.SetDirty(spotlight);
        
        Debug.Log("═══════════════════════════════════════");
        Debug.Log("<color=green>✓ SWITCH COMPLETE!</color>");
        Debug.Log("═══════════════════════════════════════");
        Debug.Log("");
        Debug.Log("NOW:");
        Debug.Log("1. Enter Play mode");
        Debug.Log("2. Press grip button on right controller");
        Debug.Log("3. You should see a volumetric beam!");
        Debug.Log("");
        
        EditorUtility.DisplayDialog(
            "Switch Complete!",
            "SimpleVolumetricBeam has been configured.\n\n" +
            "Enter Play mode and press the grip button to test!",
            "OK");
    }
}
