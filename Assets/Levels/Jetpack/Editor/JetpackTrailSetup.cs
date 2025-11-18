using UnityEngine;
using UnityEditor;

/// <summary>
/// Comprehensive setup for Jetpack Trail Renderer exhaust effects
/// Creates materials and configures trail renderers for both hand thrusters
/// </summary>
public class JetpackTrailSetup : MonoBehaviour
{
    [MenuItem("GameObject/Jetpack/Setup Trail Renderers", false, 10)]
    static void SetupTrailRenderers()
    {
        // Find the XR Origin
        GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
        if (xrOrigin == null)
        {
            Debug.LogError("Could not find XR Origin (XR Rig)!");
            return;
        }

        // Create or get the trail material
        Material trailMaterial = CreateTrailMaterial();
        if (trailMaterial == null)
        {
            Debug.LogError("Failed to create trail material!");
            return;
        }

        // Find hand thrusters
        Transform cameraOffset = xrOrigin.transform.Find("Camera Offset");
        if (cameraOffset == null)
        {
            Debug.LogError("Could not find Camera Offset!");
            return;
        }

        // Setup left hand thruster
        Transform leftController = cameraOffset.Find("Left Controller");
        Transform leftThruster = leftController?.Find("LeftHandThruster");
        if (leftThruster != null)
        {
            SetupThrusterTrail(leftThruster.gameObject, trailMaterial, "LeftHandTrail");
            Debug.Log("<color=green>✓ Left Hand Trail Renderer configured!</color>");
        }
        else
        {
            Debug.LogWarning("Could not find LeftHandThruster!");
        }

        // Setup right hand thruster
        Transform rightController = cameraOffset.Find("Right Controller");
        Transform rightThruster = rightController?.Find("RightHandThruster");
        if (rightThruster != null)
        {
            SetupThrusterTrail(rightThruster.gameObject, trailMaterial, "RightHandTrail");
            Debug.Log("<color=green>✓ Right Hand Trail Renderer configured!</color>");
        }
        else
        {
            Debug.LogWarning("Could not find RightHandThruster!");
        }

        Debug.Log("<color=cyan>★★★ Trail Renderer Setup Complete! ★★★</color>");
        Debug.Log("<color=yellow>Trail Settings: 10 second lifetime, tapered width, fade gradient</color>");
    }

    static Material CreateTrailMaterial()
    {
        // Check if material already exists
        string materialPath = "Assets/Jetpack/Materials/JetpackTrailMaterial.mat";
        Material existingMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (existingMaterial != null)
        {
            Debug.Log("Using existing JetpackTrailMaterial");
            return existingMaterial;
        }

        // Ensure directory exists
        if (!AssetDatabase.IsValidFolder("Assets/Jetpack"))
        {
            AssetDatabase.CreateFolder("Assets", "Jetpack");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Jetpack/Materials"))
        {
            AssetDatabase.CreateFolder("Assets/Jetpack", "Materials");
        }

        // Create new material with Particles/Standard Unlit shader
        Material material = new Material(Shader.Find("Particles/Standard Unlit"));
        
        // Configure for trail renderer
        material.SetFloat("_Mode", 2); // Fade mode
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;

        // Set color - light blue/cyan with transparency
        material.SetColor("_Color", new Color(0.8f, 0.9f, 1.0f, 0.5f));
        
        // Enable emission for glow
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", new Color(0.3f, 0.5f, 0.8f, 1.0f));

        // Save material
        AssetDatabase.CreateAsset(material, materialPath);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"<color=green>✓ Created JetpackTrailMaterial at {materialPath}</color>");
        return material;
    }

    static void SetupThrusterTrail(GameObject thruster, Material trailMaterial, string trailName)
    {
        // Get or add TrailRenderer
        TrailRenderer trail = thruster.GetComponent<TrailRenderer>();
        if (trail == null)
        {
            trail = thruster.AddComponent<TrailRenderer>();
        }

        // Assign material
        trail.material = trailMaterial;

        // Configure trail settings for 10-second fog effect
        trail.time = 10f; // 10 second lifetime as requested!
        trail.minVertexDistance = 0.1f;
        trail.autodestruct = false;
        trail.emitting = false; // Start disabled, controller will enable it
        
        // Width curve - start wide, taper to thin
        trail.startWidth = 0.5f;
        trail.endWidth = 0.1f;
        
        // Create gradient that fades out over lifetime
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(0.8f, 0.9f, 1.0f), 0.5f),
                new GradientColorKey(new Color(0.6f, 0.7f, 0.9f), 1f)
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(0.5f, 0.5f),
                new GradientAlphaKey(0.0f, 1f)
            }
        );
        trail.colorGradient = gradient;

        // Quality settings
        trail.numCornerVertices = 5;
        trail.numCapVertices = 5;
        trail.alignment = LineAlignment.View; // Always face camera
        trail.textureMode = LineTextureMode.Stretch;

        // Shadow settings
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;

        EditorUtility.SetDirty(thruster);
        Debug.Log($"<color=cyan>Configured trail: {trailName} with 10s lifetime</color>");
    }
}
