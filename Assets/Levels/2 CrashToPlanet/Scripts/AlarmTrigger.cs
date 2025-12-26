using UnityEngine;

public class AlarmTrigger : MonoBehaviour
{
    public AlarmLightController alarmController;

    private void OnTriggerEnter(Collider other)
    {
        // Replace "Player" with your XR rig tag if needed
        if (other.CompareTag("Player"))
        {
            alarmController.ActivateAlarms();

            Debug.Log("Activate Alarms");
        }
    }

    /*private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            alarmController.DeactivateAlarms();
        }
    }*/
}

