using System.Collections;
using UnityEngine;

public class BatteryPickup : MonoBehaviour
{
    public float respawnTime = 10f;
    public GameObject pickupEffect;
    public AudioClip pickupSound;
    public float rotationSpeed = 100f; // degrees per second

    private bool isRespawning = false;

    private void Update()
    {
        // Rotate the battery around its Y-axis
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isRespawning) return;

        if (other.CompareTag("Rover"))
        {
            RoverController rover = other.GetComponent<RoverController>();
            if (rover)
            {
                rover.boostMeter = Mathf.Min(rover.boostMeter + rover.boostRegenPerBattery, 100f);
                rover.UpdateBoostUI();
            }

            if (pickupEffect)
                Instantiate(pickupEffect, transform.position, Quaternion.identity);

            // Play sound
            if (pickupSound)
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);

            StartCoroutine(RespawnRoutine());
        }
    }

    private IEnumerator RespawnRoutine()
    {
        isRespawning = true;
        // Hide the object visually and disable its collider
        GetComponent<Collider>().enabled = false;
        GetComponentInChildren<MeshRenderer>().enabled = false;

        yield return new WaitForSeconds(respawnTime);

        // Reactivate components instead of SetActive(true)
        GetComponent<Collider>().enabled = true;
        GetComponentInChildren<MeshRenderer>().enabled = true;
        isRespawning = false;
    }
}
