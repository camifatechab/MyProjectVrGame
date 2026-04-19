using UnityEngine;

/// <summary>
/// Gates the CP06 transition: unlock rover dismount, highlight the world jetpack,
/// then unlock the XR rig's jetpack controller once the prop is grabbed.
/// </summary>
public class CP06JetpackSequence : MonoBehaviour
{
    [Header("References")]
    public RoverPhysicsController rover;
    public RoverCheckpointRespawn roverRespawn;
    public AutoJetpackController jetpackController;
    public PlayerRespawnManager playerRespawnManager;
    public JetpackPickupInteractable jetpackPickup;
    public JetpackPickupHighlighter jetpackHighlight;

    [Header("State")]
    public bool lockDismountUntilActivated = true;
    public bool disableRoverRespawnOnUnlock = true;

    private bool sequenceUnlocked;
    private bool pickupComplete;
    private bool roverParked;
    private float sequenceUnlockedAt = -1f;

    public bool SequenceUnlocked => sequenceUnlocked;
    public bool PickupComplete => pickupComplete;
    public bool RoverParked => roverParked;
    public float SequenceUnlockedAt => sequenceUnlockedAt;

    private void Awake()
    {
        rover ??= FindAnyObjectByType<RoverPhysicsController>();
        roverRespawn ??= FindAnyObjectByType<RoverCheckpointRespawn>();
        jetpackController ??= FindAnyObjectByType<AutoJetpackController>();
        playerRespawnManager ??= FindAnyObjectByType<PlayerRespawnManager>();
        jetpackPickup ??= FindAnyObjectByType<JetpackPickupInteractable>();
        jetpackHighlight ??= FindAnyObjectByType<JetpackPickupHighlighter>();

        if (jetpackPickup != null)
            jetpackPickup.PickedUp += HandleJetpackPickedUp;
    }

    private void Start()
    {
        if (lockDismountUntilActivated && rover != null)
            rover.SetDismountEnabled(false);

        if (jetpackController != null)
            jetpackController.SetExternalFlightLock(true);

        if (jetpackPickup != null)
            jetpackPickup.SetPickupEnabled(false);

        if (jetpackHighlight != null)
            jetpackHighlight.SetHighlighted(false);
    }

    private void OnDestroy()
    {
        if (jetpackPickup != null)
            jetpackPickup.PickedUp -= HandleJetpackPickedUp;
    }

    private void Update()
    {
        if (!sequenceUnlocked || pickupComplete || roverParked || rover == null)
            return;

        if (!rover.IsMounted)
            ParkRoverForOnFootSequence();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (sequenceUnlocked || pickupComplete)
            return;

        RoverPhysicsController roverInTrigger = other.GetComponentInParent<RoverPhysicsController>();
        if (roverInTrigger == null)
            return;

        UnlockSequence();
    }

    private void UnlockSequence()
    {
        sequenceUnlocked = true;
        sequenceUnlockedAt = Time.time;

        if (rover != null)
            rover.SetDismountEnabled(true);

        if (disableRoverRespawnOnUnlock && roverRespawn != null)
            roverRespawn.enabled = false;

        if (playerRespawnManager != null)
        {
            playerRespawnManager.SetRespawnWaypoint(transform);
            playerRespawnManager.SetRespawnEnabled(true);
        }

        // Pickup is enabled only after the player dismounts (see ParkRoverForOnFootSequence)
        if (jetpackPickup != null)
            jetpackPickup.SetPickupEnabled(false);

        if (jetpackHighlight != null)
            jetpackHighlight.SetHighlighted(true);

        Debug.Log("<color=#44ff88>[CP06JetpackSequence] CP06 reached. Dismount unlocked and jetpack highlighted.</color>");
    }

    private void ParkRoverForOnFootSequence()
    {
        roverParked = true;

        rover.SetInput(0f, 0f, 1f);
        rover.SetMountEnabled(false);
        rover.SetDismountEnabled(false);

        if (rover.rb != null)
        {
            rover.rb.linearVelocity = Vector3.zero;
            rover.rb.angularVelocity = Vector3.zero;
            rover.rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        RoverStuckDetector stuckDetector = rover.GetComponent<RoverStuckDetector>();
        if (stuckDetector != null)
            stuckDetector.enabled = false;

        if (jetpackPickup != null)
            jetpackPickup.SetPickupEnabled(true);

        Debug.Log("<color=#44ff88>[CP06JetpackSequence] Rover parked for on-foot jetpack transition.</color>");
    }

    private void HandleJetpackPickedUp()
    {
        if (pickupComplete)
            return;

        pickupComplete = true;

        if (jetpackController != null)
        {
            jetpackController.enabled = true;
            jetpackController.SetExternalFlightLock(false);
        }

        if (rover != null)
        {
            rover.SetMountEnabled(false);
            rover.SetDismountEnabled(false);
        }

        if (jetpackHighlight != null)
            jetpackHighlight.SetHighlighted(false);

        Debug.Log("<color=#44ff88>[CP06JetpackSequence] Jetpack grabbed. Flight controls unlocked.</color>");
    }
}
