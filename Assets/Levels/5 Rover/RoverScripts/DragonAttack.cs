using UnityEngine;
using System.Collections.Generic;

/// Dragon combat AI — survival mode.
/// All 4 dragons attack simultaneously. Short rests. Spread fire when enraged.
public class DragonAttack : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRange = 80f;
    public float attackRange    = 15f;
    public float minDistance    = 7f;

    [Header("Attack")]
    public float      attackCooldown = 1.5f;
    public GameObject projectilePrefab;
    public Transform  firePoint;

    [Header("Normal Movement")]
    public float moveSpeed           = 10f;
    public float turnSpeed           = 3.5f;
    public float bobAmplitude        = 1f;
    public float bobSpeed            = 1.2f;
    public float maxBankAngle        = 20f;
    public float bankSmoothing       = 3f;
    public float diveSpeedMultiplier = 1.6f;
    public Vector3 diveRotationOffset    = new Vector3(20.66f, 260.72f, 160.75f);
    public Vector3 forwardRotationOffset = new Vector3(75f, 0f, 0f);
    public float preferredHeight = 4f;

    [Header("Enrage (on hit)")]
    public float rageMoveSpeed   = 14f;
    public float rageTurnSpeed   = 6f;
    public float rageDuration    = 10f;
    public int   rageExtraSwoops = 3;

    [Header("Spread Fire (rage only)")]
    [Tooltip("Number of projectiles fired in a spread when enraged")]
    public int   spreadCount = 3;
    [Tooltip("Angle between spread projectiles in degrees")]
    public float spreadAngle = 15f;

    [Header("Sky Sector")]
    public float spawnHeight = 30f;
    public float spawnRadius = 45f;

    [Header("Separation")]
    public float separationRadius = 12f;
    public float separationWeight = 4f;

    [Header("Balance")]
    [Tooltip("Max dragons in Attack/Swoop at the same time (4 = all)")]
    public int maxSimultaneousAttackers = 4;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   roarSound;
    public AudioClip   fireSound;

    // --- shared ---
    private static readonly List<DragonAttack> all = new List<DragonAttack>();

    // --- sector ---
    private int   sectorIndex;
    private float sectorAngle;
    private float orbitRadius;
    private float orbitHeight;
    private float orbitSpeed;
    private float orbitAngle;

    // --- state ---
    private enum Mode { SkyPatrol, Swoop, Attack, Retreat, Rest }
    private Mode  mode = Mode.SkyPatrol;
    private float modeTimer;
    private int   rageExtraSwoopsLeft;

    // --- rage ---
    private float rageTimer;
    private bool  IsRaged => rageTimer > 0f;

    // --- movement ---
    private Transform            player;
    private FlyingCreaturePatrol patrol;
    private float                lastAttackTime = -999f;
    private float                bobOffset;
    private Vector3              smoothedDir;
    private float                currentBank;

    void OnEnable()  { if (!all.Contains(this)) all.Add(this); }
    void OnDisable() { all.Remove(this); }

    void Start()
    {
        patrol      = GetComponent<FlyingCreaturePatrol>();
        bobOffset   = Random.Range(0f, Mathf.PI * 2f);
        sectorIndex = all.IndexOf(this) % 4;
        sectorAngle = sectorIndex * 90f;
        orbitRadius = 22f + sectorIndex * 4f;
        orbitHeight = spawnHeight - sectorIndex * 3f;
        orbitSpeed  = 15f + sectorIndex * 3f;
        orbitAngle  = sectorAngle;
        smoothedDir = transform.forward;

        if (Camera.main != null)
        {
            player = Camera.main.transform;
            float rad = sectorAngle * Mathf.Deg2Rad;
            transform.position = player.position
                + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * spawnRadius
                + Vector3.up * spawnHeight;
        }

        mode      = Mode.SkyPatrol;
        modeTimer = Random.Range(0.5f, 2f) + sectorIndex * 1f;
    }

    public void OnHit()
    {
        rageTimer           = rageDuration;
        rageExtraSwoopsLeft = rageExtraSwoops;

        if (mode == Mode.SkyPatrol || mode == Mode.Rest)
        {
            if (AttackerCount() < maxSimultaneousAttackers)
            {
                mode      = Mode.Swoop;
                modeTimer = 0f;
            }
        }
    }

    void Update()
    {
        if (player == null) { if (Camera.main != null) player = Camera.main.transform; return; }

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > detectionRange) { if (patrol != null) patrol.enabled = true; return; }
        if (patrol != null) patrol.enabled = false;

        rageTimer -= Time.deltaTime;
        modeTimer -= Time.deltaTime;

        // --- State transitions ---
        switch (mode)
        {
            case Mode.SkyPatrol:
            case Mode.Rest:
                if (modeTimer <= 0f && AttackerCount() < maxSimultaneousAttackers)
                {
                    mode      = Mode.Swoop;
                    modeTimer = 0f;
                }
                break;

            case Mode.Swoop:
                if (dist <= attackRange)
                {
                    mode      = Mode.Attack;
                    modeTimer = IsRaged ? Random.Range(6f, 10f) : Random.Range(8f, 14f);
                }
                else if (modeTimer < -10f)
                {
                    GoRetreat();
                }
                break;

            case Mode.Attack:
                if (modeTimer <= 0f)
                {
                    if (IsRaged && rageExtraSwoopsLeft > 0)
                    {
                        rageExtraSwoopsLeft--;
                        mode      = Mode.Swoop;
                        modeTimer = 0f;
                    }
                    else
                    {
                        GoRetreat();
                    }
                }
                break;

            case Mode.Retreat:
                float heightDiff = Mathf.Abs(transform.position.y - (player.position.y + orbitHeight));
                if (heightDiff < 5f || modeTimer <= 0f)
                {
                    mode      = Mode.Rest;
                    modeTimer = Random.Range(1f, 3f);
                }
                break;
        }

        // --- Goal ---
        Vector3 goal = ComputeGoal();

        Vector3 flat = new Vector3(goal.x - player.position.x, 0f, goal.z - player.position.z);
        if (flat.magnitude < minDistance && flat.magnitude > 0.01f)
        {
            goal   = player.position + flat.normalized * minDistance;
            goal.y = ComputeGoalY();
        }

        // Separation
        Vector3 sep = Vector3.zero;
        foreach (var other in all)
        {
            if (other == null || other == this) continue;
            Vector3 away = transform.position - other.transform.position;
            float   d    = away.magnitude;
            if (d < separationRadius && d > 0.01f)
                sep += away.normalized * Mathf.Pow(1f - d / separationRadius, 2f) * separationWeight;
        }
        goal += sep;

        // --- Move ---
        float spd  = IsRaged ? rageMoveSpeed : moveSpeed;
        float tSpd = IsRaged ? rageTurnSpeed : turnSpeed;
        if (smoothedDir.y < 0f) spd *= 1f + Mathf.Abs(smoothedDir.y) * (diveSpeedMultiplier - 1f);

        Vector3 rawDir = (goal - transform.position).normalized;
        smoothedDir    = Vector3.Lerp(smoothedDir, rawDir, tSpd * Time.deltaTime);
        transform.position += smoothedDir * spd * Time.deltaTime;

        // Bob
        Vector3 p = transform.position;
        p.y += Mathf.Sin(Time.time * bobSpeed + bobOffset) * bobAmplitude * Time.deltaTime;
        transform.position = p;

        // --- Rotation ---
        if (smoothedDir.sqrMagnitude > 0.001f)
        {
            Quaternion tRot    = Quaternion.LookRotation(smoothedDir, Vector3.up);
            Vector3    cross   = Vector3.Cross(transform.forward, rawDir);
            float      tBank   = Mathf.Clamp(-cross.y * maxBankAngle * 10f, -maxBankAngle, maxBankAngle);
            currentBank        = Mathf.Lerp(currentBank, tBank, bankSmoothing * Time.deltaTime);
            float      dive    = Mathf.Clamp01(-smoothedDir.y);
            Quaternion normRot = tRot * Quaternion.Euler(0f, 0f, currentBank) * Quaternion.Euler(forwardRotationOffset);
            transform.rotation = Quaternion.Slerp(normRot, Quaternion.Euler(diveRotationOffset), dive);
        }

        // --- Fire ---
        if (mode == Mode.Attack && dist <= attackRange && Time.time >= lastAttackTime + attackCooldown)
            Fire();
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    int AttackerCount()
    {
        int c = 0;
        foreach (var d in all)
            if (d != null && (d.mode == Mode.Swoop || d.mode == Mode.Attack)) c++;
        return c;
    }

    void GoRetreat()
    {
        mode      = Mode.Retreat;
        modeTimer = Random.Range(3f, 6f);
    }

    float ComputeGoalY()
    {
        switch (mode)
        {
            case Mode.Swoop:  return player.position.y + preferredHeight * 0.3f;
            case Mode.Attack: return player.position.y + 2f + Mathf.Sin(Time.time * bobSpeed + bobOffset) * bobAmplitude;
            default:          return player.position.y + orbitHeight + Mathf.Sin(Time.time * 0.5f + bobOffset) * 2f;
        }
    }

    Vector3 ComputeGoal()
    {
        switch (mode)
        {
            case Mode.SkyPatrol:
            case Mode.Rest:
            {
                orbitAngle += orbitSpeed * Time.deltaTime;
                float minA = sectorAngle - 55f, maxA = sectorAngle + 55f;
                if (orbitAngle >= maxA || orbitAngle <= minA) orbitSpeed = -orbitSpeed;
                orbitAngle = Mathf.Clamp(orbitAngle, minA, maxA);
                float   rad = orbitAngle * Mathf.Deg2Rad;
                Vector3 pos = player.position + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * orbitRadius;
                pos.y = player.position.y + orbitHeight + Mathf.Sin(Time.time * 0.5f + bobOffset) * 2f;
                return pos;
            }
            case Mode.Swoop:
                // Dive directly at player
                return new Vector3(player.position.x, player.position.y + preferredHeight * 0.3f, player.position.z);

            case Mode.Attack:
            {
                float circleSpd = IsRaged ? 70f : 45f;
                orbitAngle += circleSpd * Time.deltaTime;
                float rad = orbitAngle * Mathf.Deg2Rad;
                float r   = IsRaged ? attackRange * 0.65f : attackRange * 0.8f;
                Vector3 pos = player.position + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * r;
                pos.y = player.position.y + (IsRaged ? 1.5f : 3f) + Mathf.Sin(Time.time * bobSpeed + bobOffset) * bobAmplitude;
                return pos;
            }
            case Mode.Retreat:
            default:
            {
                float rad = sectorAngle * Mathf.Deg2Rad;
                Vector3 pos = player.position + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * orbitRadius;
                pos.y = player.position.y + orbitHeight;
                return pos;
            }
        }
    }

    void Fire()
    {
        lastAttackTime = Time.time;
        if (audioSource && roarSound) audioSource.PlayOneShot(roarSound);
        if (audioSource && fireSound)  audioSource.PlayOneShot(fireSound);
        if (projectilePrefab == null || firePoint == null) return;

        if (IsRaged && spreadCount > 1)
        {
            // Spread fire: fan of projectiles centered on player
            float halfSpread = spreadAngle * (spreadCount - 1) / 2f;
            for (int i = 0; i < spreadCount; i++)
            {
                float   yaw  = -halfSpread + i * spreadAngle;
                Vector3 dir  = Quaternion.Euler(0f, yaw, 0f) * (player.position - firePoint.position).normalized;
                var     proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(dir));
                var     dp   = proj.GetComponent<DragonProjectile>();
                if (dp != null) dp.SetTarget(player);
            }
        }
        else
        {
            var proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            var dp   = proj.GetComponent<DragonProjectile>();
            if (dp != null) dp.SetTarget(player);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;    Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.green;  Gizmos.DrawWireSphere(transform.position, minDistance);
        Gizmos.color = Color.cyan;   Gizmos.DrawWireSphere(transform.position, separationRadius);
    }
}
