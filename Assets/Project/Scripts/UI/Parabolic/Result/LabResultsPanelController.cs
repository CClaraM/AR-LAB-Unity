using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LabResultsPanelController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text summaryText;

    [Header("Attempts List")]
    [SerializeField] private Transform attemptsContent;
    [SerializeField] private ResultAttemptRowUI attemptRowPrefab;

    [Header("Exit")]
    [SerializeField] private Button exitButton;

    [SerializeField] private ARPhysicsLabController labController;

    private string finalJson;

    private void Awake()
    {
        if (root != null)
            root.SetActive(false);

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitPressed);
    }

    public void ShowResults(LabFinalResult result, string json)
    {
        finalJson = json;

        if (root != null)
            root.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        ClearAttempts();

        if (result == null)
            return;

        if (titleText != null)
        {
            titleText.text = result.hitTarget
                ? "Laboratorio completado"
                : "Laboratorio finalizado";
        }

        if (summaryText != null)
        {
            summaryText.text =
                $"Resultados\n" +
                $"  Intentos usados: {result.usedAttempts}/{result.maxAttempts}\n" +
                $"  Distancia horizontal: {result.horizontalDistance:0.00} m\n" +
                $"  Diferencia de altura: {result.verticalDistance:0.00} m\n" +
                $"  Distancia recta inicial: {result.straightDistance:0.00} m";
        }

        if (result.attempts != null && attemptRowPrefab != null && attemptsContent != null)
        {
            foreach (LabAttemptResult attempt in result.attempts)
            {
                ResultAttemptRowUI row = Instantiate(attemptRowPrefab, attemptsContent);
                row.Setup(attempt);
            }

            Canvas.ForceUpdateCanvases();

            LayoutRebuilder.ForceRebuildLayoutImmediate(
                attemptsContent as RectTransform
            );

            Canvas.ForceUpdateCanvases();
        }
    }

    private void ClearAttempts()
    {
        if (attemptsContent == null)
            return;

        for (int i = attemptsContent.childCount - 1; i >= 0; i--)
        {
            Destroy(attemptsContent.GetChild(i).gameObject);
        }
    }

    public void OnExitPressed()
    {
        if (labController == null)
        {
            Debug.LogWarning("LabResultsPanelController: falta ARPhysicsLabController.");
            return;
        }

        labController.FinishAndReturnToAndroid();
    }

}