using UnityEngine;
using UnityEngine.AzureSky;

/// <summary>
/// Advances the Azure Sky timeline from startTimeline (at WP1) to endTimeline (at the last WP)
/// as the rover passes through each checkpoint. Add this to any GameObject in the scene.
/// Assign orderedCheckpoints in the Inspector from first to last.
/// </summary>
public class SkyTimeProgressManager : MonoBehaviour
{
    [Header("Azure Sky")]
    public AzureCoreSystem azureCoreSystem;

    [Header("Checkpoints (in order, first → last)")]
    [Tooltip("Drag all rover checkpoints here in the order the player visits them.")]
    public RoverCheckpoint[] orderedCheckpoints;

    [Header("Timeline Range")]
    [Tooltip("Sky timeline value at the first checkpoint (WP1).")]
    public float startTimeline = 22.7f;
    [Tooltip("Sky timeline value at the last checkpoint.")]
    public float endTimeline = 22.9f;

    [Header("Transition")]
    [Tooltip("How fast the sky slides to the target (timeline units per second).")]
    public float transitionSpeed = 0.05f;

    private RoverCheckpointRespawn roverRespawn;
    private RoverCheckpoint lastKnownCheckpoint;
    private float targetTimeline;

    private void Awake()
    {
        azureCoreSystem ??= FindFirstObjectByType<AzureCoreSystem>();
        roverRespawn     ??= FindFirstObjectByType<RoverCheckpointRespawn>();
    }

    private void Start()
    {
        targetTimeline = startTimeline;

        if (azureCoreSystem == null) return;

        // Keep Simple mode (preserves the correct sky look).
        // We control the timeline manually by overriding it each LateUpdate.
        azureCoreSystem.timeSystem.startTime = startTimeline;
        azureCoreSystem.timeSystem.timeline  = startTimeline;
    }

    private void Update()
    {
        if (roverRespawn == null) return;

        // Detect checkpoint change
        RoverCheckpoint current = roverRespawn.activeCheckpoint;
        if (current != lastKnownCheckpoint)
        {
            lastKnownCheckpoint = current;
            UpdateTarget(current);
        }
    }

    // LateUpdate runs after Azure's own Update, so our value wins every frame.
    private void LateUpdate()
    {
        if (azureCoreSystem == null) return;

        float now = azureCoreSystem.timeSystem.timeline;
        if (!Mathf.Approximately(now, targetTimeline))
            azureCoreSystem.timeSystem.timeline = Mathf.MoveTowards(now, targetTimeline, transitionSpeed * Time.deltaTime);
    }

    private void UpdateTarget(RoverCheckpoint checkpoint)
    {
        if (checkpoint == null || orderedCheckpoints == null || orderedCheckpoints.Length == 0)
            return;

        int index = System.Array.IndexOf(orderedCheckpoints, checkpoint);
        if (index < 0) return;

        float t = orderedCheckpoints.Length > 1
            ? (float)index / (orderedCheckpoints.Length - 1)
            : 1f;

        targetTimeline = Mathf.Lerp(startTimeline, endTimeline, t);

        Debug.Log($"<color=cyan>[SkyTimeProgress] Checkpoint '{checkpoint.name}' ({index + 1}/{orderedCheckpoints.Length}) → target timeline {targetTimeline:F3}</color>");
    }
}
