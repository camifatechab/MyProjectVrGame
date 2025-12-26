using UnityEngine;

public class RoverLandingSound : MonoBehaviour
{
    public AudioClip landingThud;
    public float cooldown = 0.5f; // prevents spam
    public string groundTag = "Ground";

    private float lastPlayTime;

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag(groundTag))
            return;

        if (Time.time - lastPlayTime < cooldown)
            return;

        lastPlayTime = Time.time;

        if (landingThud)
        {
            AudioSource.PlayClipAtPoint(
                landingThud,
                transform.position,
                0.8f
            );
        }
    }
}
