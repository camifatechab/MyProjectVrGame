using UnityEngine;

public class KeepFishUnderwater : MonoBehaviour
{
    [Header("Water Boundaries")]
    [SerializeField] private float waterSurfaceY = 0f;  // Water surface height
    [SerializeField] private float maxHeightBelowSurface = -1f; // Max Y they can reach (1 unit below surface)
    [SerializeField] private float minHeight = -9.5f; // Floor level
    
private void LateUpdate()
    {
        Vector3 pos = transform.position;
        
        // Clamp Y position to stay underwater (deeper water now)
        if (pos.y > maxHeightBelowSurface)
        {
            pos.y = maxHeightBelowSurface;
            transform.position = pos;
            
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 vel = rb.linearVelocity;
                // Push fish DOWN and increase horizontal movement
                vel.y = -Mathf.Abs(vel.y) * 2f; // Strong downward push
                // Boost horizontal speed to keep them moving sideways
                float horizontalSpeed = Mathf.Sqrt(vel.x * vel.x + vel.z * vel.z);
                if (horizontalSpeed < 2f) // If moving too slow horizontally
                {
                    float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    vel.x = Mathf.Cos(angle) * 3f; // Push sideways
                    vel.z = Mathf.Sin(angle) * 3f;
                }
                rb.linearVelocity = vel;
            }
        }
        
        // Prevent going through floor
        if (pos.y < minHeight)
        {
            pos.y = minHeight;
            transform.position = pos;
            
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null && rb.linearVelocity.y < 0)
            {
                Vector3 vel = rb.linearVelocity;
                vel.y = Mathf.Abs(vel.y); // Push up from floor
                rb.linearVelocity = vel;
            }
        }
    }
}
