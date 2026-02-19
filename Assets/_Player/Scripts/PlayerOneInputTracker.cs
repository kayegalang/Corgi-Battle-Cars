using UnityEngine;

namespace _Player.Scripts
{
    /// <summary>
    /// Tracks which input method PlayerOne is using (controller vs keyboard).
    /// Persists across scenes so UI can adapt (auto-select buttons for controller,
    /// don't auto-select for keyboard).
    /// </summary>
    public class PlayerOneInputTracker : MonoBehaviour
    {
        public static PlayerOneInputTracker instance;

        private bool playerOneIsUsingController = false;

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

        public void SetPlayerOneUsingController(bool isController)
        {
            playerOneIsUsingController = isController;
        }

        public bool IsPlayerOneUsingController()
        {
            return playerOneIsUsingController;
        }

        public void Reset()
        {
            playerOneIsUsingController = false;
        }
    }
}