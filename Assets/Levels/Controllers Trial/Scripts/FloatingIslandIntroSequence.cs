using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class FloatingIslandIntroSequence : MonoBehaviour
{
    private enum SequenceStage
    {
        WaitingForInitialMount,
        FlyingToEnd,
        WaitingForFinalDismount,
        Completed
    }

    private const string BootstrapObjectName = "FloatingIslandIntroSequence_Auto";
    private const string ControllersTrialSceneToken = "Controllers Trial";
    private const bool AutoBootstrapEnabled = true;

    [Header("Core References")]
    [SerializeField] private RideableCreature dragon;
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private PlayerRespawnManager respawnManager;
    [SerializeField] private RoverPhysicsController rover;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Anchors")]
    [SerializeField] private Transform playerStartAnchor;
    [SerializeField] private Transform playerSpawnAnchor;
    [SerializeField] private Transform dragonStartAnchor;
    [SerializeField] private Transform roverLandingAnchor;
    [SerializeField] private Transform roverDismountAnchor;

    [Header("Path")]
    [SerializeField] private Transform flightPathRoot;
    [SerializeField] private string testingStartWaypointName = "";

    [Header("Startup")]
    [SerializeField] private bool startInRoverOnlyMode = false;
    [SerializeField] private bool movePlayerToStartOnPlay = true;
    [SerializeField] private bool snapDragonToStartOnPlay = true;
    [SerializeField] private Vector3 playerStartPosition = new Vector3(23.7372284f, 27.1211224f, 43.6970062f);
    [SerializeField] private float startFlightPitch = 61f;

    [Header("Rover Handoff")]
    [SerializeField] private float roverLandingBackwardOffset = 8f;
    [SerializeField] private float roverLandingSideOffset = -5f;
    [SerializeField] private float roverDismountSideOffset = 0f;
    [SerializeField] private float roverDismountForwardOffset = -6f;
    [SerializeField] private float roverDismountHeight = 0.25f;
    [SerializeField] private float groundProbeHeight = 24f;
    [SerializeField] private float groundProbeDistance = 64f;

    private readonly List<Transform> firstFlightWaypoints = new();
    private readonly List<Transform> secondFlightWaypoints = new();
    private SequenceStage stage = SequenceStage.WaitingForInitialMount;

    public bool IsWaitingForFinalDismount => stage == SequenceStage.WaitingForFinalDismount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!AutoBootstrapEnabled)
            return;

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.path.Contains(ControllersTrialSceneToken))
            return;

        if (FindFirstObjectByType<FloatingIslandIntroSequence>() != null)
            return;

        GameObject bootstrapObject = new GameObject(BootstrapObjectName);
        bootstrapObject.AddComponent<FloatingIslandIntroSequence>();
    }

    private void Start()
    {
        if (!ResolveReferences())
        {
            enabled = false;
            return;
        }

        if (startInRoverOnlyMode)
        {
            ConfigureRoverOnlyMode();
            return;
        }

        ConfigureSequence();
        HookEvents();
    }

    private void Awake()
    {
        respawnManager ??= FindFirstObjectByType<PlayerRespawnManager>();
        DisableRespawnManager();
    }

    private void Update()
    {
        if (stage == SequenceStage.FlyingToEnd)
            TryPrepareForFinalDismountFromRideEnd();
    }

    private void OnDestroy()
    {
        UnhookEvents();

        if (dragon != null)
            dragon.SetScriptedSequenceActive(false, lockJetpackWhileUnmounted: false);
    }

    private bool ResolveReferences()
    {
        dragon ??= FindFirstObjectByType<RideableCreature>();
        xrOrigin ??= FindFirstObjectByType<XROrigin>();
        respawnManager ??= FindFirstObjectByType<PlayerRespawnManager>();
        rover ??= FindFirstObjectByType<RoverPhysicsController>();
        playerHealth ??= FindFirstObjectByType<PlayerHealth>();
        flightPathRoot ??= FindSceneTransform("FlightPath");

        BuildFlightPaths();

        if (HasTestingStartWaypointOverride())
        {
            playerStartAnchor = firstFlightWaypoints.FirstOrDefault();
            dragonStartAnchor = playerStartAnchor;
            playerSpawnAnchor = BuildTestingPlayerSpawnAnchor();
        }
        else
        {
            playerStartAnchor ??= firstFlightWaypoints.FirstOrDefault();
            dragonStartAnchor ??= playerStartAnchor;
            playerSpawnAnchor ??= BuildPlayerSpawnAnchor();
        }

        List<string> missingReferences = new();
        if (dragon == null) missingReferences.Add(nameof(dragon));
        if (xrOrigin == null) missingReferences.Add(nameof(xrOrigin));
        if (flightPathRoot == null) missingReferences.Add(nameof(flightPathRoot));
        if (playerStartAnchor == null) missingReferences.Add(nameof(playerStartAnchor));
        if (playerSpawnAnchor == null) missingReferences.Add(nameof(playerSpawnAnchor));
        if (dragonStartAnchor == null) missingReferences.Add(nameof(dragonStartAnchor));
        if (firstFlightWaypoints.Count < 2) missingReferences.Add(nameof(firstFlightWaypoints));

        BuildRoverAnchors();
        if (rover != null && roverLandingAnchor == null) missingReferences.Add(nameof(roverLandingAnchor));
        if (rover != null && roverDismountAnchor == null) missingReferences.Add(nameof(roverDismountAnchor));

        if (missingReferences.Count > 0)
        {
            Debug.LogWarning($"FloatingIslandIntroSequence: Missing references: {string.Join(", ", missingReferences)}");
            return false;
        }

        return true;
    }

    private void ConfigureSequence()
    {
        stage = SequenceStage.WaitingForInitialMount;

        if (movePlayerToStartOnPlay)
            MovePlayerToStart();

        if (snapDragonToStartOnPlay)
            SnapDragonToStart();

        dragon.SetScriptedSequenceActive(true, lockJetpackWhileUnmounted: true);
        dragon.allowManualParkingInput = false;
        dragon.autoDismountAfterParking = true;
        dragon.reversePathWhenFinished = false;
        dragon.SetMountEnabled(true);
        ApplyInitialFlightPath();

        if (rover != null)
            rover.SetMountEnabled(false);

        DisableRespawnManager();
    }

    private void ConfigureRoverOnlyMode()
    {
        stage = SequenceStage.Completed;

        if (dragon != null)
        {
            dragon.SetScriptedSequenceActive(false, lockJetpackWhileUnmounted: false);
            dragon.allowManualParkingInput = false;
            dragon.autoDismountAfterParking = true;
            dragon.reversePathWhenFinished = false;
            dragon.SetMountEnabled(false);
        }

        if (rover != null)
            rover.SetMountEnabled(true);

        if (roverDismountAnchor != null && xrOrigin != null)
        {
            xrOrigin.transform.SetParent(null, worldPositionStays: true);
            xrOrigin.transform.position = roverDismountAnchor.position;

            Vector3 currentEuler = xrOrigin.transform.eulerAngles;
            xrOrigin.transform.rotation = Quaternion.Euler(
                currentEuler.x,
                roverDismountAnchor.eulerAngles.y,
                currentEuler.z);
        }

        DisableRespawnManager();
    }

    private void ApplyInitialFlightPath()
    {
        if (dragon == null || firstFlightWaypoints.Count < 2)
            return;

        dragon.SetFlightPath(firstFlightWaypoints, resetProgress: true);
    }

    private void HookEvents()
    {
        UnhookEvents();

        dragon.OnPlayerMounted += HandleDragonMounted;
        dragon.OnPlayerDismounted += HandleDragonDismounted;
        dragon.OnFlightPathCompleted += HandleFlightPathCompleted;
    }

    private void UnhookEvents()
    {
        if (dragon == null)
            return;

        dragon.OnPlayerMounted -= HandleDragonMounted;
        dragon.OnPlayerDismounted -= HandleDragonDismounted;
        dragon.OnFlightPathCompleted -= HandleFlightPathCompleted;
    }

    private void BuildFlightPaths()
    {
        firstFlightWaypoints.Clear();
        secondFlightWaypoints.Clear();

        if (flightPathRoot == null)
            return;

        List<Transform> orderedWaypoints = flightPathRoot.Cast<Transform>()
            .Where(IsNamedWaypoint)
            .OrderBy(GetWaypointOrder)
            .ThenBy(waypoint => waypoint.name)
            .ToList();

        if (HasTestingStartWaypointOverride())
        {
            int startIndex = FindWaypointIndex(orderedWaypoints, testingStartWaypointName);
            if (startIndex >= 0)
            {
                orderedWaypoints = orderedWaypoints.Skip(startIndex).ToList();
            }
            else
            {
                Debug.LogWarning($"FloatingIslandIntroSequence: Testing start waypoint '{testingStartWaypointName}' was not found. Using full route.");
            }
        }

        firstFlightWaypoints.AddRange(orderedWaypoints);
    }

    private void MovePlayerToStart()
    {
        if (xrOrigin == null || playerSpawnAnchor == null)
            return;

        xrOrigin.transform.position = playerSpawnAnchor.position;
        Vector3 currentEuler = xrOrigin.transform.eulerAngles;
        xrOrigin.transform.rotation = Quaternion.Euler(currentEuler.x, playerSpawnAnchor.eulerAngles.y, currentEuler.z);
    }

    private void SnapDragonToStart()
    {
        if (dragon == null || dragonStartAnchor == null)
            return;

        dragon.transform.SetPositionAndRotation(dragonStartAnchor.position, ResolveDragonStartRotation());
    }

    private void HandleDragonMounted()
    {
        if (stage != SequenceStage.WaitingForInitialMount)
            return;

        ApplyInitialFlightPath();
        stage = SequenceStage.FlyingToEnd;
    }

    private void HandleFlightPathCompleted()
    {
        if (stage != SequenceStage.FlyingToEnd)
            return;

        PrepareForFinalDismount();
    }

    private void HandleDragonDismounted()
    {
        if (stage == SequenceStage.WaitingForFinalDismount)
        {
            CompleteRoverHandoff();
            return;
        }

        if (stage == SequenceStage.FlyingToEnd && HasDragonReachedRideEnd())
            CompleteRoverHandoff();
    }

    private void BuildRoverAnchors()
    {
        if (rover == null)
            return;

        Vector3 roverForward = Vector3.ProjectOnPlane(rover.transform.forward, Vector3.up);
        if (roverForward.sqrMagnitude < 0.01f)
            roverForward = Vector3.forward;
        roverForward.Normalize();

        Vector3 roverRight = Vector3.Cross(Vector3.up, roverForward).normalized;

        Vector3 landingGroundPoint = ResolveGroundPoint(
            rover.transform.position
            - roverForward * roverLandingBackwardOffset
            + roverRight * roverLandingSideOffset);

        Quaternion landingRotation = Quaternion.LookRotation((rover.transform.position - landingGroundPoint).normalized, Vector3.up);
        roverLandingAnchor = CreateOrMoveAnchor(
            "RoverLandingAnchor_Runtime",
            landingGroundPoint,
            landingRotation);

        Vector3 dismountGroundPoint = ResolveGroundPoint(
            rover.transform.position
            + roverRight * roverDismountSideOffset
            + roverForward * roverDismountForwardOffset);

        Quaternion dismountRotation = Quaternion.LookRotation(roverForward, Vector3.up);
        roverDismountAnchor = CreateOrMoveAnchor(
            "RoverDismountAnchor_Runtime",
            dismountGroundPoint + Vector3.up * roverDismountHeight,
            dismountRotation);
    }

    private Transform BuildPlayerSpawnAnchor()
    {
        Vector3 spawnPosition = xrOrigin != null
            ? xrOrigin.transform.position
            : playerStartPosition;

        float spawnYaw = xrOrigin != null
            ? xrOrigin.transform.eulerAngles.y
            : playerStartAnchor != null
                ? playerStartAnchor.eulerAngles.y
                : 0f;

        Quaternion spawnRotation = Quaternion.Euler(0f, spawnYaw, 0f);
        return CreateOrMoveAnchor("PlayerSpawnAnchor_Runtime", spawnPosition, spawnRotation);
    }

    private Transform BuildTestingPlayerSpawnAnchor()
    {
        if (playerStartAnchor == null)
            return BuildPlayerSpawnAnchor();

        Quaternion startRotation = ResolveDragonStartRotation();
        Vector3 forward = startRotation * Vector3.forward;
        forward = Vector3.ProjectOnPlane(forward, Vector3.up);
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;
        forward.Normalize();

        Vector3 desiredPosition = playerStartAnchor.position - forward * 4f;
        Vector3 spawnGroundPoint = ResolveGroundPoint(desiredPosition);
        Quaternion spawnRotation = Quaternion.LookRotation(forward, Vector3.up);

        return CreateOrMoveAnchor(
            "PlayerSpawnAnchor_Runtime",
            spawnGroundPoint + Vector3.up * 0.25f,
            spawnRotation);
    }

    private Quaternion ResolveDragonStartRotation()
    {
        Vector3 direction = Vector3.forward;

        if (firstFlightWaypoints.Count > 1 && firstFlightWaypoints[1] != null && dragonStartAnchor != null)
            direction = firstFlightWaypoints[1].position - dragonStartAnchor.position;

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
            return dragonStartAnchor != null ? dragonStartAnchor.rotation : Quaternion.identity;

        float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        return Quaternion.Euler(startFlightPitch, yaw, 0f);
    }

    private void CompleteRoverHandoff()
    {
        stage = SequenceStage.Completed;

        if (dragon != null)
        {
            dragon.autoDismountAfterParking = true;
            dragon.SetScriptedSequenceActive(false, lockJetpackWhileUnmounted: false);
            dragon.SetMountEnabled(false);
        }

        if (rover != null)
            rover.SetMountEnabled(true);

        DisableRespawnManager();
    }

    private void PrepareForFinalDismount()
    {
        if (stage == SequenceStage.WaitingForFinalDismount || stage == SequenceStage.Completed)
            return;

        stage = SequenceStage.WaitingForFinalDismount;

        if (dragon != null)
        {
            dragon.SetScriptedSequenceActive(false, lockJetpackWhileUnmounted: false);
            dragon.SetMountEnabled(false);
            dragon.autoDismountAfterParking = false;
            dragon.PrepareManualDismount(roverDismountAnchor);
        }

        if (rover != null)
            rover.SetMountEnabled(true);

        DisableRespawnManager();
    }

    private void TryPrepareForFinalDismountFromRideEnd()
    {
        if (!HasDragonReachedRideEnd())
            return;

        PrepareForFinalDismount();
    }

    private bool HasDragonReachedRideEnd()
    {
        return dragon != null
            && dragon.TotalWaypoints > 0
            && dragon.CurrentWaypointIndex >= dragon.TotalWaypoints - 1
            && !dragon.IsFlying
            && !dragon.IsParking;
    }

    private void DisableRespawnManager()
    {
        if (respawnManager == null)
            return;

        respawnManager.SetRespawnEnabled(false);
        respawnManager.enabled = false;
    }

    private Vector3 ResolveGroundPoint(Vector3 desiredPosition)
    {
        Vector3 rayOrigin = desiredPosition + Vector3.up * groundProbeHeight;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundProbeHeight + groundProbeDistance, ~0, QueryTriggerInteraction.Ignore))
            return hit.point;

        return desiredPosition;
    }

    private static Transform FindSceneTransform(string objectName)
    {
        return Resources.FindObjectsOfTypeAll<Transform>()
            .FirstOrDefault(transform =>
                transform != null &&
                transform.hideFlags == HideFlags.None &&
                transform.gameObject.scene.IsValid() &&
                transform.name == objectName);
    }

    private static bool IsNamedWaypoint(Transform waypoint)
    {
        return waypoint != null && GetWaypointOrder(waypoint) != int.MaxValue;
    }

    private bool HasTestingStartWaypointOverride()
    {
        return !string.IsNullOrWhiteSpace(testingStartWaypointName);
    }

    private static int FindWaypointIndex(List<Transform> orderedWaypoints, string waypointName)
    {
        if (orderedWaypoints == null || orderedWaypoints.Count == 0 || string.IsNullOrWhiteSpace(waypointName))
            return -1;

        int requestedOrder = ExtractWaypointOrder(waypointName);
        if (requestedOrder != int.MaxValue)
        {
            for (int i = 0; i < orderedWaypoints.Count; i++)
            {
                if (GetWaypointOrder(orderedWaypoints[i]) == requestedOrder)
                    return i;
            }
        }

        for (int i = 0; i < orderedWaypoints.Count; i++)
        {
            if (orderedWaypoints[i] != null && orderedWaypoints[i].name == waypointName)
                return i;
        }

        return -1;
    }

    private static int GetWaypointOrder(Transform waypoint)
    {
        return waypoint == null ? int.MaxValue : ExtractWaypointOrder(waypoint.name);
    }

    private static int ExtractWaypointOrder(string waypointName)
    {
        if (string.IsNullOrEmpty(waypointName) || waypointName.Length < 3 || !waypointName.StartsWith("WP"))
            return int.MaxValue;

        int digitStart = 2;
        int digitEnd = digitStart;
        while (digitEnd < waypointName.Length && char.IsDigit(waypointName[digitEnd]))
            digitEnd++;

        if (digitEnd == digitStart)
            return int.MaxValue;

        return int.TryParse(waypointName.Substring(digitStart, digitEnd - digitStart), out int order)
            ? order
            : int.MaxValue;
    }

    private static Transform CreateOrMoveAnchor(string name, Vector3 position, Quaternion rotation)
    {
        Transform existing = FindSceneTransform(name);
        if (existing == null)
        {
            GameObject anchorObject = new GameObject(name);
            existing = anchorObject.transform;
        }

        existing.SetPositionAndRotation(position, rotation);
        return existing;
    }
}
