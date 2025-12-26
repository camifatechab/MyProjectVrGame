using UnityEngine;

public class WindZoneSimple : MonoBehaviour
{
    /*public Vector3 forceDirection = Vector3.right;
    public float forceStrength = 50f;

    private void OnTriggerStay(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null)
        {
            rb.AddForce(forceDirection.normalized * forceStrength, ForceMode.Acceleration);
        }
    }*/

    [Header("Wind Force")]
    public Vector3 forceDirection = Vector3.up;
    public float forceStrength = 50f;

    [Header("Timing")]
    public float activeDuration = 5f;
    public float inactiveDuration = 3f;

    [Header("Effects")]
    public ParticleSystem windParticles;
    public AudioSource windAudio;

    private bool isActive;

    private void Start()
    {
        StartCoroutine(WindRoutine());
    }

    private System.Collections.IEnumerator WindRoutine()
    {
        while (true)
        {
            // WIND ON
            SetWindState(true);
            yield return new WaitForSeconds(activeDuration);

            // WIND OFF
            SetWindState(false);
            yield return new WaitForSeconds(inactiveDuration);
        }
    }

    private void SetWindState(bool state)
    {
        isActive = state;

        // Particles
        if (windParticles)
        {
            if (state && !windParticles.isPlaying)
                windParticles.Play();
            else if (!state && windParticles.isPlaying)
                windParticles.Stop();
        }

        // Audio
        if (windAudio)
        {
            if (state && !windAudio.isPlaying)
                windAudio.Play();
            else if (!state && windAudio.isPlaying)
                windAudio.Stop();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isActive)
            return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb != null)
        {
            rb.AddForce(
                forceDirection.normalized * forceStrength,
                ForceMode.Acceleration
            );
        }
    }

    // Visual helper
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, forceDirection.normalized * 2f);
    }
}