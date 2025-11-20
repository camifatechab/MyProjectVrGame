using UnityEngine;

public class VolumetricLightRays : MonoBehaviour
{
    [Header("Light Ray Settings")]
    [Tooltip("Color of the light rays")]
    public Color rayColor = new Color(0.8f, 0.4f, 0.5f, 0.15f); // Soft purple-ish glow
    
    [Range(4, 12)]
    [Tooltip("Number of individual light beams")]
    public int numberOfRays = 6;
    
    [Range(0.1f, 2f)]
    [Tooltip("Width of each light ray")]
    public float rayWidth = 0.3f;
    
    [Range(2f, 10f)]
    [Tooltip("How far the rays extend")]
    public float rayLength = 5f;
    
    [Range(0f, 30f)]
    [Tooltip("How much the rays spread outward (cone angle)")]
    public float spreadAngle = 15f;
    
    [Range(0f, 1f)]
    [Tooltip("How much rays pulse/flicker")]
    public float pulseAmount = 0.3f;
    
    [Range(0.1f, 2f)]
    [Tooltip("Speed of pulsing")]
    public float pulseSpeed = 0.5f;
    
    void Start()
    {
        ConfigureLightRays();
    }
    
    void ConfigureLightRays()
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        if (ps == null) return;
        
        // Main module - long-lasting static rays
        var main = ps.main;
        main.startLifetime = 100f; // Very long lifetime for static rays
        main.startSpeed = 0f; // No movement
        main.startSize3D = true;
        
        // Stretched particles to create ray effect
        main.startSizeX = rayWidth;
        main.startSizeY = rayLength;
        main.startSizeZ = rayWidth;
        
        main.startColor = rayColor;
        main.maxParticles = numberOfRays * 2; // Buffer for extra rays
        main.simulationSpace = ParticleSystemSimulationSpace.Local; // Stay with mushroom
        main.playOnAwake = true;
        main.loop = false; // We'll emit once
        
        // Shape - cone pointing upward for spreading rays
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = spreadAngle;
        shape.radius = 0.1f;
        shape.radiusThickness = 1f;
        shape.position = Vector3.zero;
        
        // Emission - burst of rays at start
        var emission = ps.emission;
        emission.rateOverTime = 0; // No continuous emission
        
        // Create a burst of rays
        ParticleSystem.Burst burst = new ParticleSystem.Burst(0f, numberOfRays);
        emission.SetBurst(0, burst);
        
        // Color over lifetime - pulsing effect
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        
        // Create pulsing gradient
        Gradient gradient = new Gradient();
        Color bright = rayColor * 1.5f;
        Color dim = rayColor * 0.7f;
        
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(bright, 0f),
                new GradientColorKey(dim, 0.25f),
                new GradientColorKey(bright, 0.5f),
                new GradientColorKey(dim, 0.75f),
                new GradientColorKey(bright, 1f)
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.7f, 0.5f),
                new GradientAlphaKey(1f, 1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
        
        // Rotation - rays rotate slowly for variety
        var rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-10f, 10f);
        
        // Renderer - stretched billboards for ray effect
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.cameraVelocityScale = 0f;
            renderer.velocityScale = 0f;
            renderer.lengthScale = 1f;
            
            // Create additive material for light rays
            Material rayMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            rayMat.SetColor("_BaseColor", rayColor);
            
            // Set blend mode to additive for glow
            rayMat.SetFloat("_Surface", 1); // Transparent
            rayMat.SetFloat("_Blend", 1); // Additive
            
            renderer.material = rayMat;
            renderer.sortingFudge = 100; // Render on top
        }
    }
    
    void Update()
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        if (ps == null) return;
        
        // Animate the pulse speed
        var colorOverLifetime = ps.colorOverLifetime;
        if (colorOverLifetime.enabled)
        {
            // The gradient already handles pulsing, just ensure it keeps playing
            float time = Time.time * pulseSpeed;
            
            // Optionally adjust intensity based on time for more variation
            // This could be expanded for more dynamic effects
        }
    }
}