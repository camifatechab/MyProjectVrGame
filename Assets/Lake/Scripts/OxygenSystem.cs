using UnityEngine;
using UnityEngine.Events;

namespace Lake
{
    /// <summary>
    /// Manages the player's oxygen level while underwater.
    /// Drains oxygen over time and triggers events when oxygen is depleted.
    /// </summary>
    public class OxygenSystem : MonoBehaviour
    {
        [Header("Oxygen Settings")]
        [Tooltip("Maximum oxygen capacity")]
        [SerializeField] private float maxOxygen = 100f;
        
        [Tooltip("Current oxygen level")]
        [SerializeField] private float currentOxygen = 100f;
        
        [Tooltip("Oxygen drain rate per second while underwater")]
        [SerializeField] private float drainRate = 0.48f;
        
        [Tooltip("Time in seconds before oxygen starts draining after going underwater")]
        [SerializeField] private float graceTime = 2f;
        
        [Header("State")]
        [SerializeField] private bool isUnderwater = false;
        [SerializeField] private bool isDraining = false;
        
        [Header("Surface Return")]
        [Tooltip("Position to teleport player when oxygen depletes")]
        [SerializeField] private Transform surfaceSpawnPoint;
        
        [Tooltip("Enable auto-return to surface when oxygen depletes")]
        [SerializeField] private bool autoReturnEnabled = false;
        
        [Tooltip("Use smooth transition instead of instant teleport")]
        [SerializeField] private bool smoothReturn = true;
        
        [Tooltip("Speed of smooth return (higher = faster)")]
        [SerializeField] private float returnSpeed = 0.4f;
        
        [Header("Events")]
        public UnityEvent OnOxygenDepleted;
        public UnityEvent OnOxygenLow; // Triggers at 25%
        public UnityEvent<float> OnOxygenChanged; // Passes oxygen percentage (0-1)
        
        private float graceTimer = 0f;
        
        private bool isReturningToSurface = false;
        private CharacterController characterController;
private bool hasTriggeredLowWarning = false;
        
        // Public properties
        public float CurrentOxygen => currentOxygen;
        public float MaxOxygen => maxOxygen;
        public float OxygenPercentage => currentOxygen / maxOxygen;
        public bool IsUnderwater => isUnderwater;
        
        private void Start()
        {
            // Initialize oxygen to full
            currentOxygen = maxOxygen;
            OnOxygenChanged?.Invoke(OxygenPercentage);
            
            // Get CharacterController for smooth movement
            characterController = GetComponent<CharacterController>();
        }
        
        private void OldStart()
        {
            // Initialize oxygen to full
            currentOxygen = maxOxygen;
            OnOxygenChanged?.Invoke(OxygenPercentage);
        }
        
        private void Update()
        {
            if (isUnderwater && !isReturningToSurface)
            {
                HandleUnderwaterOxygen();
            }
        }
        
        /// <summary>
        /// Handles oxygen drain and warnings while underwater
        /// </summary>
        private void HandleUnderwaterOxygen()
        {
            // Grace period before draining starts
            if (!isDraining)
            {
                graceTimer += Time.deltaTime;
                if (graceTimer >= graceTime)
                {
                    isDraining = true;
                }
                return;
            }
            
            // Drain oxygen
            if (currentOxygen > 0)
            {
                currentOxygen -= drainRate * Time.deltaTime;
                currentOxygen = Mathf.Max(0, currentOxygen);
                
                OnOxygenChanged?.Invoke(OxygenPercentage);
                
                // Check for low oxygen warning (once at 25%)
                if (!hasTriggeredLowWarning && OxygenPercentage <= 0.25f)
                {
                    hasTriggeredLowWarning = true;
                    OnOxygenLow?.Invoke();
                }
                
                // Check for depletion
                if (currentOxygen <= 0)
                {
                    HandleOxygenDepletion();
                }
            }
        }
        
        /// <summary>
        /// Called when oxygen reaches zero
        /// </summary>
        private void HandleOxygenDepletion()
        {
            OnOxygenDepleted?.Invoke();
            
            if (autoReturnEnabled)
            {
                ReturnToSurface();
            }
        }
        
        /// <summary>
        /// Teleports player to surface spawn point and refills oxygen
        /// </summary>
public void ReturnToSurface()
        {
            if (surfaceSpawnPoint == null)
            {
                Debug.LogWarning("OxygenSystem: No surface spawn point assigned!");
                return;
            }
            
            if (smoothReturn && !isReturningToSurface)
            {
                StartCoroutine(SmoothReturnToSurface());
            }
            else
            {
                // Instant teleport
                transform.position = surfaceSpawnPoint.position;
                transform.rotation = surfaceSpawnPoint.rotation;
                SetUnderwater(false);
                RefillOxygen(maxOxygen);
            }
        }
        
        /// <summary>
        /// Smoothly moves player to surface
        /// </summary>
        private System.Collections.IEnumerator SmoothReturnToSurface()
        {
            isReturningToSurface = true;
            
            Vector3 startPos = transform.position;
            Vector3 targetPos = surfaceSpawnPoint.position;
            Quaternion startRot = transform.rotation;
            Quaternion targetRot = surfaceSpawnPoint.rotation;
            
            float elapsed = 0f;
            float duration = 2f / returnSpeed; // 2 seconds at speed 1.0
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Smooth curve (ease out)
                float smoothT = 1f - Mathf.Pow(1f - t, 3f);
                
                // Move using CharacterController if available
                if (characterController != null)
                {
                    Vector3 newPos = Vector3.Lerp(startPos, targetPos, smoothT);
                    Vector3 movement = newPos - transform.position;
                    characterController.Move(movement);
                }
                else
                {
                    transform.position = Vector3.Lerp(startPos, targetPos, smoothT);
                }
                
                // Rotate smoothly
                transform.rotation = Quaternion.Slerp(startRot, targetRot, smoothT);
                
                yield return null;
            }
            
            // Ensure final position
            transform.position = targetPos;
            transform.rotation = targetRot;
            
            // Complete return
            isReturningToSurface = false;
            SetUnderwater(false);
            RefillOxygen(maxOxygen);
        }
        
        /// <summary>
        /// Sets the underwater state. Call this from your water trigger.
        /// </summary>
        public void SetUnderwater(bool underwater)
        {
            isUnderwater = underwater;
            
            if (underwater)
            {
                // Reset grace timer when entering water
                graceTimer = 0f;
                isDraining = false;
            }
            else
            {
                // Reset drain state when leaving water
                isDraining = false;
                graceTimer = 0f;
                hasTriggeredLowWarning = false;
            }
        }
        
        /// <summary>
        /// Refills oxygen by specified amount
        /// </summary>
        public void RefillOxygen(float amount)
        {
            currentOxygen = Mathf.Min(currentOxygen + amount, maxOxygen);
            OnOxygenChanged?.Invoke(OxygenPercentage);
            
            // Reset low warning flag if oxygen is restored above 25%
            if (OxygenPercentage > 0.25f)
            {
                hasTriggeredLowWarning = false;
            }
        }
        
        /// <summary>
        /// Refills oxygen to maximum
        /// </summary>
        public void RefillOxygenFull()
        {
            RefillOxygen(maxOxygen);
        }
        
        /// <summary>
        /// Sets oxygen to a specific value (for debugging/testing)
        /// </summary>
        public void SetOxygen(float value)
        {
            currentOxygen = Mathf.Clamp(value, 0, maxOxygen);
            OnOxygenChanged?.Invoke(OxygenPercentage);
        }
        
        // Debug visualization
        private void OnDrawGizmos()
        {
            if (surfaceSpawnPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(surfaceSpawnPoint.position, 0.5f);
                Gizmos.DrawLine(surfaceSpawnPoint.position, surfaceSpawnPoint.position + surfaceSpawnPoint.forward * 2f);
            }
        }
    }
}