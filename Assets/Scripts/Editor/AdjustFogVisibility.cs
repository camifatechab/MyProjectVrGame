using UnityEngine;
using UnityEditor;

/// <summary>
/// Easy fog adjustment tool - reduces fog for better visibility
/// </summary>
public class AdjustFogVisibility : EditorWindow
{
    private static float fogMultiplier = 0.3f; // Default: reduce fog to 30%
    
    [MenuItem("Tools/Adjust Fog Visibility")]
    public static void ShowWindow()
    {
        AdjustFogVisibility window = GetWindow<AdjustFogVisibility>("Fog Adjuster");
        window.minSize = new Vector2(350, 250);
        window.Show();
    }
    
    void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Fog Visibility Control", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "Adjust how thick the fog is. Lower = clearer visibility.\n\n" +
            "Current fog is TOO STRONG for gameplay.",
            MessageType.Info);
        
        GUILayout.Space(10);
        
        EditorGUILayout.LabelField("Fog Strength:", EditorStyles.boldLabel);
        fogMultiplier = EditorGUILayout.Slider("Multiplier", fogMultiplier, 0.1f, 2.0f);
        
        GUILayout.Space(5);
        string description = "";
        if (fogMultiplier < 0.3f) description = "Very Clear (Easy to see)";
        else if (fogMultiplier < 0.5f) description = "Clear (Good visibility)";
        else if (fogMultiplier < 0.8f) description = "Medium (Some atmosphere)";
        else if (fogMultiplier < 1.2f) description = "Thick (Current - too strong)";
        else description = "Very Thick (Hard to see)";
        
        EditorGUILayout.LabelField($"Result: {description}", EditorStyles.helpBox);
        
        GUILayout.Space(15);
        
        if (GUILayout.Button("Apply Fog Settings", GUILayout.Height(40)))
        {
            ApplyFogSettings();
        }
        
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "Recommended for gameplay:\n" +
            "• Multiplier: 0.2-0.4 (clear visibility)\n" +
            "• This keeps atmosphere while allowing you to see",
            MessageType.None);
    }
    
    static void ApplyFogSettings()
    {
        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("  ADJUSTING FOG VISIBILITY");
        Debug.Log("═══════════════════════════════════════════");
        
        GameObject envManager = GameObject.Find("EnvironmentManager");
        if (envManager == null)
        {
            Debug.LogError("✗ EnvironmentManager not found!");
            return;
        }
        
        // Adjust DepthFogController
        DepthFogController depthFog = envManager.GetComponent<DepthFogController>();
        if (depthFog != null)
        {
            SerializedObject so = new SerializedObject(depthFog);
            
            // Base density (currently 0.01)
            float baseDensity = 0.01f;
            float newDensity = baseDensity * fogMultiplier;
            
            so.FindProperty("surfaceFogDensity").floatValue = newDensity;
            so.FindProperty("midDepthFogDensity").floatValue = newDensity;
            so.FindProperty("deepZoneFogDensity").floatValue = newDensity;
            so.FindProperty("caveFloorFogDensity").floatValue = newDensity;
            
            so.ApplyModifiedProperties();
            Debug.Log($"✓ Set DepthFogController density to {newDensity:F4}");
        }
        
        // Adjust UnderwaterVolumetricFog
        UnderwaterVolumetricFog volumetricFog = envManager.GetComponent<UnderwaterVolumetricFog>();
        if (volumetricFog != null)
        {
            SerializedObject so = new SerializedObject(volumetricFog);
            
            // Base density (currently 0.001)
            float baseDensity = 0.001f;
            float newDensity = baseDensity * fogMultiplier;
            
            so.FindProperty("fogDensity").floatValue = newDensity;
            so.ApplyModifiedProperties();
            Debug.Log($"✓ Set UnderwaterVolumetricFog density to {newDensity:F4}");
        }
        
        // Also apply to RenderSettings (Unity's global fog)
        if (RenderSettings.fog)
        {
            float baseDensity = 0.02f;
            RenderSettings.fogDensity = baseDensity * fogMultiplier;
            Debug.Log($"✓ Set RenderSettings fog density to {RenderSettings.fogDensity:F4}");
        }
        
        EditorUtility.SetDirty(envManager);
        
        Debug.Log("═══════════════════════════════════════════");
        Debug.Log($"<color=green>✓ FOG ADJUSTED! (x{fogMultiplier:F1})</color>");
        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("Test in Play mode to see the difference!");
        
        EditorUtility.DisplayDialog(
            "Fog Adjusted!",
            $"Fog density reduced to {(fogMultiplier * 100):F0}% of original.\n\n" +
            "Test in Play mode - you should see much better now!",
            "OK");
    }
}
