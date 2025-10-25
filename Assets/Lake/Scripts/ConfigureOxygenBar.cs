using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Editor helper to configure Image as Filled type
/// </summary>
public class ConfigureOxygenBar : MonoBehaviour
{
    [ContextMenu("Configure as Filled Bar")]
    public void ConfigureBar()
    {
        Image img = GetComponent<Image>();
        if (img != null)
        {
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillAmount = 1.0f;
            Debug.Log("Image configured as Filled Horizontal bar!");
        }
    }
}
#endif
