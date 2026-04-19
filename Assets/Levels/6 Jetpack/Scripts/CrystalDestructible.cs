using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.XR.CoreUtils;

/// <summary>
/// Attach to the CrystalDestruction GameObject.
/// The laser calls OnLaserHit() directly (LaserShooter is patched to check for this).
/// When health reaches 0: crystal bursts, screen fades to black, MISSION COMPLETE appears.
/// Also shows a pulsing beacon ring + light once BeaconSequenceManager activates it.
/// </summary>
public class CrystalDestructible : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth       = 150f;
    public float damagePerSecond = 20f;

    [Header("Crystal Beacon (activated by BeaconSequenceManager)")]
    public Color  beaconColor      = new Color(1f, 0.55f, 0.08f, 1f);  // orange-gold
    public float  beaconPulseSpeed = 3f;
    public float  beaconRingRadius = 1.8f;
    public float  beaconLightRange = 8f;

    [Header("Danger Zone — Outer (mild, always-on in volcano area)")]
    public float outerDrainRadius    = 40f;  // world units — covers the whole approach
    public float mildDrainPerSecond  = 1f;   // HP/s — subtle pressure, ~100s to die

    [Header("Danger Zone — Inner (heavy, only once crystal is targeted)")]
    public float innerDrainRadius    = 15f;  // world units — close to crystal
    public float heavyDrainPerSecond = 4f;   // HP/s — ~25s to die, forces action

    [Header("Win Sequence Timing")]
    public float pauseBeforeFade  = 1.2f;
    public float fadeInDuration   = 1.0f;
    public float holdOnScreen     = 4.0f;

    [Header("Win Sequence Audio")]
    public AudioClip winStingerSound;

    // ── State ────────────────────────────────────────────────────
    private float        currentHealth;
    private bool         winTriggered;
    private bool         beaconActive;

    // ── Cached refs for drain (set lazily) ───────────────────────
    private PlayerHealth cachedHealth;
    private XROrigin     cachedPlayer;

    // ── Beacon visuals ───────────────────────────────────────────
    private LineRenderer beaconRing;
    private Light        beaconLight;

    // ── HP bar ───────────────────────────────────────────────────
    private Transform barFill;
    private Material  barFillMat;
    private Transform barRoot;

    // ── Win screen ───────────────────────────────────────────────
    private Canvas      winCanvas;
    private CanvasGroup winGroup;

    // ── Lifecycle ────────────────────────────────────────────────
    private void Awake()
    {
        currentHealth = maxHealth;
        BuildBeaconVisuals();
        BuildHPBar();
        FitBoxCollider();
        SetCrystalBeaconActive(false);
    }

    private void Update()
    {
        // ── Beacon pulse ─────────────────────────────────────────
        if (beaconActive && !winTriggered)
        {
            float pulse = (Mathf.Sin(Time.time * beaconPulseSpeed * Mathf.PI) + 1f) * 0.5f;

            if (beaconRing != null)
            {
                Color c = beaconColor;
                c.a = Mathf.Lerp(0.2f, 0.95f, pulse);
                beaconRing.startColor = c;
                beaconRing.endColor   = c;
            }

            if (beaconLight != null)
                beaconLight.intensity = Mathf.Lerp(1.2f, 3.5f, pulse);
        }

        // ── Volcano drain (distance-based, no trigger needed) ────
        if (!winTriggered)
            ApplyVolcanoDrain();
    }

    private void ApplyVolcanoDrain()
    {
        // Lazy-cache references
        if (cachedHealth == null) cachedHealth = FindFirstObjectByType<PlayerHealth>();
        if (cachedPlayer == null) cachedPlayer = FindFirstObjectByType<XROrigin>();
        if (cachedHealth == null || cachedPlayer == null) return;

        float dist = Vector3.Distance(transform.position, cachedPlayer.transform.position);

        // Inner heavy drain — only once the crystal is the active target
        if (beaconActive && dist < innerDrainRadius)
        {
            cachedHealth.TakeDamage(heavyDrainPerSecond * Time.deltaTime);
        }
        // Outer mild drain — always on once the player is in the volcano zone
        else if (dist < outerDrainRadius)
        {
            cachedHealth.TakeDamage(mildDrainPerSecond * Time.deltaTime);
        }
    }

    // ── Public API ───────────────────────────────────────────────

    /// <summary>Called by LaserShooter every frame the laser is on this crystal.</summary>
    public void OnLaserHit()
    {
        if (winTriggered) return;

        currentHealth -= damagePerSecond * Time.deltaTime;
        currentHealth  = Mathf.Max(0f, currentHealth);

        Debug.Log($"<color=orange>[Crystal] Hit! HP={currentHealth:F1}/{maxHealth}</color>");

        UpdateHPBar();
        FlashCrystal();

        if (currentHealth <= 0f)
        {
            winTriggered = true;
            StartCoroutine(WinSequence());
        }
    }

    public bool IsDead() => winTriggered;

    public void SetCrystalBeaconActive(bool active)
    {
        beaconActive = active;
        if (beaconRing  != null) beaconRing.gameObject.SetActive(active);
        if (beaconLight != null) beaconLight.enabled = active;
    }

    // ── Collider fit ─────────────────────────────────────────────
    private void FitBoxCollider()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) return;

        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            box.center = mf.sharedMesh.bounds.center;
            box.size   = mf.sharedMesh.bounds.size;
            Debug.Log($"<color=orange>[Crystal] BoxCollider fitted: center={box.center}, size={box.size}</color>");
        }
    }

    // ── Win Sequence ─────────────────────────────────────────────
    private IEnumerator WinSequence()
    {
        Debug.Log("<color=cyan>[Crystal] WinSequence started!</color>");
        SetCrystalBeaconActive(false);
        if (barRoot != null) barRoot.gameObject.SetActive(false);

        // Crystal energy explosion
        SpawnCrystalBurst();

        // Disable crystal mesh so it "vanishes"
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = false;

        yield return new WaitForSeconds(pauseBeforeFade);

        // Build and show win screen
        EnsureWinCanvas();

        if (winStingerSound != null)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.spatialBlend = 0f;
            src.PlayOneShot(winStingerSound, 1f);
        }

        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            if (winGroup != null)
                winGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }
        if (winGroup != null) winGroup.alpha = 1f;

        // Hold, then fade out and teleport to CP6
        yield return new WaitForSeconds(5f);

        elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            if (winGroup != null)
                winGroup.alpha = Mathf.Clamp01(1f - elapsed / fadeInDuration);
            yield return null;
        }
        if (winCanvas != null) winCanvas.gameObject.SetActive(false);

        TeleportToLastCheckpoint("CP6");
    }

    private void TeleportToLastCheckpoint(string checkpointName)
    {
        GameObject cpGO = GameObject.Find(checkpointName);
        if (cpGO == null)
        {
            Debug.LogWarning($"[Crystal] Checkpoint '{checkpointName}' not found in scene!");
            return;
        }

        // Use RoverCheckpoint.RespawnPosition if available, otherwise fallback
        RoverCheckpoint rcp = cpGO.GetComponent<RoverCheckpoint>();
        Vector3 respawnPos  = rcp != null
            ? rcp.RespawnPosition
            : cpGO.transform.position + Vector3.up * 1.5f;

        // Teleport XR Origin (the VR rig)
        var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin != null)
        {
            xrOrigin.transform.position = respawnPos;
            Debug.Log($"<color=cyan>[Crystal] Player teleported to {checkpointName} at {respawnPos}</color>");
        }

        // Register the checkpoint with PlayerHealth so next death also respawns here
        var health = FindFirstObjectByType<PlayerHealth>();
        if (health != null)
            health.SetCheckpoint(respawnPos);
    }

    // ── Crystal Hit Flash ────────────────────────────────────────
    private Renderer[] crystalRenderers;
    private Color[]    crystalOrigColors;
    private float      flashTimer;
    private const float FlashDuration = 0.08f;

    private void FlashCrystal()
    {
        if (crystalRenderers == null)
        {
            crystalRenderers  = GetComponentsInChildren<Renderer>();
            crystalOrigColors = new Color[crystalRenderers.Length];
            for (int i = 0; i < crystalRenderers.Length; i++)
            {
                // Read _BaseColor first (URP), fall back to .color
                if (crystalRenderers[i].material.HasProperty("_BaseColor"))
                    crystalOrigColors[i] = crystalRenderers[i].material.GetColor("_BaseColor");
                else
                    crystalOrigColors[i] = crystalRenderers[i].material.color;
            }
        }

        Color flashCol = new Color(1f, 0.9f, 0.5f, 1f); // hot white-gold
        foreach (var r in crystalRenderers)
        {
            if (r == null) continue;
            r.material.color = flashCol;
            if (r.material.HasProperty("_BaseColor"))
                r.material.SetColor("_BaseColor", flashCol);
            if (r.material.HasProperty("_EmissionColor"))
            {
                r.material.EnableKeyword("_EMISSION");
                r.material.SetColor("_EmissionColor", flashCol * 2f);
            }
        }

        flashTimer = FlashDuration;
        StartCoroutine(RestoreAfterFlash());
    }

    private IEnumerator RestoreAfterFlash()
    {
        yield return new WaitForSeconds(FlashDuration);
        if (winTriggered) yield break;
        if (crystalRenderers == null) yield break;
        for (int i = 0; i < crystalRenderers.Length; i++)
        {
            if (crystalRenderers[i] == null) continue;
            crystalRenderers[i].material.color = crystalOrigColors[i];
            if (crystalRenderers[i].material.HasProperty("_BaseColor"))
                crystalRenderers[i].material.SetColor("_BaseColor", crystalOrigColors[i]);
            if (crystalRenderers[i].material.HasProperty("_EmissionColor"))
                crystalRenderers[i].material.SetColor("_EmissionColor", Color.black);
        }
    }

    // ── HP Bar ───────────────────────────────────────────────────
    private void BuildHPBar()
    {
        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");

        barRoot = new GameObject("CrystalHPBar").transform;
        barRoot.SetParent(transform);

        // Use renderer bounds to find the actual top of the crystal in world space
        float topWorldY = transform.position.y + 1f; // fallback if no renderers
        foreach (var r in GetComponentsInChildren<Renderer>())
            topWorldY = Mathf.Max(topWorldY, r.bounds.max.y);

        // Place bar 1.5 world units above the crystal's visible top
        float targetWorldY  = topWorldY + 1.5f;
        float worldScaleY   = Mathf.Max(transform.lossyScale.y, 0.01f);
        float localY        = (targetWorldY - transform.position.y) / worldScaleY;

        barRoot.localPosition = new Vector3(0f, localY, 0f);
        barRoot.localScale = new Vector3(
            1f / Mathf.Max(transform.lossyScale.x, 0.01f),
            1f / Mathf.Max(transform.lossyScale.y, 0.01f),
            1f / Mathf.Max(transform.lossyScale.z, 0.01f));

        // BG
        CreateBarQuad("BG", 2.2f, 0.2f, 0f, new Color(0.08f, 0.04f, 0.02f), unlit);

        // Fill
        var fillGO = CreateBarQuad("Fill", 2f, 0.16f, -0.01f, new Color(1f, 0.55f, 0.08f), unlit);
        barFill    = fillGO.transform;
        barFillMat = fillGO.GetComponent<Renderer>().material;
    }

    private GameObject CreateBarQuad(string label, float w, float h, float z, Color col, Shader unlit)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = label;
        go.transform.SetParent(barRoot, false);
        go.transform.localPosition = new Vector3(0f, 0f, z);
        go.transform.localScale    = new Vector3(w, h, 1f);
        Destroy(go.GetComponent<Collider>());
        var mat = new Material(unlit);
        mat.color = col;
        mat.SetColor("_BaseColor", col);
        go.GetComponent<Renderer>().material = mat;
        return go;
    }

    private void UpdateHPBar()
    {
        if (barFill == null) return;

        // Face camera
        if (Camera.main != null && barRoot != null)
        {
            Vector3 dir = barRoot.position - Camera.main.transform.position;
            if (dir.sqrMagnitude > 0.001f)
                barRoot.rotation = Quaternion.LookRotation(dir);
        }

        float pct = currentHealth / maxHealth;
        const float maxW = 2f;
        barFill.localScale    = new Vector3(maxW * pct, 0.16f, 1f);
        barFill.localPosition = new Vector3(-maxW * 0.5f + maxW * pct * 0.5f, 0f, -0.01f);

        Color c = pct > 0.5f
            ? Color.Lerp(Color.yellow, new Color(1f, 0.55f, 0.08f), (pct - 0.5f) * 2f)
            : Color.Lerp(Color.red,    Color.yellow, pct * 2f);
        barFillMat.color = c;
        if (barFillMat.HasProperty("_BaseColor")) barFillMat.SetColor("_BaseColor", c);
    }

    // ── Beacon Visuals ───────────────────────────────────────────
    private void BuildBeaconVisuals()
    {
        // Ring
        GameObject ringGO = new GameObject("CrystalBeaconRing");
        ringGO.transform.SetParent(transform, false);
        ringGO.transform.localPosition = Vector3.zero;

        beaconRing = ringGO.AddComponent<LineRenderer>();
        beaconRing.useWorldSpace   = false;
        beaconRing.loop            = true;
        int seg = 48;
        beaconRing.positionCount   = seg;
        beaconRing.widthMultiplier = 0.06f;
        beaconRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        beaconRing.receiveShadows  = false;
        beaconRing.alignment       = LineAlignment.View;
        Shader ringSh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        Material ringMat = new Material(ringSh);
        ringMat.color = beaconColor;
        if (ringMat.HasProperty("_BaseColor")) ringMat.SetColor("_BaseColor", beaconColor);
        beaconRing.material = ringMat;

        for (int i = 0; i < seg; i++)
        {
            float angle = i / (float)seg * Mathf.PI * 2f;
            beaconRing.SetPosition(i, new Vector3(
                Mathf.Cos(angle) * beaconRingRadius, 0f,
                Mathf.Sin(angle) * beaconRingRadius));
        }

        // Light
        GameObject lightGO = new GameObject("CrystalBeaconLight");
        lightGO.transform.SetParent(transform, false);
        lightGO.transform.localPosition = Vector3.up * 0.6f;

        beaconLight           = lightGO.AddComponent<Light>();
        beaconLight.type      = LightType.Point;
        beaconLight.color     = beaconColor;
        beaconLight.range     = beaconLightRange;
        beaconLight.intensity = 2f;
        beaconLight.shadows   = LightShadows.None;
    }

    // ── Crystal Burst ─────────────────────────────────────────────
    private void SpawnCrystalBurst()
    {
        SpawnBurst("CrystalEnergy", 90, new Color(0.95f, 0.9f, 1f),    new Color(0.6f, 0.4f, 1f),  12f, 1.3f, 1.6f,  0f);
        SpawnBurst("CrystalShards", 50, new Color(0.4f, 0.85f, 1f),    new Color(0.9f, 0.6f, 1f),   8f, 0.5f, 2.2f,  0.4f);
        SpawnBurst("CrystalSpark",  70, new Color(1f, 1f, 0.8f),        new Color(1f, 0.75f, 0.15f),16f, 0.25f, 1.2f,-0.15f);
        SpawnBurst("CrystalSmoke",  30, new Color(0.55f, 0.45f, 0.7f), new Color(0.2f, 0.15f, 0.3f), 2f, 2f,   2.5f, -0.1f);
    }

    private void SpawnBurst(string label, int count, Color c1, Color c2,
        float speed, float size, float life, float gravity)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");

        GameObject go = new GameObject(label);
        go.transform.position = transform.position;
        ParticleSystem ps     = go.AddComponent<ParticleSystem>();

        var m = ps.main;
        m.loop            = false;
        m.playOnAwake     = false;
        m.startLifetime   = life;
        m.startSpeed      = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
        m.startSize       = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
        m.maxParticles    = count;
        m.startColor      = new ParticleSystem.MinMaxGradient(c1, c2);
        m.gravityModifier = gravity;
        m.simulationSpace = ParticleSystemSimulationSpace.World;

        var e = ps.emission;
        e.rateOverTime = 0f;
        e.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });

        var sh2 = ps.shape;
        sh2.shapeType = ParticleSystemShapeType.Sphere;
        sh2.radius    = 0.6f;

        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial =
            new Material(sh) { color = c1 };

        ps.Play();
        Destroy(go, life + 1f);
    }

    // ── Win Screen ───────────────────────────────────────────────
    private void EnsureWinCanvas()
    {
        if (winCanvas != null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        // Head-locked canvas — same technique as PlayerHealth overlay
        GameObject cGO = new GameObject("WinCanvas");
        cGO.transform.SetParent(cam.transform);
        cGO.transform.localPosition = new Vector3(0f, 0f, 1.4f);
        cGO.transform.localRotation = Quaternion.identity;
        cGO.transform.localScale    = Vector3.one * 0.0005f;

        winCanvas             = cGO.AddComponent<Canvas>();
        winCanvas.renderMode  = RenderMode.WorldSpace;
        winCanvas.sortingOrder = 200;

        winGroup       = cGO.AddComponent<CanvasGroup>();
        winGroup.alpha = 0f;

        var rt       = cGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1920f, 1080f);

        // Black background
        GameObject bgGO = new GameObject("BG");
        bgGO.transform.SetParent(cGO.transform, false);
        var bgImg = bgGO.AddComponent<RawImage>();
        bgImg.color = Color.black;
        var bgRT   = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // Title — "MISSION COMPLETE"
        MakeLabel("Title", cGO.transform,
            new Vector2(0f, 120f), new Vector2(1600f, 160f),
            "MISSION COMPLETE", 110f, FontStyles.Bold,
            new Color(0.95f, 0.88f, 1f, 1f));

        // Subtitle
        MakeLabel("Subtitle", cGO.transform,
            new Vector2(0f, -60f), new Vector2(1200f, 80f),
            "The crystal has been destroyed.", 54f, FontStyles.Normal,
            new Color(0.7f, 0.6f, 0.9f, 1f));
    }

    private static void MakeLabel(string name, Transform parent,
        Vector2 anchoredPos, Vector2 size,
        string text, float fontSize, FontStyles style, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var label        = go.AddComponent<TextMeshProUGUI>();
        label.text       = text;
        label.fontSize   = fontSize;
        label.fontStyle  = style;
        label.alignment  = TextAlignmentOptions.Center;
        label.color      = color;

        var rt           = go.GetComponent<RectTransform>();
        rt.anchorMin     = new Vector2(0.5f, 0.5f);
        rt.anchorMax     = new Vector2(0.5f, 0.5f);
        rt.pivot         = new Vector2(0.5f, 0.5f);
        rt.sizeDelta     = size;
        rt.anchoredPosition = anchoredPos;
    }
}
