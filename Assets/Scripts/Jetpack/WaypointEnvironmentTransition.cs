using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[DefaultExecutionOrder(-50)]
public class WaypointEnvironmentTransition : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private SkyboxBlendController blendController;
    [SerializeField] private RideableCreature rideableCreature;
    [SerializeField] private SkyboxByHeight legacyHeightController;

    [Header("Transition Waypoints")]
    [SerializeField] private Transform waypoint08;
    [SerializeField] private Transform waypoint09;
    [SerializeField] private Transform waypoint10;

    [Header("Skybox Materials")]
    [SerializeField] private Material blendedSkyboxMaterial;
    [SerializeField] private Material finalSkyboxMaterial;
    [SerializeField, Range(0.9f, 1f)] private float finalSkyboxSwapThreshold = 0.98f;

    [Header("Start Environment")]
    [SerializeField] private Color startSkyColor = new(0.8307701f, 0.63075733f, 0.36130688f, 1f);
    [SerializeField] private Color startEquatorColor = new(0.78353804f, 0.9386859f, 0.57758063f, 1f);
    [SerializeField] private Color startGroundColor = new(0.78353804f, 0.4341537f, 0.4341537f, 1f);
    [SerializeField] private bool startFogEnabled = false;
    [SerializeField] private Color startFogColor = new(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private float startFogStartDistance = 0f;
    [SerializeField] private float startFogEndDistance = 300f;

    [Header("End Environment")]
    [SerializeField] private Color endSkyColor = new(0.596f, 0.839f, 0.639f, 1f);
    [SerializeField] private Color endEquatorColor = new(0.855f, 0.678f, 0.247f, 1f);
    [SerializeField] private Color endGroundColor = new(0.835f, 0.517f, 0.208f, 1f);
    [SerializeField] private bool endFogEnabled = true;
    [SerializeField] private Color endFogColor = new(0.702f, 0.486f, 0.071f, 1f);
    [SerializeField] private float endFogStartDistance = 60f;
    [SerializeField] private float endFogEndDistance = 200f;
    [SerializeField] private FogMode fogMode = FogMode.Linear;

    private bool hasStartedTransition;

    private void Reset()
    {
        AutoAssignReferences();
    }

    private void OnValidate()
    {
        AutoAssignReferences();
    }

    private void Start()
    {
        AutoAssignReferences();
        ConfigureBlendController();

        if (legacyHeightController != null)
            legacyHeightController.enabled = false;

        blendController?.SetBlendImmediate(0f);
        ApplyEnvironment(0f);
    }

    private void Update()
    {
        if (blendController == null)
            return;

        float targetBlend = ComputeTargetBlend();
        if (targetBlend > 0.001f)
            hasStartedTransition = true;

        blendController.SetTargetBlend(targetBlend);
    }

    private void LateUpdate()
    {
        if (blendController == null)
            return;

        ApplyEnvironment(blendController.CurrentBlend);
    }

    private void AutoAssignReferences()
    {
        blendController ??= GetComponent<SkyboxBlendController>();
        rideableCreature ??= FindFirstObjectByType<RideableCreature>();
        legacyHeightController ??= GetComponent<SkyboxByHeight>();

        waypoint08 ??= FindWaypointByPrefix("WP08");
        waypoint09 ??= FindWaypointByPrefix("WP09");
        waypoint10 ??= FindWaypointByPrefix("WP10");

        if (blendedSkyboxMaterial == null && blendController != null)
            blendedSkyboxMaterial = blendController.blendedSkyboxMaterial;

#if UNITY_EDITOR
        if (finalSkyboxMaterial == null)
            finalSkyboxMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/SkySeries Freebie/DarkStorm.mat");
#endif
    }

    private void ConfigureBlendController()
    {
        if (blendController == null)
            return;

        blendController.blendMode = SkyboxBlendController.BlendMode.Manual;
        blendController.useFogTransition = false;
        blendController.transitionDelay = 0f;

        if (blendedSkyboxMaterial != null)
            blendController.blendedSkyboxMaterial = blendedSkyboxMaterial;
    }

    private float ComputeTargetBlend()
    {
        if (rideableCreature == null)
            return hasStartedTransition ? blendController.CurrentBlend : 0f;

        if (!rideableCreature.IsFlying)
            return hasStartedTransition ? blendController.CurrentBlend : 0f;

        Transform currentWaypoint = rideableCreature.CurrentWaypoint;
        if (currentWaypoint == null)
            return hasStartedTransition ? blendController.CurrentBlend : 0f;

        string currentWaypointName = currentWaypoint.name;
        if (currentWaypointName.StartsWith("WP08"))
            return ComputeSegmentBlend(waypoint08, waypoint09, rideableCreature.transform.position, 0f, 0.5f);

        if (currentWaypointName.StartsWith("WP09"))
            return ComputeSegmentBlend(waypoint09, waypoint10, rideableCreature.transform.position, 0.5f, 1f);

        if (currentWaypointName.StartsWith("WP10"))
            return 1f;

        int waypointNumber = ExtractWaypointNumber(currentWaypointName);
        if (waypointNumber >= 10)
            return 1f;

        if (waypointNumber < 8)
            return 0f;

        return hasStartedTransition ? blendController.CurrentBlend : 0f;
    }

    private void ApplyEnvironment(float blend)
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Color.Lerp(startSkyColor, endSkyColor, blend);
        RenderSettings.ambientEquatorColor = Color.Lerp(startEquatorColor, endEquatorColor, blend);
        RenderSettings.ambientGroundColor = Color.Lerp(startGroundColor, endGroundColor, blend);

        RenderSettings.fogMode = fogMode;
        RenderSettings.fogColor = Color.Lerp(startFogColor, endFogColor, blend);
        RenderSettings.fogStartDistance = Mathf.Lerp(startFogStartDistance, endFogStartDistance, blend);
        RenderSettings.fogEndDistance = Mathf.Lerp(startFogEndDistance, endFogEndDistance, blend);

        if (startFogEnabled == endFogEnabled)
        {
            RenderSettings.fog = startFogEnabled;
        }
        else
        {
            RenderSettings.fog = blend > 0.001f ? endFogEnabled : startFogEnabled;
        }

        Material desiredSkybox = blend >= finalSkyboxSwapThreshold && finalSkyboxMaterial != null
            ? finalSkyboxMaterial
            : blendedSkyboxMaterial;

        if (desiredSkybox != null && RenderSettings.skybox != desiredSkybox)
        {
            RenderSettings.skybox = desiredSkybox;
            DynamicGI.UpdateEnvironment();
        }
    }

    private static float ComputeSegmentBlend(Transform from, Transform to, Vector3 currentPosition, float minBlend, float maxBlend)
    {
        if (from == null || to == null)
            return minBlend;

        Vector3 segment = to.position - from.position;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared <= 0.0001f)
            return maxBlend;

        float t = Vector3.Dot(currentPosition - from.position, segment) / lengthSquared;
        return Mathf.Lerp(minBlend, maxBlend, Mathf.Clamp01(t));
    }

    private static int ExtractWaypointNumber(string waypointName)
    {
        if (string.IsNullOrEmpty(waypointName) || waypointName.Length < 4 || waypointName[0] != 'W' || waypointName[1] != 'P')
            return -1;

        if (!char.IsDigit(waypointName[2]) || !char.IsDigit(waypointName[3]))
            return -1;

        return (waypointName[2] - '0') * 10 + (waypointName[3] - '0');
    }

    private static Transform FindWaypointByPrefix(string prefix)
    {
        Transform[] allTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform sceneTransform in allTransforms)
        {
            if (sceneTransform != null && sceneTransform.name.StartsWith(prefix))
                return sceneTransform;
        }

        return null;
    }
}
