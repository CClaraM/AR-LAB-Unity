using UnityEngine;

public class CannonAimController : MonoBehaviour
{
    [Header("Pivots")]
    [SerializeField] private Transform yawPivot;
    [SerializeField] private Transform pitchPivot;

    [Header("Yaw Settings")]
    [SerializeField] private float yawStepDegrees = 2f;
    [SerializeField] private float minYaw = -45f;
    [SerializeField] private float maxYaw = 45f;

    [Header("Pitch Settings")]
    [SerializeField] private float pitchStepDegrees = 1f;
    [SerializeField] private float minPitch = -5f;
    [SerializeField] private float maxPitch = 80f;

    [Header("Initial Values")]
    [SerializeField] private float initialYaw = 0f;
    [SerializeField] private float initialPitch = 30f;

    private float currentYaw;
    private float currentPitch;

    public float CurrentYaw => currentYaw;
    public float CurrentPitch => currentPitch;

    public float MinYaw => minYaw;
    public float MaxYaw => maxYaw;
    public float MinPitch => minPitch;
    public float MaxPitch => maxPitch;

    private void Start()
    {
        currentYaw = Mathf.Clamp(initialYaw, minYaw, maxYaw);
        currentPitch = Mathf.Clamp(initialPitch, minPitch, maxPitch);

        ApplyRotation();
    }

    public void RotateLeft()
    {
        RotateYaw(-yawStepDegrees);
    }

    public void RotateRight()
    {
        RotateYaw(yawStepDegrees);
    }

    public void AimUp()
    {
        RotatePitch(pitchStepDegrees);
    }

    public void AimDown()
    {
        RotatePitch(-pitchStepDegrees);
    }

    public void RotateYaw(float degrees)
    {
        SetYaw(currentYaw + degrees);
    }

    public void RotatePitch(float degrees)
    {
        SetPitch(currentPitch + degrees);
    }

    public void SetYaw(float degrees)
    {
        currentYaw = Mathf.Clamp(degrees, minYaw, maxYaw);
        ApplyRotation();
    }

    public void SetPitch(float degrees)
    {
        currentPitch = Mathf.Clamp(degrees, minPitch, maxPitch);
        ApplyRotation();
    }

    public void ResetAim()
    {
        currentYaw = Mathf.Clamp(initialYaw, minYaw, maxYaw);
        currentPitch = Mathf.Clamp(initialPitch, minPitch, maxPitch);

        ApplyRotation();
    }

    private void ApplyRotation()
    {
        if (yawPivot != null)
        {
            yawPivot.localRotation = Quaternion.Euler(0f, currentYaw, 0f);
        }

        if (pitchPivot != null)
        {
            pitchPivot.localRotation = Quaternion.Euler(-currentPitch, 0f, 0f);
        }
    }
}