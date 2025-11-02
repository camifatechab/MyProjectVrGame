using UnityEngine;

public class Crystal : MonoBehaviour
{
    [Header("Visual Effects")]
    [SerializeField] private float rotationSpeed = 50f;
    [SerializeField] private float floatAmplitude = 0.3f;
    [SerializeField] private float floatSpeed = 1f;
    
    [Header("Collection")]
    [SerializeField] private bool autoDestroy = true;
    [SerializeField] private float destroyDelay = 0.1f;
    
    private Vector3 startPosition;
    private bool isCollected = false;
    
    void Start()
    {
        startPosition = transform.position;
        Debug.Log($"Crystal {gameObject.name} initialized at {startPosition}");
    }
    
    void Update()
    {
        if (!isCollected)
        {
            AnimateCrystal();
        }
    }
    
    private void AnimateCrystal()
    {
        // Rotate crystal
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        
        // Float up and down
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (!isCollected)
        {
            Debug.Log($"[Crystal {gameObject.name}] Trigger hit by: {other.gameObject.name}, Tag: {other.tag}, Layer: {LayerMask.LayerToName(other.gameObject.layer)}");
            
            // Check if player collected the crystal - be very permissive
            if (other.CompareTag("Player") || 
                other.CompareTag("MainCamera") || 
                other.name.Contains("Camera") || 
                other.name.Contains("XR") ||
                other.name.Contains("Origin"))
            {
                Debug.Log($"[Crystal {gameObject.name}] Collection condition met! Collecting...");
                CollectCrystal();
            }
            else
            {
                Debug.Log($"[Crystal {gameObject.name}] Trigger hit but not by player");
            }
        }
    }
    
    private void CollectCrystal()
    {
        isCollected = true;
        
        Debug.Log($"[Crystal {gameObject.name}] COLLECTED!");
        
        // Notify the collection system
        if (CrystalCollectionSystem.Instance != null)
        {
            CrystalCollectionSystem.Instance.CollectCrystal();
        }
        else
        {
            Debug.LogError("CrystalCollectionSystem.Instance is null!");
        }
        
        // TODO: Play collection sound
        // TODO: Play collection particle effect
        
        // Destroy or hide the crystal
        if (autoDestroy)
        {
            Destroy(gameObject, destroyDelay);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
