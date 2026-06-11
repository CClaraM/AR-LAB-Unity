using System.Collections;
using UnityEngine;

public class LabUIFadeController : MonoBehaviour
{
    [Header("Groups to Fade")]
    [SerializeField] private CanvasGroup[] groups;

    [Header("Settings")]
    [SerializeField] private float fadeOutDuration = 0.45f;
    [SerializeField] private bool deactivateAfterFade = true;

    private Coroutine routine;

    public void HideGameplayUI()
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(HideRoutine());
    }

    private IEnumerator HideRoutine()
    {
        foreach (CanvasGroup group in groups)
        {
            if (group == null)
                continue;

            group.interactable = false;
            group.blocksRaycasts = false;
        }

        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            float alpha = Mathf.Lerp(1f, 0f, t);

            foreach (CanvasGroup group in groups)
            {
                if (group != null)
                    group.alpha = alpha;
            }

            yield return null;
        }

        foreach (CanvasGroup group in groups)
        {
            if (group == null)
                continue;

            group.alpha = 0f;

            if (deactivateAfterFade)
                group.gameObject.SetActive(false);
        }

        routine = null;
    }
}