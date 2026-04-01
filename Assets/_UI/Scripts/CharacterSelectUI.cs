using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using _Cars.ScriptableObjects;
using _Player.Scripts;
using _Projectiles.ScriptableObjects;
using TMPro;

public class CharacterSelectUI : MonoBehaviour
{
    // ═══════════════════════════════════════════════
    //  INSPECTOR REFERENCES
    // ═══════════════════════════════════════════════

    [Header("Car Selection")]
    [SerializeField] private CarStats[] carTypes;
    [SerializeField] private TextMeshProUGUI carNameText;

    [Header("Weapon Selection")]
    [SerializeField] private ProjectileObject[] weaponTypes;
    [SerializeField] private TextMeshProUGUI weaponNameText;

    [Header("Stats Panel")]
    [SerializeField] private TextMeshProUGUI statsHeaderText;
    [SerializeField] private TextMeshProUGUI statLabel1;
    [SerializeField] private TextMeshProUGUI statLabel2;
    [SerializeField] private TextMeshProUGUI statLabel3;
    [SerializeField] private TextMeshProUGUI statLabel4;
    [SerializeField] private Image statBar1;
    [SerializeField] private Image statBar2;
    [SerializeField] private Image statBar3;
    [SerializeField] private Image statBar4;

    [Header("Buttons — hidden when using controller")]
    [SerializeField] private GameObject backButton;
    [SerializeField] private GameObject mapSelectionButton;

    [Header("Selection Highlights")]
    [SerializeField] private GameObject weaponHighlight;
    [SerializeField] private GameObject carHighlight;

    [Header("Navigation")]
    [SerializeField] private GameObject mapSelectionPanel;
    [SerializeField] private GameObject previousPanel;

    // ═══════════════════════════════════════════════
    //  PLAYER PREFS KEYS
    // ═══════════════════════════════════════════════

    private const string KEY_CAR    = "SelectedCarTypeIndex";
    private const string KEY_WEAPON = "SelectedWeaponTypeIndex";

    // ═══════════════════════════════════════════════
    //  STATE
    // ═══════════════════════════════════════════════

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
    //  UNITY LIFECYCLE
    // ═══════════════════════════════════════════════

    private void OnEnable()
    {
        carIndex       = Mathf.Clamp(PlayerPrefs.GetInt(KEY_CAR,    0), 0, carTypes.Length    - 1);
        weaponIndex    = Mathf.Clamp(PlayerPrefs.GetInt(KEY_WEAPON, 0), 0, weaponTypes.Length - 1);
        statsMode      = StatsMode.Weapon;
        controllerStep = ControllerStep.SelectingWeapon;
        stickConsumed  = false;
        isControllerMode = false;

        // ── Hide buttons immediately before the player sees them ──
        // Only trust PlayerOneInputTracker — never assume controller just because one is connected
        bool controllerConnected =
            PlayerOneInputTracker.instance != null && PlayerOneInputTracker.instance.IsPlayerOneUsingController();

        if (backButton         != null) backButton.SetActive(!controllerConnected);
        if (mapSelectionButton != null) mapSelectionButton.SetActive(!controllerConnected);

        RefreshAll();
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
        // Only trust PlayerOneInputTracker — what the player chose on the start screen
        // Never fall back to Gamepad.current, that would override keyboard players who have a controller connected
        bool usingController = PlayerOneInputTracker.instance != null
            && PlayerOneInputTracker.instance.IsPlayerOneUsingController();
        
        SetControllerMode(usingController);
    }

    private void SetControllerMode(bool controller)
    {
        isControllerMode = controller;

        if (backButton         != null) backButton.SetActive(!controller);
        if (mapSelectionButton != null) mapSelectionButton.SetActive(!controller);

        UpdateHighlights();
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
                {
                    if (x > 0) WeaponNext(); else WeaponPrev();
                }
                else if (controllerStep == ControllerStep.SelectingCar)
                {
                    if (x > 0) CarNext(); else CarPrev();
                }
            }
        }
        else
        {
            stickConsumed = false;
        }
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
                AdvanceToMapSelection();
                break;
        }

        UpdateHighlights();
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
    //  CAR NAVIGATION
    // ═══════════════════════════════════════════════

    public void CarNext()
    {
        carIndex  = (carIndex + 1) % carTypes.Length;
        statsMode = StatsMode.Car;
        RefreshCarName();
        RefreshStats();
        Save();
    }

    public void CarPrev()
    {
        carIndex  = (carIndex - 1 + carTypes.Length) % carTypes.Length;
        statsMode = StatsMode.Car;
        RefreshCarName();
        RefreshStats();
        Save();
    }

    // ═══════════════════════════════════════════════
    //  WEAPON NAVIGATION
    // ═══════════════════════════════════════════════

    public void WeaponNext()
    {
        weaponIndex = (weaponIndex + 1) % weaponTypes.Length;
        statsMode   = StatsMode.Weapon;
        RefreshWeaponName();
        RefreshStats();
        Save();
    }

    public void WeaponPrev()
    {
        weaponIndex = (weaponIndex - 1 + weaponTypes.Length) % weaponTypes.Length;
        statsMode   = StatsMode.Weapon;
        RefreshWeaponName();
        RefreshStats();
        Save();
    }

    // ═══════════════════════════════════════════════
    //  NAVIGATION
    // ═══════════════════════════════════════════════

    public void AdvanceToMapSelection()
    {
        Save();

        if (mapSelectionPanel != null)
        {
            mapSelectionPanel.SetActive(true);
            gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[CharacterSelectUI] mapSelectionPanel not assigned!");
        }
    }

    public void GoBack()
    {
        if (previousPanel != null)
        {
            previousPanel.SetActive(true);
            gameObject.SetActive(false);
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
        if (statsHeaderText != null) statsHeaderText.text = $"{s.CarName} Stats";
        SetBar(statLabel1, statBar1, "SPD",   s.SpeedStat        / 100f);
        SetBar(statLabel2, statBar2, "ACCEL", s.AccelerationStat / 100f);
        SetBar(statLabel3, statBar3, "JUMP",  s.JumpForceStat    / 100f);
        SetBar(statLabel4, statBar4, "HP",    s.HealthStat       / 100f);
    }

    private void ApplyWeaponStats()
    {
        var w = weaponTypes[weaponIndex];
        if (statsHeaderText != null) statsHeaderText.text = $"{w.ProjectileName} Stats";

        SetBar(statLabel1, statBar1, "DMG",  w.DamageStat   / 100f);
        SetBar(statLabel2, statBar2, "RATE", w.FireRateStat  / 100f);
        SetBar(statLabel3, statBar3, "COOL", w.CooldownStat  / 100f);
        SetBar(statLabel4, statBar4, "RCOL", w.RecoilStat    / 100f);
    }

    private void SetBar(TextMeshProUGUI label, Image bar, string labelText, float fill)
    {
        if (label != null) label.text = labelText;
        if (bar   != null) bar.fillAmount = Mathf.Clamp01(fill);
    }

    // ═══════════════════════════════════════════════
    //  SAVE
    // ═══════════════════════════════════════════════

    private void Save()
    {
        PlayerPrefs.SetInt(KEY_CAR,    carIndex);
        PlayerPrefs.SetInt(KEY_WEAPON, weaponIndex);
        PlayerPrefs.Save();
    }

    // ═══════════════════════════════════════════════
    //  PUBLIC GETTERS
    // ═══════════════════════════════════════════════

    public int              GetSelectedCarIndex()    => carIndex;
    public int              GetSelectedWeaponIndex() => weaponIndex;
    public CarStats         GetSelectedCarStats()    => carTypes[carIndex];
    public ProjectileObject GetSelectedWeapon()      => weaponTypes[weaponIndex];
}