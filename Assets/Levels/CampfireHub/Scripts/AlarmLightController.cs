/*using UnityEngine;

public class AlarmLightController : MonoBehaviour
{
    public Light[] alarmLights;

    public void ActivateAlarms()
    {
        foreach (var light in alarmLights)
        {
            light.enabled = true;
        }
    }

    public void DeactivateAlarms()
    {
        foreach (var light in alarmLights)
        {
            light.enabled = false;
        }
    }
}*/
using UnityEngine;

public class AlarmLightController : MonoBehaviour
{
    [Header("Lights to Pulse")]
    public Light[] alarmLights;

    [Header("Pulse Settings")]
    public float minIntensity = 0f;
    public float maxIntensity = 5f;
    public float pulseSpeed = 2f; // pulses per second

    private bool isPulsing = false;
    private float pulseTimer = 0f;

    void Update()
    {
        if (!isPulsing) return;

        // Ramp a 0→1→0 curve via Mathf.PingPong
        float t = Mathf.PingPong(pulseTimer * pulseSpeed, 1f);
        float intensity = Mathf.Lerp(minIntensity, maxIntensity, t);

        foreach (var light in alarmLights)
        {
            light.intensity = intensity;
        }

        pulseTimer += Time.deltaTime;
    }

    /// <summary> Start pulsing and enable the lights. </summary>
    public void ActivateAlarms()
    {
        isPulsing = true;
        pulseTimer = 0f;
        foreach (var light in alarmLights)
            light.enabled = true;
    }

    /// <summary> Stop pulsing and disable the lights. </summary>
    public void DeactivateAlarms()
    {
        isPulsing = false;
        foreach (var light in alarmLights)
        {
            light.enabled = false;
            light.intensity = minIntensity;
        }
    }
}

