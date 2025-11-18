using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FlockUnit : MonoBehaviour
{
    [SerializeField] private float FOVAngle = 90f;
    [SerializeField] private float smoothDamp = 0.5f;
    [SerializeField] private LayerMask obstacleMask = 1;
    [SerializeField] private Vector3[] directionsToCheckWhenAvoidingObstacles = {
        Vector3.forward, Vector3.back, Vector3.left, Vector3.right,
        Vector3.up, Vector3.down, new Vector3(1,1,0), new Vector3(-1,1,0),
        new Vector3(1,-1,0), new Vector3(-1,-1,0)
    };

    private List<FlockUnit> cohesionNeighbours = new List<FlockUnit>();
    private List<FlockUnit> avoidanceNeighbours = new List<FlockUnit>();
    private List<FlockUnit> alignmentNeighbours = new List<FlockUnit>();
    private Flock assignedFlock;
    private Vector3 currentVelocity;
    private Vector3 currentObstacleAvoidanceVector;
    private Rigidbody myRigidbody;
    private Transform playerTransform;


    public Transform myTransform { get; set; }

private void Awake()
    {
        myTransform = transform;
        myRigidbody = GetComponent<Rigidbody>();
        
        // Ensure gravity is disabled for underwater fish
        if (myRigidbody != null)
        {
            myRigidbody.useGravity = false;
            myRigidbody.linearDamping = 1f;
        }
        
        // Find player
        GameObject player = GameObject.Find("XR Origin (XR Rig)");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    public void AssignFlock(Flock flock)
    {
        assignedFlock = flock;
    }

    public void InitializeSpeed(float speed)
    {
        if (myRigidbody != null)
        {
            myRigidbody.linearVelocity = myTransform.forward * speed;
        }
    }

public void MoveUnit()
    {
        if (assignedFlock == null) return;
        
        FindNeighbours();
        CalculateSpeed();

        var cohesionVector = CalculateCohesionVector() * assignedFlock.cohesionWeight;
        var avoidanceVector = CalculateAvoidanceVector() * assignedFlock.avoidanceWeight;
        var alignmentVector = CalculateAlignmentVector() * assignedFlock.alignmentWeight;
        var boundsVector = CalculateBoundsVector() * assignedFlock.boundsWeight;
        var obstacleVector = CalculateObstacleVector() * assignedFlock.obstacleWeight;
        var playerAvoidanceVector = CalculatePlayerAvoidanceVector() * 5f;
        var depthBias = CalculateDepthBias() * 2f; // Keep fish at middle/bottom

        var moveVector = cohesionVector + avoidanceVector + alignmentVector + boundsVector + obstacleVector + playerAvoidanceVector + depthBias;
        
        // Reduce vertical movement component to keep fish more horizontal
        moveVector.y *= 0.3f;
        
        moveVector = Vector3.SmoothDamp(myTransform.forward, moveVector, ref currentVelocity, smoothDamp);
        moveVector = moveVector.normalized * assignedFlock.speed;
        if (moveVector == Vector3.zero)
            moveVector = myTransform.forward;

        if (myRigidbody != null)
        {
            myRigidbody.linearVelocity = moveVector;
            myTransform.forward = moveVector.normalized;
        }
    }

    private void FindNeighbours()
    {
        cohesionNeighbours.Clear();
        avoidanceNeighbours.Clear();
        alignmentNeighbours.Clear();
        var allUnits = assignedFlock.allUnits;
        for (int i = 0; i < allUnits.Length; i++)
        {
            var currentUnit = allUnits[i];
            if (currentUnit != null && currentUnit != this.gameObject)
            {
                float currentNeighbourDistanceSqr = Vector3.SqrMagnitude(currentUnit.transform.position - myTransform.position);
                if (currentNeighbourDistanceSqr <= assignedFlock.cohesionDistance * assignedFlock.cohesionDistance)
                {
                    FlockUnit unit = currentUnit.GetComponent<FlockUnit>();
                    if (unit != null) cohesionNeighbours.Add(unit);
                }
                if (currentNeighbourDistanceSqr <= assignedFlock.avoidanceDistance * assignedFlock.avoidanceDistance)
                {
                    FlockUnit unit = currentUnit.GetComponent<FlockUnit>();
                    if (unit != null) avoidanceNeighbours.Add(unit);
                }
                if (currentNeighbourDistanceSqr <= assignedFlock.alignmentDistance * assignedFlock.alignmentDistance)
                {
                    FlockUnit unit = currentUnit.GetComponent<FlockUnit>();
                    if (unit != null) alignmentNeighbours.Add(unit);
                }
            }
        }
    }

    private void CalculateSpeed()
    {
        if (cohesionNeighbours.Count == 0)
            return;
        assignedFlock.speed = 0;
        for (int i = 0; i < cohesionNeighbours.Count; i++)
        {
            if (cohesionNeighbours[i] != null && cohesionNeighbours[i].myRigidbody != null)
            {
                assignedFlock.speed += cohesionNeighbours[i].myRigidbody.linearVelocity.magnitude;
            }
        }
        assignedFlock.speed /= cohesionNeighbours.Count;
        assignedFlock.speed = Mathf.Clamp(assignedFlock.speed, assignedFlock.minSpeed, assignedFlock.maxSpeed);
    }

    private Vector3 CalculateCohesionVector()
    {
        var cohesionVector = Vector3.zero;
        if (cohesionNeighbours.Count == 0)
            return cohesionVector;
        int neighboursInFOV = 0;
        for (int i = 0; i < cohesionNeighbours.Count; i++)
        {
            if (cohesionNeighbours[i] != null && IsInFOV(cohesionNeighbours[i].myTransform.position))
            {
                neighboursInFOV++;
                cohesionVector += cohesionNeighbours[i].myTransform.position;
            }
        }
        if (neighboursInFOV == 0) return Vector3.zero;
        cohesionVector /= neighboursInFOV;
        cohesionVector -= myTransform.position;
        cohesionVector = cohesionVector.normalized;
        return cohesionVector;
    }

    private Vector3 CalculateAvoidanceVector()
    {
        var avoidanceVector = Vector3.zero;
        if (avoidanceNeighbours.Count == 0)
            return avoidanceVector;

        int neighboursInFOV = 0;
        for (int i = 0; i < avoidanceNeighbours.Count; i++)
        {
            if (avoidanceNeighbours[i] != null && IsInFOV(avoidanceNeighbours[i].myTransform.position))
            {
                neighboursInFOV++;
                avoidanceVector += (myTransform.position - avoidanceNeighbours[i].myTransform.position);
            }
        }
        if (neighboursInFOV == 0) return Vector3.zero;
        avoidanceVector /= neighboursInFOV;
        avoidanceVector = avoidanceVector.normalized;
        return avoidanceVector;
    }

    private Vector3 CalculateAlignmentVector()
    {
        var alignmentVector = Vector3.zero;
        if (alignmentNeighbours.Count == 0)
            return myTransform.forward;

        int neighboursInFOV = 0;
        for (int i = 0; i < alignmentNeighbours.Count; i++)
        {
            if (alignmentNeighbours[i] != null && IsInFOV(alignmentNeighbours[i].myTransform.position))
            {
                neighboursInFOV++;
                alignmentVector += alignmentNeighbours[i].myTransform.forward;
            }
        }
        if (neighboursInFOV == 0) return myTransform.forward;
        alignmentVector /= neighboursInFOV;
        alignmentVector = alignmentVector.normalized;
        return alignmentVector;
    }

private Vector3 CalculateBoundsVector()
    {
        // Water container bounds: X=±25, Z=±25, Y from -20 to 0
        Vector3 boundsVector = Vector3.zero;
        Vector3 pos = myTransform.position;
        
        // Check horizontal boundaries (X and Z)
        if (Mathf.Abs(pos.x) > 20f)
        {
            boundsVector.x = -Mathf.Sign(pos.x); // Push away from walls
        }
        if (Mathf.Abs(pos.z) > 20f)
        {
            boundsVector.z = -Mathf.Sign(pos.z); // Push away from walls
        }
        
        // Check vertical boundaries (Y)
        if (pos.y > -2f) // Too close to surface
        {
            boundsVector.y = -1f; // Push down
        }
        else if (pos.y < -18f) // Too close to floor
        {
            boundsVector.y = 1f; // Push up
        }
        
        return boundsVector.normalized;
    }

    private Vector3 CalculateObstacleVector()
    {
        var obstacleVector = Vector3.zero;
        RaycastHit hit;
        if (Physics.Raycast(myTransform.position, myTransform.forward, out hit, assignedFlock.obstacleDistance, obstacleMask))
        {
            obstacleVector = FindBestDirectionToAvoidObstacle();
        }
        else
        {
            currentObstacleAvoidanceVector = Vector3.zero;
        }
        return obstacleVector;
    }

    private Vector3 FindBestDirectionToAvoidObstacle()
    {
        if (currentObstacleAvoidanceVector != Vector3.zero)
        {
            RaycastHit hit;
            if (!Physics.Raycast(myTransform.position, myTransform.forward, out hit, assignedFlock.obstacleDistance, obstacleMask))
            {
                return currentObstacleAvoidanceVector;
            }
        }

        float maxDistance = int.MinValue;
        var selectedDirection = Vector3.zero;
        for (int i = 0; i < directionsToCheckWhenAvoidingObstacles.Length; i++)
        {
            RaycastHit hit;
            var currentDirection = myTransform.TransformDirection(directionsToCheckWhenAvoidingObstacles[i].normalized);
            if (Physics.Raycast(myTransform.position, currentDirection, out hit, assignedFlock.obstacleDistance, obstacleMask))
            {
                float currentDistance = (hit.point - myTransform.position).magnitude;
                if (currentDistance > maxDistance)
                {
                    maxDistance = currentDistance;
                    selectedDirection = currentDirection;
                }
            }
            else
            {
                selectedDirection = currentDirection;
                currentObstacleAvoidanceVector = currentDirection.normalized;
                return selectedDirection.normalized;
            }
        }
        currentObstacleAvoidanceVector = selectedDirection.normalized;
        return selectedDirection.normalized;
    }

    private bool IsInFOV(Vector3 position)
    {
        return Vector3.Angle(myTransform.forward, position - myTransform.position) <= FOVAngle;
    }


private Vector3 CalculatePlayerAvoidanceVector()
    {
        if (playerTransform == null) return Vector3.zero;
        
        float distanceToPlayer = Vector3.Distance(myTransform.position, playerTransform.position);
        float avoidanceRadius = 5f; // Start avoiding when player is within 5 units
        
        if (distanceToPlayer < avoidanceRadius)
        {
            // Swim away from player
            Vector3 awayFromPlayer = myTransform.position - playerTransform.position;
            // Add some randomness to make it look more natural
            awayFromPlayer += new Vector3(
                Random.Range(-0.5f, 0.5f),
                Random.Range(-0.3f, 0.3f),
                Random.Range(-0.5f, 0.5f)
            );
            return awayFromPlayer.normalized;
        }
        
        return Vector3.zero;
    }


private Vector3 CalculateDepthBias()
    {
        Vector3 depthBias = Vector3.zero;
        float currentY = myTransform.position.y;
        
        // Prefer swimming at middle depth (around y=-10)
        float targetDepth = -10f;
        float depthDifference = currentY - targetDepth;
        
        // If too high (above -6), push down strongly
        if (currentY > -6f)
        {
            depthBias.y = -2f;
        }
        // If moderately high (between -6 and -8), push down
        else if (currentY > -8f)
        {
            depthBias.y = -1f;
        }
        // If at good depth (-8 to -12), slight downward bias
        else if (currentY > -12f)
        {
            depthBias.y = -0.3f;
        }
        // If too deep (below -15), push up slightly
        else if (currentY < -15f)
        {
            depthBias.y = 0.5f;
        }
        
        return depthBias;
    }
}