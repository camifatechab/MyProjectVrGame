using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool to reset SwimmingLocomotion component to script default values
/// </summary>
public class ResetSwimmingSpeed : MonoBehaviour
{
    [MenuItem("Tools/Swimming/Reset Swimming Speed to Defaults")]
    static void ResetToDefaults()
    {
        // Find XR Origin
        GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
        if (xrOrigin == null)
        {
            Debug.LogError("XR Origin (XR Rig) not found!");
            return;
        }
        
        // Get SwimmingLocomotion component
        SwimmingLocomotion swimming = xrOrigin.GetComponent<SwimmingLocomotion>();
        if (swimming == null)
        {
            Debug.LogError("SwimmingLocomotion component not found on XR Origin!");
            return;
        }
        
        // Record undo
        Undo.RecordObject(swimming, "Reset Swimming Speed");
        
        // Set to doubled speeds (script defaults)
        swimming.swimSpeed = 16.0f;
        swimming.surfaceSwimSpeed = 14.0f;
        swimming.upwardSwimSpeed = 12.0f;
        swimming.downwardSwimSpeed = 13.0f;
        swimming.frogSwimSpeed = 13.0f;
        swimming.strafeSpeed = 7.0f;
        swimming.minArmSpeed = 0.06f;
        swimming.responseSpeed = 12.0f;
        swimming.waterDrag = 1.2f;
        swimming.maxSwimSpeed = 30.0f;
        swimming.boostSpeed = 30f;
        swimming.boostThreshold = 1.5f;
        swimming.boostDuration = 0.8f;
        
        // Mark as dirty
        EditorUtility.SetDirty(swimming);
        
        Debug.Log("<color=green>✓ Swimming speeds reset to defaults (2x faster)!</color>");
        Debug.Log("swimSpeed: 16, maxSwimSpeed: 30, waterDrag: 1.2");
    }
}
