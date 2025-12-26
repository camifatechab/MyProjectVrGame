using UnityEngine;

public class CreatureSwirl : MonoBehaviour
{
    public Transform targetPlant;
    public float orbitSpeed = 30f;
    public float orbitRadius = 1.5f;
    public float floatHeight = 0.5f;
    public float floatSpeed = 2f;

    private float angle;

    void Update()
    {
        // If plant is missing or was collected (SetActive(false)), destroy creature
        if (targetPlant == null || !targetPlant.gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }

        angle += orbitSpeed * Time.deltaTime;

        // Orbit path
        float x = Mathf.Cos(angle) * orbitRadius;
        float z = Mathf.Sin(angle) * orbitRadius;
        float y = Mathf.Sin(Time.time * floatSpeed) * 0.2f + floatHeight;

        // Move into position
        transform.position = targetPlant.position + new Vector3(x, y, z);

        // Make the creature face the plant
        transform.LookAt(targetPlant);
    }
}
