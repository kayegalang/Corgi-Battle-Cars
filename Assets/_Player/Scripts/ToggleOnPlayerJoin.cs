    using UnityEngine;
    using UnityEngine.InputSystem;

    namespace _Player.Scripts
    {
        public class ToggleOnPlayerJoin : MonoBehaviour
        {
            private PlayerInputManager playerInputManager;

            private void Awake()
            {
                playerInputManager = FindFirstObjectByType<PlayerInputManager>();
            }

            private void OnEnable()
            {
                playerInputManager.onPlayerJoined += ToggleThis;
            }

            private void OnDisable()
            {
                playerInputManager.onPlayerJoined -= ToggleThis;
            }

            private void ToggleThis(PlayerInput player)
            {
                this.gameObject.SetActive(false);
            }
        }



    }