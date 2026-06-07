using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_InputField))]
public class TMPInputFieldAutoFix : MonoBehaviour
{
    [Header("Padding")]
    [SerializeField] private float left = 12f;
    [SerializeField] private float right = 12f;
    [SerializeField] private float top = 6f;
    [SerializeField] private float bottom = 6f;

    private TMP_InputField inputField;

    private void Awake()
    {
        inputField = GetComponent<TMP_InputField>();
        ApplyFix();
    }

    private void OnEnable()
    {
        ApplyFix();
    }

    private void OnRectTransformDimensionsChange()
    {
        ApplyFix();
    }

    public void ApplyFix()
    {
        if (inputField == null)
            inputField = GetComponent<TMP_InputField>();

        if (inputField == null)
            return;

        if (inputField.textViewport != null)
        {
            StretchRect(inputField.textViewport, left, right, top, bottom);
        }

        if (inputField.textComponent != null)
        {
            RectTransform textRect = inputField.textComponent.rectTransform;
            StretchRect(textRect, 0f, 0f, 0f, 0f);
            inputField.textComponent.enableWordWrapping = false;
            inputField.textComponent.overflowMode = TextOverflowModes.Overflow;
        }

        if (inputField.placeholder != null)
        {
            RectTransform placeholderRect = inputField.placeholder.rectTransform;
            StretchRect(placeholderRect, 0f, 0f, 0f, 0f);
        }

        inputField.ForceLabelUpdate();
    }

    private void StretchRect(RectTransform rect, float leftOffset, float rightOffset, float topOffset, float bottomOffset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);

        rect.offsetMin = new Vector2(leftOffset, bottomOffset);
        rect.offsetMax = new Vector2(-rightOffset, -topOffset);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }
}