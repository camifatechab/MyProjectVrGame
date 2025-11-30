using UnityEngine;

public class SpaceshipLeverGrabber : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float grabDistance = 2f;
    [SerializeField] private string leverTag = "SpaceshipPart";
    
    [Header("UI Feedback")]
    [SerializeField] private bool showDebugMessages = true;
    
    private Camera playerCamera;
    private GameObject currentLever;
    private bool leverGrabbed = false;
    
    private void Start()
    {
        playerCamera = Camera.main;
        
        if (playerCamera == null)
        {
            Debug.LogError("No camera found! Make sure you have a camera tagged as MainCamera.");
        }
    }
    
    private void Update()
    {
        DetectLever();
        CheckGrabInput();
    }
    
private void DetectLever()
    {
        if (playerCamera == null) return;
        
        // Raycast from camera forward
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, grabDistance))
        {
            if (hit.collider.CompareTag(leverTag))
            {
                GameObject hitLever = hit.collider.gameObject;
                
                // New lever in sight
                if (currentLever != hitLever)
                {
                    // Remove highlight from old lever
                    if (currentLever != null)
                    {
                        RemoveHighlight(currentLever);
                    }
                    
                    currentLever = hitLever;
                    AddHighlight(currentLever);
                }
                
                if (showDebugMessages)
                {
                    Debug.Log("Looking at lever - Click to grab!");
                }
            }
            else
            {
                // Looking at something else
                if (currentLever != null)
                {
                    RemoveHighlight(currentLever);
                    currentLever = null;
                }
            }
        }
        else
        {
            // Not looking at anything
            if (currentLever != null)
            {
                RemoveHighlight(currentLever);
                currentLever = null;
            }
        }
    }
    
private void CheckGrabInput()
    {
        // Check for mouse click (left mouse button)
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            if (currentLever != null && !leverGrabbed)
            {
                GrabLever();
            }
        }
    }
    
    private void GrabLever()
    {
        leverGrabbed = true;
        
        Debug.Log("LEVER GRABBED! Mission Complete!");
        
        // Visual feedback - change lever color or disable glow
        PulsingGlow glow = currentLever.GetComponent<PulsingGlow>();
        if (glow != null)
        {
            glow.enabled = false;
        }
        
        // You can add more effects here:
        // - Play sound
        // - Show victory UI
        // - Trigger game end sequence
        // - etc.
    }


private void RemoveHighlight(GameObject lever)
    {
        if (lever == null) return;
        
        // Reset glow to normal
        PulsingGlow glow = lever.GetComponent<PulsingGlow>();
        if (glow != null)
        {
            glow.pulseSpeed = 2f; // Normal pulse
            glow.maxIntensity = 3f; // Normal brightness
            glow.glowColor = new Color(0f, 2f, 2f, 1f); // Cyan (original)
        }
        
        // Reset scale
        lever.transform.localScale = new Vector3(0.26577f, 0.53154f, 0.26577f);
    }


private void AddHighlight(GameObject lever)
    {
        if (lever == null) return;
        
        // Make the glow pulse faster and brighter when looking at it
        PulsingGlow glow = lever.GetComponent<PulsingGlow>();
        if (glow != null)
        {
            glow.pulseSpeed = 4f; // Faster pulse
            glow.maxIntensity = 5f; // Brighter
            glow.glowColor = new Color(1f, 2f, 0f, 1f); // Yellow-green (exciting color)
        }
        
        // Scale up slightly
        lever.transform.localScale *= 1.1f;
    }
}
