using UnityEngine;

public class RockHazard : MonoBehaviour
{
    public float slowDuration = 1.5f;   // How long player is slowed
    public float slowMultiplier = 0.5f; // 50% speed
    public float despawnTime = 5f;

    private void Start()
    {
        Destroy(gameObject, despawnTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Rover"))
        {
            PlayerSpeedController speed = collision.collider.GetComponent<PlayerSpeedController>();
            if (speed != null)
            {
                speed.ApplySlow(slowMultiplier, slowDuration);
            }

            Destroy(gameObject); // rock disappears on impact
        }
    }
}
