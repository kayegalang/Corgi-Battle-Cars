using _Player.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace _UI.Scripts
{
    public class StartScreenController : MonoBehaviour
    {
        [SerializeField] private GameObject[] scenePanels;
        [SerializeField] private GameObject startScreen;
        [SerializeField] private GameObject mainMenu;
        
        [Header("Start Screen Text")]
        [SerializeField] private TextMeshProUGUI startScreenText;
        [SerializeField] private string keyboardPrompt = "PRESS ANYWHERE TO START";
        [SerializeField] private string controllerPrompt = "PRESS ANY BUTTON TO START";

        private static bool hasSeenStartScreen = false;
        private bool waitingForInput = false;
        
        void Start()
        {
            bool showStartScreen = !hasSeenStartScreen;
            
            startScreen.SetActive(showStartScreen);
            mainMenu.SetActive(!showStartScreen);
            
            if (showStartScreen)
            {
                hasSeenStartScreen = true;
                waitingForInput = true;
                UpdateStartScreenText();
            }
            
            DeactivatePanels();
        }

        private void OnDestroy()
        {
            // Reset for next editor play session
            hasSeenStartScreen = false;
        }

        private void Update()
        {
            if (!waitingForInput) return;

            // Check for controller button press
            if (Gamepad.current != null && WasAnyGamepadButtonPressed())
            {
                DismissStartScreen(true);
                return;
            }

            // Check for mouse click anywhere on screen
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                DismissStartScreen(false);
            }
        }

        private void UpdateStartScreenText()
        {
            if (startScreenText == null) return;

            bool hasController = Gamepad.current != null;
            startScreenText.text = hasController ? controllerPrompt : keyboardPrompt;
        }

        private bool WasAnyGamepadButtonPressed()
        {
            Gamepad gamepad = Gamepad.current;
            
            return gamepad.buttonSouth.wasPressedThisFrame ||
                   gamepad.buttonNorth.wasPressedThisFrame ||
                   gamepad.buttonEast.wasPressedThisFrame ||
                   gamepad.buttonWest.wasPressedThisFrame ||
                   gamepad.startButton.wasPressedThisFrame ||
                   gamepad.selectButton.wasPressedThisFrame ||
                   gamepad.leftShoulder.wasPressedThisFrame ||
                   gamepad.rightShoulder.wasPressedThisFrame ||
                   gamepad.leftTrigger.wasPressedThisFrame ||
                   gamepad.rightTrigger.wasPressedThisFrame;
        }

        private void DismissStartScreen(bool isController)
        {
            waitingForInput = false;

            if (PlayerOneInputTracker.instance != null)
            {
                PlayerOneInputTracker.instance.SetPlayerOneUsingController(isController);
            }

            StartCoroutine(TransitionToMainMenu(isController));
        }

        private System.Collections.IEnumerator TransitionToMainMenu(bool isController)
        {
            // If controller, wait until all buttons are released
            // This prevents any lingering input from affecting the menu
            if (isController && Gamepad.current != null)
            {
                while (IsAnyGamepadButtonHeld())
                {
                    yield return null;
                }
            }
            
            // Wait one extra frame for good measure
            yield return null;
            
            // Show main menu
            mainMenu.SetActive(true);
            startScreen.SetActive(false);
        }

        private bool IsAnyGamepadButtonHeld()
        {
            Gamepad gamepad = Gamepad.current;
            if (gamepad == null) return false;

            return gamepad.buttonSouth.isPressed ||
                   gamepad.buttonNorth.isPressed ||
                   gamepad.buttonEast.isPressed ||
                   gamepad.buttonWest.isPressed ||
                   gamepad.startButton.isPressed ||
                   gamepad.selectButton.isPressed ||
                   gamepad.leftShoulder.isPressed ||
                   gamepad.rightShoulder.isPressed ||
                   gamepad.leftTrigger.isPressed ||
                   gamepad.rightTrigger.isPressed;
        }

        private void DeactivatePanels()
        {
            foreach (GameObject panel in scenePanels)
            {
                panel.SetActive(false);
            }
        }
    }
}