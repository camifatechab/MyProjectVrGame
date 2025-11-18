using UnityEngine;

public class PulsingLight : MonoBehaviour
{
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float minIntensity = 5f;
    [SerializeField] private float maxIntensity = 15f;
    [SerializeField] private float range = 20f;
    [SerializeField] private Color lightColor = new Color(0f, 1f, 1f); // Cyan
    
    private Light pointLight;
    
    private void Start()
    {
        pointLight = GetComponent<Light>();
        if (pointLight != null)
        {
            pointLight.type = LightType.Point;
            pointLight.color = lightColor;
            pointLight.range = range;
        }
    }
    
private void Update()
    {
        if (pointLight != null)
        {
            // Calculate pulsing intensity
            float pulseIntensity = Mathf.Lerp(minIntensity, maxIntensity, 
                (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
            
            // Add twinkling flicker
            float twinkle = Mathf.PerlinNoise(Time.time * pulseSpeed * 3f, 1f) * 3f;
            
            pointLight.intensity = pulseIntensity + twinkle;
        }
    }
}
