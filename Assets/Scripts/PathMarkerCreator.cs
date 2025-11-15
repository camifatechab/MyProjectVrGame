using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class PathMarkerCreator : MonoBehaviour
{
    [Header("Path Marker Settings")]
    [Tooltip("Color of the glowing marker")]
    public Color emissiveColor = new Color(0f, 0.8f, 1f); // Cyan glow
    
    [Tooltip("Emission intensity (higher = brighter in darkness)")]
    [Range(0f, 10f)]
    public float emissionIntensity = 3f;
    
    [Tooltip("Size of the marker sphere")]
    [Range(0.1f, 2f)]
    public float markerSize = 0.3f;
    
    [Tooltip("Add a point light to each marker")]
    public bool addPointLight = true;
    
    [Tooltip("Light range")]
    [Range(1f, 20f)]
    public float lightRange = 5f;
    
    [Tooltip("Light intensity")]
    [Range(0f, 5f)]
    public float lightIntensity = 2f;

    private Material emissiveMaterial;

    void OnEnable()
    {
        // Create marker immediately when enabled (even in edit mode)
        CreateEmissiveMaterial();
        
        // Check if marker already exists
        Transform existingMarker = transform.Find("GlowingSphere");
        if (existingMarker == null)
        {
            CreateMarkerAtPosition();
        }
        else
        {
            // Update existing marker's material
            MeshRenderer renderer = existingMarker.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = emissiveMaterial;
            }
        }
    }

    void OnValidate()
    {
        // Update properties when changed in inspector (works in edit mode)
        if (emissiveMaterial != null)
        {
            CreateEmissiveMaterial();
            Transform marker = transform.Find("GlowingSphere");
            if (marker != null)
            {
                marker.localScale = Vector3.one * markerSize;
                MeshRenderer renderer = marker.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = emissiveMaterial;
                }
                
                Light light = marker.GetComponent<Light>();
                if (addPointLight && light == null)
                {
                    light = marker.gameObject.AddComponent<Light>();
                    light.type = LightType.Point;
                }
                
                if (light != null)
                {
                    if (!addPointLight)
                    {
#if UNITY_EDITOR
                        DestroyImmediate(light);
#else
                        Destroy(light);
#endif
                    }
                    else
                    {
                        light.color = emissiveColor;
                        light.intensity = lightIntensity;
                        light.range = lightRange;
                        light.renderMode = LightRenderMode.ForcePixel;
                    }
                }
            }
        }
    }

    void CreateEmissiveMaterial()
    {
        // Create emissive material
        emissiveMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        emissiveMaterial.SetColor("_BaseColor", emissiveColor);
        emissiveMaterial.SetColor("_EmissionColor", emissiveColor * emissionIntensity);
        emissiveMaterial.EnableKeyword("_EMISSION");
        emissiveMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
    }

    void CreateMarkerAtPosition()
    {
        // Create sphere for this marker
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.transform.SetParent(transform);
        marker.transform.localPosition = Vector3.zero;
        marker.transform.localScale = Vector3.one * markerSize;
        marker.name = "GlowingSphere";
        
        // Apply emissive material
        MeshRenderer renderer = marker.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = emissiveMaterial;
        
        // Remove collider (markers are just visual)
#if UNITY_EDITOR
        DestroyImmediate(marker.GetComponent<SphereCollider>());
#else
        Destroy(marker.GetComponent<SphereCollider>());
#endif
        
        // Add point light if enabled
        if (addPointLight)
        {
            Light pointLight = marker.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = emissiveColor;
            pointLight.intensity = lightIntensity;
            pointLight.range = lightRange;
            pointLight.renderMode = LightRenderMode.ForcePixel;
        }
    }
}
