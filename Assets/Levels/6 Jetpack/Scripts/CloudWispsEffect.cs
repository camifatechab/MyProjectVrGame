using UnityEngine;

/// <summary>
/// Creates wispy fog/mist particles when player flies through clouds.
/// Uses distance-based detection instead of colliders for better VR compatibility.
/// Attach to a manager object - will find clouds automatically.
/// </summary>
public class CloudWispsEffect : MonoBehaviour
{
    [Header("=== DETECTION ===")]
    public float detectionRadius = 25f;
    public Transform cloudsParent;
    
    [Header("=== WISP PARTICLES ===")]
    public Color wispColor = new Color(1f, 1f, 1f, 0.5f);
    public int burstCount = 50;
    public float wispSize = 3f;
    public float wispLifetime = 2.5f;
    public float spawnRate = 20f;
    public float spawnRadius = 4f;
    
    [Header("=== SKYBOX TRANSITION ===")]
    [Tooltip("Height at which to trigger cloud burst (matches SkyboxByHeight threshold)")]
    public float transitionHeight = 15f;
    public int transitionBurstCount = 100;
    public float transitionBurstDuration = 3f;

    
    private ParticleSystem wispParticles;
    private Transform playerCamera;
    private Transform[] cloudTransforms;
    private bool[] insideCloud;
    private bool isInsideAnyCloud = false;
    private bool wasAboveThreshold = false;
    private float transitionBurstTimer = 0f;

    
    void Start()
    {
        FindPlayer();
        FindClouds();
        CreateWispParticles();
        
        Debug.Log($"<color=cyan>☁️ CloudWispsEffect: Tracking {cloudTransforms.Length} clouds</color>");
    }
    
    void FindPlayer()
    {
        // Try XR Origin first
        var xrOrigin = FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin != null && xrOrigin.Camera != null)
        {
            playerCamera = xrOrigin.Camera.transform;
            Debug.Log("CloudWispsEffect: Found XR camera");
            return;
        }
        
        // Fallback to main camera
        if (Camera.main != null)
        {
            playerCamera = Camera.main.transform;
            Debug.Log("CloudWispsEffect: Using main camera");
        }
    }
    
void FindClouds()
    {
        if (cloudsParent == null)
        {
            // Try multiple ways to find clouds
            GameObject cloudsObj = GameObject.Find("Clouds");
            if (cloudsObj != null)
            {
                cloudsParent = cloudsObj.transform;
                Debug.Log($"<color=cyan>☁️ Found Clouds parent: {cloudsObj.name}</color>");
            }
        }
        
        if (cloudsParent != null && cloudsParent.childCount > 0)
        {
            cloudTransforms = new Transform[cloudsParent.childCount];
            insideCloud = new bool[cloudsParent.childCount];
            
            for (int i = 0; i < cloudsParent.childCount; i++)
            {
                cloudTransforms[i] = cloudsParent.GetChild(i);
                insideCloud[i] = false;
                Debug.Log($"<color=cyan>☁️ Tracking cloud: {cloudTransforms[i].name} at {cloudTransforms[i].position}</color>");
            }
        }
        else
        {
            // Fallback: find all objects with "cloud" in their name
            var allObjects = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            System.Collections.Generic.List<Transform> clouds = new System.Collections.Generic.List<Transform>();
            
            foreach (var t in allObjects)
            {
                if (t.name.ToLower().Contains("cloud") && t.GetComponent<MeshRenderer>() != null)
                {
                    clouds.Add(t);
                }
            }
            
            cloudTransforms = clouds.ToArray();
            insideCloud = new bool[cloudTransforms.Length];
            Debug.Log($"<color=yellow>☁️ Fallback: Found {cloudTransforms.Length} cloud objects by name</color>");
        }
    }
    
void CreateWispParticles()
    {
        GameObject go = new GameObject("CloudWisps_Particles");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        
        wispParticles = go.AddComponent<ParticleSystem>();
        wispParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        // Main module
        var main = wispParticles.main;
        main.loop = true;
        main.prewarm = false;
        main.startLifetime = wispLifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f);
        main.startSize = new ParticleSystem.MinMaxCurve(wispSize * 0.5f, wispSize * 1.5f);
        main.maxParticles = 300;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = wispColor;
        main.gravityModifier = 0f;
        
        // Emission - controlled manually
        var emission = wispParticles.emission;
        emission.rateOverTime = 0;
        
        // Shape - sphere around player
        var shape = wispParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = spawnRadius;
        shape.radiusThickness = 0f; // Surface only for spread
        
        // Noise for organic wispy movement (instead of velocity over lifetime)
        var noise = wispParticles.noise;
        noise.enabled = true;
        noise.strength = 3f;
        noise.frequency = 0.3f;
        noise.scrollSpeed = 0.5f;
        noise.damping = true;
        noise.positionAmount = 1f;
        
        // Size over lifetime
        var size = wispParticles.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.2f);
        sizeCurve.AddKey(0.15f, 1f);
        sizeCurve.AddKey(0.7f, 1.1f);
        sizeCurve.AddKey(1f, 0f);
        size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Color over lifetime - fade in/out
        var colorOverLifetime = wispParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(Color.white, 0f), 
                new GradientColorKey(Color.white, 1f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.6f, 0.1f),
                new GradientAlphaKey(0.5f, 0.6f),
                new GradientAlphaKey(0f, 1f) 
            }
        );
        colorOverLifetime.color = gradient;
        
        // Renderer
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateWispMaterial();
    }
    
Material CreateWispMaterial()
    {
        // Use existing JetpackTrail material asset — correct URP shader, no lookup needed
        Material mat = null;
#if UNITY_EDITOR
        mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Levels/6 Jetpack/Materials/JetpackTrail.mat");
#endif
        if (mat != null) return mat;

        // Runtime fallback
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                     ?? Shader.Find("Sprites/Default");
        Material fallback = new Material(shader);
        fallback.SetTexture("_BaseMap", CreateSoftCircleTexture());
        fallback.SetTexture("_MainTex",  CreateSoftCircleTexture());
        fallback.SetColor("_BaseColor", wispColor);
        fallback.SetColor("_Color",     wispColor);
        fallback.SetFloat("_Surface", 1f);
        fallback.SetFloat("_ZWrite", 0f);
        fallback.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        fallback.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        fallback.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        fallback.renderQueue = 3000;
        return fallback;
    }
    
    Texture2D CreateSoftCircleTexture()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        
        float center = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float norm = dist / center;
                
                // Soft falloff
                float alpha = Mathf.Clamp01(1f - norm);
                alpha = Mathf.Pow(alpha, 1.5f);
                
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        
        tex.Apply();
        return tex;
    }
    
void Update()
    {
        if (playerCamera == null) return;
        
        Vector3 playerPos = playerCamera.position;
        
        // === CHECK SKYBOX TRANSITION HEIGHT ===
        CheckTransitionHeight(playerPos.y);
        
        // === CHECK CLOUDS ===
        if (cloudTransforms == null) return;
        
        bool currentlyInside = false;
        
        // Check distance to each cloud
        for (int i = 0; i < cloudTransforms.Length; i++)
        {
            if (cloudTransforms[i] == null) continue;
            
            float dist = Vector3.Distance(playerPos, cloudTransforms[i].position);
            bool wasInside = insideCloud[i];
            bool nowInside = dist < detectionRadius;
            
            if (nowInside && !wasInside)
            {
                OnEnterCloud(cloudTransforms[i]);
            }
            else if (!nowInside && wasInside)
            {
                OnExitCloud(cloudTransforms[i]);
            }
            
            insideCloud[i] = nowInside;
            
            if (nowInside)
            {
                currentlyInside = true;
            }
        }
        
        // Update particle emission for clouds
        if (currentlyInside != isInsideAnyCloud)
        {
            isInsideAnyCloud = currentlyInside;
            
            var emission = wispParticles.emission;
            if (isInsideAnyCloud)
            {
                emission.rateOverTime = spawnRate;
                wispParticles.Play();
            }
            else if (transitionBurstTimer <= 0) // Don't stop if in transition burst
            {
                emission.rateOverTime = 0;
            }
        }
        
        // Handle transition burst timer
        if (transitionBurstTimer > 0)
        {
            transitionBurstTimer -= Time.deltaTime;
            if (transitionBurstTimer <= 0 && !isInsideAnyCloud)
            {
                var emission = wispParticles.emission;
                emission.rateOverTime = 0;
            }
        }
        
        // Keep particles at player position while active
        if (isInsideAnyCloud || transitionBurstTimer > 0)
        {
            wispParticles.transform.position = playerPos;
        }
    }
    
    void CheckTransitionHeight(float playerHeight)
    {
        bool isAbove = playerHeight >= transitionHeight;
        
        if (isAbove != wasAboveThreshold)
        {
            // Crossed the threshold!
            TriggerTransitionBurst(isAbove);
            wasAboveThreshold = isAbove;
        }
    }
    
    void TriggerTransitionBurst(bool goingUp)
    {
        if (wispParticles == null) return;
        
        wispParticles.transform.position = playerCamera.position;
        
        // Big burst of particles
        wispParticles.Emit(transitionBurstCount);
        
        // Start continuous emission for duration
        var emission = wispParticles.emission;
        emission.rateOverTime = spawnRate * 2; // Double rate during transition
        wispParticles.Play();
        
        transitionBurstTimer = transitionBurstDuration;
        
        string direction = goingUp ? "UP through clouds" : "DOWN through clouds";
        Debug.Log($"<color=yellow>☁️ SKYBOX TRANSITION: {direction} at height {transitionHeight}m</color>");
    }
    
    void OnEnterCloud(Transform cloud)
    {
        // Burst of particles on entry
        wispParticles.transform.position = playerCamera.position;
        wispParticles.Emit(burstCount);
        
        Debug.Log($"<color=cyan>☁️ Entered cloud: {cloud.name}</color>");
    }
    
    void OnExitCloud(Transform cloud)
    {
        // Small burst on exit
        wispParticles.Emit(burstCount / 3);
        
        Debug.Log($"<color=cyan>☁️ Exited cloud: {cloud.name}</color>");
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.2f);
        
        if (cloudsParent != null)
        {
            foreach (Transform child in cloudsParent)
            {
                Gizmos.DrawWireSphere(child.position, detectionRadius);
            }
        }
    }
}
