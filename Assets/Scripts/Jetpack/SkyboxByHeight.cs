using UnityEngine;

public class SkyboxByHeight : MonoBehaviour
{
    public Transform creature;
    public Material lowSkybox;
    public Material highSkybox;
    public float heightThreshold = 100f;
    
    private bool isHigh = false;
    
    void Update()
    {
        if (creature == null) return;
        
        if (creature.position.y >= heightThreshold && !isHigh)
        {
            RenderSettings.skybox = highSkybox;
            DynamicGI.UpdateEnvironment();
            isHigh = true;
        }
        else if (creature.position.y < heightThreshold && isHigh)
        {
            RenderSettings.skybox = lowSkybox;
            DynamicGI.UpdateEnvironment();
            isHigh = false;
        }
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(new Vector3(0, heightThreshold, 0), new Vector3(300, 1, 300));
    }
}
