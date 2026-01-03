using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages all crystals in the scene.
/// Tracks collection progress, timing, and triggers final sequence when all are collected.
/// </summary>
public class CrystalManager : MonoBehaviour
{
    public static CrystalManager Instance { get; private set; }

    [Header("Crystal Tracking")]
    private List<CrystalCollectible> allCrystals = new List<CrystalCollectible>();
    private int collectedCount = 0;

    [Header("Final Dialogue")]
    public TMP_Text subtitleText;
    [TextArea]
    public string finalDialogueLine;
    public AudioSource dialogueSource;
    public AudioClip finalDialogueClip;

    [Header("Scene Transition")]
    public FadeScreen fadeScreen;
    public string nextSceneName = "CampfireHub";
    public float delayAfterDialogue = 1.5f;

    [Header("Completion UI")]
    [Tooltip("How long to display the completion UI before transitioning")]
    public float completionDisplayTime = 8f;

    [Header("Timer & Scoring")]
    [Tooltip("Time threshold for 3 stars (in seconds)")]
    public float threeStarTime = 90f;   // 1:30
    [Tooltip("Time threshold for 2 stars (in seconds)")]
    public float twoStarTime = 150f;    // 2:30
    [Tooltip("Time threshold for 1 star (in seconds)")]
    public float oneStarTime = 240f;    // 4:00
    
    private float elapsedTime = 0f;
    private bool timerRunning = false;
    public float ElapsedTime => elapsedTime;
    
    // PlayerPrefs key for best time
    private const string BEST_TIME_KEY = "JetpackCrystals_BestTime";

    [Header("Debug")]
    public bool showDebugMessages = true;

    [Header("Test Button (Play Mode Only)")]
    [Tooltip("Check this box during Play Mode to trigger completion")]
    public bool triggerCompletion = false;
    [Tooltip("Check this box during Play Mode to clear best time")]
    public bool clearBestTime = false;



    public int TotalCrystals => allCrystals.Count;
    public int CollectedCrystals => collectedCount;
    public int RemainingCrystals => TotalCrystals - CollectedCrystals;
    public bool AllCollected => RemainingCrystals == 0 && TotalCrystals > 0;

    private bool sequenceStarted = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if (subtitleText)
            subtitleText.text = "";
    }

    void Start()
    {
        // Start timer when scene loads
        timerRunning = true;
        elapsedTime = 0f;
        
        if (showDebugMessages)
            Debug.Log("<color=cyan>Crystal Timer started!</color>");
    }

void Update()
    {
        // Track elapsed time
        if (timerRunning)
        {
            elapsedTime += Time.deltaTime;
        }

        // Inspector checkbox trigger for testing
        if (triggerCompletion && !sequenceStarted)
        {
            triggerCompletion = false;
            DebugCompleteAllCrystals();
        }

        // Inspector checkbox to clear best time
        if (clearBestTime)
        {
            clearBestTime = false;
            DebugClearBestTime();
        }
    }

    public void RegisterCrystal(CrystalCollectible crystal)
    {
        if (!allCrystals.Contains(crystal))
        {
            allCrystals.Add(crystal);

            if (showDebugMessages)
                Debug.Log($"Crystal registered. Total: {TotalCrystals}");
        }
    }

    public void OnCrystalCollected(CrystalCollectible crystal)
    {
        if (!allCrystals.Contains(crystal)) return;

        collectedCount++;

        if (showDebugMessages)
            Debug.Log($"Crystal collected {CollectedCrystals}/{TotalCrystals}");

        if (AllCollected && !sequenceStarted)
        {
            sequenceStarted = true;
            StartCoroutine(FinalSequence());
        }
    }

    private IEnumerator FinalSequence()
    {
        // Stop timer
        timerRunning = false;
        float completionTime = elapsedTime;
        
        if (showDebugMessages)
            Debug.Log($"ALL CRYSTALS COLLECTED - Time: {FormatTime(completionTime)}");

        // Calculate stars and check for new record
        int stars = GetStarRating(completionTime);
        bool isNewRecord = TrySaveBestTime(completionTime);
        float bestTime = GetBestTime();

        if (showDebugMessages)
            Debug.Log($"Stars: {stars}, Best Time: {FormatTime(bestTime)}, New Record: {isNewRecord}");

        // Show completion UI with timer data
        if (CrystalCompleteUI.Instance != null)
        {
            CrystalCompleteUI.Instance.ShowCompletion(completionTime, stars, bestTime, isNewRecord);
        }

        // Show subtitle
        if (subtitleText)
            subtitleText.text = finalDialogueLine;

        // Play dialogue audio if available
        if (dialogueSource && finalDialogueClip)
        {
            dialogueSource.PlayOneShot(finalDialogueClip);
        }

        // Wait for completion display time
        yield return new WaitForSeconds(completionDisplayTime);

        // Hide completion UI before fade
        if (CrystalCompleteUI.Instance != null)
        {
            CrystalCompleteUI.Instance.Hide();
        }

        // Fade out
        if (fadeScreen)
            fadeScreen.FadeOut();

        yield return new WaitForSeconds(delayAfterDialogue);

        SceneManager.LoadScene(nextSceneName);
    }

    /// <summary>
    /// Calculate star rating based on completion time
    /// </summary>
    public int GetStarRating(float time)
    {
        if (time <= threeStarTime) return 3;
        if (time <= twoStarTime) return 2;
        if (time <= oneStarTime) return 1;
        return 0;
    }

    /// <summary>
    /// Get best time from PlayerPrefs (returns -1 if no best time saved)
    /// </summary>
    public float GetBestTime()
    {
        return PlayerPrefs.GetFloat(BEST_TIME_KEY, -1f);
    }

    /// <summary>
    /// Save best time if it's a new record
    /// </summary>
    public bool TrySaveBestTime(float time)
    {
        float currentBest = GetBestTime();
        
        if (currentBest < 0 || time < currentBest)
        {
            PlayerPrefs.SetFloat(BEST_TIME_KEY, time);
            PlayerPrefs.Save();
            return true; // New record!
        }
        return false;
    }

    /// <summary>
    /// Format time as M:SS
    /// </summary>
    public static string FormatTime(float seconds)
    {
        int mins = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{mins}:{secs:D2}";
    }

    public void ResetProgress()
    {
        collectedCount = 0;
        sequenceStarted = false;
        timerRunning = true;
        elapsedTime = 0f;

        if (showDebugMessages)
            Debug.Log("Crystal progress reset.");
    }

    /// <summary>
    /// DEBUG: Instantly complete all crystals for testing UI
    /// </summary>
    [ContextMenu("DEBUG: Complete All Crystals")]
    public void DebugCompleteAllCrystals()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Must be in Play Mode to test completion!");
            return;
        }

        if (sequenceStarted)
        {
            Debug.LogWarning("Sequence already started!");
            return;
        }

        Debug.Log("<color=yellow>DEBUG: Forcing crystal completion...</color>");
        
        collectedCount = allCrystals.Count;
        sequenceStarted = true;
        StartCoroutine(FinalSequence());
    }

    /// <summary>
    /// DEBUG: Clear best time record
    /// </summary>
    [ContextMenu("DEBUG: Clear Best Time")]
    public void DebugClearBestTime()
    {
        PlayerPrefs.DeleteKey(BEST_TIME_KEY);
        PlayerPrefs.Save();
        Debug.Log("<color=yellow>DEBUG: Best time cleared!</color>");
    }
}
