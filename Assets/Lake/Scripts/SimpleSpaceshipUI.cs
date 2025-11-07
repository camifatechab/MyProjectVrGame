using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Lake
{
    /// <summary>
    /// Creates simple UI at runtime - attach to SpaceshipCollectionManager
    /// </summary>
    public class SimpleSpaceshipUI : MonoBehaviour
    {
                [Header("UI Position & Size")]
        [SerializeField] private Vector3 uiPosition = new Vector3(-0.15f, -0.1f, 0.3f);
        [SerializeField] private Vector3 uiRotation = new Vector3(0, 90, 0);
        [SerializeField] private Vector3 uiScale = new Vector3(0.001f, 0.001f, 0.001f);
        [SerializeField] private Vector2 canvasSize = new Vector2(300, 150);
        
        [Header("UI Appearance")]
        [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.8f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private float fontSize = 40f;
        
        
private TextMeshProUGUI counterText;
        private SpaceshipCollectionManager manager;
        
        void Start()
        {
            manager = GetComponent<SpaceshipCollectionManager>();
            if (manager == null)
            {
                Debug.LogError("SpaceshipCollectionManager not found!");
                return;
            }
            
            CreateUI();
            
            // Subscribe to events
            manager.OnPieceCollected.AddListener(OnPieceCollected);
            manager.OnAllPiecesCollected.AddListener(OnVictory);
        }
        
void CreateUI()
        {
            GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
            if (xrOrigin == null)
            {
                Debug.LogError("XR Origin not found!");
                return;
            }
            
            Transform cameraOffset = xrOrigin.transform.Find("Camera Offset");
            if (cameraOffset == null)
            {
                Debug.LogError("Camera Offset not found!");
                return;
            }
            
            // Create Canvas on left wrist - using Inspector values
            GameObject canvasObj = new GameObject("SpaceshipUI");
            canvasObj.transform.SetParent(cameraOffset, false);
            canvasObj.transform.localPosition = uiPosition;
            canvasObj.transform.localRotation = Quaternion.Euler(uiRotation);
            canvasObj.transform.localScale = uiScale;
            
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = canvasSize;
            
            // Black background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = backgroundColor;
            
            // Counter text
            GameObject textObj = new GameObject("CounterText");
            textObj.transform.SetParent(canvasObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            counterText = textObj.AddComponent<TextMeshProUGUI>();
            counterText.text = "Parts: 0/1";
            counterText.fontSize = fontSize;
            counterText.alignment = TextAlignmentOptions.Center;
            counterText.color = textColor;
            counterText.fontStyle = FontStyles.Bold;
            
            Debug.Log("<color=green>✓ Simple Spaceship UI created!</color>");
        }
        
        void OnPieceCollected(int collected, int total)
        {
            if (counterText != null)
            {
                counterText.text = $"Parts: {collected}/{total}";
                counterText.color = Color.green;
                
                Debug.Log($"<color=green>PART COLLECTED! {collected}/{total}</color>");
            }
        }
        
        void OnVictory()
        {
            if (counterText != null)
            {
                counterText.text = "LEVEL COMPLETE!";
                counterText.color = Color.yellow;
                counterText.fontSize = 50;
                
                Debug.Log("<color=yellow>★★★ VICTORY! ★★★</color>");
            }
        }
        
        void OnDestroy()
        {
            if (manager != null)
            {
                manager.OnPieceCollected.RemoveListener(OnPieceCollected);
                manager.OnAllPiecesCollected.RemoveListener(OnVictory);
            }
        }
    }
}
