using UnityEngine;

public class CrystalAudioManager : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip crystalCollectSound;
    [SerializeField] private AudioClip allCrystalsCollectedSound;
    [SerializeField] private AudioClip crystalAmbientHum;
    
    [Header("Audio Settings")]
    [SerializeField] private float collectVolume = 0.7f;
    [SerializeField] private float victoryVolume = 1.0f;
    [SerializeField] private float ambientVolume = 0.3f;
    
    private AudioSource audioSource;
    private AudioSource ambientSource;
    private static CrystalAudioManager instance;
    
    public static CrystalAudioManager Instance => instance;
    
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        
        // Create audio sources
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound
        
        // Create ambient audio source
        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.playOnAwake = false;
        ambientSource.loop = true;
        ambientSource.volume = ambientVolume;
        ambientSource.spatialBlend = 0f; // 2D sound
    }
    
    void Start()
    {
        // Subscribe to crystal collection events
        if (CrystalCollectionSystem.Instance != null)
        {
            CrystalCollectionSystem.Instance.OnCrystalCollected.AddListener(OnCrystalCollected);
            CrystalCollectionSystem.Instance.OnAllCrystalsCollected.AddListener(OnAllCrystalsCollected);
        }
        
        // Start ambient hum if available
        if (crystalAmbientHum != null)
        {
            ambientSource.clip = crystalAmbientHum;
            ambientSource.Play();
        }
    }
    
    private void OnCrystalCollected(int current, int total)
    {
        PlayCollectSound();
    }
    
    private void OnAllCrystalsCollected()
    {
        PlayVictorySound();
        
        // Stop ambient sound
        if (ambientSource.isPlaying)
        {
            ambientSource.Stop();
        }
    }
    
    public void PlayCollectSound()
    {
        if (crystalCollectSound != null)
        {
            audioSource.PlayOneShot(crystalCollectSound, collectVolume);
            Debug.Log("Playing crystal collection sound");
        }
        else
        {
            Debug.LogWarning("Crystal collect sound not assigned!");
        }
    }
    
    public void PlayVictorySound()
    {
        if (allCrystalsCollectedSound != null)
        {
            audioSource.PlayOneShot(allCrystalsCollectedSound, victoryVolume);
            Debug.Log("Playing victory sound");
        }
        else
        {
            Debug.LogWarning("Victory sound not assigned!");
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (CrystalCollectionSystem.Instance != null)
        {
            CrystalCollectionSystem.Instance.OnCrystalCollected.RemoveListener(OnCrystalCollected);
            CrystalCollectionSystem.Instance.OnAllCrystalsCollected.RemoveListener(OnAllCrystalsCollected);
        }
    }
}
