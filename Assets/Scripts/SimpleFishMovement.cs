using UnityEngine;

/// <summary>
/// Simple fish movement that uses physics collisions to avoid obstacles.
/// Attach to each fish prefab. Much more reliable than raycasting.
/// </summary>
public class SimpleFishMovement : MonoBehaviour
{
    [Header("Movement")]
    public float swimSpeed = 3f;
    public float turnSpeed = 2f;
    public float randomTurnInterval = 2f;
    
    [Header("Boundaries")]
    public float minY = -18f;
    public float maxY = -2f;
    public float boundaryRadius = 20f;
    public Vector3 centerPoint = Vector3.zero;
    
    [Header("Obstacle Avoidance")]
    public float avoidanceDistance = 5f;
    public LayerMask obstacleLayer = 2048; // Layer 11
    
    [Header("Flashlight Reaction")]
    public float flashlightFleeDistance = 8f;
    public float flashlightFleeSpeed = 6f;
    
    private Rigidbody rb;
    private Vector3 currentDirection;
    private float nextRandomTurn;
    private Transform flashlight;
    private Light flashlightLight;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        rb.useGravity = false;
        rb.linearDamping = 1f;
        rb.angularDamping = 2f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
        // Random initial direction
        currentDirection = Random.onUnitSphere;
        currentDirection.y *= 0.3f; // Reduce vertical movement
        currentDirection.Normalize();
        
        nextRandomTurn = Time.time + Random.Range(1f, randomTurnInterval);
        
        // Find flashlight
        GameObject flashlightObj = GameObject.Find("Spotlight");
        if (flashlightObj != null)
        {
            flashlight = flashlightObj.transform;
            flashlightLight = flashlightObj.GetComponent<Light>();
        }
    }
    
    void FixedUpdate()
    {
        Vector3 desiredDirection = currentDirection;
        float currentSpeed = swimSpeed;
        
        // 1. Random direction changes
        if (Time.time > nextRandomTurn)
        {
            Vector3 randomTurn = Random.onUnitSphere * 0.5f;
            randomTurn.y *= 0.2f;
            desiredDirection += randomTurn;
            nextRandomTurn = Time.time + Random.Range(1f, randomTurnInterval);
        }
        
        // 2. Boundary avoidance - stay in the swimming area
        Vector3 toCenter = centerPoint - transform.position;
        toCenter.y = 0;
        float distFromCenter = toCenter.magnitude;
        
        if (distFromCenter > boundaryRadius)
        {
            desiredDirection += toCenter.normalized * 2f;
        }
        
        // Vertical boundaries
        if (transform.position.y > maxY)
        {
            desiredDirection.y -= 2f;
        }
        else if (transform.position.y < minY)
        {
            desiredDirection.y += 2f;
        }
        
        // 3. Obstacle avoidance using raycasts in multiple directions
        desiredDirection += GetObstacleAvoidance() * 3f;
        
        // 4. Flashlight avoidance
        if (flashlight != null && flashlightLight != null && flashlightLight.enabled)
        {
            Vector3 toFlashlight = flashlight.position - transform.position;
            float distToFlashlight = toFlashlight.magnitude;
            
            if (distToFlashlight < flashlightFleeDistance)
            {
                // Flee from flashlight
                desiredDirection -= toFlashlight.normalized * 3f;
                currentSpeed = flashlightFleeSpeed;
            }
        }
        
        // Normalize and reduce vertical movement
        desiredDirection.y *= 0.3f;
        desiredDirection.Normalize();
        
        // Smoothly turn towards desired direction
        currentDirection = Vector3.Lerp(currentDirection, desiredDirection, turnSpeed * Time.fixedDeltaTime);
        currentDirection.Normalize();
        
        // Apply movement
        rb.linearVelocity = currentDirection * currentSpeed;
        
        // Face movement direction
        if (currentDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(currentDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);
        }
    }
    
    Vector3 GetObstacleAvoidance()
    {
        Vector3 avoidance = Vector3.zero;
        
        // Cast rays in multiple directions
        Vector3[] directions = {
            transform.forward,
            transform.forward + transform.right * 0.5f,
            transform.forward - transform.right * 0.5f,
            transform.forward + transform.up * 0.3f,
            transform.forward - transform.up * 0.3f
        };
        
        foreach (Vector3 dir in directions)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, dir.normalized, out hit, avoidanceDistance, obstacleLayer))
            {
                // Push away from obstacle
                float strength = 1f - (hit.distance / avoidanceDistance);
                avoidance -= dir.normalized * strength;
                avoidance += hit.normal * strength;
            }
        }
        
        return avoidance;
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Bounce off obstacles
        if (collision.contacts.Length > 0)
        {
            Vector3 normal = collision.contacts[0].normal;
            currentDirection = Vector3.Reflect(currentDirection, normal);
            currentDirection.y *= 0.3f;
            currentDirection.Normalize();
        }
    }
    
    void OnCollisionStay(Collision collision)
    {
        // Keep pushing away from obstacle
        if (collision.contacts.Length > 0)
        {
            Vector3 normal = collision.contacts[0].normal;
            currentDirection = Vector3.Lerp(currentDirection, normal, 0.5f);
            currentDirection.Normalize();
        }
    }
}
