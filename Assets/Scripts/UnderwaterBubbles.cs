using UnityEngine;

public class UnderwaterBubbles : MonoBehaviour
{
    [Header("Bubble Settings")]
    [Tooltip("Bubbles rising from the mushroom")]
    public Color bubbleColor = new Color(1f, 1f, 1f, 0.3f); // Semi-transparent white
    
    [Range(0.05f, 0.3f)]
    public float minBubbleSize = 0.08f;
    
    [Range(0.1f, 0.5f)]
    public float maxBubbleSize = 0.2f;
    
    [Range(3, 20)]
    public int emissionRate = 8;
    
    [Range(0.5f, 3f)]
    public float riseSpeed = 1.2f;
    
    [Range(0.5f, 3f)]
    public float emissionRadius = 1.5f;
    
    [Header("Wobble")]
    [Tooltip("How much bubbles wobble as they rise")]
    [Range(0f, 1f)]
    public float wobbleAmount = 0.4f;
    
    void Start()
    {
        ConfigureBubbles();
    }
    
    void ConfigureBubbles()
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        if (ps == null) return;
        
        // Main module - bubbles that rise
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(riseSpeed * 0.5f, riseSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(minBubbleSize, maxBubbleSize);
        main.startColor = bubbleColor;
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.3f; // Bubbles float up
        
        // Shape - emit from mushroom cap area
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = emissionRadius;
        shape.radiusThickness = 0.5f;
        
        // Emission - periodic bursts
        var emission = ps.emission;
        emission.rateOverTime = emissionRate;
        
        // Velocity over lifetime - upward with slight wobble
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.y = new ParticleSystem.MinMaxCurve(riseSpeed, riseSpeed * 1.5f);
        
        // Add horizontal wobble
        AnimationCurve wobbleX = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.25f, wobbleAmount),
            new Keyframe(0.5f, 0f),
            new Keyframe(0.75f, -wobbleAmount),
            new Keyframe(1f, 0f)
        );
        AnimationCurve wobbleZ = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.3f, -wobbleAmount),
            new Keyframe(0.6f, wobbleAmount),
            new Keyframe(1f, 0f)
        );
        velocity.x = new ParticleSystem.MinMaxCurve(1f, wobbleX);
        velocity.z = new ParticleSystem.MinMaxCurve(1f, wobbleZ);
        
        // Noise - adds natural movement variation
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.2f;
        noise.frequency = 0.8f;
        noise.scrollSpeed = 0.3f;
        noise.damping = true;
        
        // Size over lifetime - bubbles expand slightly as they rise
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.7f),
            new Keyframe(0.5f, 1f),
            new Keyframe(0.9f, 1.2f),
            new Keyframe(1f, 0f) // Pop at the end
        );
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Color over lifetime - fade in/out
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 0.5f),
                new GradientColorKey(new Color(0.9f, 0.95f, 1f), 1f) // Slight blue tint
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0f, 0f),      // Fade in
                new GradientAlphaKey(0.4f, 0.1f),  // Visible
                new GradientAlphaKey(0.4f, 0.8f),  // Stay visible
                new GradientAlphaKey(0f, 1f)       // Pop/fade out
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
        
        // Rotation over lifetime - gentle spin
        var rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-45f, 45f);
        
        // Renderer - transparent spheres
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            
            // Create bubble material
            Material bubbleMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            bubbleMat.SetColor("_BaseColor", bubbleColor);
            
            // Try to make it look like a bubble
            renderer.material = bubbleMat;
            renderer.sortingFudge = 0;
        }
    }
    
    // Optional: Add burst emissions periodically
    void Update()
    {
        // You could add occasional bubble bursts here if desired
    }
}