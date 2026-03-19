using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class VehicleEntryHandler : MonoBehaviour
{
    [Header("References")]
    public GameObject xrOrigin;
    public Transform seatLocation;
    public MonoBehaviour locomotionProvider; // Drag your Continuous Move Provider here

    private bool isInCar = false;
    private Vector3 originalParentScale;

    // This method will be called by the "B" button event
    public void ToggleVehicleState()
    {
        if (!isInCar)
            EnterCar();
        else
            ExitCar();
    }

    void EnterCar()
    {
        isInCar = true;

        // 1. Disable walking so the player doesn't slide out of the car
        locomotionProvider.enabled = false;

        // 2. Parent the XR Origin to the car so it moves with the physics
        xrOrigin.transform.SetParent(transform);

        // 3. Snap player to the seat
        xrOrigin.transform.localPosition = seatLocation.localPosition;
        xrOrigin.transform.localRotation = seatLocation.localRotation;
    }

    void ExitCar()
    {
        isInCar = false;

        // 1. Unparent from the car
        xrOrigin.transform.SetParent(null);

        // 2. Move player slightly to the side so they don't clip through the car
        xrOrigin.transform.position += transform.right * 1.5f;

        // 3. Re-enable walking
        locomotionProvider.enabled = true;
    }
}