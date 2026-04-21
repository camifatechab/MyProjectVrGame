using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroSequenceManager : MonoBehaviour
{
    /*[Header("Audio")]
    public AudioSource audioSource;
    public AudioClip playerIntroVoice;
    public AudioClip computerBeep;
    public AudioClip playerAcceptVoice;

    [Header("Mission Log")]
    public TypewriterEffect missionLog; // your previous script

    [Header("Crystal Visual")]
    public GameObject crystalVisual; // assign crystal UI or 3D object

    [Header("Teleporter")]
    public GameObject teleporterTrigger; // collider or trigger zone
    public AudioSource teleporterAudio;
    public ParticleSystem teleportParticles;

    [Header("Scene")]
    public string nextSceneName;

    void Start()
    {
        teleporterTrigger.SetActive(false);
        crystalVisual.SetActive(false);

        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        // 1️ Player intro voice
        audioSource.PlayOneShot(playerIntroVoice);

        yield return new WaitForSeconds(3f);

        // 2️ Computer beep
        audioSource.PlayOneShot(computerBeep);

        // 3️ Start mission log pages
        missionLog.StartTyping();

        // Wait until all pages are done
        yield return new WaitUntil(() => missionLog.IsFinished());

        // 4️ Show crystal (during or after page 3–4)
        crystalVisual.SetActive(true);

        yield return new WaitForSeconds(2f);

        // 5️ Enable teleporter
        teleporterTrigger.SetActive(true);

        // 6️ Player accepts mission
        audioSource.PlayOneShot(playerAcceptVoice);
    }

    // Called by teleporter
    public void ActivateTeleport()
    {
        StartCoroutine(TeleportSequence());
    }

    IEnumerator TeleportSequence()
    {
        teleporterAudio.Play();
        teleportParticles.Play();

        yield return new WaitForSeconds(1.5f);

        SceneManager.LoadScene(nextSceneName);
    }*/

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip playerIntroVoice;
    public AudioClip computerBeep;
    public AudioClip playerAcceptVoice;

    [Header("Mission Log")]
    public TypewriterEffect missionLog;

    [Header("Crystal")]
    public GameObject crystalVisual;

    [Header("Teleporter")]
    public GameObject teleporterTrigger;
    public AudioSource teleporterAudio;
    public ParticleSystem teleportParticles;

    [Header("Scene")]
    public string nextSceneName;

    void Start()
    {
        teleporterTrigger.SetActive(false);
        crystalVisual.SetActive(false);

        missionLog.onPageStart += HandlePageEvents;

        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        //Player intro
        audioSource.PlayOneShot(playerIntroVoice);

        yield return new WaitForSeconds(3f);

        //Computer beep
        audioSource.PlayOneShot(computerBeep);

        //Start typing
        missionLog.StartTyping();

        yield return new WaitUntil(() => missionLog.IsFinished());

        //Player accepts
        audioSource.PlayOneShot(playerAcceptVoice);

        //Enable teleporter AFTER mission
        teleporterTrigger.SetActive(true);
    }

    void HandlePageEvents(int pageIndex)
    {
        /*if (pageIndex == 3) // page 4
        {
            crystalVisual.SetActive(true);
        }*/
        // Page 4 (index 3) → show crystal
        if (pageIndex == 3)
        {
            crystalVisual.SetActive(true);
        }
        // Page 5 (index 4) → hide crystal
        else if (pageIndex == 4)
        {
            crystalVisual.SetActive(false);
        }
    }

    public void ActivateTeleport()
    {
        StartCoroutine(TeleportSequence());
    }

    IEnumerator TeleportSequence()
    {
        teleporterAudio.Play();
        teleportParticles.Play();

        yield return new WaitForSeconds(1.5f);

        SceneManager.LoadScene(nextSceneName);
    }
}