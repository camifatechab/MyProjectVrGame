using UnityEngine;

public class AlienMushroomEffect : MonoBehaviour
{
    [Header("References")]
    public Light glowLight;
    public ParticleSystem sporeParticles;
    
    [Header("Light Settings")]
    [Tooltip("Color of the glow light - match the mushroom colors")]
    public Color lightColor = new Color(0.6f, 0.3f, 0.4f, 1f); // Purple-ish to match alien theme
    public float baseLightIntensity = 4f;
    public float pulseSpeed = 0.8f;
    public float pulseAmount = 0.2f;
    
    [Header("Particle Color")]
    [Tooltip("Color of the floating particles")]
    public Color particleColor = new Color(0.5f, 0.2f, 0.3f, 0.6f); // Dark purple
    
    private float timeOffset;
    
    void Start()
    {
        timeOffset = Random.Range(0f, 100f);
        
        // Auto-find light
        if (glowLight == null)
        {
            glowLight = GetComponentInChildren<Light>();
        }
        
        if (glowLight != null)
        {
            glowLight.color = lightColor;
            glowLight.intensity = baseLightIntensity;
        }
        
        // Auto-find particle system
        if (sporeParticles == null)
        {
            sporeParticles = GetComponentInChildren<ParticleSystem>();
        }
        
        if (sporeParticles != null)
        {
            ConfigureParticleSystem();
        }
    }
    
    void Update()
    {
        // Gentle pulsing light
        if (glowLight != null)
        {
            float pulse = 1f + Mathf.Sin((Time.time + timeOffset) * pulseSpeed) * pulseAmount;
            glowLight.intensity = baseLightIntensity * pulse;
        }
    }
    
    void ConfigureParticleSystem()
    {
        var main = sporeParticles.main;
        main.startLifetime = 5f;
        main.startSpeed = 0.3f;
        main.startSize = 0.12f;
        main.startColor = particleColor;
        main.maxParticles = 30;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        // Shape - emit from the mushroom cap
        var shape = sporeParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1.5f;
        
        // Emission
        var emission = sporeParticles.emission;
        emission.rateOverTime = 5f;
        
        // Velocity - float upward slowly
        var velocity = sporeParticles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.y = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
        velocity.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
        
        // Size over lifetime
        var size = sporeParticles.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.3f),
            new Keyframe(0.3f, 1f),
            new Keyframe(1f, 0.2f)
        );
        size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Color over lifetime - fade to darker
        var colorOverLifetime = sporeParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(particleColor, 0f),
                new GradientColorKey(particleColor * 0.7f, 1f)
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.8f, 0.2f),
                new GradientAlphaKey(0.8f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
        
        // Renderer
        var renderer = sporeParticles.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            Material particleMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            particleMat.SetColor("_BaseColor", particleColor);
            renderer.material = particleMat;
        }
    }
}