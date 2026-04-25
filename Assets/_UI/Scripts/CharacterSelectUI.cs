using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using _Cars.ScriptableObjects;
using _Player.Scripts;
using _Projectiles.ScriptableObjects;
using TMPro;
using _UI.Scripts;
using _Audio.scripts;

public class CharacterSelectUI : MonoBehaviour
{
    [Header("Car Selection")]
    [SerializeField] private CarStats[] carTypes;
    [SerializeField] private TextMeshProUGUI carNameText;

    [Header("Weapon Selection")]
    [SerializeField] private ProjectileObject[] weaponTypes;
    [SerializeField] private TextMeshProUGUI weaponNameText;

    [Header("Car Stats (Sprites)")]
    [SerializeField] private StatUI jump;
    [SerializeField] private StatUI acceleration;
    [SerializeField] private StatUI health;
    [SerializeField] private StatUI speed;

    [Header("Weapon Stats (Sprites)")]
    [SerializeField] private StatUI damage;
    [SerializeField] private StatUI cooldown;
    [SerializeField] private StatUI fireRate;

    [Header("Instruction & Selection Text")]
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI selectionNameText;

    [Header("Arrow Buttons — hidden when using controller")]
    [SerializeField] private GameObject[] arrowButtons;

    [Header("Buttons — hidden when using controller")]
    [SerializeField] private GameObject backButton;
    [SerializeField] private GameObject mapSelectionButton;

    [Header("Selection Highlights")]
    [SerializeField] private GameObject weaponHighlight;
    [SerializeField] private GameObject carHighlight;

    [Header("Navigation")]
    [SerializeField] private GameObject mapSelectionPanel;
    [SerializeField] private GameObject previousPanel;
    [SerializeField] private GameObject characterSelectionPanel;

    [Header("Preview")]
    [SerializeField] private CharacterSelectPreview preview;

    private int carIndex;
    private int weaponIndex;

    private enum StatsMode { Weapon, Car }
    private StatsMode statsMode = StatsMode.Weapon;

    private enum ControllerStep { SelectingWeapon, SelectingCar, Ready }
    private ControllerStep controllerStep = ControllerStep.SelectingWeapon;

    private bool isControllerMode = false;
    private bool stickConsumed    = false;
    private const float STICK_THRESHOLD = 0.5f;

    // ═══════════════════════════════════════════════
    //  LIFECYCLE
    // ═══════════════════════════════════════════════

    private void OnEnable()
    {
        carIndex       = 0;
        weaponIndex    = 0;
        statsMode      = StatsMode.Weapon;
        controllerStep = ControllerStep.SelectingWeapon;
        stickConsumed  = false;
        isControllerMode = false;

        bool controllerConnected =
            PlayerOneInputTracker.instance != null &&
            PlayerOneInputTracker.instance.IsPlayerOneUsingController();

        if (backButton         != null) backButton.SetActive(!controllerConnected);
        if (mapSelectionButton != null) mapSelectionButton.SetActive(!controllerConnected);

        if (arrowButtons != null)
            foreach (var btn in arrowButtons)
                if (btn != null) btn.SetActive(!controllerConnected);

        Save();
        RefreshAll();
        preview?.UpdatePreview(carIndex, weaponIndex);
        StartCoroutine(EnableControllerWhenButtonsReleased());
    }

    private IEnumerator EnableControllerWhenButtonsReleased()
    {
        Gamepad pad = Gamepad.current;
        if (pad != null)
        {
            while (pad.buttonSouth.isPressed || pad.buttonEast.isPressed ||
                   pad.buttonNorth.isPressed || pad.buttonWest.isPressed)
                yield return null;
        }

        yield return null;
        DetectInputMode();
    }

    private void Update()
    {
        if (!isControllerMode) return;
        HandleControllerInput();
    }

    // ═══════════════════════════════════════════════
    //  INPUT MODE
    // ═══════════════════════════════════════════════

    private void DetectInputMode()
    {
        bool usingController = PlayerOneInputTracker.instance != null
            && PlayerOneInputTracker.instance.IsPlayerOneUsingController();
        SetControllerMode(usingController);
    }

    private void SetControllerMode(bool controller)
    {
        isControllerMode = controller;

        if (backButton         != null) backButton.SetActive(!controller);
        if (mapSelectionButton != null) mapSelectionButton.SetActive(!controller);

        if (arrowButtons != null)
            foreach (var btn in arrowButtons)
                if (btn != null) btn.SetActive(!controller);

        UpdateHighlights();
        UpdateInstructionText();
    }

    // ═══════════════════════════════════════════════
    //  CONTROLLER INPUT
    // ═══════════════════════════════════════════════

    private void HandleControllerInput()
    {
        Gamepad pad = Gamepad.current;
        if (pad == null) return;

        HandleStick(pad);
        HandleSouth(pad);
        HandleEast(pad);
    }

    private void HandleStick(Gamepad pad)
    {
        float x = pad.leftStick.x.ReadValue();
        if (Mathf.Abs(x) > STICK_THRESHOLD)
        {
            if (!stickConsumed)
            {
                stickConsumed = true;
                if (controllerStep == ControllerStep.SelectingWeapon)
                { if (x > 0) WeaponNext(); else WeaponPrev(); }
                else if (controllerStep == ControllerStep.SelectingCar)
                { if (x > 0) CarNext(); else CarPrev(); }
            }
        }
        else stickConsumed = false;
    }

    private void HandleSouth(Gamepad pad)
    {
        if (!pad.buttonSouth.wasPressedThisFrame) return;
        switch (controllerStep)
        {
            case ControllerStep.SelectingWeapon:
                controllerStep = ControllerStep.SelectingCar;
                statsMode      = StatsMode.Car;
                RefreshStats();
                break;
            case ControllerStep.SelectingCar:
                controllerStep = ControllerStep.Ready;
                Save();
                if (AudioManager.instance != null && FMODEvents.instance != null)
                    AudioManager.instance.PlayOneShot(FMODEvents.instance.readyup, transform.position);
                AdvanceToMapSelection();
                break;
        }
        UpdateHighlights();
        UpdateInstructionText();
    }

    private void HandleEast(Gamepad pad)
    {
        if (!pad.buttonEast.wasPressedThisFrame) return;
        switch (controllerStep)
        {
            case ControllerStep.SelectingCar:
                controllerStep = ControllerStep.SelectingWeapon;
                statsMode      = StatsMode.Weapon;
                RefreshStats();
                break;
            case ControllerStep.SelectingWeapon:
                GoBack();
                break;
        }
        UpdateHighlights();
        UpdateInstructionText();
    }

    // ═══════════════════════════════════════════════
    //  INSTRUCTION TEXT
    // ═══════════════════════════════════════════════

    private void UpdateInstructionText()
    {
        if (instructionText != null)
        {
            switch (controllerStep)
            {
                case ControllerStep.SelectingWeapon:
                    instructionText.text = isControllerMode
                        ? "CHOOSE WEAPON   A: Confirm   B: Back"
                        : "CHOOSE WEAPON";
                    break;
                case ControllerStep.SelectingCar:
                    instructionText.text = isControllerMode
                        ? "CHOOSE CAR   A: Confirm   B: Back"
                        : "CHOOSE CAR";
                    break;
                case ControllerStep.Ready:
                    instructionText.text = isControllerMode ? "READY!   B: Undo" : "READY!";
                    break;
            }
        }

        if (selectionNameText != null)
        {
            selectionNameText.text = controllerStep == ControllerStep.SelectingWeapon
                ? weaponTypes[weaponIndex].ProjectileName
                : carTypes[carIndex].CarName;
        }
    }

    // ═══════════════════════════════════════════════
    //  HIGHLIGHTS
    // ═══════════════════════════════════════════════

    private void UpdateHighlights()
    {
        if (weaponHighlight != null)
            weaponHighlight.SetActive(isControllerMode && controllerStep == ControllerStep.SelectingWeapon);
        if (carHighlight != null)
            carHighlight.SetActive(isControllerMode && controllerStep == ControllerStep.SelectingCar);
    }

    // ═══════════════════════════════════════════════
    //  NAVIGATION
    // ═══════════════════════════════════════════════

    public void CarNext()
    {
        carIndex  = (carIndex + 1) % carTypes.Length;
        statsMode = StatsMode.Car;
        RefreshCarName();
        RefreshStats();
        Save();
        preview?.UpdatePreview(carIndex, weaponIndex);
        UpdateInstructionText();
        if (AudioManager.instance != null && FMODEvents.instance != null)
            AudioManager.instance.PlayOneShot(FMODEvents.instance.characterselect, transform.position);
    }

    public void CarPrev()
    {
        carIndex  = (carIndex - 1 + carTypes.Length) % carTypes.Length;
        statsMode = StatsMode.Car;
        RefreshCarName();
        RefreshStats();
        Save();
        preview?.UpdatePreview(carIndex, weaponIndex);
        UpdateInstructionText();
        if (AudioManager.instance != null && FMODEvents.instance != null)
            AudioManager.instance.PlayOneShot(FMODEvents.instance.characterselect, transform.position);
    }

    public void WeaponNext()
    {
        weaponIndex = (weaponIndex + 1) % weaponTypes.Length;
        statsMode   = StatsMode.Weapon;
        RefreshWeaponName();
        RefreshStats();
        Save();
        preview?.UpdatePreview(carIndex, weaponIndex);
        UpdateInstructionText();
        if (AudioManager.instance != null && FMODEvents.instance != null)
            AudioManager.instance.PlayOneShot(FMODEvents.instance.characterselect, transform.position);
    }

    public void WeaponPrev()
    {
        weaponIndex = (weaponIndex - 1 + weaponTypes.Length) % weaponTypes.Length;
        statsMode   = StatsMode.Weapon;
        RefreshWeaponName();
        RefreshStats();
        Save();
        preview?.UpdatePreview(carIndex, weaponIndex);
        UpdateInstructionText();
        if (AudioManager.instance != null && FMODEvents.instance != null)
            AudioManager.instance.PlayOneShot(FMODEvents.instance.characterselect, transform.position);
    }

    public void AdvanceToMapSelection()
    {
        Save();
        if (mapSelectionPanel != null)
        {
            mapSelectionPanel.SetActive(true);
            gameObject.SetActive(false);
        }
    }

    public void GoBack()
    {
        if (previousPanel != null)
        {
            previousPanel.SetActive(true);
            gameObject.SetActive(false);
            if (characterSelectionPanel != null)
                characterSelectionPanel.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // ═══════════════════════════════════════════════
    //  REFRESH
    // ═══════════════════════════════════════════════

    private void RefreshAll()
    {
        RefreshCarName();
        RefreshWeaponName();
        RefreshStats();
        UpdateHighlights();
        UpdateInstructionText();
    }

    private void RefreshCarName()
    {
        if (carNameText != null && carTypes.Length > 0)
            carNameText.text = carTypes[carIndex].CarName;
    }

    private void RefreshWeaponName()
    {
        if (weaponNameText != null && weaponTypes.Length > 0)
            weaponNameText.text = weaponTypes[weaponIndex].ProjectileName;
    }

    private void RefreshStats()
    {
        if (statsMode == StatsMode.Car) ApplyCarStats();
        else                             ApplyWeaponStats();
    }

    private void ApplyCarStats()
    {
        var s = carTypes[carIndex];
        jump.Set(s.JumpForceStat        / 100f);
        acceleration.Set(s.AccelerationStat / 100f);
        health.Set(s.HealthStat         / 100f);
        speed.Set(s.SpeedStat           / 100f);
    }

    private void ApplyWeaponStats()
    {
        var w = weaponTypes[weaponIndex];
        damage.Set(w.DamageStat    / 100f);
        cooldown.Set(w.CooldownStat / 100f);
        fireRate.Set(w.FireRateStat / 100f);
    }

    // ═══════════════════════════════════════════════
    //  SAVE — writes directly to PlayerLoadout, no PlayerPrefs
    // ═══════════════════════════════════════════════

    private void Save()
    {
        PlayerLoadout.SetCar(0,    carTypes[carIndex]);
        PlayerLoadout.SetWeapon(0, weaponTypes[weaponIndex]);
    }

    // ═══════════════════════════════════════════════
    //  GETTERS
    // ═══════════════════════════════════════════════

    public int              GetSelectedCarIndex()    => carIndex;
    public int              GetSelectedWeaponIndex() => weaponIndex;
    public CarStats         GetSelectedCarStats()    => carTypes[carIndex];
    public ProjectileObject GetSelectedWeapon()      => weaponTypes[weaponIndex];
}