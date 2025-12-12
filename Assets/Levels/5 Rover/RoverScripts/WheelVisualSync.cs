using UnityEngine;

public class WheelVisualSync : MonoBehaviour
{
    public WheelCollider wheelCollider;
    public Transform wheelVisual;

    void Update()
    {
        if (!wheelCollider || !wheelVisual) return;

        wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);

        wheelVisual.position = pos;
        wheelVisual.rotation = rot;
    }
}
