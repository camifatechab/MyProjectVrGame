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
    [SerializeField] private Transform waypoint06;
    [SerializeField] private Transform waypoint07;
    [SerializeField] private Transform waypoint08;
    [SerializeField] private Transform waypoint09;
    [SerializeField] private Transform waypoint10;
    [SerializeField, Range(0f, 1f)] private float blendStartOnWp06ToWp07 = 0.45f;

    [Header("Skybox Materials")]
    [SerializeField] private Material blendedSkyboxMaterial;
    [SerializeField] private Material finalSkyboxMaterial;
    [SerializeField] private Cubemap finalBlendCubemap;

    [Header("Blend Smoothing")]
    [SerializeField] private float manualBlendResponse = 2.25f;

    [Header("Start Environment")]
    [SerializeField] private Color startSkyColor = new(0.8307701f, 0.63075733f, 0.36130688f, 1f);
    [SerializeField] private Color startEquatorColor = new(0.78353804f, 0.9386859f, 0.57758063f, 1f);
    [SerializeField] private Color startGroundColor = new(0.78353804f, 0.4341537f, 0.4341537f, 1f);
    [SerializeField] private bool startFogEnabled = false;
    [SerializeField] private Color startFogColor = new(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private float startFogStartDistance = 0f;
    [SerializeField] private float startFogEndDistance = 300f;

    [Header("End Environment")]
    [SerializeField] private Color endSkyColor = new(0.62f, 0.24f, 0.18f, 1f);
    [SerializeField] private Color endEquatorColor = new(0.8f, 0.34f, 0.18f, 1f);
    [SerializeField] private Color endGroundColor = new(0.38f, 0.12f, 0.09f, 1f);
    [SerializeField] private bool endFogEnabled = true;
    [SerializeField] private Color endFogColor = new(0.66f, 0.2f, 0.1f, 1f);
    [SerializeField] private float endFogStartDistance = 45f;
    [SerializeField] private float endFogEndDistance = 150f;
    [SerializeField] private FogMode fogMode = FogMode.Linear;

    private bool hasStartedTransition;
    private Material originalSkyboxMaterial;
    private Material runtimeBlendMaterial;

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

    private void OnDestroy()
    {
        if (runtimeBlendMaterial != null)
        {
            if (RenderSettings.skybox == runtimeBlendMaterial && originalSkyboxMaterial != null)
                RenderSettings.skybox = originalSkyboxMaterial;

            if (Application.isPlaying)
                Destroy(runtimeBlendMaterial);
            else
                DestroyImmediate(runtimeBlendMaterial);

            runtimeBlendMaterial = null;
        }
    }

    private void AutoAssignReferences()
    {
        blendController ??= GetComponent<SkyboxBlendController>();
        rideableCreature ??= FindFirstObjectByType<RideableCreature>();
        legacyHeightController ??= GetComponent<SkyboxByHeight>();

        waypoint06 ??= FindWaypointByPrefix("WP06");
        waypoint07 ??= FindWaypointByPrefix("WP07");
        waypoint08 ??= FindWaypointByPrefix("WP08");
        waypoint09 ??= FindWaypointByPrefix("WP09");
        waypoint10 ??= FindWaypointByPrefix("WP10");

        if (blendedSkyboxMaterial == null && blendController != null)
            blendedSkyboxMaterial = blendController.blendedSkyboxMaterial;

        if (finalBlendCubemap == null && finalSkyboxMaterial != null && finalSkyboxMaterial.HasProperty("_Tex"))
            finalBlendCubemap = finalSkyboxMaterial.GetTexture("_Tex") as Cubemap;

#if UNITY_EDITOR
        if (finalSkyboxMaterial == null)
            finalSkyboxMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/SkySeries Freebie/UnearthlyRed.mat");

        if (finalBlendCubemap == null)
            finalBlendCubemap = AssetDatabase.LoadAssetAtPath<Cubemap>("Assets/SkySeries Freebie/FreebieHdri/UnearthlyRed4k.hdr");
#endif
    }

    private void ConfigureBlendController()
    {
        if (blendController == null)
            return;

        blendController.blendMode = SkyboxBlendController.BlendMode.Manual;
        blendController.useFogTransition = false;
        blendController.transitionDelay = 0f;
        blendController.transitionSpeed = manualBlendResponse;

        originalSkyboxMaterial = RenderSettings.skybox;
        runtimeBlendMaterial = CreateRuntimeBlendMaterial();
        blendController.blendedSkyboxMaterial = runtimeBlendMaterial != null
            ? runtimeBlendMaterial
            : blendedSkyboxMaterial;
    }

    private Material CreateRuntimeBlendMaterial()
    {
        if (blendedSkyboxMaterial == null)
            return null;

        Material runtimeMaterial = new Material(blendedSkyboxMaterial)
        {
            name = $"{blendedSkyboxMaterial.name}_RideRuntime"
        };

        if (finalBlendCubemap != null && runtimeMaterial.HasProperty("_SkyboxB"))
            runtimeMaterial.SetTexture("_SkyboxB", finalBlendCubemap);

        if (runtimeMaterial.HasProperty("_Blend"))
            runtimeMaterial.SetFloat("_Blend", 0f);

        return runtimeMaterial;
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

        return EvaluateBlendWindow(
            ExtractWaypointNumber(currentWaypoint.name),
            rideableCreature.transform.position,
            GetBlendStartPosition(),
            waypoint07 != null ? waypoint07.position : Vector3.zero,
            waypoint08 != null ? waypoint08.position : Vector3.zero,
            waypoint09 != null ? waypoint09.position : Vector3.zero);
    }

    private Vector3 GetBlendStartPosition()
    {
        if (waypoint06 == null && waypoint07 == null)
            return Vector3.zero;

        if (waypoint06 == null)
            return waypoint07.position;

        if (waypoint07 == null)
            return waypoint06.position;

        return Vector3.Lerp(waypoint06.position, waypoint07.position, blendStartOnWp06ToWp07);
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

        Material desiredSkybox = runtimeBlendMaterial != null ? runtimeBlendMaterial : blendedSkyboxMaterial;
        if (desiredSkybox != null && RenderSettings.skybox != desiredSkybox)
        {
            RenderSettings.skybox = desiredSkybox;
            DynamicGI.UpdateEnvironment();
        }
    }

    public static float EvaluateBlendWindow(
        int waypointNumber,
        Vector3 currentPosition,
        Vector3 blendStartPosition,
        Vector3 waypoint07Position,
        Vector3 waypoint08Position,
        Vector3 waypoint09Position)
    {
        if (waypointNumber < 6)
            return 0f;

        float firstSegmentLength = Vector3.Distance(blendStartPosition, waypoint07Position);
        float secondSegmentLength = Vector3.Distance(waypoint07Position, waypoint08Position);
        float thirdSegmentLength = Vector3.Distance(waypoint08Position, waypoint09Position);
        float totalLength = firstSegmentLength + secondSegmentLength + thirdSegmentLength;

        if (totalLength <= 0.001f)
            return waypointNumber >= 9 ? 1f : 0f;

        float travelled;
        if (waypointNumber == 6)
        {
            travelled = ProjectDistanceOnSegment(blendStartPosition, waypoint07Position, currentPosition);
        }
        else if (waypointNumber == 7)
        {
            travelled = firstSegmentLength +
                ProjectDistanceOnSegment(waypoint07Position, waypoint08Position, currentPosition);
        }
        else if (waypointNumber == 8)
        {
            travelled = firstSegmentLength + secondSegmentLength +
                ProjectDistanceOnSegment(waypoint08Position, waypoint09Position, currentPosition);
        }
        else
        {
            travelled = totalLength;
        }

        float normalized = Mathf.Clamp01(travelled / totalLength);
        return normalized * normalized * (3f - 2f * normalized);
    }

    private static float ProjectDistanceOnSegment(Vector3 start, Vector3 end, Vector3 position)
    {
        Vector3 segment = end - start;
        float length = segment.magnitude;
        if (length <= 0.001f)
            return 0f;

        Vector3 direction = segment / length;
        float projected = Vector3.Dot(position - start, direction);
        return Mathf.Clamp(projected, 0f, length);
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
