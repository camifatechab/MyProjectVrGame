using UnityEngine;
using System.Collections.Generic;

namespace JetpackGame
{
    /// <summary>
    /// Controls cloud visibility based on player height.
    /// Creates a "Maleficent moment" where clouds dramatically reveal as player ascends.
    /// </summary>
    public class CloudRevealManager : MonoBehaviour
    {
        [Header("Player Reference")]
        [Tooltip("The player/camera transform to track height")]
        public Transform playerTransform;
        
        [Header("Height Thresholds")]
        [Tooltip("Height at which clouds start to appear")]
        public float revealStartHeight = 50f;
        
        [Tooltip("Height at which clouds are fully visible")]
        public float revealEndHeight = 80f;
        
        [Tooltip("Clouds above this height will be affected by the reveal system")]
        public float cloudHeightThreshold = 60f;
        
        [Header("Reveal Settings")]
        [Tooltip("How the reveal progresses (use AnimationCurve for dramatic effect)")]
        public AnimationCurve revealCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        [Tooltip("Multiply density by this when fully revealed")]
        [Range(0f, 2f)]
        public float maxDensity = 1f;
        
        [Tooltip("Minimum density (when hidden)")]
        [Range(0f, 1f)]
        public float minDensity = 0f;
        
        [Header("Drama Effects")]
        [Tooltip("Add slight vertical movement to clouds during reveal")]
        public bool enableDramaticMovement = true;
        
        [Tooltip("How much clouds move down during reveal")]
        public float dramaticDropDistance = 5f;
        
        [Header("Debug")]
        public bool showDebugInfo = false;
        
        // Internal
        private List<CloudData> managedClouds = new List<CloudData>();
        private float currentRevealProgress = 0f;
        
        private class CloudData
        {
            public VolumetricCloud cloud;
            public float originalY;
            public float originalDensity;
            public bool isAboveThreshold;
        }
        
        private void Start()
        {
            // Find player if not assigned
            if (playerTransform == null)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    playerTransform = mainCam.transform;
                }
            }
            
            // Gather all clouds
            GatherClouds();
            
            // Initialize - hide high clouds
            UpdateCloudVisibility(true);
        }
        
        private void GatherClouds()
        {
            managedClouds.Clear();
            
            VolumetricCloud[] allClouds = FindObjectsOfType<VolumetricCloud>();
            
            foreach (var cloud in allClouds)
            {
                CloudData data = new CloudData
                {
                    cloud = cloud,
                    originalY = cloud.transform.position.y,
                    originalDensity = cloud.density,
                    isAboveThreshold = cloud.transform.position.y >= cloudHeightThreshold
                };
                
                managedClouds.Add(data);
                
                if (showDebugInfo)
                {
                    Debug.Log($"Cloud '{cloud.name}' at Y={data.originalY:F1}, Above threshold: {data.isAboveThreshold}");
                }
            }
            
            Debug.Log($"CloudRevealManager: Managing {managedClouds.Count} clouds, {managedClouds.FindAll(c => c.isAboveThreshold).Count} above threshold");
        }
        
        private void Update()
        {
            if (playerTransform == null) return;
            
            UpdateCloudVisibility(false);
        }
        
        private void UpdateCloudVisibility(bool immediate)
        {
            float playerHeight = playerTransform.position.y;
            
            // Calculate reveal progress (0 = hidden, 1 = fully revealed)
            float rawProgress = Mathf.InverseLerp(revealStartHeight, revealEndHeight, playerHeight);
            float targetProgress = revealCurve.Evaluate(rawProgress);
            
            // Smooth the transition unless immediate
            if (immediate)
            {
                currentRevealProgress = targetProgress;
            }
            else
            {
                currentRevealProgress = Mathf.Lerp(currentRevealProgress, targetProgress, Time.deltaTime * 2f);
            }
            
            // Apply to clouds above threshold
            foreach (var data in managedClouds)
            {
                if (data.cloud == null) continue;
                
                if (data.isAboveThreshold)
                {
                    // This cloud should be affected by reveal system
                    float density = Mathf.Lerp(minDensity, data.originalDensity * maxDensity, currentRevealProgress);
                    data.cloud.density = density;
                    
                    // Dramatic movement effect
                    if (enableDramaticMovement)
                    {
                        float yOffset = (1f - currentRevealProgress) * dramaticDropDistance;
                        Vector3 pos = data.cloud.transform.position;
                        pos.y = data.originalY + yOffset;
                        data.cloud.transform.position = pos;
                    }
                }
            }
            
            if (showDebugInfo && Time.frameCount % 60 == 0)
            {
                Debug.Log($"Player height: {playerHeight:F1}, Reveal progress: {currentRevealProgress:F2}");
            }
        }
        
        /// <summary>
        /// Call this to instantly reveal all clouds (for cutscenes, etc.)
        /// </summary>
        public void RevealAllClouds()
        {
            currentRevealProgress = 1f;
            foreach (var data in managedClouds)
            {
                if (data.cloud != null && data.isAboveThreshold)
                {
                    data.cloud.density = data.originalDensity * maxDensity;
                    
                    if (enableDramaticMovement)
                    {
                        Vector3 pos = data.cloud.transform.position;
                        pos.y = data.originalY;
                        data.cloud.transform.position = pos;
                    }
                }
            }
        }
        
        /// <summary>
        /// Call this to instantly hide high clouds (for reset, etc.)
        /// </summary>
        public void HideHighClouds()
        {
            currentRevealProgress = 0f;
            foreach (var data in managedClouds)
            {
                if (data.cloud != null && data.isAboveThreshold)
                {
                    data.cloud.density = minDensity;
                }
            }
        }
        
        /// <summary>
        /// Refresh the cloud list (call after spawning new clouds)
        /// </summary>
        public void RefreshCloudList()
        {
            GatherClouds();
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw height thresholds
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(new Vector3(0, revealStartHeight, 0), new Vector3(200, 0.5f, 200));
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(new Vector3(0, revealEndHeight, 0), new Vector3(200, 0.5f, 200));
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(new Vector3(0, cloudHeightThreshold, 0), new Vector3(200, 0.5f, 200));
        }
    }
}
