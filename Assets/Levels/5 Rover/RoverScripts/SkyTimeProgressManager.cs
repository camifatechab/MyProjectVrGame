using UnityEngine;
using UnityEngine.AzureSky;

/// <summary>
/// Advances the Azure Sky timeline from startTimeline (at WP01) to endTimeline (at WP10)
/// by reading the RideableCreature's FlightProgress (0..1) each frame.
/// Add this to any GameObject in the scene and assign azureCoreSystem + rideableCreature.
/// </summary>
public class SkyTimeProgressManager : MonoBehaviour
{
    [Header("Azure Sky")]
    public AzureCoreSystem azureCoreSystem;

    [Header("Flight Creature")]
    [Tooltip("The Sky_flyB creature whose FlightProgress drives the sky.")]
    public RideableCreature rideableCreature;

    [Header("Timeline Range")]
    [Tooltip("Sky timeline value at WP01 (start of flight).")]
    public float startTimeline = 22.7f;
    [Tooltip("Sky timeline value at WP10 (end of flight).")]
    public float endTimeline = 22.9f;

    [Header("Transition")]
    [Tooltip("How fast the sky slides to the target (timeline units per second).")]
    public float transitionSpeed = 0.05f;

    [Header("Debug (Play Mode Only)")]
    [Tooltip("Enable to override FlightProgress with the slider below for testing.")]
    public bool debugOverride = false;
    [Range(0f, 1f)]
    public float debugProgress = 0f;

    private float targetTimeline;

    private void Awake()
    {
        azureCoreSystem  ??= FindFirstObjectByType<AzureCoreSystem>();
        rideableCreature ??= FindFirstObjectByType<RideableCreature>();
    }

    private void Start()
    {
        targetTimeline = startTimeline;

        if (azureCoreSystem == null) return;

        azureCoreSystem.timeSystem.startTime = startTimeline;
        azureCoreSystem.timeSystem.timeline  = startTimeline;
    }

    private void Update()
    {
        float progress = debugOverride
            ? debugProgress
            : (rideableCreature != null ? rideableCreature.FlightProgress : 0f);

        targetTimeline = Mathf.Lerp(startTimeline, endTimeline, progress);
    }

    // LateUpdate runs after Azure's own Update so our override wins every frame.
    private void LateUpdate()
    {
        if (azureCoreSystem == null) return;

        float now = azureCoreSystem.timeSystem.timeline;
        if (!Mathf.Approximately(now, targetTimeline))
            azureCoreSystem.timeSystem.timeline = Mathf.MoveTowards(now, targetTimeline, transitionSpeed * Time.deltaTime);
    }
}
