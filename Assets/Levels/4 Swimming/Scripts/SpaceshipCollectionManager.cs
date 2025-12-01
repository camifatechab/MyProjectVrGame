using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Lake
{
    /// <summary>
    /// Manages the collection of spaceship pieces and tracks game progression.
    /// </summary>
    public class SpaceshipCollectionManager : MonoBehaviour
    {
        [Header("Collection Tracking")]
        [Tooltip("Total number of pieces to collect")]
        [SerializeField] private int totalPieces = 5;
        
        [Tooltip("Currently collected pieces")]
        [SerializeField] private int collectedPieces = 0;
        
        [Header("Events")]
        public UnityEvent<int, int> OnPieceCollected; // (current, total)
        public UnityEvent OnAllPiecesCollected;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLog = true;
        
        private HashSet<string> collectedPieceIDs = new HashSet<string>();
        
        // Public properties
        public int TotalPieces => totalPieces;
        public int CollectedPieces => collectedPieces;
        public float CollectionPercentage => (float)collectedPieces / totalPieces;
        public bool AllCollected => collectedPieces >= totalPieces;
        
        private void Start()
        {
            // Auto-detect total pieces in scene if set to 0
            if (totalPieces == 0)
            {
                SpaceshipPiece[] pieces =FindObjectsByType<SpaceshipPiece>(FindObjectsSortMode.None);
                totalPieces = pieces.Length;
                
                if (showDebugLog)
                {
                    Debug.Log($"SpaceshipCollectionManager: Auto-detected {totalPieces} pieces in scene");
                }
            }
        }
        
        /// <summary>
        /// Called when a spaceship piece is collected
        /// </summary>
        public void RegisterCollection(SpaceshipPiece piece)
        {
            // Prevent duplicate counting
            if (collectedPieceIDs.Contains(piece.PieceID))
            {
                if (showDebugLog)
                {
                    Debug.LogWarning($"Piece {piece.PieceID} already collected!");
                }
                return;
            }
            
            // Add to collection
            collectedPieceIDs.Add(piece.PieceID);
            collectedPieces++;
            
            if (showDebugLog)
            {
                Debug.Log($"Collected piece: {piece.PieceName} ({collectedPieces}/{totalPieces})");
            }
            
            // Trigger events
            OnPieceCollected?.Invoke(collectedPieces, totalPieces);
            
            // Check if all collected
            if (collectedPieces >= totalPieces)
            {
                OnAllCollected();
            }
        }
        
        /// <summary>
        /// Called when all pieces are collected
        /// </summary>
        private void OnAllCollected()
        {
            if (showDebugLog)
            {
                Debug.Log("ALL SPACESHIP PIECES COLLECTED! 🚀");
            }
            
            OnAllPiecesCollected?.Invoke();
        }
        
        /// <summary>
        /// Resets the collection progress
        /// </summary>
        public void ResetCollection()
        {
            collectedPieces = 0;
            collectedPieceIDs.Clear();
            
            if (showDebugLog)
            {
                Debug.Log("Collection progress reset");
            }
        }
        
        /// <summary>
        /// Checks if a specific piece ID has been collected
        /// </summary>
        public bool IsPieceCollected(string pieceID)
        {
            return collectedPieceIDs.Contains(pieceID);
        }
    }
}