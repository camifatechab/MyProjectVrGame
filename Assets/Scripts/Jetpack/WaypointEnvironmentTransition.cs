using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-50)]
public class WaypointEnvironmentTransition : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private SkyboxBlendController blendController;
    [SerializeField] private RideableCreature rideableCreature;
    [SerializeField] private SkyboxByHeight legacyHeightController;

    [Header("Skybox Blend Material")]
    [Tooltip("BlendedSkybox.mat — uses the SkyboxBlend shader with _SkyboxA and _SkyboxB slots")]
    [SerializeField] private Material blendedSkyboxMaterial;

    [Header("Cubemap Sources")]
    [Tooltip("Start cubemap — extracted from CamiSkybox1")]
    [SerializeField] private Cubemap startCubemap;
    [Tooltip("End cubemap — extracted from CamiSkybox2 (assign in Inspector)")]
    [SerializeField] private Cubemap endCubemap;

    [Header("Transition Waypoints (wp6 to wp10)")]
    [SerializeField] private Transform waypoint06;
    [SerializeField] private Transform waypoint07;
    [SerializeField] private Transform waypoint08;
    [SerializeField] private Transform waypoint09;
    [SerializeField] private Transform waypoint10;
    [SerializeField, Range(0f, 1f)] private float blendStartOnWp06ToWp07 = 0.45f;

    [Header("Blend Settings")]
    [SerializeField] private float manualBlendResponse = 2.25f;

    private bool hasStartedTransition;
    private Material runtimeBlendMaterial;

    private void Start()
    {
        AutoAssignReferences();
        DisableLegacyController();
        SetupSkybox();
    }

    private void Update()
    {
        if (blendController == null) return;

        float targetBlend = ComputeTargetBlend();
        if (targetBlend > 0.001f)
            hasStartedTransition = true;

        blendController.SetTargetBlend(targetBlend);
    }

    private void OnDestroy()
    {
        if (runtimeBlendMaterial == null) return;

        if (Application.isPlaying)
            Destroy(runtimeBlendMaterial);
        else
            DestroyImmediate(runtimeBlendMaterial);

        runtimeBlendMaterial = null;
    }

    private void AutoAssignReferences()
    {
        blendController      ??= GetComponent<SkyboxBlendController>();
        rideableCreature     ??= FindFirstObjectByType<RideableCreature>();
        legacyHeightController ??= GetComponent<SkyboxByHeight>();

        waypoint06 ??= FindWaypointByPrefix("WP06");
        waypoint07 ??= FindWaypointByPrefix("WP07");
        waypoint08 ??= FindWaypointByPrefix("WP08");
        waypoint09 ??= FindWaypointByPrefix("WP09");
        waypoint10 ??= FindWaypointByPrefix("WP10");
    }

    private void DisableLegacyController()
    {
        if (legacyHeightController != null)
            legacyHeightController.enabled = false;
    }

    private void SetupSkybox()
    {
        if (blendController == null || blendedSkyboxMaterial == null) return;

        // Configure blend controller — manual mode, no fog
        blendController.blendMode        = SkyboxBlendController.BlendMode.Manual;
        blendController.useFogTransition  = false;
        blendController.transitionDelay   = 0f;
        blendController.transitionSpeed   = manualBlendResponse;

        // Runtime copy so we never modify the source asset
        runtimeBlendMaterial = new Material(blendedSkyboxMaterial)
        {
            name = blendedSkyboxMaterial.name + "_Runtime"
        };

        // Wire the cubemaps
        if (startCubemap != null && runtimeBlendMaterial.HasProperty("_SkyboxA"))
            runtimeBlendMaterial.SetTexture("_SkyboxA", startCubemap);

        if (endCubemap != null && runtimeBlendMaterial.HasProperty("_SkyboxB"))
            runtimeBlendMaterial.SetTexture("_SkyboxB", endCubemap);

        if (runtimeBlendMaterial.HasProperty("_Blend"))
            runtimeBlendMaterial.SetFloat("_Blend", 0f);

        // Hand the runtime material to the blend controller and apply to scene
        blendController.blendedSkyboxMaterial = runtimeBlendMaterial;
        blendController.SetBlendImmediate(0f);
        RenderSettings.skybox = runtimeBlendMaterial;
        DynamicGI.UpdateEnvironment();
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

        int wpNum = ExtractWaypointNumber(currentWaypoint.name);

        if (wpNum < 6)  return 0f;
        if (wpNum >= 10) return 1f;

        return EvaluateBlendWindow(
            wpNum,
            rideableCreature.transform.position,
            GetBlendStartPosition(),
            waypoint07 != null ? waypoint07.position : Vector3.zero,
            waypoint08 != null ? waypoint08.position : Vector3.zero,
            waypoint09 != null ? waypoint09.position : Vector3.zero,
            waypoint10 != null ? waypoint10.position : Vector3.zero);
    }

    private Vector3 GetBlendStartPosition()
    {
        if (waypoint06 == null && waypoint07 == null) return Vector3.zero;
        if (waypoint06 == null) return waypoint07.position;
        if (waypoint07 == null) return waypoint06.position;
        return Vector3.Lerp(waypoint06.position, waypoint07.position, blendStartOnWp06ToWp07);
    }

    private static float EvaluateBlendWindow(
        int wpNum, Vector3 pos,
        Vector3 start, Vector3 wp07, Vector3 wp08, Vector3 wp09, Vector3 wp10)
    {
        float seg1 = Vector3.Distance(start, wp07);
        float seg2 = Vector3.Distance(wp07,  wp08);
        float seg3 = Vector3.Distance(wp08,  wp09);
        float seg4 = Vector3.Distance(wp09,  wp10);
        float total = seg1 + seg2 + seg3 + seg4;

        if (total <= 0.001f) return wpNum >= 10 ? 1f : 0f;

        float travelled = wpNum switch
        {
            6 => ProjectOnSegment(start, wp07, pos),
            7 => seg1 + ProjectOnSegment(wp07, wp08, pos),
            8 => seg1 + seg2 + ProjectOnSegment(wp08, wp09, pos),
            9 => seg1 + seg2 + seg3 + ProjectOnSegment(wp09, wp10, pos),
            _ => total
        };

        float t = Mathf.Clamp01(travelled / total);
        return t * t * (3f - 2f * t); // smoothstep
    }

    private static float ProjectOnSegment(Vector3 a, Vector3 b, Vector3 p)
    {
        Vector3 seg = b - a;
        float len = seg.magnitude;
        if (len <= 0.001f) return 0f;
        return Mathf.Clamp(Vector3.Dot(p - a, seg / len), 0f, len);
    }

    private static int ExtractWaypointNumber(string name)
    {
        if (string.IsNullOrEmpty(name) || name.Length < 4) return -1;
        if (name[0] != 'W' || name[1] != 'P') return -1;
        if (!char.IsDigit(name[2]) || !char.IsDigit(name[3])) return -1;
        return (name[2] - '0') * 10 + (name[3] - '0');
    }

    private static Transform FindWaypointByPrefix(string prefix)
    {
        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t != null && t.name.StartsWith(prefix, System.StringComparison.Ordinal))
                return t;
        }
        return null;
    }
}
