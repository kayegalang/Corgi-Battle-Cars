using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using _Cars.ScriptableObjects;
using _Player.Scripts;
using _Projectiles.ScriptableObjects;
using TMPro;

namespace _UI.Scripts
{
    /// <summary>
    /// One instance per player in multiplayer character select.
    /// Handles input from that player's specific device.
    /// </summary>
    public class PlayerCharacterSelectPanel : MonoBehaviour
    {
        // ═══════════════════════════════════════════════
        //  INSPECTOR REFERENCES
        // ═══════════════════════════════════════════════

        [Header("Car Selection")]
        [SerializeField] private TextMeshProUGUI carNameText;

        [Header("Weapon Selection")]
        [SerializeField] private TextMeshProUGUI weaponNameText;

        [Header("Stats Panel")]
        [SerializeField] private Text  statsHeaderText;
        [SerializeField] private Text  statLabel1;
        [SerializeField] private Text  statLabel2;
        [SerializeField] private Text  statLabel3;
        [SerializeField] private Text  statLabel4;
        [SerializeField] private Image statBar1;
        [SerializeField] private Image statBar2;
        [SerializeField] private Image statBar3;
        [SerializeField] private Image statBar4;

        [Header("Status")]
        [SerializeField] private TextMeshProUGUI  playerLabelText;
        [SerializeField] private TextMeshProUGUI  statusText;
        [SerializeField] private GameObject readyOverlay;

        [Header("Selection Highlights")]
        [SerializeField] private GameObject weaponHighlight;
        [SerializeField] private GameObject carHighlight;

        [Header("Buttons — hidden when using controller")]
        [SerializeField] private GameObject backButton;
        [SerializeField] private GameObject mapSelectionButton;

        // ═══════════════════════════════════════════════
        //  RUNTIME STATE
        // ═══════════════════════════════════════════════

        private CarStats[]         carTypes;
        private ProjectileObject[] weaponTypes;
        private int                playerIndex;
        private string             playerTag;
        private PlayerInput        playerInput;
        private Gamepad            assignedGamepad;
        private bool               isKeyboard;

        private int carIndex    = 0;
        private int weaponIndex = 0;

        private enum StatsMode { Weapon, Car }
        private StatsMode statsMode = StatsMode.Weapon;

        private enum Step { SelectingWeapon, SelectingCar, Ready }
        private Step currentStep = Step.SelectingWeapon;

        private bool isReady       = false;
        private bool inputEnabled  = false;
        private bool stickConsumed = false;
        private const float STICK_THRESHOLD = 0.5f;

        public bool IsReady => isReady;

        public System.Action<int> OnPlayerReady;
        public System.Action<int> OnPlayerUnready;

        // ═══════════════════════════════════════════════
        //  INITIALIZATION
        // ═══════════════════════════════════════════════

        public void Initialize(
            int playerIdx,
            string tag,
            CarStats[] cars,
            ProjectileObject[] weapons,
            PlayerInput input)
        {
            playerIndex = playerIdx;
            playerTag   = tag;
            carTypes    = cars;
            weaponTypes = weapons;
            playerInput = input;

            isKeyboard      = input != null && input.currentControlScheme == "Keyboard";
            assignedGamepad = null;

            if (!isKeyboard && input != null)
            {
                foreach (var device in input.devices)
                {
                    if (device is Gamepad gp)
                    {
                        assignedGamepad = gp;
                        break;
                    }
                }
            }

            carIndex    = Mathf.Clamp(PlayerPrefs.GetInt(CarKey(),    0), 0, carTypes.Length    - 1);
            weaponIndex = Mathf.Clamp(PlayerPrefs.GetInt(WeaponKey(), 0), 0, weaponTypes.Length - 1);

            if (playerLabelText != null)
                playerLabelText.text = $"PLAYER {playerIdx + 1}";

            statsMode   = StatsMode.Weapon;
            currentStep = Step.SelectingWeapon;
            isReady     = false;

            // Hide mouse-only buttons when using controller
            if (backButton         != null) backButton.SetActive(isKeyboard);
            if (mapSelectionButton != null) mapSelectionButton.SetActive(isKeyboard);

            RefreshAll();

            if (readyOverlay != null) readyOverlay.SetActive(false);

            StartCoroutine(EnableInputWhenReleased());
        }

        private IEnumerator EnableInputWhenReleased()
        {
            if (assignedGamepad != null)
            {
                while (assignedGamepad.buttonSouth.isPressed ||
                       assignedGamepad.buttonEast.isPressed  ||
                       assignedGamepad.buttonNorth.isPressed ||
                       assignedGamepad.buttonWest.isPressed)
                    yield return null;
            }

            yield return null;
            inputEnabled = true;
            UpdateStatusText();
        }

        // ═══════════════════════════════════════════════
        //  UPDATE
        // ═══════════════════════════════════════════════

        private void Update()
        {
            if (!inputEnabled) return;

            if (isKeyboard) HandleKeyboardInput();
            else            HandleGamepadInput();
        }

        // ═══════════════════════════════════════════════
        //  KEYBOARD INPUT
        // ═══════════════════════════════════════════════

        private void HandleKeyboardInput()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                HandleConfirm();

            if (Input.GetKeyDown(KeyCode.Escape))
                HandleBack();

            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                if (currentStep == Step.SelectingWeapon) WeaponPrev();
                else if (currentStep == Step.SelectingCar) CarPrev();
            }

            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                if (currentStep == Step.SelectingWeapon) WeaponNext();
                else if (currentStep == Step.SelectingCar) CarNext();
            }
        }

        // ═══════════════════════════════════════════════
        //  GAMEPAD INPUT
        // ═══════════════════════════════════════════════

        private void HandleGamepadInput()
        {
            Gamepad pad = assignedGamepad ?? Gamepad.current;
            if (pad == null) return;

            float x = pad.leftStick.x.ReadValue();
            if (Mathf.Abs(x) > STICK_THRESHOLD)
            {
                if (!stickConsumed)
                {
                    stickConsumed = true;
                    if (currentStep == Step.SelectingWeapon)
                    {
                        if (x > 0) WeaponNext(); else WeaponPrev();
                    }
                    else if (currentStep == Step.SelectingCar)
                    {
                        if (x > 0) CarNext(); else CarPrev();
                    }
                }
            }
            else
            {
                stickConsumed = false;
            }

            if (pad.buttonSouth.wasPressedThisFrame) HandleConfirm();
            if (pad.buttonEast.wasPressedThisFrame)  HandleBack();
        }

        // ═══════════════════════════════════════════════
        //  CONFIRM / BACK
        // ═══════════════════════════════════════════════

        private void HandleConfirm()
        {
            switch (currentStep)
            {
                case Step.SelectingWeapon:
                    currentStep = Step.SelectingCar;
                    statsMode   = StatsMode.Car;
                    RefreshStats();
                    break;

                case Step.SelectingCar:
                    currentStep = Step.Ready;
                    isReady     = true;
                    Save();
                    ShowReady();
                    OnPlayerReady?.Invoke(playerIndex);
                    break;
            }

            UpdateHighlights();
            UpdateStatusText();
        }

        private void HandleBack()
        {
            switch (currentStep)
            {
                case Step.SelectingCar:
                    currentStep = Step.SelectingWeapon;
                    statsMode   = StatsMode.Weapon;
                    RefreshStats();
                    break;

                case Step.Ready:
                    currentStep = Step.SelectingCar;
                    statsMode   = StatsMode.Car;
                    isReady     = false;
                    HideReady();
                    RefreshStats();
                    OnPlayerUnready?.Invoke(playerIndex);
                    break;
            }

            UpdateHighlights();
            UpdateStatusText();
        }

        // ═══════════════════════════════════════════════
        //  CAR / WEAPON NAVIGATION
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
        //  READY UI
        // ═══════════════════════════════════════════════

        private void ShowReady()
        {
            if (readyOverlay != null) readyOverlay.SetActive(true);
        }

        private void HideReady()
        {
            if (readyOverlay != null) readyOverlay.SetActive(false);
        }

        private void UpdateStatusText()
        {
            if (statusText == null) return;

            switch (currentStep)
            {
                case Step.SelectingWeapon:
                    statusText.text = isKeyboard
                        ? "← → Choose Weapon   Enter: Confirm"
                        : "← → Choose Weapon   A: Confirm";
                    break;
                case Step.SelectingCar:
                    statusText.text = isKeyboard
                        ? "← → Choose Car   Enter: Ready   Esc: Back"
                        : "← → Choose Car   A: Ready   B: Back";
                    break;
                case Step.Ready:
                    statusText.text = isKeyboard ? "READY!   Esc: Undo" : "READY!   B: Undo";
                    break;
            }
        }

        // ═══════════════════════════════════════════════
        //  HIGHLIGHTS
        // ═══════════════════════════════════════════════

        private void UpdateHighlights()
        {
            if (weaponHighlight != null)
                weaponHighlight.SetActive(currentStep == Step.SelectingWeapon);

            if (carHighlight != null)
                carHighlight.SetActive(currentStep == Step.SelectingCar);
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
            UpdateStatusText();
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

            float damage    = Mathf.InverseLerp(1f,    100f, w.Damage);
            float fireRate  = 1f - Mathf.InverseLerp(0.05f, 5f, w.FireRate);
            float fireForce = Mathf.InverseLerp(1f,    100f, w.FireForce);
            float recoil    = Mathf.InverseLerp(0f,    50f,  w.RecoilForce);

            SetBar(statLabel1, statBar1, "DMG",  damage);
            SetBar(statLabel2, statBar2, "RATE", fireRate);
            SetBar(statLabel3, statBar3, "SPD",  fireForce);
            SetBar(statLabel4, statBar4, "RCOL", recoil);
        }

        private void SetBar(Text label, Image bar, string labelText, float fill)
        {
            if (label != null) label.text = labelText;
            if (bar   != null) bar.fillAmount = Mathf.Clamp01(fill);
        }

        // ═══════════════════════════════════════════════
        //  SAVE
        // ═══════════════════════════════════════════════

        private void Save()
        {
            PlayerPrefs.SetInt(CarKey(),    carIndex);
            PlayerPrefs.SetInt(WeaponKey(), weaponIndex);
            PlayerPrefs.Save();
        }

        private string CarKey()    => $"SelectedCarTypeIndex_{playerTag}";
        private string WeaponKey() => $"SelectedWeaponTypeIndex_{playerTag}";

        // ═══════════════════════════════════════════════
        //  GETTERS
        // ═══════════════════════════════════════════════

        public int              GetCarIndex()       => carIndex;
        public int              GetWeaponIndex()    => weaponIndex;
        public CarStats         GetSelectedCar()    => carTypes[carIndex];
        public ProjectileObject GetSelectedWeapon() => weaponTypes[weaponIndex];
    }
}