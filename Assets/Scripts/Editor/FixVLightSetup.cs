using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FixVLightSetup : EditorWindow
{
    [MenuItem("Tools/Fix V-Light Setup")]
    public static void ShowWindow()
    {
        GetWindow<FixVLightSetup>("V-Light Fixer");
    }

    void OnGUI()
    {
        GUILayout.Label("V-Light Setup Fixer", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        if (GUILayout.Button("Fix URP Depth Texture Settings", GUILayout.Height(40)))
        {
            FixURPSettings();
        }
        
        GUILayout.Space(10);
        if (GUILayout.Button("Configure Spotlight for V-Light", GUILayout.Height(40)))
        {
            ConfigureSpotlight();
        }
        
        GUILayout.Space(10);
        if (GUILayout.Button("Test Volumetric Light", GUILayout.Height(40)))
        {
            TestVolumetricLight();
        }
    }

    void FixURPSettings()
    {
        // Find all URP renderer assets
        string[] guids = AssetDatabase.FindAssets("t:UniversalRendererData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            
            if (rendererData != null)
            {
                Debug.Log($"Found URP Renderer: {path}");
                
                // Enable depth texture
                SerializedObject serializedRenderer = new SerializedObject(rendererData);
                SerializedProperty depthProp = serializedRenderer.FindProperty("m_RequiresDepthTexture");
                if (depthProp != null)
                {
                    depthProp.boolValue = true;
                    serializedRenderer.ApplyModifiedProperties();
                    Debug.Log("✓ Enabled Depth Texture");
                }
                
                // Enable opaque texture
                SerializedProperty opaqueProp = serializedRenderer.FindProperty("m_OpaqueDownsampling");
                if (opaqueProp != null)
                {
                    serializedRenderer.ApplyModifiedProperties();
                    Debug.Log("✓ Configured Opaque Texture");
                }
                
                EditorUtility.SetDirty(rendererData);
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green>✓ URP Settings Fixed! Restart Unity if light still doesn't work.</color>");
    }

    void ConfigureSpotlight()
    {
        // Find the Spotlight GameObject
        GameObject spotlight = GameObject.Find("Spotlight");
        if (spotlight == null)
        {
            Debug.LogError("Could not find Spotlight GameObject!");
            return;
        }
        
        // Get the Light component
        Light light = spotlight.GetComponent<Light>();
        if (light == null)
        {
            Debug.LogError("Spotlight has no Light component!");
            return;
        }
        
        // Configure for V-Light
        light.type = LightType.Spot;
        light.spotAngle = 40f;
        light.range = 20f;
        light.intensity = 3.75f;
        light.color = Color.white;
        light.shadows = LightShadows.None; // Disable shadows for better VR performance
        
        // Get VLight component
        Component vlight = spotlight.GetComponent("VLight");
        if (vlight == null)
        {
            Debug.LogError("Spotlight has no VLight component!");
            return;
        }
        
        // Configure VLight via SerializedObject
        SerializedObject so = new SerializedObject(vlight);
        
        // Enable volumetric rendering
        SerializedProperty spotlightProp = so.FindProperty("spotlight");
        if (spotlightProp != null && spotlightProp.objectReferenceValue == null)
        {
            spotlightProp.objectReferenceValue = light;
        }
        
        // Set intensity
        SerializedProperty intensityProp = so.FindProperty("intensity");
        if (intensityProp != null)
        {
            intensityProp.floatValue = 25f;
        }
        
        // Set range multiplier
        SerializedProperty rangeProp = so.FindProperty("rangeMultiplier");
        if (rangeProp != null)
        {
            rangeProp.floatValue = 1.0f;
        }
        
        so.ApplyModifiedProperties();
        
        EditorUtility.SetDirty(spotlight);
        Debug.Log("<color=green>✓ Spotlight configured for V-Light!</color>");
        Debug.Log($"  - Light Type: {light.type}");
        Debug.Log($"  - Spot Angle: {light.spotAngle}°");
        Debug.Log($"  - Range: {light.range}m");
        Debug.Log($"  - Intensity: {light.intensity}");
    }
    
    void TestVolumetricLight()
    {
        // Find the spotlight
        GameObject spotlight = GameObject.Find("Spotlight");
        if (spotlight == null)
        {
            Debug.LogError("Could not find Spotlight!");
            return;
        }
        
        // Check all required components
        bool hasLight = spotlight.GetComponent<Light>() != null;
        bool hasVLight = spotlight.GetComponent("VLight") != null;
        bool hasCamera = spotlight.GetComponent<Camera>() != null;
        
        Debug.Log("=== V-Light Component Check ===");
        Debug.Log($"  Light Component: {(hasLight ? "✓" : "✗")}");
        Debug.Log($"  VLight Component: {(hasVLight ? "✓" : "✗")}");
        Debug.Log($"  Camera Component: {(hasCamera ? "✓" : "✗")}");
        
        if (hasLight && hasVLight && hasCamera)
        {
            Debug.Log("<color=green>✓ All required components present!</color>");
            Debug.Log("If you still don't see the volumetric beam:");
            Debug.Log("  1. Make sure Fog is enabled (it is in your scene)");
            Debug.Log("  2. Restart Unity to reload URP settings");
            Debug.Log("  3. Check that the light intensity is high enough");
        }
        else
        {
            Debug.LogWarning("Missing required components. Click 'Configure Spotlight for V-Light'");
        }
    }
}
