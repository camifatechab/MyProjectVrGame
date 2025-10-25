using UnityEngine;

public class ApplyWaterMaterialRuntime : MonoBehaviour
{
    [SerializeField] private Material waterMaterial;
    
    private void Awake()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null && waterMaterial != null)
        {
            renderer.sharedMaterial = waterMaterial;
        }
    }
}
