using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[ExecuteInEditMode]
public class ApplyGroundMaterial : MonoBehaviour
{
    private void Start()
    {
        ApplyMaterial();
    }

    [ContextMenu("Apply Ground Material")]
    public void ApplyMaterial()
    {
        Material groundMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Lake/GroundMaterial.mat");
        if (groundMat != null)
        {
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = groundMat;
                EditorUtility.SetDirty(renderer);
            }
        }
    }
}
#endif
