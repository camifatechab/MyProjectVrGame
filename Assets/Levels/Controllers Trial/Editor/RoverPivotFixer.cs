#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// One-click Editor script: snaps FrontLeft_Pivot and FrontRight_Pivot
/// to the world position of their wheel children, then zeros the children's local positions.
/// Run once via Tools > Fix Rover Wheel Pivots, then delete this script.
/// </summary>
public class RoverPivotFixer : MonoBehaviour
{
    [MenuItem("Tools/Fix Rover Wheel Pivots")]
    static void FixPivots()
    {
        FixPivot("FrontLeft_Pivot", "Wheel_FrontLeft");
        FixPivot("FrontRight_Pivot", "Wheel_FrontRight");

        // Also handle fenders if present inside pivot
        ReparentFender("FrontLeft_Pivot", "Fender_FrontLeft");
        ReparentFender("FrontRight_Pivot", "Fender_FrontRight");

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("<color=green>✓ Rover wheel pivots fixed! Save the scene (Ctrl+S).</color>");
    }

    static void FixPivot(string pivotName, string wheelName)
    {
        GameObject pivotGO = GameObject.Find(pivotName);
        GameObject wheelGO = GameObject.Find(wheelName);

        if (pivotGO == null) { Debug.LogWarning($"Could not find {pivotName}"); return; }
        if (wheelGO == null) { Debug.LogWarning($"Could not find {wheelName}"); return; }

        Undo.RecordObject(pivotGO.transform, $"Fix {pivotName}");
        Undo.RecordObject(wheelGO.transform, $"Fix {wheelName}");

        // Move pivot to wheel's current world position
        Vector3 wheelWorldPos = wheelGO.transform.position;
        pivotGO.transform.position = wheelWorldPos;

        // Zero out wheel's local position (it's now at the pivot center)
        wheelGO.transform.localPosition = Vector3.zero;

        Debug.Log($"<color=cyan>✓ {pivotName} snapped to {wheelWorldPos}, {wheelName} zeroed.</color>");
    }

    static void ReparentFender(string pivotName, string fenderName)
    {
        GameObject pivotGO = GameObject.Find(pivotName);
        GameObject fenderGO = GameObject.Find(fenderName);

        if (pivotGO == null || fenderGO == null) return;

        // Only reparent if not already a child
        if (fenderGO.transform.parent == pivotGO.transform) return;

        Undo.SetTransformParent(fenderGO.transform, pivotGO.transform, $"Reparent {fenderName}");
        fenderGO.transform.localPosition = Vector3.zero;
        fenderGO.transform.localRotation = Quaternion.identity;

        Debug.Log($"<color=cyan>✓ {fenderName} reparented to {pivotName} and zeroed.</color>");
    }
}
#endif
