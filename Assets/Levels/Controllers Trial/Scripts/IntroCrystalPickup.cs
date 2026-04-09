using System;
using Unity.XR.CoreUtils;
using UnityEngine;

[DisallowMultipleComponent]
public class IntroCrystalPickup : MonoBehaviour
{
    public event Action Collected;

    [Header("Presentation")]
    [SerializeField] private float rotationSpeed = 45f;
    [SerializeField] private AudioClip collectClip;
    [SerializeField] private float collectVolume = 0.8f;

    [Header("Pickup")]
    [SerializeField] private float headCollectRadius = 0.7f;
    [SerializeField] private float handCollectRadius = 0.45f;
    [SerializeField] private float gazeCollectDistance = 2f;
    [SerializeField] private float gazeViewportRadius = 0.18f;

    public bool IsCollected { get; private set; }

    private XROrigin xrOrigin;
    private Transform[] trackingPoints = Array.Empty<Transform>();
    private Renderer[] renderers = Array.Empty<Renderer>();
    private Collider[] colliders = Array.Empty<Collider>();

    private void Awake()
    {
        CacheComponents();
    }

    private void OnEnable()
    {
        CacheComponents();
    }

    private void Update()
    {
        if (IsCollected)
            return;

        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        TryCollect();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsCollected)
            return;

        if (other.GetComponentInParent<XROrigin>() != null || other.CompareTag("MainCamera") || other.CompareTag("Player"))
            Collect();
    }

    public void Initialize(XROrigin origin)
    {
        xrOrigin = origin;
        trackingPoints = Array.Empty<Transform>();
    }

    public void ResetPickup()
    {
        CacheComponents();
        IsCollected = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = true;
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = true;
        }
    }

    public void Collect()
    {
        if (IsCollected)
            return;

        IsCollected = true;

        if (collectClip != null)
            AudioSource.PlayClipAtPoint(collectClip, transform.position, collectVolume);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = false;
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = false;
        }

        Collected?.Invoke();
    }

    private void TryCollect()
    {
        xrOrigin ??= FindFirstObjectByType<XROrigin>();
        if (xrOrigin == null)
            return;

        Transform head = xrOrigin.Camera != null ? xrOrigin.Camera.transform : xrOrigin.transform;
        if (DistanceToCrystal(head.position) <= headCollectRadius || IsCenteredInView(head))
        {
            Collect();
            return;
        }

        if (trackingPoints.Length == 0)
            trackingPoints = xrOrigin.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < trackingPoints.Length; i++)
        {
            Transform trackingPoint = trackingPoints[i];
            if (trackingPoint == null || trackingPoint == xrOrigin.transform || trackingPoint == head)
                continue;

            if (DistanceToCrystal(trackingPoint.position) <= handCollectRadius)
            {
                Collect();
                return;
            }
        }
    }

    private bool IsCenteredInView(Transform head)
    {
        Camera playerCamera = xrOrigin != null ? xrOrigin.Camera : null;
        if (playerCamera == null)
            return false;

        Vector3 crystalCenter = GetCrystalCenter();
        Vector3 toCrystal = crystalCenter - head.position;
        if (toCrystal.magnitude > gazeCollectDistance)
            return false;

        Vector3 viewportPoint = playerCamera.WorldToViewportPoint(crystalCenter);
        if (viewportPoint.z <= 0f)
            return false;

        float distanceFromCenter = Vector2.Distance(new Vector2(viewportPoint.x, viewportPoint.y), new Vector2(0.5f, 0.5f));
        return distanceFromCenter <= gazeViewportRadius;
    }

    private float DistanceToCrystal(Vector3 worldPoint)
    {
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider currentCollider = colliders[i];
            if (currentCollider == null || !currentCollider.enabled)
                continue;

            Vector3 closestPoint = currentCollider.ClosestPoint(worldPoint);
            float distance = Vector3.Distance(worldPoint, closestPoint);
            if (distance <= 0.001f)
                return 0f;
        }

        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                Renderer currentRenderer = renderers[i];
                if (currentRenderer == null || !currentRenderer.enabled)
                    continue;

                bounds.Encapsulate(currentRenderer.bounds);
            }

            return Mathf.Sqrt(bounds.SqrDistance(worldPoint));
        }

        return Vector3.Distance(worldPoint, transform.position);
    }

    private Vector3 GetCrystalCenter()
    {
        if (renderers.Length == 0)
            return transform.position;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            Renderer currentRenderer = renderers[i];
            if (currentRenderer == null || !currentRenderer.enabled)
                continue;

            bounds.Encapsulate(currentRenderer.bounds);
        }

        return bounds.center;
    }

    private void CacheComponents()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
    }
}
