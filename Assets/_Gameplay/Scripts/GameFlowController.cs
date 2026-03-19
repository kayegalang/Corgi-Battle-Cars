using _Cars.Scripts;
using _UI.Scripts;
using _PowerUps.Scripts;
using UnityEngine;

namespace _Gameplay.Scripts
{
    public class GameFlowController : MonoBehaviour
    {
        public static GameFlowController instance;

        private bool isGameEnded      = false;
        private bool isGameplayActive = false;

        // ═══════════════════════════════════════════════
        //  SINGLETON
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            InitializeSingleton();
        }

        private void InitializeSingleton()
        {
            if (instance == null)
                instance = this;
            else if (instance != this)
                Destroy(this);
        }

        // ═══════════════════════════════════════════════
        //  START
        // ═══════════════════════════════════════════════

        public void StartGame()
        {
            Debug.Log($"[{nameof(GameFlowController)}] Game starting");
            isGameEnded = false;
        }

        // ═══════════════════════════════════════════════
        //  ENABLE GAMEPLAY
        // ═══════════════════════════════════════════════

        public void EnableGameplayForAllPlayers()
        {
            Debug.Log($"[{nameof(GameFlowController)}] Enabling gameplay for all players");

            isGameplayActive = true;

            EnableAllShooters();
            EnableAllPlayerUI();
            StartPowerUpSpawner();
        }

        private void EnableAllShooters()
        {
            foreach (var shooter in FindObjectsByType<CarShooter>(FindObjectsSortMode.None))
                shooter.EnableGameplay();
        }

        private void EnableAllPlayerUI()
        {
            foreach (var playerUI in FindObjectsByType<PlayerUIManager>(FindObjectsSortMode.None))
                playerUI.EnableGameplay();
        }

        private void StartPowerUpSpawner()
        {
            PowerUpSpawner spawner = FindFirstObjectByType<PowerUpSpawner>();

            if (spawner != null)
            {
                spawner.StartSpawning();
                Debug.Log($"[{nameof(GameFlowController)}] Started PowerUpSpawner! 🎁");
            }
            else
            {
                Debug.LogWarning($"[{nameof(GameFlowController)}] PowerUpSpawner not found in scene!");
            }
        }

        // ═══════════════════════════════════════════════
        //  END GAME
        // ═══════════════════════════════════════════════

        public void EndGame()
        {
            if (isGameEnded)
            {
                Debug.LogWarning($"[{nameof(GameFlowController)}] Game already ended");
                return;
            }

            Debug.Log($"[{nameof(GameFlowController)}] Game ending");

            isGameEnded = true;

            StopPowerUpSpawner();
            ShowCursor();
            DisableAllPlayerControls();
            DisableAllPlayerUI();

            // Hide power-up UI for all players
            foreach (var handler in FindObjectsByType<PowerUpHandler>(FindObjectsSortMode.None))
                handler.OnGameEnd();

            // Hide death screen for any dead players
            foreach (var dsm in FindObjectsByType<DeathSpectateManager>(FindObjectsSortMode.None))
                dsm.ForceHide();

            ShowEndScreen();
            DisplayFinalScoreboard();
        }

        private void ShowCursor()
        {
            Cursor.visible   = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void DisableAllPlayerControls()
        {
            foreach (var controller in FindObjectsByType<CarController>(FindObjectsSortMode.None))
                controller.enabled = false;

            DisableAllShooters();
        }

        private void DisableAllShooters()
        {
            foreach (var shooter in FindObjectsByType<CarShooter>(FindObjectsSortMode.None))
            {
                // DisableGameplay hides the reticle before we kill the component
                shooter.DisableGameplay();
                shooter.enabled = false;
            }
        }

        private void DisableAllPlayerUI()
        {
            foreach (var playerUI in FindObjectsByType<PlayerUIManager>(FindObjectsSortMode.None))
                playerUI.DisableGameplay();
        }

        private void StopPowerUpSpawner()
        {
            PowerUpSpawner spawner = FindFirstObjectByType<PowerUpSpawner>();

            if (spawner != null)
            {
                spawner.StopSpawning();
                spawner.ClearAllPowerUps();
                Debug.Log($"[{nameof(GameFlowController)}] Stopped and cleared PowerUpSpawner!");
            }
        }

        private void ShowEndScreen()
        {
            EndGameManager endGameManager = FindFirstObjectByType<EndGameManager>();

            if (endGameManager != null)
                endGameManager.OnGameEnd();
            else
                Debug.LogWarning($"[{nameof(GameFlowController)}] EndGameManager not found!");
        }

        private void DisplayFinalScoreboard()
        {
            if (PointsManager.instance != null)
                PointsManager.instance.DisplayFinalScoreboard();
            else
                Debug.LogWarning($"[{nameof(GameFlowController)}] PointsManager instance not found!");
        }

        // ═══════════════════════════════════════════════
        //  GETTERS / RESET
        // ═══════════════════════════════════════════════

        public bool IsGameEnded()      => isGameEnded;
        public bool IsGameplayActive() => isGameplayActive;

        public void ResetGameState()
        {
            isGameEnded      = false;
            isGameplayActive = false;
        }
    }
}