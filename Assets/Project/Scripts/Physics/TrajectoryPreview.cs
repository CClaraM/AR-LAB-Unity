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

    [Header("Partial Visibility")]
    [SerializeField] private bool hideAfterDescentStarts = true;
    [SerializeField] private int fadePointsAfterApex = 5;
    [SerializeField] private float descentStartTolerance = 0.01f;

    [Header("Line Alpha")]
    [Range(0f, 1f)]
    [SerializeField] private float visibleAlpha = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float endAlpha = 0f;

    [Header("Collision Preview")]
    [SerializeField] private bool stopOnCollision = true;
    [SerializeField] private LayerMask collisionMask = ~0;

    private LineRenderer lineRenderer;
    private Color startColor = Color.white;
    private Color endColor = Color.white;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = pointsCount;

        startColor = lineRenderer.startColor;
        endColor = lineRenderer.endColor;
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
        Vector3 previousVelocity = startVelocity;

        int visiblePoints = pointsCount;
        int descentStartIndex = -1;
        int fadeEndIndex = -1;

        for (int i = 0; i < pointsCount; i++)
        {
            float time = i * timeStep;

            Vector3 point =
                startPosition +
                startVelocity * time +
                0.5f * gravity * time * time;

            Vector3 velocityAtPoint = startVelocity + gravity * time;

            if (hideAfterDescentStarts && descentStartIndex < 0)
            {
                bool wasGoingUp = previousVelocity.y > descentStartTolerance;
                bool nowGoingDown = velocityAtPoint.y <= descentStartTolerance;

                if (i > 0 && wasGoingUp && nowGoingDown)
                {
                    descentStartIndex = i;
                    fadeEndIndex = Mathf.Min(pointsCount - 1, descentStartIndex + fadePointsAfterApex);
                    visiblePoints = fadeEndIndex + 1;
                }
            }

            if (stopOnCollision && i > 0)
            {
                Vector3 direction = point - previousPoint;
                float distance = direction.magnitude;

                if (distance > 0.0001f &&
                    Physics.Raycast(previousPoint, direction.normalized, out RaycastHit hit, distance, collisionMask))
                {
                    lineRenderer.SetPosition(i, hit.point);
                    visiblePoints = Mathf.Min(visiblePoints, i + 1);
                    break;
                }
            }

            lineRenderer.SetPosition(i, point);
            previousPoint = point;
            previousVelocity = velocityAtPoint;

            if (hideAfterDescentStarts && fadeEndIndex >= 0 && i >= fadeEndIndex)
                break;
        }

        lineRenderer.positionCount = visiblePoints;
        ApplyTrajectoryGradient(visiblePoints, descentStartIndex, fadeEndIndex);
    }

    private void ApplyTrajectoryGradient(int visiblePoints, int descentStartIndex, int fadeEndIndex)
    {
        if (lineRenderer == null)
            return;

        if (visiblePoints <= 1)
            return;

        Color visibleStart = startColor;
        Color visibleEnd = endColor;

        visibleStart.a = visibleAlpha;
        visibleEnd.a = visibleAlpha;

        Gradient gradient = new Gradient();

        if (!hideAfterDescentStarts || descentStartIndex < 0 || fadeEndIndex <= descentStartIndex)
        {
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(visibleStart, 0f),
                    new GradientColorKey(visibleEnd, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(visibleAlpha, 0f),
                    new GradientAlphaKey(visibleAlpha, 1f)
                }
            );

            lineRenderer.colorGradient = gradient;
            return;
        }

        float descentStartTime = Mathf.Clamp01((float)descentStartIndex / (visiblePoints - 1));
        float fadeEndTime = Mathf.Clamp01((float)fadeEndIndex / (visiblePoints - 1));

        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(visibleStart, 0f),
                new GradientColorKey(visibleEnd, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(visibleAlpha, 0f),
                new GradientAlphaKey(visibleAlpha, descentStartTime),
                new GradientAlphaKey(endAlpha, fadeEndTime),
                new GradientAlphaKey(endAlpha, 1f)
            }
        );

        lineRenderer.colorGradient = gradient;
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