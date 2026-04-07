using System.Collections;
using _Cars.Scripts;
using _UI.Scripts;
using UnityEngine;

namespace _Gameplay.Scripts
{
    /// <summary>
    /// Drop this on an empty GameObject in your Trailer scene.
    /// Bypasses the MainMenu flow entirely — spawns 1 human player + 3 bots,
    /// starts gameplay, and never ends the game (no timer).
    ///
    /// SCENE SETUP:
    ///   - SpawnManager (with spawn points, player prefab, bot prefab assigned)
    ///   - GameFlowController
    ///   - PowerUpSpawner (optional)
    ///   - CountdownUI (optional — assign below if you want the 3-2-1 countdown)
    ///   - Do NOT put a MatchTimerManager in this scene (that would end the game)
    /// </summary>
    public class TrailerSceneBootstrapper : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("How many human players to spawn (1 recommended for trailer)")]
        [SerializeField] private int humanPlayerCount = 1;

        [Tooltip("Optional — assign if you want the 3-2-1 countdown before gameplay starts")]
        [SerializeField] private CountdownUI countdownUI;

        [Tooltip("Seconds to wait before starting if no CountdownUI is assigned")]
        [SerializeField] private float fallbackDelay = 1f;

        // ═══════════════════════════════════════════════
        //  STARTUP
        // ═══════════════════════════════════════════════

        private IEnumerator Start()
        {
            // Wait one frame so all Awake() calls finish
            yield return null;

            SpawnPlayers();
            yield return StartCoroutine(WaitForCountdown());
            StartGameplay();
        }

        // ═══════════════════════════════════════════════
        //  SPAWN
        // ═══════════════════════════════════════════════

        private void SpawnPlayers()
        {
            SpawnManager spawnManager = FindFirstObjectByType<SpawnManager>();

            if (spawnManager == null)
            {
                Debug.LogError("[TrailerSceneBootstrapper] SpawnManager not found in scene!");
                return;
            }

            // Register players manually since GameplayManager doesn't exist here
            if (PlayerRegistry.instance != null)
            {
                PlayerRegistry.instance.Clear();

                string[] playerTags = { "PlayerOne", "PlayerTwo", "PlayerThree", "PlayerFour" };
                for (int i = 0; i < humanPlayerCount; i++)
                    PlayerRegistry.instance.RegisterPlayer(playerTags[i]);

                int botStart = humanPlayerCount + 1;
                string[] botTags = { "BotOne", "BotTwo", "BotThree", "BotFour" };
                for (int i = 0; i < 4 - humanPlayerCount; i++)
                    PlayerRegistry.instance.RegisterPlayer(botTags[i]);
            }

            spawnManager.StartGame(humanPlayerCount);

            Debug.Log($"[TrailerSceneBootstrapper] Spawned {humanPlayerCount} human player(s) + {4 - humanPlayerCount} bot(s)");
        }

        // ═══════════════════════════════════════════════
        //  COUNTDOWN
        // ═══════════════════════════════════════════════

        private IEnumerator WaitForCountdown()
        {
            Time.timeScale = 0f;

            if (countdownUI != null)
            {
                yield return StartCoroutine(countdownUI.PlayCountdown());
            }
            else
            {
                yield return new WaitForSecondsRealtime(fallbackDelay);
            }

            Time.timeScale = 1f;
        }

        // ═══════════════════════════════════════════════
        //  START GAMEPLAY
        // ═══════════════════════════════════════════════

        private void StartGameplay()
        {
            if (GameFlowController.instance != null)
            {
                GameFlowController.instance.StartGame();
                GameFlowController.instance.EnableGameplayForAllPlayers();
                Debug.Log("[TrailerSceneBootstrapper] Gameplay enabled!");
            }
            else
            {
                // Fallback — manually enable all shooters and UI
                Debug.LogWarning("[TrailerSceneBootstrapper] GameFlowController not found — enabling shooters manually.");

                foreach (var shooter in FindObjectsByType<CarShooter>(FindObjectsSortMode.None))
                    shooter.EnableGameplay();

                foreach (var ui in FindObjectsByType<PlayerUIManager>(FindObjectsSortMode.None))
                    ui.EnableGameplay();
            }
        }
    }
}
