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
    [Tooltip("Enable dynamic emission based on flight speed")]
    [SerializeField] private bool speedBasedEmission = true;
    
    [Tooltip("Minimum emission rate at slow speeds")]
    [SerializeField] private float minEmissionRate = 20f;
    
    [Tooltip("Maximum emission rate at max speed")]
    [SerializeField] private float maxEmissionRate = 100f;
    

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
    private float currentEmissionRate;
    private Rigidbody playerRigidbody;

    
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
        
        // Try to find Rigidbody for speed detection
        playerRigidbody = GetComponentInParent<Rigidbody>();
        
        // Initialize emission rate
        currentEmissionRate = emissionRate;
        
        // Configure the particle system for Iron Man vapor trail
        ConfigureParticleSystem();
        
        // Start with particles off
        vaporParticles.Stop();
        
        Debug.Log("<color=cyan>✓ Vapor Trail Controller initialized with speed-based emission!</color>");
    }
    
    void ConfigureParticleSystem()
    {
        var main = vaporParticles.main;
        main.startLifetime = particleLifetime;
        // Size variation - wider range for more organic, varied appearance
        main.startSize = new ParticleSystem.MinMaxCurve(startSize * 0.5f, startSize * 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f); // UPWARD initial velocity for visibility!
        main.startColor = vaporColor;
        // Random starting rotation - each particle starts at different angle
        main.startRotation = new ParticleSystem.MinMaxCurve(-180f * Mathf.Deg2Rad, 180f * Mathf.Deg2Rad);

        main.maxParticles = (int)(emissionRate * particleLifetime);
        main.simulationSpace = ParticleSystemSimulationSpace.World; // CRITICAL: Trail stays in world space
        main.loop = true;
        main.playOnAwake = false;
        
        // Rotation over lifetime - subtle spin for organic feel
        var rotationOverLifetime = vaporParticles.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-30f * Mathf.Deg2Rad, 30f * Mathf.Deg2Rad); // Subtle random spin (-30 to +30 degrees/sec)

        
        // Emission
        var emission = vaporParticles.emission;
        emission.rateOverTime = emissionRate;
        
        // Velocity over lifetime - particles slow down and drift
        var velocityOverLifetime = vaporParticles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
        // Slow upward drift
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.5f, 2f);
        // Slight horizontal spread
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
        
        // TURBULENCE - Makes fog swirl realistically!
        var noise = vaporParticles.noise;
        noise.enabled = true;
        noise.strength = 2f; // Moderate swirl
        noise.frequency = 0.3f; // Smooth, organic movement
        noise.scrollSpeed = 0.5f; // Slow rolling motion
        noise.damping = true;
        noise.octaveCount = 2; // Balanced detail
        noise.quality = ParticleSystemNoiseQuality.Medium;
        
        // Size over lifetime (grows as it disperses) with variation
        var sizeOverLifetime = vaporParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        // Create two curves - particles will randomly pick growth rates between these
        AnimationCurve minSizeCurve = AnimationCurve.Linear(0, 1, 1, (endSize / startSize) * 0.8f);
        AnimationCurve maxSizeCurve = AnimationCurve.Linear(0, 1, 1, (endSize / startSize) * 1.2f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, minSizeCurve, maxSizeCurve);
        
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
    }
    
    void UpdateEmissionRate()
    {
        // Calculate speed from character controller or rigidbody
        float speed = 0f;
        
        CharacterController cc = GetComponentInParent<CharacterController>();
        if (cc != null)
        {
            speed = cc.velocity.magnitude;
        }
        else if (playerRigidbody != null)
        {
            speed = playerRigidbody.linearVelocity.magnitude;
        }
        
        // Map speed to emission rate (0-15 m/s typical flight speed range)
        float speedPercent = Mathf.Clamp01(speed / 15f);
        float targetEmissionRate = Mathf.Lerp(minEmissionRate, maxEmissionRate, speedPercent);
        
        // Smooth transition
        currentEmissionRate = Mathf.Lerp(currentEmissionRate, targetEmissionRate, Time.deltaTime * 3f);
        
        // Update particle system
        var emission = vaporParticles.emission;
        emission.rateOverTime = currentEmissionRate;
        
        // Debug every 60 frames
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"<color=yellow>Speed: {speed:F1} m/s | Emission: {currentEmissionRate:F0} particles/s</color>");
        }
    }
    
Material CreateVaporMaterial()
    {
        // Use Unity's built-in soft particle shader for smooth, round particles
        Material vaporMat = new Material(Shader.Find("Particles/Standard Unlit"));
        
        // Create a soft circular gradient texture procedurally
        Texture2D softCircle = CreateSoftCircleTexture(128);
        vaporMat.SetTexture("_MainTex", softCircle);
        
        // Set color
        vaporMat.SetColor("_Color", vaporColor);
        
        // Enable transparency
        vaporMat.EnableKeyword("_ALPHABLEND_ON");
        vaporMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        vaporMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        vaporMat.SetInt("_ZWrite", 0);
        vaporMat.renderQueue = 3000;
        
        // Enable soft particles (fades near geometry)
        vaporMat.EnableKeyword("SOFTPARTICLES_ON");
        
        return vaporMat;
    }
    
    Texture2D CreateSoftCircleTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);
                
                // Soft radial gradient (smooth falloff)
                float alpha = 1f - Mathf.Clamp01(distance / radius);
                alpha = Mathf.Pow(alpha, 2f); // Smoother falloff curve
                
                // White color with gradient alpha
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        
        texture.Apply();
        return texture;
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
        
        // Update emission based on speed
        if (shouldEmit && speedBasedEmission)
        {
            UpdateEmissionRate();
        }
    }
}
