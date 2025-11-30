using UnityEngine;

public class SetCameraSkybox : MonoBehaviour
{
    private void Start()
    {
        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.Skybox;
        }
    }
}
