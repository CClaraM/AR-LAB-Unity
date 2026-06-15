using System.Collections;
using UnityEngine;

public class UIRotationPingPong : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private RectTransform target;

    [Header("Rotation")]
    [SerializeField] private float maxAngle = 15f;
    [SerializeField] private float rotationSpeed = 45f;

    [Header("Behavior")]
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private bool startFromNegativeAngle = true;

    private Coroutine rotationRoutine;
    private Quaternion originalRotation;

    private void Reset()
    {
        target = GetComponent<RectTransform>();
    }

    private void Awake()
    {
        if (target == null)
            target = GetComponent<RectTransform>();

        if (target != null)
            originalRotation = target.localRotation;
    }

    private void OnEnable()
    {
        if (target != null)
        {
            originalRotation = target.localRotation;

            float startAngle = startFromNegativeAngle ? -maxAngle : maxAngle;
            SetRotation(startAngle);
        }

        if (playOnEnable)
            Play();
    }

    private void OnDisable()
    {
        Stop();

        if (target != null)
            target.localRotation = originalRotation;
    }

    public void Play()
    {
        if (target == null)
            return;

        Stop();
        rotationRoutine = StartCoroutine(RotationRoutine());
    }

    public void Stop()
    {
        if (rotationRoutine != null)
        {
            StopCoroutine(rotationRoutine);
            rotationRoutine = null;
        }
    }

    private IEnumerator RotationRoutine()
    {
        float currentAngle = startFromNegativeAngle ? -maxAngle : maxAngle;
        float targetAngle = -currentAngle;

        SetRotation(currentAngle);

        while (true)
        {
            while (!Mathf.Approximately(currentAngle, targetAngle))
            {
                float delta = rotationSpeed * GetDeltaTime();

                currentAngle = Mathf.MoveTowards(
                    currentAngle,
                    targetAngle,
                    delta
                );

                SetRotation(currentAngle);

                yield return null;
            }

            targetAngle = -targetAngle;
            yield return null;
        }
    }

    private void SetRotation(float angle)
    {
        if (target == null)
            return;

        target.localRotation =
            originalRotation * Quaternion.Euler(0f, 0f, angle);
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }
}