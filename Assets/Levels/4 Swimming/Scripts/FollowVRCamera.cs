using UnityEngine;

public class FollowVRCamera : MonoBehaviour
{
    private Transform vrCamera;
    
    [SerializeField] private Vector3 offset = new Vector3(0.3f, 0.15f, 0.5f);
    
void Start()
    {
        vrCamera = Camera.main.transform;
        
        if (vrCamera != null)
        {
            // Parent canvas to VR camera
            transform.SetParent(vrCamera, false);
            transform.localPosition = offset;
            transform.localRotation = Quaternion.identity;
            
            // Make canvas smaller and more comfortable
            transform.localScale = Vector3.one * 0.0005f;
            
            // CRITICAL FIX: Assign camera reference for VR rendering
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.worldCamera = Camera.main;
                Debug.Log("Canvas attached to VR camera and worldCamera assigned!");
            }
            else
            {
                Debug.LogError("Canvas component not found!");
            }
        }
        else
        {
            Debug.LogError("Main Camera not found!");
        }
    }
}
