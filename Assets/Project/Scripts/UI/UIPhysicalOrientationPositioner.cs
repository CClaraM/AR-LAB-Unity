using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UIPhysicalOrientationPositioner : MonoBehaviour
{
    private enum Corner
    {
        BottomLeft,
        BottomRight,
        TopLeft,
        TopRight
    }

    [Header("Panel Size")]
    [SerializeField] private bool applyPanelSize = true;
    [SerializeField] private Vector2 size = new Vector2(800f, 300f);

    [Header("Margin")]
    [SerializeField] private Vector2 margin = new Vector2(20f, 20f);

    [Header("Rotation")]
    [SerializeField] private bool rotatePanelWithDevice = true;

    [Header("Corner Mapping")]
    [SerializeField] private Corner portraitCorner = Corner.BottomLeft;
    [SerializeField] private Corner portraitUpsideDownCorner = Corner.TopRight;
    [SerializeField] private Corner landscapeLeftCorner = Corner.TopLeft;
    [SerializeField] private Corner landscapeRightCorner = Corner.BottomRight;

    [Header("Rotation Mapping")]
    [SerializeField] private float portraitRotation = 0f;
    [SerializeField] private float portraitUpsideDownRotation = 180f;
    [SerializeField] private float landscapeLeftRotation = -90f;
    [SerializeField] private float landscapeRightRotation = 90f;

    [Header("Manual Offset Per Orientation")]
    [SerializeField] private Vector2 portraitExtraOffset = Vector2.zero;
    [SerializeField] private Vector2 portraitUpsideDownExtraOffset = Vector2.zero;
    [SerializeField] private Vector2 landscapeLeftExtraOffset = Vector2.zero;
    [SerializeField] private Vector2 landscapeRightExtraOffset = Vector2.zero;

    private RectTransform rectTransform;
    private DeviceOrientation lastOrientation = DeviceOrientation.Unknown;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplyForOrientation(GetStableOrientation());
    }

    private void Update()
    {
        DeviceOrientation currentOrientation = GetStableOrientation();

        if (currentOrientation == lastOrientation)
            return;

        ApplyForOrientation(currentOrientation);
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

    private void ApplyForOrientation(DeviceOrientation orientation)
    {
        lastOrientation = orientation;

        Corner corner = GetCornerForOrientation(orientation);
        float rotationZ = GetRotationForOrientation(orientation);
        Vector2 extraOffset = GetExtraOffsetForOrientation(orientation);

        ApplyLayout(corner, rotationZ, extraOffset);
    }

    private Corner GetCornerForOrientation(DeviceOrientation orientation)
    {
        switch (orientation)
        {
            case DeviceOrientation.Portrait:
                return portraitCorner;

            case DeviceOrientation.PortraitUpsideDown:
                return portraitUpsideDownCorner;

            case DeviceOrientation.LandscapeLeft:
                return landscapeLeftCorner;

            case DeviceOrientation.LandscapeRight:
                return landscapeRightCorner;

            default:
                return portraitCorner;
        }
    }

    private float GetRotationForOrientation(DeviceOrientation orientation)
    {
        switch (orientation)
        {
            case DeviceOrientation.Portrait:
                return portraitRotation;

            case DeviceOrientation.PortraitUpsideDown:
                return portraitUpsideDownRotation;

            case DeviceOrientation.LandscapeLeft:
                return landscapeLeftRotation;

            case DeviceOrientation.LandscapeRight:
                return landscapeRightRotation;

            default:
                return portraitRotation;
        }
    }

    private Vector2 GetExtraOffsetForOrientation(DeviceOrientation orientation)
    {
        switch (orientation)
        {
            case DeviceOrientation.Portrait:
                return portraitExtraOffset;

            case DeviceOrientation.PortraitUpsideDown:
                return portraitUpsideDownExtraOffset;

            case DeviceOrientation.LandscapeLeft:
                return landscapeLeftExtraOffset;

            case DeviceOrientation.LandscapeRight:
                return landscapeRightExtraOffset;

            default:
                return Vector2.zero;
        }
    }

    private void ApplyLayout(Corner corner, float rotationZ, Vector2 extraOffset)
    {
        if (applyPanelSize)
        {
            rectTransform.sizeDelta = size;
        }
        rectTransform.localScale = Vector3.one;

        if (rotatePanelWithDevice)
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
        else
            rectTransform.localRotation = Quaternion.identity;

        SetAnchorAndPivot(corner);

        Vector2 basePosition = GetBasePosition(corner);
        Vector2 compensation = rotatePanelWithDevice
            ? GetRotationCompensation(corner, rotationZ)
            : Vector2.zero;

        rectTransform.anchoredPosition = basePosition + compensation + extraOffset;
    }

    private void SetAnchorAndPivot(Corner corner)
    {
        switch (corner)
        {
            case Corner.BottomLeft:
                rectTransform.anchorMin = new Vector2(0f, 0f);
                rectTransform.anchorMax = new Vector2(0f, 0f);
                rectTransform.pivot = new Vector2(0f, 0f);
                break;

            case Corner.BottomRight:
                rectTransform.anchorMin = new Vector2(1f, 0f);
                rectTransform.anchorMax = new Vector2(1f, 0f);
                rectTransform.pivot = new Vector2(1f, 0f);
                break;

            case Corner.TopLeft:
                rectTransform.anchorMin = new Vector2(0f, 1f);
                rectTransform.anchorMax = new Vector2(0f, 1f);
                rectTransform.pivot = new Vector2(0f, 1f);
                break;

            case Corner.TopRight:
                rectTransform.anchorMin = new Vector2(1f, 1f);
                rectTransform.anchorMax = new Vector2(1f, 1f);
                rectTransform.pivot = new Vector2(1f, 1f);
                break;
        }
    }

    private Vector2 GetBasePosition(Corner corner)
    {
        switch (corner)
        {
            case Corner.BottomLeft:
                return new Vector2(margin.x, margin.y);

            case Corner.BottomRight:
                return new Vector2(-margin.x, margin.y);

            case Corner.TopLeft:
                return new Vector2(margin.x, -margin.y);

            case Corner.TopRight:
                return new Vector2(-margin.x, -margin.y);

            default:
                return margin;
        }
    }

    private Vector2 GetCurrentSize()
    {
        return applyPanelSize ? size : rectTransform.sizeDelta;
    }

    private Vector2 GetRotationCompensation(Corner corner, float rotationZ)
    {
        Vector2 currentSize = GetCurrentSize();
        float width = currentSize.x;
        float height = currentSize.y;

        rotationZ = NormalizeAngle(rotationZ);

        if (Mathf.Approximately(rotationZ, 0f))
            return Vector2.zero;

        switch (corner)
        {
            case Corner.BottomLeft:
                if (Mathf.Approximately(rotationZ, 90f))
                    return new Vector2(height, 0f);

                if (Mathf.Approximately(rotationZ, 270f))
                    return new Vector2(0f, width);

                if (Mathf.Approximately(rotationZ, 180f))
                    return new Vector2(width, height);
                break;

            case Corner.BottomRight:
                if (Mathf.Approximately(rotationZ, 90f))
                    return new Vector2(0f, width);

                if (Mathf.Approximately(rotationZ, 270f))
                    return new Vector2(-height, 0f);

                if (Mathf.Approximately(rotationZ, 180f))
                    return new Vector2(-width, height);
                break;

            case Corner.TopLeft:
                if (Mathf.Approximately(rotationZ, 90f))
                    return new Vector2(height, 0f);

                if (Mathf.Approximately(rotationZ, 270f))
                    return new Vector2(0f, -width);

                if (Mathf.Approximately(rotationZ, 180f))
                    return new Vector2(width, -height);
                break;

            case Corner.TopRight:
                if (Mathf.Approximately(rotationZ, 90f))
                    return new Vector2(0f, -width);

                if (Mathf.Approximately(rotationZ, 270f))
                    return new Vector2(-height, 0f);

                if (Mathf.Approximately(rotationZ, 180f))
                    return new Vector2(-width, -height);
                break;
        }

        return Vector2.zero;
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;

        if (angle < 0f)
            angle += 360f;

        return angle;
    }
}