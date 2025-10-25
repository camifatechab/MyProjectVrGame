using UnityEngine;
using Lake;

public class WaterTriggerZone : MonoBehaviour
{
private void Awake()
    {
        ConfigureTrigger();
    }
    
    private void Start()
    {
        ConfigureTrigger();
    }
    
private void ConfigureTrigger()
    {
        // Configure this object as a trigger zone
        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null)
        {
            col.isTrigger = true;
            // Cover the entire water volume
            col.size = new Vector3(50f, 20f, 50f); // 50x50 surface, 20 depth
            col.center = new Vector3(0f, 0f, 0f); // Center at object position
        }
    }

private void OnTriggerEnter(Collider other)
    {
        // Detect when player enters water
        SimplePlayerController controller = other.GetComponent<SimplePlayerController>();
        if (controller != null)
        {
            controller.EnableSwimming();
        }
        
        // Enable arm-based swimming locomotion
        SwimmingLocomotion swimLoco = other.GetComponent<SwimmingLocomotion>();
        if (swimLoco != null)
        {
            
        
        // Enable oxygen system
        Lake.OxygenSystem oxygenSystem = other.GetComponent<Lake.OxygenSystem>();
        if (oxygenSystem != null)
        {
            oxygenSystem.SetUnderwater(true);
        }
swimLoco.EnableSwimming(true);
        }
        
        // Optionally disable regular continuous movement when swimming
        UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets.DynamicMoveProvider moveProvider = other.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets.DynamicMoveProvider>();
        if (moveProvider != null)
        {
            moveProvider.enabled = false;
        }
    }

private void OnTriggerExit(Collider other)
    {
        // Detect when player exits water
        SimplePlayerController controller = other.GetComponent<SimplePlayerController>();
        if (controller != null)
        {
            controller.DisableSwimming();
        }
        
        // Disable arm-based swimming locomotion
        SwimmingLocomotion swimLoco = other.GetComponent<SwimmingLocomotion>();
        if (swimLoco != null)
        {
            
        
        // Disable oxygen system
        Lake.OxygenSystem oxygenSystem = other.GetComponent<Lake.OxygenSystem>();
        if (oxygenSystem != null)
        {
            oxygenSystem.SetUnderwater(false);
            // Optionally refill oxygen when exiting water
            oxygenSystem.RefillOxygenFull();
        }
swimLoco.EnableSwimming(false);
        }
        
        // Re-enable regular continuous movement
        UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets.DynamicMoveProvider moveProvider = other.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets.DynamicMoveProvider>();
        if (moveProvider != null)
        {
            moveProvider.enabled = true;
        }
    }
}
