using UnityEngine;

/// Assigns jetpack audio clips at runtime from the Jetpack level's audio folder.
/// Attach to XR Origin alongside JetpackAudioManager.
/// Uses AssetDatabase in editor and pre-cached guids at runtime.
[DefaultExecutionOrder(-100)]
public class JetpackAudioSetup : MonoBehaviour
{
    // All paths relative to Assets/
    private const string BASE = "Assets/Levels/6 Jetpack/Assets/Audios/JetpackAudios/";

void Awake()
    {
        var mgr = GetComponent<JetpackAudioManager>();
        if (mgr == null) return;

        // Wire audioManager reference on AutoJetpackController
        var jetpack = GetComponent<AutoJetpackController>();
        if (jetpack != null)
        {
            var field = typeof(AutoJetpackController).GetField("audioManager",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);
            if (field != null) field.SetValue(jetpack, mgr);
        }

#if UNITY_EDITOR
        Assign(mgr, "ignitionSound",          BASE + "Jetpack ignition.mp3");
        Assign(mgr, "thrustLoopSound",        BASE + "RocketPropulsion.mp3");
        Assign(mgr, "boostSound",             BASE + "Jetpack boost.mp3");
        Assign(mgr, "lowFuelWarningSound",    BASE + "Low fuel warning.mp3");
        Assign(mgr, "landingSoftSound",       BASE + "Landing soft.mp3");
        Assign(mgr, "landingHardSound",       BASE + "Landing hard.mp3");
        Assign(mgr, "platformTouchdownSound", BASE + "Platform touchdown.mp3");
        Assign(mgr, "windHighAltitudeSound",  BASE + "Wind high altitude (2).mp3");
        Assign(mgr, "windRushingSpeedSound",  BASE + "Wind rushing speed (2).mp3");
#endif
    }

#if UNITY_EDITOR
    void Assign(JetpackAudioManager mgr, string fieldName, string assetPath)
    {
        var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
        if (clip == null)
        {
            Debug.LogWarning($"[JetpackAudioSetup] Could not find clip at: {assetPath}");
            return;
        }
        var field = typeof(JetpackAudioManager).GetField(fieldName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Public);
        if (field != null)
        {
            field.SetValue(mgr, clip);
            Debug.Log($"[JetpackAudioSetup] Assigned {fieldName}: {clip.name}");
        }
    }
#endif
}
