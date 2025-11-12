using UnityEngine;
using UnityEditor;

/// <summary>
/// Sets fog density to very low values (below Inspector minimum)
/// This gives you crystal clear underwater visibility
/// </summary>
public class SetLowFogDensity : EditorWindow
{
    [MenuItem("Tools/Set Clear Fog (Low Density)")]
    public static void ShowWindow()
    {
        if (EditorUtility.DisplayDialog(
            "Set Clear Fog",
            "This will set fog density to VERY LOW values for clear visibility.\n\n" +
            "Fog will still exist but you'll be able to see much better!\n\n" +
            "Recommended values:\n" +
            "• Surface: 0.001\n" +
            "• Mid: 0.002\n" +
            "• Deep: 0.003\n" +
            "• Cave: 0.005\n\n" +
            "Continue?",
            "Set Clear Fog",
            "Cancel"))
        {
            SetClearFog();
        }
    }

    static void SetClearFog()
    {
        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("  SETTING CLEAR FOG (LOW DENSITY)");
        Debug.Log("═══════════════════════════════════════════");
        
        GameObject envManager = GameObject.Find("EnvironmentManager");
        if (envManager == null)
        {
            Debug.LogError("✗ EnvironmentManager not found!");
            return;
        }
        
        // Set DepthFogController to very low values
        DepthFogController depthFog = envManager.GetComponent<DepthFogController>();
        if (depthFog != null)
        {
            // Use SerializedObject to bypass Range limits
            SerializedObject so = new SerializedObject(depthFog);
            
            so.FindProperty("surfaceFogDensity").floatValue = 0.001f;
            so.FindProperty("midDepthFogDensity").floatValue = 0.002f;
            so.FindProperty("deepZoneFogDensity").floatValue = 0.003f;
            so.FindProperty("caveFloorFogDensity").floatValue = 0.005f;
            
            so.ApplyModifiedProperties();
            
            Debug.Log("✓ DepthFogController fog density:");
            Debug.Log("  Surface: 0.001 (very clear)");
            Debug.Log("  Mid: 0.002");
            Debug.Log("  Deep: 0.003");
            Debug.Log("  Cave: 0.005 (good for flashlight visibility)");
        }
        
        // Set UnderwaterVolumetricFog to very low
        UnderwaterVolumetricFog volumetricFog = envManager.GetComponent<UnderwaterVolumetricFog>();
        if (volumetricFog != null)
        {
            SerializedObject so = new SerializedObject(volumetricFog);
            so.FindProperty("fogDensity").floatValue = 0.0001f;
            so.ApplyModifiedProperties();
            
            Debug.Log("✓ UnderwaterVolumetricFog density: 0.0001");
        }
        
        // Set Unity's global fog
        if (RenderSettings.fog)
        {
            RenderSettings.fogDensity = 0.002f;
            Debug.Log("✓ RenderSettings fog density: 0.002");
        }
        
        EditorUtility.SetDirty(envManager);
        
        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("<color=green>✓ FOG SET TO CLEAR!</color>");
        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("");
        Debug.Log("RESULT:");
        Debug.Log("• Much better visibility underwater");
        Debug.Log("• Volumetric beam will show clearly");
        Debug.Log("• Still atmospheric (not totally clear)");
        Debug.Log("");
        Debug.Log("Test in Play mode!");
        
        EditorUtility.DisplayDialog(
            "Clear Fog Applied!",
            "Fog density is now MUCH lower.\n\n" +
            "You'll have excellent visibility while keeping the underwater atmosphere.\n\n" +
            "Test in Play mode!",
            "OK");
    }
    
    [MenuItem("Tools/Set Medium Fog (Balanced)")]
    public static void SetMediumFog()
    {
        GameObject envManager = GameObject.Find("EnvironmentManager");
        if (envManager == null) return;
        
        DepthFogController depthFog = envManager.GetComponent<DepthFogController>();
        if (depthFog != null)
        {
            SerializedObject so = new SerializedObject(depthFog);
            so.FindProperty("surfaceFogDensity").floatValue = 0.003f;
            so.FindProperty("midDepthFogDensity").floatValue = 0.005f;
            so.FindProperty("deepZoneFogDensity").floatValue = 0.007f;
            so.FindProperty("caveFloorFogDensity").floatValue = 0.01f;
            so.ApplyModifiedProperties();
        }
        
        UnderwaterVolumetricFog volumetricFog = envManager.GetComponent<UnderwaterVolumetricFog>();
        if (volumetricFog != null)
        {
            SerializedObject so = new SerializedObject(volumetricFog);
            so.FindProperty("fogDensity").floatValue = 0.0003f;
            so.ApplyModifiedProperties();
        }
        
        RenderSettings.fogDensity = 0.005f;
        
        EditorUtility.SetDirty(envManager);
        Debug.Log("<color=green>✓ Set to MEDIUM fog (balanced visibility)</color>");
        
        EditorUtility.DisplayDialog("Medium Fog", "Fog set to balanced medium density.", "OK");
    }
    
    [MenuItem("Tools/Set Thick Fog (Atmospheric)")]
    public static void SetThickFog()
    {
        GameObject envManager = GameObject.Find("EnvironmentManager");
        if (envManager == null) return;
        
        DepthFogController depthFog = envManager.GetComponent<DepthFogController>();
        if (depthFog != null)
        {
            SerializedObject so = new SerializedObject(depthFog);
            so.FindProperty("surfaceFogDensity").floatValue = 0.01f;
            so.FindProperty("midDepthFogDensity").floatValue = 0.015f;
            so.FindProperty("deepZoneFogDensity").floatValue = 0.02f;
            so.FindProperty("caveFloorFogDensity").floatValue = 0.03f;
            so.ApplyModifiedProperties();
        }
        
        UnderwaterVolumetricFog volumetricFog = envManager.GetComponent<UnderwaterVolumetricFog>();
        if (volumetricFog != null)
        {
            SerializedObject so = new SerializedObject(volumetricFog);
            so.FindProperty("fogDensity").floatValue = 0.001f;
            so.ApplyModifiedProperties();
        }
        
        RenderSettings.fogDensity = 0.015f;
        
        EditorUtility.SetDirty(envManager);
        Debug.Log("<color=green>✓ Set to THICK fog (very atmospheric)</color>");
        
        EditorUtility.DisplayDialog("Thick Fog", "Fog set to thick/atmospheric.", "OK");
    }
}
