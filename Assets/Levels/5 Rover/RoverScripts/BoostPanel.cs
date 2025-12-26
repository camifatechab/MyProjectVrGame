using UnityEngine;

public class BoostPanel : MonoBehaviour
{
    public float boostForce = 20f;        // Strength of boost
    public float boostDuration = 0.4f;    // How long the boost lasts

    public AudioSource boostSound;        // Drag your boost sound here

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Rover"))
            return;

        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Play sound if assigned
            if (boostSound != null)
                boostSound.Play();

            StartCoroutine(ApplyBoost(rb));
        }
    }

    private System.Collections.IEnumerator ApplyBoost(Rigidbody rb)
    {
        float timer = 0f;

        while (timer < boostDuration)
        {
            rb.AddForce(rb.transform.forward * boostForce, ForceMode.Acceleration);

            timer += Time.deltaTime;
            yield return null;
        }
    }
}
