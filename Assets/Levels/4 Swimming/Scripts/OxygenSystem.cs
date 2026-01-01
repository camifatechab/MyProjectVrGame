using UnityEngine;
using System;

namespace Lake
{
    public class OxygenSystem : MonoBehaviour
    {
        [Header("Oxygen Settings")]
        [SerializeField] private float maxOxygen = 60f;
        [SerializeField] private float baseConsumptionRate = 1f;
        [SerializeField] private float regenerationRate = 10f;
        [SerializeField] private float lowOxygenThreshold = 25f;
        [SerializeField] private float criticalOxygenThreshold = 15f;

        [Header("Depth Pressure System")]
        [SerializeField] private float pressureStartDepth = 5f;
        [SerializeField] private float pressureMultiplierMid = 1.5f;
        [SerializeField] private float pressureMaxDepth = 15f;
        [SerializeField] private float pressureMultiplierMax = 2.5f;

        [Header("Forced Ascent")]
        [SerializeField] private float forcedAscentSpeed = 2f;
        [SerializeField] private float depletedMovementMultiplier = 0.3f;

        [Header("Audio")]
        [SerializeField] private AudioClip calmBreathingClip;
        [SerializeField] private AudioClip heavyBreathingClip;
        [SerializeField] private AudioClip gaspingClip;
        [SerializeField] private AudioClip surfaceGaspClip;
        private AudioSource audioSource;

        [Header("References")]
        [SerializeField] private Transform playerHead;

        // State
        private float currentOxygen;
        private float waterSurfaceY;
        private bool isUnderwater;
        private bool wasLowOxygen;
        private bool wasCriticalOxygen;
        private bool wasDepleted;

        // Events
        public event Action OnLowOxygenWarning;
        public event Action OnCriticalOxygenWarning;
        public event Action OnOxygenDepleted;
        public event Action OnOxygenRestored;
        public event Action OnSurfaced;

        // Public properties
        public float CurrentOxygen => currentOxygen;
        public float MaxOxygen => maxOxygen;
        public float OxygenPercent => currentOxygen / maxOxygen;
        public bool IsUnderwater => isUnderwater;
        public bool IsLowOxygen => OxygenPercent <= lowOxygenThreshold / 100f;
        public bool IsCriticalOxygen => OxygenPercent <= criticalOxygenThreshold / 100f;
        public bool IsDepleted => currentOxygen <= 0f;
        public float CurrentDepth => isUnderwater ? Mathf.Max(0, waterSurfaceY - playerHead.position.y) : 0f;
        public float CurrentPressureMultiplier => GetPressureMultiplier();

        private void Awake()
        {
            currentOxygen = maxOxygen;
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            if (playerHead == null)
            {
                var cam = Camera.main;
                if (cam != null) playerHead = cam.transform;
            }
        }

        private void Update()
        {
            if (isUnderwater)
            {
                ConsumeOxygen();
                CheckWarnings();
            }
            else if (currentOxygen < maxOxygen)
            {
                RegenerateOxygen();
            }
        }

        public void EnterWater(float surfaceY)
        {
            waterSurfaceY = surfaceY;
            isUnderwater = true;
        }

        public void ExitWater()
        {
            isUnderwater = false;
            
            if (wasDepleted || wasCriticalOxygen)
            {
                OnSurfaced?.Invoke();
                PlaySurfaceGasp();
            }
            
            wasLowOxygen = false;
            wasCriticalOxygen = false;
            wasDepleted = false;
        }

        private void ConsumeOxygen()
        {
            float multiplier = GetPressureMultiplier();
            float consumption = baseConsumptionRate * multiplier * Time.deltaTime;
            currentOxygen = Mathf.Max(0, currentOxygen - consumption);
        }

        private void RegenerateOxygen()
        {
            currentOxygen = Mathf.Min(maxOxygen, currentOxygen + regenerationRate * Time.deltaTime);
            
            if (wasDepleted && currentOxygen > 0)
            {
                wasDepleted = false;
                OnOxygenRestored?.Invoke();
            }
        }

        private float GetPressureMultiplier()
        {
            if (!isUnderwater) return 1f;

            float depth = CurrentDepth;
            if (depth < pressureStartDepth) return 1f;
            if (depth >= pressureMaxDepth) return pressureMultiplierMax;

            float t = (depth - pressureStartDepth) / (pressureMaxDepth - pressureStartDepth);
            return Mathf.Lerp(1f, pressureMultiplierMax, t);
        }

        private void CheckWarnings()
        {
            if (IsDepleted && !wasDepleted)
            {
                wasDepleted = true;
                OnOxygenDepleted?.Invoke();
                PulseHaptics(1f, 0.5f);
            }
            else if (IsCriticalOxygen && !wasCriticalOxygen)
            {
                wasCriticalOxygen = true;
                OnCriticalOxygenWarning?.Invoke();
                PulseHaptics(0.8f, 0.3f);
            }
            else if (IsLowOxygen && !wasLowOxygen)
            {
                wasLowOxygen = true;
                OnLowOxygenWarning?.Invoke();
                PulseHaptics(0.5f, 0.2f);
            }
        }

        private void PulseHaptics(float intensity, float duration)
        {
            if (HapticsManager.Instance != null)
            {
                HapticsManager.Instance.Pulse(intensity, duration);
            }
        }

        private void PlaySurfaceGasp()
        {
            if (surfaceGaspClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(surfaceGaspClip);
            }
        }
    }
}
