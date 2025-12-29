using UnityEngine;

public class FadeTrigger : MonoBehaviour
{
    public FadeScreen fadeScreen;   // Drag your FadeScreen object here
    public bool triggerOnce = true; // Prevents multiple triggers
    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        // Optional: check for player tag
        // if (!other.CompareTag("Player")) return;

        if (triggerOnce && hasTriggered) return;

        // Call FadeOut() on the referenced FadeScreen script
        fadeScreen.FadeOut();

        hasTriggered = true;
    }
}
