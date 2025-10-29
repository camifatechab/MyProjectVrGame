using UnityEngine;

public class AudioTester : MonoBehaviour
{
    [Header("Audio Test Settings")]
    [Tooltip("Drag GemDropTreasure.wav here from Assets/Jetpack/Assets/")]
    public AudioClip testSound;
    
    [Tooltip("Volume for test (0-1)")]
    [Range(0f, 1f)]
    public float volume = 1f;
    
    private AudioSource audioSource;
    
    void Start()
    {
        // Create audio source
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = volume;
        
        Debug.Log("<color=yellow>╔════════════════════════════════════════════╗</color>");
        Debug.Log("<color=yellow>║   AUDIO TESTER READY                       ║</color>");
        Debug.Log("<color=yellow>║   Press SPACEBAR to test sound             ║</color>");
        Debug.Log("<color=yellow>║   Press P to play in VR                    ║</color>");
        Debug.Log("<color=yellow>╚════════════════════════════════════════════╝</color>");
        
        if (testSound == null)
        {
            Debug.LogError("<color=red>❌ NO SOUND ASSIGNED! Drag GemDropTreasure.wav into 'Test Sound' field in Inspector!</color>");
        }
        else
        {
            Debug.Log($"<color=green>✓ Sound loaded: {testSound.name} (Length: {testSound.length:F2}s)</color>");
        }
    }
    
    void Update()
    {
        // Test with SPACEBAR (works in Unity Editor)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PlayTestSound("SPACEBAR");
        }
        
        // Test with P key (works anywhere)
        if (Input.GetKeyDown(KeyCode.P))
        {
            PlayTestSound("P KEY");
        }
    }
    
    void PlayTestSound(string trigger)
    {
        if (testSound == null)
        {
            Debug.LogError("<color=red>❌ Cannot play sound - no AudioClip assigned!</color>");
            return;
        }
        
        if (audioSource == null)
        {
            Debug.LogError("<color=red>❌ AudioSource is missing!</color>");
            return;
        }
        
        // Play the sound
        audioSource.PlayOneShot(testSound, volume);
        
        // Visual feedback
        Debug.Log($"<color=cyan>★★★ SOUND PLAYING! ★★★ Triggered by: {trigger}</color>");
        Debug.Log($"<color=cyan>Volume: {volume} | Clip: {testSound.name} | Length: {testSound.length:F2}s</color>");
        Debug.Log($"<color=cyan>If you don't hear anything, check:</color>");
        Debug.Log($"<color=cyan>  1. Computer volume (Windows mixer/Mac volume)</color>");
        Debug.Log($"<color=cyan>  2. Unity Game tab speaker icon (not muted)</color>");
        Debug.Log($"<color=cyan>  3. VR headset audio settings</color>");
    }
}
