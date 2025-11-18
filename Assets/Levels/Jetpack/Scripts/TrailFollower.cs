using UnityEngine;

/// <summary>
/// Helper component that makes a trail segment follow the player while flying,
/// then persists independently for the full trail duration before destroying itself.
/// </summary>
public class TrailFollower : MonoBehaviour
{
    private Transform playerTransform;
    private float lifetime;
    private float timer;
    private bool isFollowing = true;
    
    public void Initialize(Transform player, float duration)
    {
        playerTransform = player;
        lifetime = duration;
        timer = 0f;
    }
    
    void Update()
    {
        if (playerTransform == null)
        {
            Destroy(gameObject);
            return;
        }
        
        // Follow the player while trail is emitting
        if (isFollowing)
        {
            transform.position = playerTransform.position;
            
            // Check if trail stopped emitting
            TrailRenderer trail = GetComponent<TrailRenderer>();
            if (trail != null && !trail.emitting)
            {
                // Stop following, let trail persist in place
                isFollowing = false;
                timer = 0f;
            }
        }
        else
        {
            // Trail is no longer emitting, count down to destruction
            timer += Time.deltaTime;
            
            if (timer >= lifetime)
            {
                // Trail has completed its lifetime, destroy this segment
                Destroy(gameObject);
            }
        }
    }
}
