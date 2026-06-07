using UnityEngine;

[System.Serializable]
public class InstructionTextBlock
{
    [TextArea(1, 4)]
    public string text;

    [Min(0.1f)]
    public float duration = 2f;
}