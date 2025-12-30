using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

/// <summary>
/// Centralized haptic feedback manager for VR.
/// Handles crystal collection pulses, jetpack vibration, and landing impacts.
/// </summary>
public class HapticsManager : MonoBehaviour
{
    public static HapticsManager Instance { get; private set; }
    
    [Header("=== CRYSTAL COLLECTION ===")]
    [Range(0f, 1f)]
    public float crystalPulseIntensity = 0.5f;
    public float crystalPulseDuration = 0.15f;
    
    [Header("=== JETPACK VIBRATION ===")]
    [Range(0f, 1f)]
    public float jetpackVibrationIntensity = 0.15f;
    [Tooltip("Subtle continuous vibration while flying")]
    public bool enableJetpackVibration = true;
    
    [Header("=== LANDING IMPACT ===")]
    [Range(0f, 1f)]
    public float softLandingIntensity = 0.3f;
    [Range(0f, 1f)]
    public float hardLandingIntensity = 0.8f;
    public float softLandingDuration = 0.1f;
    public float hardLandingDuration = 0.25f;
    [Tooltip("Velocity threshold for hard landing")]
    public float hardLandingVelocity = 8f;
    
    [Header("=== PLATFORM LANDING ===")]
    [Range(0f, 1f)]
    public float platformLandingIntensity = 0.7f;
    public float platformLandingDuration = 0.3f;
    
    [Header("=== ALL CRYSTALS COLLECTED ===")]
    [Range(0f, 1f)]
    public float victoryIntensity = 0.6f;
    public float victoryDuration = 0.5f;
    public int victoryPulseCount = 3;
    public float victoryPulseInterval = 0.15f;
    
    // XR Input Devices
    private InputDevice leftController;
    private InputDevice rightController;
    
    // State tracking
    private bool isJetpackVibrating = false;
    private float jetpackVibrationTimer = 0f;
    private const float JETPACK_PULSE_INTERVAL = 0.05f;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    private void Start()
    {
        RefreshControllers();
        Debug.Log("<color=green>✓ HapticsManager Ready</color>");
    }
    
    private void Update()
    {
        // Refresh controllers if invalid
        if (!leftController.isValid || !rightController.isValid)
        {
            RefreshControllers();
        }
        
        // Handle continuous jetpack vibration
        if (isJetpackVibrating && enableJetpackVibration)
        {
            jetpackVibrationTimer += Time.deltaTime;
            if (jetpackVibrationTimer >= JETPACK_PULSE_INTERVAL)
            {
                jetpackVibrationTimer = 0f;
                SendHapticPulse(jetpackVibrationIntensity, JETPACK_PULSE_INTERVAL);
            }
        }
    }
    
    private void RefreshControllers()
    {
        var leftDevices = new List<InputDevice>();
        var rightDevices = new List<InputDevice>();
        
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftDevices);
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightDevices);
        
        if (leftDevices.Count > 0) leftController = leftDevices[0];
        if (rightDevices.Count > 0) rightController = rightDevices[0];
    }
    
    #region Public Methods
    
    /// <summary>
    /// Pulse when collecting a crystal
    /// </summary>
    public void PulseCrystalCollect()
    {
        SendHapticPulse(crystalPulseIntensity, crystalPulseDuration);
        Debug.Log("<color=cyan>💎 Haptic: Crystal collected</color>");
    }
    
    /// <summary>
    /// Victory pulse pattern when all crystals collected
    /// </summary>
    public void PulseVictory()
    {
        StartCoroutine(VictoryPulseSequence());
        Debug.Log("<color=yellow>🏆 Haptic: Victory!</color>");
    }
    
    /// <summary>
    /// Start subtle jetpack vibration
    /// </summary>
    public void StartJetpackVibration()
    {
        isJetpackVibrating = true;
        jetpackVibrationTimer = 0f;
        Debug.Log("<color=cyan>🚀 Haptic: Jetpack vibration started</color>");
    }
    
    /// <summary>
    /// Stop jetpack vibration
    /// </summary>
    public void StopJetpackVibration()
    {
        isJetpackVibrating = false;
        StopHaptics();
        Debug.Log("<color=cyan>🚀 Haptic: Jetpack vibration stopped</color>");
    }
    
    /// <summary>
    /// Pulse on landing based on impact velocity
    /// </summary>
    public void PulseLanding(float impactVelocity)
    {
        float absVelocity = Mathf.Abs(impactVelocity);
        
        if (absVelocity >= hardLandingVelocity)
        {
            SendHapticPulse(hardLandingIntensity, hardLandingDuration);
            Debug.Log($"<color=orange>💥 Haptic: Hard landing (velocity: {absVelocity:F1})</color>");
        }
        else if (absVelocity > 2f)
        {
            // Scale intensity based on velocity
            float t = Mathf.InverseLerp(2f, hardLandingVelocity, absVelocity);
            float intensity = Mathf.Lerp(softLandingIntensity, hardLandingIntensity, t);
            float duration = Mathf.Lerp(softLandingDuration, hardLandingDuration, t);
            
            SendHapticPulse(intensity, duration);
            Debug.Log($"<color=yellow>👟 Haptic: Soft landing (velocity: {absVelocity:F1})</color>");
        }
    }
    
    /// <summary>
    /// Strong pulse when landing on a platform
    /// </summary>
    public void PulsePlatformLanding()
    {
        SendHapticPulse(platformLandingIntensity, platformLandingDuration);
        Debug.Log("<color=green>🛬 Haptic: Platform landing</color>");
    }
    
    /// <summary>
    /// Custom pulse with specified intensity and duration
    /// </summary>
    public void Pulse(float intensity, float duration)
    {
        SendHapticPulse(intensity, duration);
    }
    
    /// <summary>
    /// Pulse only left controller
    /// </summary>
    public void PulseLeft(float intensity, float duration)
    {
        SendHapticToDevice(leftController, intensity, duration);
    }
    
    /// <summary>
    /// Pulse only right controller
    /// </summary>
    public void PulseRight(float intensity, float duration)
    {
        SendHapticToDevice(rightController, intensity, duration);
    }
    
    #endregion
    
    #region Private Methods
    
    private void SendHapticPulse(float intensity, float duration)
    {
        SendHapticToDevice(leftController, intensity, duration);
        SendHapticToDevice(rightController, intensity, duration);
    }
    
    private void SendHapticToDevice(InputDevice device, float intensity, float duration)
    {
        if (!device.isValid) return;
        
        HapticCapabilities capabilities;
        if (device.TryGetHapticCapabilities(out capabilities) && capabilities.supportsImpulse)
        {
            device.SendHapticImpulse(0, intensity, duration);
        }
    }
    
    private void StopHaptics()
    {
        if (leftController.isValid)
            leftController.StopHaptics();
        if (rightController.isValid)
            rightController.StopHaptics();
    }
    
    private System.Collections.IEnumerator VictoryPulseSequence()
    {
        for (int i = 0; i < victoryPulseCount; i++)
        {
            SendHapticPulse(victoryIntensity, victoryDuration / victoryPulseCount);
            yield return new WaitForSeconds(victoryPulseInterval);
        }
    }
    
    #endregion
    
    private void OnDestroy()
    {
        StopHaptics();
        if (Instance == this) Instance = null;
    }
}
