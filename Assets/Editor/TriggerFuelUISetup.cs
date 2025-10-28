using UnityEngine;
using UnityEditor;

public class TriggerFuelUISetup : MonoBehaviour
{
    [MenuItem("Tools/Setup Fuel UI Now")]
    static void SetupFuelUI()
    {
        // Find the VR UI Canvas
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        
        if (canvas == null)
        {
            Debug.LogError("Could not find Canvas!");
            return;
        }
        
        // Get the AutoFuelUISetup component
        AutoFuelUISetup setupScript = canvas.GetComponent<AutoFuelUISetup>();
        
        if (setupScript == null)
        {
            Debug.LogError("Could not find AutoFuelUISetup component!");
            return;
        }
        
        // Use reflection to call the private SetupFuelUI method
        var method = typeof(AutoFuelUISetup).GetMethod("SetupFuelUI", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (method != null)
        {
            method.Invoke(setupScript, null);
            Debug.Log("<color=cyan>===== FUEL UI SETUP TRIGGERED! =====</color>");
            
            // Mark the scene as dirty so changes are saved
            EditorUtility.SetDirty(canvas.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        }
        else
        {
            Debug.LogError("Could not find SetupFuelUI method!");
        }
    }
}
