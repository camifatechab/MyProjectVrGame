using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Player health, damage flash, respawn.
/// Attach to XR Origin (XR Rig).
/// Auto-finds UI references from PlayerHUDCanvas on Start.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth    = 100f;
    public float currentHealth;

    [Header("Respawn")]
    public Transform respawnPoint;
    public float     respawnDelay = 3f;

    [Header("Kill Plane")]
    [Tooltip("If the player falls below this Y, they die and respawn.")]
    public float killPlaneY = -10f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   hitSound;
    public AudioClip   deathSound;

    // --- UI (auto-found) ---
    private Image  healthFillImage;
    private Image  damageFlashImage;
    private Canvas hudCanvas;

    // --- Colors ---
    private readonly Color healthyColor  = Color.green;
    private readonly Color damagedColor  = Color.yellow;
    private readonly Color criticalColor = Color.red;

    private bool isDead = false;

    // ─────────────────────────────────────────────
    // LIFECYCLE
    // ─────────────────────────────────────────────

    void Start()
    {
        currentHealth = maxHealth;
        AutoFindUI();
        UpdateHealthBar();
    }

    void Update()
    {
        // Kill plane — handles falling off the arena or out-of-fuel fall into void
        if (!isDead && transform.position.y < killPlaneY)
            Die();
    }

    // ─────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
        UpdateHealthBar();

        if (hitSound && audioSource)
            audioSource.PlayOneShot(hitSound);

        if (damageFlashImage != null)
            StartCoroutine(FlashDamage());

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
        }
        else
        {
            var go = new GameObject("DynamicRespawnPoint");
            go.transform.position = position;
            respawnPoint = go.transform;
        }
    }

    // ─────────────────────────────────────────────
    // INTERNAL
    // ─────────────────────────────────────────────

    void Die()
    {
        isDead = true;

        if (deathSound && audioSource)
            audioSource.PlayOneShot(deathSound);

        Debug.Log("<color=red>[PlayerHealth] Player died — respawning in " + respawnDelay + "s</color>");
        StartCoroutine(Respawn());
    }

    IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnDelay);

        if (respawnPoint != null)
            transform.position = respawnPoint.position;

        currentHealth = maxHealth;
        isDead        = false;
        UpdateHealthBar();

        Debug.Log("<color=cyan>[PlayerHealth] Respawned.</color>");
    }

    void UpdateHealthBar()
    {
        if (healthFillImage == null) return;

        float ratio = currentHealth / maxHealth;
        healthFillImage.fillAmount = ratio;

        if (ratio > 0.5f)
            healthFillImage.color = Color.Lerp(damagedColor, healthyColor, (ratio - 0.5f) * 2f);
        else
            healthFillImage.color = Color.Lerp(criticalColor, damagedColor, ratio * 2f);
    }

    IEnumerator FlashDamage()
    {
        damageFlashImage.gameObject.SetActive(true);
        damageFlashImage.color = new Color(1f, 0f, 0f, 0.45f);

        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(0.45f, 0f, t / 0.25f);
            damageFlashImage.color = new Color(1f, 0f, 0f, alpha);
            yield return null;
        }

        damageFlashImage.gameObject.SetActive(false);
    }

    void AutoFindUI()
    {
        // Find PlayerHUDCanvas under Main Camera
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[PlayerHealth] Main Camera not found.");
            return;
        }

        Transform hudTransform = cam.transform.Find("PlayerHUDCanvas");
        if (hudTransform == null)
        {
            Debug.LogWarning("[PlayerHealth] PlayerHUDCanvas not found under Main Camera.");
            return;
        }

        // Activate the HUD canvas
        hudCanvas = hudTransform.GetComponent<Canvas>();
        hudTransform.gameObject.SetActive(true);

        // Find HPFill Image
        Transform hpFill = hudTransform.Find("HPBarBackground/HPFill");
        if (hpFill != null)
        {
            healthFillImage = hpFill.GetComponent<Image>();
            Debug.Log("<color=cyan>[PlayerHealth] ✓ HPFill found</color>");
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] HPFill not found under HPBarBackground.");
        }

        // Find DamageFlash Image
        Transform flash = hudTransform.Find("DamageFlash");
        if (flash != null)
        {
            damageFlashImage = flash.GetComponent<Image>();
            flash.gameObject.SetActive(false); // starts hidden, only shown on hit
            Debug.Log("<color=cyan>[PlayerHealth] ✓ DamageFlash found</color>");
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] DamageFlash not found under PlayerHUDCanvas.");
        }

        Debug.Log("<color=cyan>[PlayerHealth] ✓ UI initialized</color>");
    }
}
