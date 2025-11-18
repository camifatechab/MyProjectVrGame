using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using Lake;

/// <summary>
/// Creates the spaceship collection UI system matching the jetpack scene style
/// </summary>
public class SetupSpaceshipUI : MonoBehaviour
{
    [MenuItem("Tools/Swimming/Setup Collection UI")]
    static void SetupUI()
    {
        // Find the XR Origin to attach UI to
        GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
        if (xrOrigin == null)
        {
            Debug.LogError("XR Origin (XR Rig) not found!");
            return;
        }
        
        Transform cameraOffset = xrOrigin.transform.Find("Camera Offset");
        if (cameraOffset == null)
        {
            Debug.LogError("Camera Offset not found under XR Origin!");
            return;
        }
        
        // Create main UI canvas
        GameObject canvasObj = new GameObject("SpaceshipUI_Canvas");
        canvasObj.transform.SetParent(cameraOffset, false);
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;
        
        GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();
        
        // Position canvas on left wrist (like jetpack scene)
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.localPosition = new Vector3(-0.15f, -0.1f, 0.3f); // Left wrist position
        canvasRect.localRotation = Quaternion.Euler(0, 90, 0); // Face the player
        canvasRect.sizeDelta = new Vector2(300, 200);
        canvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f); // Scale for VR
        
        // Create counter panel
        CreateCounterUI(canvasObj.transform);
        
        // Create feedback message UI
        CreateFeedbackUI(canvasObj.transform);
        
        // Create victory UI
        CreateVictoryUI(canvasObj.transform);
        
        // Add UI controller script
        AddUIController(canvasObj);
        
        Debug.Log("<color=green>✓ Spaceship UI created successfully!</color>");
        Debug.Log("  - Canvas attached to left wrist");
        Debug.Log("  - Counter, Feedback, and Victory UI ready");
        
        EditorUtility.SetDirty(canvasObj);
    }
    
    static void CreateCounterUI(Transform parent)
    {
        // Create panel for counter
        GameObject panelObj = new GameObject("CounterPanel");
        panelObj.transform.SetParent(parent, false);
        
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0, -10);
        panelRect.sizeDelta = new Vector2(280, 80);
        
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.7f);
        
        // Create counter text
        GameObject textObj = new GameObject("CounterText");
        textObj.transform.SetParent(panelObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Parts: 0/1";
        text.fontSize = 36;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.fontStyle = FontStyles.Bold;
    }
    
    static void CreateFeedbackUI(Transform parent)
    {
        // Create feedback message
        GameObject feedbackObj = new GameObject("FeedbackMessage");
        feedbackObj.transform.SetParent(parent, false);
        feedbackObj.SetActive(false); // Hidden by default
        
        RectTransform feedbackRect = feedbackObj.AddComponent<RectTransform>();
        feedbackRect.anchorMin = new Vector2(0.5f, 0.5f);
        feedbackRect.anchorMax = new Vector2(0.5f, 0.5f);
        feedbackRect.pivot = new Vector2(0.5f, 0.5f);
        feedbackRect.anchoredPosition = Vector2.zero;
        feedbackRect.sizeDelta = new Vector2(280, 60);
        
        Image feedbackImage = feedbackObj.AddComponent<Image>();
        feedbackImage.color = new Color(0, 0.5f, 0, 0.9f); // Green background
        
        // Create text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(feedbackObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Part Collected!";
        text.fontSize = 32;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.fontStyle = FontStyles.Bold;
    }
    
    static void CreateVictoryUI(Transform parent)
    {
        // Create victory message
        GameObject victoryObj = new GameObject("VictoryMessage");
        victoryObj.transform.SetParent(parent, false);
        victoryObj.SetActive(false); // Hidden by default
        
        RectTransform victoryRect = victoryObj.AddComponent<RectTransform>();
        victoryRect.anchorMin = new Vector2(0.5f, 0.5f);
        victoryRect.anchorMax = new Vector2(0.5f, 0.5f);
        victoryRect.pivot = new Vector2(0.5f, 0.5f);
        victoryRect.anchoredPosition = Vector2.zero;
        victoryRect.sizeDelta = new Vector2(280, 80);
        
        Image victoryImage = victoryObj.AddComponent<Image>();
        victoryImage.color = new Color(1f, 0.8f, 0, 0.95f); // Gold background
        
        // Create text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(victoryObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Level Complete!";
        text.fontSize = 36;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;
        text.fontStyle = FontStyles.Bold;
    }
    
static void AddUIController(GameObject canvasObj)
    {
        // Add the UI controller component
        SpaceshipUIController controller = canvasObj.AddComponent<SpaceshipUIController>();
        
        // Wire up references
        SerializedObject so = new SerializedObject(controller);
        
        // Find counter text
        Transform counterPanel = canvasObj.transform.Find("CounterPanel");
        if (counterPanel != null)
        {
            Transform counterText = counterPanel.Find("CounterText");
            if (counterText != null)
            {
                so.FindProperty("counterText").objectReferenceValue = counterText.GetComponent<TextMeshProUGUI>();
            }
        }
        
        // Find feedback message
        Transform feedbackMsg = canvasObj.transform.Find("FeedbackMessage");
        if (feedbackMsg != null)
        {
            so.FindProperty("feedbackMessage").objectReferenceValue = feedbackMsg.gameObject;
        }
        
        // Find victory message
        Transform victoryMsg = canvasObj.transform.Find("VictoryMessage");
        if (victoryMsg != null)
        {
            so.FindProperty("victoryMessage").objectReferenceValue = victoryMsg.gameObject;
        }
        
        so.ApplyModifiedProperties();
        
        Debug.Log("  - SpaceshipUIController added and wired up");
    }
}