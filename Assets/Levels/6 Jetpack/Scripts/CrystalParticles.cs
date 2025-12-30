using UnityEngine;

/// <summary>
/// Creates a bright, magical glowing orb effect around crystals.
/// Highly visible beacon effect with orbiting sparkles.
/// </summary>
[ExecuteInEditMode]
public class CrystalParticles : MonoBehaviour
{
    [Header("=== MAIN COLOR ===")]
    public Color mainColor = new Color(1f, 0.6f, 0f, 1f); // Bright orange/gold
    
    [Header("=== INNER SPARKLES ===")]
    public int innerCount = 100;
    public float innerRadius = 2f;
    public float innerSize = 0.4f;
    
    [Header("=== OUTER ORBITING SPARKLES ===")]
    public int outerCount = 60;
    public float outerRadius = 5f;
    public float outerSize = 0.5f;
    public float orbitSpeed = 1f;
    
    [Header("=== POINT LIGHT ===")]
    public float lightIntensity = 15f;
    public float lightRange = 50f;
    public float pulseSpeed = 2f;
    public float pulseAmount = 5f;
    
    [Header("=== COLLECTION BURST ===")]
    public int burstCount = 80;
    public float burstSpeed = 8f;
    
    private ParticleSystem innerPS;
    private ParticleSystem outerPS;
    private Light mainLight;
    private float baseIntensity;
    
void Start()
    {
        // Always create fresh particles on Start
        CreateEffects();
    }
    
    void OnEnable()
    {
        // Also create when component is enabled (handles edge cases)
        if (Application.isPlaying && mainLight == null)
        {
            CreateEffects();
        }
    }
    
void CreateEffects()
    {
        // Clean up any existing
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Crystal_"))
                DestroyImmediate(child.gameObject);
        }
        
        // BIGGER and more spread out
        innerCount = 50;
        innerRadius = 2f;
        innerSize = 0.5f;
        
        outerCount = 35;
        outerRadius = 4f;
        outerSize = 0.7f;
        
        lightIntensity = 15f;
        lightRange = 50f;
        
        CreateInnerSparkles();
        CreateOuterSparkles();
        CreateRisingSparkles();
        CreateMainLight();
    }
    
void CreateInnerSparkles()
    {
        GameObject go = new GameObject("Crystal_InnerSparkles");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        
        innerPS = go.AddComponent<ParticleSystem>();
        var main = innerPS.main;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = 2.5f;
        main.startSpeed = 0.5f;
        main.startSize = new ParticleSystem.MinMaxCurve(innerSize * 0.5f, innerSize);
        main.maxParticles = innerCount * 2;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startColor = mainColor;
        
        var emission = innerPS.emission;
        emission.rateOverTime = innerCount / 2f;
        
        var shape = innerPS.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = innerRadius;
        shape.radiusThickness = 0f;
        
        // Use noise instead of velocity for movement
        var noise = innerPS.noise;
        noise.enabled = true;
        noise.strength = 0.5f;
        noise.frequency = 0.5f;
        noise.positionAmount = 1f;
        
        var col = innerPS.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(mainColor, 0), new GradientColorKey(Color.white, 0.5f), new GradientColorKey(mainColor, 1) },
            new GradientAlphaKey[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.15f), new GradientAlphaKey(1, 0.7f), new GradientAlphaKey(0, 1) }
        );
        col.color = g;
        
        var size = innerPS.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0, 0.3f);
        curve.AddKey(0.3f, 1f);
        curve.AddKey(0.7f, 0.8f);
        curve.AddKey(1, 0);
        size.size = new ParticleSystem.MinMaxCurve(1f, curve);
        
        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.material = CreateAdditiveMaterial();
        
        innerPS.Play();
    }
    
void CreateOuterSparkles()
    {
        GameObject go = new GameObject("Crystal_OuterSparkles");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        
        outerPS = go.AddComponent<ParticleSystem>();
        var main = outerPS.main;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = 4f;
        main.startSpeed = 0.3f;
        main.startSize = new ParticleSystem.MinMaxCurve(outerSize * 0.5f, outerSize);
        main.maxParticles = outerCount * 2;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startColor = Color.white;
        
        var emission = outerPS.emission;
        emission.rateOverTime = outerCount / 2f;
        
        var shape = outerPS.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = outerRadius;
        shape.radiusThickness = 0f;
        
        // Use noise for organic movement
        var noise = outerPS.noise;
        noise.enabled = true;
        noise.strength = 0.4f;
        noise.frequency = 0.4f;
        noise.positionAmount = 1f;
        
        var col = outerPS.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0), new GradientColorKey(mainColor, 0.5f), new GradientColorKey(Color.white, 1) },
            new GradientAlphaKey[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.15f), new GradientAlphaKey(1, 0.8f), new GradientAlphaKey(0, 1) }
        );
        col.color = g;
        
        var size = outerPS.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0, 0.3f);
        curve.AddKey(0.25f, 1f);
        curve.AddKey(0.75f, 0.8f);
        curve.AddKey(1, 0);
        size.size = new ParticleSystem.MinMaxCurve(1f, curve);
        
        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.material = CreateAdditiveMaterial();
        
        outerPS.Play();
    }
    
    private ParticleSystem risingPS;
    
void CreateRisingSparkles()
    {
        GameObject go = new GameObject("Crystal_RisingSparkles");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        
        risingPS = go.AddComponent<ParticleSystem>();
        var main = risingPS.main;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = 4f;
        main.startSpeed = 1.5f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1f);
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = mainColor;
        main.gravityModifier = -0.2f;
        
        var emission = risingPS.emission;
        emission.rateOverTime = 10;
        
        var shape = risingPS.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 2f;
        shape.radiusThickness = 0f;
        
        // Use noise for organic floating movement
        var noise = risingPS.noise;
        noise.enabled = true;
        noise.strength = 0.8f;
        noise.frequency = 0.3f;
        noise.positionAmount = 1f;
        
        var col = risingPS.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(mainColor, 0), new GradientColorKey(Color.white, 0.4f), new GradientColorKey(mainColor, 1) },
            new GradientAlphaKey[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.1f), new GradientAlphaKey(0.6f, 0.6f), new GradientAlphaKey(0, 1) }
        );
        col.color = g;
        
        var size = risingPS.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0, 0.5f);
        curve.AddKey(0.2f, 1f);
        curve.AddKey(0.7f, 0.7f);
        curve.AddKey(1, 0);
        size.size = new ParticleSystem.MinMaxCurve(1f, curve);
        
        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.material = CreateAdditiveMaterial();
        
        risingPS.Play();
    }
    
    
void CreateMainLight()
    {
        GameObject go = new GameObject("Crystal_Light");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        
        mainLight = go.AddComponent<Light>();
        mainLight.type = LightType.Point;
        mainLight.color = mainColor;
        mainLight.intensity = lightIntensity;
        mainLight.range = lightRange;
        mainLight.shadows = LightShadows.None;
        
        baseIntensity = lightIntensity;
    }
    
    Material CreateAdditiveMaterial()
    {
        // Use built-in particle additive shader
        Shader shader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Transparent");
        
        Material mat = new Material(shader);
        mat.mainTexture = CreateGlowTexture();
        mat.SetColor("_TintColor", new Color(1, 1, 1, 0.5f));
        
        return mat;
    }
    
    Texture2D CreateGlowTexture()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        
        float center = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float norm = dist / center;
                
                // Bright center, soft falloff
                float alpha = Mathf.Clamp01(1f - norm);
                alpha = Mathf.Pow(alpha, 1.5f); // Soft glow falloff
                
                // White center fading to color
                float brightness = Mathf.Pow(Mathf.Clamp01(1f - norm * 0.7f), 2f);
                
                tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }
        
        tex.Apply();
        return tex;
    }
    
    void Update()
    {
        // Pulse the light
        if (mainLight != null)
        {
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            mainLight.intensity = baseIntensity + pulse;
        }
    }
    
    public void TriggerCollectionBurst()
    {
        if (innerPS != null)
        {
            var emission = innerPS.emission;
            emission.rateOverTime = 0;
            var main = innerPS.main;
            main.startSpeed = burstSpeed;
            main.startLifetime = 1f;
            innerPS.Emit(burstCount);
        }
        
        if (outerPS != null)
        {
            var emission = outerPS.emission;
            emission.rateOverTime = 0;
            var main = outerPS.main;
            main.startSpeed = burstSpeed;
            main.startLifetime = 1f;
            outerPS.Emit(burstCount);
        }
        
        if (mainLight != null)
        {
            StartCoroutine(FlashLight());
        }
        
        Destroy(gameObject, 2f);
    }
    
    System.Collections.IEnumerator FlashLight()
    {
        if (mainLight == null) yield break;
        
        mainLight.intensity = lightIntensity * 3f;
        
        float t = 0;
        while (t < 1f && mainLight != null)
        {
            t += Time.deltaTime * 2f;
            mainLight.intensity = Mathf.Lerp(lightIntensity * 3f, 0, t);
            yield return null;
        }
    }
    
    /// <summary>
    /// Force recreate all particle effects (call if particles are missing)
    /// </summary>
    public void RefreshParticles()
    {
        // Clean up existing
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Crystal_"))
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
        
        innerPS = null;
        outerPS = null;
        risingPS = null;
        mainLight = null;
        
        // Recreate
        CreateEffects();
        Debug.Log($"<color=green>✓ CrystalParticles refreshed on {gameObject.name}</color>");
    }
    
    
public void SetColor(Color color)
    {
        mainColor = color;
        if (mainLight != null) mainLight.color = color;
    }
    
    public void StopParticles()
    {
        if (innerPS != null) innerPS.Stop();
        if (outerPS != null) outerPS.Stop();
    }
    
    void OnValidate()
    {
        if (Application.isPlaying && mainLight != null)
        {
            mainLight.color = mainColor;
            mainLight.intensity = lightIntensity;
            mainLight.range = lightRange;
            baseIntensity = lightIntensity;
        }
    }
}
