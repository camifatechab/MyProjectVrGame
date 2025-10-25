using UnityEngine;

[ExecuteInEditMode]
public class QuickSeamFix : MonoBehaviour
{
    void Start()
    {
        FixSeamsNow();
        // Destroy this script after running once
        DestroyImmediate(this);
    }
    
    public void FixSeamsNow()
    {
        // Find WaterContainer walls
        GameObject waterContainer = GameObject.Find("WaterContainer");
        if (waterContainer == null) return;
        
        // Extend walls upward slightly to overlap with land
        ExtendWall(waterContainer.transform.Find("NorthWall"));
        ExtendWall(waterContainer.transform.Find("SouthWall"));
        ExtendWall(waterContainer.transform.Find("EastWall"));
        ExtendWall(waterContainer.transform.Find("WestWall"));
        
        Debug.Log("✓ Seams fixed! Walls extended to eliminate gaps.");
    }
    
    void ExtendWall(Transform wall)
    {
        if (wall == null) return;
        
        // Extend wall height by 10% and adjust position
        Vector3 scale = wall.localScale;
        wall.localScale = new Vector3(scale.x, scale.y * 1.1f, scale.z);
        
        // Move up half the extension amount to keep bottom aligned
        Vector3 pos = wall.localPosition;
        wall.localPosition = new Vector3(pos.x, pos.y + (scale.y * 0.05f), pos.z);
    }
}
