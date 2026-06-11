using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CannonUIController : MonoBehaviour
{
    [Header("Root Panel")]
    [SerializeField] private GameObject controlsRoot;

    [Header("Attemps Panel")]
    [SerializeField] private GameObject AttempsPanel;

    [Header("Lab Controller")]
    [SerializeField] private ARPhysicsLabController labController;

    [Header("Buttons")]
    [SerializeField] private Button fireButton;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button upButton;
    [SerializeField] private Button downButton;

    [Header("Fire Button Visual")]
    [SerializeField] private LaunchButtonVisualState fireButtonVisualState;

    [Header("Power UI")]
    [SerializeField] private Slider powerSlider;
    [SerializeField] private TMP_InputField powerInput;
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private string powerFormat = "Velocidad: {0:0.0} m/s";

    [Header("Pitch UI")]
    [SerializeField] private Slider pitchSlider;
    [SerializeField] private TMP_InputField pitchInput;
    [SerializeField] private TMP_Text pitchText;
    [SerializeField] private string pitchFormat = "Ángulo: {0:0.0}°";

    private CannonLauncher currentLauncher;
    private CannonAimController currentAimController;

    private bool updatingUI;

    private void Awake()
    {
        AutoWireFireButtonVisual();

        ShowControls(false);
        RegisterUIEvents();
    }

    private void RegisterUIEvents()
    {
        if (powerSlider != null)
            powerSlider.onValueChanged.AddListener(SetPowerFromSlider);

        if (powerInput != null)
            powerInput.onEndEdit.AddListener(SetPowerFromInput);

        if (pitchSlider != null)
            pitchSlider.onValueChanged.AddListener(SetPitchFromSlider);

        if (pitchInput != null)
            pitchInput.onEndEdit.AddListener(SetPitchFromInput);
    }

    public void SetCannon(CannonLauncher launcher, CannonAimController aimController)
    {
        currentLauncher = launcher;
        currentAimController = aimController;

        bool hasCannon = currentLauncher != null && currentAimController != null;

        if (hasCannon)
        {
            SyncUIFromCannon();
        }

        // Ojo: todavía no mostramos controles aquí.
        // Los mostraremos cuando el ARPlacementManager confirme que ya están las 2 islas.
        SetControlsInteractable(hasCannon);
    }

    public void ShowControls(bool visible)
    {
        if (controlsRoot != null)
            controlsRoot.SetActive(visible);
        if (AttempsPanel != null)
            AttempsPanel.SetActive(visible);

        SetControlsInteractable(visible && currentLauncher != null && currentAimController != null);
    }

    public void Fire()
    {
        if (currentLauncher == null)
        {
            Debug.LogWarning("No hay CannonLauncher asignado todavía.");
            return;
        }

        if (labController != null)
        {
            labController.TryFire(currentLauncher);
            return;
        }

        currentLauncher.Fire();
    }

    public void SetFireButtonInteractable(bool interactable)
    {
        if (fireButton != null)
            fireButton.interactable = interactable;

        if (fireButtonVisualState != null)
            fireButtonVisualState.SetInteractableVisual(interactable);
    }

    public void RotateLeft()
    {
        if (currentAimController == null)
            return;

        currentAimController.RotateLeft();
        SyncUIFromCannon();
    }

    public void RotateRight()
    {
        if (currentAimController == null)
            return;

        currentAimController.RotateRight();
        SyncUIFromCannon();
    }

    public void AimUp()
    {
        if (currentAimController == null)
            return;

        currentAimController.AimUp();
        SyncUIFromCannon();
    }

    public void AimDown()
    {
        if (currentAimController == null)
            return;

        currentAimController.AimDown();
        SyncUIFromCannon();
    }

    public void SetPowerFromSlider(float value)
    {
        if (updatingUI)
            return;

        ApplyPower(value);
        UpdatePowerUI(value);
    }

    public void SetPowerFromInput(string text)
    {
        if (updatingUI)
            return;

        if (!TryParseFloat(text, out float value))
        {
            SyncUIFromCannon();
            return;
        }

        if (powerSlider != null)
            value = Mathf.Clamp(value, powerSlider.minValue, powerSlider.maxValue);

        ApplyPower(value);
        UpdatePowerUI(value);
    }

    public void SetPitchFromSlider(float value)
    {
        if (updatingUI)
            return;

        ApplyPitch(value);
        UpdatePitchUI(value);
    }

    public void SetPitchFromInput(string text)
    {
        if (updatingUI)
            return;

        if (!TryParseFloat(text, out float value))
        {
            SyncUIFromCannon();
            return;
        }

        if (currentAimController != null)
            value = Mathf.Clamp(value, currentAimController.MinPitch, currentAimController.MaxPitch);

        ApplyPitch(value);
        UpdatePitchUI(value);
    }

    private void ApplyPower(float value)
    {
        if (currentLauncher != null)
            currentLauncher.SetLaunchPower(value);
    }

    private void ApplyPitch(float value)
    {
        if (currentAimController != null)
            currentAimController.SetPitch(value);
    }

    private void SyncUIFromCannon()
    {
        if (currentLauncher != null)
            UpdatePowerUI(currentLauncher.CurrentLaunchPower);

        if (currentAimController != null)
            UpdatePitchUI(currentAimController.CurrentPitch);
    }

    private void UpdatePowerUI(float value)
    {
        updatingUI = true;

        if (powerSlider != null)
            powerSlider.value = value;

        if (powerInput != null)
            powerInput.text = value.ToString("0.0");

        if (powerText != null)
            powerText.text = string.Format(powerFormat, value);

        updatingUI = false;
    }

    private void UpdatePitchUI(float value)
    {
        updatingUI = true;

        if (pitchSlider != null)
            pitchSlider.value = value;

        if (pitchInput != null)
            pitchInput.text = value.ToString("0.0");

        if (pitchText != null)
            pitchText.text = string.Format(pitchFormat, value);

        updatingUI = false;
    }

    private bool TryParseFloat(string text, out float value)
    {
        text = text.Replace(",", ".");
        return float.TryParse(
            text,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out value
        );
    }

    private void SetControlsInteractable(bool value)
    {
        if (leftButton != null)
            leftButton.interactable = value;

        if (rightButton != null)
            rightButton.interactable = value;

        if (upButton != null)
            upButton.interactable = value;

        if (downButton != null)
            downButton.interactable = value;

        if (powerSlider != null)
            powerSlider.interactable = value;

        if (powerInput != null)
            powerInput.interactable = value;

        if (pitchSlider != null)
            pitchSlider.interactable = value;

        if (pitchInput != null)
            pitchInput.interactable = value;

        bool fireInteractable = value;

        if (labController != null)
            fireInteractable = value && labController.CanFire();

        SetFireButtonInteractable(fireInteractable);
    }

    public float GetCurrentPower()
    {
        return currentLauncher != null ? currentLauncher.CurrentLaunchPower : 0f;
    }

    public float GetCurrentPitch()
    {
        return currentAimController != null ? currentAimController.CurrentPitch : 0f;
    }

    public void OnExitButtonPressed()
    {
        if (AndroidBridge.Instance == null)
        {
            Debug.LogWarning("AndroidBridge.Instance no existe.");
            return;
        }

        AndroidBridge.Instance.FinishLabAndReturn();
    }

    private void AutoWireFireButtonVisual()
    {
        if (fireButtonVisualState == null && fireButton != null)
        {
            fireButtonVisualState = fireButton.GetComponent<LaunchButtonVisualState>();
        }

        if (fireButtonVisualState == null)
        {
            Debug.LogWarning("CannonUIController: falta asignar LaunchButtonVisualState.");
        }
    }
}