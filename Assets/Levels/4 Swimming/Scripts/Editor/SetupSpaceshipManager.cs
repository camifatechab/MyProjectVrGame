using UnityEngine;
using UnityEditor;
using Lake;

/// <summary>
/// Sets up the SpaceshipCollectionManager in the scene
/// </summary>
public class SetupSpaceshipManager : MonoBehaviour
{
    [MenuItem("Tools/Swimming/Setup Collection Manager")]
    static void SetupManager()
    {
        // Find or create the manager GameObject
        GameObject managerObj = GameObject.Find("SpaceshipCollectionManager");
        
        if (managerObj == null)
        {
            managerObj = new GameObject("SpaceshipCollectionManager");
            Debug.Log("Created SpaceshipCollectionManager GameObject");
        }
        
        // Add the manager component if not exists
        SpaceshipCollectionManager manager = managerObj.GetComponent<SpaceshipCollectionManager>();
        if (manager == null)
        {
            manager = managerObj.AddComponent<SpaceshipCollectionManager>();
            Debug.Log("Added SpaceshipCollectionManager component");
        }
        
        // Configure for 1 piece (the pipe)
        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("totalPieces").intValue = 1;
        so.FindProperty("showDebugLog").boolValue = true;
        so.ApplyModifiedProperties();
        
        Debug.Log($"<color=green>✓ SpaceshipCollectionManager configured!</color>");
        Debug.Log($"  - Total Pieces: 1");
        Debug.Log($"  - Debug Logs: Enabled");
        
        // Mark scene as dirty
        EditorUtility.SetDirty(managerObj);
    }
}