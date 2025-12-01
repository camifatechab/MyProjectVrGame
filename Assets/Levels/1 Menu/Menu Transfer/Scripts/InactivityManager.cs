using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // New Input System (if you're using it)

public class InactivityManager : MonoBehaviour
{
    public float inactivityDuration = 120f; // 2 minutes
    private float inactivityTimer;

    void Start()
    {
        inactivityTimer = 0f;
    }

    void Update()
    {
        if (IsPlayerActive())
        {
            inactivityTimer = 0f; // Reset timer
        }
        else
        {
            inactivityTimer += Time.deltaTime;
            if (inactivityTimer >= inactivityDuration)
            {
                SceneManager.LoadScene(0); // Go back to Scene 0
            }
        }
    }

    bool IsPlayerActive()
    {
        // Check keyboard/mouse/gamepad activity
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            return true;

        if (Mouse.current != null && Mouse.current.delta.ReadValue() != Vector2.zero)
            return true;

        if (Gamepad.current != null && Gamepad.current.leftStick.ReadValue() != Vector2.zero)
            return true;

        // Optionally check for XR controller movement
        // Can also check if the headset has moved:
        if (XRHeadMoved()) return true;

        return false;
    }

    bool XRHeadMoved()
    {
        if (Camera.main == null) return false;

        // Detect if the XR camera moved significantly
        float movementThreshold = 0.01f;
        Vector3 currentPosition = Camera.main.transform.position;

        if (!lastHeadPos.HasValue)
        {
            lastHeadPos = currentPosition;
            return false;
        }

        float distance = Vector3.Distance(lastHeadPos.Value, currentPosition);
        lastHeadPos = currentPosition;
        return distance > movementThreshold;
    }

    private Vector3? lastHeadPos = null;
}
