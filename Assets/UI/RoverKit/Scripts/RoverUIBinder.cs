using UnityEngine;

public class RoverUIBinder : MonoBehaviour
{
    [Header("Theme")]
    [SerializeField] private RoverTheme theme;

    [Header("Anchors")]
    [SerializeField] private Transform seatAnchor;
    [SerializeField] private Transform promptAnchor;
    [SerializeField] private Transform dashboardAnchor;
    [SerializeField] private Transform warningAnchor;
    [SerializeField] private Transform leftHintAnchor;
    [SerializeField] private Transform rightHintAnchor;

    [Header("Scene References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform playerTarget;
    [SerializeField] private RoverPhysicsController roverPhysicsController;
    [SerializeField] private RoverDriver legacyRoverDriver;
    [SerializeField] private RoverRoadFailSafe roverRoadFailSafe;
    [SerializeField] private RoverCheckpointRespawn roverCheckpointRespawn;
    [SerializeField] private RoverRadioController roverRadioController;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Modules")]
    [SerializeField] private RoverSeatBeaconController seatBeacon;
    [SerializeField] private RoverMountPromptController mountPrompt;
    [SerializeField] private RoverDashboardController dashboard;

    [Header("Distances")]
    [SerializeField] private float nearbyDistance = 10f;
    [SerializeField] private float readyToMountDistance = 8f;
    [SerializeField] private bool autoFindMainCamera = true;

    public RoverTheme Theme => theme;
    public Camera PlayerCamera => playerCamera;
    public Transform PlayerTarget => playerTarget;
    public Transform SeatAnchor => seatAnchor != null ? seatAnchor : transform;
    public Transform PromptAnchor => promptAnchor != null ? promptAnchor : SeatAnchor;
    public Transform DashboardAnchor => dashboardAnchor;
    public Transform WarningAnchor => warningAnchor;
    public Transform LeftHintAnchor => leftHintAnchor;
    public Transform RightHintAnchor => rightHintAnchor;
    public float NearbyDistance => nearbyDistance;
    public float ReadyToMountDistance => readyToMountDistance;
    public float PlayerDistance { get; private set; }
    public RoverUIState CurrentState { get; private set; } = RoverUIState.Idle;
    public float ForwardSpeed => roverPhysicsController != null ? roverPhysicsController.ForwardSpeed : 0f;
    public float SpeedNormalized => roverPhysicsController != null ? roverPhysicsController.SpeedNormalized : 0f;
    public float HealthNormalized => playerHealth != null ? playerHealth.HealthNormalized : 1f;
    public string RadioDisplayText => roverRadioController != null ? roverRadioController.DisplayText : string.Empty;
    public bool IsRadioOn => roverRadioController != null && roverRadioController.IsOn;
    public bool HasRadioTracks => roverRadioController != null && roverRadioController.HasTracks;
    public bool IsResetWarningActive => roverRoadFailSafe != null && roverRoadFailSafe.IsResetWarningActive;
    public bool IsRepositioning => roverRoadFailSafe != null && roverRoadFailSafe.IsResetting;
    public bool CanReturnToRoverStart => roverRoadFailSafe != null && roverRoadFailSafe.CanReturnToRoverStart;
    public bool CanReturnToLastCheckpoint => roverCheckpointRespawn != null && roverCheckpointRespawn.HasActiveCheckpoint;
    public string LastCheckpointName => roverCheckpointRespawn != null ? roverCheckpointRespawn.ActiveCheckpointName : null;
    public bool IsMounted => roverPhysicsController != null
        ? roverPhysicsController.IsMounted
        : legacyRoverDriver != null && legacyRoverDriver.IsMounted;
    public bool CanMount => roverPhysicsController != null
        ? roverPhysicsController.CanMount
        : legacyRoverDriver != null;

    private void Reset()
    {
        AutoResolveReferences();
    }

    private void Awake()
    {
        AutoResolveReferences();
    }

    private void LateUpdate()
    {
        RefreshTracking();
        RefreshState();

        if (playerHealth != null)
            playerHealth.SetHeadHudSuppressed(IsMounted);

        if (roverRadioController != null)
            roverRadioController.Tick(IsMounted);

        if (seatBeacon != null)
            seatBeacon.Present(this);

        if (mountPrompt != null)
            mountPrompt.Present(this);

        if (dashboard != null)
            dashboard.Present(this);
    }

    private void AutoResolveReferences()
    {
        if (roverPhysicsController == null)
            roverPhysicsController = GetComponent<RoverPhysicsController>();

        if (legacyRoverDriver == null)
            legacyRoverDriver = GetComponent<RoverDriver>();

        if (roverRoadFailSafe == null)
            roverRoadFailSafe = GetComponent<RoverRoadFailSafe>();

        if (roverCheckpointRespawn == null)
            roverCheckpointRespawn = GetComponent<RoverCheckpointRespawn>();

        if (roverCheckpointRespawn == null)
            roverCheckpointRespawn = FindFirstObjectByType<RoverCheckpointRespawn>();

        if (roverRadioController == null)
            roverRadioController = GetComponent<RoverRadioController>();

        if (roverRadioController == null)
            roverRadioController = gameObject.AddComponent<RoverRadioController>();

        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (theme == null)
            theme = Resources.Load<RoverTheme>("RoverKit/DefaultRoverTheme");

        if (seatAnchor == null)
            seatAnchor = transform.Find("UI_SeatAnchor");

        if (promptAnchor == null)
            promptAnchor = transform.Find("UI_PromptAnchor");

        if (dashboardAnchor == null)
            dashboardAnchor = transform.Find("UI_DashboardAnchor");

        if (dashboardAnchor == null)
        {
            GameObject anchorObject = new("UI_DashboardAnchor");
            dashboardAnchor = anchorObject.transform;
            dashboardAnchor.SetParent(transform, false);
            dashboardAnchor.localPosition = new Vector3(0.08f, 0.84f, 0.64f);
            dashboardAnchor.localRotation = Quaternion.identity;
            dashboardAnchor.localScale = Vector3.one;
        }

        if (warningAnchor == null)
            warningAnchor = transform.Find("UI_WarningAnchor");

        if (leftHintAnchor == null)
            leftHintAnchor = transform.Find("UI_LeftHintAnchor");

        if (rightHintAnchor == null)
            rightHintAnchor = transform.Find("UI_RightHintAnchor");

        if (autoFindMainCamera && playerCamera == null)
            playerCamera = Camera.main;

        if (playerTarget == null && playerCamera != null)
            playerTarget = playerCamera.transform;

        if (seatBeacon == null)
            seatBeacon = GetComponentInChildren<RoverSeatBeaconController>(true);

        if (mountPrompt == null)
            mountPrompt = GetComponentInChildren<RoverMountPromptController>(true);

        if (dashboard == null)
            dashboard = GetComponentInChildren<RoverDashboardController>(true);

        if (dashboard == null && dashboardAnchor != null)
        {
            GameObject dashboardObject = new("DashboardPanel");
            dashboardObject.transform.SetParent(dashboardAnchor, false);
            dashboardObject.transform.localPosition = Vector3.zero;
            dashboardObject.transform.localRotation = Quaternion.identity;
            dashboardObject.transform.localScale = Vector3.one;
            dashboard = dashboardObject.AddComponent<RoverDashboardController>();
        }
    }

    private void RefreshTracking()
    {
        if (playerTarget == null && autoFindMainCamera && Camera.main != null)
        {
            playerCamera = Camera.main;
            playerTarget = playerCamera.transform;
        }

        if (playerTarget == null)
        {
            PlayerDistance = float.PositiveInfinity;
            return;
        }

        PlayerDistance = Vector3.Distance(playerTarget.position, SeatAnchor.position);
    }

    private void RefreshState()
    {
        if (playerTarget == null)
        {
            CurrentState = RoverUIState.Disabled;
            return;
        }

        if (IsMounted)
        {
            CurrentState = RoverUIState.Mounted;
            return;
        }

        if (!CanMount)
        {
            CurrentState = RoverUIState.Disabled;
            return;
        }

        if (PlayerDistance <= readyToMountDistance)
        {
            CurrentState = RoverUIState.ReadyToMount;
            return;
        }

        if (PlayerDistance <= nearbyDistance)
        {
            CurrentState = RoverUIState.PlayerNearby;
            return;
        }

        CurrentState = RoverUIState.Idle;
    }

    public void ToggleRadio()
    {
        roverRadioController?.ToggleFromUi();
    }

    public void NextRadioTrack()
    {
        roverRadioController?.NextTrackFromUi();
    }

    public bool ReturnToRoverStart()
    {
        return roverRoadFailSafe != null && roverRoadFailSafe.ReturnToRoverStartFromUi();
    }

    public bool ReturnToLastCheckpoint()
    {
        return roverCheckpointRespawn != null && roverCheckpointRespawn.RespawnAtActiveCheckpoint();
    }
}
