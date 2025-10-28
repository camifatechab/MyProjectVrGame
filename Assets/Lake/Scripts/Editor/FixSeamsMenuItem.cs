using UnityEngine;
using UnityEditor;

public class FixSeamsMenuItem
{
    [MenuItem("Tools/Fix Land-Water Seams")]
    public static void FixSeams()
    {
        // Find or create the SeamFixer
        FixLandWaterSeams fixer = Object.FindFirstObjectByType<FixLandWaterSeams>();
        
        if (fixer == null)
        {
            GameObject fixerObj = new GameObject("SeamFixer");
            fixer = fixerObj.AddComponent<FixLandWaterSeams>();
        }
        
        // Run the fix
        fixer.FixSeams();
        
        // Mark scene as dirty so changes are saved
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );
        
        EditorUtility.DisplayDialog("Seam Fix", "Land-Water seams have been fixed successfully!", "OK");
    }
}
