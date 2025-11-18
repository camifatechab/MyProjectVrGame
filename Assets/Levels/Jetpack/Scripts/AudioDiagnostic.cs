using UnityEngine;
using UnityEngine.Audio;

public class AudioDiagnostic : MonoBehaviour
{
    void Start()
    {
        Debug.Log("<color=yellow>═══════════════════════════════════════</color>");
        Debug.Log("<color=yellow>      AUDIO DIAGNOSTIC REPORT</color>");
        Debug.Log("<color=yellow>═══════════════════════════════════════</color>");
        
        // Check AudioListener
        AudioListener[] listeners = FindObjectsOfType<AudioListener>();
        if (listeners.Length == 0)
        {
            Debug.LogError("<color=red>❌ NO AUDIOLISTENER IN SCENE! Audio won't work!</color>");
        }
        else if (listeners.Length > 1)
        {
            Debug.LogWarning($"<color=orange>⚠ MULTIPLE AudioListeners ({listeners.Length}) - May cause issues!</color>");
        }
        else
        {
            Debug.Log($"<color=green>✓ AudioListener found on: {listeners[0].gameObject.name}</color>");
        }
        
        // Check Audio Settings
        AudioConfiguration config = AudioSettings.GetConfiguration();
        Debug.Log($"<color=cyan>Sample Rate: {config.sampleRate} Hz</color>");
        Debug.Log($"<color=cyan>Speaker Mode: {config.speakerMode}</color>");
        Debug.Log($"<color=cyan>DSP Buffer Size: {config.dspBufferSize}</color>");
        
        // Check if audio is muted
        Debug.Log($"<color=cyan>Audio Volume: {AudioListener.volume}</color>");
        if (AudioListener.volume == 0)
        {
            Debug.LogError("<color=red>❌ AUDIO VOLUME IS 0! Set AudioListener.volume to 1!</color>");
        }
        
        // Test loading the gem sound
        AudioClip gemSound = Resources.Load<AudioClip>("GemDropTreasure");
        if (gemSound == null)
        {
            Debug.LogWarning("<color=orange>⚠ Could not load gem sound from Resources</color>");
            // Try direct path
            gemSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Jetpack/Assets/GemDropTreasure.wav");
            if (gemSound != null)
            {
                Debug.Log($"<color=green>✓ Found gem sound at direct path</color>");
                Debug.Log($"<color=cyan>  Name: {gemSound.name}</color>");
                Debug.Log($"<color=cyan>  Length: {gemSound.length:F2}s</color>");
                Debug.Log($"<color=cyan>  Frequency: {gemSound.frequency} Hz</color>");
                Debug.Log($"<color=cyan>  Channels: {gemSound.channels}</color>");
                
                // Test playing it
                Debug.Log("<color=yellow>Playing test sound in 2 seconds...</color>");
                Invoke("PlayTestSound", 2f);
            }
            else
            {
                Debug.LogError("<color=red>❌ Could not find GemDropTreasure.wav!</color>");
            }
        }
        
        Debug.Log("<color=yellow>═══════════════════════════════════════</color>");
        Debug.Log("<color=yellow>If you see ✓ marks but hear nothing:</color>");
        Debug.Log("<color=yellow>1. Check Windows Volume Mixer - is Unity muted?</color>");
        Debug.Log("<color=yellow>2. Check Game tab speaker icon (top-right)</color>");
        Debug.Log("<color=yellow>3. Check VR headset audio settings</color>");
        Debug.Log("<color=yellow>4. Try different audio output device</color>");
        Debug.Log("<color=yellow>═══════════════════════════════════════</color>");
    }
    
    void PlayTestSound()
    {
        AudioClip gemSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Jetpack/Assets/GemDropTreasure.wav");
        if (gemSound != null)
        {
            AudioSource.PlayClipAtPoint(gemSound, Camera.main.transform.position, 1f);
            Debug.Log("<color=cyan>★★★ PLAYING TEST SOUND NOW! ★★★</color>");
            Debug.Log("<color=cyan>If you don't hear anything, it's 100% an audio output issue!</color>");
        }
    }
}
