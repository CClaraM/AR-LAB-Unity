using TMPro;
using UnityEngine;

public class ImpactDistanceLabel3D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private TextMeshPro distanceText;

    [Header("Facing")]
    [SerializeField] private bool faceCamera = true;
    [SerializeField] private bool keepUpright = true;

    [Header("Scale")]
    [SerializeField] private float labelScale = 0.08f;

    [Header("Text Format")]
    [SerializeField]
    private string format =
        "Distancia: {0:0.00} m\nHorizontal: {1:0.00} m\nAltura: {2:0.00} m";

    private void Awake()
    {
        ApplyScale();
    }

    private void OnValidate()
    {
        ApplyScale();
    }

    public void Setup(
        float straightDistance,
        float horizontalDistance,
        float heightDifference,
        Sprite backgroundSprite = null
    )
    {
        ApplyScale();

        if (backgroundRenderer != null && backgroundSprite != null)
        {
            backgroundRenderer.sprite = backgroundSprite;
        }

        if (distanceText != null)
        {
            distanceText.text = string.Format(
                format,
                straightDistance,
                horizontalDistance,
                heightDifference
            );
        }
    }

    private void ApplyScale()
    {
        if (visualRoot != null)
        {
            visualRoot.localScale = Vector3.one * labelScale;
        }
    }

    private void LateUpdate()
    {
        if (!faceCamera)
            return;

        Camera cam = Camera.main;

        if (cam == null)
            return;

        Vector3 direction = transform.position - cam.transform.position;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion lookRotation = Quaternion.LookRotation(direction.normalized);

        if (keepUpright)
        {
            Vector3 euler = lookRotation.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
        }
        else
        {
            transform.rotation = lookRotation;
        }
    }
}