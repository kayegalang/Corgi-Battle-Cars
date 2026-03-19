using UnityEngine;
using UnityEngine.InputSystem;

namespace _Player.Scripts
{
    /// <summary>
    /// Tracks which input device PlayerOne is using.
    /// Persists across scenes so UI can restrict navigation to Player 1 only.
    /// </summary>
    public class PlayerOneInputTracker : MonoBehaviour
    {
        public static PlayerOneInputTracker instance;

        private bool        playerOneIsUsingController = false;
        private InputDevice playerOneDevice            = null;

        private void Awake()
        {
            InitializeSingleton();
        }

        private void InitializeSingleton()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // ── Setters ──────────────────────────────────────────

        public void SetPlayerOneUsingController(bool isController)
        {
            playerOneIsUsingController = isController;
        }

        /// <summary>
        /// Store the exact device Player 1 used on the start screen.
        /// </summary>
        public void SetPlayerOneDevice(InputDevice device)
        {
            playerOneDevice = device;
        }

        // ── Getters ──────────────────────────────────────────

        public bool IsPlayerOneUsingController() => playerOneIsUsingController;

        /// <summary>
        /// Returns the specific device Player 1 used (gamepad or keyboard).
        /// Null until the start screen is dismissed.
        /// </summary>
        public InputDevice GetPlayerOneDevice() => playerOneDevice;

        /// <summary>
        /// Returns true if the given device belongs to Player 1.
        /// Use this to gate UI input on non-character-select screens.
        /// </summary>
        public bool IsPlayerOneDevice(InputDevice device)
        {
            if (playerOneDevice == null || device == null) return true; // permissive if unknown
            return device == playerOneDevice;
        }

        public void Reset()
        {
            playerOneIsUsingController = false;
            playerOneDevice            = null;
        }
    }
}