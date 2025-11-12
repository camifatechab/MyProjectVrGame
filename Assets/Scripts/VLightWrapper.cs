using UnityEngine;

/// <summary>
/// Wrapper for VLight that prevents the property sheet errors
/// Add this to your Spotlight instead of using VLight directly
/// </summary>
[RequireComponent(typeof(Light))]
public class VLightWrapper : MonoBehaviour
{
    [Header("VLight Settings")]
    [SerializeField] private Light spotLight;
    [SerializeField] private float volumetricIntensity = 50f;
    [SerializeField] private float rangeMultiplier = 1.0f;
    [SerializeField] private Color volumetricColor = Color.white;
    
    private Component vlightComponent;
    private bool vlightFound = false;
    
    void Start()
    {
        if (spotLight == null)
        {
            spotLight = GetComponent<Light>();
        }
        
        // Try to get VLight component
        vlightComponent = GetComponent("VLight");
        
        if (vlightComponent != null)
        {
            vlightFound = true;
            ConfigureVLight();
            Debug.Log("<color=cyan>✓ VLightWrapper: VLight component found and configured</color>");
        }
        else
        {
            Debug.LogWarning("VLightWrapper: VLight component not found. Please add VLight component from V-Light package.");
        }
    }
    
    void ConfigureVLight()
    {
        if (vlightComponent == null) return;
        
        // Configure VLight via reflection to avoid property sheet errors
        System.Type vlightType = vlightComponent.GetType();
        
        // Set intensity
        var intensityField = vlightType.GetField("intensity", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (intensityField != null)
        {
            intensityField.SetValue(vlightComponent, volumetricIntensity);
        }
        
        // Set range multiplier
        var rangeField = vlightType.GetField("rangeMultiplier",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (rangeField != null)
        {
            rangeField.SetValue(vlightComponent, rangeMultiplier);
        }
        
        // Set color tint
        var colorField = vlightType.GetField("colorTint",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (colorField != null)
        {
            colorField.SetValue(vlightComponent, volumetricColor);
        }
        
        // Set spotlight reference
        var spotlightField = vlightType.GetField("spotlight",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (spotlightField != null)
        {
            spotlightField.SetValue(vlightComponent, spotLight);
        }
    }
    
    void OnValidate()
    {
        if (Application.isPlaying && vlightFound)
        {
            ConfigureVLight();
        }
    }
    
    void OnDisable()
    {
        // Disable VLight when this component is disabled
        if (vlightComponent != null)
        {
            var enabledProperty = vlightComponent.GetType().GetProperty("enabled");
            if (enabledProperty != null)
            {
                enabledProperty.SetValue(vlightComponent, false);
            }
        }
    }
    
    void OnEnable()
    {
        // Enable VLight when this component is enabled
        if (vlightComponent != null)
        {
            var enabledProperty = vlightComponent.GetType().GetProperty("enabled");
            if (enabledProperty != null)
            {
                enabledProperty.SetValue(vlightComponent, true);
            }
        }
    }
}
