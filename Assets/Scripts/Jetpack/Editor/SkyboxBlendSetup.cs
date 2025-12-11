using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor helper to set up skybox blending material and controller.
/// </summary>
public class SkyboxBlendSetup : EditorWindow
{
    private Cubemap skyboxA;
    private Cubemap skyboxB;
    private float lowAltitude = 0f;
    private float highAltitude = 500f;
    
    [MenuItem("Tools/Skybox Blend Setup")]
    public static void ShowWindow()
    {
        GetWindow<SkyboxBlendSetup>("Skybox Blend Setup");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Skybox Blend Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "This tool creates a blended skybox material and controller.\n\n" +
            "1. Assign your two skybox cubemaps\n" +
            "2. Set altitude range for blending\n" +
            "3. Click 'Create Blended Skybox'", 
            MessageType.Info);
        
        GUILayout.Space(10);
        
        skyboxA = (Cubemap)EditorGUILayout.ObjectField("Skybox A (Low Altitude)", skyboxA, typeof(Cubemap), false);
        skyboxB = (Cubemap)EditorGUILayout.ObjectField("Skybox B (High Altitude)", skyboxB, typeof(Cubemap), false);
        
        GUILayout.Space(10);
        
        lowAltitude = EditorGUILayout.FloatField("Low Altitude (Skybox A)", lowAltitude);
        highAltitude = EditorGUILayout.FloatField("High Altitude (Skybox B)", highAltitude);
        
        GUILayout.Space(20);
        
        GUI.enabled = skyboxA != null && skyboxB != null;
        
        if (GUILayout.Button("Create Blended Skybox", GUILayout.Height(40)))
        {
            CreateBlendedSkybox();
        }
        
        GUI.enabled = true;
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Find Existing Skybox Cubemaps"))
        {
            FindExistingSkyboxes();
        }
    }
    
    private void CreateBlendedSkybox()
    {
        // Find or create shader
        Shader blendShader = Shader.Find("Skybox/BlendedCubemap");
        if (blendShader == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find 'Skybox/BlendedCubemap' shader. Make sure it exists in Assets/Shaders/", "OK");
            return;
        }
        
        // Create material
        Material blendMaterial = new Material(blendShader);
        blendMaterial.name = "BlendedSkybox";
        blendMaterial.SetTexture("_SkyboxA", skyboxA);
        blendMaterial.SetTexture("_SkyboxB", skyboxB);
        blendMaterial.SetFloat("_Blend", 0f);
        blendMaterial.SetFloat("_Exposure", 1f);
        
        // Save material
        string materialPath = "Assets/Materials/BlendedSkybox.mat";
        
        // Ensure Materials folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }
        
        AssetDatabase.CreateAsset(blendMaterial, materialPath);
        AssetDatabase.SaveAssets();
        
        // Set as current skybox
        RenderSettings.skybox = blendMaterial;
        
        // Create or find controller
        SkyboxBlendController controller = FindObjectOfType<SkyboxBlendController>();
        if (controller == null)
        {
            GameObject controllerObj = new GameObject("SkyboxBlendController");
            controller = controllerObj.AddComponent<SkyboxBlendController>();
        }
        
        // Configure controller
        controller.blendedSkyboxMaterial = blendMaterial;
        controller.lowAltitude = lowAltitude;
        controller.highAltitude = highAltitude;
        controller.blendMode = SkyboxBlendController.BlendMode.Altitude;
        
        // Try to find altitude target
        var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin != null)
        {
            controller.altitudeTarget = xrOrigin.transform;
        }
        
        // Mark scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        EditorUtility.DisplayDialog("Success", 
            $"Blended skybox created!\n\n" +
            $"Material: {materialPath}\n" +
            $"Skybox A: {skyboxA.name}\n" +
            $"Skybox B: {skyboxB.name}\n\n" +
            $"Altitude range: {lowAltitude} to {highAltitude}\n\n" +
            $"The skybox will now blend automatically based on altitude!", 
            "OK");
        
        // Select the controller
        Selection.activeGameObject = controller.gameObject;
    }
    
    private void FindExistingSkyboxes()
    {
        string[] guids = AssetDatabase.FindAssets("t:Cubemap");
        
        Debug.Log($"Found {guids.Length} cubemaps in project:");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Debug.Log($"  - {path}");
        }
        
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("No Cubemaps Found", 
                "No cubemap assets found in the project.\n\n" +
                "You can:\n" +
                "1. Import skybox packages from Asset Store\n" +
                "2. Create cubemaps from 6-sided textures\n" +
                "3. Use Unity's procedural skybox", 
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Cubemaps Found", 
                $"Found {guids.Length} cubemap(s). Check the Console for paths.\n\n" +
                "Drag them into the slots above.", 
                "OK");
        }
    }
}
