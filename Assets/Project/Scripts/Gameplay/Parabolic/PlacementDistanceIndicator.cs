using System.Collections;
using UnityEngine;

public class PlacementDistanceIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Renderer indicatorRenderer;

    [Header("Materials")]
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material invalidMaterial;

    [Header("Placement")]
    [SerializeField] private float yOffset = 0.01f;
    [SerializeField] private bool followTarget = true;

    [Header("Fade")]
    [SerializeField] private float showFadeDuration = 0.35f;
    [SerializeField] private float hideFadeDuration = 0.25f;
    [SerializeField] private float materialFadeDuration = 0.18f;

    [Header("Invalid State")]
    [SerializeField] private float invalidVisibleDuration = 1.2f;
    [SerializeField] private bool autoReturnToNormal = true;

    private Transform followTransform;
    private float currentRadius = 1f;

    private Material runtimeMaterial;
    private Coroutine fadeRoutine;
    private Coroutine invalidRoutine;

    private Material currentSourceMaterial;
    private bool isVisible;

    private void Awake()
    {
        PrepareRuntimeMaterial(normalMaterial);
        SetAlphaImmediate(0f);
        gameObject.SetActive(false);
    }

    public void Setup(Transform target, float radius)
    {
        followTransform = target;
        currentRadius = Mathf.Max(0.05f, radius);

        UpdateTransform();
        ShowAnimated(true);
    }

    public void Show(bool visible)
    {
        if (visible)
            ShowAnimated(true);
        else
            HideAnimated();
    }

    public void ShowAnimated(bool forceNormalMaterial = true)
    {
        gameObject.SetActive(true);
        isVisible = true;

        if (forceNormalMaterial)
            SetMaterialImmediate(normalMaterial, 0f);

        StartFade(1f, showFadeDuration);
    }

    public void HideAnimated()
    {
        if (!gameObject.activeSelf)
            return;

        isVisible = false;

        StopInvalidRoutine();

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(HideRoutine());
    }

    public void SetValidState(bool isValid)
    {
        if (isValid)
        {
            SwitchToNormalAnimated();
        }
        else
        {
            SwitchToInvalidAnimated();
        }
    }

    public void SwitchToInvalidAnimated()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        StopInvalidRoutine();

        invalidRoutine = StartCoroutine(InvalidStateRoutine());
    }

    public void SwitchToNormalAnimated()
    {
        StopInvalidRoutine();

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        StartCoroutine(SwitchMaterialRoutine(normalMaterial));
    }

    public void SetRadius(float radius)
    {
        currentRadius = Mathf.Max(0.05f, radius);
        UpdateScale();
    }

    private IEnumerator InvalidStateRoutine()
    {
        yield return SwitchMaterialRoutine(invalidMaterial);

        if (autoReturnToNormal)
        {
            yield return new WaitForSecondsRealtime(invalidVisibleDuration);
            yield return SwitchMaterialRoutine(normalMaterial);
        }

        invalidRoutine = null;
    }

    private IEnumerator SwitchMaterialRoutine(Material nextMaterial)
    {
        if (nextMaterial == null)
            yield break;

        if (currentSourceMaterial == nextMaterial)
        {
            StartFade(1f, materialFadeDuration);
            yield break;
        }

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        yield return FadeAlphaRoutine(GetCurrentAlpha(), 0f, materialFadeDuration);

        SetMaterialImmediate(nextMaterial, 0f);

        yield return FadeAlphaRoutine(0f, 1f, materialFadeDuration);
    }

    private IEnumerator HideRoutine()
    {
        yield return FadeAlphaRoutine(GetCurrentAlpha(), 0f, hideFadeDuration);
        gameObject.SetActive(false);
        fadeRoutine = null;
    }

    private void StartFade(float targetAlpha, float duration)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeAlphaRoutine(GetCurrentAlpha(), targetAlpha, duration));
    }

    private IEnumerator FadeAlphaRoutine(float from, float to, float duration)
    {
        if (runtimeMaterial == null)
            yield break;

        if (duration <= 0f)
        {
            SetAlphaImmediate(to);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(from, to, t);
            SetAlphaImmediate(alpha);
            yield return null;
        }

        SetAlphaImmediate(to);
    }

    private void SetMaterialImmediate(Material sourceMaterial, float alpha)
    {
        if (sourceMaterial == null || indicatorRenderer == null)
            return;

        currentSourceMaterial = sourceMaterial;

        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }

        runtimeMaterial = new Material(sourceMaterial);
        indicatorRenderer.material = runtimeMaterial;

        SetAlphaImmediate(alpha);
    }

    private void PrepareRuntimeMaterial(Material sourceMaterial)
    {
        if (sourceMaterial == null || indicatorRenderer == null)
            return;

        currentSourceMaterial = sourceMaterial;
        runtimeMaterial = new Material(sourceMaterial);
        indicatorRenderer.material = runtimeMaterial;
    }

    private void SetAlphaImmediate(float alpha)
    {
        if (runtimeMaterial == null)
            return;

        Color color = runtimeMaterial.color;
        color.a = Mathf.Clamp01(alpha);
        runtimeMaterial.color = color;
    }

    private float GetCurrentAlpha()
    {
        if (runtimeMaterial == null)
            return 0f;

        return runtimeMaterial.color.a;
    }

    private void StopInvalidRoutine()
    {
        if (invalidRoutine != null)
        {
            StopCoroutine(invalidRoutine);
            invalidRoutine = null;
        }
    }

    private void LateUpdate()
    {
        if (followTarget && followTransform != null)
        {
            UpdateTransform();
        }
    }

    private void UpdateTransform()
    {
        if (followTransform == null)
            return;

        Vector3 p = followTransform.position;
        transform.position = new Vector3(p.x, p.y + yOffset, p.z);

        transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        UpdateScale();
    }

    private void UpdateScale()
    {
        if (visualRoot == null)
            return;

        float diameter = currentRadius * 2f;
        visualRoot.localScale = new Vector3(diameter, diameter, 1f);
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }
}