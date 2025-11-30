using UnityEngine;

[ExecuteInEditMode]
public class FixWaterTrigger : MonoBehaviour
{
    void Start()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null)
        {
            col.isTrigger = true;
            col.size = new Vector3(50f, 20f, 50f);
            Debug.Log("Water trigger configured: size=50x20x50, isTrigger=true");
        }
    }
}