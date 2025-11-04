using UnityEngine;

public class WaterOxygenTrigger : MonoBehaviour
{
    private OxygenManager oxygenManager;
    
    void Start()
    {
        // Find the oxygen manager in the scene
        oxygenManager = FindObjectOfType<OxygenManager>();
        
        if (oxygenManager == null)
        {
            Debug.LogWarning("OxygenManager not found! Make sure it exists in the scene.");
        }
        
        // Make sure this has a trigger collider
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
            Debug.Log("Water trigger set up successfully");
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Check if the player (camera) entered the water
        if (other.CompareTag("MainCamera") || other.name.Contains("Camera"))
        {
            Debug.Log("Player entered water!");
            if (oxygenManager != null)
            {
                oxygenManager.SetUnderwater(true);
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        // Check if the player (camera) exited the water
        if (other.CompareTag("MainCamera") || other.name.Contains("Camera"))
        {
            Debug.Log("Player exited water!");
            if (oxygenManager != null)
            {
                oxygenManager.SetUnderwater(false);
            }
        }
    }
}