using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using _Cars.ScriptableObjects;
using _Player.Scripts;
using _Projectiles.ScriptableObjects;
using TMPro;
using _UI.Scripts;
using _Audio.scripts;

namespace _UI.Scripts
{
    public class PlayerCharacterSelectPanel : MonoBehaviour
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

        [Header("Status")]
        [SerializeField] private TextMeshProUGUI playerLabelText;
        [SerializeField] private GameObject      readyOverlay;

        [Header("Preview")]
        [SerializeField] private CharacterSelectPreview preview;

        private const string KEY_CAR    = "SelectedCarTypeIndex";
        private const string KEY_WEAPON = "SelectedWeaponTypeIndex";

        // ═══════════════════════════════════════════════
        //  RUNTIME STATE
        // ═══════════════════════════════════════════════

        private int         playerIndex;
        private string      playerTag;
        private PlayerInput playerInput;
        private Gamepad     assignedGamepad;
        private bool        isKeyboard;
        private bool        isPlayerOne;

        private int carIndex    = 0;
        private int weaponIndex = 0;

        private enum StatsMode { Weapon, Car }
        private StatsMode statsMode = StatsMode.Weapon;

        private enum ControllerStep { SelectingWeapon, SelectingCar, Ready }
        private ControllerStep controllerStep = ControllerStep.SelectingWeapon;

        private bool isControllerMode = false;
        private bool isReady          = false;
        private bool inputEnabled     = false;
        private bool stickConsumed    = false;
        private const float STICK_THRESHOLD = 0.5f;

        public bool IsReady => isReady;

        public System.Action<int> OnPlayerReady;
        public System.Action<int> OnPlayerUnready;
        public System.Action      OnPlayerOneBack;

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

            isPlayerOne     = playerIdx == 0;
            isKeyboard      = input != null && input.currentControlScheme == "Keyboard";
            assignedGamepad = null;

            if (!isKeyboard && input != null)
                foreach (var device in input.devices)
                    if (device is Gamepad gp) { assignedGamepad = gp; break; }

            carIndex    = Mathf.Clamp(PlayerPrefs.GetInt(KEY_CAR,    0), 0, carTypes.Length    - 1);
            weaponIndex = Mathf.Clamp(PlayerPrefs.GetInt(KEY_WEAPON, 0), 0, weaponTypes.Length - 1);

            // Find preview on this GameObject or children if not assigned in Inspector
            if (preview == null)
                preview = GetComponentInChildren<CharacterSelectPreview>();

            if (playerLabelText != null)
                playerLabelText.text = $"PLAYER {playerIdx + 1}";

            statsMode      = StatsMode.Weapon;
            controllerStep = ControllerStep.SelectingWeapon;
            isReady        = false;
            isControllerMode = !isKeyboard;

            bool showMouseButtons = isKeyboard;
            if (backButton         != null) backButton.SetActive(showMouseButtons);
            if (mapSelectionButton != null) mapSelectionButton.SetActive(showMouseButtons);
            if (arrowButtons       != null)
                foreach (var btn in arrowButtons)
                    if (btn != null) btn.SetActive(showMouseButtons);

            RefreshAll();

            // Pass arrays and player index to preview
            if (preview != null)
            {
                // Get RawImage on THIS instance — not the prefab's serialized reference
                var rawImage = GetComponentInChildren<UnityEngine.UI.RawImage>();
                preview.SetPreviewDisplay(rawImage);
                preview.SetAssets(carTypes, weaponTypes);
                preview.SetPlayerIndex(playerIdx);
                preview.UpdatePreview(carIndex, weaponIndex);
            }

            if (readyOverlay != null) readyOverlay.SetActive(false);

            StartCoroutine(EnableInputWhenReleased());
        }

        private IEnumerator EnableInputWhenReleased()
        {
            if (assignedGamepad != null)
            {
                while (assignedGamepad.buttonSouth.isPressed || assignedGamepad.buttonEast.isPressed  ||
                       assignedGamepad.buttonNorth.isPressed || assignedGamepad.buttonWest.isPressed)
                    yield return null;
            }
            yield return null;
            inputEnabled = true;
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
                HandleSouthPressed();
            if (Input.GetKeyDown(KeyCode.Escape))
                HandleEastPressed();
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                if (controllerStep == ControllerStep.SelectingWeapon) WeaponPrev();
                else if (controllerStep == ControllerStep.SelectingCar) CarPrev();
            }
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                if (controllerStep == ControllerStep.SelectingWeapon) WeaponNext();
                else if (controllerStep == ControllerStep.SelectingCar) CarNext();
            }
        }

        // ═══════════════════════════════════════════════
        //  GAMEPAD INPUT
        // ═══════════════════════════════════════════════

        private void HandleGamepadInput()
        {
            // Never fall back to Gamepad.current — only respond to the assigned device
            Gamepad pad = assignedGamepad;
            if (pad == null) return;

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

            if (pad.buttonSouth.wasPressedThisFrame) HandleSouthPressed();
            if (pad.buttonEast.wasPressedThisFrame)  HandleEastPressed();
        }

        // ═══════════════════════════════════════════════
        //  SOUTH / EAST
        // ═══════════════════════════════════════════════

        private void HandleSouthPressed()
        {
            switch (controllerStep)
            {
                case ControllerStep.SelectingWeapon:
                    controllerStep = ControllerStep.SelectingCar;
                    statsMode      = StatsMode.Car;
                    RefreshStats();
                    break;
                case ControllerStep.SelectingCar:
                    controllerStep = ControllerStep.Ready;
                    isReady        = true;
                    Save();
                    if (readyOverlay != null) readyOverlay.SetActive(true);
                    if (AudioManager.instance != null && FMODEvents.instance != null)
                        AudioManager.instance.PlayOneShot(FMODEvents.instance.readyup, transform.position);
                    OnPlayerReady?.Invoke(playerIndex);
                    break;
            }
            UpdateHighlights();
            UpdateInstructionText();
        }

        private void HandleEastPressed()
        {
            switch (controllerStep)
            {
                case ControllerStep.SelectingCar:
                    controllerStep = ControllerStep.SelectingWeapon;
                    statsMode      = StatsMode.Weapon;
                    RefreshStats();
                    break;
                case ControllerStep.SelectingWeapon:
                    if (isPlayerOne) OnPlayerOneBack?.Invoke();
                    break;
                case ControllerStep.Ready:
                    controllerStep = ControllerStep.SelectingCar;
                    statsMode      = StatsMode.Car;
                    isReady        = false;
                    if (readyOverlay != null) readyOverlay.SetActive(false);
                    RefreshStats();
                    OnPlayerUnready?.Invoke(playerIndex);
                    break;
            }
            UpdateHighlights();
            UpdateInstructionText();
        }

        // ═══════════════════════════════════════════════
        //  INSTRUCTION TEXT — matches CharacterSelectUI
        // ═══════════════════════════════════════════════

        private void UpdateInstructionText()
        {
            if (instructionText != null)
            {
                switch (controllerStep)
                {
                    case ControllerStep.SelectingWeapon:
                        instructionText.text = "CHOOSE WEAPON";
                        break;
                    case ControllerStep.SelectingCar:
                        instructionText.text = "CHOOSE CAR";
                        break;
                    case ControllerStep.Ready:
                        instructionText.text = "READY!";
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
        //  HIGHLIGHTS — matches CharacterSelectUI
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
            preview?.UpdatePreview(carIndex, weaponIndex);
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
        //  SAVE — same keys as CharacterSelectUI
        // ═══════════════════════════════════════════════

        private void Save()
        {
            PlayerLoadout.SetCar(playerIndex,    carTypes[carIndex]);
            PlayerLoadout.SetWeapon(playerIndex, weaponTypes[weaponIndex]);
        }

        // ═══════════════════════════════════════════════
        //  GETTERS
        // ═══════════════════════════════════════════════

        public int              GetCarIndex()       => carIndex;
        public int              GetWeaponIndex()    => weaponIndex;
        public CarStats         GetSelectedCar()    => carTypes[carIndex];
        public ProjectileObject GetSelectedWeapon() => weaponTypes[weaponIndex];
    }
}