using UnityEngine;
using UnityEngine.UI;

namespace Lake
{
    /// <summary>
    /// SIMPLE VR oxygen bar - Screen Space Camera mode for reliability
    /// </summary>
    public class VROxygenBar : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private OxygenSystem oxygenSystem;
        [SerializeField] private Image barImage;
        
        [Header("Settings")]
        [SerializeField] private bool alwaysVisible = true;
        
        [Header("Colors")]
        [SerializeField] private Color highColor = new Color(0x00 / 255f, 0x96 / 255f, 0xC7 / 255f); // #0096C7 // Cyan
        [SerializeField] private Color mediumColor = new Color(0x00 / 255f, 0x77 / 255f, 0xB6 / 255f); // #0077B6 // Yellow
        [SerializeField] private Color lowColor = new Color(0x03 / 255f, 0x04 / 255f, 0x5E / 255f); // #03045E // Red
        
        private Canvas canvas;
        private CanvasGroup canvasGroup;
        
        private void Awake()
        {
            // Find components
            canvas = GetComponentInParent<Canvas>();
            canvasGroup = canvas.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
            }
            
            if (barImage == null)
            {
                barImage = GetComponent<Image>();
            }
            
            if (oxygenSystem == null)
            {
                oxygenSystem = FindObjectOfType<OxygenSystem>();
            }
            
            // Configure image
            
            
            // Add white outline/stroke
            UnityEngine.UI.Outline outline = barImage.GetComponent<UnityEngine.UI.Outline>();
            if (outline == null)
            {
                outline = barImage.gameObject.AddComponent<UnityEngine.UI.Outline>();
            }
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(2, -2);
            outline.useGraphicAlpha = true;
            
if (barImage != null)
            {
                barImage.type = Image.Type.Filled;
                barImage.fillMethod = Image.FillMethod.Horizontal;
                barImage.fillAmount = 1.0f;
                barImage.color = highColor;
                Debug.Log("Bar configured!");
            }
        }
        
private void Start()
        {
            // Only configure canvas if it's not already set up for VR
            if (canvas != null && canvas.worldCamera == null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = Camera.main;
                canvas.planeDistance = 0.5f;
                
                Camera cam = Camera.main;
                string camName = cam != null ? cam.name : "null";
                Debug.Log("VROxygenBar: Canvas set to ScreenSpaceCamera, camera: " + camName);
            }
            else if (canvas != null)
            {
                Debug.Log("VROxygenBar: Canvas already configured (worldCamera assigned)");
            }
            
            // Make visible
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
            
            // Initial update
            if (oxygenSystem != null)
            {
                UpdateBar(oxygenSystem.OxygenPercentage);
            }
        }
        
        private void OnEnable()
        {
            if (oxygenSystem != null)
            {
                oxygenSystem.OnOxygenChanged.AddListener(UpdateBar);
            }
        }
        
        private void OnDisable()
        {
            if (oxygenSystem != null)
            {
                oxygenSystem.OnOxygenChanged.RemoveListener(UpdateBar);
            }
        }
        
        private void Update()
        {
            // Handle visibility
            if (alwaysVisible)
            {
                if (canvasGroup != null) canvasGroup.alpha = 1f;
            }
            else if (oxygenSystem != null && canvasGroup != null)
            {
                float target = oxygenSystem.IsUnderwater ? 1f : 0f;
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, target, Time.deltaTime * 5f);
            }
        }
        
        private void UpdateBar(float percentage)
        {
            if (barImage == null) return;
            
            percentage = Mathf.Clamp01(percentage);
            barImage.fillAmount = percentage;
            
            // Color based on level
            if (percentage > 0.5f)
                barImage.color = highColor;
            else if (percentage > 0.25f)
                barImage.color = Color.Lerp(mediumColor, highColor, (percentage - 0.25f) / 0.25f);
            else
                barImage.color = Color.Lerp(lowColor, mediumColor, percentage / 0.25f);
        }
    }
}