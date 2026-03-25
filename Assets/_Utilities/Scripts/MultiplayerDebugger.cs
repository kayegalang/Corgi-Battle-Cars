using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using _Gameplay.Scripts;
using _UI.Scripts;
using _Player.Scripts;

namespace _Utilities.Scripts
{
    /// <summary>
    /// Debug tool for testing multiplayer flow without physical controllers.
    /// Add to any GameObject in the MainMenu scene.
    /// 
    /// Controls:
    ///   F2 = Simulate 2 players joining
    ///   F3 = Simulate 3 players joining
    ///   F4 = Simulate 4 players joining
    ///   F5 = Skip character select (auto-confirm all players)
    ///   F6 = Skip map select (auto-pick first map)
    /// </summary>
    public class MultiplayerDebugger : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Only runs in Unity Editor or Development Builds")]
        [SerializeField] private bool onlyInEditor = true;

        [Header("Auto-skip Settings")]
        [SerializeField] private string defaultMapName = "Park Map";

        private void Update()
        {
#if !UNITY_EDITOR
            if (onlyInEditor) return;
#endif
            if (Input.GetKeyDown(KeyCode.F2)) SimulateMultiplayer(2);
            if (Input.GetKeyDown(KeyCode.F3)) SimulateMultiplayer(3);
            if (Input.GetKeyDown(KeyCode.F4)) SimulateMultiplayer(4);
            if (Input.GetKeyDown(KeyCode.F5)) SkipCharacterSelect();
            if (Input.GetKeyDown(KeyCode.F6)) SkipMapSelect();
        }

        // ═══════════════════════════════════════════════
        //  SIMULATE MULTIPLAYER JOIN
        // ═══════════════════════════════════════════════

        private void SimulateMultiplayer(int playerCount)
        {
            Debug.Log($"<color=cyan>[MultiplayerDebugger] Simulating {playerCount} player join...</color>");

            // Set game mode
            if (GameplayManager.instance != null)
            {
                GameplayManager.instance.SetGameMode(GameMode.Multiplayer);
                GameplayManager.instance.SetMultiplayerPlayerCount(playerCount);
            }

            // Set Player 1 as keyboard so no controller needed
            if (PlayerOneInputTracker.instance != null)
            {
                PlayerOneInputTracker.instance.SetPlayerOneUsingController(false);
                PlayerOneInputTracker.instance.SetPlayerOneDevice(Keyboard.current);
            }

            // Allow all devices
            var guard = FindFirstObjectByType<PlayerOneUIGuard>();
            guard?.SetAllowAllDevices(true);

            // Find join screen and simulate it
            PlayerJoinScreen joinScreen = FindFirstObjectByType<PlayerJoinScreen>(FindObjectsInactive.Include);
            if (joinScreen != null)
            {
                StartCoroutine(SimulateJoinFlow(joinScreen, playerCount));
            }
            else
            {
                // If no join screen, go straight to character select
                SimulateCharacterSelect(playerCount);
            }
        }

        private IEnumerator SimulateJoinFlow(PlayerJoinScreen joinScreen, int playerCount)
        {
            joinScreen.ShowJoinScreen(playerCount);
            yield return new WaitForSeconds(1f);

            // Simulate all players joining by calling OnAllPlayersJoined directly
            // via GameplayManager since we can't easily fake PlayerInput joining
            if (GameplayManager.instance != null)
                GameplayManager.instance.SetMultiplayerPlayerCount(playerCount);

            // Show character select directly
            yield return new WaitForSeconds(0.5f);
            SimulateCharacterSelect(playerCount);
        }

        private void SimulateCharacterSelect(int playerCount)
        {
            var charSelect = FindFirstObjectByType<MultiplayerCharacterSelectManager>(FindObjectsInactive.Include);
            if (charSelect != null)
            {
                charSelect.gameObject.SetActive(true);
                Debug.Log($"<color=cyan>[MultiplayerDebugger] Character select opened for {playerCount} players</color>");
            }
            else
            {
                Debug.LogWarning("[MultiplayerDebugger] MultiplayerCharacterSelectManager not found!");
            }
        }

        // ═══════════════════════════════════════════════
        //  SKIP CHARACTER SELECT
        // ═══════════════════════════════════════════════

        private void SkipCharacterSelect()
        {
            Debug.Log("<color=cyan>[MultiplayerDebugger] Skipping character select...</color>");

            // Auto-confirm all player panels
            var panels = FindObjectsByType<PlayerCharacterSelectPanel>(FindObjectsSortMode.None);

            if (panels.Length == 0)
            {
                Debug.LogWarning("[MultiplayerDebugger] No PlayerCharacterSelectPanels found! Are you on the character select screen?");
                return;
            }

            foreach (var panel in panels)
            {
                // Simulate pressing confirm twice (weapon → car → ready)
                panel.SendMessage("HandleConfirm", SendMessageOptions.DontRequireReceiver);
                panel.SendMessage("HandleConfirm", SendMessageOptions.DontRequireReceiver);
            }

            Debug.Log($"<color=cyan>[MultiplayerDebugger] Auto-confirmed {panels.Length} panels</color>");
        }

        // ═══════════════════════════════════════════════
        //  SKIP MAP SELECT
        // ═══════════════════════════════════════════════

        private void SkipMapSelect()
        {
            Debug.Log("<color=cyan>[MultiplayerDebugger] Skipping map select...</color>");

            if (GameplayManager.instance == null)
            {
                Debug.LogWarning("[MultiplayerDebugger] GameplayManager not found!");
                return;
            }

            GameplayManager.instance.SetMap(defaultMapName);
            GameplayManager.instance.StartGame();

            Debug.Log($"<color=cyan>[MultiplayerDebugger] Starting game on {defaultMapName}</color>");
        }

        // ═══════════════════════════════════════════════
        //  ON SCREEN HINT
        // ═══════════════════════════════════════════════

        private void OnGUI()
        {
#if !UNITY_EDITOR
            if (onlyInEditor) return;
#endif
            GUIStyle style = new GUIStyle();
            style.normal.textColor = new Color(0f, 1f, 1f, 0.8f);
            style.fontSize         = 14;

            GUI.Label(new Rect(10, 10, 400, 200),
                "MULTIPLAYER DEBUGGER\n" +
                "F2 = Simulate 2 players\n" +
                "F3 = Simulate 3 players\n" +
                "F4 = Simulate 4 players\n" +
                "F5 = Skip character select\n" +
                "F6 = Skip map select (auto-start)",
                style);
        }
    }
}