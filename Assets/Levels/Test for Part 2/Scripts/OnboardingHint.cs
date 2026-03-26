using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// World-space onboarding hint attached to a specific object in the cave.
/// Shows when the player enters the trigger zone, dismisses when the
/// relevant input is detected via RoverDriver or AutoJetpackController.
/// </summary>
public class OnboardingHint : MonoBehaviour
{
    public enum DismissMode
    {
        RoverMounted,
        RoverMoved,
        RoverDismounted,
        JetpackFired
    }

    [Header("Hint Settings")]
    [SerializeField] private string hintText = "Hint text here";
    [SerializeField] private DismissMode dismissMode = DismissMode.RoverMounted;

    [Header("Chaining")]
    [SerializeField] private GameObject nextHint;
    [SerializeField] private bool startInactive = false;

    [Header("Fade Settings")]
    [SerializeField] private float fadeInDuration = 0.4f;
    [SerializeField] private float fadeOutDuration = 0.4f;

    // Internal refs
    private CanvasGroup canvasGroup;
    private Text labelText;
    private RoverDriver roverDriver;
    private AutoJetpackController jetpackController;

    // State
    private bool isVisible = false;
    private bool isDismissed = false;
    private Vector3 lastRoverPosition;

    void Awake()
    {
        EnsureCanvas();
        if (startInactive)
            gameObject.SetActive(false);
    }

    void Start()
    {
        roverDriver = FindAnyObjectByType<RoverDriver>();
        jetpackController = FindAnyObjectByType<AutoJetpackController>();
        if (roverDriver != null)
            lastRoverPosition = roverDriver.transform.position;
    }

    void Update()
    {
        if (isDismissed || !isVisible) return;
        if (CheckDismissCondition())
        {
            StartCoroutine(FadeOut(() =>
            {
                isDismissed = true;
                if (nextHint != null)
                    nextHint.SetActive(true);
                gameObject.SetActive(false);
            }));
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isDismissed || isVisible) return;
        if (!other.CompareTag("Player") && !other.CompareTag("MainCamera") &&
            !other.CompareTag("GameController")) return;
        StartCoroutine(FadeIn());
    }

    bool CheckDismissCondition()
    {
        switch (dismissMode)
        {
            case DismissMode.RoverMounted:
                return roverDriver != null && roverDriver.IsMounted;

            case DismissMode.RoverMoved:
                if (roverDriver == null) return false;
                float delta = Vector3.Distance(roverDriver.transform.position, lastRoverPosition);
                lastRoverPosition = roverDriver.transform.position;
                return delta > 0.05f;

            case DismissMode.RoverDismounted:
                return roverDriver != null && !roverDriver.IsMounted;

            case DismissMode.JetpackFired:
                return jetpackController != null && jetpackController.WasFiredThisSession;
        }
        return false;
    }

    void EnsureCanvas()
    {
        Canvas existing = GetComponentInChildren<Canvas>();
        if (existing != null)
        {
            canvasGroup = existing.GetComponentInChildren<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = existing.gameObject.AddComponent<CanvasGroup>();
            labelText = existing.GetComponentInChildren<Text>();
            if (labelText != null) labelText.text = hintText;
            canvasGroup.alpha = 0f;
            return;
        }

        // World-space canvas
        GameObject canvasGO = new GameObject("HintCanvas");
        canvasGO.transform.SetParent(transform);
        canvasGO.transform.localPosition = new Vector3(0f, 2.2f, 0f);
        canvasGO.transform.localRotation = Quaternion.identity;
        canvasGO.transform.localScale = Vector3.one * 0.007f;

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(560f, 160f);

        canvasGroup = canvasGO.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // Background panel
        GameObject panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        Image bg = panelGO.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.12f, 0.88f);

        // Text label
        GameObject textGO = new GameObject("Label");
        textGO.transform.SetParent(panelGO.transform, false);
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20f, 10f);
        textRect.offsetMax = new Vector2(-20f, -10f);

        labelText = textGO.AddComponent<Text>();
        labelText.text = hintText;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.fontSize = 38;
        labelText.fontStyle = FontStyle.Bold;
        labelText.color = Color.white;
        labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
        labelText.verticalOverflow = VerticalWrapMode.Overflow;
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        canvasGO.AddComponent<HintBillboard>();
    }

    IEnumerator FadeIn()
    {
        isVisible = true;
        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(t / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    IEnumerator FadeOut(System.Action onComplete)
    {
        float t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(1f - (t / fadeOutDuration));
            yield return null;
        }
        canvasGroup.alpha = 0f;
        isVisible = false;
        onComplete?.Invoke();
    }
}

/// <summary>
/// Keeps the hint canvas always facing the player camera.
/// </summary>
public class HintBillboard : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main == null) return;
        transform.LookAt(Camera.main.transform);
        transform.Rotate(0f, 180f, 0f);
    }
}
