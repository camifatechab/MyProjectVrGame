using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to configure the jetpack smoke particle material
/// Menu: Tools/Jetpack/Configure Smoke Material
/// </summary>
public class JetpackSmokeMaterialSetup : Editor
{
    [MenuItem("Tools/Jetpack/Configure Smoke Material")]
    static void ConfigureSmokeMaterial()
    {
        // Load the smoke material
        Material smokeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Jetpack/Materials/JetpackSmoke.mat");
        
        if (smokeMat != null)
        {
            // Use Mobile/Particles/Additive shader (soft, performant)
            smokeMat.shader = Shader.Find("Mobile/Particles/Additive");
            
            // Set rendering mode for soft blending
            smokeMat.renderQueue = 3000;
            
            // Very subtle white color with low alpha
            smokeMat.SetColor("_TintColor", new Color(1f, 1f, 1f, 0.15f)); // 15% opacity for subtle effect
            
            // Soft particle factor for smooth edges
            if (smokeMat.HasProperty("_InvFade"))
            {
                smokeMat.SetFloat("_InvFade", 2.0f);
            }
            
            EditorUtility.SetDirty(smokeMat);
            AssetDatabase.SaveAssets();
            
            Debug.Log("<color=green>✓ Smoke material configured successfully!</color>");
            Debug.Log("Settings: Additive blend, 15% opacity, soft edges, VR-optimized");
        }
        else
        {
            Debug.LogError("Could not find JetpackSmoke.mat at Assets/Jetpack/Materials/JetpackSmoke.mat");
        }
    }
}
