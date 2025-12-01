using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SlowTerrain : MonoBehaviour
{
    [Range(0.1f, 1f)]
    public float speedMultiplier = 0.5f; // How much to slow down
    public float slowdownDuration = 2f; // How long the slowdown lasts after exiting
    public AudioClip enterSound;

    /*private void OnTriggerEnter(Collider other)
    {
        RoverController rover = other.GetComponentInParent<RoverController>();
        if (rover)
        {
            rover.ApplyTerrainSlowdown(speedMultiplier, slowdownDuration);

            if (enterSound)
                AudioSource.PlayClipAtPoint(enterSound, transform.position, 0.8f);
        }
        Debug.Log("Entered slow terrain: " + other.name);
    }*/

    private void OnCollisionEnter(Collision collision)
    {
        RoverController rover = collision.gameObject.GetComponentInParent<RoverController>();
        if (rover)
        {
            rover.ApplyTerrainSlowdown(speedMultiplier, slowdownDuration);

            if (enterSound)
                AudioSource.PlayClipAtPoint(enterSound, transform.position, 0.8f);

            Debug.Log("Entered slow terrain (collision): " + collision.gameObject.name);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        RoverController rover = collision.gameObject.GetComponentInParent<RoverController>();
        if (rover)
        {
            // Restore to normal once leaving the terrain
            rover.ApplyTerrainSlowdown(1f, 0f);
            Debug.Log("Exited slow terrain: " + collision.gameObject.name);
        }
    }
}
