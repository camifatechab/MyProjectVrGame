using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attaches a red vignette overlay to the VR camera that pulses while the laser is firing.
/// No external textures needed — gradient is generated procedurally.
/// </summary>
public class LaserVignetteEffect : MonoBehaviour
{
    [Header("Vignette Settings")]
    [Range(0f, 1f)] public float maxAlpha = 0.55f;
    [Range(0f, 1f)] public float minAlpha = 0f;
    public float fadeInSpeed  = 8f;
    public float fadeOutSpeed = 5f;
    public Color vignetteColor = new Color(0.05f, 0.05f, 1f, 1f);

    // Runtime
    private Image  vignetteImage;
    private Canvas canvas;
    private bool   isActive = false;
    private float  currentAlpha = 0f;

    void Start()
    {
        BuildVignette();
    }

    void BuildVignette()
    {
        // Canvas parented to Main Camera — always follows headset
        Camera cam = Camera.main;
        if (cam == null) { Debug.LogWarning("[LaserVignette] No Main Camera found."); return; }

        GameObject canvasGO = new GameObject("LaserVignetteCanvas");
        canvasGO.transform.SetParent(cam.transform, false);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode      = RenderMode.WorldSpace;
        canvas.worldCamera     = cam;

        // Position just in front of the camera
        RectTransform rt = canvasGO.GetComponent<RectTransform>();
        rt.localPosition = new Vector3(0f, 0f, 0.31f);
        rt.localRotation = Quaternion.identity;
        rt.localScale    = Vector3.one * 0.001f;
        rt.sizeDelta     = new Vector2(1920f, 1080f);

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Image with procedural radial gradient texture
        GameObject imgGO = new GameObject("VignetteImage");
        imgGO.transform.SetParent(canvasGO.transform, false);

        RectTransform imgRT = imgGO.AddComponent<RectTransform>();
        imgRT.anchorMin = Vector2.zero;
        imgRT.anchorMax = Vector2.one;
        imgRT.sizeDelta = Vector2.zero;
        imgRT.anchoredPosition = Vector2.zero;

        vignetteImage         = imgGO.AddComponent<Image>();
        vignetteImage.sprite  = BuildVignetteSprite(256, 256);
        vignetteImage.color   = new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, 0f);
        vignetteImage.raycastTarget = false;

        Debug.Log("<color=cyan>✓ LaserVignetteEffect ready</color>");
    }

    Sprite BuildVignetteSprite(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[w * h];

        float cx = w * 0.5f;
        float cy = h * 0.5f;
        float maxDist = Mathf.Sqrt(cx * cx + cy * cy);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx   = x - cx;
                float dy   = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                // Gradient: transparent in centre, opaque at edges
                float t    = Mathf.Clamp01(dist / maxDist);
                t          = Mathf.Pow(t, 1.5f); // steeper falloff
                pixels[y * w + x] = new Color(1f, 1f, 1f, t);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex,
            new Rect(0, 0, w, h),
            new Vector2(0.5f, 0.5f));
    }

    void Update()
    {
        if (vignetteImage == null) return;

        float target = isActive ? maxAlpha : minAlpha;
        float speed  = isActive ? fadeInSpeed : fadeOutSpeed;
        currentAlpha = Mathf.MoveTowards(currentAlpha, target, speed * Time.deltaTime);

        Color c = vignetteImage.color;
        c.a = currentAlpha;
        vignetteImage.color = c;
    }

    // Called by LaserShooter
    public void SetActive(bool active) => isActive = active;
}
