using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPlacementManager : MonoBehaviour
{
    private enum PlacementStep
    {
        PlaceCannon,
        PlaceTarget,
        Finished
    }

    [Header("UI")]
    [SerializeField] private CannonUIController cannonUIController;

    [Header("Lab Controller")]
    [SerializeField] private ARPhysicsLabController labController;

    [Header("AR Managers")]
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;

    [Header("Prefabs")]
    [SerializeField] private GameObject cannonPrefab;
    [SerializeField] private GameObject targetPrefab;
    [SerializeField] private TrajectoryPreview trajectoryPreview;

    [Header("Orientation")]
    [SerializeField] private bool rotateCannonTowardTarget = true;
    [SerializeField] private bool rotateTargetTowardCannon = true;

    [Tooltip("Usa esto si el modelo del cañón no apunta con su eje Z+ hacia adelante.")]
    [SerializeField] private Vector3 cannonRotationOffset = Vector3.zero;

    [Tooltip("Usa esto si el modelo del target no apunta con su eje Z+ hacia adelante.")]
    [SerializeField] private Vector3 targetRotationOffset = Vector3.zero;

    [Header("Plane Visibility")]
    [SerializeField] private bool hidePlanesAfterEachPlacement = true;
    [SerializeField] private bool stopPlaneDetectionAfterTarget = true;

    private GameObject spawnedCannon;
    private GameObject spawnedTarget;

    private bool placementEnabled = true;

    private PlacementStep currentStep = PlacementStep.PlaceCannon;

    private static readonly List<ARRaycastHit> hits = new();

    public void SetPlacementEnabled(bool enabled)
    {
        placementEnabled = enabled;
    }

    private void Update()
    {
        if (!placementEnabled)
            return;

        if (currentStep == PlacementStep.Finished)
            return;

        if (Touchscreen.current == null)
            return;

        var touch = Touchscreen.current.primaryTouch;

        if (!touch.press.wasPressedThisFrame)
            return;

        Vector2 screenPosition = touch.position.ReadValue();

        if (IsPointerOverUI())
            return;

        if (raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;

            if (currentStep == PlacementStep.PlaceCannon)
            {
                PlaceCannon(hitPose);
            }
            else if (currentStep == PlacementStep.PlaceTarget)
            {
                PlaceTarget(hitPose);
            }
        }
    }

    private void PlaceCannon(Pose pose)
    {
        if (cannonPrefab == null)
        {
            Debug.LogError("Cannon Prefab no está asignado.");
            return;
        }

        spawnedCannon = Instantiate(cannonPrefab, pose.position, pose.rotation);
        spawnedCannon.transform.rotation *= Quaternion.Euler(cannonRotationOffset);

        if (labController != null)
        {
            labController.NotifyCannonPlaced(spawnedCannon);
        }

        CannonLauncher launcher = spawnedCannon.GetComponentInChildren<CannonLauncher>();
        CannonAimController aimController = spawnedCannon.GetComponentInChildren<CannonAimController>();

        if (cannonUIController != null)
        {
            cannonUIController.SetCannon(launcher, aimController);
        }

        if (trajectoryPreview != null && launcher != null)
        {
            trajectoryPreview.SetCannonLauncher(launcher);
            trajectoryPreview.SetVisible(true);
        }

        currentStep = PlacementStep.PlaceTarget;

        if (hidePlanesAfterEachPlacement)
            SetPlaneVisualsVisible(false);

        if (cannonUIController != null)
        {
            cannonUIController.ShowControls(false);
        }

        Debug.Log("Cañón colocado. Ahora toca otro punto para colocar el target.");
    }

    private void PlaceTarget(Pose pose)
    {
        if (targetPrefab == null)
        {
            Debug.LogError("Target Prefab no está asignado.");
            return;
        }

        spawnedTarget = Instantiate(targetPrefab, pose.position, pose.rotation);
        spawnedTarget.transform.rotation *= Quaternion.Euler(targetRotationOffset);

        OrientObjects();

        if (labController != null)
        {
            labController.NotifyTargetPlaced(spawnedTarget);
        }

        currentStep = PlacementStep.Finished;

        if (cannonUIController != null)
        {
            cannonUIController.ShowControls(true);
        }

        if (hidePlanesAfterEachPlacement)
            SetPlaneVisualsVisible(false);

        if (stopPlaneDetectionAfterTarget)
            StopPlaneDetection();

        Debug.Log("Target colocado. Detección de planos detenida.");
    }

    private void OrientObjects()
    {
        if (spawnedCannon == null || spawnedTarget == null)
            return;

        Vector3 cannonPosition = spawnedCannon.transform.position;
        Vector3 targetPosition = spawnedTarget.transform.position;

        if (rotateCannonTowardTarget)
        {
            Vector3 directionToTarget = targetPosition - cannonPosition;
            directionToTarget.y = 0f;

            if (directionToTarget.sqrMagnitude > 0.001f)
            {
                spawnedCannon.transform.rotation =
                    Quaternion.LookRotation(directionToTarget.normalized, Vector3.up)
                    * Quaternion.Euler(cannonRotationOffset);
            }
        }

        if (rotateTargetTowardCannon)
        {
            Vector3 directionToCannon = cannonPosition - targetPosition;
            directionToCannon.y = 0f;

            if (directionToCannon.sqrMagnitude > 0.001f)
            {
                spawnedTarget.transform.rotation =
                    Quaternion.LookRotation(directionToCannon.normalized, Vector3.up)
                    * Quaternion.Euler(targetRotationOffset);
            }
        }
    }

    private void SetPlaneVisualsVisible(bool visible)
    {
        if (planeManager == null)
            return;

        foreach (ARPlane plane in planeManager.trackables)
        {
            MeshRenderer meshRenderer = plane.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
                meshRenderer.enabled = visible;

            LineRenderer lineRenderer = plane.GetComponent<LineRenderer>();
            if (lineRenderer != null)
                lineRenderer.enabled = visible;

            ARPlaneMeshVisualizer meshVisualizer = plane.GetComponent<ARPlaneMeshVisualizer>();
            if (meshVisualizer != null)
                meshVisualizer.enabled = visible;

            Renderer[] childRenderers = plane.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer childRenderer in childRenderers)
            {
                childRenderer.enabled = visible;
            }
        }
    }

    private void StopPlaneDetection()
    {
        if (planeManager != null)
        {
            planeManager.requestedDetectionMode = PlaneDetectionMode.None;
            planeManager.enabled = false;
        }

        if (raycastManager != null)
        {
            raycastManager.enabled = false;
        }

        SetPlaneVisualsVisible(false);
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        return EventSystem.current.IsPointerOverGameObject();
    }

    public void ResetPlacement()
    {
        if (spawnedCannon != null)
            Destroy(spawnedCannon);

        if (spawnedTarget != null)
            Destroy(spawnedTarget);

        spawnedCannon = null;
        spawnedTarget = null;

        currentStep = PlacementStep.PlaceCannon;

        if (planeManager != null)
        {
            planeManager.enabled = true;
            planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
        }

        if (raycastManager != null)
        {
            raycastManager.enabled = true;
        }

        SetPlaneVisualsVisible(true);
    }
}