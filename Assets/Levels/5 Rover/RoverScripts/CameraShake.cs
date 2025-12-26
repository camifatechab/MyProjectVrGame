using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private Vector3 originalPos;
    private float shakeAmount = 0.05f;
    private float shakeDuration = 0f;

    void Start()
    {
        originalPos = transform.localPosition;
    }

    void LateUpdate()
    {
        if (shakeDuration > 0)
        {
            transform.localPosition = originalPos + Random.insideUnitSphere * shakeAmount;
            shakeDuration -= Time.deltaTime;
        }
        else
        {
            transform.localPosition = originalPos;
        }
    }

    public void Shake(float amount, float duration)
    {
        shakeAmount = amount;
        shakeDuration = duration;
    }
}
