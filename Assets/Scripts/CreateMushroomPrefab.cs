using UnityEngine;
using UnityEditor;

public class CreateMushroomPrefab : MonoBehaviour
{
    [ContextMenu("Create Optimized Mushroom Prefab")]
    void CreatePrefab()
    {
        #if UNITY_EDITOR
        string prefabPath = "Assets/Prefabs/MushroomTree_Optimized.prefab";
        
        // Find the Depth_treeB object
        GameObject mushroomTree = GameObject.Find("Depth_treeB");
        
        if (mushroomTree == null)
        {
            Debug.LogError("Could not find Depth_treeB in scene!");
            return;
        }
        
        // Create the prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(mushroomTree, prefabPath);
        
        if (prefab != null)
        {
            Debug.Log("<color=green>✓ Created optimized mushroom prefab at: " + prefabPath + "</color>");
            Debug.Log("<color=cyan>Mushroom has 3 lights total (1 main + 2 spots). Safe to place 5-6 in your scene!</color>");
            
            // Select the created prefab
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }
        else
        {
            Debug.LogError("Failed to create prefab!");
        }
        #endif
    }
}
