using UnityEngine;

/// <summary>
/// Controls the sequential beacon path toward the crystal.
/// Assign beacons in visit order, then assign the crystal.
/// Automatically starts beacon[0] on Play.
/// </summary>
public class BeaconSequenceManager : MonoBehaviour
{
    [Header("Beacon Path (in visit order)")]
    [Tooltip("Drag platforms in the order the player should visit them.")]
    public PlatformBeacon[] beacons;

    [Header("Final Target")]
    [Tooltip("The crystal the player must destroy after visiting all platforms.")]
    public CrystalDestructible crystal;

    private int currentIndex = -1;

    // ── Lifecycle ────────────────────────────────────────────────
    private void Start()
    {
        Debug.Log($"<color=cyan>[BeaconSequence] Start — beacons={beacons?.Length ?? -1}, crystal={(crystal != null ? crystal.name : "NULL")}</color>");

        // Start everything off
        if (beacons != null)
            foreach (var b in beacons)
                if (b != null) b.SetActive(false);

        if (crystal != null)
            crystal.SetCrystalBeaconActive(false);

        // Light up the first beacon
        ActivateBeacon(0);
    }

    // ── Called by PlatformBeacon when player enters ───────────────
    public void OnBeaconReached(PlatformBeacon beacon)
    {
        int index = System.Array.IndexOf(beacons, beacon);
        if (index < 0) return;

        beacon.SetActive(false);
        Debug.Log($"<color=cyan>[BeaconSequence] Platform {index} reached.</color>");

        int next = index + 1;
        if (next < beacons.Length)
        {
            ActivateBeacon(next);
        }
        else
        {
            // All platforms visited — light up the crystal
            Debug.Log("<color=cyan>[BeaconSequence] All platforms reached → Activating crystal target!</color>");
            if (crystal != null)
                crystal.SetCrystalBeaconActive(true);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────
    private void ActivateBeacon(int index)
    {
        if (beacons == null || index >= beacons.Length) return;
        currentIndex = index;
        if (beacons[index] != null)
        {
            beacons[index].SetActive(true);
            Debug.Log($"<color=cyan>[BeaconSequence] Beacon {index} → {beacons[index].gameObject.name} activated.</color>");
        }
    }
}
