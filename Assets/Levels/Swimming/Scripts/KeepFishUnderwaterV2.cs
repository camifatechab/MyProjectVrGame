using UnityEngine;

public class KeepFishUnderwaterV2 : MonoBehaviour
{
    [Header("Water Boundaries - Deeper Container")]
    // [SerializeField] private float waterSurfaceY = 0f; // Not currently used
    [SerializeField] private float maxHeightBelowSurface = -2f; // Stay 2 units below surface
    [SerializeField] private float minHeight = -18f; // Floor at Y=-20
    
    private void LateUpdate()
    {
        Vector3 pos = transform.position;
        
        if (pos.y > maxHeightBelowSurface)
        {
            pos.y = maxHeightBelowSurface;
            transform.position = pos;
            
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 vel = rb.linearVelocity;
                vel.y = -Mathf.Abs(vel.y) * 2f;
                
                float horizontalSpeed = new Vector2(vel.x, vel.z).magnitude;
                if (horizontalSpeed < 2f)
                {
                    vel.x += UnityEngine.Random.Range(-1f, 1f);
                    vel.z += UnityEngine.Random.Range(-1f, 1f);
                }
                rb.linearVelocity = vel;
            }
        }
        
        if (pos.y < minHeight)
        {
            pos.y = minHeight;
            transform.position = pos;
            
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null && rb.linearVelocity.y < 0)
            {
                Vector3 vel = rb.linearVelocity;
                vel.y = Mathf.Abs(vel.y);
                rb.linearVelocity = vel;
            }
        }
    }
}
