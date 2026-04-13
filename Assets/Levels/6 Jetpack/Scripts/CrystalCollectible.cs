using UnityEngine;
using Unity.XR.CoreUtils;
using System;

/// <summary>
/// Simple collectible script for crystals/spaceship parts.
/// Attach to each collectible object.
/// </summary>
public class CrystalCollectible : MonoBehaviour
{
    public event Action Collected;

    [Header("Collection Settings")]
    [Tooltip("Optional audio clip to play when collected")]
    public AudioClip collectionSound;
    
    [Tooltip("Volume of collection sound (0-1)")]
    [Range(0f, 1f)]
    public float soundVolume = 0.7f;
    
    [Header("Visual Feedback")]
    [Tooltip("Should the crystal rotate while idle?")]
    public bool rotateWhileIdle = true;
    
    [Tooltip("Rotation speed")]
    public float rotationSpeed = 30f;

    [Header("VR Pickup")]
    [Tooltip("How close the player's head can be to collect the crystal")]
    public float headCollectRadius = 0.5f;

    [Tooltip("Extra distance allowed around the visible crystal for hand/controller pickup")]
    public float handCollectRadius = 0.35f;
    
    private bool isCollected = false;
    private bool collectionEnabled = true;
    // AudioSource no longer needed - using PlayClipAtPoint instead
    private XROrigin xrOrigin;
    private Transform[] playerTargets = System.Array.Empty<Transform>();
    private Collider[] cachedColliders;
    private Renderer[] cachedRenderers;
    private RideableCreature cachedCreature;

    public bool IsCollected => isCollected;
    public bool CollectionEnabled => collectionEnabled;

void Start()
    {
        cachedColliders = GetComponentsInChildren<Collider>(true);
        cachedRenderers = GetComponentsInChildren<Renderer>(true);

        // CrystalManager is optional so this collectible can be reused for
        // single-object objectives without the old crystal minigame flow.
        if (CrystalManager.Instance != null)
        {
            CrystalManager.Instance.RegisterCrystal(this);
        }
        
        // Auto-add particle effects if not present
        if (GetComponent<CrystalParticles>() == null)
        {
            gameObject.AddComponent<CrystalParticles>();
        }
    }

    void Update()
    {
        // Simple idle rotation
        if (rotateWhileIdle && !isCollected)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }

        if (!isCollected && collectionEnabled && !IsPlayerMountedOnCreature())
        {
            TryCollectFromPlayerProximity();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!collectionEnabled || IsPlayerMountedOnCreature())
            return;

        // Check if player collected it
        if (!isCollected && (other.CompareTag("MainCamera") || other.CompareTag("Player") || other.GetComponentInParent<XROrigin>() != null))
        {
            Collect();
        }
    }

    public void SetCollectionEnabled(bool enabled)
    {
        collectionEnabled = enabled;
    }

    private bool IsPlayerMountedOnCreature()
    {
        if (cachedCreature == null)
            cachedCreature = FindFirstObjectByType<RideableCreature>();
        return cachedCreature != null && cachedCreature.IsPlayerMounted;
    }

void Collect()
    {
        if (isCollected) return; // Prevent double collection
        
        isCollected = true;
        
        // STEP 1: Trigger particle burst BEFORE hiding
        CrystalParticles particles = GetComponent<CrystalParticles>();
        if (particles != null)
        {
            // Detach particles so they continue after crystal is destroyed
            particles.transform.SetParent(null);
            particles.TriggerCollectionBurst();
        }
        
        // STEP 2: Hide visual IMMEDIATELY (instant feedback!)
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }
        
        // STEP 3: Disable collider IMMEDIATELY (prevent double collection)
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }
        
        // STEP 4: Play sound (after gem is hidden) - Use PlayClipAtPoint for reliability!
        if (collectionSound != null)
        {
            AudioSource.PlayClipAtPoint(collectionSound, transform.position, soundVolume);
            Debug.Log($"<color=cyan>♪ Crystal collection sound playing at {transform.position}</color>");
        }
        else
        {
            Debug.LogWarning("<color=yellow>No collection sound assigned to crystal!</color>");
        }
        
        // STEP 5: Notify manager (updates UI immediately)
        if (CrystalManager.Instance != null)
        {
            CrystalManager.Instance.OnCrystalCollected(this);
        }

        Collected?.Invoke();
        
        // STEP 6: Destroy immediately (sound and particles play independently)
        Destroy(gameObject, 0.1f);
    }

    void TryCollectFromPlayerProximity()
    {
        xrOrigin ??= FindFirstObjectByType<XROrigin>();
        if (xrOrigin == null)
            return;

        Transform head = xrOrigin.Camera != null ? xrOrigin.Camera.transform : xrOrigin.transform;
        if (DistanceToCrystal(head.position) <= headCollectRadius)
        {
            Collect();
            return;
        }

        if (playerTargets == null || playerTargets.Length == 0)
            playerTargets = xrOrigin.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < playerTargets.Length; i++)
        {
            Transform target = playerTargets[i];
            if (target == null || target == xrOrigin.transform || target == head)
                continue;

            if (DistanceToCrystal(target.position) <= handCollectRadius)
            {
                Collect();
                return;
            }
        }
    }

    float DistanceToCrystal(Vector3 worldPoint)
    {
        if (cachedColliders != null)
        {
            for (int i = 0; i < cachedColliders.Length; i++)
            {
                Collider currentCollider = cachedColliders[i];
                if (currentCollider == null || !currentCollider.enabled)
                    continue;

                Vector3 closestPoint = currentCollider.ClosestPoint(worldPoint);
                float distance = Vector3.Distance(worldPoint, closestPoint);
                if (distance <= 0.001f)
                    return 0f;
            }
        }

        if (cachedRenderers != null && cachedRenderers.Length > 0)
        {
            Bounds bounds = cachedRenderers[0].bounds;
            for (int i = 1; i < cachedRenderers.Length; i++)
            {
                Renderer currentRenderer = cachedRenderers[i];
                if (currentRenderer == null || !currentRenderer.enabled)
                    continue;

                bounds.Encapsulate(currentRenderer.bounds);
            }

            return Mathf.Sqrt(bounds.SqrDistance(worldPoint));
        }

        return Vector3.Distance(worldPoint, transform.position);
    }
}
