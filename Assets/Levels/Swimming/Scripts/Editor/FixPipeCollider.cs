using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to properly configure the Pipe collider
/// </summary>
public class FixPipeCollider : MonoBehaviour
{
    [MenuItem("Tools/Fix Pipe Collider")]
    static void FixCollider()
    {
        // Find the Pipe GameObject
        GameObject pipe = GameObject.Find("Pipe");
        
        if (pipe == null)
        {
            Debug.LogError("Pipe GameObject not found in scene!");
            return;
        }
        
        // Remove SphereCollider if exists
        SphereCollider sphereCollider = pipe.GetComponent<SphereCollider>();
        if (sphereCollider != null)
        {
            DestroyImmediate(sphereCollider);
            Debug.Log("Removed SphereCollider from Pipe");
        }
        
        // Add BoxCollider if not exists
        BoxCollider boxCollider = pipe.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = pipe.AddComponent<BoxCollider>();
            Debug.Log("Added BoxCollider to Pipe");
        }
        
        // Configure the box collider to encompass the entire pipe
        // Based on children positions: extends from 0 to ~3.18 in local X
        boxCollider.isTrigger = true;
        boxCollider.center = new Vector3(1.6f, 0f, 0f); // Center along the pipe length
        boxCollider.size = new Vector3(3.5f, 1.5f, 1.5f); // Length x Height x Depth
        
        Debug.Log($"<color=green>✓ Pipe collider configured!</color>");
        Debug.Log($"  - Type: BoxCollider (Trigger)");
        Debug.Log($"  - Center: {boxCollider.center}");
        Debug.Log($"  - Size: {boxCollider.size}");
        
        // Mark scene as dirty
        EditorUtility.SetDirty(pipe);
    }
}