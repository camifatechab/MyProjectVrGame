using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Lake
{
    public class OxygenUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image oxygenBarFill;
        [SerializeField] private Image oxygenBarBackground;
        [SerializeField] private TextMeshProUGUI oxygenText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI depthText;
        [SerializeField] private Image vignetteImage;

        [Header("Colors")]
        [SerializeField] private Color fullOxygenColor = Color.cyan;
        [SerializeField] private Color mediumOxygenColor = Color.yellow;
        [SerializeField] private Color lowOxygenColor = Color.red;

        [Header("Thresholds")]
        [SerializeField] private float mediumThreshold = 50f;
        [SerializeField] private float lowThreshold = 25f;
        [SerializeField] private float criticalThreshold = 15f;

        [Header("Vignette Settings")]
        [SerializeField] private float vignetteMaxAlpha = 0.6f;
        [SerializeField] private float vignetteStartThreshold = 20f;

        [Header("Animation")]
        [SerializeField] private float fillLerpSpeed = 5f;
        [SerializeField] private float flashSpeed = 4f;

        private OxygenSystem oxygenSystem;
        private float displayedFill;
        private float flashTimer;
        private bool isFlashing;

        private void Start()
        {
            oxygenSystem = FindFirstObjectByType<OxygenSystem>();
            if (oxygenSystem == null)
            {
                Debug.LogError("OxygenUI: No OxygenSystem found in scene!");
                enabled = false;
                return;
            }

            oxygenSystem.OnCriticalOxygenWarning += StartFlashing;
            oxygenSystem.OnOxygenRestored += StopFlashing;
            oxygenSystem.OnSurfaced += StopFlashing;

            displayedFill = 1f;
            UpdateUI();
        }

        private void OnDestroy()
        {
            if (oxygenSystem != null)
            {
                oxygenSystem.OnCriticalOxygenWarning -= StartFlashing;
                oxygenSystem.OnOxygenRestored -= StopFlashing;
                oxygenSystem.OnSurfaced -= StopFlashing;
            }
        }

        private void Update()
        {
            if (oxygenSystem == null) return;

            UpdateFillAmount();
            UpdateBarColor();
            UpdateTexts();
            UpdateVignette();
            UpdateFlashing();
        }

        private void UpdateFillAmount()
        {
            float targetFill = oxygenSystem.OxygenPercent;
            displayedFill = Mathf.Lerp(displayedFill, targetFill, fillLerpSpeed * Time.deltaTime);

            if (oxygenBarFill != null)
            {
                oxygenBarFill.fillAmount = displayedFill;
            }
        }

        private void UpdateBarColor()
        {
            if (oxygenBarFill == null) return;

            float percent = oxygenSystem.OxygenPercent * 100f;
            Color targetColor;

            if (percent <= lowThreshold)
            {
                targetColor = lowOxygenColor;
            }
            else if (percent <= mediumThreshold)
            {
                float t = (percent - lowThreshold) / (mediumThreshold - lowThreshold);
                targetColor = Color.Lerp(lowOxygenColor, mediumOxygenColor, t);
            }
            else
            {
                float t = (percent - mediumThreshold) / (100f - mediumThreshold);
                targetColor = Color.Lerp(mediumOxygenColor, fullOxygenColor, t);
            }

            oxygenBarFill.color = targetColor;
        }

        private void UpdateTexts()
        {
            if (oxygenText != null)
            {
                int percent = Mathf.RoundToInt(oxygenSystem.OxygenPercent * 100f);
                oxygenText.text = $"{percent}%";
            }

            if (statusText != null)
            {
                statusText.text = GetStatusText();
            }

            if (depthText != null)
            {
                if (oxygenSystem.IsUnderwater)
                {
                    float depth = oxygenSystem.CurrentDepth;
                    float multiplier = oxygenSystem.CurrentPressureMultiplier;
                    
                    if (multiplier > 1.1f)
                    {
                        depthText.text = $"Depth: {depth:F1}m ({multiplier:F1}x)";
                        depthText.color = multiplier > 2f ? lowOxygenColor : mediumOxygenColor;
                    }
                    else
                    {
                        depthText.text = $"Depth: {depth:F1}m";
                        depthText.color = Color.white;
                    }
                }
                else
                {
                    depthText.text = "Surface";
                    depthText.color = Color.white;
                }
            }
        }

        private string GetStatusText()
        {
            if (!oxygenSystem.IsUnderwater) return "SURFACE";
            if (oxygenSystem.IsDepleted) return "NO OXYGEN";
            if (oxygenSystem.IsCriticalOxygen) return "CRITICAL";
            if (oxygenSystem.IsLowOxygen) return "LOW O₂";
            return "DIVING";
        }

        private void UpdateVignette()
        {
            if (vignetteImage == null) return;

            float percent = oxygenSystem.OxygenPercent * 100f;
            
            if (percent < vignetteStartThreshold && oxygenSystem.IsUnderwater)
            {
                float t = 1f - (percent / vignetteStartThreshold);
                float alpha = Mathf.Lerp(0f, vignetteMaxAlpha, t);
                vignetteImage.color = new Color(0, 0, 0, alpha);
                vignetteImage.enabled = true;
            }
            else
            {
                vignetteImage.enabled = false;
            }
        }

        private void UpdateFlashing()
        {
            if (!isFlashing || statusText == null) return;

            flashTimer += Time.deltaTime * flashSpeed;
            float alpha = (Mathf.Sin(flashTimer * Mathf.PI * 2f) + 1f) / 2f;
            
            Color c = statusText.color;
            c.a = Mathf.Lerp(0.3f, 1f, alpha);
            statusText.color = c;
        }

        private void StartFlashing()
        {
            isFlashing = true;
            flashTimer = 0f;
        }

        private void StopFlashing()
        {
            isFlashing = false;
            if (statusText != null)
            {
                Color c = statusText.color;
                c.a = 1f;
                statusText.color = c;
            }
        }

        private void UpdateUI()
        {
            UpdateFillAmount();
            UpdateBarColor();
            UpdateTexts();
            UpdateVignette();
        }
    }
}
