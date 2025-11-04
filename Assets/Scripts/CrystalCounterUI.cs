using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CrystalCounterUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI counterText;
    [SerializeField] private Image crystalIcon;
    
    [Header("Animation Settings")]
    [SerializeField] private bool animateOnCollection = true;
    [SerializeField] private float popScale = 1.2f;
    [SerializeField] private float popDuration = 0.2f;
    
    private Vector3 originalScale;
    private bool isAnimating = false;
    
    void Start()
    {
        if (counterText != null)
        {
            originalScale = counterText.transform.localScale;
        }
        
        // Subscribe to collection events
        if (CrystalCollectionSystem.Instance != null)
        {
            CrystalCollectionSystem.Instance.OnCrystalCollected.AddListener(UpdateCounter);
            
            // Initialize display
            int collected = CrystalCollectionSystem.Instance.GetCrystalsCollected();
            int total = CrystalCollectionSystem.Instance.GetTotalCrystals();
            UpdateCounter(collected, total);
        }
        else
        {
            Debug.LogWarning("CrystalCollectionSystem.Instance is null!");
        }
    }
    
private void UpdateCounter(int collected, int total)
    {
        if (counterText != null)
        {
            int remaining = total - collected;
            counterText.text = $"Crystals: {collected}/{total}\nRemaining: {remaining}";
            
            if (animateOnCollection && collected > 0)
            {
                AnimatePop();
            }
        }
    }
    
    private void AnimatePop()
    {
        if (!isAnimating && counterText != null)
        {
            StartCoroutine(PopAnimation());
        }
    }
    
    private System.Collections.IEnumerator PopAnimation()
    {
        isAnimating = true;
        
        // Scale up
        float elapsed = 0f;
        while (elapsed < popDuration / 2)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (popDuration / 2);
            counterText.transform.localScale = Vector3.Lerp(originalScale, originalScale * popScale, t);
            yield return null;
        }
        
        // Scale down
        elapsed = 0f;
        while (elapsed < popDuration / 2)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (popDuration / 2);
            counterText.transform.localScale = Vector3.Lerp(originalScale * popScale, originalScale, t);
            yield return null;
        }
        
        counterText.transform.localScale = originalScale;
        isAnimating = false;
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (CrystalCollectionSystem.Instance != null)
        {
            CrystalCollectionSystem.Instance.OnCrystalCollected.RemoveListener(UpdateCounter);
        }
    }
}
