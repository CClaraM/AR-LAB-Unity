using System;
using System.Collections;
using TMPro;
using UnityEngine;
using static InstructionPanelController;

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
        Completed,
        Results
    }

    [Header("Attempts")]
    [SerializeField] private int defaultMaxAttempts = 3;
    [SerializeField] private AttemptsDisplayUI attemptsDisplayUI;

    [Header("Startup Loading")]
    [SerializeField] private GameObject startupPanel;
    [SerializeField] private CanvasGroup startupCanvasGroup;
    [SerializeField] private float startupVisibleDuration = 1.5f;
    [SerializeField] private float startupFadeOutDuration = 0.5f;

    [Header("AR Placement")]
    [SerializeField] private ARPlacementManager placementManager;
    [SerializeField] private bool allowPlacementDuringIntro = false;

    [Header("Audio")]
    [SerializeField] private ARJukebox arJukebox;

    [Header("Instruction System")]
    [SerializeField] private InstructionPanelController instructionPanelController;

    [SerializeField] private InstructionStep introStep;
    [SerializeField] private InstructionStep placeCannonStep;
    [SerializeField] private InstructionStep placeTargetStep;
    [SerializeField] private InstructionStep readyStep;
    [SerializeField] private InstructionStep noAttemptsStep;

    [Header("Temporary Instruction Steps")]
    [SerializeField] private InstructionStep targetTooCloseStep;
    [SerializeField] private InstructionStep missedTargetStep;
    [SerializeField] private InstructionStep outOfBoundsStep;
    [SerializeField] private InstructionStep hitTargetStep;
    [SerializeField] private InstructionStep resetIslandsStep;

    [Header("Exercise UI")]
    [SerializeField] private TMP_Text distanceText;
    [SerializeField] private TMP_Text heightText;
    [SerializeField] private TMP_Text horizontalDistanceText;

    [Header("UI Controllers")]
    [SerializeField] private CannonUIController cannonUIController;

    [Header("Projectile Safety")]
    [SerializeField] private ProjectileKillZone projectileKillZone;

    [Header("Impact Visualization")]
    [SerializeField] private ImpactMeasurementVisualizer impactMeasurementVisualizer;

    [Header("Trajectory Preview")]
    [SerializeField] private TrajectoryPreview trajectoryPreview;

    [Header("Projectile Camera")]
    [SerializeField] private ProjectileFlightCameraController projectileFlightCameraController;
    [SerializeField] private UnityEngine.UI.Toggle projectileCameraToggle;

    [Header("Runtime Attempt Data")]
    [SerializeField] private float currentShotPower;
    [SerializeField] private float currentShotAngle;
    [SerializeField] private bool hasCurrentShotData;

    [Header("Final Results")]
    [SerializeField] private LabUIFadeController labUIFadeController;
    [SerializeField] private ARLabSceneCleaner arLabSceneCleaner;
    [SerializeField] private LabResultsPanelController resultsPanelController;
    [SerializeField] private float delayBeforeFinalPanel = 0.25f;

    private readonly System.Collections.Generic.List<LabAttemptResult> attemptResults =
    new System.Collections.Generic.List<LabAttemptResult>();

    private Coroutine returnToReadyRoutine;

    private LabLocalProgress currentLocalProgress;
    private string currentRunId;


    private bool finalHitTarget;
    private bool suppressNextReadyInstruction;

    private int maxAttempts;
    private int remainingAttempts;
    private int usedAttempts;
    private bool labLocked;
    private bool finalSequenceStarted;

    private CannonLauncher cannonLauncher;
    private AppleTarget appleTarget;

    private float horizontalDistance;
    private float heightDifference;
    private float straightDistance;

    private LabState currentState;
    private Coroutine introRoutine;

    private bool currentProjectileResultRegistered;
    public float HorizontalDistance => horizontalDistance;
    public float HeightDifference => heightDifference;
    public float StraightDistance => straightDistance;

    public int MaxAttempts => maxAttempts;
    public int RemainingAttempts => remainingAttempts;
    public int UsedAttempts => usedAttempts;

    private void Start()
    {
        ApplyExerciseData(AndroidBridge.Instance != null
            ? AndroidBridge.Instance.CurrentLabInput
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
        if (arJukebox != null)
            arJukebox.Play();

        CancelNarrativeRoutines();

        if (placementManager != null)
            placementManager.SetPlacementEnabled(allowPlacementDuringIntro);

        introRoutine = StartCoroutine(IntroFlowRoutine());
    }

    private IEnumerator IntroFlowRoutine()
    {
        SetState(LabState.Intro);

        bool finished = false;

        if (instructionPanelController != null && introStep != null)
        {
            instructionPanelController.ShowStep(
                introStep,
                () => finished = true
            );

            yield return new WaitUntil(() => finished);

        }

        introRoutine = null;

        SetState(LabState.WaitingForCannonPlacement);
    }

    private void SetState(LabState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            case LabState.Intro:
                if (placementManager != null)
                    placementManager.SetPlacementEnabled(allowPlacementDuringIntro);
                break;

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

                if (!suppressNextReadyInstruction)
                {
                    ShowReadyInstruction();
                }

                suppressNextReadyInstruction = false;
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

            case LabState.Results:
                if (placementManager != null)
                    placementManager.SetPlacementEnabled(false);
                break;
        }
        UpdateFireButtonState();
    }

    // Metodos publicos que muestran mensajes temporales segun el resultado del intento de disparo. Estos pueden ser llamados desde otros scripts como CannonProjectile al detectar colisiones o condiciones de impacto.
    public void ShowTargetTooCloseInstruction()
    {
        if (instructionPanelController == null || targetTooCloseStep == null)
            return;

        instructionPanelController.ShowTemporaryStep(
            targetTooCloseStep,
            TemporaryRestoreMode.HideAfterTemporary //RestoreVisualOnly o RestoreFullStep
        );
    }

    public void ShowMissedTargetInstruction()
    {
        if (instructionPanelController == null || missedTargetStep == null)
            return;

        instructionPanelController.ShowTemporaryStep(
            missedTargetStep,
            TemporaryRestoreMode.HideAfterTemporary
        );
    }

    public void ShowOutOfBoundsInstruction()
    {
        if (instructionPanelController == null || outOfBoundsStep == null)
            return;

        instructionPanelController.ShowTemporaryStep(
            outOfBoundsStep,
            TemporaryRestoreMode.HideAfterTemporary
        );
    }

    public void ShowHitTargetInstruction()
    {
        if (instructionPanelController == null || hitTargetStep == null)
            return;

        instructionPanelController.ShowTemporaryStep(
            hitTargetStep,
            TemporaryRestoreMode.HideAfterTemporary
        );
    }

    public void ShowResetIslandsInstruction()
    {
        if (instructionPanelController == null || resetIslandsStep == null)
            return;

        instructionPanelController.ShowStep(resetIslandsStep);
    }

    private void CancelNarrativeRoutines()
    {
        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
            introRoutine = null;
        }
    }

    private void LoadLocalProgress(LabLocalProgress progress)
    {
        currentLocalProgress = progress;

        maxAttempts = progress.maxAttempts;
        usedAttempts = progress.usedAttempts;
        remainingAttempts = progress.remainingAttempts;

        labLocked = false;
        finalHitTarget = progress.hitTarget;

        attemptResults.Clear();

        if (progress.attempts != null)
            attemptResults.AddRange(progress.attempts);

        Debug.Log(
            $"Progreso local cargado. RunId: {progress.runId} | " +
            $"Usados: {usedAttempts} | Restantes: {remainingAttempts}"
        );
    }

    private void CreateInitialLocalProgress(LabBridgeInput data)
    {
        if (data == null || string.IsNullOrEmpty(data.runId))
            return;

        currentLocalProgress = new LabLocalProgress
        {
            schemaVersion = data.schemaVersion,
            requestId = data.requestId,
            runId = data.runId,

            labKey = data.scene != null ? data.scene.labKey : "PARABOLIC-001",
            exerciseId = data.exercise != null ? data.exercise.exerciseId : "",

            participantId = data.participant != null ? data.participant.participantId : "",
            participantName = data.participant != null ? data.participant.displayName : "",

            maxAttempts = maxAttempts,
            usedAttempts = usedAttempts,
            remainingAttempts = remainingAttempts,

            completed = false,
            hitTarget = false,

            attempts = attemptResults.ToArray(),

            startedAt = DateTime.UtcNow.ToString("o"),
            updatedAt = DateTime.UtcNow.ToString("o")
        };

        LabProgressStorage.Save(currentLocalProgress);
    }

    public void ApplyExerciseData(LabBridgeInput data)
    {
        int configuredAttempts = defaultMaxAttempts;

        currentRunId = data != null ? data.runId : "";

        bool allowResume = data != null &&
                           data.exercise != null &&
                           data.exercise.allowResume;

        if (!string.IsNullOrEmpty(currentRunId) && allowResume)
        {
            LabLocalProgress savedProgress = LabProgressStorage.Load(currentRunId);

            if (savedProgress != null && !savedProgress.completed)
            {
                LoadLocalProgress(savedProgress);
                UpdateAttemptsUI();
                return;
            }
        }

        if (data != null &&
            data.exercise != null &&
            data.exercise.maxAttempts > 0)
        {
            configuredAttempts = data.exercise.maxAttempts;
        }

        maxAttempts = configuredAttempts;
        remainingAttempts = maxAttempts;
        usedAttempts = 0;
        labLocked = false;
        finalHitTarget = false;
        attemptResults.Clear();

        CreateInitialLocalProgress(data);

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
            UpdateAttemptsUI();
            ShowNoAttemptsInstruction();
            return false;
        }

        if (launcher == null)
        {
            Debug.LogWarning("No hay CannonLauncher para disparar.");
            return false;
        }

        CaptureCurrentShotData(launcher);

        bool fired = launcher.Fire();

        if (!fired)
            return false;

        currentProjectileResultRegistered = false;

        usedAttempts++;
        remainingAttempts--;
        UpdateAttemptsUI();

        SetState(LabState.ProjectileInFlight);

        StartProjectileCameraIfEnabled(launcher);

        return true;
    }

    private void CaptureCurrentShotData(CannonLauncher launcher)
    {
        currentShotPower = launcher != null ? launcher.CurrentLaunchPower : -1f;

        CannonAimController aimController = launcher != null
            ? launcher.GetComponentInChildren<CannonAimController>()
            : null;

        currentShotAngle = aimController != null ? aimController.CurrentPitch : -1f;

        hasCurrentShotData = true;
    }

    private void ClearCurrentShotData()
    {
        currentShotPower = 0f;
        currentShotAngle = 0f;
        hasCurrentShotData = false;
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

        CannonAimController aimController = cannonLauncher != null
            ? cannonLauncher.GetComponentInChildren<CannonAimController>()
            : null;

        if (cannonLauncher != null)
        {
            cannonLauncher.SetLabReferences(
                this,
                appleTarget,
                aimController
            );
        }

        SetState(LabState.ReadyToLaunch);
    }

    public void NotifyProjectileImpact(ProjectileImpactData data)
    {
        if (data == null)
            return;

        if (currentProjectileResultRegistered)
        {
            Debug.LogWarning("Impacto ignorado: este proyectil ya registró un resultado.");
            return;
        }

        if (currentState != LabState.ProjectileInFlight)
        {
            Debug.LogWarning($"Impacto ignorado: estado actual no válido ({currentState}).");
            return;
        }

        currentProjectileResultRegistered = true;

        RegisterAttemptResult(data);
        ShowImpactMeasurementIfNeeded(data);
        ClearCurrentShotData();

        //RestoreProjectileCameraIfActive();

        Debug.Log(
            $"Impacto: {data.impactType} | " +
            $"Distancia al target: {data.impactDistanceToTarget:0.00} m | " +
            $"Horizontal: {data.impactHorizontalDistance:0.00} m | " +
            $"Altura: {data.impactHeightDifference:0.00} m"
        );

        if (projectileFlightCameraController != null &&
            projectileFlightCameraController.IsActive)
        {
            projectileFlightCameraController.RestoreARCameraAfterImpactDelay(
                () => ContinueAfterProjectileImpact(data)
            );

            return;
        }

        ContinueAfterProjectileImpact(data);

        /*switch (data.impactType)
        {
            case ProjectileImpactType.HitTarget:
                finalHitTarget = true;
                labLocked = true;

                SetState(LabState.Completed);

                if (instructionPanelController != null && hitTargetStep != null)
                {
                    instructionPanelController.ShowTemporaryStep(
                        hitTargetStep,
                        TemporaryRestoreMode.HideAfterTemporary,
                        () => StartFinalResultsSequence()
                    );
                }
                else
                {
                    StartFinalResultsSequence();
                }

                break;

            case ProjectileImpactType.MissedTarget:
                SetState(LabState.AttemptResult);

                if (instructionPanelController != null && missedTargetStep != null)
                {
                    instructionPanelController.ShowTemporaryStep(
                        missedTargetStep,
                        TemporaryRestoreMode.HideAfterTemporary,
                        () => ReturnToReadyIfPossible(false)
                    );
                }
                else
                {
                    ReturnToReadyIfPossible(false);
                }

                break;

            case ProjectileImpactType.OutOfBounds:
                SetState(LabState.AttemptResult);

                if (instructionPanelController != null && outOfBoundsStep != null)
                {
                    instructionPanelController.ShowTemporaryStep(
                        outOfBoundsStep,
                        TemporaryRestoreMode.HideAfterTemporary,
                        () => ReturnToReadyIfPossible(false)
                    );
                }
                else
                {
                    ReturnToReadyIfPossible(false);
                }

                break;
        }*/
    }

    private void ContinueAfterProjectileImpact(ProjectileImpactData data)
    {
        switch (data.impactType)
        {
            case ProjectileImpactType.HitTarget:
                finalHitTarget = true;
                labLocked = true;

                SetState(LabState.Completed);

                if (instructionPanelController != null && hitTargetStep != null)
                {
                    instructionPanelController.ShowTemporaryStep(
                        hitTargetStep,
                        TemporaryRestoreMode.HideAfterTemporary,
                        () => StartFinalResultsSequence()
                    );
                }
                else
                {
                    StartFinalResultsSequence();
                }

                break;

            case ProjectileImpactType.MissedTarget:
                SetState(LabState.AttemptResult);

                if (instructionPanelController != null && missedTargetStep != null)
                {
                    instructionPanelController.ShowTemporaryStep(
                        missedTargetStep,
                        TemporaryRestoreMode.HideAfterTemporary,
                        () => ReturnToReadyIfPossible(false)
                    );
                }
                else
                {
                    ReturnToReadyIfPossible(false);
                }

                break;

            case ProjectileImpactType.OutOfBounds:
                SetState(LabState.AttemptResult);

                if (instructionPanelController != null && outOfBoundsStep != null)
                {
                    instructionPanelController.ShowTemporaryStep(
                        outOfBoundsStep,
                        TemporaryRestoreMode.HideAfterTemporary,
                        () => ReturnToReadyIfPossible(false)
                    );
                }
                else
                {
                    ReturnToReadyIfPossible(false);
                }

                break;
        }
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

    private void ReturnToReadyIfPossible(bool showReadyInstruction = true)
    {
        if (remainingAttempts <= 0)
        {
            labLocked = true;
            SetState(LabState.Completed);

            //ShowNoAttemptsInstruction();
            //StartFinalResultsSequence(noAttemptsStep);
            if (instructionPanelController != null && noAttemptsStep != null)
            {
                instructionPanelController.ShowStep(
                    noAttemptsStep,
                    () => StartFinalResultsSequence()
                );
            }
            else
            {
                StartFinalResultsSequence();
            }
            return;
        }

        suppressNextReadyInstruction = !showReadyInstruction;
        SetState(LabState.ReadyToLaunch);
    }

    private void RegisterAttemptResult(ProjectileImpactData data)
    {
        bool isOutOfBounds = data.impactType == ProjectileImpactType.OutOfBounds;
        bool isHit = data.impactType == ProjectileImpactType.HitTarget;

        LabAttemptResult result = new LabAttemptResult
        {
            attempt = usedAttempts,
            hit = isHit,

            power = hasCurrentShotData ? currentShotPower : data.power,
            angle = hasCurrentShotData ? currentShotAngle : data.angle,

            impactDistanceToTarget = isOutOfBounds ? -1f : data.impactDistanceToTarget,
            impactHorizontalDistance = isOutOfBounds ? -1f : data.impactHorizontalDistance,
            impactHeightDifference = isOutOfBounds ? -1f : data.impactHeightDifference,

            impactType = data.impactType.ToString(),

            impactPoint = new Vector3Serializable(data.impactPoint),
            targetPoint = new Vector3Serializable(data.targetPoint)
        };

        attemptResults.Add(result);
        SaveCurrentProgress();

        Debug.Log($"Intento agregado al JSON: {JsonUtility.ToJson(result)}");
    }

    private void SaveCurrentProgress()
    {
        LabBridgeInput input = AndroidBridge.Instance != null
            ? AndroidBridge.Instance.CurrentLabInput
            : null;

        if (input == null || string.IsNullOrEmpty(input.runId))
            return;

        if (currentLocalProgress == null)
            CreateInitialLocalProgress(input);

        if (currentLocalProgress == null)
            return;

        currentLocalProgress.usedAttempts = usedAttempts;
        currentLocalProgress.remainingAttempts = remainingAttempts;
        currentLocalProgress.completed = false;
        currentLocalProgress.hitTarget = finalHitTarget;
        currentLocalProgress.attempts = attemptResults.ToArray();

        LabProgressStorage.Save(currentLocalProgress);
    }

    private void ShowImpactMeasurementIfNeeded(ProjectileImpactData data)
    {
        if (impactMeasurementVisualizer == null)
            return;

        if (data.impactType == ProjectileImpactType.OutOfBounds)
            return;

        impactMeasurementVisualizer.ShowMeasurement(
            data.impactPoint,
            data.targetPoint,
            data.impactDistanceToTarget,
            data.impactHorizontalDistance,
            data.impactHeightDifference
        );
    }

    private LabFinalResult BuildFinalResultObject(bool completed, string resultStatus = "", string exitReason = "")
    {
        LabBridgeInput input = AndroidBridge.Instance != null
            ? AndroidBridge.Instance.CurrentLabInput
            : null;

        return new LabFinalResult
        {
            schemaVersion = input != null ? input.schemaVersion : 1,

            requestId = input != null ? input.requestId : "",
            runId = input != null ? input.runId : "",

            labKey = input != null && input.scene != null
                ? input.scene.labKey
                : "PARABOLIC-001",

            unitySceneName = input != null && input.scene != null
                ? input.scene.unitySceneName
                : "",

            exerciseId = input != null && input.exercise != null
                ? input.exercise.exerciseId
                : "",

            participantId = input != null && input.participant != null
                ? input.participant.participantId
                : "",

            participantName = input != null && input.participant != null
                ? input.participant.displayName
                : "",

            organizationName = input != null && input.context != null
                ? input.context.organizationName
                : "",

            courseName = input != null && input.context != null
                ? input.context.courseName
                : "",

            groupName = input != null && input.context != null
                ? input.context.groupName
                : "",

            horizontalDistance = horizontalDistance,
            verticalDistance = heightDifference,
            straightDistance = straightDistance,

            hitTarget = finalHitTarget,
            maxAttempts = maxAttempts,
            usedAttempts = usedAttempts,
            remainingAttempts = remainingAttempts,
            completed = completed,
            resultStatus = string.IsNullOrEmpty(resultStatus)
                ? (completed ? "completed" : "incomplete")
                : resultStatus,
            exitReason = exitReason,

            attempts = attemptResults.ToArray(),

            finishedAt = DateTime.UtcNow.ToString("o")
        };
    }

    public string BuildFinalResultJson(
    bool completed,
    string resultStatus = "",
    string exitReason = ""
)
    {
        LabFinalResult finalResult = BuildFinalResultObject(
            completed,
            resultStatus,
            exitReason
        );

        return JsonUtility.ToJson(finalResult, true);
    }

    private void UpdateFireButtonState()
    {
        bool canFire = CanFire();
        bool canExit = currentState != LabState.ProjectileInFlight &&
                   currentState != LabState.Results;

        if (cannonUIController != null)
        {
            cannonUIController.SetFireButtonInteractable(canFire);
            cannonUIController.SetExitButtonInteractable(canExit);
        }

        if (trajectoryPreview != null)
            trajectoryPreview.SetVisible(canFire);
    }

    public void FinishAndReturnToAndroid()
    {
        string json = BuildFinalResultJson(
            true,
            "completed",
            ""
        );

        if (AndroidBridge.Instance != null)
            AndroidBridge.Instance.FinishLabAndReturn(json);

        LabBridgeInput input = AndroidBridge.Instance != null
            ? AndroidBridge.Instance.CurrentLabInput
            : null;

        if (input != null && !string.IsNullOrEmpty(input.runId))
            LabProgressStorage.Delete(input.runId);
    }

    public void ExitIncompleteAndReturnToAndroid()
    {
        SaveCurrentProgress();

        string json = BuildFinalResultJson(
            false,
            "incomplete",
            "user_exit"
        );

        if (AndroidBridge.Instance != null)
            AndroidBridge.Instance.FinishLabAndReturn(json);

        // Importante:
        // NO borrar LabProgressStorage aquí.
        // Si se borra, el usuario podría reiniciar el laboratorio sin consumir intentos.
    }

    private void StartReturnToReadyAfterTemporaryStep(InstructionStep step)
    {
        if (returnToReadyRoutine != null)
        {
            StopCoroutine(returnToReadyRoutine);
            returnToReadyRoutine = null;
        }

        returnToReadyRoutine = StartCoroutine(ReturnToReadyAfterTemporaryStepRoutine(step));
    }

    private IEnumerator ReturnToReadyAfterTemporaryStepRoutine(InstructionStep step)
    {
        float delay = 1.5f;

        if (instructionPanelController != null && step != null)
        {
            delay = instructionPanelController.GetTotalStepDuration(step);
        }

        yield return new WaitForSecondsRealtime(delay + 0.05f);

        returnToReadyRoutine = null;

        ReturnToReadyIfPossible(false);
    }

    private void StartFinalResultsSequence()
    {
        if (finalSequenceStarted)
            return;

        finalSequenceStarted = true;

        StartCoroutine(FinalResultsSequenceRoutine());
    }

    private IEnumerator FinalResultsSequenceRoutine()
    {
        yield return new WaitForSecondsRealtime(delayBeforeFinalPanel);

        if (arLabSceneCleaner != null)
            arLabSceneCleaner.CleanScene();

        LabFinalResult finalResult = BuildFinalResultObject(true);
        string json = JsonUtility.ToJson(finalResult, true);

        SetState(LabState.Results);

        if (resultsPanelController != null)
            resultsPanelController.ShowResults(finalResult, json);
    }

    private bool UseProjectileCamera()
    {
        return projectileCameraToggle != null && projectileCameraToggle.isOn;
    }

    private void StartProjectileCameraIfEnabled(CannonLauncher launcher)
    {
        if (!UseProjectileCamera())
            return;

        if (projectileFlightCameraController == null)
        {
            Debug.LogWarning("Falta ProjectileFlightCameraController.");
            return;
        }

        if (launcher == null || launcher.LastProjectile == null)
        {
            Debug.LogWarning("No se pudo activar cámara: LastProjectile es null.");
            return;
        }

        projectileFlightCameraController.StartFollow(
            launcher.LastProjectile.transform
        );
    }

    private void RestoreProjectileCameraIfActive()
    {
        if (projectileFlightCameraController == null)
            return;

        if (!projectileFlightCameraController.IsActive)
            return;

        projectileFlightCameraController.RestoreARCamera();
    }
}