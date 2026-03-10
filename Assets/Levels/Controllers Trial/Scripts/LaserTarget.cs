using System.Collections;
using UnityEngine;

public class LaserTarget : MonoBehaviour
{
    [Header("Health")]
    public float maxHP           = 100f;
    public float damagePerSecond = 20f;

    [Header("Health Bar")]
    public float barWidth   = 1.5f;
    public float barHeight  = 0.15f;
    public float barOffsetY = 4f;

    [Header("Hit Reaction")]
    public float flashDuration   = 0.1f;
    public float knockbackForce  = 3f;
    public float staggerDuration = 0.18f;

    private float      currentHP;
    private bool       isDead;
    private float      flashTimer;
    private bool       isStaggering;
    private Renderer[] bodyRenderers;
    private Color[]    originalColors;
    private Shader     urpUnlit;
    private GameObject barRoot;
    private Transform  fillTransform;
    private Material   fillMat;
    private Transform  camTransform;
    private ParticleSystem bloodSplatter;
    private ParticleSystem burnMark;

    void Start()
    {
        currentHP    = maxHP;
        urpUnlit     = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        bodyRenderers  = GetComponentsInChildren<Renderer>();
        originalColors = new Color[bodyRenderers.Length];
        for (int i = 0; i < bodyRenderers.Length; i++)
            originalColors[i] = bodyRenderers[i].material.color;
        camTransform = Camera.main != null ? Camera.main.transform : null;
        BuildBar();
        BuildBloodSplatter();
        BuildBurnMark();
    }

public void OnLaserHit()
    {
        if (isDead) return;
        currentHP -= damagePerSecond * Time.deltaTime;
        currentHP  = Mathf.Max(currentHP, 0f);
        UpdateBar();
        FlashColor(new Color(1f, 0.15f, 0.1f));
        flashTimer = flashDuration;
        if (bloodSplatter != null && !bloodSplatter.isPlaying)
            bloodSplatter.Play();
        if (!isStaggering)
            StartCoroutine(Stagger());
        // Notify DragonAttack so it can enrage
        var da = GetComponent<DragonAttack>();
        if (da != null) da.OnHit();
        if (currentHP <= 0f) Die();
    }

IEnumerator Stagger()
    {
        isStaggering = true;
        float elapsed = 0f;
        // Shake renderers only — never touch transform.position (DragonAttack owns it)
        Vector3[] originalLocalPos = new Vector3[bodyRenderers.Length];
        for (int i = 0; i < bodyRenderers.Length; i++)
            if (bodyRenderers[i] != null)
                originalLocalPos[i] = bodyRenderers[i].transform.localPosition;

        while (elapsed < staggerDuration)
        {
            float shake = knockbackForce * (1f - elapsed / staggerDuration) * 0.04f;
            for (int i = 0; i < bodyRenderers.Length; i++)
                if (bodyRenderers[i] != null)
                    bodyRenderers[i].transform.localPosition = originalLocalPos[i]
                        + Random.insideUnitSphere * shake;
            elapsed += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < bodyRenderers.Length; i++)
            if (bodyRenderers[i] != null)
                bodyRenderers[i].transform.localPosition = originalLocalPos[i];

        isStaggering = false;
    }

    void Update()
    {
        if (camTransform == null && Camera.main != null)
            camTransform = Camera.main.transform;
        if (barRoot != null && camTransform != null)
        {
            Vector3 dir = barRoot.transform.position - camTransform.position;
            if (dir.sqrMagnitude > 0.001f)
                barRoot.transform.rotation = Quaternion.LookRotation(dir);
        }
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f) RestoreColors();
        }
    }

    void Die()
    {
        isDead = true;
        StopAllCoroutines();
        if (barRoot != null) barRoot.SetActive(false);
        if (bloodSplatter != null) bloodSplatter.Stop();
        FlashColor(new Color(0.08f, 0.04f, 0.02f));
        SpawnKillExplosion();
        StartCoroutine(ScreenShake(0.3f, 0.25f));
        Invoke(nameof(Deactivate), 0.8f);
        Debug.Log($"<color=magenta>[Target] {name} DESTROYED!</color>");
    }

    void Deactivate() => gameObject.SetActive(false);

    IEnumerator ScreenShake(float magnitude, float duration)
    {
        if (camTransform == null) yield break;
        Vector3 origin  = camTransform.localPosition;
        float   elapsed = 0f;
        while (elapsed < duration)
        {
            float fade = 1f - elapsed / duration;
            camTransform.localPosition = origin + Random.insideUnitSphere * magnitude * fade;
            elapsed += Time.deltaTime;
            yield return null;
        }
        camTransform.localPosition = origin;
    }

    void BuildBar()
    {
        float topY = transform.localScale.y * 0.5f + barOffsetY;
        barRoot = new GameObject("HPBar_Root");
        barRoot.transform.SetParent(transform);
        barRoot.transform.localPosition = new Vector3(0f, topY / transform.localScale.y, 0f);
        barRoot.transform.localScale = new Vector3(
            1f / transform.localScale.x, 1f / transform.localScale.y, 1f / transform.localScale.z);
        CreateQuad("BG",   barWidth + 0.02f, barHeight + 0.02f, 0f,    new Color(0.1f, 0.02f, 0.02f));
        var fill     = CreateQuad("Fill", barWidth, barHeight, -0.01f, Color.green);
        fillTransform = fill.transform;
        fillMat       = fill.GetComponent<Renderer>().material;
    }

    GameObject CreateQuad(string label, float w, float h, float z, Color col)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = label;
        go.transform.SetParent(barRoot.transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, z);
        go.transform.localScale    = new Vector3(w, h, 1f);
        Destroy(go.GetComponent<Collider>());
        var mat = new Material(urpUnlit);
        mat.color = col;
        go.GetComponent<Renderer>().material = mat;
        return go;
    }

    void UpdateBar()
    {
        if (fillTransform == null) return;
        float pct = currentHP / maxHP;
        fillTransform.localScale    = new Vector3(barWidth * pct, barHeight, 1f);
        fillTransform.localPosition = new Vector3(-barWidth * 0.5f + barWidth * pct * 0.5f, 0f, -0.01f);
        fillMat.color = pct > 0.5f
            ? Color.Lerp(Color.yellow, Color.green,  (pct - 0.5f) * 2f)
            : Color.Lerp(Color.red,    Color.yellow, pct * 2f);
    }

    void BuildBloodSplatter()
    {
        var go = new GameObject("BloodSplatter");
        go.transform.SetParent(transform);
        go.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        bloodSplatter = go.AddComponent<ParticleSystem>();
        var main               = bloodSplatter.main;
        main.loop              = false;
        main.playOnAwake       = false;
        main.simulationSpace   = ParticleSystemSimulationSpace.World;
        main.startLifetime     = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
        main.startSpeed        = new ParticleSystem.MinMaxCurve(3f, 9f);
        main.startSize         = new ParticleSystem.MinMaxCurve(0.06f, 0.18f);
        main.maxParticles      = 80;
        main.startColor        = new ParticleSystem.MinMaxGradient(new Color(0.55f, 0f, 0f), new Color(0.9f, 0.05f, 0f));
        main.gravityModifier   = 1.8f;
        var emission           = bloodSplatter.emission;
        emission.rateOverTime  = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18, 24) });
        var shape              = bloodSplatter.shape;
        shape.shapeType        = ParticleSystemShapeType.Cone;
        shape.angle            = 35f;
        shape.radius           = 0.1f;
        var col                = bloodSplatter.colorOverLifetime;
        col.enabled            = true;
        var grad               = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(new Color(0.8f, 0f, 0f), 0f), new GradientColorKey(new Color(0.3f, 0f, 0f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);
        var rend               = bloodSplatter.GetComponent<ParticleSystemRenderer>();
        rend.renderMode        = ParticleSystemRenderMode.Billboard;
        var mat                = new Material(urpUnlit ?? Shader.Find("Sprites/Default"));
        mat.color              = new Color(0.7f, 0f, 0f);
        rend.sharedMaterial    = mat;
    }

    void BuildBurnMark()
    {
        var go = new GameObject("BurnMark");
        go.transform.SetParent(transform);
        go.transform.localPosition = new Vector3(0f, 1.5f, -0.05f);
        burnMark = go.AddComponent<ParticleSystem>();
        var main             = burnMark.main;
        main.loop            = true;
        main.playOnAwake     = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        main.maxParticles    = 30;
        main.startColor      = new ParticleSystem.MinMaxGradient(new Color(1f, 0.5f, 0f), new Color(0.3f, 0.1f, 0f));
        main.gravityModifier = -0.1f;
        var emission         = burnMark.emission;
        emission.rateOverTime = 8f;
        var shape            = burnMark.shape;
        shape.shapeType      = ParticleSystemShapeType.Circle;
        shape.radius         = 0.15f;
        var col              = burnMark.colorOverLifetime;
        col.enabled          = true;
        var grad             = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.4f, 0f), 0f), new GradientColorKey(new Color(0.1f, 0.1f, 0.1f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);
        var rend             = burnMark.GetComponent<ParticleSystemRenderer>();
        rend.renderMode      = ParticleSystemRenderMode.Billboard;
        var mat              = new Material(urpUnlit ?? Shader.Find("Sprites/Default"));
        mat.color            = new Color(0.9f, 0.3f, 0f);
        rend.sharedMaterial  = mat;
    }

    void SpawnKillExplosion()
    {
        SpawnBurst("KillFire",  60, new Color(1f, 0.3f, 0f),     new Color(1f, 0.8f, 0f),     8f,  0.9f, 1f,   -0.3f);
        SpawnBurst("KillSmoke", 30, new Color(0.15f,0.15f,0.15f), new Color(0.3f,0.3f,0.3f),  4f,  1.5f, 1.5f, -0.1f);
        SpawnBurst("KillBlood", 50, new Color(0.7f, 0f, 0f),      new Color(0.4f, 0f, 0f),    10f, 0.4f, 0.6f,  2f);
    }

    void SpawnBurst(string label, int count, Color c1, Color c2,
                    float speed, float size, float life, float gravity)
    {
        var go  = new GameObject(label);
        go.transform.position = transform.position + Vector3.up * 1.5f;
        var ps  = go.AddComponent<ParticleSystem>();
        var m   = ps.main;
        m.loop  = false; m.playOnAwake = false;
        m.startLifetime  = life;
        m.startSpeed     = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
        m.startSize      = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
        m.maxParticles   = count;
        m.startColor     = new ParticleSystem.MinMaxGradient(c1, c2);
        m.gravityModifier = gravity;
        m.simulationSpace = ParticleSystemSimulationSpace.World;
        var e   = ps.emission;
        e.rateOverTime = 0f;
        e.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });
        var sh  = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.8f;
        var rend = ps.GetComponent<ParticleSystemRenderer>();
        var mat  = new Material(urpUnlit ?? Shader.Find("Sprites/Default"));
        mat.color = c1; rend.sharedMaterial = mat;
        ps.Play();
        Destroy(go, life + 0.5f);
    }

    void FlashColor(Color c)
    {
        foreach (var r in bodyRenderers)
            if (r != null) r.material.color = c;
    }

    void RestoreColors()
    {
        for (int i = 0; i < bodyRenderers.Length; i++)
            if (bodyRenderers[i] != null)
                bodyRenderers[i].material.color = originalColors[i];
        if (burnMark != null && !burnMark.isPlaying && currentHP / maxHP < 0.5f)
            burnMark.Play();
    }

    public float GetHP()        => currentHP;
    public float GetMaxHP()     => maxHP;
    public float GetHPPercent() => currentHP / maxHP;
    public bool  IsDead()       => isDead;

    public void ResetTarget()
    {
        StopAllCoroutines();
        currentHP = maxHP; isDead = false;
        gameObject.SetActive(true);
        if (barRoot  != null) barRoot.SetActive(true);
        if (burnMark != null) burnMark.Stop();
        RestoreColors();
        UpdateBar();
    }
}
