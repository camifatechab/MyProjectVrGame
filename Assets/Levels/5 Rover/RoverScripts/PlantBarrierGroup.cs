using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Groups DestructiblePlant instances and shows a world-space UI panel
/// telling the player to shoot the road blockers.
/// </summary>
public class PlantBarrierGroup : MonoBehaviour
{
    [Header("Plants")]
    [Tooltip("Assign the DestructiblePlant objects. If empty, auto-finds nearby plants.")]
    public DestructiblePlant[] plants;

    [Header("Auto-Find")]
    [Tooltip("Optional name prefix used when auto-finding plants.")]
    public string autoFindPrefix = "";

    [Tooltip("Auto-find plants within this radius of the group object.")]
    public float autoFindRadius = 45f;

    [Header("UI Positioning")]
    [Tooltip("World offset from this object for the instruction panel.")]
    public Vector3 uiOffset = new(0f, 3.5f, 0f);

    [Tooltip("Show the prompt in a comfortable VR position in front of the player.")]
    public bool useComfortUiPlacement = true;

    [Tooltip("Distance from the headset for the prompt when it is visible.")]
    public float comfortUiDistance = 2.35f;

    [Tooltip("Vertical offset from eye level for the prompt.")]
    public float comfortUiVerticalOffset = -0.06f;

    [Tooltip("Horizontal offset from the center view for the prompt.")]
    public float comfortUiHorizontalOffset = 0f;

    [Tooltip("How quickly the UI follows the player in comfort mode.")]
    public float comfortUiFollowSpeed = 10f;

    [Tooltip("Scale of the world-space canvas.")]
    public float uiScale = 0.00225f;

    [Tooltip("Distance within which the UI becomes visible.")]
    public float showRadius = 60f;

    [Header("UI Timing")]
    [Tooltip("How long the Path Clear message stays visible before fading.")]
    public float clearMessageDuration = 4f;

    private static readonly Color PanelColor = new(0.04f, 0.09f, 0.14f, 0.82f);
    private static readonly Color PrimaryGlow = new(0.43f, 0.92f, 0.95f, 1f);
    private static readonly Color TextColor = new(0.92f, 0.97f, 1f, 1f);
    private static readonly Color MutedText = new(0.67f, 0.83f, 0.9f, 1f);
    private static readonly Color WarningGlow = new(1f, 0.76f, 0.29f, 1f);
    private static readonly Color SuccessGlow = new(0.3f, 1f, 0.5f, 1f);

    private Canvas uiCanvas;
    private CanvasGroup canvasGroup;
    private TextMeshProUGUI titleLabel;
    private TextMeshProUGUI subtitleLabel;
    private TextMeshProUGUI counterLabel;
    private Image progressFill;
    private Image accentLine;
    private Transform playerCamera;
    private Vector3 uiAnchorPosition;

    private int totalPlants;
    private int destroyedCount;
    private bool allCleared;
    private float clearTimer;

    private void Start()
    {
        ResolvePlants();

        totalPlants = plants.Length;
        if (totalPlants == 0)
        {
            Debug.LogWarning("[PlantBarrierGroup] No plants found - disabling.");
            enabled = false;
            return;
        }

        for (int i = 0; i < plants.Length; i++)
        {
            if (plants[i] != null)
                plants[i].Destroyed += OnPlantDestroyed;
        }

        BuildUi();
        UpdateUi();
    }

    private void Update()
    {
        if (uiCanvas == null)
            return;

        playerCamera ??= ResolvePlayerCamera();
        if (playerCamera == null)
            return;

        UpdateUiPlacement();

        float distance = Vector3.Distance(playerCamera.position, GetUiFocusPoint());
        float targetAlpha = allCleared
            ? (clearTimer > 0f ? 1f : 0f)
            : (distance <= showRadius ? 1f : 0f);
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime * 3f);

        Vector3 toCamera = playerCamera.position - uiCanvas.transform.position;
        if (toCamera.sqrMagnitude > 0.001f)
            uiCanvas.transform.rotation = Quaternion.LookRotation(-toCamera.normalized, playerCamera.up);

        if (!allCleared)
            return;

        clearTimer -= Time.deltaTime;
        if (clearTimer <= 0f && canvasGroup.alpha <= 0.01f)
            uiCanvas.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (plants == null)
            return;

        for (int i = 0; i < plants.Length; i++)
        {
            if (plants[i] != null)
                plants[i].Destroyed -= OnPlantDestroyed;
        }
    }

    private void OnPlantDestroyed()
    {
        destroyedCount++;
        UpdateUi();

        if (destroyedCount < totalPlants)
            return;

        allCleared = true;
        clearTimer = clearMessageDuration;
        ShowPathClearMessage();
        Debug.Log("<color=green>[PlantBarrierGroup] All plants destroyed - path is clear.</color>");
    }

    private void UpdateUi()
    {
        if (allCleared)
            return;

        if (counterLabel != null)
            counterLabel.text = $"{destroyedCount} / {totalPlants}";

        if (progressFill != null)
            progressFill.fillAmount = totalPlants > 0 ? (float)destroyedCount / totalPlants : 0f;
    }

    private void ShowPathClearMessage()
    {
        titleLabel.text = "PATH CLEAR";
        titleLabel.color = SuccessGlow;
        titleLabel.fontStyle = FontStyles.Bold;

        subtitleLabel.text = "Proceed forward";
        subtitleLabel.color = SuccessGlow;

        if (counterLabel != null)
            counterLabel.gameObject.SetActive(false);

        if (accentLine != null)
            accentLine.color = SuccessGlow;

        if (progressFill != null)
        {
            progressFill.fillAmount = 1f;
            progressFill.color = SuccessGlow;
        }
    }

    private void ResolvePlants()
    {
        if (plants != null && plants.Length > 0)
            return;

        DestructiblePlant[] childPlants = GetComponentsInChildren<DestructiblePlant>(true);
        if (childPlants.Length > 0)
        {
            plants = childPlants;
            return;
        }

        DestructiblePlant[] allPlants = FindObjectsByType<DestructiblePlant>(FindObjectsSortMode.None);
        List<DestructiblePlant> matches = new();

        for (int i = 0; i < allPlants.Length; i++)
        {
            DestructiblePlant plant = allPlants[i];
            if (plant == null)
                continue;

            if (!string.IsNullOrEmpty(autoFindPrefix) && !plant.name.StartsWith(autoFindPrefix))
                continue;

            if (Vector3.Distance(transform.position, plant.transform.position) > autoFindRadius)
                continue;

            matches.Add(plant);
        }

        plants = matches.ToArray();
    }

    private void BuildUi()
    {
        UpdateUiPlacement(immediate: true);
        Vector3 position = uiAnchorPosition;
        GameObject root = new("PlantBarrierUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        root.transform.position = position;
        root.transform.localScale = Vector3.one * uiScale;

        uiCanvas = root.GetComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.WorldSpace;
        uiCanvas.overrideSorting = true;
        uiCanvas.sortingOrder = 50;
        uiCanvas.pixelPerfect = false;

        canvasGroup = root.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        RectTransform canvasRect = root.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(260f, 108f);

        Image panel = CreateImage(canvasRect, "Panel", new Vector2(260f, 108f), Vector2.zero, PanelColor);
        panel.raycastTarget = false;

        accentLine = CreateImage(canvasRect, "Accent", new Vector2(196f, 3f), new Vector2(0f, 42f), PrimaryGlow);
        accentLine.raycastTarget = false;

        titleLabel = CreateText(canvasRect, "Title", new Vector2(0f, 27f), new Vector2(230f, 24f), 15f);
        titleLabel.text = "ROAD BLOCKED";
        titleLabel.color = WarningGlow;
        titleLabel.fontStyle = FontStyles.Bold;
        titleLabel.alignment = TextAlignmentOptions.Center;

        subtitleLabel = CreateText(canvasRect, "Subtitle", new Vector2(0f, 8f), new Vector2(224f, 20f), 9f);
        subtitleLabel.text = "Shoot the cyan plants to clear the road";
        subtitleLabel.color = TextColor;
        subtitleLabel.fontStyle = FontStyles.Bold;
        subtitleLabel.alignment = TextAlignmentOptions.Center;

        Image progressBackground = CreateImage(
            canvasRect,
            "ProgressBG",
            new Vector2(180f, 8f),
            new Vector2(0f, -12f),
            new Color(0.08f, 0.12f, 0.16f, 0.9f));
        progressBackground.raycastTarget = false;

        progressFill = CreateImage(progressBackground.rectTransform, "ProgressFill", new Vector2(180f, 8f), Vector2.zero, PrimaryGlow);
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        progressFill.fillAmount = 0f;
        progressFill.raycastTarget = false;

        counterLabel = CreateText(canvasRect, "Counter", new Vector2(0f, -31f), new Vector2(224f, 18f), 10f);
        counterLabel.text = $"0 / {totalPlants}";
        counterLabel.color = MutedText;
        counterLabel.fontStyle = FontStyles.Bold;
        counterLabel.alignment = TextAlignmentOptions.Center;
    }

    private void UpdateUiPlacement(bool immediate = false)
    {
        Vector3 desiredPosition = GetWorldAnchorPosition();
        if (useComfortUiPlacement && playerCamera != null)
            desiredPosition = GetComfortUiPosition();

        if (immediate || uiCanvas == null)
            uiAnchorPosition = desiredPosition;
        else
            uiAnchorPosition = Vector3.Lerp(uiAnchorPosition, desiredPosition, Time.deltaTime * comfortUiFollowSpeed);

        if (uiCanvas != null)
            uiCanvas.transform.position = uiAnchorPosition;
    }

    private Vector3 GetUiFocusPoint()
    {
        Vector3 origin = transform.position;
        if (TryGetPlantBounds(out Bounds bounds))
            origin = bounds.center;

        return origin;
    }

    private Vector3 GetWorldAnchorPosition()
    {
        Vector3 origin = transform.position;
        if (TryGetPlantBounds(out Bounds bounds))
            origin = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);

        return origin + uiOffset;
    }

    private Vector3 GetComfortUiPosition()
    {
        Vector3 forward = playerCamera.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = playerCamera.forward;
        forward.Normalize();

        Vector3 right = playerCamera.right;
        right.y = 0f;
        if (right.sqrMagnitude < 0.001f)
            right = Vector3.right;
        right.Normalize();

        return playerCamera.position
            + (forward * comfortUiDistance)
            + (playerCamera.up * comfortUiVerticalOffset)
            + (right * comfortUiHorizontalOffset);
    }

    private bool TryGetPlantBounds(out Bounds bounds)
    {
        bounds = default;
        bool found = false;

        if (plants == null)
            return false;

        for (int i = 0; i < plants.Length; i++)
        {
            DestructiblePlant plant = plants[i];
            if (plant == null)
                continue;

            Renderer[] renderers = plant.GetComponentsInChildren<Renderer>(true);
            for (int j = 0; j < renderers.Length; j++)
            {
                Renderer renderer = renderers[j];
                if (renderer == null)
                    continue;

                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
        }

        return found;
    }

    private static Transform ResolvePlayerCamera()
    {
        if (Camera.main != null)
            return Camera.main.transform;

        Camera anyCamera = FindFirstObjectByType<Camera>();
        return anyCamera != null ? anyCamera.transform : null;
    }

    private static Image CreateImage(Transform parent, string name, Vector2 size, Vector2 position, Color color)
    {
        GameObject gameObject = new(name, typeof(RectTransform), typeof(Image));
        gameObject.transform.SetParent(parent, false);

        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;

        Image image = gameObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, Vector2 position, Vector2 size, float fontSize)
    {
        GameObject gameObject = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        gameObject.transform.SetParent(parent, false);

        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;

        TextMeshProUGUI label = gameObject.GetComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = fontSize;
        label.enableAutoSizing = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Overflow;
        label.richText = false;
        return label;
    }
}
