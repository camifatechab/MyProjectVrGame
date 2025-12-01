using UnityEngine;
using UnityEditor;

/// <summary>
/// Fixes tags and creates UI for spaceship collection
/// </summary>
public class FixSpaceshipSetup : MonoBehaviour
{
    [MenuItem("Tools/Swimming/Fix Everything Now")]
    static void FixEverything()
    {
        Debug.Log("<color=yellow>===  FIXING SPACESHIP SETUP ===</color>");
        
        // Fix 1: Add Player tag to XR Origin
        GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
        if (xrOrigin != null)
        {
            xrOrigin.tag = "Player";
            Debug.Log("<color=green>✓ XR Origin tagged as 'Player'</color>");
        }
        else
        {
            Debug.LogError("XR Origin (XR Rig) not found!");
        }
        
        // Fix 2: Verify SpaceshipCollectionManager exists
        GameObject managerObj = GameObject.Find("SpaceshipCollectionManager");
        if (managerObj == null)
        {
            Debug.LogError("SpaceshipCollectionManager not found! Run 'Setup Collection Manager' first.");
            return;
        }
        
        Lake.SpaceshipCollectionManager manager = managerObj.GetComponent<Lake.SpaceshipCollectionManager>();
        if (manager == null)
        {
            manager = managerObj.AddComponent<Lake.SpaceshipCollectionManager>();
            Debug.Log("<color=green>✓ Added SpaceshipCollectionManager component</color>");
        }
        
        // Fix 3: Verify Pipe is setup correctly
        GameObject pipe = GameObject.Find("Pipe");
        if (pipe != null)
        {
            Lake.SpaceshipPiece piece = pipe.GetComponent<Lake.SpaceshipPiece>();
            if (piece != null)
            {
                Debug.Log("<color=green>✓ Pipe has SpaceshipPiece component</color>");
            }
            else
            {
                Debug.LogError("Pipe missing SpaceshipPiece component!");
            }
            
            BoxCollider col = pipe.GetComponent<BoxCollider>();
            if (col != null && col.isTrigger)
            {
                Debug.Log("<color=green>✓ Pipe has trigger collider</color>");
            }
            else
            {
                Debug.LogError("Pipe missing trigger collider!");
            }
        }
        
        Debug.Log("<color=yellow>=== BASIC FIXES COMPLETE ===</color>");
        Debug.Log("<color=cyan>Now test: Swim to the pipe and it should collect!</color>");
        
        EditorUtility.SetDirty(xrOrigin);
    }
}