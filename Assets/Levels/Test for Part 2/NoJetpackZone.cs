using UnityEngine;

/// Place this on a GameObject with a Trigger Collider.
/// While the player is inside, the jetpack is disabled.
public class NoJetpackZone : MonoBehaviour
{
    private AutoJetpackController jetpack;

    void Start()
    {
        jetpack = FindAnyObjectByType<AutoJetpackController>();

        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<AutoJetpackController>() == null &&
            other.GetComponentInParent<AutoJetpackController>() == null) return;
        if (jetpack != null) jetpack.enabled = false;
    }

void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<AutoJetpackController>() == null &&
            other.GetComponentInParent<AutoJetpackController>() == null) return;
        if (jetpack != null) jetpack.enabled = true;
    }
}
