using UnityEngine;

public class MushroomGlowingSpots : MonoBehaviour
{
    [Header("Glowing Spots Settings")]
    [Tooltip("Number of glowing spots around the mushroom cap")]
    [Range(3, 12)]
    public int numberOfSpots = 6;
    
    [Tooltip("Color of the glowing spots")]
    public Color spotColor = new Color(0.6f, 0.2f, 0.3f, 1f);
    
    [Tooltip("Intensity of each spot light")]
    [Range(0.5f, 5f)]
    public float spotIntensity = 2f;
    
    [Tooltip("Range of each spot light")]
    [Range(0.5f, 3f)]
    public float spotRange = 1.5f;
    
    [Tooltip("Radius where spots are placed around cap")]
    [Range(0.5f, 5f)]
    public float placementRadius = 2f;
    
    [Tooltip("Height offset for spots (Y position)")]
    [Range(-2f, 5f)]
    public float heightOffset = 2.5f;
    
    [Header("Animation")]
    [Tooltip("Should spots pulse/flicker")]
    public bool enablePulsing = true;
    
    [Range(0.1f, 2f)]
    public float pulseSpeed = 0.8f;
    
    [Range(0f, 0.5f)]
    public float pulseAmount = 0.3f;
    
    private GameObject spotsContainer;
    private Light[] spotLights;
    private float[] timeOffsets;
    
        void Start()
    {
        Debug.Log($"MushroomGlowingSpots: Starting on {gameObject.name} with {numberOfSpots} spots");
        CreateGlowingSpots();
    }
    
    void CreateGlowingSpots()
    {
        // Create container for organization
        spotsContainer = new GameObject("GlowingSpots");
        spotsContainer.transform.SetParent(transform);
        Debug.Log("Creating GlowingSpots container");
        spotsContainer.transform.localPosition = Vector3.zero;
        spotsContainer.transform.localRotation = Quaternion.identity;
        
        spotLights = new Light[numberOfSpots];
        timeOffsets = new float[numberOfSpots];
        
        // Create spots in a circle around the mushroom
        for (int i = 0; i < numberOfSpots; i++)
        {
            Debug.Log($"Creating glow spot {i} of {numberOfSpots}");
            // Calculate position in circle
            float angle = (360f / numberOfSpots) * i;
            float radians = angle * Mathf.Deg2Rad;
            
            Vector3 position = new Vector3(
                Mathf.Cos(radians) * placementRadius,
                heightOffset,
                Mathf.Sin(radians) * placementRadius
            );
            
            // Create spot light
            GameObject spotObj = new GameObject($"GlowSpot_{i}");
            spotObj.transform.SetParent(spotsContainer.transform);
            spotObj.transform.localPosition = position;
            
            Light spotLight = spotObj.AddComponent<Light>();
            spotLight.type = LightType.Point;
            spotLight.color = spotColor;
            spotLight.intensity = spotIntensity;
            spotLight.range = spotRange;
            spotLight.shadows = LightShadows.None; // No shadows for performance
            
            spotLights[i] = spotLight;
            timeOffsets[i] = Random.Range(0f, 100f); // Random offset for varied pulsing
        }
    }
    
    void Update()
    {
        if (!enablePulsing || spotLights == null) return;
        
        // Animate each spot with slight variations
        for (int i = 0; i < spotLights.Length; i++)
        {
            if (spotLights[i] == null) continue;
            
            float pulse = 1f + Mathf.Sin((Time.time + timeOffsets[i]) * pulseSpeed) * pulseAmount;
            spotLights[i].intensity = spotIntensity * pulse;
        }
    }
    
    void OnDestroy()
    {
        // Clean up spots container
        if (spotsContainer != null)
        {
            Destroy(spotsContainer);
        }
    }
    
    // Visualize spot positions in editor
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            Gizmos.color = spotColor;
            
            for (int i = 0; i < numberOfSpots; i++)
            {
                float angle = (360f / numberOfSpots) * i;
                float radians = angle * Mathf.Deg2Rad;
                
                Vector3 position = transform.position + new Vector3(
                    Mathf.Cos(radians) * placementRadius,
                    heightOffset,
                    Mathf.Sin(radians) * placementRadius
                );
                
                Gizmos.DrawWireSphere(position, spotRange * 0.2f);
            }
        }
    }
}