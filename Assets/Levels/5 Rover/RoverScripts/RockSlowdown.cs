using UnityEngine;

public class RockSlowdown : MonoBehaviour
{
    public float slowMultiplier = 0.6f;  // 60% speed
    public float slowDuration = 1.5f;    // slow lasts for 1.5s
    public AudioClip impactSound;
    public float impactVolume = 0.8f;
    public GameObject hitVFX;

    private void OnCollisionEnter(Collision collision)
    {
        RoverController rover = collision.collider.GetComponentInParent<RoverController>();
        if (rover != null)
        {
            rover.ApplyTerrainSlowdown(slowMultiplier, slowDuration);

            // --- Camera Shake ---
            CameraShake shake = Camera.main.GetComponent<CameraShake>();
            if (shake != null)
                shake.Shake(0.05f, 0.2f);

            // Play sound at hit point
            if (impactSound)
                AudioSource.PlayClipAtPoint(impactSound, collision.contacts[0].point, impactVolume);

            //VFX
            if (hitVFX)
            {
                Instantiate(hitVFX, collision.contacts[0].point, Quaternion.identity);
            }
        }
    }
}
