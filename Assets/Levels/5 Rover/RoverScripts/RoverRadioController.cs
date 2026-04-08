using System;
using UnityEngine;
using UnityEngine.XR;

[DisallowMultipleComponent]
public class RoverRadioController : MonoBehaviour
{
    [Serializable]
    public class RadioTrack
    {
        public string displayName = "Track";
        public AudioClip clip;
    }

    [Header("Audio")]
    [SerializeField] private AudioSource radioSource;
    [SerializeField] private RadioTrack[] tracks = Array.Empty<RadioTrack>();
    [SerializeField] [Range(0f, 1f)] private float radioVolume = 0.45f;
    [SerializeField] private bool playOnMount;
    [SerializeField] [Range(0f, 1f)] private float spatialBlend = 0.35f;
    [SerializeField] private float minDistance = 0.6f;
    [SerializeField] private float maxDistance = 5f;

    [Header("Controls")]
    [SerializeField] private KeyCode toggleKey = KeyCode.R;
    [SerializeField] private KeyCode nextTrackKey = KeyCode.T;
    [SerializeField] private XRNode inputNode = XRNode.RightHand;
    [SerializeField] private bool primaryButtonTogglesRadio = true;
    [SerializeField] private bool secondaryButtonChangesTrack = true;

    [Header("Display")]
    [SerializeField] private float messageHoldDuration = 1.6f;

    private InputDevice inputDevice;
    private bool isOn;
    private int currentTrackIndex;
    private bool previousPrimaryPressed;
    private bool previousSecondaryPressed;
    private float messageTimer;
    private string temporaryMessage = string.Empty;
    private bool wasMounted;

    public bool IsOn => isOn;
    public bool HasTracks => tracks != null && tracks.Length > 0;
    public string DisplayText => GetDisplayText();

    private void Awake()
    {
        EnsureAudioSource();
        ApplySourceSettings();
    }

    public void Tick(bool isMounted)
    {
        EnsureAudioSource();
        ApplySourceSettings();
        RefreshDevice();

        if (isMounted && !wasMounted && playOnMount && HasTracks && !isOn)
            ToggleRadio();

        if (isMounted)
            HandleInput();

        if (isOn && radioSource != null && radioSource.clip != null && !radioSource.isPlaying)
            radioSource.Play();

        if (messageTimer > 0f)
            messageTimer = Mathf.Max(0f, messageTimer - Time.deltaTime);

        wasMounted = isMounted;
    }

    private void HandleInput()
    {
        bool togglePressed = Input.GetKeyDown(toggleKey);
        bool nextPressed = Input.GetKeyDown(nextTrackKey);

        if (inputDevice.isValid)
        {
            if (primaryButtonTogglesRadio &&
                inputDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryPressed))
            {
                if (primaryPressed && !previousPrimaryPressed)
                    togglePressed = true;

                previousPrimaryPressed = primaryPressed;
            }
            else
            {
                previousPrimaryPressed = false;
            }

            if (secondaryButtonChangesTrack &&
                inputDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondaryPressed))
            {
                if (secondaryPressed && !previousSecondaryPressed)
                    nextPressed = true;

                previousSecondaryPressed = secondaryPressed;
            }
            else
            {
                previousSecondaryPressed = false;
            }
        }
        else
        {
            previousPrimaryPressed = false;
            previousSecondaryPressed = false;
        }

        if (togglePressed)
            ToggleRadio();

        if (nextPressed)
            NextTrack();
    }

    private void ToggleRadio()
    {
        if (!HasTracks)
        {
            PushMessage("RADIO EMPTY");
            return;
        }

        isOn = !isOn;
        if (isOn)
        {
            PlayTrack(currentTrackIndex, true);
        }
        else
        {
            if (radioSource != null)
                radioSource.Stop();

            PushMessage("RADIO OFF");
        }
    }

    private void NextTrack()
    {
        if (!HasTracks)
        {
            PushMessage("RADIO EMPTY");
            return;
        }

        currentTrackIndex = (currentTrackIndex + 1) % tracks.Length;
        if (isOn)
        {
            PlayTrack(currentTrackIndex, true);
        }
        else
        {
            PushMessage("TRACK " + GetTrackNumber(currentTrackIndex));
        }
    }

    private void PlayTrack(int index, bool pushNowPlaying)
    {
        if (!HasTracks || radioSource == null)
            return;

        index = Mathf.Clamp(index, 0, tracks.Length - 1);
        currentTrackIndex = index;
        RadioTrack track = tracks[currentTrackIndex];

        if (track == null || track.clip == null)
        {
            PushMessage("TRACK MISSING");
            return;
        }

        radioSource.clip = track.clip;
        radioSource.Play();

        if (pushNowPlaying)
            PushMessage(GetTrackDisplay(track));
    }

    private string GetDisplayText()
    {
        if (messageTimer > 0f && !string.IsNullOrWhiteSpace(temporaryMessage))
            return temporaryMessage;

        if (!HasTracks)
            return "RADIO EMPTY";

        if (!isOn)
            return "RADIO OFF";

        RadioTrack track = tracks[Mathf.Clamp(currentTrackIndex, 0, tracks.Length - 1)];
        return GetTrackDisplay(track);
    }

    private string GetTrackDisplay(RadioTrack track)
    {
        if (track == null)
            return "RADIO";

        string name = string.IsNullOrWhiteSpace(track.displayName) ? "TRACK " + GetTrackNumber(currentTrackIndex) : track.displayName.Trim().ToUpperInvariant();
        return name.Length > 16 ? name[..16] : name;
    }

    private static string GetTrackNumber(int index)
    {
        return (index + 1).ToString("00");
    }

    private void PushMessage(string message)
    {
        temporaryMessage = message;
        messageTimer = messageHoldDuration;
    }

    private void RefreshDevice()
    {
        if (inputDevice.isValid)
            return;

        inputDevice = InputDevices.GetDeviceAtXRNode(inputNode);
    }

    private void EnsureAudioSource()
    {
        if (radioSource != null)
            return;

        Transform existingSpeaker = transform.Find("RadioSpeaker");
        if (existingSpeaker == null)
        {
            GameObject speakerObject = new("RadioSpeaker");
            existingSpeaker = speakerObject.transform;
            existingSpeaker.SetParent(transform, false);
            existingSpeaker.localPosition = new Vector3(0.18f, 0.88f, 0.42f);
            existingSpeaker.localRotation = Quaternion.identity;
            existingSpeaker.localScale = Vector3.one;
        }

        radioSource = existingSpeaker.GetComponent<AudioSource>();
        if (radioSource == null)
            radioSource = existingSpeaker.gameObject.AddComponent<AudioSource>();
    }

    private void ApplySourceSettings()
    {
        if (radioSource == null)
            return;

        radioSource.playOnAwake = false;
        radioSource.loop = true;
        radioSource.spatialBlend = spatialBlend;
        radioSource.minDistance = minDistance;
        radioSource.maxDistance = maxDistance;
        radioSource.rolloffMode = AudioRolloffMode.Linear;
        radioSource.volume = radioVolume;
    }

    public void ToggleFromUi()
    {
        ToggleRadio();
    }

    public void NextTrackFromUi()
    {
        NextTrack();
    }
}
