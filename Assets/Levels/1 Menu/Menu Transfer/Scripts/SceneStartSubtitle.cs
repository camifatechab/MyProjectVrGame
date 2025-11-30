using UnityEngine;

public class SceneStartSubtitle : MonoBehaviour
{
    [Header("Subtitle Settings")]
    [Tooltip("The SubtitleManager instance in your scene.")]
    public SubtitleManager subtitleManager;

    [Tooltip("The message to display at the start of the scene.")]
    [TextArea(3, 5)]
    public string message = "Welcome!";

    [Tooltip("How long the message should be displayed (in seconds).")]
    public float displayDuration = 5.0f;

    [Tooltip("If true, the subtitle will play automatically when the scene starts.")]
    public bool playOnStart = true;

    [Tooltip("Delay before showing the subtitle after the scene starts (in seconds).")]
    public float startDelay = 0.5f; // Optional delay

    void Start()
    {
        if (playOnStart)
        {
            if (subtitleManager == null)
            {
                // Try to find it if not assigned
                subtitleManager = FindFirstObjectByType<SubtitleManager>();
                if (subtitleManager == null)
                {
                    Debug.LogError("SceneStartSubtitle: SubtitleManager not assigned and could not be found in the scene! Cannot display start subtitle.", this);
                    return;
                }
            }

            if (string.IsNullOrEmpty(message))
            {
                Debug.LogWarning("SceneStartSubtitle: No message set to display.", this);
                return;
            }

            if (startDelay > 0)
            {
                Invoke(nameof(DisplayMessage), startDelay);
            }
            else
            {
                DisplayMessage();
            }
        }
    }

    void DisplayMessage()
    {
        if (subtitleManager != null)
        {
            subtitleManager.ShowTimedSubtitle(message, displayDuration);
        }
        else
        {
            Debug.LogError("SceneStartSubtitle: SubtitleManager became null before DisplayMessage could be called.", this);
        }
    }

    // Optional: Public method to trigger it again if needed
    public void ShowSceneStartMessage()
    {
        if (subtitleManager == null)
        {
            subtitleManager = FindFirstObjectByType<SubtitleManager>();
            if (subtitleManager == null)
            {
                Debug.LogError("SceneStartSubtitle: SubtitleManager not assigned and could not be found in the scene!", this);
                return;
            }
        }
        DisplayMessage();
    }
}