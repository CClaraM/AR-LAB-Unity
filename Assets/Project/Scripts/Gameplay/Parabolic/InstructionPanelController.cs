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

    [Header("Default Fade")]
    [SerializeField] private float fadeInDuration = 0.35f;
    [SerializeField] private float fadeOutDuration = 0.35f;

    private InstructionStep currentPersistentStep;
    private Coroutine routine;

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
    }

    public void ShowStep(InstructionStep step)
    {
        if (step == null)
        {
            Debug.LogWarning("InstructionStep es null.");
            return;
        }

        currentPersistentStep = step; // Conservar es estado

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(ShowStepRoutine(step));
    }

    public void ShowTemporaryStep(
    InstructionStep temporaryStep,
    TemporaryRestoreMode restoreMode = TemporaryRestoreMode.RestoreVisualOnly
)
    {
        if (temporaryStep == null)
        {
            Debug.LogWarning("Temporary InstructionStep es null.");
            return;
        }

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(ShowTemporaryStepRoutine(temporaryStep, restoreMode));
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
        if (panelRoot != null)
            panelRoot.SetActive(true);

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

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        yield return new WaitForSecondsRealtime(step.showDelay);

        yield return FadeTo(1f, fadeInDuration);

        if (step.HasTextBlocks())
        {
            yield return ShowTextBlocksRoutine(step);
        }
        else
        {
            yield return new WaitForSecondsRealtime(step.visibleDuration);
        }

        if (step.autoHide)
        {
            yield return FadeTo(0f, fadeOutDuration);
        }
    }

    private IEnumerator ShowTextBlocksRoutine(InstructionStep step)
    {
        for (int i = 0; i < step.textBlocks.Length; i++)
        {
            InstructionTextBlock block = step.textBlocks[i];

            if (block == null)
                continue;

            if (instructionText != null)
                instructionText.text = block.text;

            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, block.duration));
        }
    }

    private IEnumerator ShowTemporaryStepRoutine(
    InstructionStep temporaryStep,
    TemporaryRestoreMode restoreMode
)
    {
        yield return ShowStepRoutine(temporaryStep);

        switch (restoreMode)
        {
            case TemporaryRestoreMode.RestoreFullStep:
                if (currentPersistentStep != null)
                {
                    routine = StartCoroutine(ShowStepRoutine(currentPersistentStep));
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
        if (audioSource == null)
            return;

        audioSource.Stop();

        if (step.audioClip != null)
        {
            audioSource.clip = step.audioClip;
            audioSource.Play();
        }
    }

    private IEnumerator HideRoutine()
    {
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
}