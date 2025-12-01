using UnityEngine;

public class SuspensionBob : MonoBehaviour
{
    [Header("Bob Settings")]
    public float bobHeight = 0.05f;  // How much it moves up/down
    public float bobSpeed = 1f;      // How fast it bobs (cycles per second)

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        float offset = Mathf.Sin(Time.time * bobSpeed * Mathf.PI * 2f) * bobHeight;
        transform.localPosition = startPos + new Vector3(0f, offset, 0f);
    }
}