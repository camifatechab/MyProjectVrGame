/*using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public DialogueLine[] dialogueLines;

    private bool triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        if (other.CompareTag("Player"))
        {
            DialogueManager.Instance.StartDialogue(dialogueLines);
            triggered = true;
        }
    }
}*/
/*using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public AudioSource dialogueSource;
    public AudioClip dialogueClip;
    private bool hasPlayed = false;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Entered trigger with: " + other.name);

        if (!hasPlayed && other.CompareTag("Player"))
        {
            Debug.Log("Player entered. Trying to play dialogue.");

            if (dialogueSource != null && dialogueClip != null)
            {
                dialogueSource.clip = dialogueClip;
                dialogueSource.Play();
                Debug.Log("Dialogue played: " + dialogueClip.name);
                hasPlayed = true;
            }
            else
            {
                Debug.LogWarning("Missing AudioSource or AudioClip.");
            }
        }
    }
}*/

//Update to enable subtitles
using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource dialogueSource;
    public AudioClip dialogueClip;

    [Header("Subtitle Settings")]
    [TextArea(3, 5)] // Makes it a nice multiline text box in inspector
    public string subtitleText = "Your subtitle text here...";
    public SubtitleManager subtitleManager; // Assign your SubtitleManager instance

    private bool hasPlayed = false;

    void Start()
    {
        // Attempt to find SubtitleManager if not assigned (optional, but helpful)
        if (subtitleManager == null)
        {
            subtitleManager = FindFirstObjectByType<SubtitleManager>();
            if (subtitleManager == null)
            {
                Debug.LogError($"DialogueTrigger on {gameObject.name}: SubtitleManager not assigned and could not be found! Subtitles will not work.", this);
            }
        }
        if (dialogueSource == null)
        {
            // If dialogueSource is not set, try to get it from this GameObject
            dialogueSource = GetComponent<AudioSource>();
            if (dialogueSource == null)
            {
                Debug.LogError($"DialogueTrigger on {gameObject.name}: AudioSource not assigned and none found on this GameObject.", this);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Debug.Log("Entered trigger with: " + other.name); // Already have this

        if (!hasPlayed && other.CompareTag("Player"))
        {
            // Debug.Log("Player entered. Trying to play dialogue."); // Already have this

            if (dialogueSource != null && dialogueClip != null && subtitleManager != null)
            {
                // Let the SubtitleManager handle playing the audio and showing the text
                subtitleManager.PlayDialogue(dialogueSource, dialogueClip, subtitleText);
                Debug.Log("Dialogue and subtitle initiated via SubtitleManager: " + dialogueClip.name);
                hasPlayed = true; // Set hasPlayed after successfully starting
            }
            else
            {
                if (subtitleManager == null) Debug.LogWarning($"DialogueTrigger on {gameObject.name}: SubtitleManager reference is missing.");
                if (dialogueSource == null) Debug.LogWarning($"DialogueTrigger on {gameObject.name}: Missing AudioSource.");
                if (dialogueClip == null) Debug.LogWarning($"DialogueTrigger on {gameObject.name}: Missing AudioClip.");
            }
        }
    }
}