/*using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SubtitleManager : MonoBehaviour
{
    public TextMeshProUGUI subtitleText;
    public float displayDuration = 2f;

    private Coroutine currentSubtitle;

    public void ShowSubtitle(string line, float duration)
    {
        if (currentSubtitle != null)
            StopCoroutine(currentSubtitle);

        currentSubtitle = StartCoroutine(ShowSubtitleRoutine(line, duration));
    }

    private IEnumerator ShowSubtitleRoutine(string line, float duration)
    {
        subtitleText.gameObject.SetActive(true);
        subtitleText.text = line;
        yield return new WaitForSeconds(duration);
        subtitleText.text = "";
        subtitleText.gameObject.SetActive(false);
        currentSubtitle = null;
    }
}*/

/*using UnityEngine;
using TMPro; // Make sure to import TextMeshPro

public class SubtitleManager : MonoBehaviour
{
    [Header("UI Reference")]
    [Tooltip("The TextMeshProUGUI element used to display subtitles.")]
    public TextMeshProUGUI subtitleTextUI;

    private AudioSource currentAudioSource; // The AudioSource currently being monitored
    private bool isShowingSubtitles = false;

    void Awake()
    {
        if (subtitleTextUI == null)
        {
            Debug.LogError("SubtitleManager: Subtitle Text UI not assigned! Subtitles will not work.", this);
            enabled = false; // Disable this script if the UI isn't set up
            return;
        }

        // Start with subtitles hidden
        subtitleTextUI.gameObject.SetActive(false);
        subtitleTextUI.text = "";
    }

    void Update()
    {
        if (isShowingSubtitles)
        {
            // If the audio source has stopped playing or became null (e.g., destroyed)
            if (currentAudioSource == null || !currentAudioSource.isPlaying)
            {
                HideSubtitles();
            }
        }
    }

    /// <summary>
    /// Plays an audio clip from the specified AudioSource and shows its corresponding subtitle.
    /// Any previously showing subtitle/audio will be stopped.
    /// </summary>
    /// <param name="sourceToPlayFrom">The AudioSource that will play the clip.</param>
    /// <param name="clipToPlay">The AudioClip for the dialogue.</param>
    /// <param name="textToShow">The subtitle text for this audio clip.</param>
    public void PlayDialogue(AudioSource sourceToPlayFrom, AudioClip clipToPlay, string textToShow)
    {
        if (subtitleTextUI == null)
        {
            Debug.LogWarning("SubtitleManager: Cannot play dialogue, SubtitleTextUI is missing.", this);
            return;
        }

        if (sourceToPlayFrom == null)
        {
            Debug.LogWarning("SubtitleManager: 'sourceToPlayFrom' is null. Cannot play audio.", this);
            return;
        }

        if (clipToPlay == null)
        {
            Debug.LogWarning("SubtitleManager: 'clipToPlay' is null. Cannot play audio.", this);
            return;
        }

        // If other subtitles are currently showing (even from a different source), stop them and hide.
        if (isShowingSubtitles && currentAudioSource != null && currentAudioSource.isPlaying)
        {
            currentAudioSource.Stop();
        }
        // HideSubtitles() will be called by Update if audio stopped, or immediately if we start new audio.
        // Forcing a hide here ensures clean transition if rapidly calling PlayDialogue.
        HideSubtitlesInstantly();


        // Set up the new audio and subtitle
        currentAudioSource = sourceToPlayFrom;
        currentAudioSource.clip = clipToPlay; // Ensure the correct clip is on the source

        subtitleTextUI.text = textToShow;
        subtitleTextUI.gameObject.SetActive(true);

        currentAudioSource.Play();
        isShowingSubtitles = true;
    }

    /// <summary>
    /// Hides the subtitles immediately.
    /// </summary>
    private void HideSubtitlesInstantly()
    {
        if (subtitleTextUI != null)
        {
            subtitleTextUI.text = "";
            subtitleTextUI.gameObject.SetActive(false);
        }
        isShowingSubtitles = false;
        // currentAudioSource is not nulled here, as Update() might still need to check it
        // or a new PlayDialogue call will reassign it.
    }

    /// <summary>
    /// Called by Update when audio naturally finishes or source becomes invalid.
    /// </summary>
    private void HideSubtitles()
    {
        HideSubtitlesInstantly();
        currentAudioSource = null; // Clear the reference now that it's truly done or invalid
    }

    /// <summary>
    /// Call this to immediately stop any playing dialogue and hide subtitles.
    /// </summary>
    public void StopAndHideDialogue()
    {
        if (currentAudioSource != null && currentAudioSource.isPlaying)
        {
            currentAudioSource.Stop();
        }
        HideSubtitles(); // This will hide UI and set isShowingSubtitles to false
    }
}*/

//Updated
using UnityEngine;
using TMPro;
using System.Collections; // Required for IEnumerator

public class SubtitleManager : MonoBehaviour
{
    [Header("UI Reference")]
    [Tooltip("The TextMeshProUGUI element used to display subtitles.")]
    public TextMeshProUGUI subtitleTextUI;

    private AudioSource currentAudioSource;
    private bool isShowingSubtitles = false;
    private Coroutine activeTimedSubtitleCoroutine; // To manage timed subtitles

    void Awake()
    {
        if (subtitleTextUI == null)
        {
            Debug.LogError("SubtitleManager: Subtitle Text UI not assigned! Subtitles will not work.", this);
            enabled = false;
            return;
        }
        subtitleTextUI.gameObject.SetActive(false);
        subtitleTextUI.text = "";
    }

    void Update()
    {
        // This part is mainly for audio-linked subtitles
        if (isShowingSubtitles && currentAudioSource != null) // Check if currentAudioSource is not null
        {
            if (!currentAudioSource.isPlaying)
            {
                HideSubtitles();
            }
        }
        // If isShowingSubtitles is true but currentAudioSource is null,
        // it means a timed subtitle is active and its coroutine will handle hiding it.
    }

    public void PlayDialogue(AudioSource sourceToPlayFrom, AudioClip clipToPlay, string textToShow)
    {
        if (subtitleTextUI == null)
        {
            Debug.LogWarning("SubtitleManager: Cannot play dialogue, SubtitleTextUI is missing.", this);
            return;
        }
        if (sourceToPlayFrom == null || clipToPlay == null)
        {
            Debug.LogWarning("SubtitleManager: AudioSource or AudioClip missing for PlayDialogue.", this);
            return;
        }

        // Stop and clear any existing subtitle (audio or timed)
        StopAndHideDialogue(); // This will also stop any active timed coroutine

        currentAudioSource = sourceToPlayFrom;
        currentAudioSource.clip = clipToPlay;

        subtitleTextUI.text = textToShow;
        subtitleTextUI.gameObject.SetActive(true);
        currentAudioSource.Play();
        isShowingSubtitles = true;
    }

    /// <summary>
    /// Shows a subtitle for a specific duration, not linked to an AudioSource.
    /// </summary>
    /// <param name="textToShow">The subtitle text.</param>
    /// <param name="duration">How long to show the text in seconds.</param>
    public void ShowTimedSubtitle(string textToShow, float duration)
    {
        if (subtitleTextUI == null)
        {
            Debug.LogWarning("SubtitleManager: Cannot show timed subtitle, SubtitleTextUI is missing.", this);
            return;
        }

        // Stop and clear any existing subtitle (audio or timed)
        StopAndHideDialogue(); // This ensures a clean slate

        subtitleTextUI.text = textToShow;
        subtitleTextUI.gameObject.SetActive(true);
        isShowingSubtitles = true;
        currentAudioSource = null; // Indicate this is not an audio-linked subtitle

        activeTimedSubtitleCoroutine = StartCoroutine(HideSubtitleAfterDurationCoroutine(duration));
    }

    private IEnumerator HideSubtitleAfterDurationCoroutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        // Only hide if no new audio dialogue has started over it.
        // The currentAudioSource == null check is key here.
        // If PlayDialogue was called, currentAudioSource would no longer be null.
        if (isShowingSubtitles && currentAudioSource == null)
        {
            HideSubtitlesInstantly(); // Use instant hide for timed ones
        }
        activeTimedSubtitleCoroutine = null;
    }

    private void HideSubtitlesInstantly()
    {
        if (subtitleTextUI != null)
        {
            subtitleTextUI.text = "";
            subtitleTextUI.gameObject.SetActive(false);
        }
        // Don't set isShowingSubtitles or currentAudioSource here,
        // let the calling context manage that.
        // However, if a timed coroutine was specifically for THIS hiding action,
        // it should have already nulled itself.
        // This is more of a "force clear UI" method.
    }

    private void HideSubtitles() // This is generally called when audio stops or for explicit stop
    {
        if (subtitleTextUI != null)
        {
            subtitleTextUI.text = "";
            subtitleTextUI.gameObject.SetActive(false);
        }
        isShowingSubtitles = false;
        currentAudioSource = null; // Clear audio source reference

        // If a timed coroutine was running, ensure it's stopped
        // (though its own logic should handle completion)
        if (activeTimedSubtitleCoroutine != null)
        {
            StopCoroutine(activeTimedSubtitleCoroutine);
            activeTimedSubtitleCoroutine = null;
        }
    }

    public void StopAndHideDialogue()
    {
        // Stop audio if it's playing
        if (currentAudioSource != null && currentAudioSource.isPlaying)
        {
            currentAudioSource.Stop();
        }

        // Stop any timed subtitle coroutine
        if (activeTimedSubtitleCoroutine != null)
        {
            StopCoroutine(activeTimedSubtitleCoroutine);
            activeTimedSubtitleCoroutine = null;
        }

        // Hide UI and reset state
        HideSubtitles(); // This now handles resetting flags
    }
}