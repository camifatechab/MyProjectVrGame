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
        FlyingToCrystal,
        LandingAtCrystal,
        WaitingForCrystalPickup,
        WaitingForCrystalRemount,
        FlyingToEnd,
        LandingAtRover,
        Completed
    }

    private const string BootstrapObjectName = "FloatingIslandIntroSequence_Auto";
    private const string ControllersTrialSceneToken = "Controllers Trial";
    private const bool AutoBootstrapEnabled = true;

    [Header("Core References")]
    [SerializeField] private RideableCreature dragon;
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private PlayerRespawnManager respawnManager;
    [SerializeField] private CrystalCollectible targetCrystal;
    [SerializeField] private List<CrystalCollectible> requiredCrystals = new();
    [SerializeField] private RoverPhysicsController rover;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Anchors")]
    [SerializeField] private Transform playerStartAnchor;
    [SerializeField] private Transform playerSpawnAnchor;
    [SerializeField] private Transform dragonStartAnchor;
    [SerializeField] private Transform crystalLandingAnchor;
    [SerializeField] private Transform crystalDismountAnchor;
    [SerializeField] private Transform crystalRespawnAnchor;
    [SerializeField] private Transform crystalApproachWaypoint;
    [SerializeField] private Transform roverLandingAnchor;
    [SerializeField] private Transform roverDismountAnchor;

    [Header("Path")]
    [SerializeField] private Transform flightPathRoot;
    [SerializeField] private string crystalStopWaypointName = "WP03";

    [Header("Startup")]
    [SerializeField] private bool startInRoverOnlyMode = true;
    [SerializeField] private bool movePlayerToStartOnPlay = true;
    [SerializeField] private bool snapDragonToStartOnPlay = true;
    [SerializeField] private bool hideCrystalsUntilLanding = true;
    [SerializeField] private Vector3 playerStartPosition = new Vector3(23.7372284f, 27.1211224f, 43.6970062f);

    [Header("Crystal Landing")]
    [SerializeField] private float startFlightPitch = 61f;
    [SerializeField] private float crystalStopReachDistance = 8f;
    [SerializeField] private float crystalPlatformDistance = 10f;
    [SerializeField] private float crystalLandingHeight = 3.25f;
    [SerializeField] private float crystalDismountHeight = 0.25f;
    [SerializeField] private float crystalApproachLift = 5f;
    [SerializeField] private float crystalApproachPullback = 2f;
    [SerializeField] private float groundProbeHeight = 24f;
    [SerializeField] private float groundProbeDistance = 64f;

    [Header("Rover Handoff")]
    [SerializeField] private float roverLandingBackwardOffset = 8f;
    [SerializeField] private float roverLandingSideOffset = -5f;
    [SerializeField] private float roverDismountSideOffset = 0f;
    [SerializeField] private float roverDismountForwardOffset = -6f;
    [SerializeField] private float roverDismountHeight = 0.25f;

    private readonly List<Transform> firstFlightWaypoints = new();
    private readonly List<Transform> secondFlightWaypoints = new();
    private readonly List<CrystalCollectible> resolvedCrystals = new();
    private SequenceStage stage = SequenceStage.WaitingForInitialMount;
    private Transform crystalStopWaypoint;
    private Transform remountStartAnchor;
    private bool crystalLandingTriggered;

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

    private void Update()
    {
        if (stage == SequenceStage.FlyingToCrystal)
            TryTriggerCrystalLandingFromWaypoint();
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
        ResolveRequiredCrystals();
        targetCrystal ??= ResolveTargetCrystal();
        targetCrystal ??= resolvedCrystals.FirstOrDefault();

        playerStartAnchor ??= FindWaypoint("WP01_");
        dragonStartAnchor ??= playerStartAnchor;
        playerSpawnAnchor ??= BuildPlayerSpawnAnchor();

        List<string> missingReferences = new();
        if (dragon == null) missingReferences.Add(nameof(dragon));
        if (xrOrigin == null) missingReferences.Add(nameof(xrOrigin));
        if (flightPathRoot == null) missingReferences.Add(nameof(flightPathRoot));
        if (resolvedCrystals.Count == 0) missingReferences.Add(nameof(requiredCrystals));
        if (targetCrystal == null) missingReferences.Add(nameof(targetCrystal));
        if (playerStartAnchor == null) missingReferences.Add(nameof(playerStartAnchor));
        if (playerSpawnAnchor == null) missingReferences.Add(nameof(playerSpawnAnchor));
        if (dragonStartAnchor == null) missingReferences.Add(nameof(dragonStartAnchor));

        if (missingReferences.Count > 0)
        {
            Debug.LogWarning($"FloatingIslandIntroSequence: Missing references: {string.Join(", ", missingReferences)}");
            return false;
        }

        BuildFlightPaths();
        if (firstFlightWaypoints.Count < 2 || secondFlightWaypoints.Count < 2)
        {
            Debug.LogWarning("FloatingIslandIntroSequence: Flight path is incomplete.");
            return false;
        }

        BuildCrystalAnchors();
        BuildRoverAnchors();

        missingReferences.Clear();
        if (crystalLandingAnchor == null) missingReferences.Add(nameof(crystalLandingAnchor));
        if (crystalDismountAnchor == null) missingReferences.Add(nameof(crystalDismountAnchor));
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

        SetObjectiveCrystalsActive(!hideCrystalsUntilLanding);
        SetObjectiveCrystalCollectionEnabled(false);
        crystalLandingTriggered = false;

        dragon.SetScriptedSequenceActive(true, lockJetpackWhileUnmounted: true);
        dragon.allowManualParkingInput = false;
        dragon.reversePathWhenFinished = false;
        dragon.SetMountEnabled(true);
        ApplyInitialFlightPath();

        if (rover != null)
            rover.SetMountEnabled(false);

        if (respawnManager != null)
            respawnManager.SetRespawnEnabled(true);
        
        SyncRespawnAndCheckpoint(playerSpawnAnchor);
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
        dragon.OnLandingCompleted += HandleLandingCompleted;
        for (int i = 0; i < resolvedCrystals.Count; i++)
        {
            CrystalCollectible crystal = resolvedCrystals[i];
            if (crystal != null)
                crystal.Collected += HandleCrystalCollected;
        }
    }

    private void UnhookEvents()
    {
        if (dragon != null)
        {
            dragon.OnPlayerMounted -= HandleDragonMounted;
            dragon.OnPlayerDismounted -= HandleDragonDismounted;
            dragon.OnFlightPathCompleted -= HandleFlightPathCompleted;
            dragon.OnLandingCompleted -= HandleLandingCompleted;
        }

        for (int i = 0; i < resolvedCrystals.Count; i++)
        {
            CrystalCollectible crystal = resolvedCrystals[i];
            if (crystal != null)
                crystal.Collected -= HandleCrystalCollected;
        }
    }

    private void BuildFlightPaths()
    {
        firstFlightWaypoints.Clear();
        secondFlightWaypoints.Clear();

        List<Transform> orderedWaypoints = flightPathRoot.Cast<Transform>()
            .Where(IsNamedWaypoint)
            .OrderBy(GetWaypointOrder)
            .ThenBy(waypoint => waypoint.name)
            .ToList();

        if (orderedWaypoints.Count == 0)
            return;

        int stopIndex = FindStopWaypointIndex(orderedWaypoints);
        crystalStopWaypoint = orderedWaypoints[stopIndex];
        crystalApproachWaypoint = BuildCrystalApproachWaypoint(orderedWaypoints, stopIndex);

        for (int i = 0; i <= stopIndex; i++)
            firstFlightWaypoints.Add(orderedWaypoints[i]);

        for (int i = stopIndex + 1; i < orderedWaypoints.Count; i++)
            secondFlightWaypoints.Add(orderedWaypoints[i]);
    }

    private void MovePlayerToStart()
    {
        xrOrigin.transform.position = playerSpawnAnchor.position;
        Vector3 currentEuler = xrOrigin.transform.eulerAngles;
        xrOrigin.transform.rotation = Quaternion.Euler(currentEuler.x, playerSpawnAnchor.eulerAngles.y, currentEuler.z);
    }

    private void SnapDragonToStart()
    {
        dragon.transform.SetPositionAndRotation(dragonStartAnchor.position, ResolveDragonStartRotation());
    }

    private void HandleDragonMounted()
    {
        if (stage == SequenceStage.WaitingForInitialMount)
        {
            ApplyInitialFlightPath();
            crystalLandingTriggered = false;
            stage = SequenceStage.FlyingToCrystal;
            return;
        }

        if (stage == SequenceStage.WaitingForCrystalRemount)
            stage = SequenceStage.FlyingToEnd;
    }

    private void HandleFlightPathCompleted()
    {
        if (stage == SequenceStage.FlyingToCrystal)
        {
            BeginCrystalLanding();
            return;
        }

        if (stage == SequenceStage.FlyingToEnd)
        {
            if (roverLandingAnchor != null)
            {
                stage = SequenceStage.LandingAtRover;
                dragon.SetMountEnabled(false);
                dragon.ParkAtTarget(roverLandingAnchor, roverDismountAnchor);
            }
            else
            {
                CompleteRoverHandoff();
            }
        }
    }

    private void ConfigureRoverOnlyMode()
    {
        stage = SequenceStage.Completed;

        SetObjectiveCrystalsActive(false);

        if (dragon != null)
        {
            dragon.SetScriptedSequenceActive(false, lockJetpackWhileUnmounted: false);
            dragon.allowManualParkingInput = false;
            dragon.reversePathWhenFinished = false;
            dragon.SetMountEnabled(false);
        }

        if (rover != null)
            rover.SetMountEnabled(true);

        if (roverDismountAnchor != null)
        {
            xrOrigin.transform.SetParent(null, worldPositionStays: true);
            xrOrigin.transform.position = roverDismountAnchor.position;

            Vector3 currentEuler = xrOrigin.transform.eulerAngles;
            xrOrigin.transform.rotation = Quaternion.Euler(
                currentEuler.x,
                roverDismountAnchor.eulerAngles.y,
                currentEuler.z);
        }

        SyncRespawnAndCheckpoint(roverDismountAnchor);
    }

    private void HandleLandingCompleted(Transform _)
    {
        if (stage != SequenceStage.LandingAtCrystal)
            return;

        stage = SequenceStage.WaitingForCrystalPickup;
        dragon.SetMountEnabled(false);
        SetObjectiveCrystalsActive(true);
        SetObjectiveCrystalCollectionEnabled(true);
        SyncRespawnAndCheckpoint(crystalDismountAnchor);
    }

    private void HandleCrystalCollected()
    {
        if (stage != SequenceStage.WaitingForCrystalPickup)
            return;

        if (!AreAllRequiredCrystalsCollected())
            return;

        stage = SequenceStage.WaitingForCrystalRemount;
        SetObjectiveCrystalCollectionEnabled(false);
        UpdateCrystalRespawnAnchor();
        remountStartAnchor = CreateOrMoveAnchor(
            "CrystalRemountStart_Runtime",
            dragon.transform.position,
            dragon.transform.rotation);

        List<Transform> remountFlightPath = new() { remountStartAnchor };
        remountFlightPath.AddRange(secondFlightWaypoints);

        dragon.SetFlightPath(remountFlightPath, resetProgress: true);
        dragon.SetMountEnabled(true);
        SyncRespawnAndCheckpoint(crystalRespawnAnchor != null ? crystalRespawnAnchor : crystalDismountAnchor);
    }

    private void HandleDragonDismounted()
    {
        if (stage == SequenceStage.LandingAtRover)
            CompleteRoverHandoff();
    }

    private void BuildCrystalAnchors()
    {
        Vector3 crystalObjectiveCenter = GetCrystalObjectiveCenter();
        bool hasCrystalObjective = crystalObjectiveCenter != Vector3.zero;
        bool hasStopWaypoint = crystalStopWaypoint != null;
        if (!hasCrystalObjective && !hasStopWaypoint)
            return;

        Vector3 landingReferencePoint = hasStopWaypoint
            ? crystalStopWaypoint.position
            : crystalObjectiveCenter + ResolveCrystalApproachDirection() * crystalPlatformDistance;

        Vector3 platformGroundPoint = ResolveGroundPoint(landingReferencePoint);
        Quaternion anchorRotation = GetAnchorFacingRotation(platformGroundPoint);

        crystalLandingAnchor = CreateOrMoveAnchor(
            "CrystalLandingAnchor_Runtime",
            platformGroundPoint + Vector3.up * crystalLandingHeight,
            anchorRotation);

        crystalDismountAnchor = CreateOrMoveAnchor(
            "CrystalDismountAnchor_Runtime",
            platformGroundPoint + Vector3.up * crystalDismountHeight,
            anchorRotation);
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

    private void UpdateCrystalRespawnAnchor()
    {
        if (crystalDismountAnchor == null)
            return;

        float respawnYaw = xrOrigin != null
            ? xrOrigin.transform.eulerAngles.y
            : crystalDismountAnchor.eulerAngles.y;
        Quaternion respawnRotation = Quaternion.Euler(0f, respawnYaw, 0f);

        crystalRespawnAnchor = CreateOrMoveAnchor(
            "CrystalRespawnAnchor_Runtime",
            crystalDismountAnchor.position,
            respawnRotation);
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

    private void CompleteRoverHandoff()
    {
        stage = SequenceStage.Completed;
        dragon.SetScriptedSequenceActive(false, lockJetpackWhileUnmounted: false);
        dragon.SetMountEnabled(false);

        if (rover != null)
            rover.SetMountEnabled(true);

        SyncRespawnAndCheckpoint(roverDismountAnchor);
    }

    private Transform BuildCrystalApproachWaypoint(List<Transform> orderedWaypoints, int stopIndex)
    {
        if (stopIndex < 0 || stopIndex >= orderedWaypoints.Count)
            return null;

        Transform stopWaypoint = orderedWaypoints[stopIndex];
        if (stopWaypoint == null)
            return null;

        Vector3 incomingDirection = Vector3.forward;
        if (stopIndex > 0 && orderedWaypoints[stopIndex - 1] != null)
        {
            incomingDirection = stopWaypoint.position - orderedWaypoints[stopIndex - 1].position;
            incomingDirection.y = 0f;
        }

        if (incomingDirection.sqrMagnitude < 0.01f)
            incomingDirection = ResolveCrystalApproachDirection();

        Vector3 adjustedPosition = stopWaypoint.position + Vector3.up * crystalApproachLift;
        if (incomingDirection.sqrMagnitude >= 0.01f)
            adjustedPosition -= incomingDirection.normalized * crystalApproachPullback;

        Quaternion adjustedRotation = Quaternion.Euler(0f, stopWaypoint.eulerAngles.y, 0f);
        return CreateOrMoveAnchor("CrystalApproachWaypoint_Runtime", adjustedPosition, adjustedRotation);
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

    private Vector3 ResolveCrystalApproachDirection()
    {
        Vector3 crystalObjectiveCenter = GetCrystalObjectiveCenter();
        Vector3 referencePoint = crystalStopWaypoint != null
            ? crystalStopWaypoint.position
            : playerStartAnchor != null
                ? playerStartAnchor.position
                : dragon != null
                    ? dragon.transform.position
                    : crystalObjectiveCenter + Vector3.back;

        Vector3 direction = referencePoint - crystalObjectiveCenter;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            direction = Vector3.back;

        return direction.normalized;
    }

    private Vector3 ResolveGroundPoint(Vector3 desiredPosition)
    {
        Vector3 rayOrigin = desiredPosition + Vector3.up * groundProbeHeight;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundProbeHeight + groundProbeDistance, ~0, QueryTriggerInteraction.Ignore))
            return hit.point;

        return desiredPosition;
    }

    private Quaternion GetAnchorFacingRotation(Vector3 anchorPosition)
    {
        Vector3 lookDirection = GetCrystalObjectiveCenter() - anchorPosition;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude < 0.01f)
            return dragon != null ? dragon.transform.rotation : Quaternion.identity;

        return Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
    }

    private CrystalCollectible ResolveTargetCrystal()
    {
        Transform crystalTransform = FindSceneTransform("Crystal1") ?? FindSceneTransform("Crystal01");
        return crystalTransform != null ? crystalTransform.GetComponent<CrystalCollectible>() : null;
    }

    private int FindStopWaypointIndex(List<Transform> orderedWaypoints)
    {
        int configuredStopOrder = ExtractWaypointOrder(crystalStopWaypointName);
        int namedIndex = configuredStopOrder != int.MaxValue
            ? orderedWaypoints.FindIndex(waypoint => GetWaypointOrder(waypoint) == configuredStopOrder)
            : orderedWaypoints.FindIndex(waypoint => waypoint.name == crystalStopWaypointName);
        if (namedIndex >= 1)
            return namedIndex;

        if (resolvedCrystals.Count == 0 && targetCrystal == null)
            return Mathf.Clamp(orderedWaypoints.Count / 2, 1, orderedWaypoints.Count - 1);

        int nearestIndex = 1;
        float nearestDistance = float.MaxValue;
        Vector3 crystalObjectiveCenter = GetCrystalObjectiveCenter();

        for (int i = 1; i < orderedWaypoints.Count - 1; i++)
        {
            float distance = Vector3.Distance(orderedWaypoints[i].position, crystalObjectiveCenter);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    private void ResolveRequiredCrystals()
    {
        resolvedCrystals.Clear();

        IEnumerable<CrystalCollectible> crystals = requiredCrystals.Where(crystal => crystal != null);
        if (!crystals.Any())
            crystals = ResolveSceneCrystals();

        foreach (CrystalCollectible crystal in crystals.Distinct())
            resolvedCrystals.Add(crystal);

        requiredCrystals.Clear();
        requiredCrystals.AddRange(resolvedCrystals);
    }

    private IEnumerable<CrystalCollectible> ResolveSceneCrystals()
    {
        Transform crystalRoot = FindSceneTransform("Crystals");

        IEnumerable<CrystalCollectible> crystals = Resources.FindObjectsOfTypeAll<CrystalCollectible>()
            .Where(crystal =>
                crystal != null &&
                crystal.hideFlags == HideFlags.None &&
                crystal.gameObject.scene.IsValid());

        if (crystalRoot != null)
            crystals = crystals.Where(crystal => crystal.transform != null && crystal.transform.IsChildOf(crystalRoot));

        return crystals.OrderBy(crystal => crystal.name);
    }

    private void SetObjectiveCrystalsActive(bool active)
    {
        for (int i = 0; i < resolvedCrystals.Count; i++)
        {
            CrystalCollectible crystal = resolvedCrystals[i];
            if (crystal != null)
                crystal.gameObject.SetActive(active);
        }
    }

    private void SetObjectiveCrystalCollectionEnabled(bool enabled)
    {
        for (int i = 0; i < resolvedCrystals.Count; i++)
        {
            CrystalCollectible crystal = resolvedCrystals[i];
            if (crystal != null)
                crystal.SetCollectionEnabled(enabled);
        }
    }

    private bool AreAllRequiredCrystalsCollected()
    {
        int crystalCount = 0;

        for (int i = 0; i < resolvedCrystals.Count; i++)
        {
            CrystalCollectible crystal = resolvedCrystals[i];
            if (crystal == null)
                continue;

            crystalCount++;
            if (!crystal.IsCollected)
                return false;
        }

        return crystalCount > 0;
    }

    private Vector3 GetCrystalObjectiveCenter()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;

        for (int i = 0; i < resolvedCrystals.Count; i++)
        {
            CrystalCollectible crystal = resolvedCrystals[i];
            if (crystal == null)
                continue;

            sum += crystal.transform.position;
            count++;
        }

        if (count > 0)
            return sum / count;

        return targetCrystal != null ? targetCrystal.transform.position : Vector3.zero;
    }

    private void SyncRespawnAndCheckpoint(Transform anchor)
    {
        if (anchor == null)
            return;

        if (respawnManager != null)
        {
            respawnManager.SetRespawnEnabled(true);
            respawnManager.SetRespawnWaypoint(anchor);
        }

        if (playerHealth != null)
            playerHealth.SetCheckpoint(anchor.position);
    }

    private static Transform FindWaypoint(string prefix)
    {
        return Resources.FindObjectsOfTypeAll<Transform>()
            .FirstOrDefault(transform =>
                transform != null &&
                transform.hideFlags == HideFlags.None &&
                transform.gameObject.scene.IsValid() &&
                transform.name.StartsWith(prefix));
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

    private static int GetWaypointOrder(Transform waypoint)
    {
        return waypoint == null ? int.MaxValue : ExtractWaypointOrder(waypoint.name);
    }

    private void TryTriggerCrystalLandingFromWaypoint()
    {
        if (crystalLandingTriggered || dragon == null || crystalStopWaypoint == null)
            return;

        if (Vector3.Distance(dragon.transform.position, crystalStopWaypoint.position) > crystalStopReachDistance)
            return;

        BeginCrystalLanding();
    }

    private void BeginCrystalLanding()
    {
        if (stage != SequenceStage.FlyingToCrystal || crystalLandingTriggered)
            return;

        crystalLandingTriggered = true;
        stage = SequenceStage.LandingAtCrystal;
        dragon.ParkAtTarget(crystalStopWaypoint != null ? crystalStopWaypoint : crystalLandingAnchor, crystalDismountAnchor);
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
