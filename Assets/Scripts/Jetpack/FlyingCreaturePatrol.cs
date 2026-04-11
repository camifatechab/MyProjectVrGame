using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Makes creatures patrol through the environment using waypoints.
/// Smooth movement with natural bobbing and banking.
/// </summary>
public class FlyingCreaturePatrol : MonoBehaviour
{
    private const string AttackAreaObjectName = "Attack_Area";
    private const float CollisionPadding = 0.05f;
    private const float TerrainClearance = 2f;
    private const int MaxWaypointSearchAttempts = 12;

    [Header("Patrol Settings")]
    [Tooltip("Waypoints to patrol between. If empty, generates random waypoints.")]
    public List<Transform> waypoints = new List<Transform>();
    
    [Tooltip("Speed of flight")]
    public float moveSpeed = 8f;
    
    [Tooltip("How quickly creature turns toward next waypoint")]
    public float turnSpeed = 2f;
    
    [Tooltip("Distance to waypoint before moving to next")]
    public float waypointReachDistance = 3f;
    
    [Tooltip("Loop back to first waypoint, or reverse direction")]
    public bool loopWaypoints = true;
    
    [Header("Auto-Generate Waypoints")]
    [Tooltip("If no waypoints assigned, generate this many random ones")]
    public int autoWaypointCount = 5;
    
    [Tooltip("Area bounds for auto-generated waypoints")]
    public Vector3 patrolAreaCenter = Vector3.zero;
    public Vector3 patrolAreaSize = new Vector3(60f, 30f, 60f);
    
    [Tooltip("Minimum Y height for waypoints")]
    public float minHeight = 10f;
    
    [Tooltip("Maximum Y height for waypoints")]
    public float maxHeight = 60f;
    
    [Header("Natural Movement")]
    [Tooltip("Vertical bobbing amplitude")]
    public float bobAmplitude = 1f;
    
    [Tooltip("Bobbing speed")]
    public float bobSpeed = 1.5f;
    
    [Tooltip("Banking angle when turning")]
    public float maxBankAngle = 20f;
    
    [Tooltip("How quickly banking responds")]
    public float bankSmoothing = 3f;
    
    [Tooltip("Extra pitch angle when diving down")]
    public float divePitchAngle = 30f;
    
    [Tooltip("Speed multiplier when diving down")]
    public float diveSpeedMultiplier = 1.5f;
    
    [Tooltip("Rotation offset when diving deep")]
    public Vector3 diveRotationOffset = new Vector3(20.66f, 260.72f, 160.75f);



    
    [Header("Facing Direction")]
    [Tooltip("Rotation offset to make model face forward")]
    public Vector3 forwardRotationOffset = new Vector3(75f, 0f, 0f);
    
    [Header("Debug Gizmos")]
    public bool showGizmos = false;

    
[Header("Cave Bounds Clamp")]
    [Tooltip("If true, clamps position every frame to boundsMin/boundsMax (world space).")]
    public bool useBoundsClamp = false;
    public Vector3 boundsMin;
    public Vector3 boundsMax;

    
[Header("Idle Behavior")]
    [Tooltip("Pause at each waypoint")]
    public bool pauseAtWaypoints = false;
    public float pauseDuration = 2f;
    
    // Runtime state
    private List<Vector3> waypointPositions = new List<Vector3>();
    private int currentWaypointIndex = 0;
    
    /// <summary>
    /// Public accessor for current waypoint index (for SkyboxTransitionController)
    /// </summary>
    public int CurrentWaypointIndex => currentWaypointIndex;

    private int waypointDirection = 1;
    private float bobOffset;
    private float currentBank;
    private float pauseTimer;
    private bool isPaused;
    private bool useDynamicAreaPatrol;
    private Collider cachedCollider;
    private Vector3 lastPosition;
    private Vector3 smoothedDirection;
    
    private void Start()
    {
        cachedCollider = GetComponent<Collider>();
        bobOffset = Random.Range(0f, Mathf.PI * 2f);
        lastPosition = transform.position;
        smoothedDirection = transform.forward;

        ConfigureAttackAreaPatrol();
        InitializeWaypoints();

        if (waypointPositions.Count == 0)
        {
            Debug.LogWarning($"{gameObject.name}: No waypoints available!");
            enabled = false;
        }
    }

    private void ConfigureAttackAreaPatrol()
    {
        if (!gameObject.name.StartsWith("Target_") || waypoints.Count > 0)
            return;

        GameObject attackArea = GameObject.Find(AttackAreaObjectName);
        if (attackArea == null)
            return;

        BoxCollider attackBounds = attackArea.GetComponent<BoxCollider>();
        if (attackBounds == null)
            return;

        Bounds bounds = attackBounds.bounds;
        patrolAreaCenter = bounds.center;
        patrolAreaSize = bounds.size;
        minHeight = bounds.min.y;
        maxHeight = bounds.max.y;
        useBoundsClamp = true;
        boundsMin = bounds.min;
        boundsMax = bounds.max;
        useDynamicAreaPatrol = true;
        autoWaypointCount = 1;
    }
    
    private void InitializeWaypoints()
    {
        waypointPositions.Clear();
        
        // Use assigned waypoints if available
        if (waypoints.Count > 0)
        {
            foreach (var wp in waypoints)
            {
                if (wp != null)
                    waypointPositions.Add(wp.position);
            }
        }
        
        // Auto-generate if none assigned
        if (waypointPositions.Count == 0)
        {
            GenerateRandomWaypoints();
        }
        
        // Find nearest waypoint to start
        if (waypointPositions.Count > 0)
        {
            float nearestDist = float.MaxValue;
            for (int i = 0; i < waypointPositions.Count; i++)
            {
                float dist = Vector3.Distance(transform.position, waypointPositions[i]);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    currentWaypointIndex = i;
                }
            }
        }
    }
    
    private void GenerateRandomWaypoints()
    {
        Vector3 center = patrolAreaCenter;
        if (center == Vector3.zero)
            center = transform.position;
        
        for (int i = 0; i < autoWaypointCount; i++)
            waypointPositions.Add(GenerateRandomWaypointPosition(center));
        
        Debug.Log($"{gameObject.name}: Generated {autoWaypointCount} patrol waypoints");
    }

    private Vector3 GenerateRandomWaypointPosition(Vector3 center)
    {
        for (int attempt = 0; attempt < MaxWaypointSearchAttempts; attempt++)
        {
            Vector3 randomPoint = center + new Vector3(
                Random.Range(-patrolAreaSize.x / 2f, patrolAreaSize.x / 2f),
                0f,
                Random.Range(-patrolAreaSize.z / 2f, patrolAreaSize.z / 2f)
            );

            randomPoint.y = Random.Range(minHeight, maxHeight);
            randomPoint = RaiseAboveTerrain(randomPoint);

            if (IsPointClear(randomPoint))
                return randomPoint;
        }

        Vector3 fallback = transform.position;
        if (fallback == Vector3.zero)
            fallback = center;

        return RaiseAboveTerrain(ClampToBounds(fallback));
    }

    private Vector3 RaiseAboveTerrain(Vector3 point)
    {
        float requiredClearance = GetRequiredTerrainClearance();
        float probeTop = useBoundsClamp ? boundsMax.y + requiredClearance : maxHeight + requiredClearance;
        float probeDistance = Mathf.Max(1f, probeTop - minHeight + requiredClearance);

        if (Physics.Raycast(new Vector3(point.x, probeTop, point.z), Vector3.down, out RaycastHit hit, probeDistance, ~0, QueryTriggerInteraction.Ignore))
            point.y = Mathf.Max(point.y, hit.point.y + requiredClearance);

        if (useBoundsClamp)
            point = ClampToBounds(point);

        return point;
    }

    private bool IsPointClear(Vector3 point)
    {
        float radius = GetCollisionProbeRadius();
        Collider[] overlaps = Physics.OverlapSphere(point, radius, ~0, QueryTriggerInteraction.Ignore);

        foreach (Collider overlap in overlaps)
        {
            if (overlap == null)
                continue;

            if (cachedCollider != null && overlap == cachedCollider)
                continue;

            if (overlap.transform == transform || overlap.transform.IsChildOf(transform))
                continue;

            if (overlap.gameObject.name == AttackAreaObjectName)
                continue;

            return false;
        }

        return true;
    }

    private float GetCollisionProbeRadius()
    {
        if (cachedCollider == null)
            return 1f;

        if (cachedCollider is CapsuleCollider capsule)
        {
            Vector3 lossyScale = transform.lossyScale;
            float maxScale = Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.z));
            return Mathf.Max(capsule.radius * maxScale, 0.5f);
        }

        if (cachedCollider is SphereCollider sphere)
        {
            float maxScale = Mathf.Max(
                Mathf.Abs(transform.lossyScale.x),
                Mathf.Abs(transform.lossyScale.y),
                Mathf.Abs(transform.lossyScale.z));
            return Mathf.Max(sphere.radius * maxScale, 0.5f);
        }

        if (cachedCollider is BoxCollider box)
        {
            Vector3 scaledSize = Vector3.Scale(box.size, transform.lossyScale);
            return Mathf.Max(Mathf.Min(Mathf.Abs(scaledSize.x), Mathf.Abs(scaledSize.y), Mathf.Abs(scaledSize.z)) * 0.5f, 0.5f);
        }

        Bounds bounds = cachedCollider.bounds;
        return Mathf.Max(Mathf.Min(bounds.extents.x, bounds.extents.y, bounds.extents.z), 0.5f);
    }

    private float GetRequiredTerrainClearance()
    {
        return Mathf.Max(TerrainClearance, GetCollisionProbeRadius() + CollisionPadding);
    }

    private Vector3 ClampToBounds(Vector3 position)
    {
        if (!useBoundsClamp)
            return position;

        position.x = Mathf.Clamp(position.x, boundsMin.x, boundsMax.x);
        position.y = Mathf.Clamp(position.y, boundsMin.y, boundsMax.y);
        position.z = Mathf.Clamp(position.z, boundsMin.z, boundsMax.z);
        return position;
    }
    
    private void Update()
    {
        if (waypointPositions.Count == 0) return;
        
        // Handle pause at waypoints
        if (isPaused)
        {
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f)
            {
                isPaused = false;
                MoveToNextWaypoint();
            }
            ApplyBobbing();
            return;
        }
        
        Vector3 targetPosition = waypointPositions[currentWaypointIndex];
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        
        // Smooth direction for natural turning
        smoothedDirection = Vector3.Lerp(smoothedDirection, directionToTarget, turnSpeed * Time.deltaTime);
        
        // Move toward waypoint
        // Calculate speed - faster when diving
        float currentSpeed = moveSpeed;
        if (smoothedDirection.y < 0)
        {
            currentSpeed = moveSpeed * (1f + Mathf.Abs(smoothedDirection.y) * (diveSpeedMultiplier - 1f));
        }
        
        // Move toward waypoint
        Vector3 movement = smoothedDirection * currentSpeed * Time.deltaTime;
        movement = AdjustMovementForObstacles(movement);
        transform.position += movement;
        
        // Apply bobbing
        ApplyBobbing();
        ResolveCollisions();
        transform.position = RaiseAboveTerrain(transform.position);
        
        // Apply rotation with banking
        ApplyRotationAndBanking(directionToTarget);
        
        // Check if reached waypoint
        float distanceToWaypoint = Vector3.Distance(transform.position, targetPosition);
        if (distanceToWaypoint < waypointReachDistance)
        {
            if (pauseAtWaypoints)
            {
                isPaused = true;
                pauseTimer = pauseDuration;
            }
            else
            {
                MoveToNextWaypoint();
            }
        }
        
        lastPosition = transform.position;
    }
    
    private void ApplyBobbing()
    {
        float bob = Mathf.Sin((Time.time * bobSpeed) + bobOffset) * bobAmplitude;
        Vector3 pos = transform.position;
        pos.y += bob * Time.deltaTime;
        transform.position = pos;
    }
    
private void ApplyRotationAndBanking(Vector3 targetDirection)
    {
        if (targetDirection.sqrMagnitude < 0.001f) return;
        
        // Calculate desired facing rotation
        Quaternion targetRotation = Quaternion.LookRotation(smoothedDirection, Vector3.up);
        
        // Calculate banking based on turning
        Vector3 cross = Vector3.Cross(transform.forward, targetDirection);
        float turnAmount = cross.y;
        
        float targetBank = -turnAmount * maxBankAngle * 10f;
        targetBank = Mathf.Clamp(targetBank, -maxBankAngle, maxBankAngle);
        currentBank = Mathf.Lerp(currentBank, targetBank, bankSmoothing * Time.deltaTime);
        
        // Calculate dive amount (0 = level, 1 = steep dive)
        float diveAmount = Mathf.Clamp01(-smoothedDirection.y);
        
        // Normal flight rotation
        Quaternion offsetRotation = Quaternion.Euler(forwardRotationOffset);
        Quaternion bankRotation = Quaternion.Euler(0f, 0f, currentBank);
        Quaternion normalRotation = targetRotation * bankRotation * offsetRotation;
        
        // Dive rotation - use exact rotation you specified
        Quaternion diveRotation = Quaternion.Euler(diveRotationOffset);
        
        // Blend between normal and dive rotation
        transform.rotation = Quaternion.Slerp(normalRotation, diveRotation, diveAmount);
    }
    
    private void MoveToNextWaypoint()
    {
        if (useDynamicAreaPatrol && waypointPositions.Count > 0)
        {
            Vector3 center = patrolAreaCenter != Vector3.zero ? patrolAreaCenter : transform.position;
            waypointPositions[currentWaypointIndex] = GenerateRandomWaypointPosition(center);
            return;
        }

        if (loopWaypoints)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypointPositions.Count;
        }
        else
        {
            currentWaypointIndex++;
            
            // Stop at last waypoint
            if (currentWaypointIndex >= waypointPositions.Count)
            {
                currentWaypointIndex = waypointPositions.Count - 1;
                enabled = false;
                Debug.Log("Flight complete - creature stopped at final waypoint");
            }
        }
    }
    
    /// <summary>
    /// Regenerate waypoints at runtime
    /// </summary>
    public void RegenerateWaypoints()
    {
        waypoints.Clear();
        waypointPositions.Clear();
        GenerateRandomWaypoints();
        currentWaypointIndex = 0;
    }

    private void ResolveCollisions()
    {
        if (cachedCollider == null)
            return;

        float probeRadius = GetCollisionProbeRadius();
        Collider[] overlaps = Physics.OverlapSphere(transform.position, probeRadius, ~0, QueryTriggerInteraction.Ignore);

        foreach (Collider overlap in overlaps)
        {
            if (overlap == null)
                continue;

            if (overlap == cachedCollider)
                continue;

            if (overlap.transform == transform || overlap.transform.IsChildOf(transform))
                continue;

            if (overlap.gameObject.name == AttackAreaObjectName)
                continue;

            if (Physics.ComputePenetration(
                    cachedCollider, transform.position, transform.rotation,
                    overlap, overlap.transform.position, overlap.transform.rotation,
                    out Vector3 pushDirection, out float pushDistance))
            {
                transform.position += pushDirection * (pushDistance + CollisionPadding);
            }
        }

        transform.position = ClampToBounds(transform.position);
    }

    private Vector3 AdjustMovementForObstacles(Vector3 movement)
    {
        if (cachedCollider == null || movement.sqrMagnitude <= 0.0001f)
            return movement;

        float movementDistance = movement.magnitude;
        float probeRadius = GetCollisionProbeRadius();
        Vector3 origin = cachedCollider.bounds.center;
        Vector3 direction = movement / movementDistance;

        if (!Physics.SphereCast(origin, probeRadius, direction, out RaycastHit hit, movementDistance + CollisionPadding, ~0, QueryTriggerInteraction.Ignore))
            return movement;

        if (hit.collider == null || hit.collider == cachedCollider)
            return movement;

        if (hit.transform == transform || hit.transform.IsChildOf(transform))
            return movement;

        if (hit.collider.gameObject.name == AttackAreaObjectName)
            return movement;

        float allowedDistance = Mathf.Max(0f, hit.distance - CollisionPadding);
        if (allowedDistance <= 0.01f)
            MoveToNextWaypoint();

        return direction * allowedDistance;
    }
     
    private void LateUpdate()
    {
        if (!useBoundsClamp) return;
        transform.position = ClampToBounds(transform.position);
    }

    private void OnValidate()
    {
        cachedCollider = GetComponent<Collider>();
    }

    
private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        
        // Draw patrol area
        Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
        Vector3 center = patrolAreaCenter != Vector3.zero ? patrolAreaCenter : transform.position;
        Gizmos.DrawWireCube(center, patrolAreaSize);
        
        // Draw waypoints
        var points = Application.isPlaying ? waypointPositions : new List<Vector3>();
        
        // In editor, show assigned waypoints
        if (!Application.isPlaying && waypoints.Count > 0)
        {
            foreach (var wp in waypoints)
            {
                if (wp != null) points.Add(wp.position);
            }
        }
        
        for (int i = 0; i < points.Count; i++)
        {
            bool isCurrent = Application.isPlaying && i == currentWaypointIndex;
            Gizmos.color = isCurrent ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(points[i], isCurrent ? 1.5f : 1f);
            
            // Draw path lines
            if (i > 0)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(points[i - 1], points[i]);
            }
        }
        
        // Draw loop connection
        if (loopWaypoints && points.Count > 1)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
            Gizmos.DrawLine(points[points.Count - 1], points[0]);
        }
    }
}
