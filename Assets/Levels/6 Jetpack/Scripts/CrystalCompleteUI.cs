using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Displays a celebration UI with stars, time, and personal best when all crystals are collected.
/// Uses Image sprites for stars - assign filled/empty sprites in Inspector.
/// </summary>
public class CrystalCompleteUI : MonoBehaviour
{
    public static CrystalCompleteUI Instance { get; private set; }

    [Header("UI References (Auto-Found)")]
    [SerializeField] private GameObject completionPanel;
    [SerializeField] private TextMeshProUGUI completionText;
    [SerializeField] private TextMeshProUGUI subText;
    [SerializeField] private Image panelBackground;

    [Header("Star Images")]
    [SerializeField] private Image[] starImages = new Image[3];
    [SerializeField] private Sprite starFilledSprite;
    [SerializeField] private Sprite starEmptySprite;

    [Header("Timer UI References")]
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI bestTimeText;
    [SerializeField] private TextMeshProUGUI newRecordText;

    [Header("Animation Settings")]
    [SerializeField] private float scaleInSpeed = 4f;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float scaleOvershoot = 1.15f;

    [Header("Color Settings")]
    [SerializeField] private Color completionColor = new Color(0.3f, 1f, 0.5f);
    [SerializeField] private Color glowColor = new Color(1f, 0.95f, 0.4f);
    [SerializeField] private Color subTextColor = new Color(0.8f, 0.9f, 1f);
    [SerializeField] private Color starFilledColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color starEmptyColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    [SerializeField] private Color newRecordColor = new Color(1f, 0.5f, 0.8f);

    [Header("Text Content")]
    [SerializeField] private string completionMessage = "ALL CRYSTALS COLLECTED!";
    [SerializeField] private string subMessage = "Returning to camp...";

    // Internal state
    private bool hasTriggered = false;
    private bool isAnimating = false;
    private float animationProgress = 0f;
    private Vector3 targetScale = Vector3.one;
    private float pulseTimer = 0f;
    private int earnedStars = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        AutoFindReferences();

        if (completionPanel != null)
        {
            targetScale = completionPanel.transform.localScale;
        }
    }

    void Start()
    {
        if (completionPanel != null)
            completionPanel.SetActive(false);

        if (completionText != null)
            completionText.text = completionMessage;
        if (subText != null)
            subText.text = subMessage;

        if (newRecordText != null)
            newRecordText.gameObject.SetActive(false);

        // Initialize stars as hidden
        InitializeStars();

        Debug.Log("<color=green>CrystalCompleteUI initialized!</color>");
    }

    void AutoFindReferences()
    {
        if (completionPanel == null)
        {
            Transform panelTransform = transform.Find("CompletionPanel");
            if (panelTransform != null)
            {
                completionPanel = panelTransform.gameObject;
                panelBackground = panelTransform.GetComponent<Image>();
            }
        }

        if (completionPanel != null)
        {
            if (completionText == null)
            {
                Transform t = completionPanel.transform.Find("CompletionText");
                if (t != null) completionText = t.GetComponent<TextMeshProUGUI>();
            }

            if (subText == null)
            {
                Transform t = completionPanel.transform.Find("SubText");
                if (t != null) subText = t.GetComponent<TextMeshProUGUI>();
            }

            if (timeText == null)
            {
                Transform t = completionPanel.transform.Find("TimeText");
                if (t != null) timeText = t.GetComponent<TextMeshProUGUI>();
            }

            if (bestTimeText == null)
            {
                Transform t = completionPanel.transform.Find("BestTimeText");
                if (t != null) bestTimeText = t.GetComponent<TextMeshProUGUI>();
            }

            if (newRecordText == null)
            {
                Transform t = completionPanel.transform.Find("NewRecordText");
                if (t != null) newRecordText = t.GetComponent<TextMeshProUGUI>();
            }

            // Find star images
            for (int i = 0; i < 3; i++)
            {
                if (starImages[i] == null)
                {
                    Transform starT = completionPanel.transform.Find($"Star{i + 1}");
                    if (starT != null)
                        starImages[i] = starT.GetComponent<Image>();
                }
            }

            if (panelBackground == null)
                panelBackground = completionPanel.GetComponent<Image>();
        }
    }

    void InitializeStars()
    {
        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] != null)
            {
                starImages[i].transform.localScale = Vector3.zero;
                if (starEmptySprite != null)
                    starImages[i].sprite = starEmptySprite;
                starImages[i].color = starEmptyColor;
            }
        }
    }

    void Update()
    {
        if (isAnimating)
        {
            UpdateScaleAnimation();
        }

        if (hasTriggered && !isAnimating)
        {
            UpdatePulseEffect();
        }
    }

    /// <summary>
    /// Called by CrystalManager when all crystals are collected.
    /// </summary>
    public void ShowCompletion(float completionTime, int stars, float bestTime, bool isNewRecord)
    {
        if (hasTriggered) return;

        hasTriggered = true;
        isAnimating = true;
        animationProgress = 0f;
        earnedStars = stars;

        Debug.Log($"<color=cyan>CrystalCompleteUI: Time {CrystalManager.FormatTime(completionTime)}, Stars: {stars}, New Record: {isNewRecord}</color>");

        if (completionPanel != null)
        {
            completionPanel.SetActive(true);
            completionPanel.transform.localScale = Vector3.zero;
        }

        if (completionText != null)
            completionText.color = completionColor;
        if (subText != null)
            subText.color = subTextColor;
        if (panelBackground != null)
            panelBackground.color = new Color(0f, 0f, 0f, 0.85f);

        if (timeText != null)
            timeText.text = $"Time: {CrystalManager.FormatTime(completionTime)}";

        if (bestTimeText != null)
        {
            if (bestTime > 0)
                bestTimeText.text = $"Best: {CrystalManager.FormatTime(bestTime)}";
            else
                bestTimeText.text = "";
        }

        if (newRecordText != null)
        {
            newRecordText.gameObject.SetActive(isNewRecord);
            if (isNewRecord)
            {
                newRecordText.text = "NEW RECORD!";
                newRecordText.color = newRecordColor;
                newRecordText.transform.localScale = Vector3.one;
            }
        }

        StartCoroutine(AnimateStars(stars));
    }

    /// <summary>
    /// Backwards compatible overload for testing
    /// </summary>
    public void ShowCompletion()
    {
        ShowCompletion(0f, 3, -1f, false);
    }

    IEnumerator AnimateStars(int count)
    {
        // Wait for panel to scale in
        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < 3; i++)
        {
            if (starImages[i] != null)
            {
                bool isFilled = (i < count);
                StartCoroutine(PopInStar(starImages[i], isFilled));
            }

            yield return new WaitForSeconds(0.25f);
        }
    }

    IEnumerator PopInStar(Image star, bool filled)
    {
        float duration = 0.3f;
        float elapsed = 0f;

        // Set sprite
        if (filled && starFilledSprite != null)
            star.sprite = starFilledSprite;
        else if (!filled && starEmptySprite != null)
            star.sprite = starEmptySprite;

        // Set color
        star.color = filled ? starFilledColor : starEmptyColor;

        // Animate scale with overshoot
        float targetScale = filled ? 1f : 0.7f;
        float overshoot = filled ? 1.3f : 0.8f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale;
            
            if (t < 0.6f)
            {
                // Scale up with overshoot
                scale = Mathf.Lerp(0f, overshoot, t / 0.6f);
            }
            else
            {
                // Settle back
                scale = Mathf.Lerp(overshoot, targetScale, (t - 0.6f) / 0.4f);
            }

            star.transform.localScale = Vector3.one * scale;
            yield return null;
        }

        star.transform.localScale = Vector3.one * targetScale;
    }

    void UpdateScaleAnimation()
    {
        animationProgress += Time.deltaTime * scaleInSpeed;

        if (completionPanel != null)
        {
            float t = animationProgress;
            float scale;

            if (t < 1f)
            {
                scale = Mathf.Lerp(0f, scaleOvershoot, EaseOutBack(t));
            }
            else
            {
                float settleT = (t - 1f) * 2f;
                scale = Mathf.Lerp(scaleOvershoot, 1f, Mathf.Clamp01(settleT));
            }

            completionPanel.transform.localScale = targetScale * scale;

            if (t >= 1.5f)
            {
                isAnimating = false;
                completionPanel.transform.localScale = targetScale;
            }
        }
    }

    void UpdatePulseEffect()
    {
        pulseTimer += Time.deltaTime * pulseSpeed;
        float pulse = (Mathf.Sin(pulseTimer * Mathf.PI) + 1f) / 2f;

        if (completionText != null)
        {
            completionText.color = Color.Lerp(completionColor, glowColor, pulse * 0.5f);
        }

        // Gentle color pulse on new record (no scale)
        if (newRecordText != null && newRecordText.gameObject.activeSelf)
        {
            float recordPulse = (Mathf.Sin(pulseTimer * Mathf.PI * 1.5f) + 1f) / 2f;
            newRecordText.color = Color.Lerp(newRecordColor, Color.white, recordPulse * 0.4f);
        }

        // Star shimmer for filled stars
        for (int i = 0; i < earnedStars && i < starImages.Length; i++)
        {
            if (starImages[i] != null)
            {
                float starPulse = (Mathf.Sin((pulseTimer + i * 0.3f) * Mathf.PI) + 1f) / 2f;
                starImages[i].color = Color.Lerp(starFilledColor, Color.white, starPulse * 0.3f);
            }
        }

        if (panelBackground != null)
        {
            float alpha = Mathf.Lerp(0.75f, 0.9f, pulse * 0.3f);
            Color bgColor = panelBackground.color;
            bgColor.a = alpha;
            panelBackground.color = bgColor;
        }
    }

    float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    public void Hide()
    {
        if (completionPanel != null)
            completionPanel.SetActive(false);
    }

    public void ResetUI()
    {
        hasTriggered = false;
        isAnimating = false;
        animationProgress = 0f;
        pulseTimer = 0f;
        earnedStars = 0;

        if (completionPanel != null)
        {
            completionPanel.SetActive(false);
            completionPanel.transform.localScale = targetScale;
        }

        InitializeStars();

        if (newRecordText != null)
            newRecordText.gameObject.SetActive(false);
    }
}
