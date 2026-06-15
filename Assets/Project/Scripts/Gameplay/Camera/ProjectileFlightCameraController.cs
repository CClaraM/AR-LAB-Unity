using System;
using System.Collections;
using UnityEngine;

public class ProjectileFlightCameraController : MonoBehaviour
{
    private enum FlightCameraPhase
    {
        Ascending,
        Apex,
        Descending
    }

    [Header("Cameras")]
    [SerializeField] private Camera arCamera;
    [SerializeField] private Camera projectileCamera;

    [Header("Restore Delay")]
    [SerializeField] private float restoreDelayAfterImpact = 1.2f;

    [Header("UI To Hide During Flight")]
    [SerializeField] private CanvasGroup[] uiGroupsToHide;
    [SerializeField] private float uiFadeDuration = 0.15f;

    [Header("Phase Detection")]
    [SerializeField] private float apexVerticalVelocityThreshold = 0.18f;
    [SerializeField] private float descendingVerticalVelocityThreshold = -0.12f;

    [Header("Ascending View")]
    [SerializeField] private Vector3 ascendingOffset = new Vector3(0f, -0.25f, -0.85f);
    [SerializeField] private Vector3 ascendingLookOffset = new Vector3(0f, 0.12f, 0f);

    [Header("Apex / Transition View")]
    [SerializeField] private Vector3 apexOffset = new Vector3(0f, 0.1f, -0.55f);
    [SerializeField] private Vector3 apexLookOffset = new Vector3(0f, 0.05f, 0f);

    [Header("Descending View")]
    [SerializeField] private Vector3 descendingOffset = new Vector3(0f, 0.85f, -0.18f);
    [SerializeField] private Vector3 descendingLookOffset = new Vector3(0f, -0.08f, 0f);

    [Header("Smooth Movement")]
    [SerializeField] private float positionSmooth = 7.5f;
    [SerializeField] private float rotationSmooth = 9f;
    [SerializeField] private float offsetBlendSmooth = 4.5f;

    [Header("Camera Feel")]
    [SerializeField] private bool useProjectileLocalDirection = true;
    [SerializeField] private float minimumCameraDistance = 0.25f;

    private Transform projectileTarget;
    private Rigidbody projectileRigidbody;

    private Coroutine followRoutine;
    private Coroutine uiRoutine;

    private bool active;

    private Vector3 currentOffset;
    private Vector3 currentLookOffset;

    public bool IsActive => active;

    private void Awake()
    {
        currentOffset = ascendingOffset;
        currentLookOffset = ascendingLookOffset;

        SetProjectileCameraActive(false);
    }

    public void StartFollow(Transform projectile)
    {
        if (projectile == null)
        {
            Debug.LogWarning("ProjectileFlightCameraController: projectile es null.");
            return;
        }

        if (projectileCamera == null)
        {
            Debug.LogWarning("ProjectileFlightCameraController: falta Projectile Camera.");
            return;
        }

        projectileTarget = projectile;
        projectileRigidbody = projectile.GetComponent<Rigidbody>();

        if (projectileRigidbody == null)
            projectileRigidbody = projectile.GetComponentInChildren<Rigidbody>();

        currentOffset = ascendingOffset;
        currentLookOffset = ascendingLookOffset;

        active = true;

        SetProjectileCameraActive(true);
        HideUI();

        PositionCameraImmediately();

        if (followRoutine != null)
            StopCoroutine(followRoutine);

        followRoutine = StartCoroutine(FollowRoutine());

        Debug.Log("ProjectileFlightCameraController: cámara dinámica activada.");
    }

    public void RestoreARCamera()
    {
        if (followRoutine != null)
        {
            StopCoroutine(followRoutine);
            followRoutine = null;
        }

        projectileTarget = null;
        projectileRigidbody = null;
        active = false;

        SetProjectileCameraActive(false);
        ShowUI();

        Debug.Log("ProjectileFlightCameraController: cámara AR restaurada.");
    }

    private IEnumerator FollowRoutine()
    {
        while (active && projectileTarget != null)
        {
            FlightCameraPhase phase = DetectPhase();

            Vector3 targetOffset = GetOffsetForPhase(phase);
            Vector3 targetLookOffset = GetLookOffsetForPhase(phase);

            currentOffset = Vector3.Lerp(
                currentOffset,
                targetOffset,
                Time.unscaledDeltaTime * offsetBlendSmooth
            );

            currentLookOffset = Vector3.Lerp(
                currentLookOffset,
                targetLookOffset,
                Time.unscaledDeltaTime * offsetBlendSmooth
            );

            Vector3 desiredPosition = GetWorldCameraPosition(currentOffset);
            Vector3 lookPoint = projectileTarget.position + currentLookOffset;

            float distance = Vector3.Distance(desiredPosition, lookPoint);

            if (distance < minimumCameraDistance)
            {
                Vector3 awayDirection = (desiredPosition - lookPoint).normalized;

                if (awayDirection.sqrMagnitude < 0.0001f)
                    awayDirection = -projectileTarget.forward;

                desiredPosition = lookPoint + awayDirection * minimumCameraDistance;
            }

            projectileCamera.transform.position = Vector3.Lerp(
                projectileCamera.transform.position,
                desiredPosition,
                Time.unscaledDeltaTime * positionSmooth
            );

            Vector3 direction = lookPoint - projectileCamera.transform.position;

            if (direction.sqrMagnitude > 0.0001f)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(direction.normalized);

                projectileCamera.transform.rotation = Quaternion.Slerp(
                    projectileCamera.transform.rotation,
                    desiredRotation,
                    Time.unscaledDeltaTime * rotationSmooth
                );
            }

            yield return null;
        }
    }

    private FlightCameraPhase DetectPhase()
    {
        float verticalVelocity = GetVerticalVelocity();

        if (verticalVelocity > apexVerticalVelocityThreshold)
            return FlightCameraPhase.Ascending;

        if (verticalVelocity < descendingVerticalVelocityThreshold)
            return FlightCameraPhase.Descending;

        return FlightCameraPhase.Apex;
    }

    private float GetVerticalVelocity()
    {
        if (projectileRigidbody == null)
            return 0f;

#if UNITY_6000_0_OR_NEWER
        return projectileRigidbody.linearVelocity.y;
#else
        return projectileRigidbody.velocity.y;
#endif
    }

    private Vector3 GetOffsetForPhase(FlightCameraPhase phase)
    {
        switch (phase)
        {
            case FlightCameraPhase.Ascending:
                return ascendingOffset;

            case FlightCameraPhase.Apex:
                return apexOffset;

            case FlightCameraPhase.Descending:
                return descendingOffset;
        }

        return ascendingOffset;
    }

    private Vector3 GetLookOffsetForPhase(FlightCameraPhase phase)
    {
        switch (phase)
        {
            case FlightCameraPhase.Ascending:
                return ascendingLookOffset;

            case FlightCameraPhase.Apex:
                return apexLookOffset;

            case FlightCameraPhase.Descending:
                return descendingLookOffset;
        }

        return Vector3.zero;
    }

    private Vector3 GetWorldCameraPosition(Vector3 offset)
    {
        if (projectileTarget == null)
            return transform.position;

        if (useProjectileLocalDirection)
            return projectileTarget.position + projectileTarget.TransformDirection(offset);

        return projectileTarget.position + offset;
    }

    private void PositionCameraImmediately()
    {
        if (projectileCamera == null || projectileTarget == null)
            return;

        Vector3 desiredPosition = GetWorldCameraPosition(currentOffset);
        Vector3 lookPoint = projectileTarget.position + currentLookOffset;

        projectileCamera.transform.position = desiredPosition;

        Vector3 direction = lookPoint - projectileCamera.transform.position;

        if (direction.sqrMagnitude > 0.0001f)
            projectileCamera.transform.rotation = Quaternion.LookRotation(direction.normalized);
    }

    private void SetProjectileCameraActive(bool value)
    {
        if (projectileCamera != null)
        {
            projectileCamera.gameObject.SetActive(value);
            projectileCamera.enabled = value;
        }

        if (arCamera != null)
            arCamera.enabled = !value;
    }

    private void HideUI()
    {
        FadeUI(0f, false);
    }

    private void ShowUI()
    {
        FadeUI(1f, true);
    }

    private void FadeUI(float targetAlpha, bool interactableAfter)
    {
        if (uiRoutine != null)
            StopCoroutine(uiRoutine);

        uiRoutine = StartCoroutine(FadeUIRoutine(targetAlpha, interactableAfter));
    }

    private IEnumerator FadeUIRoutine(float targetAlpha, bool interactableAfter)
    {
        if (uiGroupsToHide == null || uiGroupsToHide.Length == 0)
            yield break;

        float elapsed = 0f;
        float[] startAlpha = new float[uiGroupsToHide.Length];

        for (int i = 0; i < uiGroupsToHide.Length; i++)
        {
            if (uiGroupsToHide[i] == null)
                continue;

            startAlpha[i] = uiGroupsToHide[i].alpha;
            uiGroupsToHide[i].interactable = false;
            uiGroupsToHide[i].blocksRaycasts = false;
        }

        if (uiFadeDuration <= 0f)
        {
            ApplyUIFinalState(targetAlpha, interactableAfter);
            yield break;
        }

        while (elapsed < uiFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / uiFadeDuration);

            for (int i = 0; i < uiGroupsToHide.Length; i++)
            {
                if (uiGroupsToHide[i] != null)
                {
                    uiGroupsToHide[i].alpha =
                        Mathf.Lerp(startAlpha[i], targetAlpha, t);
                }
            }

            yield return null;
        }

        ApplyUIFinalState(targetAlpha, interactableAfter);
    }

    private void ApplyUIFinalState(float alpha, bool interactable)
    {
        if (uiGroupsToHide == null)
            return;

        foreach (CanvasGroup group in uiGroupsToHide)
        {
            if (group == null)
                continue;

            group.alpha = alpha;
            group.interactable = interactable;
            group.blocksRaycasts = interactable;
        }
    }

    public void RestoreARCameraAfterImpactDelay(Action onFinished = null)
    {
        if (!active)
        {
            onFinished?.Invoke();
            return;
        }

        if (followRoutine != null)
        {
            StopCoroutine(followRoutine);
            followRoutine = null;
        }

        StartCoroutine(RestoreAfterDelayRoutine(onFinished));
    }

    private IEnumerator RestoreAfterDelayRoutine(Action onFinished)
    {
        yield return new WaitForSecondsRealtime(restoreDelayAfterImpact);

        RestoreARCamera();

        onFinished?.Invoke();
    }
}