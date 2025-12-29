using UnityEngine;

/// <summary>
/// Simple visual tilt for rover body. Plug inputs from your RoverController:
/// - turnInput: -1 (left) to +1 (right)
/// - accelInput: -1 (brake) to +1 (accelerate)
/// - speed: current forward speed (m/s)
/// </summary>
public class RoverSuspensionTilt : MonoBehaviour
{
    [Header("Inputs (set these from RoverController)")]
    [Range(-1f, 1f)] public float turnInput = 0f;
    [Range(-1f, 1f)] public float accelInput = 0f;
    public float speed = 0f;

    [Header("Tilt Settings")]
    public float maxTurnTilt = 4f;         // degrees (roll)
    public float maxAccelTilt = 3f;        // degrees (pitch)
    public float tiltReturnSpeed = 5f;     // how fast it lerps to target
    public float speedAffectsTilt = 0.07f; // multiplier on speed to shape tilt

    Quaternion baseRotation;

    void Start()
    {
        baseRotation = transform.localRotation;
    }

    void Update()
    {
        // small speed factor: keeps tilt small at low speeds, slightly larger at high speeds
        float speedFactor = Mathf.Clamp01(speed * speedAffectsTilt);

        // compute target angles
        float roll = -turnInput * maxTurnTilt * speedFactor; // Z axis roll
        float pitch = -accelInput * maxAccelTilt;           // X axis pitch (negative to tilt back on accel)

        Quaternion target = baseRotation * Quaternion.Euler(pitch, 0f, roll);

        transform.localRotation = Quaternion.Lerp(transform.localRotation, target, Time.deltaTime * tiltReturnSpeed);
    }
}
