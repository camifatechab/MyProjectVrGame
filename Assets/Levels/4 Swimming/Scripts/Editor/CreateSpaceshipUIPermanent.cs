using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

/// <summary>
/// Editor script to create the spaceship UI permanently in the scene
/// </summary>
public class CreateSpaceshipUIPermanent : MonoBehaviour
{
    [MenuItem("Tools/Swimming/Create UI in Scene")]
    static void CreateUI()
    {
        // Find XR Origin
        GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
        if (xrOrigin == null)
        {
            Debug.LogError("XR Origin (XR Rig) not found!");
            return;
        }
        
        Transform cameraOffset = xrOrigin.transform.Find("Camera Offset");
        if (cameraOffset == null)
        {
            Debug.LogError("Camera Offset not found!");
            return;
        }
        
        Transform mainCamera = cameraOffset.Find("Main Camera");
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
            return;
        }
        
        // Create Canvas
        GameObject canvasObj = new GameObject("VR UI Canvas - Parts");
        canvasObj.transform.SetParent(mainCamera, false);
        canvasObj.transform.localPosition = new Vector3(-0.0548f, 0.2f, 0.6548f);
        canvasObj.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
        canvasObj.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(100f, 100f);
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        scaler.referencePixelsPerUnit = 100f;
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create Parts Panel
        GameObject panel = new GameObject("PartsPanel");
        panel.transform.SetParent(canvasObj.transform, false);
        
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(300f, 80f);
        
        // Background
        GameObject background = new GameObject("Background");
        background.transform.SetParent(panel.transform, false);
        
        RectTransform bgRect = background.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        // Parts Text
        GameObject partsTextObj = new GameObject("PartsText");
        partsTextObj.transform.SetParent(panel.transform, false);
        
        RectTransform partsRect = partsTextObj.AddComponent<RectTransform>();
        partsRect.anchorMin = new Vector2(0f, 0.5f);
        partsRect.anchorMax = new Vector2(1f, 1f);
        partsRect.offsetMin = new Vector2(10f, 5f);
        partsRect.offsetMax = new Vector2(-10f, -5f);
        
        TextMeshProUGUI partsText = partsTextObj.AddComponent<TextMeshProUGUI>();
        partsText.text = "Parts: 0/1";
        partsText.fontSize = 24;
        partsText.alignment = TextAlignmentOptions.Center;
        partsText.color = Color.white;
        
        // Remaining Text
        GameObject remainingTextObj = new GameObject("RemainingText");
        remainingTextObj.transform.SetParent(panel.transform, false);
        
        RectTransform remainingRect = remainingTextObj.AddComponent<RectTransform>();
        remainingRect.anchorMin = new Vector2(0f, 0f);
        remainingRect.anchorMax = new Vector2(1f, 0.5f);
        remainingRect.offsetMin = new Vector2(10f, 5f);
        remainingRect.offsetMax = new Vector2(-10f, -5f);
        
        TextMeshProUGUI remainingText = remainingTextObj.AddComponent<TextMeshProUGUI>();
        remainingText.text = "Remaining: 1";
        remainingText.fontSize = 20;
        remainingText.alignment = TextAlignmentOptions.Center;
        remainingText.color = new Color(0.6f, 0.8f, 1f);
        
        // Add the controller script
        Lake.SpaceshipUIController controller = canvasObj.AddComponent<Lake.SpaceshipUIController>();
        
        // Wire up the controller
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("counterText").objectReferenceValue = partsText;
        so.FindProperty("feedbackMessage").objectReferenceValue = null;
        so.FindProperty("victoryMessage").objectReferenceValue = null;
        so.ApplyModifiedProperties();
        
        Debug.Log("<color=green>✓ UI Created in Scene! Find it under Main Camera > VR UI Canvas - Parts</color>");
        EditorUtility.SetDirty(canvasObj);
        Selection.activeGameObject = canvasObj;
    }
}
