using UnityEngine;

/// <summary>
/// Controls the Iron Man-style vapor trail particle effect during flight
/// Automatically finds the jetpack controller and syncs with flight state
/// </summary>
public class VaporTrailController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Will auto-find if not assigned")]
    [SerializeField] private AutoJetpackController jetpackController;
    
    [Header("Vapor Trail Settings")]
    [Tooltip("How many particles per second (higher = denser trail)")]
    [SerializeField] private float emissionRate = 50f;
    
    [Tooltip("How long particles live (10 seconds for Iron Man effect)")]
    [SerializeField] private float particleLifetime = 10f;
    
    [Tooltip("Starting size of particles")]
    [SerializeField] private float startSize = 2f;
    
    [Tooltip("Ending size of particles (grows as it disperses)")]
    [SerializeField] private float endSize = 5f;
    
    [Tooltip("Color of the vapor trail")]
    [SerializeField] private Color vaporColor = new Color(0.9f, 0.9f, 1f, 0.5f); // Light blue-white
    
    [Tooltip("How fast particles fade out")]
    [SerializeField] private AnimationCurve fadeOverTime = AnimationCurve.Linear(0, 1, 1, 0);
    
    private ParticleSystem vaporParticles;
    
    void Start()
    {
        // Get the particle system on this GameObject
        vaporParticles = GetComponent<ParticleSystem>();
        
        if (vaporParticles == null)
        {
            Debug.LogError("VaporTrailController: No ParticleSystem found! Add a ParticleSystem component.");
            enabled = false;
            return;
        }
        
        // Auto-find jetpack controller if not assigned
        if (jetpackController == null)
        {
            jetpackController = GetComponentInParent<AutoJetpackController>();
            
            if (jetpackController == null)
            {
                Debug.LogError("VaporTrailController: Could not find AutoJetpackController!");
                enabled = false;
                return;
            }
        }
        
        // Configure the particle system for Iron Man vapor trail
        ConfigureParticleSystem();
        
        // Start with particles off
        vaporParticles.Stop();
        
        Debug.Log("<color=cyan>✓ Vapor Trail Controller initialized!</color>");
    }
    
    void ConfigureParticleSystem()
    {
        var main = vaporParticles.main;
        main.startLifetime = particleLifetime;
        main.startSize = new ParticleSystem.MinMaxCurve(startSize);
        main.startSpeed = 0f; // Particles don't move on their own - they follow the player
        main.startColor = vaporColor;
        main.maxParticles = (int)(emissionRate * particleLifetime); // Ensure we have enough particles
        main.simulationSpace = ParticleSystemSimulationSpace.World; // CRITICAL: Trail stays in world space
        main.loop = true;
        main.playOnAwake = false;
        
        // Emission
        var emission = vaporParticles.emission;
        emission.rateOverTime = emissionRate;
        
        // Size over lifetime (grows as it disperses)
        var sizeOverLifetime = vaporParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = AnimationCurve.Linear(0, 1, 1, endSize / startSize);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Color over lifetime (fade out)
        var colorOverLifetime = vaporParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(Color.white, 0f), 
                new GradientColorKey(Color.white, 1f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0.5f, 0f),    // Start at 50% alpha
                new GradientAlphaKey(0.3f, 0.5f),  // Middle at 30%
                new GradientAlphaKey(0f, 1f)       // End completely transparent
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
        
        // Renderer
        var renderer = vaporParticles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateVaporMaterial();
        
        Debug.Log("<color=green>✓ Particle system configured for Iron Man vapor trail!</color>");
    }
    
    Material CreateVaporMaterial()
    {
        // Create a simple additive material for the vapor effect
        Material vaporMat = new Material(Shader.Find("Particles/Standard Unlit"));
        vaporMat.SetColor("_Color", vaporColor);
        vaporMat.EnableKeyword("_ALPHABLEND_ON");
        vaporMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        vaporMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        vaporMat.SetInt("_ZWrite", 0);
        vaporMat.renderQueue = 3000;
        
        return vaporMat;
    }
    
    void Update()
    {
        if (vaporParticles == null || jetpackController == null)
            return;
        
        // Enable particles only when flying
        bool shouldEmit = jetpackController.IsFlying();
        
        if (shouldEmit && !vaporParticles.isPlaying)
        {
            vaporParticles.Play();
            Debug.Log("<color=cyan>★ Vapor Trail ACTIVE ★</color>");
        }
        else if (!shouldEmit && vaporParticles.isPlaying)
        {
            vaporParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Debug.Log("<color=cyan>★ Vapor Trail STOPPED (particles fading...) ★</color>");
        }
    }
}
