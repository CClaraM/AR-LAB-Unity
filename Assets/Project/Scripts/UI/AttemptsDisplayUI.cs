using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AttemptsDisplayUI : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text attemptsText;

    [Header("Dragon Slots")]
    [SerializeField] private Image[] dragonSlots;
    [SerializeField] private Sprite emptyDragonSprite;

    [Header("Extra Attempts Indicator")]
    [SerializeField] private Image extraAttemptsImage;
    [SerializeField] private Sprite extraAttemptsSprite;
    [SerializeField] private bool hideExtraImageWhenNotNeeded = true;

    [Header("Format")]
    [SerializeField] private string attemptsFormat = "Intentos: {0}/{1}";

    private Sprite[] fullDragonSprites;

    private void Awake()
    {
        CacheOriginalSprites();
    }

    private void CacheOriginalSprites()
    {
        if (dragonSlots == null)
            return;

        fullDragonSprites = new Sprite[dragonSlots.Length];

        for (int i = 0; i < dragonSlots.Length; i++)
        {
            if (dragonSlots[i] != null)
            {
                fullDragonSprites[i] = dragonSlots[i].sprite;
            }
        }
    }

    public void UpdateAttempts(int remainingAttempts, int maxAttempts)
    {
        remainingAttempts = Mathf.Max(0, remainingAttempts);
        maxAttempts = Mathf.Max(0, maxAttempts);

        if (attemptsText != null)
        {
            attemptsText.text = string.Format(
                attemptsFormat,
                remainingAttempts,
                maxAttempts
            );
        }

        UpdateDragonSlots(remainingAttempts, maxAttempts);
        UpdateExtraAttemptsImage(remainingAttempts);
    }

    private void UpdateDragonSlots(int remainingAttempts, int maxAttempts)
    {
        if (dragonSlots == null)
            return;

        int visibleSlots = Mathf.Min(maxAttempts, 3);

        for (int i = 0; i < dragonSlots.Length; i++)
        {
            Image slot = dragonSlots[i];

            if (slot == null)
                continue;

            bool shouldShowSlot = i < visibleSlots;
            slot.gameObject.SetActive(shouldShowSlot);

            if (!shouldShowSlot)
                continue;

            bool hasAttempt = i < Mathf.Min(remainingAttempts, 3);

            if (hasAttempt)
            {
                if (fullDragonSprites != null &&
                    i < fullDragonSprites.Length &&
                    fullDragonSprites[i] != null)
                {
                    slot.sprite = fullDragonSprites[i];
                }
            }
            else
            {
                slot.sprite = emptyDragonSprite;
            }
        }
    }

    private void UpdateExtraAttemptsImage(int maxAttempts)
    {
        if (extraAttemptsImage == null)
            return;

        bool shouldShowExtra = maxAttempts > 3;

        if (hideExtraImageWhenNotNeeded)
        {
            extraAttemptsImage.gameObject.SetActive(shouldShowExtra);
        }
        else
        {
            extraAttemptsImage.gameObject.SetActive(true);
        }

        if (shouldShowExtra)
        {
            if (extraAttemptsSprite != null)
                extraAttemptsImage.sprite = extraAttemptsSprite;
        }
        else
        {
            if (emptyDragonSprite != null)
                extraAttemptsImage.sprite = emptyDragonSprite;
        }
    }
}