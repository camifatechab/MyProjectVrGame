using UnityEngine;

public class MushroomGlowEffect : MonoBehaviour
{
    [Header("References")]
    public Light glowLight;
    public ParticleSystem sporeParticles;
    
    [Header("Gradient Colors")]
    [Tooltip("Bottom color - darker orange/red")]
    public Color bottomColor = new Color(1f, 0.3f, 0.1f, 1f);
    
    [Tooltip("Top color - bright yellow")]
    public Color topColor = new Color(1f, 0.9f, 0.2f, 1f);
    
    [Tooltip("How strong the emission/glow is")]
    [Range(0f, 10f)]
    public float emissionStrength = 4f;
    
    [Header("Gradient Settings")]
    [Range(0f, 1f)]
    public float gradientHeight = 0.4f;
    
    [Range(0.01f, 1f)]
    public float gradientSmoothness = 0.3f;
    
    [Header("Animation")]
    public float pulseSpeed = 0.8f;
    public float pulseAmount = 0.15f;
    
    [Header("Light Settings")]
    public float baseLightIntensity = 7f;
    public Color lightColor = new Color(1f, 0.6f, 0.2f);
    
    private float timeOffset;
    private Material instanceMaterial;
    private MeshRenderer meshRenderer;
    
    void Start()
    {
        // Create random time offset
        timeOffset = Random.Range(0f, 100f);
        
        // Get mesh renderer
        meshRenderer = GetComponent<MeshRenderer>();
        
        // Create material with custom shader
        if (meshRenderer != null)
        {
            Shader gradientShader = Shader.Find("Custom/MushroomGradientGlow");
            
            if (gradientShader != null)
            {
                instanceMaterial = new Material(gradientShader);
                meshRenderer.material = instanceMaterial;
                
                // Set initial shader properties
                UpdateShaderProperties();
            }
            else
            {
                Debug.LogError("MushroomGradientGlow shader not found! Make sure the shader is in the project.");
            }
        }
        
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
        if (instanceMaterial == null) return;
        
        // Pulsing effect
        float pulse = 1f + Mathf.Sin((Time.time + timeOffset) * pulseSpeed) * pulseAmount;
        
        // Update shader with pulse
        instanceMaterial.SetFloat("_EmissionStrength", emissionStrength * pulse);
        
        // Update light
        if (glowLight != null)
        {
            glowLight.intensity = baseLightIntensity * pulse;
        }
    }
    
    void UpdateShaderProperties()
    {
        if (instanceMaterial == null) return;
        
        instanceMaterial.SetColor("_BottomColor", bottomColor);
        instanceMaterial.SetColor("_TopColor", topColor);
        instanceMaterial.SetFloat("_EmissionStrength", emissionStrength);
        instanceMaterial.SetFloat("_GradientHeight", gradientHeight);
        instanceMaterial.SetFloat("_GradientSmoothness", gradientSmoothness);
    }
    
    void OnValidate()
    {
        // Update shader when values change in inspector
        if (Application.isPlaying && instanceMaterial != null)
        {
            UpdateShaderProperties();
        }
    }
    
    void ConfigureParticleSystem()
    {
        var main = sporeParticles.main;
        main.startLifetime = 5f;
        main.startSpeed = 0.3f;
        main.startSize = 0.15f;
        main.startColor = new Color(1f, 0.7f, 0.3f, 0.6f);
        main.maxParticles = 40;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        var shape = sporeParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 2f;
        
        var emission = sporeParticles.emission;
        emission.rateOverTime = 8f;
        
        var velocity = sporeParticles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.y = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        velocity.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
        
        var size = sporeParticles.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.5f),
            new Keyframe(0.3f, 1f),
            new Keyframe(1f, 0.3f)
        );
        size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        var colorOverLifetime = sporeParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(new Color(1f, 0.9f, 0.3f), 0f),
                new GradientColorKey(new Color(1f, 0.5f, 0.2f), 0.5f),
                new GradientColorKey(new Color(1f, 0.3f, 0.1f), 1f)
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.2f),
                new GradientAlphaKey(1f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
        
        var renderer = sporeParticles.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            Material particleMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            particleMat.SetColor("_BaseColor", new Color(1f, 0.7f, 0.3f, 1f));
            renderer.material = particleMat;
        }
    }
    
    void OnDestroy()
    {
        if (instanceMaterial != null)
        {
            Destroy(instanceMaterial);
        }
    }
}