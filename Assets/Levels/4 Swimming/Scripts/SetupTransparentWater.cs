using UnityEngine;

public class SetupTransparentWater : MonoBehaviour
{
private void Start()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material mat = renderer.material;
            
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            mat.SetInt("_Cull", 0);
            mat.doubleSidedGI = true;
            
            // Darker blue from underwater view, lighter from above
            Color waterColor = new Color(0.15f, 0.35f, 0.55f, 0.6f); // Darker, more opaque
            mat.SetColor("_BaseColor", waterColor);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0.9f);
            
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        }
    }
}
