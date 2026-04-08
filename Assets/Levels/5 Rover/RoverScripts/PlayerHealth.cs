using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Player health, damage flash, respawn, and a VR-safe head-locked HUD.
/// Attach to the XR Origin root.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Respawn")]
    public Transform respawnPoint;
    public float respawnDelay = 3f;

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
    public Vector3 hudLocalScale = new Vector3(0.00042f, 0.00042f, 0.00042f);
    public Vector2 hudCanvasSize = new Vector2(102f, 18f);
    public Vector2 healthBarSize = new Vector2(42f, 2f);

    private Image healthFillImage;
    private Image damageFlashImage;
    private Canvas hudCanvas;
    private CanvasGroup hudCanvasGroup;
    private Transform hudTransform;
    private RectTransform statusRoot;
    private TextMeshProUGUI healthLabel;
    private TextMeshProUGUI lowHealthLabel;

    private readonly Color healthyColor = new(0.78f, 0.81f, 0.85f, 1f);
    private readonly Color damagedColor = new(1f, 0.76f, 0.29f, 1f);
    private readonly Color criticalColor = new(1f, 0.38f, 0.32f, 1f);

    private bool isDead;
    private bool suppressHeadHud;
    private float hudAttention;
    private float lowHealthPulse;

    public float HealthNormalized => maxHealth <= 0.001f ? 0f : Mathf.Clamp01(currentHealth / maxHealth);

    private void Start()
    {
        currentHealth = maxHealth;
        EnsureHUD();
        UpdateHealthBar();
    }

    private void Update()
    {
        if (!isDead && transform.position.y < killPlaneY)
            Die();

        EnsureHUD();
        UpdateHeadHudAlpha();
    }

    public void TakeDamage(float amount)
    {
        if (isDead)
            return;

        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
        UpdateHealthBar();

        if (hitSound != null && audioSource != null)
            audioSource.PlayOneShot(hitSound);

        if (damageFlashImage != null)
            StartCoroutine(FlashDamage());

        hudAttention = 1f;
        lowHealthPulse = currentHealth <= maxHealth * 0.35f ? 1f : lowHealthPulse;

        if (currentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (isDead)
            return;

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

        GameObject checkpoint = new GameObject("DynamicRespawnPoint");
        checkpoint.transform.position = position;
        respawnPoint = checkpoint.transform;
    }

    public void SetHeadHudSuppressed(bool suppressed)
    {
        suppressHeadHud = suppressed;

        if (hudTransform != null)
            hudTransform.gameObject.SetActive(true);
    }

    private void Die()
    {
        isDead = true;

        if (deathSound != null && audioSource != null)
            audioSource.PlayOneShot(deathSound);

        Debug.Log($"<color=red>[PlayerHealth] Player died. Respawning in {respawnDelay}s</color>");
        StartCoroutine(Respawn());
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnDelay);

        if (respawnPoint != null)
            transform.position = respawnPoint.position;

        currentHealth = maxHealth;
        isDead = false;
        UpdateHealthBar();

        Debug.Log("<color=cyan>[PlayerHealth] Respawned.</color>");
    }

    private void UpdateHealthBar()
    {
        if (healthFillImage == null)
            return;

        float ratio = Mathf.Clamp01(currentHealth / maxHealth);
        healthFillImage.fillAmount = ratio;

        if (ratio > 0.5f)
            healthFillImage.color = Color.Lerp(damagedColor, healthyColor, (ratio - 0.5f) * 2f);
        else
            healthFillImage.color = Color.Lerp(criticalColor, damagedColor, ratio * 2f);
    }

    private IEnumerator FlashDamage()
    {
        if (damageFlashImage == null)
            yield break;

        damageFlashImage.gameObject.SetActive(true);
        damageFlashImage.color = new Color(1f, 0f, 0f, 0.22f);

        float elapsed = 0f;
        const float flashDuration = 0.25f;

        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0.22f, 0f, elapsed / flashDuration);
            damageFlashImage.color = new Color(1f, 0f, 0f, alpha);
            yield return null;
        }

        damageFlashImage.gameObject.SetActive(false);
    }

    private void EnsureHUD()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        if (hudTransform != null &&
            hudTransform.parent == cam.transform &&
            healthFillImage != null &&
            damageFlashImage != null &&
            healthLabel != null &&
            lowHealthLabel != null &&
            statusRoot != null)
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

        hudCanvas.renderMode = RenderMode.WorldSpace;
        hudCanvas.overrideSorting = true;
        hudCanvas.sortingOrder = 60;
        hudCanvas.pixelPerfect = false;

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
        root.localScale = hudLocalScale;
    }

    private void EnsureHUDStructure(Transform root)
    {
        if (root is not RectTransform rootRect)
            return;

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
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;
        fill.raycastTarget = false;
        healthFillImage = fill;

        Image flash = GetOrCreateImage(rootRect, "DamageFlash", hudCanvasSize, Vector2.zero, new Color(1f, 0f, 0f, 0f));
        flash.raycastTarget = false;
        flash.gameObject.SetActive(false);
        damageFlashImage = flash;

        TextMeshProUGUI lowHealth = GetOrCreateText(rootRect, "LowHealthLabel", new Vector2(0f, 22f), new Vector2(84f, 10f), 4.5f);
        lowHealth.alignment = TextAlignmentOptions.Center;
        lowHealth.text = "LOW HEALTH";
        lowHealth.color = new Color(1f, 0.55f, 0.38f, 0f);
        lowHealthLabel = lowHealth;
    }

    private void UpdateHeadHudAlpha()
    {
        if (hudCanvasGroup == null)
            return;

        hudAttention = Mathf.MoveTowards(hudAttention, 0f, Time.deltaTime * 1.35f);
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
        {
            Color labelColor = new Color(0.7f, 0.74f, 0.79f, hudCanvasGroup.alpha);
            healthLabel.color = labelColor;
        }

        if (lowHealthLabel != null)
        {
            bool showLowHealth = health <= 0.32f && !isDead;
            float pulse = 0.45f + Mathf.PingPong(Time.time * 1.8f, 0.35f);
            float alertAlpha = showLowHealth ? Mathf.Max(lowHealthPulse, pulse) : 0f;
            lowHealthLabel.color = new Color(1f, 0.55f, 0.38f, alertAlpha);
            lowHealthLabel.gameObject.SetActive(showLowHealth || alertAlpha > 0.01f);
        }
    }

    private static RectTransform GetOrCreateRect(Transform parent, string name, Vector2 size, Vector2 anchoredPosition)
    {
        Transform existing = parent.Find(name);
        RectTransform rect;

        if (existing != null && existing is RectTransform existingRect)
        {
            rect = existingRect;
        }
        else
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            rect = go.GetComponent<RectTransform>();
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        rect.localScale = Vector3.one;
        return rect;
    }

    private static TextMeshProUGUI GetOrCreateText(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, float fontSize)
    {
        Transform existing = parent.Find(name);
        RectTransform rect;
        TextMeshProUGUI label;

        if (existing != null)
        {
            rect = existing as RectTransform;
            label = existing.GetComponent<TextMeshProUGUI>();
            if (label == null)
                label = existing.gameObject.AddComponent<TextMeshProUGUI>();
        }
        else
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            rect = go.GetComponent<RectTransform>();
            label = go.GetComponent<TextMeshProUGUI>();
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        rect.localScale = Vector3.one;

        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = fontSize;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.richText = false;
        return label;
    }

    private static Image GetOrCreateImage(Transform parent, string name, Vector2 size, Vector2 anchoredPosition, Color color)
    {
        Transform existing = parent.Find(name);
        RectTransform rect;
        Image image;

        if (existing != null)
        {
            rect = existing as RectTransform;
            image = existing.GetComponent<Image>();
            if (image == null)
                image = existing.gameObject.AddComponent<Image>();
        }
        else
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            rect = go.GetComponent<RectTransform>();
            image = go.GetComponent<Image>();
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        rect.localScale = Vector3.one;
        image.color = color;
        return image;
    }
}
