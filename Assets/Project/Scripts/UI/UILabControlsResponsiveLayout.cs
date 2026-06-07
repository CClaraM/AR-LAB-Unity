using UnityEngine;
using UnityEngine.UI;

public class UILabControlsResponsiveLayout : MonoBehaviour
{
    [System.Serializable]
    public class UIElementLayout
    {
        public string name;

        [Header("Target")]
        public RectTransform target;

        [Header("Transform")]
        public Vector2 anchoredPosition;
        public Vector2 size;
        public float rotationZ;
        public bool applySize = true;
        public bool setActive = true;

        [Header("Optional Image")]
        public Image image;
        public Sprite sprite;
        public Color color = Color.white;
        public bool applyImage = false;
    }

    [System.Serializable]
    public class PanelLayout
    {
        [Header("Panel")]
        public Vector2 panelSize = new Vector2(800f, 300f);
        public Sprite backgroundSprite;
        public Color backgroundColor = Color.white;

        [Header("Main Elements")]
        public UIElementLayout fireButton;
        public UIElementLayout exitButton;
        public UIElementLayout distanceBox;
        public UIElementLayout pitchBox;
        public UIElementLayout powerBox;

        [Header("Additional Elements")]
        public UIElementLayout[] additionalElements;
    }

    [Header("Panel")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private Image panelBackground;

    [Header("Layouts")]
    [SerializeField] private PanelLayout portraitLayout;
    [SerializeField] private PanelLayout landscapeLayout;

    private DeviceOrientation lastOrientation = DeviceOrientation.Unknown;

    private void Awake()
    {
        CacheReferences();
        ApplyLayout(GetStableOrientation());
    }

    private void OnEnable()
    {
        CacheReferences();
        ApplyLayout(GetStableOrientation());
    }

    private void Update()
    {
        DeviceOrientation currentOrientation = GetStableOrientation();

        if (currentOrientation == lastOrientation)
            return;

        ApplyLayout(currentOrientation);
    }

    private void CacheReferences()
    {
        if (panelRect == null)
            panelRect = GetComponent<RectTransform>();

        if (panelBackground == null)
            panelBackground = GetComponent<Image>();
    }

    private DeviceOrientation GetStableOrientation()
    {
        DeviceOrientation orientation = Input.deviceOrientation;

        if (orientation == DeviceOrientation.Unknown ||
            orientation == DeviceOrientation.FaceUp ||
            orientation == DeviceOrientation.FaceDown)
        {
            return lastOrientation == DeviceOrientation.Unknown
                ? DeviceOrientation.Portrait
                : lastOrientation;
        }

        return orientation;
    }

    private void ApplyLayout(DeviceOrientation orientation)
    {
        lastOrientation = orientation;

        bool isLandscape =
            orientation == DeviceOrientation.LandscapeLeft ||
            orientation == DeviceOrientation.LandscapeRight;

        PanelLayout layout = isLandscape ? landscapeLayout : portraitLayout;

        ApplyPanelLayout(layout);

        ApplyElementLayout(layout.fireButton);
        ApplyElementLayout(layout.exitButton);
        ApplyElementLayout(layout.distanceBox);
        ApplyElementLayout(layout.pitchBox);
        ApplyElementLayout(layout.powerBox);

        ApplyAdditionalElements(layout.additionalElements);
    }

    private void ApplyPanelLayout(PanelLayout layout)
    {
        if (layout == null)
            return;

        if (panelRect != null)
            panelRect.sizeDelta = layout.panelSize;

        if (panelBackground != null)
        {
            if (layout.backgroundSprite != null)
                panelBackground.sprite = layout.backgroundSprite;

            panelBackground.color = layout.backgroundColor;
        }
    }

    private void ApplyAdditionalElements(UIElementLayout[] elements)
    {
        if (elements == null)
            return;

        foreach (UIElementLayout element in elements)
        {
            ApplyElementLayout(element);
        }
    }

    private void ApplyElementLayout(UIElementLayout element)
    {
        if (element == null || element.target == null)
            return;

        element.target.gameObject.SetActive(element.setActive);

        if (!element.setActive)
            return;

        element.target.anchoredPosition = element.anchoredPosition;
        element.target.localRotation = Quaternion.Euler(0f, 0f, element.rotationZ);
        element.target.localScale = Vector3.one;

        if (element.applySize)
            element.target.sizeDelta = element.size;

        if (element.applyImage && element.image != null)
        {
            if (element.sprite != null)
                element.image.sprite = element.sprite;

            element.image.color = element.color;
        }
    }
}