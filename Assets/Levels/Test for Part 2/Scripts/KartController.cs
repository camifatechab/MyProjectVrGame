using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class KartController : MonoBehaviour
{
    [Header("Movement")]
    public float acceleration = 20f;
    public float brakeStrength = 10f;
    public float turnSpeed = 80f;
    public float maxSpeed = 15f;

    [Header("Input")]
    public InputActionReference accelerateAction;
    public InputActionReference brakeAction;

    private Rigidbody rb;
    private float accelerationInput;
    private float brakeInput;

    public bool canDrive = false;

    [Header("Audio")]
    public AudioSource engineAudio;
    public float minPitch = 0.8f;
    public float maxPitch = 2f;
    public AudioSource brakeAudio;

    [Header("Steering")]
    public SteeringWheel steeringWheel;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
    }

    void OnEnable()
    {
        accelerateAction.action.Enable();
        brakeAction.action.Enable();
    }

    void OnDisable()
    {
        accelerateAction.action.Disable();
        brakeAction.action.Disable();
    }

    void Update()
    {
        accelerationInput = accelerateAction.action.ReadValue<float>();
        brakeInput = brakeAction.action.ReadValue<float>();
        UpdateEngineSound();
    }

    void FixedUpdate()
    {
        if (!canDrive) return;

        Debug.Log("FixedUpdate running");

        Move();
        Turn();
    }

    void Move()
    {
        if (rb.linearVelocity.magnitude < maxSpeed)
        {
            Vector3 force = transform.forward * accelerationInput * acceleration;
            rb.AddForce(force, ForceMode.Acceleration);
        }

        if (brakeInput > 0.1f)
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, brakeInput * brakeStrength * Time.fixedDeltaTime);
        }

        if (brakeInput > 0.5f && rb.linearVelocity.magnitude > 2f)
        {
            if (!brakeAudio.isPlaying)
                brakeAudio.Play();
        }
    }

    void Turn()
    {
        /*if (steeringWheel == null) return;

        float steerInput = steeringWheel.GetSteeringPercent();

        float turn = steerInput * turnSpeed * Time.fixedDeltaTime;

        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turn, 0f));*/

        /*if (steeringWheel == null)
        {
            Debug.Log("SteeringWheel NOT assigned!");
            return;
        }

        float steerInput = steeringWheel.GetSteeringPercent();
        Debug.Log("Steer Input: " + steerInput);

        float turn = steerInput * turnSpeed * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turn, 0f));*/

        if (steeringWheel == null) return;

        float steerInput = steeringWheel.GetSteeringPercent();

        Vector3 velocity = rb.linearVelocity;

        if (velocity.magnitude > 0.5f)
        {
            //float steerAmount = steerInput * turnSpeed * Time.fixedDeltaTime;
            float speedFactor = rb.linearVelocity.magnitude / maxSpeed;
            float steerAmount = steerInput * turnSpeed * speedFactor * Time.fixedDeltaTime;

            Quaternion turnRotation = Quaternion.Euler(0f, steerAmount, 0f);

            rb.MoveRotation(rb.rotation * turnRotation);
        }
    }

    void UpdateEngineSound()
    {
        if (!canDrive)
        {
            engineAudio.Stop();
            return;
        }

        float speedPercent = rb.linearVelocity.magnitude / maxSpeed;

        if (!engineAudio.isPlaying)
            engineAudio.Play();

        engineAudio.pitch = Mathf.Lerp(minPitch, maxPitch, speedPercent);
    }
}