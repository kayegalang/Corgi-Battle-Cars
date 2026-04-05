using _Player.Scripts;
using UnityEngine;
using _Audio.scripts;
using UnityEngine.InputSystem;
using TMPro;
using FMODUnity;

namespace _UI.Scripts
{
    public class StartScreenController : MonoBehaviour
    {
        [SerializeField] private GameObject[] scenePanels;
        [SerializeField] private GameObject   startScreen;
        [SerializeField] private GameObject   mainMenu;

        [Header("Start Screen Text")]
        [SerializeField] private TextMeshProUGUI startScreenText;
        [SerializeField] private string keyboardPrompt   = "PRESS ANYWHERE TO START";
        [SerializeField] private string controllerPrompt = "PRESS ANY BUTTON TO START";
        
        [Header("FMOD Audio")]
        [SerializeField] private EventReference clicksound;

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
                waitingForInput    = true;
                UpdateStartScreenText();
            }

            DeactivatePanels();
        }

        private void Update()
        {
            if (!waitingForInput) return;

            // Check each connected gamepad individually so we know WHICH one pressed
            foreach (Gamepad gamepad in Gamepad.all)
            {
                if (WasAnyButtonPressed(gamepad))
                {
                    DismissStartScreen(isController: true, device: gamepad);
                    return;
                }
            }

            // Check for mouse click
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                DismissStartScreen(isController: false, device: Keyboard.current);
            }
        }

        private void UpdateStartScreenText()
        {
            if (startScreenText == null) return;
            startScreenText.text = Gamepad.all.Count > 0 ? controllerPrompt : keyboardPrompt;
        }

        private bool WasAnyButtonPressed(Gamepad gamepad)
        {
            AudioManager.instance.PlayOneShot(clicksound, this.transform.position);
            
            return gamepad.buttonSouth.wasPressedThisFrame ||
                   gamepad.buttonNorth.wasPressedThisFrame ||
                   gamepad.buttonEast.wasPressedThisFrame  ||
                   gamepad.buttonWest.wasPressedThisFrame  ||
                   gamepad.startButton.wasPressedThisFrame ||
                   gamepad.selectButton.wasPressedThisFrame;
        }

        private void DismissStartScreen(bool isController, InputDevice device)
        {
            waitingForInput = false;

            if (PlayerOneInputTracker.instance != null)
            {
                PlayerOneInputTracker.instance.SetPlayerOneUsingController(isController);
                PlayerOneInputTracker.instance.SetPlayerOneDevice(device); // ← save specific device
            }

            StartCoroutine(TransitionToMainMenu(isController, device));
        }

        private System.Collections.IEnumerator TransitionToMainMenu(bool isController, InputDevice device)
        {
            if (isController && device is Gamepad gp)
            {
                while (IsAnyButtonHeld(gp))
                    yield return null;
            }

            yield return null;

            mainMenu.SetActive(true);
            startScreen.SetActive(false);
        }

        private bool IsAnyButtonHeld(Gamepad gamepad)
        {
            return gamepad.buttonSouth.isPressed ||
                   gamepad.buttonNorth.isPressed ||
                   gamepad.buttonEast.isPressed  ||
                   gamepad.buttonWest.isPressed  ||
                   gamepad.startButton.isPressed ||
                   gamepad.selectButton.isPressed;
        }

        private void DeactivatePanels()
        {
            foreach (GameObject panel in scenePanels)
                panel.SetActive(false);
        }
    }
}