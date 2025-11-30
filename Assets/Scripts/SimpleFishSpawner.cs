using UnityEngine;

/// <summary>
/// Simple fish spawner - spawns fish with SimpleFishMovement.
/// Replace the complex Flock system with this.
/// </summary>
public class SimpleFishSpawner : MonoBehaviour
{
    [Header("Spawning")]
    public GameObject fishPrefab;
    public int fishCount = 15;
    
    [Header("Spawn Area")]
    public Vector3 spawnCenter = new Vector3(0, -10, 0);
    public Vector3 spawnSize = new Vector3(30, 10, 30);
    
    [Header("Fish Settings")]
    public float minSpeed = 2f;
    public float maxSpeed = 4f;
    public float minY = -18f;
    public float maxY = -2f;
    public float boundaryRadius = 40f;
    
    private GameObject[] spawnedFish;
    
    void Start()
    {
        SpawnFish();
    }
    
    void SpawnFish()
    {
        spawnedFish = new GameObject[fishCount];
        
        for (int i = 0; i < fishCount; i++)
        {
            // Random position within spawn area
            Vector3 pos = spawnCenter + new Vector3(
                Random.Range(-spawnSize.x / 2, spawnSize.x / 2),
                Random.Range(-spawnSize.y / 2, spawnSize.y / 2),
                Random.Range(-spawnSize.z / 2, spawnSize.z / 2)
            );
            
            // Random rotation
            Quaternion rot = Quaternion.Euler(0, Random.Range(0, 360), 0);
            
            // Spawn fish
            GameObject fish = Instantiate(fishPrefab, pos, rot);
            fish.name = "Fish_" + i;
            
            // Setup components
            Rigidbody rb = fish.GetComponent<Rigidbody>();
            if (rb == null) rb = fish.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.linearDamping = 1f;
            
            // Add collider if missing
            Collider col = fish.GetComponent<Collider>();
            if (col == null)
            {
                SphereCollider sphere = fish.AddComponent<SphereCollider>();
                sphere.radius = 0.5f;
                sphere.isTrigger = false; // Physical collisions!
            }
            else
            {
                // Make sure it's not a trigger
                col.isTrigger = false;
            }
            
            // Add or configure SimpleFishMovement
            SimpleFishMovement movement = fish.GetComponent<SimpleFishMovement>();
            if (movement == null) movement = fish.AddComponent<SimpleFishMovement>();
            
            movement.swimSpeed = Random.Range(minSpeed, maxSpeed);
            movement.minY = minY;
            movement.maxY = maxY;
            movement.boundaryRadius = boundaryRadius;
            movement.centerPoint = spawnCenter;
            movement.obstacleLayer = 1 << 11; // Obstacles layer
            
            // Remove old FlockUnit if present
            FlockUnit oldUnit = fish.GetComponent<FlockUnit>();
            if (oldUnit != null) Destroy(oldUnit);
            
            spawnedFish[i] = fish;
        }
        
        Debug.Log($"SimpleFishSpawner: Spawned {fishCount} fish");
    }
}
