using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InstructionPanelController : MonoBehaviour
{
    public enum TemporaryRestoreMode
    {
        RestoreFullStep,
        RestoreVisualOnly,
        HideAfterTemporary
    }

    [Header("References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Message")]
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private RectTransform instructionTextRect;
    [SerializeField] private RectTransform messageRect;
    [SerializeField] private Image messageBackgroundImage;
    [SerializeField] private RectTransform messageBackgroundRect;

    [Header("Dragon")]
    [SerializeField] private Image dragonImage;
    [SerializeField] private RectTransform dragonRect;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("Mute Button")]
    [SerializeField] private Button muteButton;
    [SerializeField] private RectTransform muteRect;

    [Header("Instruction Controls")]
    [SerializeField] private bool audioMuted = false;
    [SerializeField] private bool allowSkip = true;

    [Header("Instruction Buttons")]
    [SerializeField] private GameObject skipButtonRoot;
    [SerializeField] private UnityEngine.UI.Button skipButton;

    [Header("Instruction Audio Volume")]
    [SerializeField] private float normalInstructionVolume = 1f;
    [SerializeField] private float mutedInstructionVolume = 0f;
    [SerializeField] private float muteFadeDuration = 0.35f;

    [Header("Default Fade")]
    [SerializeField] private float fadeInDuration = 0.35f;
    [SerializeField] private float fadeOutDuration = 0.35f;

    private InstructionStep currentPersistentStep;
    private Coroutine routine;

    private bool skipRequested;
    private Coroutine audioVolumeRoutine;
    public bool IsAudioMuted => audioMuted;

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        if (canvasGroup == null)
            canvasGroup = panelRoot.GetComponent<CanvasGroup>();

        if (messageRect == null && instructionText != null)
            messageRect = instructionText.rectTransform.parent as RectTransform;

        if (instructionTextRect == null && instructionText != null)
            instructionTextRect = instructionText.rectTransform;

        if (messageBackgroundRect == null && messageBackgroundImage != null)
            messageBackgroundRect = messageBackgroundImage.rectTransform;

        if (dragonRect == null && dragonImage != null)
            dragonRect = dragonImage.rectTransform;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (LabAudioController.Instance != null && audioSource != null)
        {
            LabAudioController.Instance.RegisterInstructionAudioSource(audioSource);
        }
    }

    public void ShowStep(InstructionStep step, Action onFinished = null)
    {
        if (step == null)
        {
            Debug.LogWarning("InstructionStep es null.");
            return;
        }

        currentPersistentStep = step;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(ShowStepWithCallbackRoutine(step, onFinished));
    }

    public void ShowTemporaryStep(
    InstructionStep temporaryStep,
    TemporaryRestoreMode restoreMode = TemporaryRestoreMode.RestoreVisualOnly,
    Action onFinished = null
)
    {
        if (temporaryStep == null)
        {
            Debug.LogWarning("Temporary InstructionStep es null.");
            return;
        }

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(
            ShowTemporaryStepRoutine(temporaryStep, restoreMode, onFinished)
        );
    }

    public void Hide()
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(HideRoutine());
    }

    public float GetTotalStepDuration(InstructionStep step)
    {
        if (step == null)
            return 0f;

        float textDuration = step.HasTextBlocks()
            ? step.GetTextBlocksDuration()
            : step.visibleDuration;

        float total = step.showDelay + fadeInDuration + textDuration;

        if (step.autoHide)
            total += fadeOutDuration;

        return total;
    }

    private IEnumerator ShowStepRoutine(InstructionStep step)
    {
        skipRequested = false;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        SetInstructionInteraction(true);

        ApplyDragon(step);
        ApplyMessageLayout(step);
        ApplyMessageBackground(step);
        ApplyTextLayout(step);
        PlayAudio(step);

        if (instructionText != null)
        {
            if (step.HasTextBlocks())
                instructionText.text = step.textBlocks[0].text;
            else
                instructionText.text = step.message;
        }

        if (canvasGroup == null)
            yield break;

        yield return new WaitForSecondsRealtime(step.showDelay);

        yield return FadeTo(1f, fadeInDuration);

        if (step.HasTextBlocks())
        {
            yield return ShowTextBlocksRoutine(step);
        }
        else
        {
            yield return WaitStepRealtime(step.visibleDuration);
        }

        if (step.autoHide || skipRequested)
        {
            yield return FadeTo(0f, fadeOutDuration);
            SetInstructionInteraction(false);
        }
    }

    private IEnumerator ShowStepWithCallbackRoutine(InstructionStep step, Action onFinished)
    {
        yield return ShowStepRoutine(step);

        routine = null;
        onFinished?.Invoke();
    }

    private IEnumerator ShowTextBlocksRoutine(InstructionStep step)
    {
        skipRequested = false;

        if (step == null || step.textBlocks == null)
            yield break;

        for (int i = 0; i < step.textBlocks.Length; i++)
        {
            InstructionTextBlock block = step.textBlocks[i];

            if (block == null)
                continue;

            if (instructionText != null)
                instructionText.text = block.text;

            yield return WaitStepRealtime(Mathf.Max(0.1f, block.duration));

            if (skipRequested)
                yield break;
        }
    }

    private IEnumerator WaitStepRealtime(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (skipRequested)
                yield break;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator ShowTemporaryStepRoutine(
    InstructionStep temporaryStep,
    TemporaryRestoreMode restoreMode,
    Action onFinished
)
    {
        yield return ShowStepRoutine(temporaryStep);

        switch (restoreMode)
        {
            case TemporaryRestoreMode.RestoreFullStep:
                if (currentPersistentStep != null)
                {
                    yield return ShowStepRoutine(currentPersistentStep);
                }
                break;

            case TemporaryRestoreMode.RestoreVisualOnly:
                if (currentPersistentStep != null)
                {
                    ApplyStepVisualOnly(currentPersistentStep);
                }
                break;

            case TemporaryRestoreMode.HideAfterTemporary:
                yield return HideRoutine();
                break;
        }

        routine = null;
        onFinished?.Invoke();
    }

    private void ApplyStepVisualOnly(InstructionStep step)
    {
        if (step == null)
            return;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        ApplyDragon(step);
        ApplyMessageLayout(step);
        ApplyMessageBackground(step);
        ApplyTextLayout(step);

        if (instructionText != null)
        {
            if (step.HasTextBlocks() && step.textBlocks[0] != null)
                instructionText.text = step.textBlocks[0].text;
            else
                instructionText.text = step.message;
        }

        if (audioSource != null)
            audioSource.Stop();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void ApplyDragon(InstructionStep step)
    {
        if (dragonImage != null)
        {
            dragonImage.sprite = step.dragonSprite;
            dragonImage.enabled = step.dragonSprite != null;
        }

        if (dragonRect != null)
        {
            dragonRect.anchoredPosition = step.dragonAnchoredPosition;
            dragonRect.sizeDelta = step.dragonSize;
            dragonRect.localRotation = Quaternion.Euler(0f, 0f, step.dragonRotationZ);

            dragonRect.localScale = GetMirrorScale(
                step.dragonMirrorHorizontal,
                step.dragonMirrorVertical
            );
        }
    }

    private void ApplyMessageLayout(InstructionStep step)
    {
        if (messageRect == null)
            return;

        messageRect.anchoredPosition = step.messageAnchoredPosition;
        messageRect.sizeDelta = step.messageSize;
        messageRect.localRotation = Quaternion.Euler(0f, 0f, step.messageRotationZ);

        // No hacemos mirror aquí para no invertir el texto.
        messageRect.localScale = Vector3.one;
    }

    private void ApplyTextLayout(InstructionStep step)
    {
        if (instructionTextRect != null)
        {
            instructionTextRect.anchoredPosition = step.textAnchoredPosition;
            instructionTextRect.sizeDelta = step.textSize;
            instructionTextRect.localRotation = Quaternion.identity;
            instructionTextRect.localScale = Vector3.one;
        }

        if (instructionText != null)
        {
            instructionText.fontSize = step.textFontSize;
        }
    }

    private void ApplyMessageBackground(InstructionStep step)
    {
        if (messageBackgroundImage != null)
        {
            if (step.messageBackgroundSprite != null)
                messageBackgroundImage.sprite = step.messageBackgroundSprite;

            messageBackgroundImage.enabled = messageBackgroundImage.sprite != null;
        }

        if (messageBackgroundRect != null)
        {
            messageBackgroundRect.anchoredPosition = step.backgroundAnchoredPosition;
            messageBackgroundRect.sizeDelta = step.backgroundSize;

            messageBackgroundRect.localScale = GetMirrorScale(
                step.messageMirrorHorizontal,
                step.messageMirrorVertical
            );
        }
    }

    private Vector3 GetMirrorScale(bool mirrorHorizontal, bool mirrorVertical)
    {
        float x = mirrorHorizontal ? -1f : 1f;
        float y = mirrorVertical ? -1f : 1f;

        return new Vector3(x, y, 1f);
    }

    private void PlayAudio(InstructionStep step)
    {
        if (audioSource == null || step == null || step.audioClip == null)
            return;

        audioSource.Stop();
        audioSource.clip = step.audioClip;
        audioSource.mute = false;

        audioSource.volume = LabAudioController.Instance != null
            ? LabAudioController.Instance.GetInstructionTargetVolume()
            : 1f;

        audioSource.Play();
    }

    public void ToggleMute()
    {
        SetMuted(!audioMuted);
    }

    public void SetMuted(bool muted)
    {
        audioMuted = muted;

        float targetVolume = audioMuted
            ? mutedInstructionVolume
            : normalInstructionVolume;

        FadeInstructionAudioVolume(targetVolume, muteFadeDuration);
    }

    private void FadeInstructionAudioVolume(float targetVolume, float duration)
    {
        if (audioSource == null)
            return;

        if (audioVolumeRoutine != null)
            StopCoroutine(audioVolumeRoutine);

        audioVolumeRoutine = StartCoroutine(
            FadeInstructionAudioVolumeRoutine(targetVolume, duration)
        );
    }

    private IEnumerator FadeInstructionAudioVolumeRoutine(float targetVolume, float duration)
    {
        if (audioSource == null)
            yield break;

        float startVolume = audioSource.volume;

        if (duration <= 0f)
        {
            audioSource.volume = targetVolume;
            audioVolumeRoutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, t);

            yield return null;
        }

        audioSource.volume = targetVolume;
        audioVolumeRoutine = null;
    }

    public void SkipCurrentInstruction()
    {
        if (!allowSkip)
            return;

        skipRequested = true;

        if (audioVolumeRoutine != null)
        {
            StopCoroutine(audioVolumeRoutine);
            audioVolumeRoutine = null;
        }

        if (audioSource != null)
            audioSource.Stop();
    }

    private IEnumerator HideRoutine()
    {
        skipRequested = true;

        if (audioSource != null)
            audioSource.Stop();

        SetInstructionInteraction(false);

        if (canvasGroup != null)
            yield return FadeTo(0f, fadeOutDuration);
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (canvasGroup == null)
            yield break;

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        if (duration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    private void SetInstructionInteraction(bool enabled)
    {
        if (canvasGroup != null)
        {
            canvasGroup.interactable = enabled;
            canvasGroup.blocksRaycasts = enabled;
        }

        if (skipButtonRoot != null)
            skipButtonRoot.SetActive(enabled);

        if (skipButton != null)
            skipButton.interactable = enabled && allowSkip;
    }
}