using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays a celebration UI when all crystals are collected.
/// Uses singleton pattern so CrystalManager can trigger it directly.
/// Follows the same style as JetpackFuelUI and VRCrystalPanel.
/// </summary>
public class CrystalCompleteUI : MonoBehaviour
{
    // Singleton for easy access from CrystalManager
    public static CrystalCompleteUI Instance { get; private set; }

    [Header("UI References (Auto-Found)")]
    [Tooltip("The completion panel that appears when all crystals collected")]
    [SerializeField] private GameObject completionPanel;
    
    [Tooltip("Main completion text (e.g., 'ALL CRYSTALS COLLECTED!')")]
    [SerializeField] private TextMeshProUGUI completionText;
    
    [Tooltip("Sub text for additional info (e.g., 'Returning to camp...')")]
    [SerializeField] private TextMeshProUGUI subText;
    
    [Tooltip("Optional background image for glow effect")]
    [SerializeField] private Image panelBackground;

    [Header("Animation Settings")]
    [Tooltip("How fast the panel scales in")]
    [SerializeField] private float scaleInSpeed = 4f;
    
    [Tooltip("How fast colors pulse")]
    [SerializeField] private float pulseSpeed = 2f;
    
    [Tooltip("Scale overshoot for bounce effect (1.0 = no overshoot)")]
    [SerializeField] private float scaleOvershoot = 1.15f;

    [Header("Color Settings")]
    [SerializeField] private Color completionColor = new Color(0.3f, 1f, 0.5f); // Bright green
    [SerializeField] private Color glowColor = new Color(1f, 0.95f, 0.4f); // Golden
    [SerializeField] private Color subTextColor = new Color(0.8f, 0.9f, 1f); // Light blue

    [Header("Text Content")]
    [SerializeField] private string completionMessage = "ALL CRYSTALS COLLECTED!";
    [SerializeField] private string subMessage = "Returning to camp...";

    // Internal state
    private bool hasTriggered = false;
    private bool isAnimating = false;
    private float animationProgress = 0f;
    private Vector3 targetScale = Vector3.one;
    private float pulseTimer = 0f;

    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple CrystalCompleteUI found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        // Auto-find child references
        AutoFindReferences();

        // Store target scale
        if (completionPanel != null)
        {
            targetScale = completionPanel.transform.localScale;
        }
    }

    void Start()
    {
        // Hide panel initially
        if (completionPanel != null)
        {
            completionPanel.SetActive(false);
        }

        // Set initial text content
        if (completionText != null)
            completionText.text = completionMessage;
        if (subText != null)
            subText.text = subMessage;

        Debug.Log("<color=green>✓ CrystalCompleteUI initialized and ready!</color>");
    }

    void AutoFindReferences()
    {
        // Find CompletionPanel child if not assigned
        if (completionPanel == null)
        {
            Transform panelTransform = transform.Find("CompletionPanel");
            if (panelTransform != null)
            {
                completionPanel = panelTransform.gameObject;
                panelBackground = panelTransform.GetComponent<Image>();
            }
        }

        // Find text components inside completion panel
        if (completionPanel != null)
        {
            if (completionText == null)
            {
                Transform textTransform = completionPanel.transform.Find("CompletionText");
                if (textTransform != null)
                    completionText = textTransform.GetComponent<TextMeshProUGUI>();
            }

            if (subText == null)
            {
                Transform subTransform = completionPanel.transform.Find("SubText");
                if (subTransform != null)
                    subText = subTransform.GetComponent<TextMeshProUGUI>();
            }

            if (panelBackground == null)
            {
                panelBackground = completionPanel.GetComponent<Image>();
            }
        }

        // Log what was found
        if (completionPanel != null)
            Debug.Log("<color=cyan>✓ Found CompletionPanel</color>");
        if (completionText != null)
            Debug.Log("<color=cyan>✓ Found CompletionText</color>");
        if (subText != null)
            Debug.Log("<color=cyan>✓ Found SubText</color>");
    }

    void Update()
    {
        // Handle animation
        if (isAnimating)
        {
            UpdateScaleAnimation();
        }

        // Handle pulse effect after animation complete
        if (hasTriggered && !isAnimating)
        {
            UpdatePulseEffect();
        }
    }

    /// <summary>
    /// Called by CrystalManager when all crystals are collected.
    /// Triggers the completion UI to appear with animation.
    /// </summary>
    public void ShowCompletion()
    {
        if (hasTriggered) return;

        hasTriggered = true;
        isAnimating = true;
        animationProgress = 0f;

        Debug.Log("<color=cyan>★ CrystalCompleteUI: ALL CRYSTALS COLLECTED!</color>");

        // Show and prepare panel for animation
        if (completionPanel != null)
        {
            completionPanel.SetActive(true);
            completionPanel.transform.localScale = Vector3.zero;
        }

        // Set initial colors
        if (completionText != null)
            completionText.color = completionColor;
        if (subText != null)
            subText.color = subTextColor;
        if (panelBackground != null)
            panelBackground.color = new Color(0f, 0f, 0f, 0.85f);
    }

    /// <summary>
    /// Animates the panel scaling in with bounce effect
    /// </summary>
    void UpdateScaleAnimation()
    {
        animationProgress += Time.deltaTime * scaleInSpeed;

        if (completionPanel != null)
        {
            float t = animationProgress;

            // Elastic ease-out for bounce effect
            float scale;
            if (t < 1f)
            {
                // Overshoot phase
                scale = Mathf.Lerp(0f, scaleOvershoot, EaseOutBack(t));
            }
            else
            {
                // Settle phase
                float settleT = (t - 1f) * 2f;
                scale = Mathf.Lerp(scaleOvershoot, 1f, Mathf.Clamp01(settleT));
            }

            completionPanel.transform.localScale = targetScale * scale;

            // Animation complete
            if (t >= 1.5f)
            {
                isAnimating = false;
                completionPanel.transform.localScale = targetScale;
                Debug.Log("<color=green>✓ CrystalCompleteUI: Animation complete!</color>");
            }
        }
    }

    /// <summary>
    /// Creates a pulsing glow effect on the completion text
    /// </summary>
    void UpdatePulseEffect()
    {
        pulseTimer += Time.deltaTime * pulseSpeed;

        float pulse = (Mathf.Sin(pulseTimer * Mathf.PI) + 1f) / 2f; // 0 to 1

        // Pulse text color between completion and glow
        if (completionText != null)
        {
            completionText.color = Color.Lerp(completionColor, glowColor, pulse * 0.5f);
        }

        // Subtle background pulse
        if (panelBackground != null)
        {
            float alpha = Mathf.Lerp(0.75f, 0.9f, pulse * 0.3f);
            Color bgColor = panelBackground.color;
            bgColor.a = alpha;
            panelBackground.color = bgColor;
        }
    }

    /// <summary>
    /// Ease out back function for bounce effect
    /// </summary>
    float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    /// <summary>
    /// Hide the completion UI (called before scene transition)
    /// </summary>
    public void Hide()
    {
        if (completionPanel != null)
        {
            completionPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Reset UI state (for testing/reloading)
    /// </summary>
    public void ResetUI()
    {
        hasTriggered = false;
        isAnimating = false;
        animationProgress = 0f;
        pulseTimer = 0f;

        if (completionPanel != null)
        {
            completionPanel.SetActive(false);
            completionPanel.transform.localScale = targetScale;
        }
    }
}
