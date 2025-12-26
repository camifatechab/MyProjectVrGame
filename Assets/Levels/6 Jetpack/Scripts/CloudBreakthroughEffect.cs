using UnityEngine;

namespace JetpackGame
{
    /// <summary>
    /// Controls Northern Lights visibility based on Skybox blend.
    /// Aurora is DISABLED at start, ENABLED only when Skybox 2 is fully visible.
    /// NO particles, NO mist - just aurora control.
    /// </summary>
    public class CloudBreakthroughEffect : MonoBehaviour
    {
        [Header("Player Reference")]
        public Transform playerTransform;
        
        [Header("Skybox Blend Reference")]
        [Tooltip("Reference to blended skybox material to know when Skybox 2 is fully visible")]
        public Material blendedSkyboxMaterial;
        
        [Header("Northern Lights (Aurora)")]
        [Tooltip("The Aurora GameObject to show/hide")]
        public GameObject northernLightsObject;
        
        [Header("Debug")]
        public bool showDebugInfo = false;
        
        // Skybox blend tracking
        private bool skybox2FullyVisible = false;
        private bool auroraCurrentlyVisible = false;
        
        private void Start()
        {
            // Auto-find player if not assigned
            if (playerTransform == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    playerTransform = player.transform;
                else
                {
                    var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
                    if (xrOrigin != null)
                        playerTransform = xrOrigin.transform;
                }
            }
            
            // Try to find blended skybox material if not assigned
            if (blendedSkyboxMaterial == null)
            {
                blendedSkyboxMaterial = RenderSettings.skybox;
            }
            
            // HIDE Aurora at start
            if (northernLightsObject != null)
            {
                northernLightsObject.SetActive(false);
                auroraCurrentlyVisible = false;
                
                if (showDebugInfo)
                    Debug.Log("[CloudBreakthroughEffect] Aurora HIDDEN at start");
            }
            else
            {
                Debug.LogWarning("[CloudBreakthroughEffect] Northern Lights Object not assigned!");
            }
        }
        
        private void Update()
        {
            CheckSkyboxBlendAndUpdateAurora();
        }
        
        private void CheckSkyboxBlendAndUpdateAurora()
        {
            if (blendedSkyboxMaterial == null || northernLightsObject == null) return;
            
            // Get current blend value from skybox material
            float currentBlend = 0f;
            
            if (blendedSkyboxMaterial.HasProperty("_Blend"))
            {
                currentBlend = blendedSkyboxMaterial.GetFloat("_Blend");
            }
            else if (blendedSkyboxMaterial.HasProperty("_BlendCubemaps"))
            {
                currentBlend = blendedSkyboxMaterial.GetFloat("_BlendCubemaps");
            }
            
            // Check if Skybox 2 is fully visible (blend >= 0.99)
            bool wasFullyVisible = skybox2FullyVisible;
            skybox2FullyVisible = currentBlend >= 0.99f;
            
            // SHOW Aurora when Skybox 2 becomes fully visible
            if (skybox2FullyVisible && !auroraCurrentlyVisible)
            {
                northernLightsObject.SetActive(true);
                auroraCurrentlyVisible = true;
                
                if (showDebugInfo)
                    Debug.Log($"[CloudBreakthroughEffect] Skybox blend = {currentBlend:F2} - Aurora ENABLED");
            }
            // HIDE Aurora when Skybox 2 is no longer fully visible
            else if (!skybox2FullyVisible && auroraCurrentlyVisible)
            {
                northernLightsObject.SetActive(false);
                auroraCurrentlyVisible = false;
                
                if (showDebugInfo)
                    Debug.Log($"[CloudBreakthroughEffect] Skybox blend = {currentBlend:F2} - Aurora DISABLED");
            }
            
            if (showDebugInfo && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[CloudBreakthroughEffect] Blend: {currentBlend:F2} | Skybox2Full: {skybox2FullyVisible} | AuroraVisible: {auroraCurrentlyVisible}");
            }
        }
        
        /// <summary>
        /// Call this to reset (e.g., when restarting the level)
        /// </summary>
        public void ResetEffect()
        {
            skybox2FullyVisible = false;
            auroraCurrentlyVisible = false;
            
            if (northernLightsObject != null)
            {
                northernLightsObject.SetActive(false);
            }
        }
    }
}
