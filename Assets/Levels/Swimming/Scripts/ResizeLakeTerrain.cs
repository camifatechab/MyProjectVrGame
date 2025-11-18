using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[ExecuteInEditMode]
public class ResizeLakeTerrain : MonoBehaviour
{
private void OnValidate()
    {
        // Defer the resize to avoid SendMessage errors during OnValidate
        EditorApplication.delayCall += () =>
        {
            if (this != null)
            {
                ResizeTerrain();
            }
        };
    }

    [ContextMenu("Resize to Lake Floor")]
    public void ResizeTerrain()
    {
        Terrain terrain = GetComponent<Terrain>();
        if (terrain != null && terrain.terrainData != null)
        {
            TerrainData data = terrain.terrainData;
            
            // Resize to 50x50 to match lake floor
            data.size = new Vector3(50, 5, 50);
            
            // Position terrain so it aligns with lake floor
            // Lake floor is at Y=-20, terrain origin is corner
            transform.position = new Vector3(-25f, -20f, -25f);
            
            EditorUtility.SetDirty(data);
            EditorUtility.SetDirty(terrain);
            
            Debug.Log("Terrain resized to 50x50 and positioned at lake floor!");
        }
    }
}
#endif
