using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; 
// If using the new Input System
// If using XR Interaction Toolkit's specific input actions, you might need:
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class PauseManager : MonoBehaviour
{
    [Header("Pause Menu UI")]
    [Tooltip("The parent GameObject of your pause menu UI elements.")]
    public GameObject pauseMenuUI;

    [Header("Input Settings")]
    [Tooltip("Input Action for toggling the pause menu (e.g., Menu button on controller).")]
    public InputActionReference pauseActionReference; // For New Input System

    private bool isPaused = false;

    // Store the original time scale in case it was modified by something else
    private float originalTimeScale;

    void Awake()
    {
        // Ensure the pause menu is hidden at the start
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
        else
        {
            Debug.LogError("PauseManager: Pause Menu UI not assigned!", this);
        }

        originalTimeScale = Time.timeScale; // Usually 1.0f
    }

    void OnEnable()
    {
        Debug.Log("PauseManager: OnEnable called."); // DEBUG
        if (pauseActionReference != null /*Added*/&& pauseActionReference.action != null)
        {
            pauseActionReference.action.performed += OnPauseButtonPressed;
            pauseActionReference.action.Enable();

            //Added
            if (!pauseActionReference.action.actionMap.enabled)
            {
                Debug.Log($"PauseManager: ActionMap '{pauseActionReference.action.actionMap.name}' was not enabled. Enabling it now."); // DEBUG
                pauseActionReference.action.actionMap.Enable();
            }

            // Get the binding path for logging (safer way)
            string bindingPathInfo = "No bindings found or error.";
            if (pauseActionReference.action.bindings.Count > 0)
            {
                bindingPathInfo = pauseActionReference.action.bindings[0].effectivePath; // Log first binding for simplicity
            }

            Debug.Log($"PauseManager: Attempting to subscribe to action '{pauseActionReference.action.name}' in map '{pauseActionReference.action.actionMap.name}'. First Binding Path: {bindingPathInfo}"); // DEBUG
            pauseActionReference.action.performed += OnPauseButtonPressed;
            // pauseActionReference.action.Enable(); // Enabling the action map above should cover this, but doesn't hurt.
            // Let's be explicit with the action too, just in case.
            if (!pauseActionReference.action.enabled)
            {
                Debug.Log($"PauseManager: Action '{pauseActionReference.action.name}' was not enabled. Enabling it now."); // DEBUG
                pauseActionReference.action.Enable();
            }

            Debug.Log($"PauseManager: Action '{pauseActionReference.action.name}' enabled: {pauseActionReference.action.enabled}. Subscribed to performed event."); // DEBUG
        }
        else
        {
            Debug.LogError("PauseManager: Pause Action Reference is NOT SET or its internal action is NULL!", this); // DEBUG
        }
    }

    void OnDisable()
    {
        if (pauseActionReference != null)
        {
            pauseActionReference.action.performed -= OnPauseButtonPressed;
            pauseActionReference.action.Disable();
        }
        // Ensure time is restored if the PauseManager object is disabled/destroyed while paused
        if (Time.timeScale != originalTimeScale && isPaused)
        {
            Time.timeScale = originalTimeScale;
        }
    }

    private void OnPauseButtonPressed(InputAction.CallbackContext context)
    {
        /*Added*/Debug.Log("PauseManager: OnPauseButtonPressed - INPUT DETECTED! Context phase: " + context.phase); // CRITICAL DEBUG
        TogglePause();
    }

    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        if (isPaused) return; // Already paused

        isPaused = true;
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
        }
        originalTimeScale = Time.timeScale; // Store current time scale before pausing
        Time.timeScale = 0f; // Pause game time

        // Optional: You might want to unlock cursor or enable specific UI interaction modes here
        // For VR, ensure your ray interactors (if used for UI) are active or can target the UI.
        // Usually, if the UI is on a World Space canvas with an Event Camera, XR Ray Interactors work fine.
        Debug.Log("Game Paused");
    }

    public void ResumeGame()
    {
        if (!isPaused) return; // Not paused

        isPaused = false;
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
        Time.timeScale = originalTimeScale; // Resume game time to its previous state (usually 1.0f)

        Debug.Log("Game Resumed");
    }

    // --- Button Click Handlers ---
    // These methods will be assigned to the OnClick() events of your UI buttons

    public void OnResumeButtonClicked()
    {
        ResumeGame();
    }

    public void OnBackToMenuButtonClicked()
    {
        ResumeGame(); // Unpause time before loading new scene
        SceneManager.LoadScene(0); // Assuming your main menu is scene index 0
        Debug.Log("Returning to Main Menu (Scene 0)");
    }

    public void OnQuitButtonClicked()
    {
        Debug.Log("Quitting Game...");
        // Unpause time just in case, though quitting usually handles this
        Time.timeScale = originalTimeScale;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Stops play mode in Editor
#else
        Application.Quit(); // Quits the built application
#endif
    }
}