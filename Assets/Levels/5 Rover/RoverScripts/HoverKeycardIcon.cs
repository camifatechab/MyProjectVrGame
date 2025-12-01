using UnityEngine;

public class HoverKeycardIcon : MonoBehaviour
{
    void Update()
    {
        transform.Rotate(0f, 50f * Time.deltaTime, 0f); // Rotate slowly
    }
}
