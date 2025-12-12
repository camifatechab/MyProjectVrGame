using UnityEngine;

[System.Serializable]
public class WheelSetup
{
    public WheelCollider collider;
    public Transform mesh;
}

public class WheelVisualUpdater : MonoBehaviour
{
    public WheelSetup[] wheels;

    void LateUpdate()
    {
        foreach (var w in wheels)
        {
            if (w.collider == null || w.mesh == null) continue;

            Vector3 pos;
            Quaternion rot;

            w.collider.GetWorldPose(out pos, out rot);

            w.mesh.position = pos;
            w.mesh.rotation = rot;
        }
    }
}
