using UnityEngine;
using UnityEditor;

public class CleanMissingScripts
{
    [MenuItem("Tools/Clean Missing Scripts on Rover")]
    public static void Clean()
    {
        var rover = GameObject.Find("Rover_Cami_Trial");
        if (rover == null) { Debug.LogError("Rover_Cami_Trial not found in scene!"); return; }

        int total = 0;
        foreach (var go in rover.GetComponentsInChildren<Transform>(true))
        {
            int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go.gameObject);
            if (count > 0)
            {
                Debug.Log($"Removed {count} missing script(s) from: {go.name}");
                total += count;
            }
        }
        Debug.Log($"Done. Total missing scripts removed: {total}");
        EditorUtility.SetDirty(rover);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(rover.scene);
    }
}
