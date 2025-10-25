using UnityEngine;

namespace Lake
{
    /// <summary>
    /// Creates a safe zone that refills the player's oxygen when they enter it.
    /// Can be set to permanent or limited use.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class OxygenRefillZone : MonoBehaviour
    {
        [Header("Refill Settings")]
        [Tooltip("How much oxygen to refill per second")]
        [SerializeField] private float refillRate = 30f;
        
        [Tooltip("Should this refill instantly or gradually?")]
        [SerializeField] private bool instantRefill = false;
        
        [Tooltip("Instant refill amount (if instant refill is enabled)")]
        [SerializeField] private float instantRefillAmount = 50f;
        
        [Header("Limited Use")]
        [Tooltip("Maximum number of uses (0 = unlimited)")]
        [SerializeField] private int maxUses = 0;
        
        [Tooltip("Current uses remaining")]
        [SerializeField] private int usesRemaining = 0;
        
        [Header("Visual Feedback")]
        [Tooltip("Particle system to play when player enters")]
        [SerializeField] private ParticleSystem entryParticles;
        
        [Tooltip("Mesh renderer that shows the bubble visual")]
        [SerializeField] private MeshRenderer bubbleVisual;
        
        [Tooltip("Should the bubble disappear when depleted?")]
        [SerializeField] private bool hideWhenDepleted = true;
        
        [Header("Audio")]
        [Tooltip("Sound to play when entering the zone")]
        [SerializeField] private AudioClip refillSound;
        
        [Tooltip("Sound to play when zone is depleted")]
        [SerializeField] private AudioClip depletedSound;
        
        private OxygenSystem currentPlayerOxygen;
        private AudioSource audioSource;
        private bool isPlayerInside = false;
        private bool isDepleted = false;
        
        private void Awake()
        {
            // Ensure collider is trigger
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;
            
            // Setup uses
            if (maxUses > 0)
            {
                usesRemaining = maxUses;
            }
            
            // Setup audio
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && (refillSound != null || depletedSound != null))
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f; // 3D sound
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            // Check if this is the player with OxygenSystem
            OxygenSystem oxygen = other.GetComponent<OxygenSystem>();
            if (oxygen != null)
            {
                if (isDepleted)
                {
                    // Play depleted sound
                    PlaySound(depletedSound);
                    return;
                }
                
                currentPlayerOxygen = oxygen;
                isPlayerInside = true;
                
                // Instant refill
                if (instantRefill)
                {
                    PerformRefill(instantRefillAmount);
                }
                
                // Visual feedback
                if (entryParticles != null)
                {
                    entryParticles.Play();
                }
                
                // Audio feedback
                PlaySound(refillSound);
                
                Debug.Log($"Player entered oxygen refill zone: {gameObject.name}");
            }
        }
        
        private void OnTriggerStay(Collider other)
        {
            // Gradual refill while staying in zone
            if (!instantRefill && isPlayerInside && currentPlayerOxygen != null && !isDepleted)
            {
                float refillAmount = refillRate * Time.deltaTime;
                currentPlayerOxygen.RefillOxygen(refillAmount);
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            OxygenSystem oxygen = other.GetComponent<OxygenSystem>();
            if (oxygen != null && oxygen == currentPlayerOxygen)
            {
                isPlayerInside = false;
                currentPlayerOxygen = null;
                
                // Stop particles
                if (entryParticles != null)
                {
                    entryParticles.Stop();
                }
                
                Debug.Log($"Player exited oxygen refill zone: {gameObject.name}");
            }
        }
        
        /// <summary>
        /// Performs a refill and handles limited uses
        /// </summary>
        private void PerformRefill(float amount)
        {
            if (currentPlayerOxygen == null) return;
            
            currentPlayerOxygen.RefillOxygen(amount);
            
            // Handle limited uses
            if (maxUses > 0)
            {
                usesRemaining--;
                
                if (usesRemaining <= 0)
                {
                    MarkAsDepleted();
                }
            }
        }
        
        /// <summary>
        /// Marks this zone as depleted
        /// </summary>
        private void MarkAsDepleted()
        {
            isDepleted = true;
            isPlayerInside = false;
            currentPlayerOxygen = null;
            
            // Visual feedback
            if (hideWhenDepleted && bubbleVisual != null)
            {
                bubbleVisual.enabled = false;
            }
            
            // Stop particles
            if (entryParticles != null)
            {
                entryParticles.Stop();
            }
            
            // Audio feedback
            PlaySound(depletedSound);
            
            Debug.Log($"Oxygen refill zone depleted: {gameObject.name}");
        }
        
        /// <summary>
        /// Resets the zone to full uses (for respawning bubbles)
        /// </summary>
        public void ResetZone()
        {
            isDepleted = false;
            usesRemaining = maxUses;
            
            if (bubbleVisual != null)
            {
                bubbleVisual.enabled = true;
            }
        }
        
        /// <summary>
        /// Plays a sound if available
        /// </summary>
        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
        
        // Debug visualization
        private void OnDrawGizmos()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Gizmos.color = isDepleted ? Color.red : Color.cyan;
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
                
                // Draw based on collider type
                if (col is SphereCollider sphere)
                {
                    Gizmos.DrawSphere(transform.position, sphere.radius * transform.lossyScale.x);
                }
                else if (col is BoxCollider box)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawCube(box.center, box.size);
                }
                else
                {
                    Gizmos.DrawSphere(transform.position, 0.5f);
                }
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw refill info
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Gizmos.color = Color.cyan;
                Vector3 labelPos = transform.position + Vector3.up * 2f;
                
                #if UNITY_EDITOR
                string label = instantRefill ? "Instant Refill" : "Gradual Refill";
                if (maxUses > 0)
                {
                    label += $"\nUses: {usesRemaining}/{maxUses}";
                }
                UnityEditor.Handles.Label(labelPos, label);
                #endif
            }
        }
    }
}