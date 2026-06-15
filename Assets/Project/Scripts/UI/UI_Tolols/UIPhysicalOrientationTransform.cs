using UnityEngine;

public class UIPhysicalOrientationTransform : MonoBehaviour
{
    private enum PhysicalOrientation
    {
        Portrait,
        PortraitUpsideDown,
        LandscapeLeft,
        LandscapeRight
    }

    [System.Serializable]
    private class OrientationTransform
    {
        public Vector2 anchoredPosition;
        public float rotationZ;
    }

    [Header("Target")]
    [SerializeField] private RectTransform target;

    [Header("Portrait")]
    [SerializeField]
    private OrientationTransform portrait = new OrientationTransform
    {
        anchoredPosition = Vector2.zero,
        rotationZ = 0f
    };

    [Header("Portrait Upside Down")]
    [SerializeField]
    private OrientationTransform portraitUpsideDown = new OrientationTransform
    {
        anchoredPosition = Vector2.zero,
        rotationZ = 180f
    };

    [Header("Landscape Left")]
    [SerializeField]
    private OrientationTransform landscapeLeft = new OrientationTransform
    {
        anchoredPosition = Vector2.zero,
        rotationZ = 90f
    };

    [Header("Landscape Right")]
    [SerializeField]
    private OrientationTransform landscapeRight = new OrientationTransform
    {
        anchoredPosition = Vector2.zero,
        rotationZ = -90f
    };

    [Header("Behavior")]
    [SerializeField] private bool applyOnEnable = true;
    [SerializeField] private bool updateContinuously = true;
    [SerializeField] private bool fallbackToScreenRatio = true;

    private PhysicalOrientation currentOrientation;

    private void Reset()
    {
        target = GetComponent<RectTransform>();
    }

    private void Awake()
    {
        if (target == null)
            target = GetComponent<RectTransform>();

        ConfigureAnchorsToCenter();
    }

    private void OnEnable()
    {
        if (!applyOnEnable)
            return;

        currentOrientation = DetectOrientation();
        ApplyCurrentOrientation();
    }

    private void Update()
    {
        if (!updateContinuously)
            return;

        PhysicalOrientation detected = DetectOrientation();

        if (detected == currentOrientation)
            return;

        currentOrientation = detected;
        ApplyCurrentOrientation();
    }

    public void ApplyCurrentOrientation()
    {
        if (target == null)
            return;

        OrientationTransform data = GetData(currentOrientation);

        target.anchoredPosition = data.anchoredPosition;
        target.localRotation = Quaternion.Euler(0f, 0f, data.rotationZ);
    }

    private OrientationTransform GetData(PhysicalOrientation orientation)
    {
        switch (orientation)
        {
            case PhysicalOrientation.Portrait:
                return portrait;

            case PhysicalOrientation.PortraitUpsideDown:
                return portraitUpsideDown;

            case PhysicalOrientation.LandscapeLeft:
                return landscapeLeft;

            case PhysicalOrientation.LandscapeRight:
                return landscapeRight;
        }

        return portrait;
    }

    private PhysicalOrientation DetectOrientation()
    {
        DeviceOrientation deviceOrientation = Input.deviceOrientation;

        switch (deviceOrientation)
        {
            case DeviceOrientation.Portrait:
                return PhysicalOrientation.Portrait;

            case DeviceOrientation.PortraitUpsideDown:
                return PhysicalOrientation.PortraitUpsideDown;

            case DeviceOrientation.LandscapeLeft:
                return PhysicalOrientation.LandscapeLeft;

            case DeviceOrientation.LandscapeRight:
                return PhysicalOrientation.LandscapeRight;
        }

        if (fallbackToScreenRatio)
        {
            if (Screen.width > Screen.height)
                return PhysicalOrientation.LandscapeLeft;

            return PhysicalOrientation.Portrait;
        }

        return currentOrientation;
    }

    private void ConfigureAnchorsToCenter()
    {
        if (target == null)
            return;

        target.anchorMin = new Vector2(0.5f, 0.5f);
        target.anchorMax = new Vector2(0.5f, 0.5f);
        target.pivot = new Vector2(0.5f, 0.5f);
    }
}