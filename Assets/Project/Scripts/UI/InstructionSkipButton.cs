using UnityEngine;
using UnityEngine.UI;

public class InstructionSkipButton : MonoBehaviour
{
    [SerializeField] private InstructionPanelController instructionPanelController;
    [SerializeField] private Button button;

    private void Reset()
    {
        button = GetComponent<Button>();
    }

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(Skip);
    }

    private void Skip()
    {
        if (instructionPanelController != null)
            instructionPanelController.SkipCurrentInstruction();
    }
}