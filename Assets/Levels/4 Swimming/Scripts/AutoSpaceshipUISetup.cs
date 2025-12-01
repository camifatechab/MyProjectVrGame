using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Lake
{
    /// <summary>
    /// EXACT copy of jetpack UI system but for spaceship parts
    /// Automatically sets up UI matching the jetpack scene
    /// </summary>
    public class AutoSpaceshipUISetup : MonoBehaviour
    {
        [Header("UI Configuration - Matches Jetpack Scene")]
        [SerializeField] private Vector3 canvasPosition = new Vector3(-0.0548f, 0.2f, 0.6548f);
        [SerializeField] private Vector3 canvasRotation = new Vector3(15f, 0f, 0f);
        [SerializeField] private Vector3 canvasScale = new Vector3(0.001f, 0.001f, 0.001f);
        [SerializeField] private Vector2 canvasSize = new Vector2(100f, 100f);
        
        private TextMeshProUGUI partsText;
        private TextMeshProUGUI remainingText;
        private SpaceshipCollectionManager manager;
        
        void Start()
        {
            manager = GetComponent<SpaceshipCollectionManager>();
            if (manager == null)
            {
                Debug.LogError("SpaceshipCollectionManager not found!");
                return;
            }
            
            CreateUICanvas();
            
            manager.OnPieceCollected.AddListener(UpdateUI);
            manager.OnAllPiecesCollected.AddListener(OnVictory);
            
            UpdateUI(0, manager.TotalPieces);
        }
        
        void CreateUICanvas()
        {
            GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
            if (xrOrigin == null) return;
            
            Transform cameraOffset = xrOrigin.transform.Find("Camera Offset");
            if (cameraOffset == null) return;
            
            Transform mainCamera = cameraOffset.Find("Main Camera");
            if (mainCamera == null) return;
            
            GameObject canvasObj = new GameObject("VR_UI_Canvas_Spaceship");
            canvasObj.transform.SetParent(mainCamera, false);
            canvasObj.transform.localPosition = canvasPosition;
            canvasObj.transform.localRotation = Quaternion.Euler(canvasRotation);
            canvasObj.transform.localScale = canvasScale;
            
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = canvasSize;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            scaler.referencePixelsPerUnit = 100f;
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            CreatePartsPanel(canvasObj.transform);
            
            Debug.Log("Spaceship UI created!");
        }
        
void CreatePartsPanel(Transform canvasTransform)
        {
            GameObject panel = new GameObject("PartsPanel");
            panel.transform.SetParent(canvasTransform, false);
            
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(300f, 80f);
            
            GameObject background = new GameObject("Background");
            background.transform.SetParent(panel.transform, false);
            
            RectTransform bgRect = background.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            GameObject partsTextObj = new GameObject("PartsText");
            partsTextObj.transform.SetParent(panel.transform, false);
            
            RectTransform partsRect = partsTextObj.AddComponent<RectTransform>();
            partsRect.anchorMin = new Vector2(0f, 0.5f);
            partsRect.anchorMax = new Vector2(1f, 1f);
            partsRect.offsetMin = new Vector2(10f, 5f);
            partsRect.offsetMax = new Vector2(-10f, -5f);
            
            partsText = partsTextObj.AddComponent<TextMeshProUGUI>();
            partsText.text = "Parts: 0/1";
            partsText.fontSize = 24;
            partsText.alignment = TextAlignmentOptions.Center;
            partsText.color = Color.white;
            
            GameObject remainingTextObj = new GameObject("RemainingText");
            remainingTextObj.transform.SetParent(panel.transform, false);
            
            RectTransform remainingRect = remainingTextObj.AddComponent<RectTransform>();
            remainingRect.anchorMin = new Vector2(0f, 0f);
            remainingRect.anchorMax = new Vector2(1f, 0.5f);
            remainingRect.offsetMin = new Vector2(10f, 5f);
            remainingRect.offsetMax = new Vector2(-10f, -5f);
            
            remainingText = remainingTextObj.AddComponent<TextMeshProUGUI>();
            remainingText.text = "Remaining: 1";
            remainingText.fontSize = 20;
            remainingText.alignment = TextAlignmentOptions.Center;
            remainingText.color = new Color(0.6f, 0.8f, 1f);
        }
        
        void UpdateUI(int collected, int total)
        {
            if (partsText != null)
            {
                partsText.text = $"Parts: {collected}/{total}";
            }
            
            if (remainingText != null)
            {
                int remaining = total - collected;
                remainingText.text = $"Remaining: {remaining}";
            }
        }
        
void OnVictory()
        {
            if (partsText != null)
            {
                partsText.text = "Complete!";
                partsText.color = Color.white;
            }
            
            if (remainingText != null)
            {
                remainingText.text = "Level Complete!";
                remainingText.color = new Color(1f, 0.9f, 0.4f);
            }
        }
        
        void OnDestroy()
        {
            if (manager != null)
            {
                manager.OnPieceCollected.RemoveListener(UpdateUI);
                manager.OnAllPiecesCollected.RemoveListener(OnVictory);
            }
        }
    }
}
