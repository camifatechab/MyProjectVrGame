/*using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlantManager : MonoBehaviour
{
    public static PlantManager Instance;
    public TMP_Text uiText;
    public FadeScreen fadeScreen; // Assign in Inspector
    public string nextSceneName = "NextLevel"; // change in Inspector
    public float delayBeforeSceneLoad = 2f; // seconds after fade

    private int totalPlants;
    private int collected = 0;
    private bool allCollected = false;

    void Awake()
    {
        Instance = this;
        totalPlants = FindObjectsByType<CollectiblePlant>(FindObjectsSortMode.None).Length;
        UpdateUI();
    }

    public void CollectPlant()
    {
        if (allCollected) return;

        collected++;
        UpdateUI();

        //Trigger fade and transition when all plants are collected
        if (collected >= totalPlants)
        {
            allCollected = true;
            Debug.Log("All plants collected! Fading out and loading next scene...");

            if (fadeScreen != null)
                StartCoroutine(FadeAndLoad());
            else
                Debug.LogWarning("No FadeScreen assigned to PlantManager!");
        }
    }

    private IEnumerator FadeAndLoad()
    {
        fadeScreen.FadeOut(); // fade to white
        yield return new WaitForSeconds(delayBeforeSceneLoad);
        SceneManager.LoadScene(nextSceneName);
    }

    void UpdateUI()
    {
        if (uiText)
            uiText.text = $"{collected}/{totalPlants}";
    }
}*/

using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlantManager : MonoBehaviour
{
    public static PlantManager Instance;

    [Header("UI")]
    public TMP_Text uiText;                 // plant counter
    public TMP_Text subtitleText;           // NEW: subtitle canvas text

    [Header("Dialogue")]
    public AudioSource dialogueSource;      // NEW: AudioSource (2D)
    public AudioClip finalDialogueClip;     // NEW: voice line
    [TextArea]
    public string finalDialogueLine;        // NEW: subtitle text

    [Header("Scene Transition")]
    public FadeScreen fadeScreen;
    public string nextSceneName = "NextLevel";
    public float delayBeforeSceneLoad = 2f;

    private int totalPlants;
    private int collected = 0;
    private bool allCollected = false;

    void Awake()
    {
        Instance = this;
        totalPlants = FindObjectsByType<CollectiblePlant>(FindObjectsSortMode.None).Length;
        UpdateUI();

        if (subtitleText)
            subtitleText.text = ""; // start empty
    }

    public void CollectPlant()
    {
        if (allCollected) return;

        collected++;
        UpdateUI();

        if (collected >= totalPlants)
        {
            allCollected = true;
            Debug.Log("All plants collected!");

            StartCoroutine(FinalSequence());
        }
    }

    private IEnumerator FinalSequence()
    {
        // --- Show subtitle ---
        if (subtitleText)
            subtitleText.text = finalDialogueLine;

        // --- Play dialogue ---
        float waitTime = delayBeforeSceneLoad;

        if (dialogueSource && finalDialogueClip)
        {
            dialogueSource.PlayOneShot(finalDialogueClip);
            waitTime = finalDialogueClip.length;
        }

        // Wait for dialogue to finish
        yield return new WaitForSeconds(waitTime);

        // --- Fade and load next scene ---
        if (fadeScreen)
            fadeScreen.FadeOut();

        yield return new WaitForSeconds(delayBeforeSceneLoad);
        SceneManager.LoadScene(nextSceneName);
    }

    void UpdateUI()
    {
        if (uiText)
            uiText.text = $"{collected}/{totalPlants}";
    }
}
