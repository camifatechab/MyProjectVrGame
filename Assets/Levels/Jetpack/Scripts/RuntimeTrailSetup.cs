using UnityEngine;

/// <summary>
/// Runtime setup for Jetpack Trail Renderers
/// Attach this to the XR Origin and it will automatically configure trail renderers on play
/// </summary>
public class RuntimeTrailSetup : MonoBehaviour
{
    [Header("Trail Settings")]
    [SerializeField] private float trailTime = 10f;
    [SerializeField] private float startWidth = 0.5f;
    [SerializeField] private float endWidth = 0.1f;
    [SerializeField] private Color startColor = new Color(0.8f, 0.9f, 1.0f, 0.8f);
    [SerializeField] private Color endColor = new Color(0.6f, 0.7f, 0.9f, 0.0f);

    void Start()
    {
        SetupTrailRenderers();
    }

    void SetupTrailRenderers()
    {
        // Find hand thrusters
        Transform cameraOffset = transform.Find("Camera Offset");
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
            ConfigureThrusterTrail(leftThruster.gameObject, "LeftHandTrail");
        }

        // Setup right hand thruster  
        Transform rightController = cameraOffset.Find("Right Controller");
        Transform rightThruster = rightController?.Find("RightHandThruster");
        if (rightThruster != null)
        {
            ConfigureThrusterTrail(rightThruster.gameObject, "RightHandTrail");
        }

        Debug.Log("<color=cyan>★★★ Trail Renderers Configured at Runtime! ★★★</color>");
    }

    void ConfigureThrusterTrail(GameObject thruster, string trailName)
    {
        // Get or add TrailRenderer
        TrailRenderer trail = thruster.GetComponent<TrailRenderer>();
        if (trail == null)
        {
            trail = thruster.AddComponent<TrailRenderer>();
            Debug.Log($"<color=green>✓ Added TrailRenderer to {trailName}</color>");
        }

        // Create material
        Material trailMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
        trailMaterial.SetColor("_Color", startColor);
        trailMaterial.SetFloat("_Mode", 2); // Fade
        trailMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        trailMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        trailMaterial.SetInt("_ZWrite", 0);
        trailMaterial.renderQueue = 3000;
        trailMaterial.EnableKeyword("_ALPHABLEND_ON");

        trail.material = trailMaterial;

        // Configure trail settings for 10-second fog effect
        trail.time = trailTime;
        trail.minVertexDistance = 0.1f;
        trail.autodestruct = false;
        trail.emitting = false; // Start disabled
        
        // Width curve - start wide, taper to thin
        trail.startWidth = startWidth;
        trail.endWidth = endWidth;
        
        // Create gradient that fades out
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
        trail.alignment = LineAlignment.View;
        trail.textureMode = LineTextureMode.Stretch;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;

        Debug.Log($"<color=cyan>✓ Configured {trailName} - {trailTime}s lifetime</color>");
    }
}
