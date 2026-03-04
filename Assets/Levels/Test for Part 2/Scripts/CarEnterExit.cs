using UnityEngine;

using UnityEngine.InputSystem;



public class CarEnterExit : MonoBehaviour

{

    public Transform seatPoint;

    public GameObject xrOrigin;

    public Behaviour moveProvider;

    public Behaviour turnProvider;



    public InputActionReference toggleVehicleAction;



    private bool isSeated = false;



    public KartController kartController;



    private bool playerInRange = false;



    void OnEnable()

    {

        toggleVehicleAction.action.Enable();

        toggleVehicleAction.action.performed += OnToggleVehicle;

    }



    void OnDisable()

    {

        toggleVehicleAction.action.performed -= OnToggleVehicle;

        toggleVehicleAction.action.Disable();

    }



    /*private void OnToggleVehicle(InputAction.CallbackContext context)

    {

        if (!isSeated)

            EnterCar();

        else

            ExitCar();

    }*/



    private void OnToggleVehicle(InputAction.CallbackContext context)

    {

        if (!isSeated && playerInRange)

        {

            EnterCar();

        }

        else if (isSeated)

        {

            ExitCar();

        }

    }



    void EnterCar()

    {

        xrOrigin.transform.SetParent(seatPoint);

        xrOrigin.transform.position = seatPoint.position;

        xrOrigin.transform.rotation = seatPoint.rotation;



        moveProvider.enabled = false;

        turnProvider.enabled = false;

        kartController.canDrive = true;



        isSeated = true;

    }



    void ExitCar()

    {

        // Unparent XR Origin

        xrOrigin.transform.SetParent(null);



        // Move player slightly beside car

        xrOrigin.transform.position = transform.position + transform.right * 2f;



        moveProvider.enabled = true;

        turnProvider.enabled = true;

        kartController.canDrive = false;



        isSeated = false;

    }



    void OnTriggerEnter(Collider other)

    {

        if (other.CompareTag("Player"))

        {

            playerInRange = true;

        }

    }



    void OnTriggerExit(Collider other)

    {

        if (other.CompareTag("Player"))

        {

            playerInRange = false;

        }

    }

}