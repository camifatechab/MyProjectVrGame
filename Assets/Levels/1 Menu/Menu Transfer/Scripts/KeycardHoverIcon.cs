using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class KeycardHoverIcon : MonoBehaviour
{
    public GameObject hoverIcon; // Assign this to the child object in Inspector
    private XRGrabInteractable grabInteractable;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrab);
            grabInteractable.selectExited.AddListener(OnRelease);
        }
    }

    void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrab);
            grabInteractable.selectExited.RemoveListener(OnRelease);
        }
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        if (hoverIcon != null)
            hoverIcon.SetActive(false);
    }

    void OnRelease(SelectExitEventArgs args)
    {
        if (hoverIcon != null)
            hoverIcon.SetActive(true);
    }
}
