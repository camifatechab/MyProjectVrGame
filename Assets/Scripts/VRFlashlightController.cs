using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class VRFlashlightController : MonoBehaviour
{
    [Header("Flashlight References")]
    [Tooltip("The flashlight prefab (automatically found if not assigned)")]
    public GameObject flashlightPrefab;
    
    [Header("Light Components")]
    [Tooltip("Spotlight component (automatically found)")]
    public Light spotlight;
    
    [Tooltip("Point light component (automatically found)")]
    public Light pointLight;
    
    [Header("Light Settings")]
    [Tooltip("Intensity for underwater - HIGH value needed (80-100)")]
        public float lightIntensity = 100.0f;
    
    [Tooltip("Spotlight angle - wider for better coverage")]
    public float spotAngle = 90f;
    
    [Tooltip("Light range - longer for deep water")]
    public float lightRange = 60f;
    
    [Header("Toggle Animation GameObjects")]
    [Tooltip("GameObject to show when lights are ON (automatically found)")]
    public GameObject lightsOnObject;
    
    [Tooltip("GameObject to show when lights are OFF (automatically found)")]
    public GameObject lightsOffObject;
    
    [Header("VR Controller Settings")]
    [Tooltip("Use right controller grip button")]
    public XRNode controllerNode = XRNode.RightHand;
    
    
    
    [Header("Haptic Feedback")]
    [Tooltip("Enable haptic feedback on toggle")]
    public bool enableHaptics = true;
    
    [Tooltip("Haptic intensity (0-1)")]
    [Range(0f, 1f)]
    public float hapticIntensity = 0.5f;
    
    [Tooltip("Haptic duration in seconds")]
    public float hapticDuration = 0.1f;
    
    // State
    private bool lightsOn = true;
    private VolumetricLightBeam volumetricBeam;

    private bool buttonWasPressed = false;
    private InputDevice controllerDevice;
    
void Start()
    {
        Debug.Log("VRFlashlightController: Starting initialization...");
        
        // Auto-find components if not assigned - using GetComponentInChildren for more robust search
        if (flashlightPrefab == null)
        {
            // Search in children
            foreach (Transform child in transform)
            {
                if (child.name == "FlashlightPrefab")
                {
                    flashlightPrefab = child.gameObject;
                    Debug.Log("VRFlashlightController: Found FlashlightPrefab at " + flashlightPrefab.transform.position);
                    break;
                }
            }
            
            if (flashlightPrefab == null)
            {
                Debug.LogError("VRFlashlightController: FlashlightPrefab not found as child of " + transform.name);
                return;
            }
        }
        
        // Find light components using GetComponentsInChildren
        if (spotlight == null)
        {
            Light[] lights = flashlightPrefab.GetComponentsInChildren<Light>(true);
            foreach (Light light in lights)
            {
                if (light.type == LightType.Spot)
                {
                    spotlight = light;
                    // Also find volumetric beam component
                    volumetricBeam = light.GetComponent<VolumetricLightBeam>();

                    Debug.Log("VRFlashlightController: Found Spotlight via GetComponentsInChildren");
                    break;
                }
            }
        }
        
        if (pointLight == null)
        {
            Light[] lights = flashlightPrefab.GetComponentsInChildren<Light>(true);
            foreach (Light light in lights)
            {
                if (light.type == LightType.Point)
                {
                    pointLight = light;
                    Debug.Log("VRFlashlightController: Found Point Light via GetComponentsInChildren");
                    break;
                }
            }
        }
        
        // Find animation objects by searching children
        if (lightsOnObject == null)
        {
            Transform lightsOnTransform = flashlightPrefab.transform.Find("FlashlightLightsON");
            if (lightsOnTransform == null)
            {
                // Try searching all children
                foreach (Transform child in flashlightPrefab.transform)
                {
                    if (child.name.Contains("LightsON"))
                    {
                        lightsOnTransform = child;
                        break;
                    }
                }
            }
            
            if (lightsOnTransform != null)
            {
                lightsOnObject = lightsOnTransform.gameObject;
                Debug.Log("VRFlashlightController: Found FlashlightLightsON object");
            }
            else
            {
                Debug.LogError("VRFlashlightController: Could not find FlashlightLightsON child");
            }
        }
        
        if (lightsOffObject == null)
        {
            Transform lightsOffTransform = flashlightPrefab.transform.Find("FlashlightLightsOFF");
            if (lightsOffTransform == null)
            {
                // Try searching all children
                foreach (Transform child in flashlightPrefab.transform)
                {
                    if (child.name.Contains("LightsOFF"))
                    {
                        lightsOffTransform = child;
                        break;
                    }
                }
            }
            
            if (lightsOffTransform != null)
            {
                lightsOffObject = lightsOffTransform.gameObject;
                Debug.Log("VRFlashlightController: Found FlashlightLightsOFF object");
            }
            else
            {
                Debug.LogError("VRFlashlightController: Could not find FlashlightLightsOFF child");
            }
        }
        
        // Get the controller device
        controllerDevice = InputDevices.GetDeviceAtXRNode(controllerNode);
        
        // Apply initial settings
        // ApplyLightSettings(); // Commented out to preserve manual inspector settings
        
        // Start with lights OFF so player must turn on flashlight in dark cave
        SetLightsState(false);
        
        Debug.Log($"VRFlashlightController: Initialized on {controllerNode} controller with lights ON");
        Debug.Log($"References: Spotlight={spotlight != null}, PointLight={pointLight != null}, LightsOn={lightsOnObject != null}, LightsOff={lightsOffObject != null}");
    }
    
void Update()
    {
        // Make sure we have a valid device
        if (!controllerDevice.isValid)
        {
            controllerDevice = InputDevices.GetDeviceAtXRNode(controllerNode);
        }
        
        // Dynamically find lights if they're missing
        if (spotlight == null || pointLight == null)
        {
            FindLightComponents();
        }
        
        // Check for grip button press using InputDevice.TryGetFeatureValue
        bool buttonPressed = false;
        if (controllerDevice.isValid)
        {
            // Try to read the grip button value
            if (controllerDevice.TryGetFeatureValue(CommonUsages.gripButton, out bool gripValue))
            {
                buttonPressed = gripValue;
            }
        }
        
        // Toggle on button press (detect rising edge)
        if (buttonPressed && !buttonWasPressed)
        {
            // Button just pressed - toggle lights
            ToggleLights();
        }
        
        buttonWasPressed = buttonPressed;
    }
    
void FindLightComponents()
    {
        if (flashlightPrefab == null)
        {
            Debug.LogWarning("VRFlashlightController: flashlightPrefab is null, cannot find lights");
            return;
        }
        
        Debug.Log("VRFlashlightController: Searching for light components in " + flashlightPrefab.name);
        
        // Find spotlight
        if (spotlight == null)
        {
            Transform spotlightTransform = flashlightPrefab.transform.Find("FlashlightLightsON/Spotlight");
            if (spotlightTransform != null)
            {
                spotlight = spotlightTransform.GetComponent<Light>();
                Debug.Log("VRFlashlightController: Found Spotlight component");
            }
            else
            {
                Debug.LogError("VRFlashlightController: Could not find FlashlightLightsON/Spotlight child");
            }
        }
        
        // Find point light
        if (pointLight == null)
        {
            Transform pointLightTransform = flashlightPrefab.transform.Find("FlashlightLightsON/Point light");
            if (pointLightTransform != null)
            {
                pointLight = pointLightTransform.GetComponent<Light>();
                Debug.Log("VRFlashlightController: Found Point Light component");
            }
            else
            {
                Debug.LogError("VRFlashlightController: Could not find 'FlashlightLightsON/Point light' child");
            }
        }
    }
    
void FindAnimationObjects()
    {
        if (flashlightPrefab == null)
        {
            Debug.LogWarning("VRFlashlightController: flashlightPrefab is null, cannot find animation objects");
            return;
        }
        
        // Find lights ON object
        if (lightsOnObject == null)
        {
            Transform lightsOnTransform = flashlightPrefab.transform.Find("FlashlightLightsON");
            if (lightsOnTransform != null)
            {
                lightsOnObject = lightsOnTransform.gameObject;
                Debug.Log("VRFlashlightController: Found FlashlightLightsON object");
            }
            else
            {
                Debug.LogError("VRFlashlightController: Could not find FlashlightLightsON child");
            }
        }
        
        // Find lights OFF object  
        if (lightsOffObject == null)
        {
            Transform lightsOffTransform = flashlightPrefab.transform.Find("FlashlightLightsOFF");
            if (lightsOffTransform != null)
            {
                lightsOffObject = lightsOffTransform.gameObject;
                Debug.Log("VRFlashlightController: Found FlashlightLightsOFF object");
            }
            else
            {
                Debug.LogError("VRFlashlightController: Could not find FlashlightLightsOFF child");
            }
        }
    }
    
    void ApplyLightSettings()
    {
        if (spotlight != null)
        {
            spotlight.intensity = lightIntensity;
            spotlight.spotAngle = spotAngle;
            spotlight.range = lightRange;
            Debug.Log($"VRFlashlightController: Spotlight settings applied - Intensity: {lightIntensity}");
        }
        
        if (pointLight != null)
        {
            pointLight.intensity = lightIntensity * 0.5f; // Point light at half intensity
            pointLight.range = lightRange * 0.3f;
            Debug.Log($"VRFlashlightController: Point light settings applied");
        }
    }
    
    void ToggleLights()
    {
        lightsOn = !lightsOn;
        SetLightsState(lightsOn);
        
        // Trigger haptic feedback
        if (enableHaptics && controllerDevice.isValid)
        {
            HapticCapabilities capabilities;
            if (controllerDevice.TryGetHapticCapabilities(out capabilities) && capabilities.supportsImpulse)
            {
                controllerDevice.SendHapticImpulse(0, hapticIntensity, hapticDuration);
            }
        }
        
        Debug.Log($"VRFlashlightController: Lights {(lightsOn ? "ON" : "OFF")}");
    }
    
void SetLightsState(bool on)
    {
        Debug.Log($"VRFlashlightController: SetLightsState called with on={on}");
        
        // Enable/disable lights
        if (spotlight != null)
        {
            spotlight.enabled = on;
            Debug.Log($"VRFlashlightController: Spotlight.enabled = {on}");
        }
        else
        {
            Debug.LogWarning("VRFlashlightController: spotlight is null in SetLightsState");
        }
        
        if (pointLight != null)
        {
            pointLight.enabled = on;
            Debug.Log($"VRFlashlightController: PointLight.enabled = {on}");
        }
        else
        {
            Debug.LogWarning("VRFlashlightController: pointLight is null in SetLightsState");
        }
        
        // Toggle volumetric beam
        if (volumetricBeam != null)
        {
            volumetricBeam.enabled = on;
            Debug.Log($"VRFlashlightController: VolumetricBeam.enabled = {on}");
        }
        
        // Show/hide animation objects
        if (lightsOnObject != null)
        {
            lightsOnObject.SetActive(on);
            Debug.Log($"VRFlashlightController: lightsOnObject.SetActive({on})");
        }
        else
        {
            Debug.LogWarning("VRFlashlightController: lightsOnObject is null in SetLightsState");
        }
        
        if (lightsOffObject != null)
        {
            lightsOffObject.SetActive(!on);
            Debug.Log($"VRFlashlightController: lightsOffObject.SetActive({!on})");
        }
        else
        {
            Debug.LogWarning("VRFlashlightController: lightsOffObject is null in SetLightsState");
        }
    }
}