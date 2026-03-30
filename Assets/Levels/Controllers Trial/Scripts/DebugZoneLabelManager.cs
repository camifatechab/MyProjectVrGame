using System.Linq;
using UnityEngine;

[ExecuteAlways]
public class DebugZoneLabelManager : MonoBehaviour
{
    public float sideOffset = 20f;
    public float labelHeight = 5.2f;
    public float maxVisibleDistance = 35f;
    public float textCharacterSize = 0.07f;
    public int textFontSize = 48;
    public Vector3 labelScale = new Vector3(0.55f, 0.55f, 0.55f);

    private Camera targetCamera;
    private TextMesh[] labels;

    void Awake()
    {
        ArrangeLabels();
    }

    void OnEnable()
    {
        ArrangeLabels();
    }

    void ArrangeLabels()
    {
        labels = GetComponentsInChildren<TextMesh>(true)
            .OrderBy(label => label.name)
            .ToArray();

        float centerX = transform.parent != null ? transform.parent.position.x : transform.position.x;

        for (int i = 0; i < labels.Length; i++)
        {
            TextMesh label = labels[i];
            if (label == null)
                continue;

            bool placeOnRight = (i % 2) == 1;
            Vector3 position = label.transform.position;
            position.x = centerX + (placeOnRight ? sideOffset : -sideOffset);
            position.y = labelHeight;
            label.transform.position = position;
            label.transform.rotation = Quaternion.Euler(0f, placeOnRight ? -90f : 90f, 0f);
            label.transform.localScale = labelScale;
            label.fontSize = textFontSize;
            label.characterSize = textCharacterSize;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
        }
    }

    void LateUpdate()
    {
        if (targetCamera == null)
            targetCamera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();

        if (labels == null)
            return;

        foreach (TextMesh label in labels)
        {
            if (label == null)
                continue;

            Renderer rendererComponent = label.GetComponent<Renderer>();
            if (rendererComponent == null)
                continue;

            bool shouldShow = true;
            if (targetCamera != null)
            {
                Vector3 toLabel = label.transform.position - targetCamera.transform.position;
                shouldShow = toLabel.sqrMagnitude <= maxVisibleDistance * maxVisibleDistance;
            }

            rendererComponent.enabled = shouldShow;
        }
    }
}
