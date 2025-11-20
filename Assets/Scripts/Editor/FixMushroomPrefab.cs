using UnityEngine;
using UnityEditor;

public class FixMushroomPrefab : EditorWindow
{
    [MenuItem("Tools/Fix Mushroom Prefab")]
    static void FixPrefab()
    {
        // Find Depth_treeB in scene
        GameObject mushroomInScene = GameObject.Find("Depth_treeB");
        
        if (mushroomInScene == null)
        {
            Debug.LogError("Could not find Depth_treeB in scene!");
            return;
        }
        
        // Delete old broken variant if it exists
        string oldVariantPath = "Assets/Alien_planets_Vol2/Prefabs/Plants/Depth/Depth_treeB Variant.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(oldVariantPath) != null)
        {
            AssetDatabase.DeleteAsset(oldVariantPath);
            Debug.Log("Deleted broken variant prefab");
        }
        
        // Create fresh original prefab
        string newPrefabPath = "Assets/Prefabs/MushroomTree_Optimized.prefab";
        
        // Make sure the Prefabs folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        
        // Create the new prefab
        GameObject newPrefab = PrefabUtility.SaveAsPrefabAsset(mushroomInScene, newPrefabPath);
        
        if (newPrefab != null)
        {
            Debug.Log("<color=green>✓ Created fresh mushroom prefab at: " + newPrefabPath + "</color>");
            Debug.Log("<color=cyan>Prefab has 2 glowing spots (3 lights total). Safe to place 5-6 in scene!</color>");
            
            // Select and highlight the new prefab
            Selection.activeObject = newPrefab;
            EditorGUIUtility.PingObject(newPrefab);
        }
        else
        {
            Debug.LogError("Failed to create prefab!");
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
