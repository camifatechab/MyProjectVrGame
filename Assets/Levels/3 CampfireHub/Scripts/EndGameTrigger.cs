using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class EndGameTrigger : MonoBehaviour
{
    [Header("Trigger")]
    public string playerTag = "Player";

    [Header("Fade")]
    public FadeScreen fadeScreen;

    [Header("Fade Sound")]
    public AudioSource audioSource;     // 2D AudioSource
    public AudioClip fadeSound;

    [Header("UI Message")]
    public TMP_Text endMessage;
    public string messageText = "The End!";
    public float showTextDelay = 0.2f;

    [Header("Scene")]
    public float returnDelay = 5f;

    private bool triggered = false;

    private void Start()
    {
        if (endMessage)
        {
            endMessage.gameObject.SetActive(true);   // MUST be active
            endMessage.text = "";
            endMessage.alpha = 0f;                   // hidden safely
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag(playerTag)) return;

        triggered = true;
        StartCoroutine(EndSequence());
    }

    private IEnumerator EndSequence()
    {
        // --- Play fade sound ---
        if (audioSource && fadeSound)
            audioSource.PlayOneShot(fadeSound);

        // --- Fade to black ---
        if (fadeScreen)
            fadeScreen.FadeOut();
        else
            Debug.LogWarning("FadeScreen not assigned!");

        // Small delay so fade starts first
        yield return new WaitForSeconds(showTextDelay);

        // --- Show text ---
        if (endMessage)
        {
            endMessage.text = messageText;
            endMessage.alpha = 1f;   // FORCE visible
        }
        else
        {
            Debug.LogWarning("End message TMP not assigned!");
        }

        // --- Wait ---
        yield return new WaitForSeconds(returnDelay);

        // --- Load Scene 0 ---
        SceneManager.LoadScene(0);
    }
}
