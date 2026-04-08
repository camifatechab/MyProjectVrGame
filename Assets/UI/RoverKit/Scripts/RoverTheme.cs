using UnityEngine;

[CreateAssetMenu(fileName = "RoverTheme", menuName = "UI/Rover Kit Theme")]
public class RoverTheme : ScriptableObject
{
    [Header("Palette")]
    public Color primaryGlow = new(0.43f, 0.92f, 0.95f, 1f);
    public Color secondaryGlow = new(0.23f, 0.55f, 1f, 1f);
    public Color warningGlow = new(1f, 0.76f, 0.29f, 1f);
    public Color panelTint = new(0.04f, 0.09f, 0.14f, 0.82f);
    public Color textColor = new(0.92f, 0.97f, 1f, 1f);
    public Color mutedTextColor = new(0.67f, 0.83f, 0.9f, 1f);

    [Header("Seat Beacon")]
    public float beaconPulseFar = 1.1f;
    public float beaconPulseNear = 2.6f;
    public float beaconGlowFar = 0.45f;
    public float beaconGlowNear = 1.55f;
    public float beaconLightFar = 1.2f;
    public float beaconLightNear = 3.8f;
    public float beaconScaleFar = 1f;
    public float beaconScaleNear = 1.14f;

    [Header("Mount Prompt")]
    public string mountTitle = "Mount Rover";
    public string mountSubtitle = "Grip to ride";
    public string mountConfirmTitle = "Rover Linked";
    public string mountConfirmSubtitle = "Drive ready";
    public Color mountConfirmPanelTint = new(0.16f, 0.18f, 0.2f, 0.9f);
    public Color mountConfirmAccent = new(0.7f, 0.73f, 0.77f, 1f);
    public Color mountConfirmTextColor = new(0.93f, 0.94f, 0.96f, 1f);
    public Color mountConfirmMutedTextColor = new(0.72f, 0.75f, 0.79f, 1f);
    public float promptFadeSpeed = 6f;
    public float mountConfirmDuration = 1.8f;
    public Vector3 promptOffset = new(0f, 0.2f, 0f);

    [Header("Dashboard")]
    public string dashboardStatusLabel = "SYSTEM ONLINE";
    public string dashboardSpeedUnit = "km/h";
    public string dashboardResetWarningLabel = "RESET IMMINENT";
    public string dashboardRepositioningLabel = "REPOSITIONING";
    public Color dashboardPanelTint = new(0.06f, 0.09f, 0.11f, 0.9f);
    public Color dashboardAccent = new(0.76f, 0.8f, 0.85f, 1f);
    public Color dashboardTextColor = new(0.95f, 0.97f, 0.99f, 1f);
    public Color dashboardMutedTextColor = new(0.63f, 0.68f, 0.73f, 1f);
    public Color dashboardWarningAccent = new(1f, 0.76f, 0.29f, 1f);
    public Color dashboardWarningTextColor = new(0.99f, 0.95f, 0.86f, 1f);
    public float dashboardFadeSpeed = 5f;
    public Vector3 dashboardOffset = new(0f, 0f, 0f);

    [Header("General Motion")]
    public float bobAmplitude = 0.03f;
    public float bobFrequency = 0.8f;
}
