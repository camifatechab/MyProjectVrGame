using UnityEngine;

public class CollectiblePlant : MonoBehaviour
{
    public GameObject collectEffect;
    public AudioClip collectSound;
    private bool collected = false; // Prevents double collection

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return; // already collected, skip

        if (other.CompareTag("Rover"))
        {
            collected = true;

            PlantManager.Instance.CollectPlant();

            if (collectEffect)
                Instantiate(collectEffect, transform.position, Quaternion.identity);
            if (collectSound)
                AudioSource.PlayClipAtPoint(collectSound, transform.position);

            /*PlantManager.Instance?.CollectPlant();
            Destroy(gameObject);*/

            // Hide or disable plant
            gameObject.SetActive(false);
        }
    }
}
