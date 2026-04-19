using UnityEngine;
using System.Collections.Generic;

/// Dragon combat AI — gradual aggression.
/// Aggression ramps from 0→1 over aggressionRampTime seconds (shared across all dragons).
/// At 0: 1 attacker, slow, long rests, no spread.
/// At 1: 4 attackers, fast, short rests, spread fire on rage.
public class DragonAttack : MonoBehaviour
{
    private const float EngagementZonePadding = 18f;
    private const float AwarenessZonePaddingMin = 10f;
    private const float AwarenessZonePaddingMax = 34f;
    private const float ProactiveDetectionRangeMultiplierMin = 0.45f;
    private const float ProactiveDetectionRangeMultiplierMax = 1.15f;
    private const float AggressiveAttackRangePadding = 7f;
    private const float AggressiveMinDistanceFactor = 0.4f;
    private const float AggressiveAttackCooldownMultiplier = 0.62f;
    private const float LowSpeedPursuitSpeedMultiplier = 1.08f;
    private const float AggressivePursuitSpeedMultiplier = 1.38f;
    private const float LowSpeedTurnSpeedMultiplier = 1.04f;
    private const float AggressiveTurnSpeedMultiplier = 1.25f;
    private const float PlayerSpeedThreatStart = 8f;
    private const float PlayerSpeedThreatMax = 28f;
    private const float FastPlayerAggroThreshold = 0.6f;
    private const float SpeedPressureWeight = 0.75f;
    private const float AlertMemoryMin = 2.75f;
    private const float AlertMemoryMax = 7.5f;

    [Header("Debug Gizmos")]
    public bool showGizmos = false;

    
[Header("Detection")]
    public float detectionRange = 80f;
    public float attackRange    = 15f;
    public float minDistance    = 7f;

    [Header("Attack")]
    public float      attackCooldownMin = 1.5f;   // at full aggression
    public float      attackCooldownMax = 4.5f;   // at zero aggression
    public GameObject projectilePrefab;
    public Transform  firePoint;

    [Header("Normal Movement")]
    public float moveSpeedMin        = 5f;    // at zero aggression
    public float moveSpeedMax        = 10f;   // at full aggression
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

    [Header("Spread Fire (rage only, high aggression)")]
    public int   spreadCount = 3;
    public float spreadAngle = 15f;

    [Header("Sky Sector")]
    public float spawnHeight = 30f;
    public float spawnRadius = 45f;

    [Header("Separation")]
    public float separationRadius = 12f;
    public float separationWeight = 4f;

    [Header("Aggression Ramp")]
    [Tooltip("Seconds to reach full aggression from zero")]
    public float aggressionRampTime = 120f;

    [Header("Volcano Area Overrides")]
    [Tooltip("Skip the shared Attack_Area zone and use patrol waypoints as the engagement zone instead.")]
    public bool ignoreAttackArea = false;
    [Tooltip("Max dragons that can attack simultaneously. Overrides the default scale of 2-4.")]
    public int maxSimultaneousAttackers = 4;

    // --- shared aggression / grace timers (all dragons read from same static clock) ---
    private static float s_aggressionTimer = 0f;
    private static float s_rampTime        = 120f;
    private static float s_graceTimer      = 0f;
    private static bool  s_graceExpired    = false;
    private static float s_alertUntil      = 0f;
    private const  float CombatGracePeriod = 3f;

    // Aggression 0..1
    private static float Aggression => Mathf.Clamp01(s_aggressionTimer / s_rampTime);

    // Max simultaneous attackers: 2 at 0 aggression → maxSimultaneousAttackers at full
    private int MaxAttackers => Mathf.RoundToInt(Mathf.Lerp(2f, maxSimultaneousAttackers, GetBehaviorPressure()));

    // Rest time: long at low aggression → short at high
    private float RestTimeMin => Mathf.Lerp(4f, 1f, GetBehaviorPressure());
    private float RestTimeMax => Mathf.Lerp(8f, 2f, GetBehaviorPressure());

    // First patrol delay before first swoop: shorter than before so targets react sooner.
    private float FirstPatrolMin => Mathf.Lerp(4f, 0.8f, GetBehaviorPressure());
    private float FirstPatrolMax => Mathf.Lerp(9f, 2f, GetBehaviorPressure());

    // When the player enters the combat footprint, clamp any lingering patrol wait to this range.
    private float AggressiveEngageDelayMin => Mathf.Lerp(2.25f, 0.25f, GetBehaviorPressure());
    private float AggressiveEngageDelayMax => Mathf.Lerp(4f, 0.9f, GetBehaviorPressure());

    // Current attack cooldown
    private float AttackCooldown => Mathf.Lerp(attackCooldownMax, attackCooldownMin, GetBehaviorPressure());

    // Current move speed
    private float MoveSpeed => Mathf.Lerp(moveSpeedMin, moveSpeedMax, GetBehaviorPressure());

    // Spread fire only above 70% aggression
    private bool CanSpread => IsRaged && Aggression > 0.7f && spreadCount > 1;

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
    private Collider             cachedCollider;   // FIX 7: cached once in Start
    private float                lastAttackTime = -999f;
    private float                bobOffset;
    private Vector3              smoothedDir;
    private float                currentBank;
    private bool                 alwaysChasePlayer;
    private Bounds               engagementZoneBounds;
    private bool                 hasEngagementZoneBounds;
    private bool                 playerInsideEngagementZone;
    private bool                 playerWithinAwarenessZone;
    private bool                 wasAggressiveZoneActive;
    private RoverPhysicsController roverPhysics;
    private Vector3              lastPlayerPosition;
    private Vector3              smoothedPlayerVelocity;
    private float                smoothedPlayerSpeed;
    private bool                 hasPlayerPositionSample;
    private float                instanceAttackCooldownMult;
    private float                instanceSpeedMult;
    private float                instanceOrbitSpeedMult;

    void OnEnable()
    {
        if (all.Count == 0)
        {
            s_aggressionTimer = 0f;
            s_graceTimer = 0f;
            s_graceExpired = false;
            s_alertUntil = 0f;
        }
        if (!all.Contains(this)) all.Add(this);
    }
    void OnDisable() { all.Remove(this); }

    void Start()
    {
        s_rampTime     = aggressionRampTime;
        cachedCollider = GetComponent<Collider>();
        patrol      = GetComponent<FlyingCreaturePatrol>();
        bobOffset   = Random.Range(0f, Mathf.PI * 2f);
        sectorIndex = all.IndexOf(this) % 4;
        sectorAngle = sectorIndex * 90f;
        orbitRadius = 22f + sectorIndex * 4f;
        orbitHeight = spawnHeight - sectorIndex * 3f;
        orbitSpeed  = 15f + sectorIndex * 3f;
        orbitAngle  = sectorAngle;
        smoothedDir = transform.forward;
        instanceAttackCooldownMult = Random.Range(0.7f, 1.3f);
        instanceSpeedMult = Random.Range(0.85f, 1.15f);
        instanceOrbitSpeedMult = Random.Range(0.8f, 1.2f);
        alwaysChasePlayer = gameObject.name.StartsWith("Target_") || gameObject.name.StartsWith("VolcanoTarget");
        TryInitializeEngagementZoneBounds();
        SnapTargetIntoWaypointRouteIfNeeded();

        player = ResolvePlayerTransform();
        // Only reposition non-target dragons to orbit the player at start.
        // Targets stay in their authored patrol space.
        if (player != null && !alwaysChasePlayer)
        {
            float rad = sectorAngle * Mathf.Deg2Rad;
            transform.position = player.position
                + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * spawnRadius
                + Vector3.up * spawnHeight;
        }

        mode      = Mode.SkyPatrol;
        // Stagger first attack: each dragon waits longer at start
        float initialStagger = alwaysChasePlayer ? sectorIndex * 0.6f : sectorIndex * 3f;
        modeTimer = Random.Range(FirstPatrolMin, FirstPatrolMax) + initialStagger;
    }

    public void OnHit()
    {
        rageTimer           = rageDuration;
        rageExtraSwoopsLeft = rageExtraSwoops;

        if (mode == Mode.SkyPatrol || mode == Mode.Rest)
        {
            if (AttackerCount() < MaxAttackers)
            {
                mode      = Mode.Swoop;
                modeTimer = 0f;
            }
        }
    }

    void Update()
    {
        // FIX 1: only sectorIndex 0 ticks timers — prevents 4x speed with 4 dragons
        if (sectorIndex == 0)
        {
            s_aggressionTimer += Time.deltaTime;
            s_aggressionTimer  = Mathf.Min(s_aggressionTimer, aggressionRampTime);

            if (!s_graceExpired)
            {
                s_graceTimer += Time.deltaTime;
                if (s_graceTimer >= CombatGracePeriod)
                    s_graceExpired = true;
            }
        }

        if (player == null)
        {
            player = ResolvePlayerTransform();
            if (player == null)
                return;
        }

        UpdatePlayerMotionState();

        if (alwaysChasePlayer && !hasEngagementZoneBounds)
            TryInitializeEngagementZoneBounds();

        playerInsideEngagementZone = IsPlayerInsideEngagementZone(player.position);
        playerWithinAwarenessZone = IsPlayerWithinAwarenessZone(player.position);
        float dist = Vector3.Distance(transform.position, player.position);
        float speedPressure = GetPlayerSpeedThreat01();
        float proactiveDetectionRange = GetProactiveDetectionRange();
        bool canSeePlayerThreat = alwaysChasePlayer &&
            (playerInsideEngagementZone || playerWithinAwarenessZone || dist <= proactiveDetectionRange);
        if (canSeePlayerThreat)
        {
            float alertMemory = Mathf.Lerp(AlertMemoryMin, AlertMemoryMax, Mathf.Max(GetBehaviorPressure(), speedPressure));
            s_alertUntil = Mathf.Max(s_alertUntil, Time.time + alertMemory);
        }

        bool globalAlert = alwaysChasePlayer && Time.time <= s_alertUntil;
        bool aggressiveZoneActive = alwaysChasePlayer && (playerInsideEngagementZone || playerWithinAwarenessZone || globalAlert);
        bool isCommittedToAttackRun = mode == Mode.Swoop || mode == Mode.Attack || mode == Mode.Retreat;
        if (aggressiveZoneActive && !wasAggressiveZoneActive && (mode == Mode.SkyPatrol || mode == Mode.Rest))
        {
            float engageDelay = Random.Range(AggressiveEngageDelayMin, AggressiveEngageDelayMax) + sectorIndex * 0.35f;
            if (speedPressure >= FastPlayerAggroThreshold)
                engageDelay = Mathf.Min(engageDelay, Mathf.Lerp(0.65f, 0.2f, speedPressure));
            modeTimer = Mathf.Min(modeTimer, engageDelay);
        }
        wasAggressiveZoneActive = aggressiveZoneActive;

        float currentAttackRange = GetCurrentAttackRange();
        float currentMinDistance = GetCurrentMinDistance();
        float baseAttackCooldown = aggressiveZoneActive
            ? Mathf.Lerp(AttackCooldown, attackCooldownMin * AggressiveAttackCooldownMultiplier, speedPressure)
            : AttackCooldown;
        float currentAttackCooldown = baseAttackCooldown * instanceAttackCooldownMult;
        float effectiveDetectionRange = alwaysChasePlayer
            ? ((aggressiveZoneActive || isCommittedToAttackRun) ? float.PositiveInfinity : proactiveDetectionRange)
            : detectionRange;

        if (dist > effectiveDetectionRange)
        {
            if (patrol != null) patrol.enabled = true;
            if (aggressiveZoneActive == false && mode != Mode.SkyPatrol)
            {
                mode = Mode.SkyPatrol;
                modeTimer = Mathf.Max(modeTimer, 0.5f);
            }
            return;
        }

        if (patrol != null) patrol.enabled = false;

        rageTimer -= Time.deltaTime;
        modeTimer -= Time.deltaTime;

        // --- State transitions ---
        switch (mode)
        {
            case Mode.SkyPatrol:
            case Mode.Rest:
                if (aggressiveZoneActive)
                {
                    if (modeTimer <= 0f && AttackerCount() < MaxAttackers)
                    {
                        mode = dist <= currentAttackRange ? Mode.Attack : Mode.Swoop;
                        modeTimer = dist <= currentAttackRange ? GetAggressiveAttackDuration() : 0f;
                    }
                }
                else if (modeTimer <= 0f && AttackerCount() < MaxAttackers)
                {
                    mode      = Mode.Swoop;
                    modeTimer = 0f;
                }
                break;

            case Mode.Swoop:
                if (dist <= currentAttackRange)
                {
                    mode      = Mode.Attack;
                    modeTimer = aggressiveZoneActive
                        ? GetAggressiveAttackDuration()
                        : (IsRaged
                            ? Random.Range(6f, 10f)
                            : Mathf.Lerp(12f, 8f, Aggression));
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
                    else GoRetreat();
                }
                break;

            case Mode.Retreat:
                float heightDiff = Mathf.Abs(transform.position.y - (player.position.y + orbitHeight));
                if (heightDiff < 5f || modeTimer <= 0f)
                {
                    mode      = Mode.Rest;
                    modeTimer = aggressiveZoneActive
                        ? Random.Range(AggressiveEngageDelayMin, AggressiveEngageDelayMax)
                        : Random.Range(RestTimeMin, RestTimeMax);
                }
                break;
        }

        bool shouldUsePatrolMovement = !aggressiveZoneActive && (mode == Mode.SkyPatrol || mode == Mode.Rest);
        if (patrol != null)
            patrol.enabled = shouldUsePatrolMovement;

        if (shouldUsePatrolMovement)
            return;

        // --- Goal ---
        Vector3 goal = ComputeGoal();

        Vector3 flat = new Vector3(goal.x - player.position.x, 0f, goal.z - player.position.z);
        if (flat.magnitude < currentMinDistance && flat.magnitude > 0.01f)
        {
            goal   = player.position + flat.normalized * currentMinDistance;
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
        bool aggressiveCombatMovement = aggressiveZoneActive && (mode == Mode.Swoop || mode == Mode.Attack || mode == Mode.Retreat);
        float combatSpeedMultiplier = aggressiveCombatMovement ? Mathf.Lerp(LowSpeedPursuitSpeedMultiplier, AggressivePursuitSpeedMultiplier, speedPressure) : 1f;
        float combatTurnMultiplier = aggressiveCombatMovement ? Mathf.Lerp(LowSpeedTurnSpeedMultiplier, AggressiveTurnSpeedMultiplier, speedPressure) : 1f;
        float spd  = (IsRaged ? rageMoveSpeed : (aggressiveZoneActive ? moveSpeedMax : MoveSpeed)) * instanceSpeedMult * combatSpeedMultiplier;
        float tSpd = (IsRaged ? rageTurnSpeed : turnSpeed) * combatTurnMultiplier;
        if (smoothedDir.y < 0f) spd *= 1f + Mathf.Abs(smoothedDir.y) * (diveSpeedMultiplier - 1f);

        Vector3 rawDir = (goal - transform.position).normalized;
        smoothedDir    = Vector3.Lerp(smoothedDir, rawDir, tSpd * Time.deltaTime);
        transform.position += smoothedDir * spd * Time.deltaTime;

        // Wall depenetration — FIX 7: use cachedCollider instead of GetComponent every frame — push dragon out of any solid it overlaps
        Collider[] overlaps = Physics.OverlapSphere(transform.position, 3f, ~(1 << 13), QueryTriggerInteraction.Ignore);
        foreach (var col in overlaps)
        {
            if (col.transform == transform || col.transform.IsChildOf(transform)) continue;
            if (Physics.ComputePenetration(
                    cachedCollider, transform.position, transform.rotation,
                    col, col.transform.position, col.transform.rotation,
                    out Vector3 pushDir, out float pushDist))
            {
                transform.position += pushDir * (pushDist + 0.05f);
            }
        }

        // Bob
        Vector3 p = transform.position;
        p.y += Mathf.Sin(Time.time * bobSpeed + bobOffset) * bobAmplitude * Time.deltaTime;
        transform.position = p;

        // --- Rotation ---
        if (smoothedDir.sqrMagnitude > 0.001f)
        {
            Quaternion tRot  = Quaternion.LookRotation(smoothedDir, Vector3.up);
            Vector3    cross = Vector3.Cross(transform.forward, rawDir);
            float      tBank = Mathf.Clamp(-cross.y * maxBankAngle * 10f, -maxBankAngle, maxBankAngle);
            currentBank      = Mathf.Lerp(currentBank, tBank, bankSmoothing * Time.deltaTime);
            float      dive  = Mathf.Clamp01(-smoothedDir.y);
            Quaternion normRot = tRot * Quaternion.Euler(0f, 0f, currentBank) * Quaternion.Euler(forwardRotationOffset);
            transform.rotation = Quaternion.Slerp(normRot, Quaternion.Euler(diveRotationOffset), dive);
        }

        // --- Fire ---
        if (mode == Mode.Attack && dist <= currentAttackRange && Time.time >= lastAttackTime + currentAttackCooldown)
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
        modeTimer = Random.Range(
            Mathf.Lerp(2.5f, 1.15f, GetBehaviorPressure()),
            Mathf.Lerp(4.5f, 2.4f, GetBehaviorPressure()));
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
        float currentAttackRange = GetCurrentAttackRange();
        Vector3 predictedPlayerPosition = GetPredictedPlayerPosition(Mathf.Lerp(0.18f, 0.7f, GetPlayerSpeedThreat01()));

        switch (mode)
        {
            case Mode.SkyPatrol:
            case Mode.Rest:
            {
                orbitAngle += orbitSpeed * Time.deltaTime;
                float minA = sectorAngle - 55f, maxA = sectorAngle + 55f;
                if (orbitAngle >= maxA || orbitAngle <= minA) orbitSpeed = -orbitSpeed;
                orbitAngle = Mathf.Clamp(orbitAngle, minA, maxA);
                float pressure = (playerInsideEngagementZone || playerWithinAwarenessZone) ? GetBehaviorPressure() : 0f;
                float shadowRadius = Mathf.Lerp(orbitRadius, Mathf.Max(currentAttackRange * 1.2f, 11f), pressure);
                float   rad = orbitAngle * Mathf.Deg2Rad;
                Vector3 pos = predictedPlayerPosition + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * shadowRadius;
                pos.y = predictedPlayerPosition.y + orbitHeight + Mathf.Sin(Time.time * 0.5f + bobOffset) * 2f;
                return pos;
            }
            case Mode.Swoop:
                return new Vector3(
                    predictedPlayerPosition.x,
                    player.position.y + Mathf.Lerp(preferredHeight * 0.3f, preferredHeight * 0.15f, GetPlayerSpeedThreat01()),
                    predictedPlayerPosition.z);

            case Mode.Attack:
            {
                float circleSpd = (IsRaged ? 70f : 45f) * instanceOrbitSpeedMult;
                orbitAngle += circleSpd * Time.deltaTime;
                float rad = orbitAngle * Mathf.Deg2Rad;
                float r   = IsRaged ? currentAttackRange * 0.65f : Mathf.Lerp(currentAttackRange * 0.8f, currentAttackRange * 0.6f, GetPlayerSpeedThreat01());
                Vector3 pos = predictedPlayerPosition + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * r;
                pos.y = player.position.y + (IsRaged ? 1.5f : 3f) + Mathf.Sin(Time.time * bobSpeed + bobOffset) * bobAmplitude;
                return pos;
            }
            case Mode.Retreat:
            default:
            {
                float rad = sectorAngle * Mathf.Deg2Rad;
                Vector3 pos = predictedPlayerPosition + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * orbitRadius;
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

        if (CanSpread)
        {
            float halfSpread = spreadAngle * (spreadCount - 1) / 2f;
            for (int i = 0; i < spreadCount; i++)
            {
                float   yaw  = -halfSpread + i * spreadAngle;
                Vector3 dir  = Quaternion.Euler(0f, yaw, 0f) * (player.position - firePoint.position).normalized;
                var     proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(dir));
                var     dp   = proj.GetComponent<DragonProjectile>() ?? proj.AddComponent<DragonProjectile>();
                dp.SetTarget(player);
            }
        }
        else
        {
            var proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            var dp   = proj.GetComponent<DragonProjectile>() ?? proj.AddComponent<DragonProjectile>();
            dp.SetTarget(player);
        }
    }

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   roarSound;
    public AudioClip   fireSound;

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;    Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.green;  Gizmos.DrawWireSphere(transform.position, minDistance);
        Gizmos.color = Color.cyan;   Gizmos.DrawWireSphere(transform.position, separationRadius);
    }

    private void TryInitializeEngagementZoneBounds()
    {
        if (!alwaysChasePlayer)
            return;

        // Prefer Attack_Area bounds directly — independent of Start ordering
        if (!ignoreAttackArea)
        {
            GameObject attackArea = GameObject.Find("Attack_Area");
            if (attackArea != null && attackArea.activeInHierarchy)
            {
                BoxCollider attackBounds = attackArea.GetComponent<BoxCollider>();
                if (attackBounds != null && attackBounds.enabled)
                {
                    engagementZoneBounds = attackBounds.bounds;
                    engagementZoneBounds.Expand(Vector3.one * EngagementZonePadding);
                    hasEngagementZoneBounds = true;
                    return;
                }
            }
        }

        // Fallback to patrol waypoints/bounds
        if (patrol == null) return;

        if (patrol.waypoints != null && patrol.waypoints.Count > 0)
        {
            bool foundWaypoint = false;
            Bounds bounds = default;

            foreach (Transform waypoint in patrol.waypoints)
            {
                if (waypoint == null)
                    continue;

                if (!foundWaypoint)
                {
                    bounds = new Bounds(waypoint.position, Vector3.zero);
                    foundWaypoint = true;
                }
                else
                {
                    bounds.Encapsulate(waypoint.position);
                }
            }

            if (foundWaypoint)
            {
                bounds.Expand(Vector3.one * EngagementZonePadding);
                engagementZoneBounds = bounds;
                hasEngagementZoneBounds = true;
                return;
            }
        }

        if (patrol.useBoundsClamp)
        {
            engagementZoneBounds = new Bounds();
            engagementZoneBounds.SetMinMax(patrol.boundsMin, patrol.boundsMax);
            engagementZoneBounds.Expand(Vector3.one * EngagementZonePadding);
            hasEngagementZoneBounds = true;
        }
    }

    private bool IsPlayerInsideEngagementZone(Vector3 playerPosition)
    {
        if (!alwaysChasePlayer)
            return false;

        if (!hasEngagementZoneBounds)
            return true;

        return playerPosition.x >= engagementZoneBounds.min.x &&
               playerPosition.x <= engagementZoneBounds.max.x &&
               playerPosition.z >= engagementZoneBounds.min.z &&
               playerPosition.z <= engagementZoneBounds.max.z;
    }

    private bool IsPlayerWithinAwarenessZone(Vector3 playerPosition)
    {
        if (!alwaysChasePlayer)
            return false;

        float pressure = GetPlayerSpeedThreat01();
        float padding = Mathf.Lerp(AwarenessZonePaddingMin, AwarenessZonePaddingMax, pressure);
        if (hasEngagementZoneBounds)
        {
            return playerPosition.x >= engagementZoneBounds.min.x - padding &&
                   playerPosition.x <= engagementZoneBounds.max.x + padding &&
                   playerPosition.z >= engagementZoneBounds.min.z - padding &&
                   playerPosition.z <= engagementZoneBounds.max.z + padding;
        }

        return Vector3.Distance(transform.position, playerPosition) <= GetProactiveDetectionRange();
    }

    private static bool AnyPlayerInEngagementZone()
    {
        foreach (var d in all)
        {
            if (d != null && d.playerInsideEngagementZone)
                return true;
        }
        return false;
    }

    private float GetCurrentAttackRange()
    {
        if (alwaysChasePlayer && (playerInsideEngagementZone || playerWithinAwarenessZone))
            return Mathf.Max(
                attackRange,
                GetCurrentMinDistance() + Mathf.Lerp(AggressiveAttackRangePadding, AggressiveAttackRangePadding + 3f, GetPlayerSpeedThreat01()));

        return attackRange;
    }

    private float GetCurrentMinDistance()
    {
        if (alwaysChasePlayer && (playerInsideEngagementZone || playerWithinAwarenessZone))
            return Mathf.Min(minDistance * Mathf.Lerp(AggressiveMinDistanceFactor, 0.35f, GetPlayerSpeedThreat01()), attackRange);

        return minDistance;
    }

    private float GetAggressiveAttackDuration()
    {
        float pressure = Mathf.Max(GetBehaviorPressure(), GetPlayerSpeedThreat01());
        float baseDuration = Mathf.Lerp(4.75f, 7.25f, pressure);
        return IsRaged ? baseDuration + 1.5f : baseDuration;
    }

    private void UpdatePlayerMotionState()
    {
        if (player == null)
            return;

        if (roverPhysics == null)
            roverPhysics = Object.FindFirstObjectByType<RoverPhysicsController>();

        Vector3 currentPosition = player.position;
        if (!hasPlayerPositionSample)
        {
            lastPlayerPosition = currentPosition;
            hasPlayerPositionSample = true;
            return;
        }

        float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 delta = currentPosition - lastPlayerPosition;
        lastPlayerPosition = currentPosition;

        Vector3 measuredVelocity = delta.sqrMagnitude > 900f ? Vector3.zero : delta / deltaTime;
        float measuredSpeed = measuredVelocity.magnitude;

        if (roverPhysics != null && roverPhysics.IsMounted)
        {
            Vector3 roverVelocity = roverPhysics.rb != null
                ? roverPhysics.rb.linearVelocity
                : roverPhysics.transform.forward * roverPhysics.ForwardSpeed;

            if (roverVelocity.magnitude > measuredSpeed)
            {
                measuredVelocity = roverVelocity;
                measuredSpeed = roverVelocity.magnitude;
            }
        }

        float smoothing = 1f - Mathf.Exp(-6f * Time.deltaTime);
        smoothedPlayerVelocity = Vector3.Lerp(smoothedPlayerVelocity, measuredVelocity, smoothing);
        smoothedPlayerSpeed = Mathf.Lerp(smoothedPlayerSpeed, measuredSpeed, smoothing);
    }

    private static float EvaluatePlayerSpeedThreat(float speed)
    {
        return Mathf.InverseLerp(PlayerSpeedThreatStart, PlayerSpeedThreatMax, speed);
    }

    private float GetPlayerSpeedThreat01()
    {
        return EvaluatePlayerSpeedThreat(smoothedPlayerSpeed);
    }

    private float GetBehaviorPressure()
    {
        return Mathf.Max(Aggression, GetPlayerSpeedThreat01() * SpeedPressureWeight);
    }

    private float GetProactiveDetectionRange()
    {
        return detectionRange * Mathf.Lerp(ProactiveDetectionRangeMultiplierMin, ProactiveDetectionRangeMultiplierMax, GetPlayerSpeedThreat01());
    }

    private Vector3 GetPredictedPlayerPosition(float leadTime)
    {
        if (player == null)
            return transform.position;

        Vector3 horizontalVelocity = Vector3.ProjectOnPlane(smoothedPlayerVelocity, Vector3.up);
        float maxLeadDistance = Mathf.Lerp(3f, 12f, GetPlayerSpeedThreat01());
        Vector3 lead = Vector3.ClampMagnitude(horizontalVelocity * leadTime, maxLeadDistance);
        return player.position + lead;
    }

    private void SnapTargetIntoWaypointRouteIfNeeded()
    {
        if (!alwaysChasePlayer || patrol == null || patrol.waypoints == null || patrol.waypoints.Count == 0)
            return;

        if (hasEngagementZoneBounds && engagementZoneBounds.Contains(transform.position))
            return;

        Transform nearestWaypoint = null;
        float nearestDistance = float.MaxValue;

        foreach (Transform waypoint in patrol.waypoints)
        {
            if (waypoint == null)
                continue;

            float distance = Vector3.Distance(transform.position, waypoint.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestWaypoint = waypoint;
            }
        }

        if (nearestWaypoint != null)
            transform.position = nearestWaypoint.position;
    }

    private Transform ResolvePlayerTransform()
    {
        PlayerHealth health = Object.FindFirstObjectByType<PlayerHealth>();
        if (health != null)
        {
            Camera playerCamera = health.GetComponentInChildren<Camera>();
            return playerCamera != null ? playerCamera.transform : health.transform;
        }

        if (Camera.main != null)
            return Camera.main.transform;

        Camera anyCamera = Object.FindFirstObjectByType<Camera>();
        return anyCamera != null ? anyCamera.transform : null;
    }
}
