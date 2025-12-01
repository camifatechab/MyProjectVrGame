using UnityEngine;

public class LeverGlowSetup : MonoBehaviour
{
    private void Start()
    {
        // Simple script to ensure the lever has the glowing material
        var renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            // The material should already be assigned in the prefab
            // This script just makes sure it's working
            if (renderer.sharedMaterial != null)
            {
                // Enable emission keyword for the material to glow
                renderer.sharedMaterial.EnableKeyword("_EMISSION");
            }
        }
    }
}
