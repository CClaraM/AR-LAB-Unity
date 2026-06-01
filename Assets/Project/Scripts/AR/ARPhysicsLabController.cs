using System.Collections;
using TMPro;
using UnityEngine;

public class ARPhysicsLabController : MonoBehaviour
{
    [Header("Attempts")]
    [SerializeField] private int defaultMaxAttempts = 3;
    [SerializeField] private AttemptsDisplayUI attemptsDisplayUI;

    private int maxAttempts;
    private int remainingAttempts;
    private int usedAttempts;
    private bool labLocked;

    [Header("Instruction UI")]
    [SerializeField] private GameObject instructionPanel;
    [SerializeField] private CanvasGroup instructionCanvasGroup;
    [SerializeField] private TMP_Text instructionText;

    [Header("Instruction Transition")]
    [SerializeField] private float showDelay = 0.4f;
    [SerializeField] private float fadeInDuration = 0.35f;
    [SerializeField] private float fadeOutDuration = 0.35f;

    [Header("Exercise UI")]
    [SerializeField] private TMP_Text distanceText;

    [Header("Messages")]
    [SerializeField]
    private string waitingCannonMessage =
        "Busca una superficie y toca para colocar la isla del cañón.";

    [SerializeField]
    private string waitingTargetMessage =
        "Ahora toca otra superficie para colocar la isla objetivo.";

    [SerializeField]
    private string readyMessage =
        "Reto listo. Ajusta la velocidad y el ángulo para impactar el objetivo.";

    [Header("Projectile Safety")]
    [SerializeField] private ProjectileKillZone projectileKillZone;

    [SerializeField] private float readyMessageDuration = 3f;

    private CannonLauncher cannonLauncher;
    private AppleTarget appleTarget;

    private float horizontalDistance;
    private float heightDifference;
    private float straightDistance;

    private Coroutine instructionRoutine;

    public float HorizontalDistance => horizontalDistance;
    public float HeightDifference => heightDifference;
    public float StraightDistance => straightDistance;

    public int MaxAttempts => maxAttempts;
    public int RemainingAttempts => remainingAttempts;
    public int UsedAttempts => usedAttempts;

    private void Awake()
    {
        if (instructionCanvasGroup == null && instructionPanel != null)
        {
            instructionCanvasGroup = instructionPanel.GetComponent<CanvasGroup>();
        }

        if (instructionCanvasGroup != null)
        {
            instructionCanvasGroup.alpha = 0f;
            instructionCanvasGroup.interactable = false;
            instructionCanvasGroup.blocksRaycasts = false;
        }

        if (instructionPanel != null)
            instructionPanel.SetActive(true);
    }

    private void Start()
    {
        ApplyExerciseData(AndroidBridge.Instance != null
            ? AndroidBridge.Instance.CurrentExerciseData
            : null);

        ShowWaitingForCannon();
    }

    public void ApplyExerciseData(ExerciseData data)
    {
        int configuredAttempts = defaultMaxAttempts;

        if (data != null && data.maxAttempts > 0)
        {
            configuredAttempts = data.maxAttempts;
        }

        maxAttempts = configuredAttempts;
        remainingAttempts = maxAttempts;
        usedAttempts = 0;
        labLocked = false;

        UpdateAttemptsUI();
    }

    public bool TryFire(CannonLauncher launcher)
    {
        if (labLocked)
        {
            Debug.LogWarning("Laboratorio bloqueado. No se puede disparar.");
            return false;
        }

        if (remainingAttempts <= 0)
        {
            Debug.LogWarning("No quedan intentos.");
            labLocked = true;
            UpdateAttemptsUI();
            ShowLaunchInfo("No quedan intentos. Laboratorio finalizado.");
            return false;
        }

        if (launcher == null)
        {
            Debug.LogWarning("No hay CannonLauncher para disparar.");
            return false;
        }

        bool fired = launcher.Fire();

        if (!fired)
            return false;

        usedAttempts++;
        remainingAttempts--;

        UpdateAttemptsUI();

        if (remainingAttempts <= 0)
        {
            labLocked = true;
            ShowLaunchInfo("No quedan intentos. Laboratorio finalizado.");
        }

        return true;
    }

    public bool CanFire()
    {
        return !labLocked && remainingAttempts > 0;
    }

    private void UpdateAttemptsUI()
    {
        if (attemptsDisplayUI != null)
        {
            attemptsDisplayUI.UpdateAttempts(remainingAttempts, maxAttempts);
        }
    }

    public void ShowWaitingForCannon()
    {
        ShowInstruction(waitingCannonMessage, false);

        if (distanceText != null)
            distanceText.text = "";
    }

    public void NotifyCannonPlaced(GameObject cannonObject)
    {
        if (cannonObject == null)
            return;

        cannonLauncher = cannonObject.GetComponentInChildren<CannonLauncher>();

        ShowInstruction(waitingTargetMessage, false);

        if (distanceText != null)
            distanceText.text = "";
    }

    private void ConfigureKillZone()
    {
        if (projectileKillZone == null)
            return;

        if (cannonLauncher == null || cannonLauncher.FirePoint == null)
            return;

        if (appleTarget == null)
            return;

        Vector3 launchPoint = cannonLauncher.FirePoint.position;
        Vector3 targetPoint = appleTarget.transform.position;

        projectileKillZone.ConfigureFromPoints(launchPoint, targetPoint);
    }

    public void NotifyTargetPlaced(GameObject targetObject)
    {
        if (targetObject == null)
            return;

        appleTarget = targetObject.GetComponentInChildren<AppleTarget>();

        CalculateDistances();
        UpdateDistanceUI();
        ConfigureKillZone();

        ShowInstruction(readyMessage, true);
    }

    public void ShowLaunchInfo(string message)
    {
        ShowInstruction(message, true);
    }

    private void ShowInstruction(string message, bool autoHide)
    {
        if (instructionRoutine != null)
            StopCoroutine(instructionRoutine);

        instructionRoutine = StartCoroutine(ShowInstructionRoutine(message, autoHide));
    }

    private IEnumerator ShowInstructionRoutine(string message, bool autoHide)
    {
        if (instructionPanel != null)
            instructionPanel.SetActive(true);

        if (instructionText != null)
            instructionText.text = message;

        if (instructionCanvasGroup == null)
            yield break;

        instructionCanvasGroup.interactable = false;
        instructionCanvasGroup.blocksRaycasts = false;

        yield return new WaitForSeconds(showDelay);

        yield return FadeInstruction(1f, fadeInDuration);

        if (autoHide)
        {
            yield return new WaitForSeconds(readyMessageDuration);
            yield return FadeInstruction(0f, fadeOutDuration);
        }
    }

    public void HideInstructionPanel()
    {
        if (instructionRoutine != null)
            StopCoroutine(instructionRoutine);

        instructionRoutine = StartCoroutine(HideInstructionRoutine());
    }

    private IEnumerator HideInstructionRoutine()
    {
        if (instructionCanvasGroup != null)
            yield return FadeInstruction(0f, fadeOutDuration);
    }

    private IEnumerator FadeInstruction(float targetAlpha, float duration)
    {
        if (instructionCanvasGroup == null)
            yield break;

        float startAlpha = instructionCanvasGroup.alpha;
        float elapsed = 0f;

        if (duration <= 0f)
        {
            instructionCanvasGroup.alpha = targetAlpha;
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            instructionCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            yield return null;
        }

        instructionCanvasGroup.alpha = targetAlpha;
    }

    public void CalculateDistances()
    {
        if (cannonLauncher == null || cannonLauncher.FirePoint == null)
        {
            Debug.LogWarning("No se puede calcular distancia: falta CannonLauncher o FirePoint.");
            return;
        }

        if (appleTarget == null)
        {
            Debug.LogWarning("No se puede calcular distancia: falta AppleTarget.");
            return;
        }

        Vector3 launchPoint = cannonLauncher.FirePoint.position;
        Vector3 targetPoint = appleTarget.transform.position;

        Vector3 flatLaunch = new Vector3(launchPoint.x, 0f, launchPoint.z);
        Vector3 flatTarget = new Vector3(targetPoint.x, 0f, targetPoint.z);

        horizontalDistance = Vector3.Distance(flatLaunch, flatTarget);
        heightDifference = targetPoint.y - launchPoint.y;
        straightDistance = Vector3.Distance(launchPoint, targetPoint);

        Debug.Log(
            $"Distancia horizontal: {horizontalDistance:0.00} m | " +
            $"Diferencia de altura: {heightDifference:0.00} m | " +
            $"Distancia recta: {straightDistance:0.00} m"
        );
    }

    private void UpdateDistanceUI()
    {
        if (distanceText == null)
            return;

        distanceText.text =
            $"Distancia horizontal: {horizontalDistance:0.00} m\n" +
            $"Altura objetivo: {heightDifference:0.00} m\n" +
            $"Distancia recta: {straightDistance:0.00} m";
    }
}