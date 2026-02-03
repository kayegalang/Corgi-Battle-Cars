using _Cars.Scripts;
using _UI.Scripts;
using UnityEngine;

namespace _Gameplay.Scripts
{
    public class GameFlowController : MonoBehaviour
    {
        public static GameFlowController instance;
        
        private bool isGameEnded = false;
        private bool isGameplayActive = false;
        
        private void Awake()
        {
            InitializeSingleton();
        }
        
        private void InitializeSingleton()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(this);
            }
        }
        
        public void StartGame()
        {
            Debug.Log($"[{nameof(GameFlowController)}] Game starting");
            isGameEnded = false;
        }
        
        public void EnableGameplayForAllPlayers()
        {
            Debug.Log($"[{nameof(GameFlowController)}] Enabling gameplay for all players");
            
            isGameplayActive = true;
            
            EnableAllShooters();
            EnableAllPlayerUI();
        }
        
        private void EnableAllShooters()
        {
            CarShooter[] allShooters = FindObjectsByType<CarShooter>(FindObjectsSortMode.None);
            
            foreach (CarShooter shooter in allShooters)
            {
                shooter.EnableGameplay();
            }
        }
        
        private void EnableAllPlayerUI()
        {
            PlayerUIManager[] allPlayerUI = FindObjectsByType<PlayerUIManager>(FindObjectsSortMode.None);
            
            foreach (PlayerUIManager playerUI in allPlayerUI)
            {
                playerUI.EnableGameplay();
            }
        }
        
        public void EndGame()
        {
            if (isGameEnded)
            {
                Debug.LogWarning($"[{nameof(GameFlowController)}] Game already ended");
                return;
            }
            
            Debug.Log($"[{nameof(GameFlowController)}] Game ending");
            
            isGameEnded = true;
            
            ShowCursor();
            DisableAllPlayerControls();
            DisableAllPlayerUI();
            ShowEndScreen();
            DisplayFinalScoreboard();
        }
        
        private void ShowCursor()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        
        private void DisableAllPlayerControls()
        {
            DisableAllControllers();
            DisableAllShooters();
        }
        
        private void DisableAllControllers()
        {
            CarController[] carControllers = FindObjectsByType<CarController>(FindObjectsSortMode.None);
            
            foreach (CarController controller in carControllers)
            {
                controller.enabled = false;
            }
        }
        
        private void DisableAllShooters()
        {
            CarShooter[] carShooters = FindObjectsByType<CarShooter>(FindObjectsSortMode.None);
            
            foreach (CarShooter shooter in carShooters)
            {
                shooter.enabled = false;
            }
        }
        
        private void DisableAllPlayerUI()
        {
            PlayerUIManager[] allPlayerUI = FindObjectsByType<PlayerUIManager>(FindObjectsSortMode.None);
            
            foreach (PlayerUIManager playerUI in allPlayerUI)
            {
                playerUI.DisableGameplay();
            }
        }
        
        private void ShowEndScreen()
        {
            EndGameManager endGameManager = FindFirstObjectByType<EndGameManager>();
            
            if (endGameManager != null)
            {
                endGameManager.OnGameEnd();
            }
            else
            {
                Debug.LogWarning($"[{nameof(GameFlowController)}] EndGameManager not found!");
            }
        }
        
        private void DisplayFinalScoreboard()
        {
            if (PointsManager.instance != null)
            {
                PointsManager.instance.DisplayFinalScoreboard();
            }
            else
            {
                Debug.LogWarning($"[{nameof(GameFlowController)}] PointsManager instance not found!");
            }
        }
        
        public bool IsGameEnded()
        {
            return isGameEnded;
        }
        
        public void ResetGameState()
        {
            isGameEnded = false;
            isGameplayActive = false;
        }
        
        public bool IsGameplayActive()
        {
            return isGameplayActive;
        }
    }
}