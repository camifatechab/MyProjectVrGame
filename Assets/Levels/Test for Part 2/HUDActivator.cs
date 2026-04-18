using UnityEngine;

/// Activates and positions the player HUD canvases under Main Camera at runtime.
[DefaultExecutionOrder(-20)]
public class HUDActivator : MonoBehaviour
{
    private static readonly Vector3 PlayerHudPosition = new(-0.24f, -0.03f, 1.02f);
    private static readonly Vector3 PlayerHudScale = Vector3.one * 0.00042f;

    private void Awake()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        ActivateCanvas(cam.transform.Find("PlayerHUDCanvas"), PlayerHudPosition, Quaternion.identity, PlayerHudScale);
        ActivateCanvas(cam.transform.Find("VR UI Canvas"), new Vector3(-0.078f, -0.014f, 0.62f), Quaternion.Euler(12f, 0f, 0f), Vector3.one * 0.001f);
    }

    private static void ActivateCanvas(Transform canvasTransform, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
    {
        if (canvasTransform == null)
            return;

        canvasTransform.gameObject.SetActive(true);
        canvasTransform.localPosition = localPosition;
        canvasTransform.localRotation = localRotation;
        canvasTransform.localScale = localScale;
    }
}
