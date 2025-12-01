using UnityEngine;

[ExecuteInEditMode]
public class SetupPlayerCollider : MonoBehaviour
{
    private void Start()
    {
        ConfigureCharacterController();
    }

    [ContextMenu("Configure Character Controller")]
    public void ConfigureCharacterController()
    {
        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            // Proper capsule for standing player
            controller.radius = 0.3f;
            controller.height = 1.8f;
            controller.center = new Vector3(0f, 0.9f, 0f); // Center at waist height
            controller.detectCollisions = true;
            controller.enableOverlapRecovery = true;
            
            Debug.Log("CharacterController configured for land collision");
        }
    }
}
