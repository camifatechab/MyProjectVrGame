using UnityEngine;
using UnityEngine.InputSystem;

public class WheelTurn : MonoBehaviour
{
    [Header("Steering Input")]
    public InputActionProperty steerAction; // assign your same Left Stick (Vector2)

    [Header("Wheel Settings")]
    public Transform[] wheels; // assign wheel child objects here
    public float turnAngle = 25f; // max visual rotation angle
    public float smoothSpeed = 5f; // smooth turning speed

    private float currentAngle;

    void Update()
    {
        Vector2 steer = steerAction.action?.ReadValue<Vector2>() ?? Vector2.zero;
        float targetAngle = steer.x * turnAngle;

        currentAngle = Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime * smoothSpeed);

        foreach (Transform wheel in wheels)
        {
            if (wheel)
                wheel.localRotation = Quaternion.Euler(-90f, 0f, currentAngle); // turn on Z axis
        }
    }
}
