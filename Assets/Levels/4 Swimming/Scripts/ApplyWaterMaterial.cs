using UnityEngine;

public class ApplyWaterMaterial : MonoBehaviour
{
    private void Start()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            // Load the transparent water material
            Material waterMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Lake/TransparentWaterMaterial.mat");
            if (waterMat != null)
            {
                renderer.sharedMaterial = waterMat;
            }
        }
    }
}
