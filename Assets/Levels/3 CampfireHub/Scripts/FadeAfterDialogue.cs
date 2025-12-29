using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class FadeAfterDialogue : MonoBehaviour
{
    [Header("References")]
    public AudioSource dialogueSource;
    public FadeScreen fadeScreen;

    [Header("Scene Transition")]
    public string nextSceneName;
    public float delayAfterDialogue = 0.5f;
    public float delayAfterFade = 1.5f;

    private bool hasTriggered = false;

    void Start()
    {
        if (fadeScreen == null)
            fadeScreen = FindFirstObjectByType<FadeScreen>();
    }

    void Update()
    {
        if (hasTriggered) return;

        // Dialogue finished playing
        if (dialogueSource != null &&
            !dialogueSource.isPlaying &&
            dialogueSource.time > 0f)
        {
            hasTriggered = true;
            StartCoroutine(FadeAndLoad());
        }
    }

    private IEnumerator FadeAndLoad()
    {
        // Small pause after dialogue
        yield return new WaitForSeconds(delayAfterDialogue);

        // Fade to black
        if (fadeScreen)
            fadeScreen.FadeOut();

        // Wait for fade to finish
        yield return new WaitForSeconds(delayAfterFade);

        // Load next scene
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
        else
            Debug.LogWarning("FadeAfterDialogue: No nextSceneName set!");
    }
}
