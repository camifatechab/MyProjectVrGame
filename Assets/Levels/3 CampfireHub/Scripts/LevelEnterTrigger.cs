using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LevelEnterTrigger : MonoBehaviour
{
    [Header("Scene Transition Settings")]
    public FadeScreen fadeScreen; // assign in Inspector
    public string nextSceneName = "Rover Level"; // set in Inspector
    public float delayBeforeSceneLoad = 5f;

    [Header("Audio")]
    public AudioClip transitionSound;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("PlayerHand") || other.CompareTag("Player"))
        {
            hasTriggered = true;
            StartCoroutine(FadeAndLoad());
        }
    }

    private IEnumerator FadeAndLoad()
    {
        if (fadeScreen != null)
            fadeScreen.FadeOut(); // uses your existing fade system

        if (transitionSound != null)
            AudioSource.PlayClipAtPoint(transitionSound, Camera.main.transform.position);

        yield return new WaitForSeconds(delayBeforeSceneLoad);

        SceneManager.LoadScene(nextSceneName);
    }
}
