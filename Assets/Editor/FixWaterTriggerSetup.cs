using UnityEngine;
using UnityEditor;

public class FixWaterTriggerSetup
{
    [MenuItem("Tools/Fix Water Trigger")]
[MenuItem("Tools/Fix Water Trigger")]
    public static void FixWaterTrigger()
    {
        GameObject waterTrigger = GameObject.Find("WaterTrigger");
        if (waterTrigger == null)
        {
            Debug.LogError("WaterTrigger not found!");
            return;
        }

        // Remove all missing/broken script components
        int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(waterTrigger);
        Debug.Log($"Removed {removed} broken component(s)");

        // Add WaterTriggerZone if not present
        if (waterTrigger.GetComponent<WaterTriggerZone>() == null)
        {
            waterTrigger.AddComponent<WaterTriggerZone>();
            Debug.Log("Added WaterTriggerZone component!");
        }
        else
        {
            Debug.Log("WaterTriggerZone already present.");
        }

        EditorUtility.SetDirty(waterTrigger);
        Debug.Log("WaterTrigger fixed successfully!");
    }
}
