using System.Collections;
using UnityEngine;

public class DialogueTriggerZone : MonoBehaviour
{
    [Header("Dialogue Audio")]
    public AudioSource audioSource;
    public AudioClip[] dialogueClips;

    [Header("Settings")]
    public bool playOnlyOnce = true;
    public float delayBetweenLines = 0.5f;

    private bool hasPlayed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (playOnlyOnce && hasPlayed) return;

        Debug.Log("Dialogue triggered by: " + other.name);

        hasPlayed = true;
        StartCoroutine(PlayDialogue());
    }

    IEnumerator PlayDialogue()
    {
        foreach (AudioClip clip in dialogueClips)
        {
            if (clip == null) continue;

            Debug.Log("Playing dialogue: " + clip.name);

            /*audioSource.clip = clip;
            audioSource.Play();*/
            audioSource.PlayOneShot(clip);

            /* Wait until clip finishes
            //yield return new WaitForSeconds(clip.length);

            // Small gap between lines
            //yield return new WaitForSeconds(delayBetweenLines);*/
            yield return new WaitForSeconds(clip.length + delayBetweenLines);
        }
    }
}