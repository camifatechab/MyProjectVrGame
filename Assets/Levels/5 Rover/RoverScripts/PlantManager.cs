/*using UnityEngine;
using TMPro;

public class PlantManager : MonoBehaviour
{
    public static PlantManager Instance;
    public TMP_Text uiText;
    //Added 10/30/2025
    public FadeScreen fadeScreen; 

    private int totalPlants;
    private int collected = 0;
    //Added 10/30/2025
    private bool allCollected = false;

    void Awake()
    {
        Instance = this;
        totalPlants = FindObjectsByType<CollectiblePlant>(FindObjectsSortMode.None).Length;
        UpdateUI();
    }

    public void CollectPlant()
    {
        /*collected++;
        UpdateUI();

        if (allCollected) return; // safety check

        collected++;
        UpdateUI();

        //Trigger fade when all plants are collected
        if (collected >= totalPlants)
        {
            allCollected = true;
            Debug.Log("All plants collected! Fading out...");

            if (fadeScreen != null)
                fadeScreen.FadeOut(); // or FadeIn(), depending on your fade direction
            else
                Debug.LogWarning("No FadeScreen assigned to PlantManager!");
        }
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
}