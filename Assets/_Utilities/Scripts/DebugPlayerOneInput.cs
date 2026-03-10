using UnityEngine;
using UnityEngine.InputSystem;
using _Player.Scripts;

namespace _Utilities.Scripts
{
    /// <summary>
    /// TEMPORARY DEBUG SCRIPT
    /// Attach to any GameObject in game scene to verify PlayerOne control scheme.
    /// Check console for detailed info.
    /// </summary>
    public class DebugPlayerOneInput : MonoBehaviour
    {
        void Start()
        {
            Invoke(nameof(CheckPlayerOne), 1f); // Wait 1 second for spawn
        }
        
        void CheckPlayerOne()
        {
            Debug.Log("========================================");
            Debug.Log("DEBUG PLAYERONE INPUT");
            Debug.Log("========================================");
            
            // Check tracker
            if (PlayerOneInputTracker.instance == null)
            {
                Debug.LogError("❌ PlayerOneInputTracker is NULL!");
            }
            else
            {
                bool shouldUseController = PlayerOneInputTracker.instance.IsPlayerOneUsingController();
                Debug.Log($"✓ Tracker says PlayerOne should use: {(shouldUseController ? "CONTROLLER" : "KEYBOARD")}");
            }
            
            // Find PlayerOne
            GameObject playerOne = GameObject.Find("PlayerOne");
            if (playerOne == null)
            {
                Debug.LogError("❌ PlayerOne GameObject not found!");
                Debug.Log("Searching for any player...");
                
                // Try to find any player
                PlayerInput[] allPlayers = FindObjectsByType<PlayerInput>(FindObjectsSortMode.None);
                Debug.Log($"Found {allPlayers.Length} PlayerInput objects:");
                foreach (var p in allPlayers)
                {
                    Debug.Log($"  - {p.gameObject.name}, Tag: {p.gameObject.tag}, Scheme: {p.currentControlScheme}");
                }
            }
            else
            {
                Debug.Log($"✓ Found PlayerOne GameObject");
                
                PlayerInput playerInput = playerOne.GetComponent<PlayerInput>();
                if (playerInput == null)
                {
                    Debug.LogError("❌ PlayerOne has no PlayerInput component!");
                }
                else
                {
                    Debug.Log($"✓ PlayerOne PlayerInput exists");
                    Debug.Log($"✓ Current Control Scheme: {playerInput.currentControlScheme}");
                    Debug.Log($"✓ Devices: {string.Join(", ", System.Array.ConvertAll(playerInput.devices.ToArray(), d => d.displayName))}");
                    
                    // Check what's expected
                    bool shouldUseController = PlayerOneInputTracker.instance != null && 
                                             PlayerOneInputTracker.instance.IsPlayerOneUsingController();
                    string expectedScheme = shouldUseController ? "Controller" : "Keyboard";
                    
                    if (playerInput.currentControlScheme == expectedScheme)
                    {
                        Debug.Log($"✓✓✓ CORRECT! PlayerOne is using {expectedScheme}");
                    }
                    else
                    {
                        Debug.LogError($"❌❌❌ WRONG! Expected {expectedScheme} but got {playerInput.currentControlScheme}");
                    }
                }
            }
            
            Debug.Log("========================================");
        }
    }
}