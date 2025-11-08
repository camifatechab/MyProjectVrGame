using UnityEngine;
using UnityEditor;

public class CreateWaterMaterial
{
    [MenuItem("Tools/Create Water Material")]
    public static void Create()
    {
        // Create new material with URP/Lit shader
        Material waterMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        
        // Set up for transparency
        waterMat.SetFloat("_Surface", 1); // 1 = Transparent
        waterMat.SetFloat("_Blend", 0); // 0 = Alpha
        waterMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        waterMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        waterMat.SetFloat("_ZWrite", 0);
        waterMat.renderQueue = 3000;
        
        // Set nice blue color
        waterMat.SetColor("_BaseColor", new Color(0.2f, 0.5f, 0.9f, 0.6f));
        
        // Set smoothness for reflections
        waterMat.SetFloat("_Smoothness", 0.9f);
        
        // Enable keywords for transparency
        waterMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        waterMat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        
        // Save it
        AssetDatabase.CreateAsset(waterMat, "Assets/Lake/Materials/WaterMaterial.mat");
        AssetDatabase.SaveAssets();
        
        Debug.Log("Water Material created at Assets/Lake/Materials/WaterMaterial.mat");
    }
}