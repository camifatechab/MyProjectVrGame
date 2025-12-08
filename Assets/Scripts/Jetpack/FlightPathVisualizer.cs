using UnityEngine;

public class FlightPathVisualizer : MonoBehaviour
{
    public Color pathColor = Color.cyan;
    public Color waypointColor = Color.green;
    public float waypointSize = 2f;
    public bool showLabels = true;
    
    void OnDrawGizmos()
    {
        Transform[] waypoints = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            waypoints[i] = transform.GetChild(i);
        }
        
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            
            // Draw waypoint sphere
            Gizmos.color = waypointColor;
            Gizmos.DrawWireSphere(waypoints[i].position, waypointSize);
            
            // Draw line to next waypoint
            if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
            {
                Gizmos.color = pathColor;
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
            
            // Draw label
            #if UNITY_EDITOR
            if (showLabels)
            {
                UnityEditor.Handles.Label(waypoints[i].position + Vector3.up * (waypointSize + 1), waypoints[i].name);
            }
            #endif
        }
    }
}
