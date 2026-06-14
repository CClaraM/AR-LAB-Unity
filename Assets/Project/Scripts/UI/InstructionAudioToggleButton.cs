using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class InstructionAudioToggleButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InstructionPanelController instructionPanelController;
    [SerializeField] private Button button;
    [SerializeField] private Image dragonFaceImage;
    [SerializeField] private RectTransform dragonFaceRect;

    [Header("Sprites")]
    [SerializeField] private Sprite unmutedSprite;
    [SerializeField] private Sprite mutedSprite;

    [Header("Muted Animation")]
    [SerializeField] private bool animateWhenMuted = true;
    [SerializeField] private float nodAngle = 30f;
    [SerializeField] private float nodStepDuration = 0.12f;
    [SerializeField] private float unmutedAnimationDelay = 2.5f;

    [Header("Unmuted Animation")]
    [SerializeField] private bool animateWhenUnmuted = true;
    [SerializeField] private float shakeAmount = 5f;
    [SerializeField] private float shakeDuration = 0.35f;
    [SerializeField] private float mutedAnimationDelay = 3f;

    [Header("Visual Behavior")]
    [SerializeField] private bool showActionIcon = true;

    private Coroutine animationRoutine;
    private Vector2 originalAnchoredPosition;

    private void Reset()
    {
        button = GetComponent<Button>();

        if (dragonFaceImage == null)
            dragonFaceImage = GetComponentInChildren<Image>();

        if (dragonFaceImage != null)
            dragonFaceRect = dragonFaceImage.rectTransform;
    }

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (dragonFaceImage == null)
            dragonFaceImage = GetComponentInChildren<Image>();

        if (dragonFaceImage != null && dragonFaceRect == null)
            dragonFaceRect = dragonFaceImage.rectTransform;

        if (dragonFaceRect != null)
            originalAnchoredPosition = dragonFaceRect.anchoredPosition;

        if (button != null)
            button.onClick.AddListener(ToggleMute);

        RefreshVisual();
    }

    private void OnEnable()
    {
        RefreshVisual();
        StartAnimationLoop();
    }

    private void OnDisable()
    {
        StopAnimationLoop();
    }

    private void ToggleMute()
    {
        if (LabAudioController.Instance == null)
        {
            Debug.LogWarning("InstructionAudioToggleButton: falta InstructionPanelController.");
            return;
        }

        LabAudioController.Instance.ToggleMuteAll();

        RefreshVisual();
        StartAnimationLoop();
    }

    private void RefreshVisual()
    {
        if (dragonFaceImage == null)
            return;

        bool muted = LabAudioController.Instance != null &&
                 LabAudioController.Instance.IsMuted;

        bool showMutedVisual = showActionIcon ? !muted : muted;

        Sprite targetSprite = showMutedVisual ? mutedSprite : unmutedSprite;

        if (targetSprite != null)
            dragonFaceImage.sprite = targetSprite;

        ResetFaceTransform();
    }

    private void StartAnimationLoop()
    {
        StopAnimationLoop();

        if (!gameObject.activeInHierarchy)
            return;

        animationRoutine = StartCoroutine(AnimationLoop());
    }

    private void StopAnimationLoop()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        ResetFaceTransform();
    }

    private IEnumerator AnimationLoop()
    {
        while (true)
        {
            bool muted = LabAudioController.Instance != null &&
             LabAudioController.Instance.IsMuted;

            if (!muted)
            {
                if (animateWhenMuted)
                    yield return ShakeRoutine();

                yield return new WaitForSecondsRealtime(mutedAnimationDelay);
            }
            else
            {
                if (animateWhenUnmuted)
                    yield return NodRoutine();

                yield return new WaitForSecondsRealtime(unmutedAnimationDelay);
            }
        }
    }

    private IEnumerator NodRoutine()
    {
        yield return RotateTo(nodAngle, nodStepDuration);
        yield return RotateTo(0f, nodStepDuration);
        yield return RotateTo(-nodAngle, nodStepDuration);
        yield return RotateTo(0f, nodStepDuration);
    }

    private IEnumerator ShakeRoutine()
    {
        if (dragonFaceRect == null)
            yield break;

        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float x = Random.Range(-shakeAmount, shakeAmount);
            float y = Random.Range(-shakeAmount, shakeAmount);
            float z = Random.Range(-shakeAmount, shakeAmount);

            dragonFaceRect.anchoredPosition = originalAnchoredPosition + new Vector2(x, y);
            dragonFaceRect.localRotation = Quaternion.Euler(0f, 0f, z);

            yield return null;
        }

        ResetFaceTransform();
    }

    private IEnumerator RotateTo(float targetZ, float duration)
    {
        if (dragonFaceRect == null)
            yield break;

        float startZ = dragonFaceRect.localEulerAngles.z;

        if (startZ > 180f)
            startZ -= 360f;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float z = Mathf.Lerp(startZ, targetZ, t);

            dragonFaceRect.localRotation = Quaternion.Euler(0f, 0f, z);

            yield return null;
        }

        dragonFaceRect.localRotation = Quaternion.Euler(0f, 0f, targetZ);
    }

    private void ResetFaceTransform()
    {
        if (dragonFaceRect == null)
            return;

        dragonFaceRect.anchoredPosition = originalAnchoredPosition;
        dragonFaceRect.localRotation = Quaternion.identity;
    }
}