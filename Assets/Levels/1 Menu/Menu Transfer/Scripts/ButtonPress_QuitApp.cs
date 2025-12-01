using UnityEngine;

public class ButtonPress_QuitApp : MonoBehaviour
{
    [HideInInspector] public bool isPressed = false;

    private Material originalMaterial;
    public Material pressedMaterial;
    private Renderer rend;

    public AudioSource pressSound;
    public FadeScreen fadeScreen;
    public float fadeDelay = 1.0f;

    void Start()
    {
        rend = GetComponentInChildren<Renderer>();

        if (rend != null)
            originalMaterial = rend.material;

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

        Invoke(nameof(QuitApp), fadeDelay);
    }

    private void QuitApp()
    {
/*#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else*/
        Application.Quit();
//#endif
    }

    public void ResetButton()
    {
        isPressed = false;

        if (rend != null)
            rend.material = originalMaterial;
    }
}
