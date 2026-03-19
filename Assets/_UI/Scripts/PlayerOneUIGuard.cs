using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using _Player.Scripts;

namespace _UI.Scripts
{
    /// <summary>
    /// Ensures only Player 1's device can drive UI in menu screens.
    /// Disables all non-Player-1 gamepads while in menus so they can't
    /// accidentally navigate buttons. Re-enables them when gameplay starts
    /// or when the join screen is showing.
    ///
    /// Add this to the same persistent GameObject as PlayerOneInputTracker.
    /// </summary>
    public class PlayerOneUIGuard : MonoBehaviour
    {
        [Tooltip("Scene names where only Player 1 should control UI")]
        [SerializeField] private string[] menuSceneNames = { "MainMenu" };

        private bool isInMenuScene    = false;
        private bool allowAllDevices  = false; // set true during join screen

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            CheckCurrentScene();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            EnableAllGamepads();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            allowAllDevices = false; // reset on every scene load
            CheckCurrentScene();
        }

        private void CheckCurrentScene()
        {
            string current = SceneManager.GetActiveScene().name;
            isInMenuScene  = System.Array.Exists(menuSceneNames, s => s == current);

            if (isInMenuScene)
                ApplyMenuRestriction();
            else
                EnableAllGamepads();
        }

        private void Update()
        {
            if (!isInMenuScene) return;
            ApplyMenuRestriction();
        }

        // ═══════════════════════════════════════════════
        //  PUBLIC — called by PlayerJoinScreen
        // ═══════════════════════════════════════════════

        /// <summary>
        /// Call with true when the join screen opens (all gamepads need to join).
        /// Call with false when the join screen closes (restrict back to Player 1).
        /// </summary>
        public void SetAllowAllDevices(bool allow)
        {
            allowAllDevices = allow;

            if (allow)
                EnableAllGamepads();
            else
                ApplyMenuRestriction();
        }

        // ═══════════════════════════════════════════════
        //  RESTRICTION LOGIC
        // ═══════════════════════════════════════════════

        private void ApplyMenuRestriction()
        {
            // If join screen is open allow everything
            if (allowAllDevices)
            {
                EnableAllGamepads();
                return;
            }

            InputDevice playerOneDevice = PlayerOneInputTracker.instance?.GetPlayerOneDevice();

            foreach (Gamepad gamepad in Gamepad.all)
            {
                // If Player 1's device is unknown yet, allow all
                if (playerOneDevice == null)
                {
                    if (!gamepad.enabled) InputSystem.EnableDevice(gamepad);
                    continue;
                }

                if (gamepad == playerOneDevice)
                {
                    if (!gamepad.enabled) InputSystem.EnableDevice(gamepad);
                }
                else
                {
                    if (gamepad.enabled) InputSystem.DisableDevice(gamepad);
                }
            }
        }

        private void EnableAllGamepads()
        {
            foreach (Gamepad gamepad in Gamepad.all)
                if (!gamepad.enabled) InputSystem.EnableDevice(gamepad);
        }
    }
}