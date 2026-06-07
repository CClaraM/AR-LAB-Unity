using System.Collections;
using TMPro;
using UnityEngine;

public class ARPhysicsLabController : MonoBehaviour
{
    private enum LabState
    {
        Intro,
        WaitingForCannonPlacement,
        WaitingForTargetPlacement,
        ReadyToLaunch,
        ProjectileInFlight,
        AttemptResult,
        Completed
    }

    [Header("Attempts")]
    [SerializeField] private int defaultMaxAttempts = 3;
    [SerializeField] private AttemptsDisplayUI attemptsDisplayUI;

    private int maxAttempts;
    private int remainingAttempts;
    private int usedAttempts;
    private bool labLocked;

    [Header("Startup Loading")]
    [SerializeField] private GameObject startupPanel;
    [SerializeField] private CanvasGroup startupCanvasGroup;
    [SerializeField] private float startupVisibleDuration = 1.5f;
    [SerializeField] private float startupFadeOutDuration = 0.5f;

    [Header("AR Placement")]
    [SerializeField] private ARPlacementManager placementManager;
    [SerializeField] private bool allowPlacementDuringIntro = false;


    [Header("Instruction System")]
    [SerializeField] private InstructionPanelController instructionPanelController;

    [SerializeField] private InstructionStep introStep;
    [SerializeField] private InstructionStep placeCannonStep;
    [SerializeField] private InstructionStep placeTargetStep;
    [SerializeField] private InstructionStep readyStep;
    [SerializeField] private InstructionStep noAttemptsStep;

    [Header("Exercise UI")]
    [SerializeField] private TMP_Text distanceText;
    [SerializeField] private TMP_Text heightText;
    [SerializeField] private TMP_Text horizontalDistanceText;

    [Header("Projectile Safety")]
    [SerializeField] private ProjectileKillZone projectileKillZone;

    private CannonLauncher cannonLauncher;
    private AppleTarget appleTarget;

    private float horizontalDistance;
    private float heightDifference;
    private float straightDistance;

    private LabState currentState;
    private Coroutine introRoutine;

    public float HorizontalDistance => horizontalDistance;
    public float HeightDifference => heightDifference;
    public float StraightDistance => straightDistance;

    public int MaxAttempts => maxAttempts;
    public int RemainingAttempts => remainingAttempts;
    public int UsedAttempts => usedAttempts;

    private void Start()
    {
        ApplyExerciseData(AndroidBridge.Instance != null
            ? AndroidBridge.Instance.CurrentExerciseData
            : null);

        if (placementManager != null)
            placementManager.SetPlacementEnabled(false);

        if (startupPanel != null)
            startupPanel.SetActive(true);

        if (startupCanvasGroup != null)
            startupCanvasGroup.alpha = 1f;

        StartCoroutine(StartupRoutine());
    }

    private IEnumerator StartupRoutine()
    {
        yield return new WaitForSecondsRealtime(startupVisibleDuration);

        if (startupCanvasGroup != null)
        {
            float elapsed = 0f;
            float startAlpha = startupCanvasGroup.alpha;

            while (elapsed < startupFadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / startupFadeOutDuration);
                startupCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }

            startupCanvasGroup.alpha = 0f;
        }

        if (startupPanel != null)
            startupPanel.SetActive(false);

        StartIntroFlow();
    }

    private void StartIntroFlow()
    {
        CancelNarrativeRoutines();

        if (placementManager != null)
            placementManager.SetPlacementEnabled(allowPlacementDuringIntro);

        introRoutine = StartCoroutine(IntroFlowRoutine());
    }

    private IEnumerator IntroFlowRoutine()
    {
        currentState = LabState.Intro;

        if (instructionPanelController != null && introStep != null)
        {
            instructionPanelController.ShowStep(introStep);
            yield return new WaitForSecondsRealtime(
                instructionPanelController.GetTotalStepDuration(introStep)
            );
        }

        introRoutine = null;

        SetState(LabState.WaitingForCannonPlacement);
    }

    private void SetState(LabState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            case LabState.WaitingForCannonPlacement:
                if (placementManager != null)
                    placementManager.SetPlacementEnabled(true);

                ShowPlaceCannonInstruction();
                break;

            case LabState.WaitingForTargetPlacement:
                if (placementManager != null)
                    placementManager.SetPlacementEnabled(true);

                ShowPlaceTargetInstruction();
                break;

            case LabState.ReadyToLaunch:
                if (placementManager != null)
                    placementManager.SetPlacementEnabled(false);

                ShowReadyInstruction();
                break;

            case LabState.ProjectileInFlight:
                if (placementManager != null)
                    placementManager.SetPlacementEnabled(false);
                break;

            case LabState.AttemptResult:
                break;

            case LabState.Completed:
                if (placementManager != null)
                    placementManager.SetPlacementEnabled(false);
                break;
        }
    }

    private void CancelNarrativeRoutines()
    {
        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
            introRoutine = null;
        }
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

        if (currentState != LabState.ReadyToLaunch)
        {
            Debug.LogWarning("El laboratorio todavía no está listo para disparar.");
            return false;
        }

        if (remainingAttempts <= 0)
        {
            Debug.LogWarning("No quedan intentos.");
            labLocked = true;
            UpdateAttemptsUI();
            ShowNoAttemptsInstruction();
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

        currentState = LabState.ProjectileInFlight;

        if (remainingAttempts <= 0)
        {
            labLocked = true;
        }

        return true;
    }

    public bool CanFire()
    {
        return !labLocked &&
               remainingAttempts > 0 &&
               currentState == LabState.ReadyToLaunch;
    }

    private void UpdateAttemptsUI()
    {
        if (attemptsDisplayUI != null)
        {
            attemptsDisplayUI.UpdateAttempts(remainingAttempts, maxAttempts);
        }
    }

    private void ShowPlaceCannonInstruction()
    {
        if (distanceText != null)
            distanceText.text = "";

        if(heightText != null)
            heightText.text = "";

        if(horizontalDistanceText != null)
            horizontalDistanceText.text = "";


        if (instructionPanelController != null && placeCannonStep != null)
            instructionPanelController.ShowStep(placeCannonStep);
    }

    private void ShowPlaceTargetInstruction()
    {
        if (distanceText != null)
            distanceText.text = "";

        if (heightText != null)
            heightText.text = "";

        if (horizontalDistanceText != null)
            horizontalDistanceText.text = "";

        if (instructionPanelController != null && placeTargetStep != null)
            instructionPanelController.ShowStep(placeTargetStep);
    }

    private void ShowReadyInstruction()
    {
        if (instructionPanelController != null && readyStep != null)
            instructionPanelController.ShowStep(readyStep);
    }

    private void ShowNoAttemptsInstruction()
    {
        if (instructionPanelController != null && noAttemptsStep != null)
            instructionPanelController.ShowStep(noAttemptsStep);
    }

    public void NotifyCannonPlaced(GameObject cannonObject)
    {
        if (cannonObject == null)
            return;

        CancelNarrativeRoutines();

        cannonLauncher = cannonObject.GetComponentInChildren<CannonLauncher>();

        SetState(LabState.WaitingForTargetPlacement);
    }

    public void NotifyTargetPlaced(GameObject targetObject)
    {
        if (targetObject == null)
            return;

        CancelNarrativeRoutines();

        appleTarget = targetObject.GetComponentInChildren<AppleTarget>();

        CalculateDistances();
        UpdateDistanceUI();
        ConfigureKillZone();

        SetState(LabState.ReadyToLaunch);
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
        if (distanceText == null) return;

        if (heightText == null) return;

        if (horizontalDistanceText == null) return;

        heightText.text = $"Altura: {heightDifference:0.00} m";

        horizontalDistanceText.text = $"Horizontal: {horizontalDistance:0.00} m";

        distanceText.text = $"Recta: {straightDistance:0.00} m";
    }
}