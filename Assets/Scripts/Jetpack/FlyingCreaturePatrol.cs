using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Makes creatures patrol through the environment using waypoints.
/// Smooth movement with natural bobbing and banking.
/// </summary>
public class FlyingCreaturePatrol : MonoBehaviour
{
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
    
    [Header("Facing Direction")]
    [Tooltip("Rotation offset to make model face forward")]
    public Vector3 forwardRotationOffset = new Vector3(75f, 0f, 0f);
    
    [Header("Idle Behavior")]
    [Tooltip("Pause at each waypoint")]
    public bool pauseAtWaypoints = false;
    public float pauseDuration = 2f;
    
    // Runtime state
    private List<Vector3> waypointPositions = new List<Vector3>();
    private int currentWaypointIndex = 0;
    private int waypointDirection = 1;
    private float bobOffset;
    private float currentBank;
    private float pauseTimer;
    private bool isPaused;
    private Vector3 lastPosition;
    private Vector3 smoothedDirection;
    
    private void Start()
    {
        bobOffset = Random.Range(0f, Mathf.PI * 2f);
        lastPosition = transform.position;
        smoothedDirection = transform.forward;
        
        InitializeWaypoints();
        
        if (waypointPositions.Count == 0)
        {
            Debug.LogWarning($"{gameObject.name}: No waypoints available!");
            enabled = false;
        }
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
        {
            Vector3 randomPoint = center + new Vector3(
                Random.Range(-patrolAreaSize.x / 2f, patrolAreaSize.x / 2f),
                Random.Range(minHeight, maxHeight),
                Random.Range(-patrolAreaSize.z / 2f, patrolAreaSize.z / 2f)
            );
            waypointPositions.Add(randomPoint);
        }
        
        Debug.Log($"{gameObject.name}: Generated {autoWaypointCount} patrol waypoints");
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
        Vector3 movement = smoothedDirection * moveSpeed * Time.deltaTime;
        transform.position += movement;
        
        // Apply bobbing
        ApplyBobbing();
        
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
        Vector3 velocity = (transform.position - lastPosition) / Time.deltaTime;
        Vector3 cross = Vector3.Cross(transform.forward, targetDirection);
        float turnAmount = cross.y;
        
        float targetBank = -turnAmount * maxBankAngle * 10f;
        targetBank = Mathf.Clamp(targetBank, -maxBankAngle, maxBankAngle);
        currentBank = Mathf.Lerp(currentBank, targetBank, bankSmoothing * Time.deltaTime);
        
        // Apply rotation with offset and banking
        Quaternion offsetRotation = Quaternion.Euler(forwardRotationOffset);
        Quaternion bankRotation = Quaternion.Euler(0f, 0f, currentBank);
        
        transform.rotation = targetRotation * bankRotation * offsetRotation;
    }
    
    private void MoveToNextWaypoint()
    {
        if (loopWaypoints)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypointPositions.Count;
        }
        else
        {
            currentWaypointIndex += waypointDirection;
            
            if (currentWaypointIndex >= waypointPositions.Count)
            {
                currentWaypointIndex = waypointPositions.Count - 2;
                waypointDirection = -1;
            }
            else if (currentWaypointIndex < 0)
            {
                currentWaypointIndex = 1;
                waypointDirection = 1;
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
    
    private void OnDrawGizmosSelected()
    {
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
