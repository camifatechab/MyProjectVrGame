using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Lake
{
    /// <summary>
    /// Displays the oxygen level as a visual bar in the UI
    /// </summary>
    public class OxygenUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The OxygenSystem to monitor")]
        [SerializeField] private OxygenSystem oxygenSystem;
        
        [Header("UI Elements")]
        [Tooltip("Image component for the oxygen bar fill")]
        [SerializeField] private Image fillImage;
        
        [Tooltip("Optional background panel")]
        [SerializeField] private Image backgroundPanel;
        
        [Tooltip("Optional text showing percentage")]
        [SerializeField] private TextMeshProUGUI percentageText;
        
        [Header("Visual Settings")]
        [Tooltip("Color when oxygen is high (above 50%)")]
        [SerializeField] private Color highOxygenColor = new Color(0.2f, 0.8f, 1f); // Cyan
        
        [Tooltip("Color when oxygen is medium (25-50%)")]
        [SerializeField] private Color mediumOxygenColor = new Color(1f, 0.9f, 0.2f); // Yellow
        
        [Tooltip("Color when oxygen is low (below 25%)")]
        [SerializeField] private Color lowOxygenColor = new Color(1f, 0.2f, 0.2f); // Red
        
        [Tooltip("Should the UI be hidden when not underwater?")]
        [SerializeField] private bool hideWhenNotUnderwater = true;
        
        [Header("Low Oxygen Warning")]
        [Tooltip("Enable pulsing effect when oxygen is low")]
        [SerializeField] private bool enablePulseWarning = true;
        
        [Tooltip("Pulse speed when oxygen is low")]
        [SerializeField] private float pulseSpeed = 3f;
        
        private CanvasGroup canvasGroup;
        private bool isLowOxygen = false;
        private float pulseTimer = 0f;
        
        private void Awake()
        {
            // Get or add CanvasGroup for fading
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            // Try to find OxygenSystem if not assigned
            if (oxygenSystem == null)
            {
                oxygenSystem = FindObjectOfType<OxygenSystem>();
                if (oxygenSystem == null)
                {
                    Debug.LogError("OxygenUI: No OxygenSystem found in scene!");
                }
            }
        }
        
        private void OnEnable()
        {
            if (oxygenSystem != null)
            {
                // Subscribe to oxygen events
                oxygenSystem.OnOxygenChanged.AddListener(UpdateOxygenBar);
                oxygenSystem.OnOxygenLow.AddListener(OnOxygenLow);
            }
        }
        
        private void OnDisable()
        {
            if (oxygenSystem != null)
            {
                // Unsubscribe from events
                oxygenSystem.OnOxygenChanged.RemoveListener(UpdateOxygenBar);
                oxygenSystem.OnOxygenLow.RemoveListener(OnOxygenLow);
            }
        }
        
        private void Start()
        {
            // Initialize UI
            if (oxygenSystem != null)
            {
                UpdateOxygenBar(oxygenSystem.OxygenPercentage);
            }
        }
        
        private void Update()
        {
            if (oxygenSystem == null) return;
            
            // Handle visibility based on underwater state
            if (hideWhenNotUnderwater)
            {
                float targetAlpha = oxygenSystem.IsUnderwater ? 1f : 0f;
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * 5f);
            }
            
            // Handle low oxygen pulsing effect
            if (isLowOxygen && enablePulseWarning && fillImage != null)
            {
                pulseTimer += Time.deltaTime * pulseSpeed;
                float pulse = (Mathf.Sin(pulseTimer) + 1f) * 0.5f; // 0 to 1
                float alphaPulse = Mathf.Lerp(0.6f, 1f, pulse);
                
                Color currentColor = fillImage.color;
                currentColor.a = alphaPulse;
                fillImage.color = currentColor;
            }
        }
        
        /// <summary>
        /// Updates the oxygen bar visual (0-1 range)
        /// </summary>
        private void UpdateOxygenBar(float percentage)
        {
            percentage = Mathf.Clamp01(percentage);
            
            // Update fill amount
            if (fillImage != null)
            {
                fillImage.fillAmount = percentage;
                
                // Update color based on oxygen level
                Color targetColor = GetColorForPercentage(percentage);
                fillImage.color = targetColor;
            }
            
            // Update percentage text
            if (percentageText != null)
            {
                percentageText.text = Mathf.RoundToInt(percentage * 100f) + "%";
            }
            
            // Update low oxygen state
            isLowOxygen = percentage <= 0.25f;
        }
        
        /// <summary>
        /// Returns appropriate color based on oxygen percentage
        /// </summary>
        private Color GetColorForPercentage(float percentage)
        {
            if (percentage > 0.5f)
            {
                // High oxygen - cyan
                return highOxygenColor;
            }
            else if (percentage > 0.25f)
            {
                // Medium oxygen - blend from cyan to yellow
                float t = (percentage - 0.25f) / 0.25f; // 0 at 25%, 1 at 50%
                return Color.Lerp(mediumOxygenColor, highOxygenColor, t);
            }
            else
            {
                // Low oxygen - blend from yellow to red
                float t = percentage / 0.25f; // 0 at 0%, 1 at 25%
                return Color.Lerp(lowOxygenColor, mediumOxygenColor, t);
            }
        }
        
        /// <summary>
        /// Called when oxygen drops below 25%
        /// </summary>
        private void OnOxygenLow()
        {
            isLowOxygen = true;
            pulseTimer = 0f;
            
            // Optional: Play warning sound here
            Debug.Log("OxygenUI: Low oxygen warning!");
        }
        
        /// <summary>
        /// Public method to manually show/hide the UI
        /// </summary>
        public void SetVisible(bool visible)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
        }
    }
}