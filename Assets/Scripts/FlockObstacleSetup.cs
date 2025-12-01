using UnityEngine;

/// <summary>
/// Attach to the Flock object. Ensures all spawned fish detect obstacles on the correct layer.
/// </summary>
public class FlockObstacleSetup : MonoBehaviour
{
    [Header("Obstacle Detection")]
    [Tooltip("Layer mask for obstacles (set to Obstacles layer)")]
    public LayerMask obstacleLayer;
    
    [Tooltip("How far ahead fish look for obstacles")]
    public float obstacleDetectionDistance = 10f;
    
    private Flock flock;
    
    void Start()
    {
        flock = GetComponent<Flock>();
        
        // Set default to Obstacles layer (layer 11) if not set
        if (obstacleLayer.value == 0)
        {
            obstacleLayer = 1 << 11; // Layer 11 = Obstacles
        }
        
        Debug.Log($"FlockObstacleSetup: Obstacle layer mask = {obstacleLayer.value}");
    }
    
    void LateUpdate()
    {
        if (flock == null || flock.allUnits == null) return;
        
        // Update all fish units with correct obstacle mask
        foreach (var unit in flock.allUnits)
        {
            if (unit != null)
            {
                FlockUnit flockUnit = unit.GetComponent<FlockUnit>();
                if (flockUnit != null)
                {
                    // Use reflection to set the private obstacleMask field
                    var field = typeof(FlockUnit).GetField("obstacleMask", 
                        System.Reflection.BindingFlags.NonPublic | 
                        System.Reflection.BindingFlags.Instance);
                    
                    if (field != null)
                    {
                        field.SetValue(flockUnit, obstacleLayer);
                    }
                }
            }
        }
    }
}
