using UnityEngine;
using UnityEngine.UI;

public class LaunchButtonVisualState : MonoBehaviour
{
    private enum PhysicalOrientation
    {
        Portrait,
        Landscape
    }

    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private RectTransform backgroundRect;

    [Header("Enabled Visual")]
    [SerializeField] private Sprite enabledSprite;

    [Header("Disabled Visual")]
    [SerializeField] private Sprite disabledSprite;

    [Header("Portrait Sizes")]
    [SerializeField] private Vector2 enabledPortraitSize = new Vector2(180f, 80f);
    [SerializeField] private Vector2 disabledPortraitSize = new Vector2(180f, 80f);

    [Header("Landscape Sizes")]
    [SerializeField] private Vector2 enabledLandscapeSize = new Vector2(180f, 80f);
    [SerializeField] private Vector2 disabledLandscapeSize = new Vector2(180f, 80f);

    [Header("Optional Tint")]
    [SerializeField] private bool applyTint = false;
    [SerializeField] private Color enabledColor = Color.white;
    [SerializeField] private Color disabledColor = Color.white;

    [Header("Orientation Detection")]
    [SerializeField] private bool usePhysicalDeviceOrientation = true;
    [SerializeField] private bool fallbackToScreenOrientation = true;

    private bool currentInteractableState = true;
    private PhysicalOrientation currentOrientation = PhysicalOrientation.Portrait;

    private void Reset()
    {
        button = GetComponent<Button>();

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (backgroundImage != null)
            backgroundRect = backgroundImage.rectTransform;
    }

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (backgroundImage != null && backgroundRect == null)
            backgroundRect = backgroundImage.rectTransform;

        currentOrientation = DetectOrientation();
        ApplyState(currentInteractableState);
    }

    private void Update()
    {
        PhysicalOrientation detectedOrientation = DetectOrientation();

        if (detectedOrientation == currentOrientation)
            return;

        currentOrientation = detectedOrientation;
        ApplyState(currentInteractableState);
    }

    public void SetInteractableVisual(bool interactable)
    {
        currentInteractableState = interactable;
        currentOrientation = DetectOrientation();
        ApplyState(interactable);
    }

    private void ApplyState(bool interactable)
    {
        if (button != null)
            button.interactable = interactable;

        if (backgroundImage != null)
        {
            Sprite targetSprite = interactable ? enabledSprite : disabledSprite;

            if (targetSprite != null)
                backgroundImage.sprite = targetSprite;

            if (applyTint)
                backgroundImage.color = interactable ? enabledColor : disabledColor;
        }

        if (backgroundRect != null)
        {
            backgroundRect.sizeDelta = GetSizeForState(interactable);
        }
    }

    private Vector2 GetSizeForState(bool interactable)
    {
        bool isLandscape = currentOrientation == PhysicalOrientation.Landscape;

        if (interactable)
            return isLandscape ? enabledLandscapeSize : enabledPortraitSize;

        return isLandscape ? disabledLandscapeSize : disabledPortraitSize;
    }

    private PhysicalOrientation DetectOrientation()
    {
        if (usePhysicalDeviceOrientation)
        {
            DeviceOrientation deviceOrientation = Input.deviceOrientation;

            switch (deviceOrientation)
            {
                case DeviceOrientation.LandscapeLeft:
                case DeviceOrientation.LandscapeRight:
                    return PhysicalOrientation.Landscape;

                case DeviceOrientation.Portrait:
                case DeviceOrientation.PortraitUpsideDown:
                    return PhysicalOrientation.Portrait;
            }
        }

        if (fallbackToScreenOrientation)
        {
            if (Screen.width > Screen.height)
                return PhysicalOrientation.Landscape;

            return PhysicalOrientation.Portrait;
        }

        return currentOrientation;
    }
}