using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonPress_LoadScene : MonoBehaviour
{
    [HideInInspector] public bool isPressed = false;

    private Material originalMaterial;
    public Material pressedMaterial;
    private Renderer rend;

    public AudioSource pressSound;

    public FadeScreen fadeScreen;
    public string sceneToLoad;
    public float fadeDelay = 1.0f;

    void Start()
    {
        // Find renderer on this object OR children
        rend = GetComponentInChildren<Renderer>();

        if (rend != null)
            originalMaterial = rend.material;
        else
            Debug.LogWarning("ButtonPress_LoadScene: No Renderer found on button or its children.");

        isPressed = false;
    }

    public void PressButton()
    {
        if (isPressed) return;
        isPressed = true;

        if (rend != null && pressedMaterial != null)
            rend.material = pressedMaterial;

        if (pressSound != null)
            pressSound.Play();

        if (fadeScreen != null)
            fadeScreen.FadeOut();

        if (!string.IsNullOrEmpty(sceneToLoad))
            Invoke(nameof(LoadScene), fadeDelay);
    }

    private void LoadScene()
    {
        SceneManager.LoadScene(sceneToLoad);
    }

    public void ResetButton()
    {
        isPressed = false;

        if (rend != null)
            rend.material = originalMaterial;
    }
}
