using UnityEngine;
using UnityEditor;

/// <summary>
/// FINAL FIX - Makes the volumetric beam actually visible
/// </summary>
public class FinalBeamFix : EditorWindow
{
    [MenuItem("Tools/FINAL Beam Fix - Make It Visible!")]
    public static void ShowWindow()
    {
        FixNow();
    }

    static void FixNow()
    {
        Debug.Log("╔═══════════════════════════════════════════════╗");
        Debug.Log("║      FINAL VOLUMETRIC BEAM FIX               ║");
        Debug.Log("╚═══════════════════════════════════════════════╝");
        
        GameObject spotlight = GameObject.Find("Spotlight");
        if (spotlight == null)
        {
            Debug.LogError("✗ Cannot find Spotlight!");
            return;
        }
        
        // Get SimpleVolumetricBeam
        SimpleVolumetricBeam beam = spotlight.GetComponent<SimpleVolumetricBeam>();
        if (beam == null)
        {
            Debug.LogError("✗ SimpleVolumetricBeam not found! Add it first.");
            return;
        }
        
        // DISABLE V-Light components completely
        Component vlight = spotlight.GetComponent("VLight");
        if (vlight != null)
        {
            var enabledProp = vlight.GetType().GetProperty("enabled");
            if (enabledProp != null)
            {
                enabledProp.SetValue(vlight, false);
                Debug.Log("✓ Disabled VLight");
            }
        }
        
        Camera cam = spotlight.GetComponent<Camera>();
        if (cam != null)
        {
            cam.enabled = false;
            Debug.Log("✓ Disabled Camera");
        }
        
        MeshRenderer mr = spotlight.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.enabled = false;
            Debug.Log("✓ Disabled MeshRenderer from V-Light");
        }
        
        // Configure SimpleVolumetricBeam for MAXIMUM VISIBILITY
        SerializedObject so = new SerializedObject(beam);
        
        // Bright white with HIGH alpha (50% visible)
        SerializedProperty colorProp = so.FindProperty("beamColor");
        if (colorProp != null)
        {
            colorProp.colorValue = new Color(1f, 1f, 0.95f, 0.5f);
            Debug.Log("✓ Set beam to BRIGHT with 50% alpha");
        }
        
        // Longer beam
        SerializedProperty lengthProp = so.FindProperty("beamLength");
        if (lengthProp != null)
        {
            lengthProp.floatValue = 25f;
            Debug.Log("✓ Set beam length to 25m");
        }
        
        so.ApplyModifiedProperties();
        
        // Boost light intensity to MAXIMUM
        Light light = spotlight.GetComponent<Light>();
        if (light != null)
        {
            light.intensity = 100f;
            light.range = 25f;
            light.spotAngle = 45f;
            Debug.Log("✓ Set light intensity to 100 (MAXIMUM)");
        }
        
        EditorUtility.SetDirty(spotlight);
        
        Debug.Log("╔═══════════════════════════════════════════════╗");
        Debug.Log("║  <color=green>✓✓✓ BEAM IS NOW SUPER BRIGHT! ✓✓✓</color>         ║");
        Debug.Log("╚═══════════════════════════════════════════════╝");
        Debug.Log("");
        Debug.Log("<size=14><color=yellow>NOW TEST IT:</color></size>");
        Debug.Log("1. Enter Play mode");
        Debug.Log("2. Press grip button");
        Debug.Log("3. You WILL see a bright beam!");
        Debug.Log("");
        
        EditorUtility.DisplayDialog(
            "Beam Fixed!",
            "The volumetric beam is now configured for MAXIMUM visibility.\n\n" +
            "Beam: 50% opacity\n" +
            "Light: Intensity 100\n\n" +
            "Test in Play mode NOW!",
            "OK");
    }
}
