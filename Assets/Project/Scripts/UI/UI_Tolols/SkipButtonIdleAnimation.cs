using System.Collections;
using UnityEngine;

public class SkipButtonIdleAnimation : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private RectTransform target;

    [Header("Movement")]
    [SerializeField] private float moveAmountX = 18f;
    [SerializeField] private float moveDuration = 0.45f;
    [SerializeField] private float pauseAtEnds = 0.08f;

    [Header("Scale")]
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float expandedScale = 1.12f;

    [Header("Behavior")]
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool useUnscaledTime = true;

    private Vector2 originalAnchoredPosition;
    private Coroutine animationRoutine;

    private void Reset()
    {
        target = GetComponent<RectTransform>();
    }

    private void Awake()
    {
        if (target == null)
            target = GetComponent<RectTransform>();

        if (target != null)
            originalAnchoredPosition = target.anchoredPosition;
    }

    private void OnEnable()
    {
        if (target != null)
        {
            originalAnchoredPosition = target.anchoredPosition;
            target.localScale = Vector3.one * normalScale;
        }

        if (playOnEnable)
            Play();
    }

    private void OnDisable()
    {
        Stop();

        if (target != null)
        {
            target.anchoredPosition = originalAnchoredPosition;
            target.localScale = Vector3.one * normalScale;
        }
    }

    public void Play()
    {
        if (target == null)
            return;

        Stop();

        animationRoutine = StartCoroutine(AnimationRoutine());
    }

    public void Stop()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }
    }

    private IEnumerator AnimationRoutine()
    {
        Vector2 leftPosition = originalAnchoredPosition;
        Vector2 rightPosition = originalAnchoredPosition + new Vector2(moveAmountX, 0f);

        Vector3 normal = Vector3.one * normalScale;
        Vector3 expanded = Vector3.one * expandedScale;

        while (true)
        {
            yield return Animate(
                leftPosition,
                rightPosition,
                normal,
                expanded,
                moveDuration
            );

            yield return Wait(pauseAtEnds);

            yield return Animate(
                rightPosition,
                leftPosition,
                expanded,
                normal,
                moveDuration
            );

            yield return Wait(pauseAtEnds);
        }
    }

    private IEnumerator Animate(
        Vector2 fromPosition,
        Vector2 toPosition,
        Vector3 fromScale,
        Vector3 toScale,
        float duration
    )
    {
        float elapsed = 0f;

        if (duration <= 0f)
        {
            target.anchoredPosition = toPosition;
            target.localScale = toScale;
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += GetDeltaTime();
            float t = Mathf.Clamp01(elapsed / duration);

            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            target.anchoredPosition = Vector2.Lerp(
                fromPosition,
                toPosition,
                smoothT
            );

            target.localScale = Vector3.Lerp(
                fromScale,
                toScale,
                smoothT
            );

            yield return null;
        }

        target.anchoredPosition = toPosition;
        target.localScale = toScale;
    }

    private IEnumerator Wait(float duration)
    {
        if (duration <= 0f)
            yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += GetDeltaTime();
            yield return null;
        }
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }
}