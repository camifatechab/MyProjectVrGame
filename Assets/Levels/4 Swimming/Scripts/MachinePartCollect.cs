using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class MachinePartCollect : MonoBehaviour
{
    [Header("Subtitle")]
    public TMP_Text subtitleText;          // Subtitle canvas text
    [TextArea]
    public string dialogueLine;

    [Header("Dialogue Audio")]
    public AudioSource dialogueSource;     // 2D AudioSource
    public AudioClip dialogueClip;

    [Header("Scene Transition")]
    public FadeScreen fadeScreen;
    public string nextSceneName = "NextLevel";
    public float delayAfterDialogue = 1.5f;

    private bool collected = false;

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        if (other.CompareTag("Player"))
        {
            collected = true;
            StartCoroutine(CollectSequence());
        }
    }

    private IEnumerator CollectSequence()
    {
        // Disable the part visually
        gameObject.SetActive(false);

        // Show subtitle
        if (subtitleText)
            subtitleText.text = dialogueLine;

        float waitTime = delayAfterDialogue;

        // Play dialogue
        if (dialogueSource && dialogueClip)
        {
            dialogueSource.PlayOneShot(dialogueClip);
            waitTime = dialogueClip.length;
        }

        // Wait for dialogue
        yield return new WaitForSeconds(waitTime);

        // Fade out
        if (fadeScreen)
            fadeScreen.FadeOut();

        yield return new WaitForSeconds(delayAfterDialogue);

        // Load next scene
        SceneManager.LoadScene(nextSceneName);
    }
}
