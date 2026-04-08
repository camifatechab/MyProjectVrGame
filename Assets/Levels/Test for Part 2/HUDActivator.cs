using UnityEngine;

/// Activates and positions the PlayerHUDCanvas under Main Camera at runtime.
[DefaultExecutionOrder(-20)]
public class HUDActivator : MonoBehaviour
{
    void Awake()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Transform hud = cam.transform.Find("PlayerHUDCanvas");
        if (hud == null) return;

        hud.gameObject.SetActive(true);
        hud.localPosition = new Vector3(-0.24f, -0.03f, 1.02f);
        hud.localRotation = Quaternion.identity;
        hud.localScale    = Vector3.one * 0.00042f;
    }
}
