using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple UI prompts for the rideable creature system.
/// Shows "Grip to Ride" when near, "Grip to Park" while flying.
/// </summary>
public class CreatureRideUI : MonoBehaviour
{
    [Header("UI References")]
    public Canvas rideCanvas;
    public Image backgroundPanel;
    public TextMeshProUGUI promptText;
    
    [Header("Prompt Messages")]
    public string nearCreaturePrompt = "Grip to Ride";
    public string flyingPrompt = "Grip to Park";
    
    [Header("Settings")]
    public float fadeSpeed = 3f;
    public float displayDistance = 5f;
    
    [Header("Glass Style")]
    public Color glassColor = new Color(0.1f, 0.1f, 0.1f, 0.6f);
    public Color textColor = Color.white;
    
    // References
    private RideableCreature creature;
    private Transform playerCamera;
    private CanvasGroup canvasGroup;
    private bool shouldShow = false;
    
    private void Start()
    {
        // Auto-find references if not assigned
        if (rideCanvas == null)
            rideCanvas = GetComponent<Canvas>();
        
        if (backgroundPanel == null)
            backgroundPanel = GetComponentInChildren<Image>();
        
        if (promptText == null)
            promptText = GetComponentInChildren<TextMeshProUGUI>();
        
        // Find creature
        creature = FindObjectOfType<RideableCreature>();
        
        // Find player camera
        var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin != null && xrOrigin.Camera != null)
        {
            playerCamera = xrOrigin.Camera.transform;
        }
        
        // Setup canvas group for fading
        if (rideCanvas != null)
        {
            canvasGroup = rideCanvas.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = rideCanvas.gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;
        }
        
        // Apply glass style
        ApplyGlassStyle();
    }
    
    private void ApplyGlassStyle()
    {
        if (backgroundPanel != null)
        {
            backgroundPanel.color = glassColor;
        }
        
        if (promptText != null)
        {
            promptText.color = textColor;
        }
    }
    
    private void Update()
    {
        if (creature == null || playerCamera == null) return;
        
        UpdatePromptState();
        UpdateCanvasFade();
        UpdateCanvasPosition();
    }
    
    private void UpdatePromptState()
    {
        // Simple logic: 2 states only
        if (creature.IsPlayerMounted && creature.IsFlying)
        {
            // Flying - show park prompt
            SetPrompt(flyingPrompt, true);
        }
        else if (!creature.IsPlayerMounted)
        {
            // Not mounted - check distance
            float distance = Vector3.Distance(playerCamera.position, creature.transform.position);
            if (distance <= displayDistance)
            {
                SetPrompt(nearCreaturePrompt, true);
            }
            else
            {
                SetPrompt("", false);
            }
        }
        else
        {
            // Mounted but not flying (parking or idle) - hide UI
            SetPrompt("", false);
        }
    }
    
    private void SetPrompt(string prompt, bool show)
    {
        shouldShow = show;
        
        if (promptText != null && !string.IsNullOrEmpty(prompt))
        {
            promptText.text = prompt;
        }
    }
    
    private void UpdateCanvasFade()
    {
        if (canvasGroup == null) return;
        
        float targetAlpha = shouldShow ? 1f : 0f;
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
    }
    
    private void UpdateCanvasPosition()
    {
        if (rideCanvas == null || playerCamera == null) return;
        
        // Position canvas in front of player (world space)
        if (rideCanvas.renderMode == RenderMode.WorldSpace)
        {
            Vector3 targetPos = playerCamera.position + playerCamera.forward * 2f;
            targetPos.y = playerCamera.position.y - 0.3f;
            
            rideCanvas.transform.position = Vector3.Lerp(
                rideCanvas.transform.position, 
                targetPos, 
                10f * Time.deltaTime
            );
            
            rideCanvas.transform.rotation = Quaternion.LookRotation(
                rideCanvas.transform.position - playerCamera.position
            );
        }
    }
}
