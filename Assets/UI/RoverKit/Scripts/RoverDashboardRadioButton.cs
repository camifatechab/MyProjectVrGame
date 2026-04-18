using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[DisallowMultipleComponent]
public class RoverDashboardRadioButton : MonoBehaviour
{
    public enum ButtonAction
    {
        ToggleRadio,
        NextTrack,
        ReturnToRoverStart,
        ReturnToLastCheckpoint
    }

    [SerializeField] private ButtonAction buttonAction;
    [SerializeField] private Transform capTransform;
    [SerializeField] private Renderer capRenderer;
    [SerializeField] private Renderer baseRenderer;
    [SerializeField] private float pressDepth = 0.0025f;

    private RoverUIBinder binder;
    private XRSimpleInteractable interactable;
    private Vector3 capUpLocalPosition;
    private float pressedTimer;

    private static readonly Color IdleCapColor = new(0.18f, 0.22f, 0.26f, 1f);
    private static readonly Color IdleBaseColor = new(0.06f, 0.09f, 0.12f, 1f);
    private static readonly Color ActiveCapColor = new(0.72f, 0.78f, 0.84f, 1f);
    private static readonly Color GlowCapColor = new(0.47f, 0.9f, 0.95f, 1f);

    public float PressedStrength => pressedTimer;

    public void Bind(RoverUIBinder targetBinder, ButtonAction action)
    {
        binder = targetBinder;
        buttonAction = action;
    }

    private void Awake()
    {
        if (capTransform == null)
            capTransform = transform.Find("Cap");

        if (capRenderer == null && capTransform != null)
            capRenderer = capTransform.GetComponent<Renderer>();

        if (baseRenderer == null)
        {
            Transform baseTransform = transform.Find("Base");
            if (baseTransform != null)
                baseRenderer = baseTransform.GetComponent<Renderer>();
        }

        if (capTransform != null)
            capUpLocalPosition = capTransform.localPosition;

        interactable = GetComponent<XRSimpleInteractable>();
        if (interactable != null)
            interactable.selectEntered.AddListener(OnSelectEntered);
    }

    private void OnDestroy()
    {
        if (interactable != null)
            interactable.selectEntered.RemoveListener(OnSelectEntered);
    }

    private void Update()
    {
        if (pressedTimer > 0f)
            pressedTimer = Mathf.Max(0f, pressedTimer - Time.deltaTime * 6f);

        UpdateVisuals();
    }

    private void OnMouseDown()
    {
        Trigger();
    }

    private void OnSelectEntered(SelectEnterEventArgs _)
    {
        Trigger();
    }

    private void Trigger()
    {
        if (binder == null)
            return;

        bool handled = true;

        switch (buttonAction)
        {
            case ButtonAction.ToggleRadio:
                binder.ToggleRadio();
                break;
            case ButtonAction.NextTrack:
                binder.NextRadioTrack();
                break;
            case ButtonAction.ReturnToRoverStart:
                handled = binder.ReturnToRoverStart();
                break;
            case ButtonAction.ReturnToLastCheckpoint:
                handled = binder.ReturnToLastCheckpoint();
                break;
        }

        if (handled)
            pressedTimer = 1f;
    }

    private void UpdateVisuals()
    {
        bool radioOn = binder != null && binder.IsRadioOn;
        bool highlight = buttonAction == ButtonAction.ToggleRadio && radioOn;

        if (capTransform != null)
        {
            Vector3 downPosition = capUpLocalPosition + new Vector3(0f, 0f, -pressDepth);
            capTransform.localPosition = Vector3.Lerp(capUpLocalPosition, downPosition, pressedTimer);
        }

        if (capRenderer != null)
        {
            Color target = highlight ? GlowCapColor : IdleCapColor;
            if (pressedTimer > 0.01f)
                target = ActiveCapColor;

            capRenderer.material.color = target;
        }

        if (baseRenderer != null)
            baseRenderer.material.color = IdleBaseColor;
    }
}
