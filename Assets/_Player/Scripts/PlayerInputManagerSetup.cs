using UnityEngine;
using UnityEngine.InputSystem;

namespace _Player.Scripts
{
    /// <summary>
    /// Attach to PlayerInputManager GameObject.
    /// Disables joining by default so start screen button presses don't auto-join players.
    /// Joining is only enabled when you reach the actual join screen.
    /// </summary>
    [RequireComponent(typeof(PlayerInputManager))]
    public class PlayerInputManagerSetup : MonoBehaviour
    {
        private PlayerInputManager playerInputManager;

        private void Awake()
        {
            playerInputManager = GetComponent<PlayerInputManager>();
            
            // CRITICAL: Disable joining by default
            // This prevents the start screen A button from auto-joining players
            if (playerInputManager != null)
            {
                playerInputManager.DisableJoining();
                Debug.Log("[PlayerInputManagerSetup] Joining DISABLED by default");
            }
        }
    }
}