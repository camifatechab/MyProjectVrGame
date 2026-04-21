using UnityEngine;

public class Teleporter : MonoBehaviour
{
    public IntroSequenceManager sequenceManager;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            sequenceManager.ActivateTeleport();
        }
    }
}