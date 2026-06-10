using System.Collections;
using UnityEngine;

public class ImpactMeasurementVisualizer : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject impactMarkerPrefab;
    [SerializeField] private GameObject targetMarkerPrefab;
    [SerializeField] private ImpactDistanceLabel3D distanceLabelPrefab;

    [Header("Line")]
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Label Visual")]
    [SerializeField] private Sprite distanceLabelBackgroundSprite;

    [Header("Label Position")]
    [SerializeField] private Vector3 labelOffsetFromImpact = new Vector3(0f, 0.25f, 0f);
    [SerializeField] private bool placeLabelAtMiddleOfLine = false;
    [SerializeField] private Vector3 labelOffsetFromMiddle = new Vector3(0f, 0.25f, 0f);

    [Header("Settings")]
    [SerializeField] private float markerLifetime = 2.5f;
    [SerializeField] private float markerYOffset = 0.03f;
    [SerializeField] private bool hideLineOnStart = true;

    private GameObject impactMarkerInstance;
    private GameObject targetMarkerInstance;
    private ImpactDistanceLabel3D distanceLabelInstance;
    private Coroutine routine;

    private void Awake()
    {
        if (hideLineOnStart && lineRenderer != null)
            lineRenderer.enabled = false;
    }

    public void ShowMeasurement(
        Vector3 impactPoint,
        Vector3 targetPoint,
        float straightDistance,
        float horizontalDistance,
        float heightDifference
    )
    {
        Clear();

        routine = StartCoroutine(
            ShowMeasurementRoutine(
                impactPoint,
                targetPoint,
                straightDistance,
                horizontalDistance,
                heightDifference
            )
        );
    }

    public void Clear()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        if (impactMarkerInstance != null)
        {
            Destroy(impactMarkerInstance);
            impactMarkerInstance = null;
        }

        if (targetMarkerInstance != null)
        {
            Destroy(targetMarkerInstance);
            targetMarkerInstance = null;
        }

        if (distanceLabelInstance != null)
        {
            Destroy(distanceLabelInstance.gameObject);
            distanceLabelInstance = null;
        }

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
            lineRenderer.positionCount = 0;
        }
    }

    private IEnumerator ShowMeasurementRoutine(
        Vector3 impactPoint,
        Vector3 targetPoint,
        float straightDistance,
        float horizontalDistance,
        float heightDifference
    )
    {
        Vector3 adjustedImpactPoint = impactPoint + Vector3.up * markerYOffset;
        Vector3 adjustedTargetPoint = targetPoint + Vector3.up * markerYOffset;

        if (impactMarkerPrefab != null)
        {
            impactMarkerInstance = Instantiate(
                impactMarkerPrefab,
                adjustedImpactPoint,
                Quaternion.identity
            );
        }

        if (targetMarkerPrefab != null)
        {
            targetMarkerInstance = Instantiate(
                targetMarkerPrefab,
                adjustedTargetPoint,
                Quaternion.identity
            );
        }

        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, adjustedImpactPoint);
            lineRenderer.SetPosition(1, adjustedTargetPoint);
        }

        CreateDistanceLabel(
            impactPoint,
            targetPoint,
            straightDistance,
            horizontalDistance,
            heightDifference
        );

        yield return new WaitForSecondsRealtime(markerLifetime);

        Clear();
    }

    private void CreateDistanceLabel(
        Vector3 impactPoint,
        Vector3 targetPoint,
        float straightDistance,
        float horizontalDistance,
        float heightDifference
    )
    {
        if (distanceLabelPrefab == null)
            return;

        Vector3 labelPosition;

        if (placeLabelAtMiddleOfLine)
        {
            labelPosition = Vector3.Lerp(impactPoint, targetPoint, 0.5f)
                            + labelOffsetFromMiddle;
        }
        else
        {
            labelPosition = impactPoint + labelOffsetFromImpact;
        }

        distanceLabelInstance = Instantiate(
            distanceLabelPrefab,
            labelPosition,
            Quaternion.identity
        );

        distanceLabelInstance.Setup(
            straightDistance,
            horizontalDistance,
            heightDifference,
            distanceLabelBackgroundSprite
        );
    }

    private void OnDisable()
    {
        Clear();
    }
}