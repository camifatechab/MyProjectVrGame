using UnityEngine;
using UnityEngine.Events;

namespace Lake
{
    /// <summary>
    /// Collectible spaceship piece that the player can pick up underwater.
    /// Tracks collection and can trigger events for game progression.
    /// </summary>
    public class SpaceshipPiece : MonoBehaviour
    {
        [Header("Piece Info")]
        [Tooltip("Unique identifier for this piece")]
        [SerializeField] private string pieceID = "Piece_01";
        
        [Tooltip("Display name of this piece")]
        [SerializeField] private string pieceName = "Engine Part";
        
        [Tooltip("Description of this piece")]
        [SerializeField] private string description = "A crucial component of the spaceship engine.";
        
        [Header("Collection Settings")]
        [Tooltip("Can this piece be collected?")]
        [SerializeField] private bool canBeCollected = true;
        
        [Tooltip("Auto-collect on touch or require button press?")]
        [SerializeField] private bool autoCollect = true;
        
        [Tooltip("Tag required to collect this piece")]
        [SerializeField] private string collectorTag = "Player";
        
        [Header("Visual Effects")]
        [Tooltip("Object to rotate for floating effect")]
        [SerializeField] private Transform visualObject;
        
        [Tooltip("Rotation speed (degrees per second)")]
        [SerializeField] private float rotationSpeed = 30f;
        
        [Tooltip("Bobbing effect enabled")]
        [SerializeField] private bool enableBobbing = true;
        
        [Tooltip("Bobbing height")]
        [SerializeField] private float bobbingHeight = 0.3f;
        
        [Tooltip("Bobbing speed")]
        [SerializeField] private float bobbingSpeed = 2f;
        
        [Header("Particle Effects")]
        [Tooltip("Particle system to play when collected")]
        [SerializeField] private ParticleSystem collectParticles;
        
        [Tooltip("Optional glow effect")]
        [SerializeField] private GameObject glowEffect;
        
        [Header("Audio")]
        [Tooltip("Sound to play when collected")]
        [SerializeField] private AudioClip collectSound;
        
        [Header("Events")]
        public UnityEvent OnCollected;
        public UnityEvent<string> OnPieceCollected; // Passes piece ID
        
        private Vector3 startPosition;
        private float bobbingTimer = 0f;
        private bool isCollected = false;
        private AudioSource audioSource;
        
        // Public properties
        public string PieceID => pieceID;
        public string PieceName => pieceName;
        public string Description => description;
        public bool IsCollected => isCollected;
        
        private void Awake()
        {
            // Store starting position for bobbing
            startPosition = transform.position;
            
            // Random bobbing offset for variety
            bobbingTimer = Random.Range(0f, Mathf.PI * 2f);
            
            // Setup audio
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && collectSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f; // 3D sound
            }
            
            // If no visual object specified, use self
            if (visualObject == null)
            {
                visualObject = transform;
            }
        }
        
        private void Update()
        {
            if (isCollected) return;
            
            // Rotate the piece
            if (visualObject != null)
            {
                visualObject.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            }
            
            // Bobbing effect
            if (enableBobbing)
            {
                bobbingTimer += Time.deltaTime * bobbingSpeed;
                float yOffset = Mathf.Sin(bobbingTimer) * bobbingHeight;
                transform.position = startPosition + new Vector3(0f, yOffset, 0f);
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            // Check if collector has the right tag
            if (!other.CompareTag(collectorTag)) return;
            
            // Check if can be collected
            if (!canBeCollected || isCollected) return;
            
            // Auto-collect or wait for input
            if (autoCollect)
            {
                CollectPiece(other.gameObject);
            }
        }
        
        /// <summary>
        /// Collects this spaceship piece
        /// </summary>
        public void CollectPiece(GameObject collector)
        {
            if (isCollected) return;
            
            isCollected = true;
            
            // Visual feedback
            if (collectParticles != null)
            {
                collectParticles.transform.SetParent(null); // Detach so it plays after destroy
                collectParticles.Play();
                Destroy(collectParticles.gameObject, collectParticles.main.duration);
            }
            
            if (glowEffect != null)
            {
                glowEffect.SetActive(false);
            }
            
            // Audio feedback
            if (audioSource != null && collectSound != null)
            {
                // Play sound at position then destroy
                AudioSource.PlayClipAtPoint(collectSound, transform.position);
            }
            
            // Trigger events
            OnCollected?.Invoke();
            OnPieceCollected?.Invoke(pieceID);
            
            // Log collection
            Debug.Log($"Collected spaceship piece: {pieceName} (ID: {pieceID})");
            
            // Notify collection manager if exists
            SpaceshipCollectionManager manager = FindObjectOfType<SpaceshipCollectionManager>();
            if (manager != null)
            {
                manager.RegisterCollection(this);
            }
            
            // Destroy or hide the piece
            if (visualObject != null && visualObject != transform)
            {
                visualObject.gameObject.SetActive(false);
            }
            
            Destroy(gameObject, 0.1f);
        }
        
        /// <summary>
        /// Shows interaction prompt (call this from a proximity trigger)
        /// </summary>
        public void ShowPrompt(bool show)
        {
            // You can implement UI prompt here
            // For now, just debug
            if (show && !isCollected)
            {
                Debug.Log($"Press [E] to collect: {pieceName}");
            }
        }
        
        // Debug visualization
        private void OnDrawGizmos()
        {
            if (isCollected) return;
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
        
        private void OnDrawGizmosSelected()
        {
            if (isCollected) return;
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position, 0.6f);
            
            #if UNITY_EDITOR
            Vector3 labelPos = transform.position + Vector3.up * 2f;
            UnityEditor.Handles.Label(labelPos, $"{pieceName}\nID: {pieceID}");
            #endif
        }
    }
}