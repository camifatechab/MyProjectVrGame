using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Player health, damage vignette, death sequence, and respawn.
/// Attach to the XR Origin root.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Respawn")]
    public Transform respawnPoint;
    public float respawnDelay = 3f; // kept for API compatibility

    [Header("Kill Plane")]
    [Tooltip("If the player falls below this Y, they die and respawn.")]
    public float killPlaneY = -10f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip hitSound;
    public AudioClip deathSound;

    [Header("HUD")]
    public bool showHeadHud = false;
    public Vector3 hudLocalPosition = new Vector3(-0.24f, -0.03f, 1.02f);
    public Vector3 hudLocalScale    = new Vector3(0.00042f, 0.00042f, 0.00042f);
    public Vector2 hudCanvasSize    = new Vector2(102f, 18f);
    public Vector2 healthBarSize    = new Vector2(42f, 2f);

    [Header("Damage Vignette")]
    [Tooltip("Distance in front of the camera the full-screen vignette plane sits (meters).")]
    public float vignetteDistance = 0.3f;

    [Header("Death Sequence Audio")]
    [Tooltip("Looping heartbeat that plays below 35% HP. Gets louder and faster as HP drops.")]
    public AudioClip heartbeatSound;
    [Tooltip("Stinger that plays the moment the player dies.")]
    public AudioClip deathStingerSound;

    // ── HUD internals ───────────────────────────────────────────────────
    private Image            healthFillImage;
    private Image            damageFlashImage;
    private Canvas           hudCanvas;
    private CanvasGroup      hudCanvasGroup;
    private Transform        hudTransform;
    private RectTransform    statusRoot;
    private TextMeshProUGUI  healthLabel;
    private TextMeshProUGUI  lowHealthLabel;

    private readonly Color healthyColor  = new(0.78f, 0.81f, 0.85f, 1f);
    private readonly Color damagedColor  = new(1f,    0.76f, 0.29f, 1f);
    private readonly Color criticalColor = new(1f,    0.38f, 0.32f, 1f);

    // ── Vignette / death overlay ────────────────────────────────────────
    private Canvas    overlayCanvas;
    private RawImage  vignetteImage;   // radial gradient ring
    private RawImage  redFillImage;    // solid red — fills screen at death
    private RawImage  blackFadeImage;  // solid black — final fade
    private Texture2D vignetteTex;
    private float     currentVignetteAlpha;
    private float     pulsePhase;
    private bool      inDeathSequence;

    // ── Heartbeat audio ─────────────────────────────────────────────────
    private AudioSource heartbeatSource;

    // ── General state ───────────────────────────────────────────────────
    private bool             isDead;
    private bool             suppressHeadHud;
    private float            hudAttention;
    private float            lowHealthPulse;
    private RideableCreature cachedCreature;

    public float HealthNormalized => maxHealth <= 0.001f ? 0f : Mathf.Clamp01(currentHealth / maxHealth);

    // ═══════════════════════════════════════════════════════════════════
    // Lifecycle
    // ═══════════════════════════════════════════════════════════════════

    private void Start()
    {
        currentHealth = maxHealth;
        EnsureHUD();
        EnsureOverlay();
        EnsureHeartbeat();
        UpdateHealthBar();
    }

    private void Update()
    {
        if (!isDead && !IsPlayerMountedOnCreature() && transform.position.y < killPlaneY)
            Die();

        EnsureHUD();
        EnsureOverlay();
        UpdateHeadHudAlpha();
        UpdateVignette();
        UpdateHeartbeat();
    }

    // ═══════════════════════════════════════════════════════════════════
    // Public API
    // ═══════════════════════════════════════════════════════════════════

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
        UpdateHealthBar();

        if (hitSound != null && audioSource != null)
            audioSource.PlayOneShot(hitSound);

        if (damageFlashImage != null)
            StartCoroutine(FlashDamage());

        hudAttention   = 1f;
        lowHealthPulse = currentHealth <= maxHealth * 0.35f ? 1f : lowHealthPulse;

        if (currentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        UpdateHealthBar();
    }

    public void SetCheckpoint(Vector3 position)
    {
        if (respawnPoint != null)
        {
            respawnPoint.position = position;
            return;
        }

        GameObject cp = new GameObject("DynamicRespawnPoint");
        cp.transform.position = position;
        respawnPoint = cp.transform;
    }

    public void SetHeadHudSuppressed(bool suppressed)
    {
        suppressHeadHud = suppressed;
        if (hudTransform != null)
            hudTransform.gameObject.SetActive(true);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Death & Respawn
    // ═══════════════════════════════════════════════════════════════════

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (deathSound != null && audioSource != null)
            audioSource.PlayOneShot(deathSound);

        Debug.Log("<color=red>[PlayerHealth] Player died — starting death sequence.</color>");
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        inDeathSequence = true;

        // Stop heartbeat the moment death starts
        if (heartbeatSource != null) heartbeatSource.Stop();

        // Play death stinger
        if (deathStingerSound != null && audioSource != null)
            audioSource.PlayOneShot(deathStingerSound);

        // 1. Strobe phase — rapid vignette flicker while red fill builds (0.5s)
        float strobeElapsed = 0f;
        const float strobeDuration = 0.5f;
        while (strobeElapsed < strobeDuration)
        {
            strobeElapsed += Time.deltaTime;
            float t       = strobeElapsed / strobeDuration;
            float strobe  = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(strobeElapsed * Mathf.PI * 9f));
            float fillAlpha = Mathf.Lerp(0f, 0.6f, t);
            currentVignetteAlpha = strobe;
            SetVignetteAlpha(strobe);
            SetRedFillAlpha(fillAlpha);
            yield return null;
        }

        // 2. Full red lockout — vignette + solid fill both to 1 (0.35s)
        yield return StartCoroutine(AnimateRedFill(0.6f, 1f, 0.35f));
        SetVignetteAlpha(1f);
        currentVignetteAlpha = 1f;

        // 3. Hold on blood red
        yield return new WaitForSeconds(0.25f);

        // 4. Slam to black (fast — 0.35s)
        yield return AnimateBlack(0f, 1f, 0.35f);

        // 5. Teleport + restore while blacked out
        if (respawnPoint != null)
            transform.position = respawnPoint.position;

        currentHealth = maxHealth;
        UpdateHealthBar();

        currentVignetteAlpha = 0f;
        SetVignetteAlpha(0f);
        SetRedFillAlpha(0f);

        yield return new WaitForSeconds(0.1f);

        isDead          = false;
        inDeathSequence = false;

        // 6. Fade from black
        yield return AnimateBlack(1f, 0f, 0.9f);

        Debug.Log("<color=cyan>[PlayerHealth] Respawned.</color>");
    }

    // ═══════════════════════════════════════════════════════════════════
    // Vignette — continuous update
    // ═══════════════════════════════════════════════════════════════════

    private void UpdateVignette()
    {
        if (vignetteImage == null || inDeathSequence) return;

        float hp = HealthNormalized;

        // Aggressive curve: clearly visible at 75% HP, intense at 50%, severe at 25%
        // 100%→0 | 75%→0.15 | 50%→0.35 | 25%→0.58 | 10%→0.76 | 0%→0.85
        float target = hp >= 1f ? 0f : Mathf.Pow(1f - hp, 1.3f) * 0.85f;

        // Heartbeat pulse below 35% HP — rate increases as HP drops
        if (hp <= 0.35f && !isDead)
        {
            float pulseSpeed = Mathf.Lerp(5f, 2.5f, hp / 0.35f);
            pulsePhase += Time.deltaTime * pulseSpeed;
            target += Mathf.Abs(Mathf.Sin(pulsePhase * Mathf.PI)) * 0.22f;
        }
        else
        {
            pulsePhase = 0f;
        }

        target = Mathf.Clamp01(target);
        currentVignetteAlpha = Mathf.MoveTowards(currentVignetteAlpha, target, Time.deltaTime * 3f);
        SetVignetteAlpha(currentVignetteAlpha);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Heartbeat audio
    // ═══════════════════════════════════════════════════════════════════

    private void EnsureHeartbeat()
    {
        if (heartbeatSource != null || heartbeatSound == null) return;
        GameObject go = new GameObject("HeartbeatAudio");
        go.transform.SetParent(transform);
        heartbeatSource           = go.AddComponent<AudioSource>();
        heartbeatSource.clip      = heartbeatSound;
        heartbeatSource.loop      = true;
        heartbeatSource.spatialBlend = 0f;
        heartbeatSource.playOnAwake  = false;
        heartbeatSource.volume    = 0f;
    }

    private void UpdateHeartbeat()
    {
        if (heartbeatSource == null || inDeathSequence) return;

        float hp = HealthNormalized;
        if (hp <= 0.35f && !isDead)
        {
            if (!heartbeatSource.isPlaying) heartbeatSource.Play();
            // Volume and pitch rise as HP drops
            heartbeatSource.volume = Mathf.Lerp(0f, 0.9f, 1f - (hp / 0.35f));
            heartbeatSource.pitch  = Mathf.Lerp(0.85f, 1.35f, 1f - (hp / 0.35f));
        }
        else
        {
            if (heartbeatSource.isPlaying) heartbeatSource.Stop();
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // Overlay setup
    // ═══════════════════════════════════════════════════════════════════

    private void EnsureOverlay()
    {
        if (overlayCanvas != null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        GameObject root = new GameObject("HealthOverlayCanvas");
        root.transform.SetParent(cam.transform, false);
        root.transform.localPosition = new Vector3(0f, 0f, vignetteDistance);
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale    = Vector3.one;

        overlayCanvas              = root.AddComponent<Canvas>();
        overlayCanvas.renderMode   = RenderMode.WorldSpace;
        overlayCanvas.sortingOrder = 100;

        // 2×2 m plane at vignetteDistance covers well beyond any headset's FOV
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(2f, 2f);

        // Layer 1: red radial vignette (ring grows from edges)
        vignetteTex   = CreateVignetteTexture(128);
        vignetteImage = CreateOverlayImage(root.transform, "Vignette", new Color(1f, 0f, 0f, 0f));
        vignetteImage.texture = vignetteTex;

        // Layer 2: solid red fill (for death — covers full screen)
        redFillImage = CreateOverlayImage(root.transform, "RedFill", new Color(0.8f, 0f, 0f, 0f));

        // Layer 3: solid black for final death fade (on top of everything)
        blackFadeImage = CreateOverlayImage(root.transform, "BlackFade", new Color(0f, 0f, 0f, 0f));
    }

    private static RawImage CreateOverlayImage(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RawImage img     = go.AddComponent<RawImage>();
        img.color        = color;
        img.raycastTarget = false;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.sizeDelta  = Vector2.zero;
        rt.localScale = Vector3.one;
        return img;
    }

    // Radial gradient: transparent at center, opaque red at edges
    private static Texture2D CreateVignetteTexture(int res)
    {
        Texture2D tex  = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.wrapMode   = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2(0.5f, 0.5f);
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float u    = (float)x / (res - 1);
                float v    = (float)y / (res - 1);
                float dist = Vector2.Distance(new Vector2(u, v), center) * 2f;
                float a    = Mathf.Clamp01(Mathf.Pow(dist, 2.2f));
                tex.SetPixel(x, y, new Color(1f, 0f, 0f, a));
            }
        }
        tex.Apply();
        return tex;
    }

    // ═══════════════════════════════════════════════════════════════════
    // Animation helpers
    // ═══════════════════════════════════════════════════════════════════

    private IEnumerator AnimateVignette(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(from, to, elapsed / duration);
            currentVignetteAlpha = a;
            SetVignetteAlpha(a);
            yield return null;
        }
        currentVignetteAlpha = to;
        SetVignetteAlpha(to);
    }

    private IEnumerator AnimateRedFill(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetRedFillAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }
        SetRedFillAlpha(to);
    }

    private IEnumerator AnimateBlack(float from, float to, float duration)
    {
        if (blackFadeImage == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            blackFadeImage.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }
        blackFadeImage.color = new Color(0f, 0f, 0f, to);
    }

    private void SetVignetteAlpha(float alpha)
    {
        if (vignetteImage != null)
            vignetteImage.color = new Color(1f, 0f, 0f, alpha);
    }

    private void SetRedFillAlpha(float alpha)
    {
        if (redFillImage != null)
            redFillImage.color = new Color(0.8f, 0f, 0f, alpha);
    }

    // ═══════════════════════════════════════════════════════════════════
    // HUD (unchanged)
    // ═══════════════════════════════════════════════════════════════════

    private void UpdateHealthBar()
    {
        if (healthFillImage == null) return;

        float ratio = Mathf.Clamp01(currentHealth / maxHealth);
        healthFillImage.fillAmount = ratio;

        if (ratio > 0.5f)
            healthFillImage.color = Color.Lerp(damagedColor, healthyColor, (ratio - 0.5f) * 2f);
        else
            healthFillImage.color = Color.Lerp(criticalColor, damagedColor, ratio * 2f);
    }

    private IEnumerator FlashDamage()
    {
        if (damageFlashImage == null) yield break;

        damageFlashImage.gameObject.SetActive(true);
        damageFlashImage.color = new Color(1f, 0f, 0f, 0.5f);

        float elapsed = 0f;
        const float flashDuration = 0.3f;

        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            damageFlashImage.color = new Color(1f, 0f, 0f, Mathf.Lerp(0.5f, 0f, elapsed / flashDuration));
            yield return null;
        }

        damageFlashImage.gameObject.SetActive(false);
    }

    private bool IsPlayerMountedOnCreature()
    {
        if (cachedCreature == null)
            cachedCreature = FindFirstObjectByType<RideableCreature>();
        return cachedCreature != null && cachedCreature.IsPlayerMounted;
    }

    private void EnsureHUD()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        if (hudTransform != null &&
            hudTransform.parent == cam.transform &&
            healthFillImage   != null &&
            damageFlashImage  != null &&
            healthLabel       != null &&
            lowHealthLabel    != null &&
            statusRoot        != null)
        {
            ConfigureHUDTransform(hudTransform);
            return;
        }

        hudTransform = cam.transform.Find("PlayerHUDCanvas");
        if (hudTransform == null)
            hudTransform = CreateHUDRoot(cam.transform);

        ConfigureHUDCanvas(hudTransform);
        EnsureHUDStructure(hudTransform);
        hudTransform.gameObject.SetActive(true);
        UpdateHealthBar();
    }

    private Transform CreateHUDRoot(Transform cameraTransform)
    {
        GameObject root = new GameObject("PlayerHUDCanvas", typeof(RectTransform), typeof(Canvas));
        root.transform.SetParent(cameraTransform, false);
        return root.transform;
    }

    private void ConfigureHUDCanvas(Transform root)
    {
        hudCanvas = root.GetComponent<Canvas>();
        if (hudCanvas == null)
            hudCanvas = root.gameObject.AddComponent<Canvas>();

        hudCanvas.renderMode    = RenderMode.WorldSpace;
        hudCanvas.overrideSorting = true;
        hudCanvas.sortingOrder  = 60;
        hudCanvas.pixelPerfect  = false;

        hudCanvasGroup = root.GetComponent<CanvasGroup>();
        if (hudCanvasGroup == null)
            hudCanvasGroup = root.gameObject.AddComponent<CanvasGroup>();

        if (root is RectTransform rect)
            rect.sizeDelta = hudCanvasSize;

        ConfigureHUDTransform(root);
    }

    private void ConfigureHUDTransform(Transform root)
    {
        root.localPosition = hudLocalPosition;
        root.localRotation = Quaternion.identity;
        root.localScale    = hudLocalScale;
    }

    private void EnsureHUDStructure(Transform root)
    {
        if (root is not RectTransform rootRect) return;

        statusRoot = GetOrCreateRect(rootRect, "StatusRoot", new Vector2(70f, 10f), Vector2.zero);

        Image plate = GetOrCreateImage(statusRoot, "VitalsPlate", new Vector2(70f, 10f), Vector2.zero, new Color(0.03f, 0.06f, 0.08f, 0.2f));
        plate.raycastTarget = false;

        TextMeshProUGUI label = GetOrCreateText(statusRoot, "HealthLabel", new Vector2(-22f, 0f), new Vector2(14f, 7f), 4.1f);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.text = "HP";
        healthLabel = label;

        Image background = GetOrCreateImage(statusRoot, "HPBarBackground", healthBarSize + new Vector2(2f, 2f), new Vector2(8f, 0f), new Color(0.05f, 0.09f, 0.12f, 0.48f));
        background.raycastTarget = false;

        Image fill = GetOrCreateImage(background.rectTransform, "HPFill", healthBarSize, Vector2.zero, healthyColor);
        fill.type        = Image.Type.Filled;
        fill.fillMethod  = Image.FillMethod.Horizontal;
        fill.fillOrigin  = (int)Image.OriginHorizontal.Left;
        fill.fillAmount  = 1f;
        fill.raycastTarget = false;
        healthFillImage  = fill;

        Image flash = GetOrCreateImage(rootRect, "DamageFlash", hudCanvasSize, Vector2.zero, new Color(1f, 0f, 0f, 0f));
        flash.raycastTarget = false;
        flash.gameObject.SetActive(false);
        damageFlashImage = flash;

        TextMeshProUGUI lowHealth = GetOrCreateText(rootRect, "LowHealthLabel", new Vector2(0f, 22f), new Vector2(84f, 10f), 4.5f);
        lowHealth.alignment = TextAlignmentOptions.Center;
        lowHealth.text  = "LOW HEALTH";
        lowHealth.color = new Color(1f, 0.55f, 0.38f, 0f);
        lowHealthLabel  = lowHealth;
    }

    private void UpdateHeadHudAlpha()
    {
        if (hudCanvasGroup == null) return;

        hudAttention   = Mathf.MoveTowards(hudAttention,   0f, Time.deltaTime * 1.35f);
        lowHealthPulse = Mathf.MoveTowards(lowHealthPulse, 0f, Time.deltaTime * 0.9f);

        float health = HealthNormalized;
        float targetAlpha = !showHeadHud || suppressHeadHud
            ? 0f
            : health < 0.3f
                ? 0.72f
                : health < 0.65f
                    ? 0.42f
                    : 0.14f;

        targetAlpha = Mathf.Max(targetAlpha, hudAttention * 0.75f);
        hudCanvasGroup.alpha = Mathf.MoveTowards(hudCanvasGroup.alpha, targetAlpha, Time.deltaTime * 3.5f);

        if (statusRoot != null)
            statusRoot.gameObject.SetActive(showHeadHud && !suppressHeadHud);

        if (healthLabel != null)
            healthLabel.color = new Color(0.7f, 0.74f, 0.79f, hudCanvasGroup.alpha);

        if (lowHealthLabel != null)
        {
            bool showLowHealth = health <= 0.32f && !isDead;
            float pulse      = 0.45f + Mathf.PingPong(Time.time * 1.8f, 0.35f);
            float alertAlpha = showLowHealth ? Mathf.Max(lowHealthPulse, pulse) : 0f;
            lowHealthLabel.color = new Color(1f, 0.55f, 0.38f, alertAlpha);
            lowHealthLabel.gameObject.SetActive(showLowHealth || alertAlpha > 0.01f);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // UI helpers
    // ═══════════════════════════════════════════════════════════════════

    private static RectTransform GetOrCreateRect(Transform parent, string name, Vector2 size, Vector2 anchoredPosition)
    {
        Transform existing = parent.Find(name);
        RectTransform rect = existing != null && existing is RectTransform er
            ? er
            : new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();

        rect.transform.SetParent(parent, false);
        rect.anchorMin        = new Vector2(0.5f, 0.5f);
        rect.anchorMax        = new Vector2(0.5f, 0.5f);
        rect.pivot            = new Vector2(0.5f, 0.5f);
        rect.sizeDelta        = size;
        rect.anchoredPosition = anchoredPosition;
        rect.localScale       = Vector3.one;
        return rect;
    }

    private static TextMeshProUGUI GetOrCreateText(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, float fontSize)
    {
        Transform existing = parent.Find(name);
        RectTransform rect;
        TextMeshProUGUI label;

        if (existing != null)
        {
            rect  = existing as RectTransform;
            label = existing.GetComponent<TextMeshProUGUI>() ?? existing.gameObject.AddComponent<TextMeshProUGUI>();
        }
        else
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            rect  = go.GetComponent<RectTransform>();
            label = go.GetComponent<TextMeshProUGUI>();
        }

        rect.anchorMin        = new Vector2(0.5f, 0.5f);
        rect.anchorMax        = new Vector2(0.5f, 0.5f);
        rect.pivot            = new Vector2(0.5f, 0.5f);
        rect.sizeDelta        = size;
        rect.anchoredPosition = anchoredPosition;
        rect.localScale       = Vector3.one;

        label.font            = TMP_Settings.defaultFontAsset;
        label.fontSize        = fontSize;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode    = TextOverflowModes.Ellipsis;
        label.richText        = false;
        return label;
    }

    private static Image GetOrCreateImage(Transform parent, string name, Vector2 size, Vector2 anchoredPosition, Color color)
    {
        Transform existing = parent.Find(name);
        RectTransform rect;
        Image image;

        if (existing != null)
        {
            rect  = existing as RectTransform;
            image = existing.GetComponent<Image>() ?? existing.gameObject.AddComponent<Image>();
        }
        else
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            rect  = go.GetComponent<RectTransform>();
            image = go.GetComponent<Image>();
        }

        rect.anchorMin        = new Vector2(0.5f, 0.5f);
        rect.anchorMax        = new Vector2(0.5f, 0.5f);
        rect.pivot            = new Vector2(0.5f, 0.5f);
        rect.sizeDelta        = size;
        rect.anchoredPosition = anchoredPosition;
        rect.localScale       = Vector3.one;
        image.color           = color;
        return image;
    }
}
