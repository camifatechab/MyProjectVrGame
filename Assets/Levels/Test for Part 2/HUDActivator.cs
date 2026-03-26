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
        hud.localPosition = new Vector3(0f, -0.12f, 0.5f);
        hud.localRotation = Quaternion.identity;
        hud.localScale    = Vector3.one * 0.0008f;
    }
}
