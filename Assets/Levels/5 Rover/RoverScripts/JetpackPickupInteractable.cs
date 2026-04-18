using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Turns the world jetpack prop into a gated XR grab pickup.
/// </summary>
public class JetpackPickupInteractable : MonoBehaviour
{
    public event Action PickedUp;

    [Header("State")]
    public bool pickupEnabledOnStart;

    [Header("Optional References")]
    public JetpackPickupHighlighter highlighter;

    private XRGrabInteractable grabInteractable;
    private Rigidbody body;
    private BoxCollider boxCollider;
    private bool pickedUp;

    private void Awake()
    {
        EnsureComponents();

        if (highlighter == null)
            highlighter = GetComponent<JetpackPickupHighlighter>();

        grabInteractable.selectEntered.AddListener(OnSelectEntered);
        SetPickupEnabled(pickupEnabledOnStart);
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
            grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
    }

    public void SetPickupEnabled(bool enabled)
    {
        bool finalEnabled = enabled && !pickedUp;

        if (grabInteractable != null)
            grabInteractable.enabled = finalEnabled;

        if (highlighter != null)
            highlighter.SetHighlighted(finalEnabled);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (pickedUp)
            return;

        pickedUp = true;
        SetPickupEnabled(false);
        SetVisualsVisible(false);
        PickedUp?.Invoke();
    }

    private void EnsureComponents()
    {
        body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();

        body.useGravity = false;
        body.isKinematic = true;

        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
            boxCollider = gameObject.AddComponent<BoxCollider>();

        ConfigureBoxCollider();

        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
            grabInteractable = gameObject.AddComponent<XRGrabInteractable>();

        grabInteractable.throwOnDetach = false;
        grabInteractable.trackPosition = false;
        grabInteractable.trackRotation = false;
    }

    private void ConfigureBoxCollider()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            boxCollider.center = Vector3.zero;
            boxCollider.size = Vector3.one * 0.5f;
            return;
        }

        Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        foreach (Renderer renderer in renderers)
        {
            Bounds bounds = renderer.bounds;
            Vector3 extents = bounds.extents;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 worldPoint = bounds.center + Vector3.Scale(extents, new Vector3(x, y, z));
                        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
                        min = Vector3.Min(min, localPoint);
                        max = Vector3.Max(max, localPoint);
                    }
                }
            }
        }

        boxCollider.center = (min + max) * 0.5f;
        boxCollider.size = Vector3.Max(max - min, Vector3.one * 0.05f);
    }

    private void SetVisualsVisible(bool visible)
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            renderer.enabled = visible;

        if (boxCollider != null)
            boxCollider.enabled = visible;
    }
}
