using UnityEngine;

public class BioluminescentDust : MonoBehaviour
{
    [Header("Dust Settings")]
    [Tooltip("Tiny glowing particles like plankton")]
    public Color dustColor = new Color(0.4f, 0.7f, 0.9f, 0.3f); // Soft cyan glow
    
    [Range(0.02f, 0.2f)]
    public float particleSize = 0.05f;
    
    [Range(10, 100)]
    public int emissionRate = 30;
    
    [Range(2f, 8f)]
    public float sphereRadius = 3f;
    
    [Range(0.1f, 1f)]
    public float driftSpeed = 0.3f;
    
    void Start()
    {
        ConfigureDustParticles();
    }
    
    void ConfigureDustParticles()
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        if (ps == null) return;
        
        // Main module - tiny, slow-moving particles
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startSize = particleSize;
        main.startColor = dustColor;
        main.maxParticles = 200;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.05f; // Very slight upward drift
        
        // Shape - sphere around the mushroom
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = sphereRadius;
        shape.radiusThickness = 0.8f; // Emit from within the sphere volume
        
        // Emission - constant gentle flow
        var emission = ps.emission;
        emission.rateOverTime = emissionRate;
        
        // Velocity over lifetime - gentle random drift
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        
        // Random 3D drift
        AnimationCurve driftCurve = AnimationCurve.Constant(0, 1, driftSpeed);
        velocity.x = new ParticleSystem.MinMaxCurve(driftSpeed, new AnimationCurve(
            new Keyframe(0, Random.Range(-1f, 1f)),
            new Keyframe(0.5f, Random.Range(-1f, 1f)),
            new Keyframe(1, Random.Range(-1f, 1f))
        ));
        velocity.y = new ParticleSystem.MinMaxCurve(driftSpeed * 0.3f, new AnimationCurve(
            new Keyframe(0, Random.Range(-0.5f, 0.5f)),
            new Keyframe(0.5f, Random.Range(-0.5f, 0.5f)),
            new Keyframe(1, Random.Range(-0.5f, 0.5f))
        ));
        velocity.z = new ParticleSystem.MinMaxCurve(driftSpeed, new AnimationCurve(
            new Keyframe(0, Random.Range(-1f, 1f)),
            new Keyframe(0.5f, Random.Range(-1f, 1f)),
            new Keyframe(1, Random.Range(-1f, 1f))
        ));
        
        // Noise module - adds organic random movement
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.3f;
        noise.frequency = 0.5f;
        noise.scrollSpeed = 0.2f;
        noise.damping = false;
        noise.octaveCount = 2;
        
        // Size over lifetime - subtle pulsing
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.5f),
            new Keyframe(0.3f, 1f),
            new Keyframe(0.7f, 0.9f),
            new Keyframe(1f, 0.5f)
        );
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Color over lifetime - gentle fade in and out with slight brightness pulse
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        
        // Create a pulsing glow effect
        Color brightColor = dustColor * 1.5f;
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(dustColor, 0f),
                new GradientColorKey(brightColor, 0.3f),
                new GradientColorKey(dustColor, 0.7f),
                new GradientColorKey(dustColor * 0.7f, 1f)
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0f, 0f),      // Fade in
                new GradientAlphaKey(0.5f, 0.2f),  // Visible
                new GradientAlphaKey(0.6f, 0.5f),  // Peak
                new GradientAlphaKey(0.5f, 0.8f),  // Fade
                new GradientAlphaKey(0f, 1f)       // Fade out
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
        
        // Rotation over lifetime - gentle spin
        var rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-30f, 30f);
        
        // Renderer - glowing particles
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            
            // Create glowing material
            Material particleMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            particleMat.SetColor("_BaseColor", dustColor);
            
            // Enable soft particles for smooth blending
            renderer.material = particleMat;
            renderer.sortingFudge = 0;
        }
    }
}