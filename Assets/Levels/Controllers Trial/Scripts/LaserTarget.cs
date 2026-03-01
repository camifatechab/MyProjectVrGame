using UnityEngine;

/// <summary>
/// Dragon placeholder target. World-space HP bar built from quads (no Canvas/UI).
/// Fully URP-compatible. Bar shifts green->yellow->red. Body flashes white on hit.
/// </summary>
public class LaserTarget : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private float damagePerSecond = 20f;

    [Header("Health Bar Size")]
    [SerializeField] private float barWidth = 0.8f;
    [SerializeField] private float barHeight = 0.08f;
    [SerializeField] private float barOffsetY = 0.5f; // above top of object

    [Header("Hit Flash")]
    [SerializeField] private float flashDuration = 0.1f;

    // State
    private float currentHP;
    private bool isDead = false;
    private float flashTimer = 0f;

    // Body
    private Material bodyMat;
    private Color defaultColor = new Color(0.8f, 0.8f, 0.8f);

    // Bar quads
    private GameObject barRoot;
    private Transform fillTransform;
    private Material fillMat;
    private Material bgMat;

    private Transform cameraTransform;
    private Shader urpUnlit;

void Start()
    {
        currentHP = maxHP;

        urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (urpUnlit == null) urpUnlit = Shader.Find("Unlit/Color");

        // Try own renderer first, then children (for prefabs with child meshes)
        Renderer rend = GetComponent<Renderer>();
        if (rend == null) rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            bodyMat = rend.material;
            defaultColor = bodyMat.color != Color.clear ? bodyMat.color : new Color(0.8f, 0.8f, 0.8f);
        }

        cameraTransform = Camera.main != null ? Camera.main.transform : null;
        BuildBar();
    }

    // ─────────────────────────────────────────────
    // BUILD QUAD-BASED BAR
    // ─────────────────────────────────────────────

void BuildBar()
    {
        float topOffset = transform.localScale.y * 0.5f + barOffsetY;

        barRoot = new GameObject("HPBar_Root");
        barRoot.transform.SetParent(transform);
        barRoot.transform.localPosition = new Vector3(0f, topOffset / transform.localScale.y, 0f);
        barRoot.transform.localScale = new Vector3(
            1f / transform.localScale.x,
            1f / transform.localScale.y,
            1f / transform.localScale.z);

        // Background at z=0, fill at z=-0.01 (closer to camera after LookRotation)
        GameObject bg = CreateQuad("BG", barWidth + 0.02f, barHeight + 0.02f, 0f, new Color(0.15f, 0.02f, 0.02f));
        bg.transform.SetParent(barRoot.transform, false);
        bgMat = bg.GetComponent<Renderer>().material;

        GameObject fill = CreateQuad("Fill", barWidth, barHeight, -0.01f, Color.green);
        fill.transform.SetParent(barRoot.transform, false);
        fillTransform = fill.transform;
        fillMat = fill.GetComponent<Renderer>().material;
    }

GameObject CreateQuad(string name, float width, float height, float zOffset, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        go.transform.localPosition = new Vector3(0f, 0f, zOffset);
        go.transform.localScale = new Vector3(width, height, 1f);
        Destroy(go.GetComponent<Collider>());

        Renderer rend = go.GetComponent<Renderer>();
        Material mat = new Material(urpUnlit);
        mat.color = color; // use .color — maps to _BaseColor in URP automatically
        rend.material = mat;
        return go;
    }

    // ─────────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────────

    void Update()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        // Billboard
        if (barRoot != null && cameraTransform != null)
        {
            Vector3 dir = barRoot.transform.position - cameraTransform.position;
            if (dir.sqrMagnitude > 0.001f)
                barRoot.transform.rotation = Quaternion.LookRotation(dir);
        }

        // Flash timeout
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f)
                SetBodyColor(defaultColor);
        }
    }

    // ─────────────────────────────────────────────
    // HIT
    // ─────────────────────────────────────────────

    public void OnLaserHit()
    {
        if (isDead) return;

        // Flash white
        SetBodyColor(Color.white);
        flashTimer = flashDuration;

        // Damage
        currentHP -= damagePerSecond * Time.deltaTime;
        currentHP = Mathf.Max(currentHP, 0f);
        UpdateBar();

        if (currentHP <= 0f) Die();
    }

    // ─────────────────────────────────────────────
    // BAR UPDATE
    // ─────────────────────────────────────────────

void UpdateBar()
    {
        if (fillTransform == null || fillMat == null) return;

        float pct = currentHP / maxHP;

        float scaledWidth = barWidth * pct;
        fillTransform.localScale = new Vector3(scaledWidth, barHeight, 1f);
        fillTransform.localPosition = new Vector3(-barWidth * 0.5f + scaledWidth * 0.5f, 0f, -0.01f);

        Color barColor;
        if (pct > 0.5f)
            barColor = Color.Lerp(Color.yellow, Color.green, (pct - 0.5f) * 2f);
        else
            barColor = Color.Lerp(Color.red, Color.yellow, pct * 2f);

        fillMat.color = barColor;
    }

    // ─────────────────────────────────────────────
    // BODY COLOR
    // ─────────────────────────────────────────────

void SetBodyColor(Color c)
    {
        if (bodyMat == null) return;
        bodyMat.color = c;
    }

    // ─────────────────────────────────────────────
    // DEATH
    // ─────────────────────────────────────────────

void Die()
    {
        isDead = true;
        if (barRoot != null) barRoot.SetActive(false);
        SetBodyColor(new Color(0.1f, 0.05f, 0.05f));
        SpawnKillExplosion();
        Invoke(nameof(Deactivate), 0.6f);
        Debug.Log($"<color=magenta>[Target] {gameObject.name} DESTROYED!</color>");
    }

    void Deactivate() => gameObject.SetActive(false);

    // ─────────────────────────────────────────────
    // PUBLIC
    // ─────────────────────────────────────────────

    public float GetHP() => currentHP;
    public float GetMaxHP() => maxHP;
    public float GetHPPercent() => currentHP / maxHP;
    public bool IsDead() => isDead;

    public void ResetTarget()
    {
        currentHP = maxHP;
        isDead = false;
        gameObject.SetActive(true);
        if (barRoot != null) barRoot.SetActive(true);
        SetBodyColor(defaultColor);
        UpdateBar();
    }


void SpawnKillExplosion()
    {
        // Fire burst
        GameObject fireGO = new GameObject("KillExplosion_Fire");
        fireGO.transform.position = transform.position + Vector3.up * 1.5f;
        ParticleSystem fire = fireGO.AddComponent<ParticleSystem>();

        var fMain = fire.main;
        fMain.loop = false;
        fMain.playOnAwake = false;
        fMain.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 1.0f);
        fMain.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
        fMain.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
        fMain.maxParticles = 60;
        fMain.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.3f, 0f), new Color(1f, 0.8f, 0f));
        fMain.gravityModifier = -0.3f;

        var fEmit = fire.emission;
        fEmit.enabled = true;
        fEmit.rateOverTime = 0f;
        fEmit.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 60) });

        var fShape = fire.shape;
        fShape.shapeType = ParticleSystemShapeType.Sphere;
        fShape.radius = 0.8f;

        Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (urpUnlit == null) urpUnlit = Shader.Find("Unlit/Color");
        Material fMat = new Material(urpUnlit);
        fMat.color = new Color(1f, 0.4f, 0f);
        fire.GetComponent<ParticleSystemRenderer>().material = fMat;

        // Smoke burst
        GameObject smokeGO = new GameObject("KillExplosion_Smoke");
        smokeGO.transform.position = transform.position + Vector3.up * 1.5f;
        ParticleSystem smoke = smokeGO.AddComponent<ParticleSystem>();

        var sMain = smoke.main;
        sMain.loop = false;
        sMain.playOnAwake = false;
        sMain.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        sMain.startSpeed = new ParticleSystem.MinMaxCurve(1f, 4f);
        sMain.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        sMain.maxParticles = 30;
        sMain.startColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        sMain.gravityModifier = -0.1f;

        var sEmit = smoke.emission;
        sEmit.enabled = true;
        sEmit.rateOverTime = 0f;
        sEmit.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });

        var sShape = smoke.shape;
        sShape.shapeType = ParticleSystemShapeType.Sphere;
        sShape.radius = 0.5f;

        Material sMat = new Material(urpUnlit);
        sMat.color = new Color(0.15f, 0.15f, 0.15f);
        smoke.GetComponent<ParticleSystemRenderer>().material = sMat;

        fire.Play();
        smoke.Play();

        Destroy(fireGO, 2f);
        Destroy(smokeGO, 2f);
    }
}
