using UnityEngine;

/// <summary>
/// Manages all audio for crystal collectibles.
/// Integrates directly with CrystalManager (not CrystalCollectionSystem).
/// Features: collection, proximity hum, spawn, pulse, combo system, and victory fanfare.
/// </summary>
public class CrystalAudioManager : MonoBehaviour
{
    [Header("=== AUDIO CLIPS ===")]
    [Tooltip("Sound when collecting crystals 1-4")]
    public AudioClip collectClip;
    
    [Tooltip("Looping hum for nearby crystals")]
    public AudioClip nearbyHumClip;
    
    [Tooltip("Sound when crystal spawns/appears")]
    public AudioClip spawnClip;
    
    [Tooltip("Looping pulse/glow sound for crystals")]
    public AudioClip pulseClip;
    
    [Tooltip("Sound for rapid combo collection")]
    public AudioClip comboClip;
    
    [Tooltip("Victory sound when 5th/final crystal collected")]
    public AudioClip allCollectedClip;
    
    [Header("=== COLLECTION SOUND ===")]
    [Range(0f, 1f)]
    public float collectVolume = 0.8f;
    public float collectPitchMin = 0.95f;
    public float collectPitchMax = 1.1f;
    
    [Header("=== PROXIMITY HUM ===")]
    public float humMaxDistance = 15f;
    public float humMinDistance = 2f;
    [Range(0f, 1f)]
    public float humMaxVolume = 0.4f;
    public float humPitchMin = 0.8f;
    public float humPitchMax = 1.2f;
    
    [Header("=== PULSE/GLOW SOUND ===")]
    [Range(0f, 1f)]
    public float pulseVolume = 0.1f;
    public bool pulseDynamicVolume = true;
    
    [Header("=== SPAWN SOUND ===")]
    [Range(0f, 1f)]
    public float spawnVolume = 0.7f;
    
    [Header("=== COMBO SYSTEM ===")]
    [Tooltip("Time window to collect another crystal for combo (seconds)")]
    public float comboWindow = 15f;
    [Tooltip("Minimum combo count to trigger combo sound")]
    public int comboThreshold = 2;
    [Range(0f, 1f)]
    public float comboVolume = 0.9f;
    public float comboPitchBase = 1f;
    public float comboPitchIncrement = 0.1f;
    public float comboPitchMax = 1.5f;
    
    [Header("=== VICTORY SOUND ===")]
    [Range(0f, 1f)]
    public float victoryVolume = 1f;
    
    [Header("=== REFERENCES ===")]
    public Transform playerTransform;
    
    // Audio sources
    private AudioSource collectSource;
    private AudioSource humSource;
    private AudioSource pulseSource;
    private AudioSource comboSource;
    private AudioSource victorySource;
    
    // Crystal tracking
    private CrystalCollectible[] allCrystals;
    private int lastKnownCollectedCount = 0;
    private bool hasPlayedVictory = false;
    
    // Combo tracking
    private int comboCount = 0;
    private float lastCollectTime = -999f;
    
    private void Start()
    {
        // Find player if not assigned
        if (playerTransform == null)
        {
            var xrOrigin = FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null)
                playerTransform = xrOrigin.Camera?.transform;
            else
                playerTransform = Camera.main?.transform;
        }
        
        CreateAudioSources();
        RefreshCrystalReferences();
        
        Debug.Log($"CrystalAudioManager: Initialized. Polling CrystalManager for collection events.");
    }
    
    private void CreateAudioSources()
    {
        // Collection sound source
        GameObject collectObj = new GameObject("CrystalCollectAudio");
        collectObj.transform.SetParent(transform);
        collectSource = collectObj.AddComponent<AudioSource>();
        collectSource.playOnAwake = false;
        collectSource.spatialBlend = 0f;
        collectSource.priority = 64;
        
        // Proximity hum source (looping)
        GameObject humObj = new GameObject("CrystalHumAudio");
        humObj.transform.SetParent(transform);
        humSource = humObj.AddComponent<AudioSource>();
        humSource.clip = nearbyHumClip;
        humSource.loop = true;
        humSource.playOnAwake = false;
        humSource.spatialBlend = 0f;
        humSource.volume = 0f;
        humSource.priority = 128;
        
        // Pulse source (looping)
        GameObject pulseObj = new GameObject("CrystalPulseAudio");
        pulseObj.transform.SetParent(transform);
        pulseSource = pulseObj.AddComponent<AudioSource>();
        pulseSource.clip = pulseClip;
        pulseSource.loop = true;
        pulseSource.playOnAwake = false;
        pulseSource.spatialBlend = 0f;
        pulseSource.volume = 0f;
        pulseSource.priority = 140;
        
        // Combo source
        GameObject comboObj = new GameObject("CrystalComboAudio");
        comboObj.transform.SetParent(transform);
        comboSource = comboObj.AddComponent<AudioSource>();
        comboSource.playOnAwake = false;
        comboSource.spatialBlend = 0f;
        comboSource.priority = 48;
        
        // Victory source
        GameObject victoryObj = new GameObject("CrystalVictoryAudio");
        victoryObj.transform.SetParent(transform);
        victorySource = victoryObj.AddComponent<AudioSource>();
        victorySource.playOnAwake = false;
        victorySource.spatialBlend = 0f;
        victorySource.priority = 32;
        
        // Start looping sounds (at zero volume)
        if (nearbyHumClip != null)
            humSource.Play();
        
        if (pulseClip != null)
            pulseSource.Play();
    }
    
    private void RefreshCrystalReferences()
    {
        allCrystals = FindObjectsByType<CrystalCollectible>(FindObjectsSortMode.None);
    }
    
    private void Update()
    {
        UpdateProximityHum();
        UpdatePulseSound();
        UpdateComboTimer();
        
        // Poll CrystalManager for collection changes
        PollCrystalManager();
    }
    
    /// <summary>
    /// Poll CrystalManager to detect when crystals are collected.
    /// This is the KEY integration point since CrystalManager doesn't use UnityEvents.
    /// </summary>
    private void PollCrystalManager()
    {
        if (CrystalManager.Instance == null) return;
        
        int currentCollected = CrystalManager.Instance.CollectedCrystals;
        int total = CrystalManager.Instance.TotalCrystals;
        
        // Detect new collection
        if (currentCollected > lastKnownCollectedCount)
        {
            int crystalsJustCollected = currentCollected - lastKnownCollectedCount;
            
            for (int i = 0; i < crystalsJustCollected; i++)
            {
                int crystalNumber = lastKnownCollectedCount + i + 1;
                OnCrystalCollected(crystalNumber, total);
            }
            
            lastKnownCollectedCount = currentCollected;
            RefreshCrystalReferences();
        }
    }
    
    private void OnCrystalCollected(int crystalNumber, int total)
    {
        Debug.Log($"CrystalAudioManager: Crystal {crystalNumber}/{total} collected!");
        
        // Update combo
        if (Time.time - lastCollectTime <= comboWindow)
        {
            comboCount++;
        }
        else
        {
            comboCount = 1;
        }
        lastCollectTime = Time.time;
        
        // Check if this is the LAST crystal (5th of 5)
        if (crystalNumber >= total)
        {
            Debug.Log($"CrystalAudioManager: FINAL crystal #{crystalNumber} collected! Playing victory sound!");
            PlayVictorySound();
            
            // Stop ambient sounds
            if (humSource != null) humSource.Stop();
            if (pulseSource != null) pulseSource.Stop();
            
            hasPlayedVictory = true;
            return;
        }
        
        // For crystals 1-4: play collect or combo sound
        if (comboCount >= comboThreshold && comboClip != null)
        {
            Debug.Log($"CrystalAudioManager: Combo x{comboCount}!");
            PlayComboSound();
        }
        else
        {
            PlayCollectSound();
        }
    }
    
    private void UpdateProximityHum()
    {
        if (playerTransform == null || humSource == null || nearbyHumClip == null) return;
        
        float nearestDistance = float.MaxValue;
        
        if (allCrystals != null)
        {
            foreach (var crystal in allCrystals)
            {
                if (crystal != null && crystal.gameObject.activeInHierarchy)
                {
                    float dist = Vector3.Distance(playerTransform.position, crystal.transform.position);
                    if (dist < nearestDistance)
                        nearestDistance = dist;
                }
            }
        }
        
        float targetVolume = 0f;
        float targetPitch = humPitchMin;
        
        if (nearestDistance < humMaxDistance)
        {
            float proximity = 1f - Mathf.InverseLerp(humMinDistance, humMaxDistance, nearestDistance);
            proximity = Mathf.Clamp01(proximity);
            proximity = Mathf.SmoothStep(0f, 1f, proximity);
            
            targetVolume = humMaxVolume * proximity;
            targetPitch = Mathf.Lerp(humPitchMin, humPitchMax, proximity);
        }
        
        // Smooth transitions
        humSource.volume = Mathf.Lerp(humSource.volume, targetVolume, Time.deltaTime * 0.8f);
        humSource.pitch = Mathf.Lerp(humSource.pitch, targetPitch, Time.deltaTime * 0.8f);
    }
    
    private void UpdatePulseSound()
    {
        if (playerTransform == null || pulseSource == null || pulseClip == null) return;
        
        int activeCrystals = 0;
        float nearestDistance = float.MaxValue;
        
        if (allCrystals != null)
        {
            foreach (var crystal in allCrystals)
            {
                if (crystal != null && crystal.gameObject.activeInHierarchy)
                {
                    activeCrystals++;
                    float dist = Vector3.Distance(playerTransform.position, crystal.transform.position);
                    if (dist < nearestDistance)
                        nearestDistance = dist;
                }
            }
        }
        
        float targetVolume = 0f;
        
        if (activeCrystals > 0)
        {
            if (pulseDynamicVolume && nearestDistance < humMaxDistance)
            {
                float proximity = 1f - Mathf.InverseLerp(humMinDistance, humMaxDistance, nearestDistance);
                targetVolume = pulseVolume * Mathf.Lerp(0.3f, 1f, proximity);
            }
            else
            {
                targetVolume = pulseVolume * 0.5f;
            }
        }
        
        pulseSource.volume = Mathf.Lerp(pulseSource.volume, targetVolume, Time.deltaTime * 0.8f);
    }
    
    private void UpdateComboTimer()
    {
        if (comboCount > 0 && Time.time - lastCollectTime > comboWindow)
        {
            comboCount = 0;
        }
    }
    
    public void PlayCollectSound()
    {
        if (collectSource == null || collectClip == null) return;
        
        collectSource.pitch = Random.Range(collectPitchMin, collectPitchMax);
        collectSource.PlayOneShot(collectClip, collectVolume);
    }
    
    public void PlayComboSound()
    {
        if (comboSource == null || comboClip == null) return;
        
        float pitch = Mathf.Min(comboPitchBase + (comboCount - comboThreshold) * comboPitchIncrement, comboPitchMax);
        comboSource.pitch = pitch;
        comboSource.PlayOneShot(comboClip, comboVolume);
    }
    
    public void PlaySpawnSound()
    {
        if (spawnClip == null) return;
        AudioSource.PlayClipAtPoint(spawnClip, playerTransform != null ? playerTransform.position : transform.position, spawnVolume);
    }
    
    public void PlaySpawnSoundAt(Vector3 position)
    {
        if (spawnClip == null) return;
        AudioSource.PlayClipAtPoint(spawnClip, position, spawnVolume);
    }
    
    public void PlayVictorySound()
    {
        if (victorySource == null || allCollectedClip == null)
        {
            Debug.LogWarning("CrystalAudioManager: Cannot play victory sound - victorySource or allCollectedClip is null!");
            return;
        }
        
        victorySource.PlayOneShot(allCollectedClip, victoryVolume);
        Debug.Log("CrystalAudioManager: *** VICTORY SOUND PLAYING ***");
    }
    
    public int GetComboCount() => comboCount;
    
    public void RefreshCrystals()
    {
        RefreshCrystalReferences();
    }
}
