using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UIPhysicalOrientationRotator : MonoBehaviour
{
    [Header("Rotation Mapping")]
    [SerializeField] private float portraitRotation = 0f;
    [SerializeField] private float portraitUpsideDownRotation = 180f;
    [SerializeField] private float landscapeLeftRotation = -90f;
    [SerializeField] private float landscapeRightRotation = 90f;

    private RectTransform rectTransform;
    private DeviceOrientation lastOrientation = DeviceOrientation.Unknown;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplyRotation(GetStableOrientation());
    }

    private void Update()
    {
        DeviceOrientation currentOrientation = GetStableOrientation();

        if (currentOrientation == lastOrientation)
            return;

        ApplyRotation(currentOrientation);
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

    private void ApplyRotation(DeviceOrientation orientation)
    {
        lastOrientation = orientation;

        float rotationZ = GetRotationForOrientation(orientation);
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
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
}