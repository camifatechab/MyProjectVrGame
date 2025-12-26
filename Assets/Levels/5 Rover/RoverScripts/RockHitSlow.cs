using UnityEngine;

public class RockHitSlow : MonoBehaviour
{
    public float slowMultiplier = 0.5f; // 50% speed
    public float slowDuration = 1.5f;   // how long rover is slowed

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Rover"))
        {
            RoverController rover = collision.collider.GetComponent<RoverController>();

            if (rover != null)
            {
                rover.ApplyTerrainSlowdown(slowMultiplier, slowDuration);
            }
        }
    }
}
