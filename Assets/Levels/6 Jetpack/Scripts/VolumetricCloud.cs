using UnityEngine;

namespace JetpackGame
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshRenderer))]
    public class VolumetricCloud : MonoBehaviour
    {
        [Header("Appearance")]
        [Range(0f, 2f)]
        public float density = 1f;
        
        [Range(0f, 5f)]
        public float softness = 1.5f;
        
        [Range(0f, 2f)]
        public float edgeFade = 1f;
        
        public Color cloudColor = Color.white;
        
        [ColorUsage(false, true)]
        public Color shadowColor = new Color(0.7f, 0.8f, 0.95f);
        
        [Header("Lighting")]
        public Transform sunLight;
        
        [Range(0f, 1f)]
        public float lightInfluence = 0.5f;
        
        [Range(0f, 1f)]
        public float ambientLight = 0.3f;
        
        [Header("Animation")]
        public bool animate = true;
        
        public Vector3 windDirection = new Vector3(1f, 0.2f, 0.5f);
        
        [Range(0f, 2f)]
        public float windSpeed = 0.5f;
        
        [Range(0f, 2f)]
        public float noiseScale = 1f;
        
        [Range(0f, 1f)]
        public float noiseStrength = 0.3f;
        
        [Header("Depth")]
        [Range(0f, 50f)]
        public float depthFadeDistance = 10f;
        
        [Range(0f, 1f)]
        public float depthFadeStrength = 0.5f;
        
        // Private
        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock propertyBlock;
        private Material cloudMaterial;
        
        // Shader property IDs
        private static readonly int _Density = Shader.PropertyToID("_Density");
        private static readonly int _Softness = Shader.PropertyToID("_Softness");
        private static readonly int _EdgeFade = Shader.PropertyToID("_EdgeFade");
        private static readonly int _CloudColor = Shader.PropertyToID("_CloudColor");
        private static readonly int _ShadowColor = Shader.PropertyToID("_ShadowColor");
        private static readonly int _LightDir = Shader.PropertyToID("_LightDir");
        private static readonly int _LightInfluence = Shader.PropertyToID("_LightInfluence");
        private static readonly int _AmbientLight = Shader.PropertyToID("_AmbientLight");
        private static readonly int _WindDirection = Shader.PropertyToID("_WindDirection");
        private static readonly int _WindSpeed = Shader.PropertyToID("_WindSpeed");
        private static readonly int _NoiseScale = Shader.PropertyToID("_NoiseScale");
        private static readonly int _NoiseStrength = Shader.PropertyToID("_NoiseStrength");
        private static readonly int _DepthFadeDistance = Shader.PropertyToID("_DepthFadeDistance");
        private static readonly int _DepthFadeStrength = Shader.PropertyToID("_DepthFadeStrength");
        private static readonly int _TimeOffset = Shader.PropertyToID("_TimeOffset");
        
        private float timeOffset;
        
        private void OnEnable()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            propertyBlock = new MaterialPropertyBlock();
            
            // Random time offset so clouds don't all animate in sync
            timeOffset = Random.Range(0f, 100f);
            
            // Try to find sun light if not assigned
            if (sunLight == null)
            {
                Light[] lights = FindObjectsOfType<Light>();
                foreach (Light light in lights)
                {
                    if (light.type == LightType.Directional)
                    {
                        sunLight = light.transform;
                        break;
                    }
                }
            }
            
            UpdateMaterial();
        }
        
        private void Update()
        {
            if (animate || !Application.isPlaying)
            {
                UpdateMaterial();
            }
        }
        
        public void UpdateMaterial()
        {
            if (meshRenderer == null) return;
            
            meshRenderer.GetPropertyBlock(propertyBlock);
            
            propertyBlock.SetFloat(_Density, density);
            propertyBlock.SetFloat(_Softness, softness);
            propertyBlock.SetFloat(_EdgeFade, edgeFade);
            propertyBlock.SetColor(_CloudColor, cloudColor);
            propertyBlock.SetColor(_ShadowColor, shadowColor);
            propertyBlock.SetFloat(_LightInfluence, lightInfluence);
            propertyBlock.SetFloat(_AmbientLight, ambientLight);
            propertyBlock.SetFloat(_NoiseScale, noiseScale);
            propertyBlock.SetFloat(_NoiseStrength, noiseStrength);
            propertyBlock.SetFloat(_DepthFadeDistance, depthFadeDistance);
            propertyBlock.SetFloat(_DepthFadeStrength, depthFadeStrength);
            propertyBlock.SetFloat(_TimeOffset, timeOffset);
            
            if (animate)
            {
                propertyBlock.SetVector(_WindDirection, windDirection.normalized);
                propertyBlock.SetFloat(_WindSpeed, windSpeed);
            }
            else
            {
                propertyBlock.SetFloat(_WindSpeed, 0f);
            }
            
            // Light direction
            if (sunLight != null)
            {
                propertyBlock.SetVector(_LightDir, -sunLight.forward);
            }
            else
            {
                propertyBlock.SetVector(_LightDir, new Vector3(0.5f, 1f, 0.3f).normalized);
            }
            
            meshRenderer.SetPropertyBlock(propertyBlock);
        }
        
        // Editor helper to apply material
        [ContextMenu("Apply Volumetric Cloud Material")]
        public void ApplyCloudMaterial()
        {
            Shader cloudShader = Shader.Find("Custom/VolumetricCloud");
            if (cloudShader != null)
            {
                Material mat = new Material(cloudShader);
                GetComponent<MeshRenderer>().sharedMaterial = mat;
                Debug.Log("Applied VolumetricCloud material!");
            }
            else
            {
                Debug.LogError("VolumetricCloud shader not found! Make sure the shader is in your project.");
            }
        }
    }
}
