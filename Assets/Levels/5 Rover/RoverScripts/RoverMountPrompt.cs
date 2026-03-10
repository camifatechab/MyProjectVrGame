using UnityEngine;
using UnityEngine.UI;

/// Floating "Grip to Ride" prompt above the rover.
/// Auto-creates its own canvas — no manual setup needed.
public class RoverMountPrompt : MonoBehaviour
{
    [Header("Settings")]
    public float showRadius   = 5f;
    public float floatHeight  = 2.5f;
    public string promptText  = "✋  Grip to Ride";

    private Transform   playerHead;
    private Canvas      canvas;
    private Text        label;
    private RoverDriver driver;

    void Start()
    {
        driver = GetComponent<RoverDriver>();

        // Find headset
        var cam = Camera.main;
        if (cam != null) playerHead = cam.transform;

        // Build world-space canvas as child of rover
        var go = new GameObject("MountPromptCanvas");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, floatHeight, 0f);
        go.transform.localScale    = Vector3.one * 0.01f; // world-space canvas scale

        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400f, 80f);

        // Background panel
        var bg = new GameObject("BG");
        bg.transform.SetParent(go.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.55f);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        // Text
        var textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        label = textGo.AddComponent<Text>();
        label.text      = promptText;
        label.fontSize  = 48;
        label.alignment = TextAnchor.MiddleCenter;
        label.color     = Color.white;
        label.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        canvas.gameObject.SetActive(false);
    }

    void Update()
    {
        if (canvas == null || playerHead == null || driver == null) return;

        // Hide when mounted
        if (driver.IsMounted)
        {
            canvas.gameObject.SetActive(false);
            return;
        }

        float dist = Vector3.Distance(playerHead.position, transform.position);
        canvas.gameObject.SetActive(dist < showRadius);

        // Always face the player
        if (canvas.gameObject.activeSelf)
            canvas.transform.LookAt(playerHead);
    }
}
