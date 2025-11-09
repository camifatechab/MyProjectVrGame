using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VRFlashlightController))]
public class VRFlashlightControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        VRFlashlightController controller = (VRFlashlightController)target;
        
        if (GUILayout.Button("Auto-Setup Flashlight References"))
        {
            SetupReferences(controller);
        }
    }
    
    private void SetupReferences(VRFlashlightController controller)
    {
        // Find FlashlightPrefab
        Transform flashlightTransform = controller.transform.Find("FlashlightPrefab");
        if (flashlightTransform != null)
        {
            controller.flashlightPrefab = flashlightTransform.gameObject;
            Debug.Log("Found FlashlightPrefab");
            
            // Find Spotlight
            Transform spotlightTransform = flashlightTransform.Find("FlashlightLightsON/Spotlight");
            if (spotlightTransform != null)
            {
                controller.spotlight = spotlightTransform.GetComponent<Light>();
                Debug.Log("Found Spotlight");
            }
            
            // Find Point Light
            Transform pointLightTransform = flashlightTransform.Find("FlashlightLightsON/Point light");
            if (pointLightTransform != null)
            {
                controller.pointLight = pointLightTransform.GetComponent<Light>();
                Debug.Log("Found Point Light");
            }
            
            // Find Lights ON object
            Transform lightsOnTransform = flashlightTransform.Find("FlashlightLightsON");
            if (lightsOnTransform != null)
            {
                controller.lightsOnObject = lightsOnTransform.gameObject;
                Debug.Log("Found FlashlightLightsON");
            }
            
            // Find Lights OFF object
            Transform lightsOffTransform = flashlightTransform.Find("FlashlightLightsOFF");
            if (lightsOffTransform != null)
            {
                controller.lightsOffObject = lightsOffTransform.gameObject;
                Debug.Log("Found FlashlightLightsOFF");
            }
            
            EditorUtility.SetDirty(controller);
            Debug.Log("VRFlashlightController setup complete!");
        }
        else
        {
            Debug.LogError("FlashlightPrefab not found as child of " + controller.gameObject.name);
        }
    }
}
