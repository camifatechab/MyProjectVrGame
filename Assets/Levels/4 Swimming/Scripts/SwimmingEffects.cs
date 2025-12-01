using UnityEngine;

public class SwimmingEffects : MonoBehaviour
{
    [Header("Particle References")]
    [SerializeField] private ParticleSystem bubbleParticles;
    [SerializeField] private ParticleSystem splashParticles;
    [SerializeField] private ParticleSystem boostParticles;
    
    [Header("Swimming References")]
    [SerializeField] private SimpleSwimmingMovement keyboardSwimming;
    [SerializeField] private SwimmingLocomotion vrSwimming;
    
    [Header("Effect Settings")]
    [SerializeField] private float speedThresholdForBubbles = 2f;
    [SerializeField] private float surfaceDetectionY = 10f;
    [SerializeField] private float surfaceThreshold = 0.3f;
    
    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource waterMovementAudio;
    [SerializeField] private AudioSource splashAudio;
    [SerializeField] private AudioSource boostAudio;
    
    private bool wasNearSurface = false;
    private bool wasBoostActive = false;
    
    private void Update()
    {
        UpdateEffects();
    }
    
    private void UpdateEffects()
    {
        float currentSpeed = GetCurrentSpeed();
        bool isNearSurface = IsNearSurface();
        bool isBoostActive = IsBoostActive();
        
        // Handle bubble particles for underwater movement
        if (bubbleParticles != null)
        {
            if (!isNearSurface && currentSpeed > speedThresholdForBubbles)
            {
                if (!bubbleParticles.isPlaying)
                {
                    bubbleParticles.Play();
                }
                
                // Scale emission rate based on speed
                var emission = bubbleParticles.emission;
                emission.rateOverTime = Mathf.Lerp(10f, 50f, currentSpeed / 10f);
            }
            else
            {
                if (bubbleParticles.isPlaying)
                {
                    bubbleParticles.Stop();
                }
            }
        }
        
        // Handle splash particles when breaking surface
        if (splashParticles != null)
        {
            if (isNearSurface && !wasNearSurface && currentSpeed > 1f)
            {
                splashParticles.Play();
                
                if (splashAudio != null && !splashAudio.isPlaying)
                {
                    splashAudio.Play();
                }
            }
        }
        wasNearSurface = isNearSurface;
        
        // Handle boost particles
        if (boostParticles != null)
        {
            if (isBoostActive && !wasBoostActive)
            {
                boostParticles.Play();
                
                if (boostAudio != null)
                {
                    boostAudio.Play();
                }
            }
        }
        wasBoostActive = isBoostActive;
        
        // Handle water movement audio
        if (waterMovementAudio != null)
        {
            if (currentSpeed > 0.5f)
            {
                if (!waterMovementAudio.isPlaying)
                {
                    waterMovementAudio.Play();
                }
                
                // Adjust volume based on speed
                waterMovementAudio.volume = Mathf.Lerp(0.1f, 0.8f, currentSpeed / 10f);
            }
            else
            {
                if (waterMovementAudio.isPlaying)
                {
                    waterMovementAudio.Stop();
                }
            }
        }
    }
    
    private float GetCurrentSpeed()
    {
        if (keyboardSwimming != null)
        {
            return keyboardSwimming.GetCurrentSpeed();
        }
        else if (vrSwimming != null)
        {
            return vrSwimming.GetCurrentSpeed();
        }
        return 0f;
    }
    
    private bool IsNearSurface()
    {
        float distanceToSurface = Mathf.Abs(transform.position.y - surfaceDetectionY);
        return distanceToSurface < surfaceThreshold;
    }
    
    private bool IsBoostActive()
    {
        if (keyboardSwimming != null)
        {
            return keyboardSwimming.IsBoostActive();
        }
        else if (vrSwimming != null)
        {
            return vrSwimming.IsBoostActive();
        }
        return false;
    }
    
    private void OnValidate()
    {
        // Try to auto-find swimming controllers
        if (keyboardSwimming == null)
        {
            keyboardSwimming = GetComponent<SimpleSwimmingMovement>();
        }
        
        if (vrSwimming == null)
        {
            vrSwimming = GetComponent<SwimmingLocomotion>();
        }
    }
}
