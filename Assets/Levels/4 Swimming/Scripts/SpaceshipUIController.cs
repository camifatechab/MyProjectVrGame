using UnityEngine;
using TMPro;
using System.Collections;

namespace Lake
{
    /// <summary>
    /// Controls the spaceship collection UI updates
    /// </summary>
    public class SpaceshipUIController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI counterText;
        [SerializeField] private GameObject feedbackMessage;
        [SerializeField] private GameObject victoryMessage;
        
        [Header("Feedback Settings")]
        [SerializeField] private float feedbackDisplayTime = 2f;
        
        private SpaceshipCollectionManager manager;
        
        void Start()
        {
            // Find the collection manager
            manager = FindFirstObjectByType<SpaceshipCollectionManager>();
            
            if (manager == null)
            {
                Debug.LogError("SpaceshipCollectionManager not found!");
                return;
            }
            
            // Subscribe to collection events
            manager.OnPieceCollected.AddListener(OnPieceCollected);
            manager.OnAllPiecesCollected.AddListener(OnAllPiecesCollected);
            
            // Initialize counter
            UpdateCounter(0, manager.TotalPieces);
            
            // Hide messages
            if (feedbackMessage != null) feedbackMessage.SetActive(false);
            if (victoryMessage != null) victoryMessage.SetActive(false);
        }
        
void OnPieceCollected(int collected, int total)
        {
            UpdateCounter(collected, total);
            
            if (counterText != null)
            {
                counterText.color = Color.green;
                StartCoroutine(ResetColorAfterDelay());
            }
        }
        
void OnAllPiecesCollected()
        {
            if (counterText != null)
            {
                counterText.text = "Complete!";
                counterText.color = Color.white;
            }
            
            GameObject remainingText = GameObject.Find("RemainingText");
            if (remainingText != null)
            {
                TextMeshProUGUI tmp = remainingText.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = "Level Complete!";
                    tmp.color = new Color(1f, 0.9f, 0.4f);
                }
            }
            
            Debug.Log("<color=yellow>★★★ LEVEL COMPLETE! ★★★</color>");
        }
        
void UpdateCounter(int collected, int total)
        {
            if (counterText != null)
            {
                counterText.text = $"Parts: {collected}/{total}";
            }
            
            GameObject remainingText = GameObject.Find("RemainingText");
            if (remainingText != null)
            {
                TextMeshProUGUI tmp = remainingText.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    int remaining = total - collected;
                    tmp.text = $"Remaining: {remaining}";
                }
            }
        }
        
        IEnumerator ShowFeedbackMessage()
        {
            feedbackMessage.SetActive(true);
            yield return new WaitForSeconds(feedbackDisplayTime);
            feedbackMessage.SetActive(false);
        }
        
        void OnDestroy()
        {
            // Unsubscribe from events
            if (manager != null)
            {
                manager.OnPieceCollected.RemoveListener(OnPieceCollected);
                manager.OnAllPiecesCollected.RemoveListener(OnAllPiecesCollected);
            }
        }
    

IEnumerator ResetColorAfterDelay()
        {
            yield return new WaitForSeconds(1f);
            if (counterText != null)
            {
                counterText.color = Color.white;
            }
        }
}
}