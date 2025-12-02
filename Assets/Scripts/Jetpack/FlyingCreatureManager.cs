using UnityEngine;

/// <summary>
/// Manages all flying creatures in the scene.
/// Configures patrol behavior for ambient creatures.
/// </summary>
public class FlyingCreatureManager : MonoBehaviour
{
    [Header("Auto-Setup")]
    [Tooltip("Automatically find and setup child creatures on Start")]
    public bool autoSetupOnStart = true;
    
    [Header("Patrol Area")]
    [Tooltip("Center of patrol area for all creatures")]
    public Vector3 patrolCenter = new Vector3(0f, 35f, 0f);
    
    [Tooltip("Size of patrol area")]
    public Vector3 patrolAreaSize = new Vector3(80f, 40f, 80f);
    
    [Tooltip("Minimum flight height")]
    public float minHeight = 10f;
    
    [Tooltip("Maximum flight height")]
    public float maxHeight = 65f;
    
    [Header("Default Flight Settings")]
    public float defaultMoveSpeed = 8f;
    public float defaultBobAmplitude = 1.5f;
    public float defaultBobSpeed = 0.8f;
    public float defaultBankAngle = 15f;
    public int waypointsPerCreature = 6;
    
    [Header("Variation")]
    [Range(0f, 0.5f)]
    public float randomVariation = 0.25f;
    
    [Header("References")]
    public FlyingCreaturePatrol[] creatures;
    
    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupCreatures();
        }
    }
    
    [ContextMenu("Setup Creatures")]
    public void SetupCreatures()
    {
        // Auto-add FlyingCreaturePatrol to children that don't have it
        foreach (Transform child in transform)
        {
            if (child.GetComponent<FlyingCreaturePatrol>() == null)
            {
                // Skip the rideable creature (Sky_flyB) - it uses RideableCreature instead
                if (child.name.Contains("Sky_flyB"))
                {
                    // Add RideableCreature if not present
                    if (child.GetComponent<RideableCreature>() == null)
                    {
                        var rideable = child.gameObject.AddComponent<RideableCreature>();
                        rideable.mountDistance = 4f;
                        rideable.rideSpeed = 10f;
                        Debug.Log($"Added RideableCreature to {child.name}");
                    }
                    // Also add patrol for when not being ridden
                    child.gameObject.AddComponent<FlyingCreaturePatrol>();
                    Debug.Log($"Added FlyingCreaturePatrol to {child.name}");
                }
                else
                {
                    child.gameObject.AddComponent<FlyingCreaturePatrol>();
                    Debug.Log($"Added FlyingCreaturePatrol to {child.name}");
                }
            }
        }
        
        // Now get all patrol components
        creatures = GetComponentsInChildren<FlyingCreaturePatrol>();
        
        if (creatures.Length == 0)
        {
            Debug.LogWarning("FlyingCreatureManager: No creatures found to setup!");
            return;
        }
        
        for (int i = 0; i < creatures.Length; i++)
        {
            FlyingCreaturePatrol creature = creatures[i];
            
            // Apply settings with variation
            creature.moveSpeed = ApplyVariation(defaultMoveSpeed);
            creature.bobAmplitude = ApplyVariation(defaultBobAmplitude);
            creature.bobSpeed = ApplyVariation(defaultBobSpeed);
            creature.maxBankAngle = ApplyVariation(defaultBankAngle);
            creature.autoWaypointCount = waypointsPerCreature + Random.Range(-2, 3);
            
            // Set patrol area
            creature.patrolAreaCenter = patrolCenter;
            creature.patrolAreaSize = patrolAreaSize;
            creature.minHeight = minHeight;
            creature.maxHeight = maxHeight;
            
            // Set forward rotation for downward facing
            creature.forwardRotationOffset = new Vector3(75f, 0f, 0f);
            
            // Alternate loop behavior
            creature.loopWaypoints = (i % 2 == 0);
            
            Debug.Log($"Configured {creature.gameObject.name}: speed={creature.moveSpeed:F1}");
        }
    }
    
    private float ApplyVariation(float baseValue)
    {
        float variation = Random.Range(-randomVariation, randomVariation);
        return baseValue * (1f + variation);
    }
    
    public void SetCreaturesActive(bool active)
    {
        foreach (var creature in creatures)
        {
            if (creature != null)
                creature.enabled = active;
        }
    }
}
