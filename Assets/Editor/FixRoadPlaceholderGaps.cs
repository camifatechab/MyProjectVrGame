using UnityEngine;
using UnityEditor;

public static class FixRoadPlaceholderGaps
{
    [MenuItem("Tools/Fix Road Placeholder Gaps")]
    static void Fix()
    {
        int count = 176;

        // Each prefix lives under a different parent object
        // suffix: "" for center segments, "_L" / "_R" for edge pairs
        string[][] groups = {
            new[] { "Road_Placeholder_",           "Road_PlaceholderStyled",       "" },
            new[] { "Base_Road_Placeholder_",      "Base_Road_PlaceholderStyled",  "" },
            new[] { "Edge_Road_Placeholder_",      "Edges_Road_PlaceholderStyled", "_L" },
            new[] { "Edge_Road_Placeholder_",      "Edges_Road_PlaceholderStyled", "_R" },
        };

        foreach (string[] group in groups)
        {
            string prefix = group[0];
            string parentName = group[1];
            string suffix = group[2];

            Transform groupParent = null;
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.name == parentName)
                {
                    groupParent = go.transform;
                    break;
                }
            }

            if (groupParent == null)
            {
                Debug.LogWarning($"[FixRoadGaps] Parent '{parentName}' not found, skipping prefix '{prefix}'.");
                continue;
            }

            Transform[] segments = new Transform[count];
            int found = 0;
            for (int i = 0; i < count; i++)
            {
                string childName = prefix + i.ToString("D3") + suffix;
                Transform child = groupParent.Find(childName);
                if (child != null)
                {
                    segments[i] = child;
                    found++;
                }
            }

            if (found < 2)
            {
                Debug.LogWarning($"[FixRoadGaps] Only {found} segments found for prefix '{prefix}', skipping.");
                continue;
            }

            Object[] undoTargets = new Object[found];
            int idx = 0;
            for (int i = 0; i < count; i++)
            {
                if (segments[i] != null)
                    undoTargets[idx++] = segments[i];
            }
            Undo.RecordObjects(undoTargets, "Fix Road Placeholder Gaps");

            float halfWidth = 4.8f; // road width 9.6 / 2
            int modified = 0;

            for (int i = 0; i < count; i++)
            {
                if (segments[i] == null) continue;

                float anglePrev = 0f;
                float angleNext = 0f;

                // Find previous valid segment
                Transform prev = null;
                for (int p = i - 1; p >= 0; p--)
                {
                    if (segments[p] != null) { prev = segments[p]; break; }
                }

                // Find next valid segment
                Transform next = null;
                for (int n = i + 1; n < count; n++)
                {
                    if (segments[n] != null) { next = segments[n]; break; }
                }

                Vector3 fwd = Vector3.ProjectOnPlane(segments[i].forward, Vector3.up);
                if (fwd.sqrMagnitude < 0.0001f) continue;
                fwd.Normalize();

                if (prev != null)
                {
                    Vector3 prevFwd = Vector3.ProjectOnPlane(prev.forward, Vector3.up);
                    if (prevFwd.sqrMagnitude > 0.0001f)
                        anglePrev = Vector3.Angle(fwd, prevFwd.normalized);
                }

                if (next != null)
                {
                    Vector3 nextFwd = Vector3.ProjectOnPlane(next.forward, Vector3.up);
                    if (nextFwd.sqrMagnitude > 0.0001f)
                        angleNext = Vector3.Angle(fwd, nextFwd.normalized);
                }

                float tanPrev = Mathf.Tan(anglePrev * 0.5f * Mathf.Deg2Rad);
                float tanNext = Mathf.Tan(angleNext * 0.5f * Mathf.Deg2Rad);
                float extraZ = halfWidth * (tanPrev + tanNext);

                // Add 15% safety margin on the correction to ensure full coverage
                extraZ *= 1.15f;

                if (extraZ < 0.01f) continue;

                Vector3 scale = segments[i].localScale;
                float newZ = scale.z + extraZ;
                segments[i].localScale = new Vector3(scale.x, scale.y, newZ);
                modified++;
            }

            Debug.Log($"[FixRoadGaps] Fixed {modified}/{found} '{prefix}*' segments.");
        }

        Debug.Log("[FixRoadGaps] Done. Remember to save the scene (Ctrl+S).");
    }
}
