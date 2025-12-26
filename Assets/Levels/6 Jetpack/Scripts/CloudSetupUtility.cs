using UnityEngine;
using UnityEditor;

namespace JetpackGame
{
    /// <summary>
    /// Editor utility to set up all clouds with VolumetricCloud component and material
    /// </summary>
    public class CloudSetupUtility : MonoBehaviour
    {
        [Header("Setup Settings")]
        public Material cloudMaterial;
        public Transform cloudsParent;
        
        [ContextMenu("Setup All Child Clouds")]
        public void SetupAllClouds()
        {
            if (cloudsParent == null)
            {
                cloudsParent = GameObject.Find("Clouds")?.transform;
            }
            
            if (cloudsParent == null)
            {
                Debug.LogError("Could not find Clouds parent!");
                return;
            }
            
            if (cloudMaterial == null)
            {
                cloudMaterial = Resources.Load<Material>("VolumetricCloudMaterial");
                if (cloudMaterial == null)
                {
                    Debug.LogError("Could not find VolumetricCloudMaterial!");
                    return;
                }
            }
            
            int setupCount = 0;
            
            foreach (Transform child in cloudsParent)
            {
                MeshRenderer mr = child.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    // Apply material
                    mr.sharedMaterial = cloudMaterial;
                    
                    // Add VolumetricCloud if not present
                    VolumetricCloud vc = child.GetComponent<VolumetricCloud>();
                    if (vc == null)
                    {
                        vc = child.gameObject.AddComponent<VolumetricCloud>();
                    }
                    
                    // Set to Default layer so camera can see it
                    child.gameObject.layer = 0;
                    
                    setupCount++;
                    Debug.Log($"Setup cloud: {child.name} at Y={child.position.y:F1}");
                }
            }
            
            Debug.Log($"Setup complete! Configured {setupCount} clouds.");
        }
        
        [ContextMenu("Remove All VolumetricCloud Components")]
        public void RemoveAllCloudComponents()
        {
            if (cloudsParent == null)
            {
                cloudsParent = GameObject.Find("Clouds")?.transform;
            }
            
            if (cloudsParent == null) return;
            
            foreach (Transform child in cloudsParent)
            {
                VolumetricCloud vc = child.GetComponent<VolumetricCloud>();
                if (vc != null)
                {
                    DestroyImmediate(vc);
                }
            }
            
            Debug.Log("Removed all VolumetricCloud components");
        }
    }
}
