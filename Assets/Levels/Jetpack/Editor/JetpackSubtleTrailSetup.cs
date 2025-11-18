using UnityEngine;
using UnityEditor;

/// <summary>
/// Configure Trail Renderer with SUBTLE, NATURAL fog settings
/// Menu: Tools/Jetpack/Configure Subtle Trail
/// </summary>
public class JetpackSubtleTrailSetup : Editor
{
    [MenuItem("Tools/Jetpack/Configure Subtle Trail")]
    static void ConfigureSubtleTrail()
    {
        // Find the XR Origin
        GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
        
        if (xrOrigin == null)
        {
            Debug.LogError("Could not find 'XR Origin (XR Rig)' GameObject!");
            return;
        }
        
        // Get TrailRenderer
        TrailRenderer trail = xrOrigin.GetComponent<TrailRenderer>();
        
        if (trail == null)
        {
            Debug.LogError("No TrailRenderer found on XR Origin!");
            return;
        }
        
        // Load the smoke material (use the new subtle one)
        Material smokeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Jetpack/Materials/JetpackSmoke.mat");
        
        if (smokeMat == null)
        {
            Debug.LogWarning("JetpackSmoke material not found, using JetpackTrail as fallback");
            smokeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Jetpack/Materials/JetpackTrail.mat");
        }
        
        if (smokeMat == null)
        {
            Debug.LogError("Could not find trail material!");
            return;
        }
        
        // Assign material
        trail.material = smokeMat;
        
        // CRITICAL: Shorter lifetime = less intrusive (5 seconds instead of 10)
        trail.time = 5f;
        
        // Width: Start wide, taper smoothly (fog cloud effect)
        trail.widthMultiplier = 1.0f;
        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(0.0f, 1.0f);    // Start: full width
        widthCurve.AddKey(0.2f, 0.95f);   // Early: still wide
        widthCurve.AddKey(0.5f, 0.7f);    // Middle: narrowing
        widthCurve.AddKey(0.8f, 0.4f);    // Late: thin
        widthCurve.AddKey(1.0f, 0.15f);   // End: very thin (soft fade)
        trail.widthCurve = widthCurve;
        
        // Fog cloud dimensions
        trail.startWidth = 1.0f;  // Wider fog cloud
        trail.endWidth = 0.15f;   // Thin at end
        
        // SUBTLE COLOR: Light gray-white, LOW opacity
        Gradient colorGradient = new Gradient();
        
        // Color keys: Soft white to light gray
        GradientColorKey[] colorKeys = new GradientColorKey[3];
        colorKeys[0] = new GradientColorKey(new Color(0.95f, 0.95f, 0.95f), 0.0f);  // Very light gray
        colorKeys[1] = new GradientColorKey(new Color(0.85f, 0.85f, 0.85f), 0.5f);  // Lighter gray
        colorKeys[2] = new GradientColorKey(new Color(0.7f, 0.7f, 0.7f), 1.0f);     // Soft gray
        
        // Alpha keys: VERY SUBTLE (20-30% max, not 80%!)
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[5];
        alphaKeys[0] = new GradientAlphaKey(0.25f, 0.0f);   // Start: 25% visible
        alphaKeys[1] = new GradientAlphaKey(0.30f, 0.2f);   // Early: 30% (peak visibility)
        alphaKeys[2] = new GradientAlphaKey(0.20f, 0.5f);   // Middle: 20%
        alphaKeys[3] = new GradientAlphaKey(0.10f, 0.8f);   // Late: 10%
        alphaKeys[4] = new GradientAlphaKey(0.0f, 1.0f);    // End: fully transparent
        
        colorGradient.SetKeys(colorKeys, alphaKeys);
        trail.colorGradient = colorGradient;
        
        // Quality settings
        trail.numCornerVertices = 8;      // Smoother corners
        trail.numCapVertices = 8;         // Smoother caps
        trail.minVertexDistance = 0.15f;  // Smoother trail
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        
        // Texture mode: Stretch
        trail.textureMode = LineTextureMode.Stretch;
        
        // Alignment: View (faces camera - important for VR)
        trail.alignment = LineAlignment.View;
        
        // Start disabled
        trail.emitting = false;
        
        EditorUtility.SetDirty(xrOrigin);
        
        Debug.Log("<color=green>✓ Subtle Trail configured successfully!</color>");
        Debug.Log("Settings: 5s lifetime, 20-30% opacity, soft gray-white fog, VR-optimized");
        Debug.Log("KEY CHANGE: Much lower opacity for natural, non-intrusive effect!");
    }
}
