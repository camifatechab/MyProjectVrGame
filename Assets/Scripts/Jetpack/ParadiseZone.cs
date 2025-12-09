using UnityEngine;

/// <summary>
/// Controls the "Paradise Zone" above the clouds.
/// Handles skybox transitions, lighting changes, and particle effects.
/// Works with CloudTransition script.
/// </summary>
public class ParadiseZone : MonoBehaviour
{
    [Header("Skybox Settings")]
    [Tooltip("Skybox to use below clouds")]
    public Material normalSkybox;
    
    [Tooltip("Magical skybox for paradise zone")]
    public Material paradiseSkybox;
    
    [Tooltip("How quickly skybox blend transitions")]
    public float skyboxTransitionSpeed = 1f;
    
    [Header("Lighting Changes")]
    [Tooltip("Ambient light color below clouds")]
    public Color normalAmbientColor = new Color(0.5f, 0.5f, 0.6f);
    
    [Tooltip("Warm ambient light in paradise")]
    public Color paradiseAmbientColor = new Color(1f, 0.9f, 0.7f);
    
    [Tooltip("Ambient intensity in paradise")]
    public float paradiseAmbientIntensity = 1.2f;
    
    [Header("Particle Effects")]
    [Tooltip("Golden sparkle particles in paradise")]
    public ParticleSystem goldenSparkles;
    
    [Tooltip("Parent object for paradise-only effects")]
    public GameObject paradiseEffectsParent;
    
    [Header("Audio")]
    [Tooltip("Ethereal ambient audio for paradise")]
    public AudioSource paradiseAmbience;
    
    [Tooltip("Volume fade speed")]
    public float audioFadeSpeed = 1f;
    
    [Header("References")]
    public CloudTransition cloudTransition;
    
    // State
    private bool isInParadise = false;
    private float currentBlend = 0f;
    private float normalAmbientIntensity;
    private float targetAudioVolume = 0f;
    private float maxAudioVolume = 1f;
    
    private void Start()
    {
        normalAmbientIntensity = RenderSettings.ambientIntensity;
        
        // Auto-find cloud transition
        if (cloudTransition == null)
            cloudTransition = FindObjectOfType<CloudTransition>();
        
        if (cloudTransition != null)
        {
            cloudTransition.OnZoneChanged += OnCloudZoneChanged;
            cloudTransition.OnFirstParadiseEntry += OnFirstParadiseEntry;
        }
        
        // Store initial audio volume
        if (paradiseAmbience != null)
        {
            maxAudioVolume = paradiseAmbience.volume;
            paradiseAmbience.volume = 0f;
        }
        
        // Disable paradise effects initially
        if (paradiseEffectsParent != null)
            paradiseEffectsParent.SetActive(false);
        
        if (goldenSparkles != null)
            goldenSparkles.Stop();
        
        // Store normal skybox
        if (normalSkybox == null)
            normalSkybox = RenderSettings.skybox;
    }
    
    private void OnDestroy()
    {
        if (cloudTransition != null)
        {
            cloudTransition.OnZoneChanged -= OnCloudZoneChanged;
            cloudTransition.OnFirstParadiseEntry -= OnFirstParadiseEntry;
        }
    }
    
    private void Update()
    {
        // Smoothly transition effects
        float targetBlend = isInParadise ? 1f : 0f;
        currentBlend = Mathf.MoveTowards(currentBlend, targetBlend, skyboxTransitionSpeed * Time.deltaTime);
        
        // Update ambient lighting
        RenderSettings.ambientLight = Color.Lerp(normalAmbientColor, paradiseAmbientColor, currentBlend);
        RenderSettings.ambientIntensity = Mathf.Lerp(normalAmbientIntensity, paradiseAmbientIntensity, currentBlend);
        
        // Update skybox if both are assigned and support blending
        if (paradiseSkybox != null && normalSkybox != null)
        {
            // For procedural skyboxes that support _Blend or exposure
            if (RenderSettings.skybox.HasProperty("_Exposure"))
            {
                float exposure = Mathf.Lerp(1f, 1.5f, currentBlend);
                RenderSettings.skybox.SetFloat("_Exposure", exposure);
            }
        }
        
        // Fade audio
        if (paradiseAmbience != null)
        {
            targetAudioVolume = isInParadise ? maxAudioVolume : 0f;
            paradiseAmbience.volume = Mathf.MoveTowards(
                paradiseAmbience.volume, 
                targetAudioVolume, 
                audioFadeSpeed * Time.deltaTime
            );
        }
    }
    
    private void OnCloudZoneChanged(CloudTransition.CloudZone zone)
    {
        bool wasInParadise = isInParadise;
        isInParadise = (zone == CloudTransition.CloudZone.AboveClouds);
        
        if (isInParadise && !wasInParadise)
        {
            EnterParadise();
        }
        else if (!isInParadise && wasInParadise)
        {
            ExitParadise();
        }
    }
    
    private void EnterParadise()
    {
        Debug.Log("ParadiseZone: Entering paradise!");
        
        // Enable effects
        if (paradiseEffectsParent != null)
            paradiseEffectsParent.SetActive(true);
        
        if (goldenSparkles != null)
            goldenSparkles.Play();
        
        // Swap skybox
        if (paradiseSkybox != null)
            RenderSettings.skybox = paradiseSkybox;
        
        // Start ambient audio
        if (paradiseAmbience != null && !paradiseAmbience.isPlaying)
            paradiseAmbience.Play();
    }
    
    private void ExitParadise()
    {
        Debug.Log("ParadiseZone: Leaving paradise");
        
        // Disable effects
        if (paradiseEffectsParent != null)
            paradiseEffectsParent.SetActive(false);
        
        if (goldenSparkles != null)
            goldenSparkles.Stop();
        
        // Restore normal skybox
        if (normalSkybox != null)
            RenderSettings.skybox = normalSkybox;
    }
    
    private void OnFirstParadiseEntry()
    {
        Debug.Log("ParadiseZone: First time in paradise - WOW moment!");
        // Add any special first-time effects here
    }
    
    /// <summary>
    /// Create sparkle particles at runtime if none assigned
    /// </summary>
    [ContextMenu("Create Golden Sparkles")]
    public void CreateGoldenSparkles()
    {
        if (goldenSparkles != null) return;
        
        GameObject sparkleObj = new GameObject("GoldenSparkles");
        sparkleObj.transform.SetParent(transform);
        sparkleObj.transform.localPosition = Vector3.zero;
        
        goldenSparkles = sparkleObj.AddComponent<ParticleSystem>();
        
        var main = goldenSparkles.main;
        main.startColor = new Color(1f, 0.85f, 0.3f, 0.8f);
        main.startSize = 0.1f;
        main.startLifetime = 5f;
        main.startSpeed = 0.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 500;
        
        var emission = goldenSparkles.emission;
        emission.rateOverTime = 50f;
        
        var shape = goldenSparkles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(50f, 10f, 50f);
        
        var colorOverLifetime = goldenSparkles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0f),
                new GradientColorKey(new Color(1f, 0.8f, 0.3f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.2f),
                new GradientAlphaKey(1f, 0.8f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        goldenSparkles.Stop();
        Debug.Log("Created golden sparkle particle system");
    }
}
