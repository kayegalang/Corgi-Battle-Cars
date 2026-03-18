using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Events;
using _Gameplay.Scripts;
using _Player.Scripts;

namespace _UI.Scripts
{
    public class MapSelector : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button[] mapButtons;

        [Header("Button Colors")]
        [SerializeField] private Color normalColor      = Color.white;
        [SerializeField] private Color highlightedColor = Color.gray;

        [Header("Settings")]
        [SerializeField] private string comingSoonButtonName = "Coming Soon";

        [Header("Buttons — hidden when using controller")]
        [SerializeField] private GameObject backButton;

        [Header("Navigation")]
        [Tooltip("Panel to return to when going back")]
        [SerializeField] private GameObject previousPanel;

        [Header("Events")]
        public UnityEvent<string> onMapSelected;
        public UnityEvent onStartGameClicked;

        // ─── state ───────────────────────────────────────────
        private Button selectedButton;
        private bool   isControllerMode = false;

        private enum ControllerStep { SelectingMap, ConfirmingStart }
        private ControllerStep controllerStep = ControllerStep.SelectingMap;

        // ═══════════════════════════════════════════════
        //  UNITY LIFECYCLE
        // ═══════════════════════════════════════════════

        private void OnEnable()
        {
            selectedButton   = null;
            controllerStep   = ControllerStep.SelectingMap;
            isControllerMode = false;

            // Hide buttons immediately before the player sees them
            bool controllerConnected =
                (PlayerOneInputTracker.instance != null && PlayerOneInputTracker.instance.IsPlayerOneUsingController())
                || Gamepad.current != null;

            if (backButton != null)
                backButton.SetActive(!controllerConnected);

            if (startGameButton != null)
                startGameButton.interactable = false;

            InitializeButtons();
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
            bool usingController = false;

            if (PlayerOneInputTracker.instance != null)
                usingController = PlayerOneInputTracker.instance.IsPlayerOneUsingController();

            if (!usingController && Gamepad.current != null)
                usingController = true;

            SetControllerMode(usingController);
        }

        private void SetControllerMode(bool controller)
        {
            isControllerMode = controller;

            if (backButton != null)
                backButton.SetActive(!controller);
        }

        // ═══════════════════════════════════════════════
        //  CONTROLLER INPUT
        //  South is handled automatically by EventSystem
        //  We only need East for going back / reselecting
        // ═══════════════════════════════════════════════

        private void HandleControllerInput()
        {
            Gamepad pad = Gamepad.current;
            if (pad == null) return;

            if (pad.buttonEast.wasPressedThisFrame)
            {
                if (controllerStep == ControllerStep.ConfirmingStart)
                {
                    // Go back to map selection
                    controllerStep = ControllerStep.SelectingMap;
                    DeselectButton();

                    if (mapButtons.Length > 0)
                        EventSystem.current.SetSelectedGameObject(mapButtons[0].gameObject);
                }
                else
                {
                    GoBack();
                }
            }
        }

        // ═══════════════════════════════════════════════
        //  BUTTON SETUP
        // ═══════════════════════════════════════════════

        private void InitializeButtons()
        {
            if (startGameButton != null)
                startGameButton.interactable = false;

            foreach (Button button in mapButtons)
            {
                Button btn = button;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnMapButtonClicked(btn));
            }
        }

        // ═══════════════════════════════════════════════
        //  MAP SELECTION
        // ═══════════════════════════════════════════════

        private void OnMapButtonClicked(Button btn)
        {
            // Clicking the already-selected map deselects it
            if (selectedButton == btn)
            {
                DeselectButton();
                controllerStep = ControllerStep.SelectingMap;
                return;
            }

            if (selectedButton != null)
                SetButtonColor(selectedButton, normalColor);

            selectedButton = btn;
            SetButtonColor(selectedButton, highlightedColor);

            bool isValidMap = selectedButton.name != comingSoonButtonName;

            if (startGameButton != null)
                startGameButton.interactable = isValidMap;

            if (isValidMap)
            {
                onMapSelected?.Invoke(btn.name);

                if (GameplayManager.instance != null)
                    GameplayManager.instance.SetMap(btn.name);

                if (isControllerMode)
                {
                    controllerStep = ControllerStep.ConfirmingStart;
                    EventSystem.current.SetSelectedGameObject(startGameButton.gameObject);
                }
            }
        }

        private void DeselectButton()
        {
            if (selectedButton != null)
                SetButtonColor(selectedButton, normalColor);

            selectedButton = null;

            if (startGameButton != null)
                startGameButton.interactable = false;
        }

        private void SetButtonColor(Button button, Color color)
        {
            if (button == null) return;
            Image image = button.GetComponent<Image>();
            if (image != null) image.color = color;
        }

        // ═══════════════════════════════════════════════
        //  ACTIONS
        // ═══════════════════════════════════════════════

        public void OnStartGameButtonClicked()
        {
            onStartGameClicked?.Invoke();

            if (GameplayManager.instance != null)
                GameplayManager.instance.StartGame();
        }

        public void GoBack()
        {
            DeselectButton();
            controllerStep = ControllerStep.SelectingMap;

            if (previousPanel != null)
            {
                previousPanel.SetActive(true);
                gameObject.SetActive(false);
            }
        }

        public void ResetSelection()
        {
            DeselectButton();
            controllerStep = ControllerStep.SelectingMap;
        }
    }
}