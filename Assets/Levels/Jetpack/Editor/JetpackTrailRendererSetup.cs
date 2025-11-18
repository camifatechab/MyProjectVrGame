using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to configure the Trail Renderer on XR Origin
/// Menu: Tools/Jetpack/Configure Trail Renderer
/// </summary>
public class JetpackTrailRendererSetup : Editor
{
    [MenuItem("Tools/Jetpack/Configure Trail Renderer")]
    static void ConfigureTrailRenderer()
    {
        // Find the XR Origin
        GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
        
        if (xrOrigin == null)
        {
            Debug.LogError("Could not find 'XR Origin (XR Rig)' GameObject!");
            return;
        }
        
        // Get or add TrailRenderer
        TrailRenderer trail = xrOrigin.GetComponent<TrailRenderer>();
        
        if (trail == null)
        {
            trail = xrOrigin.AddComponent<TrailRenderer>();
            Debug.Log("TrailRenderer component added.");
        }
        
        // Load the trail material
        Material trailMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Jetpack/Materials/JetpackTrail.mat");
        
        if (trailMat == null)
        {
            Debug.LogError("Could not find JetpackTrail material!");
            return;
        }
        
        // Configure Trail Renderer settings
        trail.material = trailMat;
        
        // Time: 10 seconds (your requirement)
        trail.time = 10f;
        
        // Width: Fog cloud effect (start wide, taper to narrow)
        trail.widthMultiplier = 1.0f;
        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(0.0f, 1.0f);  // Start: full width
        widthCurve.AddKey(0.3f, 0.8f);  // Middle: slightly narrower
        widthCurve.AddKey(1.0f, 0.3f);  // End: thin (fading out)
        trail.widthCurve = widthCurve;
        
        // Start/End Width for fog cloud
        trail.startWidth = 0.8f;  // Wide fog cloud
        trail.endWidth = 0.2f;    // Tapers to thin
        
        // Color gradient: White to transparent
        Gradient colorGradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[3];
        colorKeys[0] = new GradientColorKey(Color.white, 0.0f);    // Start: bright white
        colorKeys[1] = new GradientColorKey(Color.white, 0.5f);    // Middle: still white
        colorKeys[2] = new GradientColorKey(new Color(0.9f, 0.9f, 0.9f), 1.0f); // End: slightly darker
        
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[4];
        alphaKeys[0] = new GradientAlphaKey(0.8f, 0.0f);   // Start: visible
        alphaKeys[1] = new GradientAlphaKey(0.6f, 0.3f);   // Early: still visible
        alphaKeys[2] = new GradientAlphaKey(0.3f, 0.7f);   // Late: fading
        alphaKeys[3] = new GradientAlphaKey(0.0f, 1.0f);   // End: fully transparent
        
        colorGradient.SetKeys(colorKeys, alphaKeys);
        trail.colorGradient = colorGradient;
        
        // Quality settings
        trail.numCornerVertices = 5;      // Smooth corners
        trail.numCapVertices = 5;         // Smooth caps
        trail.minVertexDistance = 0.1f;   // Optimize vertex count
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // No shadows for performance
        trail.receiveShadows = false;
        
        // Texture mode: Stretch (better for trails)
        trail.textureMode = LineTextureMode.Stretch;
        
        // Alignment: View (faces camera)
        trail.alignment = LineAlignment.View;
        
        // Start disabled (we'll enable it when flying)
        trail.emitting = false;
        
        EditorUtility.SetDirty(xrOrigin);
        
        Debug.Log("<color=green>✓ Trail Renderer configured successfully!</color>");
        Debug.Log($"Settings: Time={trail.time}s, Width={trail.startWidth}->{trail.endWidth}, Fog cloud effect");
    }
}
