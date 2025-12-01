using UnityEngine;
using TMPro;

public class SwimmingDebugUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SimpleSwimmingMovement keyboardSwimming;
    [SerializeField] private SwimmingLocomotion vrSwimming;
    [SerializeField] private TextMeshProUGUI debugText;
    
    [Header("Settings")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;
    
    private void Update()
    {
        // Toggle debug display
        if (Input.GetKeyDown(toggleKey))
        {
            showDebugInfo = !showDebugInfo;
            if (debugText != null)
            {
                debugText.gameObject.SetActive(showDebugInfo);
            }
        }
        
        // Update debug text
        if (showDebugInfo && debugText != null)
        {
            UpdateDebugDisplay();
        }
    }
    
    private void UpdateDebugDisplay()
    {
        string info = "<b>=== SWIMMING DEBUG ===</b>\n\n";
        
        // Keyboard swimming info
        if (keyboardSwimming != null)
        {
            info += "<b>KEYBOARD SWIMMING</b>\n";
            info += $"Mode: {keyboardSwimming.GetSwimmingMode()}\n";
            info += $"Speed: {keyboardSwimming.GetCurrentSpeed():F2} m/s\n";
            info += $"Near Surface: {keyboardSwimming.IsNearSurface()}\n";
            info += $"Boost Active: {keyboardSwimming.IsBoostActive()}\n\n";
            info += "<color=yellow>Controls:</color>\n";
            info += "WASD - Move\n";
            info += "Space/Shift - Up/Down (Underwater)\n";
            info += "L-Shift + W - Sprint\n";
            info += "L-Ctrl - Boost\n";
            info += "Space (Surface) - Jump\n\n";
        }
        
        // VR swimming info
        if (vrSwimming != null && vrSwimming.isSwimmingEnabled)
        {
            info += "<b>VR SWIMMING</b>\n";
            info += $"Mode: {vrSwimming.GetSwimmingMode()}\n";
            info += $"Speed: {vrSwimming.GetCurrentSpeed():F2} m/s\n";
            info += $"Near Surface: {vrSwimming.IsNearSurface()}\n";
            info += $"Boost Active: {vrSwimming.IsBoostActive()}\n\n";
            info += "<color=yellow>Mechanics:</color>\n";
            info += "Pull arms back - Swim forward\n";
            info += "Push arms forward - Breaststroke\n";
            info += "Pull arms down - Swim up\n";
            info += "Push arms up - Dive down\n";
            info += "Both arms fast pull - Boost\n";
        }
        
        info += "\n<color=cyan>Press F1 to toggle this display</color>";
        
        debugText.text = info;
    }
    
    private void OnValidate()
    {
        // Try to auto-find components
        if (keyboardSwimming == null)
        {
            keyboardSwimming = FindFirstObjectByType<SimpleSwimmingMovement>();
        }
        
        if (vrSwimming == null)
        {
            vrSwimming = FindFirstObjectByType<SwimmingLocomotion>();
        }
    }
}
