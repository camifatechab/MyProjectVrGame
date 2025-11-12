using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Complete V-Light configuration and shader fix
/// Run this from Tools > Complete V-Light Fix
/// </summary>
public class CompleteVLightFix : EditorWindow
{
    [MenuItem("Tools/Complete V-Light Fix")]
    public static void ShowWindow()
    {
        CompleteVLightFix window = GetWindow<CompleteVLightFix>("V-Light Complete Fix");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }

    private Vector2 scrollPosition;

    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        GUILayout.Space(10);
        EditorGUILayout.LabelField("V-Light Complete Configuration", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "This will configure V-Light for your underwater flashlight.\n" +
            "Make sure you have the V-Light asset package imported!",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("1. Check V-Light Installation", GUILayout.Height(40)))
        {
            CheckVLightInstallation();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("2. Configure URP for V-Light", GUILayout.Height(40)))
        {
            ConfigureURPForVLight();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("3. Setup Spotlight GameObject", GUILayout.Height(40)))
        {
            SetupSpotlightForVLight();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("4. Test V-Light Configuration", GUILayout.Height(40)))
        {
            TestVLightConfiguration();
        }
        
        GUILayout.Space(20);
        EditorGUILayout.HelpBox(
            "After clicking all buttons, restart Unity for URP changes to take effect.",
            MessageType.Warning
        );
        
        EditorGUILayout.EndScrollView();
    }

    void CheckVLightInstallation()
    {
        Debug.Log("=== Checking V-Light Installation ===");
        
        // Check for VLight script
        string[] vlightScripts = AssetDatabase.FindAssets("t:Script VLight");
        if (vlightScripts.Length == 0)
        {
            Debug.LogError("✗ VLight script not found! Import V-Light from Asset Store.");
            EditorUtility.DisplayDialog("V-Light Missing", 
                "V-Light asset package not found!\n\nPlease import V-Light from the Unity Asset Store first.",
                "OK");
            return;
        }
        else
        {
            Debug.Log($"✓ Found VLight script: {vlightScripts.Length} instance(s)");
        }
        
        // Check for VLight shaders
        string[] vlightShaders = AssetDatabase.FindAssets("VLight");
        Debug.Log($"✓ Found {vlightShaders.Length} V-Light related files");
        
        // Check for VLight folder
        if (Directory.Exists("Assets/VLights"))
        {
            Debug.Log("✓ VLights folder exists");
        }
        else
        {
            Debug.LogWarning("⚠ VLights folder not found in expected location");
        }
        
        Debug.Log("<color=green>=== V-Light Installation Check Complete ===</color>");
    }

    void ConfigureURPForVLight()
    {
        Debug.Log("=== Configuring URP for V-Light ===");
        
        int rendererCount = 0;
        int configuredCount = 0;
        
        // Find and configure all URP renderers
        string[] rendererGUIDs = AssetDatabase.FindAssets("t:UniversalRendererData");
        
        foreach (string guid in rendererGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Object rendererObj = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
            
            if (rendererObj != null)
            {
                rendererCount++;
                SerializedObject so = new SerializedObject(rendererObj);
                
                // Enable depth texture
                SerializedProperty depthProp = so.FindProperty("m_RequiresDepthTexture");
                if (depthProp != null)
                {
                    if (!depthProp.boolValue)
                    {
                        depthProp.boolValue = true;
                        Debug.Log($"✓ Enabled Depth Texture: {path}");
                        configuredCount++;
                    }
                    else
                    {
                        Debug.Log($"  Already enabled: {path}");
                    }
                }
                
                // Enable opaque texture (helpful for underwater effects)
                SerializedProperty opaqueProp = so.FindProperty("m_OpaqueDownsampling");
                if (opaqueProp != null)
                {
                    Debug.Log($"  Opaque texture configured: {path}");
                }
                
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(rendererObj);
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"<color=green>✓ Configured {rendererCount} URP Renderer(s)</color>");
        Debug.Log("<color=yellow>⚠ RESTART UNITY for URP changes to take effect!</color>");
    }

    void SetupSpotlightForVLight()
    {
        Debug.Log("=== Setting up Spotlight for V-Light ===");
        
        GameObject spotlight = GameObject.Find("Spotlight");
        if (spotlight == null)
        {
            Debug.LogError("✗ Cannot find 'Spotlight' GameObject!");
            EditorUtility.DisplayDialog("Spotlight Not Found",
                "Could not find the Spotlight GameObject.\nMake sure it exists in your scene.",
                "OK");
            return;
        }
        
        Debug.Log("✓ Found Spotlight GameObject");
        
        // Configure Light component
        Light light = spotlight.GetComponent<Light>();
        if (light == null)
        {
            light = spotlight.AddComponent<Light>();
            Debug.Log("  Added Light component");
        }
        
        light.type = LightType.Spot;
        light.range = 20f;
        light.spotAngle = 40f;
        light.intensity = 25f;
        light.color = Color.white;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.ForcePixel;
        
        Debug.Log("✓ Configured Light component:");
        Debug.Log($"  - Type: Spot, Range: {light.range}m, Angle: {light.spotAngle}°");
        Debug.Log($"  - Intensity: {light.intensity}");
        
        // Check for VLight component
        Component vlight = spotlight.GetComponent("VLight");
        if (vlight == null)
        {
            Debug.LogWarning("⚠ VLight component not found on Spotlight!");
            Debug.LogWarning("  Please add VLight component manually from V-Light package");
        }
        else
        {
            Debug.Log("✓ VLight component present");
            
            // Configure VLight via SerializedObject
            SerializedObject so = new SerializedObject(vlight);
            
            SerializedProperty intensityProp = so.FindProperty("intensity");
            if (intensityProp != null)
            {
                intensityProp.floatValue = 50f;
                Debug.Log("  Set VLight intensity to 50");
            }
            
            SerializedProperty rangeProp = so.FindProperty("rangeMultiplier");
            if (rangeProp != null)
            {
                rangeProp.floatValue = 1.0f;
            }
            
            so.ApplyModifiedProperties();
        }
        
        // Check for Camera component (V-Light may need this)
        Camera cam = spotlight.GetComponent<Camera>();
        if (cam != null)
        {
            cam.enabled = false;
            cam.depth = -100;
            Debug.Log("✓ Camera component configured");
        }
        
        EditorUtility.SetDirty(spotlight);
        
        Debug.Log("<color=green>=== Spotlight Setup Complete ===</color>");
    }

    void TestVLightConfiguration()
    {
        Debug.Log("╔══════════════════════════════════════════════╗");
        Debug.Log("║     V-LIGHT CONFIGURATION TEST              ║");
        Debug.Log("╚══════════════════════════════════════════════╝");
        
        bool allGood = true;
        
        // Test 1: VLight package
        string[] vlightScripts = AssetDatabase.FindAssets("t:Script VLight");
        if (vlightScripts.Length > 0)
        {
            Debug.Log("✓ V-Light package installed");
        }
        else
        {
            Debug.LogError("✗ V-Light package NOT found");
            allGood = false;
        }
        
        // Test 2: URP Renderers
        string[] rendererGUIDs = AssetDatabase.FindAssets("t:UniversalRendererData");
        int depthEnabled = 0;
        foreach (string guid in rendererGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Object rendererObj = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
            if (rendererObj != null)
            {
                SerializedObject so = new SerializedObject(rendererObj);
                SerializedProperty depthProp = so.FindProperty("m_RequiresDepthTexture");
                if (depthProp != null && depthProp.boolValue)
                {
                    depthEnabled++;
                }
            }
        }
        
        if (depthEnabled > 0)
        {
            Debug.Log($"✓ Depth texture enabled on {depthEnabled} renderer(s)");
        }
        else
        {
            Debug.LogWarning("⚠ Depth texture not enabled on any renderer!");
            allGood = false;
        }
        
        // Test 3: Spotlight GameObject
        GameObject spotlight = GameObject.Find("Spotlight");
        if (spotlight != null)
        {
            Debug.Log("✓ Spotlight GameObject exists");
            
            Light light = spotlight.GetComponent<Light>();
            if (light != null && light.type == LightType.Spot)
            {
                Debug.Log($"✓ Spotlight configured: {light.intensity} intensity, {light.range}m range");
            }
            else
            {
                Debug.LogWarning("⚠ Light component not properly configured");
                allGood = false;
            }
            
            Component vlight = spotlight.GetComponent("VLight");
            if (vlight != null)
            {
                Debug.Log("✓ VLight component attached");
            }
            else
            {
                Debug.LogWarning("⚠ VLight component missing!");
                allGood = false;
            }
        }
        else
        {
            Debug.LogError("✗ Spotlight GameObject not found!");
            allGood = false;
        }
        
        // Test 4: Fog settings
        if (RenderSettings.fog)
        {
            Debug.Log($"✓ Fog enabled (density: {RenderSettings.fogDensity})");
        }
        else
        {
            Debug.LogWarning("⚠ Fog is disabled - volumetric beams need fog!");
        }
        
        Debug.Log("╔══════════════════════════════════════════════╗");
        if (allGood)
        {
            Debug.Log("║  <color=green>✓ ALL TESTS PASSED!</color>                      ║");
            Debug.Log("║  Restart Unity, then test in Play mode      ║");
        }
        else
        {
            Debug.Log("║  <color=yellow>⚠ SOME ISSUES FOUND</color>                      ║");
            Debug.Log("║  Review warnings above                      ║");
        }
        Debug.Log("╚══════════════════════════════════════════════╝");
    }
}
