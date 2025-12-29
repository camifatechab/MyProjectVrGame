using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class MachinePartCollect : MonoBehaviour
{
    [Header("Subtitle")]
    public TMP_Text subtitleText;
    [TextArea] public string dialogueLine;

    [Header("Dialogue Audio")]
    public AudioSource dialogueSource;
    public AudioClip dialogueClip;

    [Header("Scene Transition")]
    public FadeScreen fadeScreen;
    public string nextSceneName = "NextLevel";
    public float delayAfterFade = 1.5f;

    private bool triggered = false;

    // 🔹 THIS is what SpaceshipPiece will call
    public void OnPartCollected()
    {
        if (triggered) return;
        triggered = true;

        StartCoroutine(CollectSequence());
    }

    private IEnumerator CollectSequence()
    {
        // Subtitle
        if (subtitleText)
            subtitleText.text = dialogueLine;

        float waitTime = 1f;

        // Audio
        if (dialogueSource && dialogueClip)
        {
            dialogueSource.PlayOneShot(dialogueClip);
            waitTime = dialogueClip.length;
        }

        yield return new WaitForSeconds(waitTime);

        // Fade to black
        if (fadeScreen)
            fadeScreen.FadeOut();

        yield return new WaitForSeconds(delayAfterFade);

        // Scene change
        SceneManager.LoadScene(nextSceneName);
    }
}