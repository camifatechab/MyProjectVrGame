using UnityEngine;

/// Attach to any GameObject in the scene (e.g. XR Origin).
/// Disables dragon-dragon physics collisions at runtime.
public class DragonLayerSetup : MonoBehaviour
{
    void Awake()
    {
        int dragonLayer = LayerMask.NameToLayer("Dragon");
        if (dragonLayer >= 0)
            Physics.IgnoreLayerCollision(dragonLayer, dragonLayer, true);
    }
}
