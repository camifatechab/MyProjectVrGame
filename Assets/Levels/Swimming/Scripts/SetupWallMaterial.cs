using UnityEngine;

public class SetupWallMaterial : MonoBehaviour
{
private void Start()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material mat = renderer.material;
            
            // Make walls opaque, visible, and LIGHTER
            mat.SetFloat("_Surface", 0); // Opaque
            mat.SetInt("_Cull", 0); // Double-sided
            
            // MUCH LIGHTER stone/concrete color - visible underwater
            Color wallColor = new Color(0.4f, 0.45f, 0.5f, 1f); // Light grey-blue
            mat.SetColor("_BaseColor", wallColor);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0.2f);
            
            // Enable emission for slight glow to be visible in dark water
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", wallColor * 0.3f);
        }
    }
}
