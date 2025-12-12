using UnityEngine;

public class CockpitShake : MonoBehaviour
{
    public float shakeIntensity = 0.1f;

    void Update()
    {
        Vector3 offset = Random.insideUnitSphere * shakeIntensity;
        transform.localPosition = offset;
    }
}
