using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Builds all post-processing volumes at runtime.
///
/// SETUP:
///   1. Attach to any persistent GameObject in SceneCopyFinal.
///   2. Right-click the component header → "Setup Zone GameObjects".
///      Three child objects ([PP] Volcano Zone, etc.) appear in the Hierarchy.
///   3. Select each child and use the Move (W) / Scale (R) gizmos to position
///      and size each zone exactly where you want it.
///   4. Hit Play — volumes are built on those GameObjects automatically.
///
/// The serialised Vector3 centre/size fields are only used as DEFAULTS when
/// the zone root is missing (e.g. first time before step 2).
/// </summary>
public class PostProcessSetup : MonoBehaviour
{
    // ── Zone Root GameObjects ─────────────────────────────────────────────────
    [Header("Zone Roots — move & scale these in Scene view")]
    [Tooltip("Run 'Setup Zone GameObjects' from the context menu to create them.")]
    [SerializeField] private GameObject volcanoZoneRoot;
    [SerializeField] private GameObject islandZoneRoot;
    [SerializeField] private GameObject transitionZoneRoot;

    // ── Global ────────────────────────────────────────────────────────────────
    [Header("Global — Whole Scene Baseline")]
    [SerializeField] private float globalBloomIntensity   = 0.25f;
    [SerializeField] private float globalBloomThreshold   = 0.90f;
    [SerializeField] private float globalContrast         = 12f;
    [SerializeField] private float globalSaturation       = 15f;
    [SerializeField] private float globalVignetteIntensity = 0.25f;

    // ── Volcano Zone ──────────────────────────────────────────────────────────
    [Header("Volcano Zone — Default Position / Size")]
    [SerializeField] private Vector3 volcanoCenter = new Vector3(253f, -270f, 953f);
    [SerializeField] private Vector3 volcanoSize   = new Vector3(500f, 250f, 500f);
    [SerializeField] private float   volcanoBlend  = 60f;

    [Header("Volcano Zone — Effects")]
    [SerializeField] private float   volcBloomIntensity   = 0.50f;
    [SerializeField] private float   volcBloomThreshold   = 0.70f;
    [SerializeField] private float   volcContrast         = 20f;
    [SerializeField] private float   volcSaturation       = 10f;
    [SerializeField] private Color   volcColorFilter      = new Color(1f, 0.88f, 0.72f);
    [SerializeField] private float   volcVignetteIntensity = 0.38f;
    [SerializeField] private Vector4 volcLiftColor        = new Vector4(0.03f, 0.01f, -0.01f, 0f);

    // ── Floating Island Zone ──────────────────────────────────────────────────
    [Header("Floating Island Zone — Default Position / Size")]
    [SerializeField] private Vector3 islandCenter = new Vector3(1072f, -50f, -121f);
    [SerializeField] private Vector3 islandSize   = new Vector3(350f, 200f, 350f);
    [SerializeField] private float   islandBlend  = 50f;

    [Header("Floating Island Zone — Effects")]
    [SerializeField] private float   islandBloomIntensity   = 0.35f;
    [SerializeField] private float   islandBloomThreshold   = 0.85f;
    [SerializeField] private float   islandContrast         = 8f;
    [SerializeField] private float   islandSaturation       = 25f;
    [SerializeField] private Color   islandColorFilter      = new Color(0.92f, 0.96f, 1f);
    [SerializeField] private float   islandVignetteIntensity = 0.18f;

    // ── WP6+ Skybox Transition Zone ───────────────────────────────────────────
    [Header("WP6+ Skybox Transition Zone — Default Position / Size")]
    [SerializeField] private Vector3 transitionCenter = new Vector3(55f, 30f, 171f);
    [SerializeField] private Vector3 transitionSize   = new Vector3(380f, 120f, 280f);
    [SerializeField] private float   transitionBlend  = 60f;

    [Header("WP6+ Skybox Transition Zone — Effects")]
    [SerializeField] private float   transBloomIntensity    = 0.40f;
    [SerializeField] private float   transBloomThreshold    = 0.75f;
    [SerializeField] private float   transContrast          = 15f;
    [SerializeField] private float   transSaturation        = -10f;
    [SerializeField] private float   transPostExposure      = -0.25f;
    [SerializeField] private float   transVignetteIntensity = 0.45f;

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        CreateGlobalVolume();
        CreateVolcanoVolume();
        CreateIslandVolume();
        CreateTransitionVolume();
    }

    // ── Context menu — run this ONCE in the Editor before hitting Play ────────

    [ContextMenu("Setup Zone GameObjects")]
    private void SetupZoneGameObjects()
    {
        volcanoZoneRoot    = EnsureZoneRoot("[PP] Volcano Zone",    volcanoCenter,    volcanoSize);
        islandZoneRoot     = EnsureZoneRoot("[PP] Island Zone",     islandCenter,     islandSize);
        transitionZoneRoot = EnsureZoneRoot("[PP] Transition Zone", transitionCenter, transitionSize);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        Debug.Log("[PostProcessSetup] Zone GameObjects ready — select them and use Move (W) / Scale (R) gizmos.");
    }

    private GameObject EnsureZoneRoot(string label, Vector3 center, Vector3 size)
    {
        // Reuse existing child if present
        Transform existing = transform.Find(label);
        if (existing != null)
        {
            existing.position   = center;
            existing.localScale = size;
            return existing.gameObject;
        }

        var go = new GameObject(label);
        go.transform.SetParent(transform);
        go.transform.position   = center;
        go.transform.localScale = size;
        return go;
    }

    // ── Global ────────────────────────────────────────────────────────────────

    private void CreateGlobalVolume()
    {
        var go  = new GameObject("[PP] Global");
        go.transform.SetParent(transform);

        var vol = go.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 0f;
        vol.profile  = BuildGlobalProfile();
    }

    private VolumeProfile BuildGlobalProfile()
    {
        var p = ScriptableObject.CreateInstance<VolumeProfile>();

        var tone = p.Add<Tonemapping>(true);
        tone.mode.Override(TonemappingMode.ACES);

        var bloom = p.Add<Bloom>(true);
        bloom.intensity.Override(globalBloomIntensity);
        bloom.threshold.Override(globalBloomThreshold);
        bloom.scatter.Override(0.7f);

        var ca = p.Add<ColorAdjustments>(true);
        ca.contrast.Override(globalContrast);
        ca.saturation.Override(globalSaturation);

        var vig = p.Add<Vignette>(true);
        vig.intensity.Override(globalVignetteIntensity);
        vig.smoothness.Override(0.5f);
        vig.rounded.Override(true);

        return p;
    }

    // ── Volcano ───────────────────────────────────────────────────────────────

    private void CreateVolcanoVolume()
    {
        var go = GetOrBuildZone(volcanoZoneRoot, "[PP] Volcano Zone", volcanoCenter, volcanoSize);
        AttachCollider(go);

        var vol = go.GetComponent<Volume>() ?? go.AddComponent<Volume>();
        vol.isGlobal      = false;
        vol.priority      = 1f;
        vol.blendDistance = volcanoBlend;
        vol.profile       = BuildVolcanoProfile();
    }

    private VolumeProfile BuildVolcanoProfile()
    {
        var p = ScriptableObject.CreateInstance<VolumeProfile>();

        var bloom = p.Add<Bloom>(true);
        bloom.intensity.Override(volcBloomIntensity);
        bloom.threshold.Override(volcBloomThreshold);
        bloom.scatter.Override(0.75f);

        var ca = p.Add<ColorAdjustments>(true);
        ca.contrast.Override(volcContrast);
        ca.saturation.Override(volcSaturation);
        ca.colorFilter.Override(volcColorFilter);

        var lgg = p.Add<LiftGammaGain>(true);
        lgg.lift.Override(new Vector4(volcLiftColor.x, volcLiftColor.y, volcLiftColor.z, 0f));

        var vig = p.Add<Vignette>(true);
        vig.intensity.Override(volcVignetteIntensity);
        vig.smoothness.Override(0.6f);
        vig.rounded.Override(true);

        return p;
    }

    // ── Floating Island ───────────────────────────────────────────────────────

    private void CreateIslandVolume()
    {
        var go = GetOrBuildZone(islandZoneRoot, "[PP] Island Zone", islandCenter, islandSize);
        AttachCollider(go);

        var vol = go.GetComponent<Volume>() ?? go.AddComponent<Volume>();
        vol.isGlobal      = false;
        vol.priority      = 1f;
        vol.blendDistance = islandBlend;
        vol.profile       = BuildIslandProfile();
    }

    private VolumeProfile BuildIslandProfile()
    {
        var p = ScriptableObject.CreateInstance<VolumeProfile>();

        var bloom = p.Add<Bloom>(true);
        bloom.intensity.Override(islandBloomIntensity);
        bloom.threshold.Override(islandBloomThreshold);
        bloom.scatter.Override(0.65f);

        var ca = p.Add<ColorAdjustments>(true);
        ca.contrast.Override(islandContrast);
        ca.saturation.Override(islandSaturation);
        ca.colorFilter.Override(islandColorFilter);

        var vig = p.Add<Vignette>(true);
        vig.intensity.Override(islandVignetteIntensity);
        vig.smoothness.Override(0.4f);
        vig.rounded.Override(true);

        return p;
    }

    // ── WP6+ Transition ───────────────────────────────────────────────────────

    private void CreateTransitionVolume()
    {
        var go = GetOrBuildZone(transitionZoneRoot, "[PP] Transition Zone", transitionCenter, transitionSize);
        AttachCollider(go);

        var vol = go.GetComponent<Volume>() ?? go.AddComponent<Volume>();
        vol.isGlobal      = false;
        vol.priority      = 1f;
        vol.blendDistance = transitionBlend;
        vol.profile       = BuildTransitionProfile();
    }

    private VolumeProfile BuildTransitionProfile()
    {
        var p = ScriptableObject.CreateInstance<VolumeProfile>();

        var bloom = p.Add<Bloom>(true);
        bloom.intensity.Override(transBloomIntensity);
        bloom.threshold.Override(transBloomThreshold);
        bloom.scatter.Override(0.8f);

        var ca = p.Add<ColorAdjustments>(true);
        ca.contrast.Override(transContrast);
        ca.saturation.Override(transSaturation);
        ca.postExposure.Override(transPostExposure);

        var vig = p.Add<Vignette>(true);
        vig.intensity.Override(transVignetteIntensity);
        vig.smoothness.Override(0.65f);
        vig.rounded.Override(true);

        return p;
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the serialised zone root if assigned, otherwise creates a
    /// temporary runtime-only GameObject at the default position/size.
    /// </summary>
    private GameObject GetOrBuildZone(GameObject root, string label, Vector3 center, Vector3 size)
    {
        if (root != null) return root;

        var go = new GameObject(label);
        go.transform.SetParent(transform);
        go.transform.position   = center;
        go.transform.localScale = size;
        return go;
    }

    /// <summary>
    /// Adds a BoxCollider sized to Vector3.one — the GameObject's localScale
    /// drives the actual world-space volume extent.
    /// </summary>
    private static void AttachCollider(GameObject go)
    {
        var col = go.GetComponent<BoxCollider>() ?? go.AddComponent<BoxCollider>();
        col.size      = Vector3.one;
        col.center    = Vector3.zero;
        col.isTrigger = false;
    }

    // ── Gizmos — visible in Scene view even outside Play mode ─────────────────

    private void OnDrawGizmos()
    {
        DrawZone(volcanoZoneRoot,    volcanoCenter,    volcanoSize,    new Color(1f, 0.4f, 0f, 0.12f),   new Color(1f, 0.4f, 0f, 0.8f));
        DrawZone(islandZoneRoot,     islandCenter,     islandSize,     new Color(0.4f, 0.8f, 1f, 0.12f), new Color(0.4f, 0.8f, 1f, 0.8f));
        DrawZone(transitionZoneRoot, transitionCenter, transitionSize, new Color(0.5f, 0.2f, 1f, 0.12f), new Color(0.5f, 0.2f, 1f, 0.8f));
    }

    private static void DrawZone(GameObject root, Vector3 defaultCenter, Vector3 defaultSize, Color fill, Color wire)
    {
        Vector3 center = root != null ? root.transform.position   : defaultCenter;
        Vector3 size   = root != null ? root.transform.localScale : defaultSize;

        Gizmos.color = fill;
        Gizmos.DrawCube(center, size);
        Gizmos.color = wire;
        Gizmos.DrawWireCube(center, size);
    }
}
