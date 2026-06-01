using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryPreview : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CannonLauncher cannonLauncher;

    [Header("Trajectory Settings")]
    [SerializeField] private int pointsCount = 40;
    [SerializeField] private float timeStep = 0.05f;
    [SerializeField] private bool showTrajectory = true;

    [Header("Collision Preview")]
    [SerializeField] private bool stopOnCollision = true;
    [SerializeField] private LayerMask collisionMask = ~0;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = pointsCount;
    }

    private void Update()
    {
        if (!showTrajectory || cannonLauncher == null || cannonLauncher.FirePoint == null)
        {
            lineRenderer.enabled = false;
            return;
        }

        DrawTrajectory();
    }

    private void DrawTrajectory()
    {
        lineRenderer.enabled = true;

        Vector3 startPosition = cannonLauncher.FirePoint.position;
        Vector3 startVelocity = cannonLauncher.FirePoint.forward * cannonLauncher.CurrentLaunchPower;
        Vector3 gravity = Physics.gravity;

        Vector3 previousPoint = startPosition;
        int visiblePoints = pointsCount;

        for (int i = 0; i < pointsCount; i++)
        {
            float time = i * timeStep;

            Vector3 point =
                startPosition +
                startVelocity * time +
                0.5f * gravity * time * time;

            if (stopOnCollision && i > 0)
            {
                Vector3 direction = point - previousPoint;
                float distance = direction.magnitude;

                if (Physics.Raycast(previousPoint, direction.normalized, out RaycastHit hit, distance, collisionMask))
                {
                    lineRenderer.SetPosition(i, hit.point);
                    visiblePoints = i + 1;
                    break;
                }
            }

            lineRenderer.SetPosition(i, point);
            previousPoint = point;
        }

        lineRenderer.positionCount = visiblePoints;
    }

    public void SetCannonLauncher(CannonLauncher launcher)
    {
        cannonLauncher = launcher;
    }

    public void SetVisible(bool visible)
    {
        showTrajectory = visible;

        if (lineRenderer != null)
            lineRenderer.enabled = visible;
    }
}