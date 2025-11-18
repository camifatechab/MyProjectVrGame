using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to configure the jetpack trail material
/// Menu: Tools > Jetpack > Configure Trail Material
/// </summary>
public class JetpackTrailMaterialSetup : Editor
{
    [MenuItem("Tools/Jetpack/Configure Trail Material")]
    static void ConfigureTrailMaterial()
    {
        // Load the trail material
        Material trailMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Jetpack/Materials/JetpackTrail.mat");
        
        if (trailMat != null)
        {
            // Configure for transparent white smoke trail
            trailMat.shader = Shader.Find("Particles/Standard Unlit");
            
            // Set to Fade rendering mode (for transparency)
            trailMat.SetFloat("_Mode", 2); // Fade mode
            trailMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            trailMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            trailMat.SetInt("_ZWrite", 0);
            trailMat.DisableKeyword("_ALPHATEST_ON");
            trailMat.EnableKeyword("_ALPHABLEND_ON");
            trailMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            trailMat.renderQueue = 3000;
            
            // Set white color with transparency for smoke effect
            trailMat.SetColor("_Color", new Color(1f, 1f, 1f, 0.5f));
            
            // Enable emission for energy glow
            trailMat.EnableKeyword("_EMISSION");
            trailMat.SetColor("_EmissionColor", new Color(0.4f, 0.4f, 0.4f, 1f));
            
            // Soft particles for better blending
            trailMat.SetFloat("_SoftParticlesNearFadeDistance", 0f);
            trailMat.SetFloat("_SoftParticlesFarFadeDistance", 1f);
            
            // Set flip book mode to simple for trails
            trailMat.SetFloat("_FlipbookMode", 0);
            
            EditorUtility.SetDirty(trailMat);
            AssetDatabase.SaveAssets();
            
            Debug.Log("<color=green>✓ Trail material configured successfully!</color>");
            Debug.Log("Material settings: White smoke with energy glow, transparent, soft edges");
        }
        else
        {
            Debug.LogError("Could not find JetpackTrail.mat at Assets/Jetpack/Materials/JetpackTrail.mat");
        }
    }
}
