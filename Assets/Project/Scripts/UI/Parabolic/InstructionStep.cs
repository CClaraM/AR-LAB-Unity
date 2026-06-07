using UnityEngine;

[System.Serializable]
public class InstructionStep
{
    [Header("Content")]
    [TextArea(2, 5)]
    public string message;

    public InstructionTextBlock[] textBlocks;

    public Sprite dragonSprite;
    public AudioClip audioClip;

    [Header("Dragon Layout")]
    public Vector2 dragonAnchoredPosition = new Vector2(260f, -40f);
    public Vector2 dragonSize = new Vector2(220f, 220f);
    public float dragonRotationZ = 0f;

    [Header("Dragon Mirror")]
    public bool dragonMirrorHorizontal;
    public bool dragonMirrorVertical;

    [Header("Message Layout")]
    public Vector2 messageAnchoredPosition = new Vector2(-170f, 0f);
    public Vector2 messageSize = new Vector2(520f, 180f);
    public float messageRotationZ = 0f;

    [Header("Message Background")]
    public Sprite messageBackgroundSprite;
    public bool messageMirrorHorizontal;
    public bool messageMirrorVertical;

    [Header("Background Layout")]
    public Vector2 backgroundAnchoredPosition = Vector2.zero;
    public Vector2 backgroundSize = new Vector2(520f, 180f);

    [Header("Text Layout")]
    public Vector2 textAnchoredPosition = Vector2.zero;
    public Vector2 textSize = new Vector2(460f, 130f);
    public float textFontSize = 32f;

    [Header("Timing")]
    public float showDelay = 0.25f;
    public float visibleDuration = 3f;
    public bool autoHide = true;

    public bool HasTextBlocks()
    {
        return textBlocks != null && textBlocks.Length > 0;
    }

    public float GetTextBlocksDuration()
    {
        if (!HasTextBlocks())
            return visibleDuration;

        float total = 0f;

        foreach (InstructionTextBlock block in textBlocks)
        {
            if (block != null)
                total += Mathf.Max(0.1f, block.duration);
        }

        return total;
    }
}