using UnityEngine;

public class PlayerSpeedController : MonoBehaviour
{
    public float normalSpeed = 12f;
    public float boostSpeed = 20f;
    public float currentSpeed;

    private float slowTimer = 0f;
    private float slowMultiplier = 1f;

    void Start()
    {
        currentSpeed = normalSpeed;
    }

    void Update()
    {
        // Handle slow timer
        if (slowTimer > 0)
        {
            slowTimer -= Time.deltaTime;

            if (slowTimer <= 0)
                slowMultiplier = 1f; // reset slow
        }

        // Example move
        float input = Input.GetAxis("Vertical");
        Vector3 move = transform.forward * input * (currentSpeed * slowMultiplier);
        transform.position += move * Time.deltaTime;
    }

    public void ApplySlow(float multiplier, float duration)
    {
        slowMultiplier = multiplier;
        slowTimer = duration;
    }
}
