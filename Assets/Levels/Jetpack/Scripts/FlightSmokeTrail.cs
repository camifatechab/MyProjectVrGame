using UnityEngine;

public class FlightSmokeTrail : MonoBehaviour
{
    [Header("Trail Settings")]
    [Tooltip("How long the smoke trail lasts (in seconds)")]
    public float trailDuration = 15f;
    
    [Tooltip("Width of the trail at the start")]
    public float trailStartWidth = 8f;
    
    [Tooltip("Width of the trail at the end")]
    public float trailEndWidth = 6f;
    
    [Header("Smoke Appearance")]
    [Tooltip("Color of the smoke trail")]
    public Color smokeColor = new Color(0.9f, 0.9f, 0.95f, 0.6f);
    
    [Tooltip("How smoothly the trail follows corners (higher = smoother)")]
    public int cornerVertices = 15;
    
    [Tooltip("Minimum distance player must move before adding a trail segment")]
    public float minVertexDistance = 0.05f;
    
    [Header("References")]
    [Tooltip("Reference to the AutoJetpackController (auto-found if empty)")]
    public AutoJetpackController jetpackController;
    
    private TrailRenderer trailRenderer;
    private bool isFlying = false;
    
void Start()
    {
        // Auto-find jetpack controller if not assigned
        if (jetpackController == null)
        {
            jetpackController = FindFirstObjectByType<AutoJetpackController>();
            if (jetpackController == null)
            {
                Debug.LogError("FlightSmokeTrail: Could not find AutoJetpackController!");
            }
        }
    }
    

    
    Material CreateSmokeMaterial()
    {
        // Create a soft particle material
        Material mat = new Material(Shader.Find("Particles/Standard Unlit"));
        mat.SetColor("_Color", Color.white);
        
        // Enable transparency
        mat.SetFloat("_Mode", 2); // Fade mode
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        
        // Create a soft circular texture
        Texture2D texture = CreateSoftCircleTexture(128);
        mat.SetTexture("_MainTex", texture);
        
        return mat;
    }
    
    Texture2D CreateSoftCircleTexture(int resolution)
    {
        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
        float maxDist = resolution / 2f;
        
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float normalizedDist = dist / maxDist;
                
                // Soft falloff
                float alpha = 1f - Mathf.Pow(normalizedDist, 2f);
                alpha = Mathf.Clamp01(alpha);
                
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        
        tex.Apply();
        return tex;
    }
    
void Update()
    {
        if (jetpackController == null) return;
        
        // Check if we're currently flying
        bool currentlyFlying = jetpackController.IsFlying();
        
        // Handle trail state changes
        if (currentlyFlying && !isFlying)
        {
            // Just started flying - create a new trail segment
            StartTrail();
        }
        else if (!currentlyFlying && isFlying)
        {
            // Just stopped flying - stop emitting but let trail persist
            StopTrail();
        }
        
        isFlying = currentlyFlying;
    }
    
void StartTrail()
    {
        // Create a new GameObject for this trail segment
        GameObject trailObj = new GameObject("SmokeTrailSegment");
        trailObj.transform.position = transform.position;
        trailObj.transform.SetParent(null); // Independent from player
        
        // Add and configure TrailRenderer
        TrailRenderer trail = trailObj.AddComponent<TrailRenderer>();
        
        // Configure trail settings - wider for cloud-like appearance
        trail.time = trailDuration;
        trail.startWidth = trailStartWidth;
        trail.endWidth = trailEndWidth;
        trail.minVertexDistance = minVertexDistance;
        trail.numCornerVertices = cornerVertices;
        trail.emitting = true;
        
        // Create smoke material
        Material smokeMaterial = CreateSmokeMaterial();
        trail.material = smokeMaterial;
        
        // Configure gradient for fade effect - stays visible longer
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(smokeColor, 0.0f),
                new GradientColorKey(smokeColor, 0.9f)  // Stay solid for 90% of lifetime
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(smokeColor.a, 0.0f),
                new GradientAlphaKey(smokeColor.a, 0.8f), // Stay visible for 80%
                new GradientAlphaKey(0.0f, 1.0f) // Quick fade at the very end
            }
        );
        trail.colorGradient = gradient;
        
        // Store reference to current trail
        trailRenderer = trail;
        
        // Add component to follow player and auto-destroy
        TrailFollower follower = trailObj.AddComponent<TrailFollower>();
        follower.Initialize(transform, trailDuration);
    }
    
void StopTrail()
    {
        if (trailRenderer != null)
        {
            // Stop emitting new trail points
            trailRenderer.emitting = false;
            
            // Let the trail persist and fade naturally
            // The TrailFollower component will destroy it after trailDuration seconds
            trailRenderer = null;
        }
    }
    
    // Public method to manually clear the trail
public void ClearTrail()
    {
        // Find and destroy all active trail segments
        TrailFollower[] allTrails = FindObjectsByType<TrailFollower>(FindObjectsSortMode.None);
        foreach (TrailFollower trail in allTrails)
        {
            Destroy(trail.gameObject);
        }
    }
    
    // Allow adjusting trail duration at runtime
    public void SetTrailDuration(float duration)
    {
        trailDuration = duration;
        if (trailRenderer != null)
        {
            trailRenderer.time = trailDuration;
        }
    }
}