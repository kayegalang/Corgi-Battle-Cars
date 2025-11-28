using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using _UI.Scripts;
using _Cars.Scripts;
using TMPro;

namespace _Gameplay.Scripts
{
    public class GameplayManager : MonoBehaviour
    {
        public static GameplayManager instance;

        private GameMode currentGameMode;
        private string mapChosen;
        private SpawnManager spawnManager;
        private List<string> mapNames;
        private List<string> playerTags;
        
        private int multiplayerPlayerCount = 0; // Number of human players in multiplayer

        private TextMeshProUGUI matchTimerText = null;
        private TextMeshProUGUI gameTimerText = null;
        
        private bool isGameEnded = false;
        
        private EndGameManager endGameManager;
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);

                SceneManager.sceneLoaded += OnSceneLoaded;
            }
            else
            {
                Destroy(gameObject);
            }

            spawnManager = null;
            InitializeMapNames();
            playerTags = new List<string>();
        }

        private void InitializeMapNames()
        {
            mapNames = new List<string>();
            mapNames.Add("Park Map");
            mapNames.Add("Prototype Map");
        }

        public void SetGameMode(GameMode mode)
        {
            currentGameMode = mode;
        }
        
        public void SetMultiplayerPlayerCount(int count)
        {
            multiplayerPlayerCount = count;
        }

        public void SetMap(string mapName)
        {
            mapChosen = mapName;
        }

        public void StartGame()
        {
            LoadingManager.instance.LoadScene(mapChosen);
        }

        private void StartSingleplayerGame()
        {
            spawnManager = FindFirstObjectByType<SpawnManager>();
            spawnManager.StartSingleplayerGame();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Reset state when loading a map (starting a new game)
            if (mapNames.Contains(scene.name))
            {
                ResetGameState();
                SetupGame();
            }
        }
        
        private void ResetGameState()
        {
            // Clear player tags from previous game
            playerTags.Clear();
            
            // Reset references
            spawnManager = null;
            matchTimerText = null;
            gameTimerText = null;
            endGameManager = null;
            
            // Reset game ended flag
            isGameEnded = false;
            
            // Stop any running coroutines
            StopAllCoroutines();
        }

        private void SetupGame()
        {
            if (currentGameMode == GameMode.Singleplayer)
            {
                StartSingleplayerGame();
            }

            if (currentGameMode == GameMode.Multiplayer)
            {
                StartMultiplayerGame();
            }
        }

        private void StartMultiplayerGame()
        {
            spawnManager = FindFirstObjectByType<SpawnManager>();
            spawnManager.StartMultiplayerGame(multiplayerPlayerCount);
        }

        public bool IsGameSetupComplete()
        {
            // Check if we have players registered (works for both single & multiplayer)
            return playerTags.Count > 0;
        }

        private IEnumerator GameTimer(int time)
        {
            while (time > 0)
            {
                gameTimerText.text = "Time: " + time;
                
                yield return new WaitForSeconds(1);

                time--;
            }

            if (time == 0)
            {
                gameTimerText.text = "Time: " + time;

                EndGame();
            }
        }

        private IEnumerator MatchTimer()
        {
            Time.timeScale = 0;
            var time = 3;

            while (time >= 0)
            {
                if (time == 0)
                {
                    matchTimerText.text = "Go!";
                }
                else
                {
                    matchTimerText.text = time.ToString();
                }
                
                yield return new WaitForSecondsRealtime(1);
                time--;
            }
            
            matchTimerText.text = "";
            Time.timeScale = 1;
            StartGameTimer();
        }

        public void StartMatchTimer()
        {
            matchTimerText = GameObject.Find("MatchTimerText").GetComponent<TextMeshProUGUI>();
            StartCoroutine(MatchTimer());
        }

        private void StartGameTimer()
        {
            gameTimerText = GameObject.Find("GameTimerText").GetComponent<TextMeshProUGUI>();
            
            // Enable gameplay for all players now that the match has started
            EnableGameplayForAllPlayers();
            
            StartCoroutine(GameTimer(60));
        }
        
        private void EnableGameplayForAllPlayers()
        {
            // Find all CarShooter components and enable gameplay
            CarShooter[] allShooters = FindObjectsByType<CarShooter>(FindObjectsSortMode.None);
            foreach (CarShooter shooter in allShooters)
            {
                shooter.EnableGameplay();
            }
            
            PlayerUIManager[] allPlayerUI = FindObjectsByType<PlayerUIManager>(FindObjectsSortMode.None);
            foreach (PlayerUIManager playerUI in allPlayerUI)
            {
                playerUI.EnableGameplay();
            }
            
        }

        private void EndGame()
        {
            // Mark game as ended
            isGameEnded = true;
            
            // Show and unlock cursor when game ends
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            // Find all CarController and CarShooter components and disable them
            CarController[] carControllers = FindObjectsByType<CarController>(FindObjectsSortMode.None);
            foreach (CarController controller in carControllers)
            {
                controller.enabled = false;
            }
            
            CarShooter[] carShooters = FindObjectsByType<CarShooter>(FindObjectsSortMode.None);
            foreach (CarShooter shooter in carShooters)
            {
                shooter.enabled = false;
            }
            
            PlayerUIManager[] allPlayerUI = FindObjectsByType<PlayerUIManager>(FindObjectsSortMode.None);
            foreach (PlayerUIManager playerUI in allPlayerUI)
            {
                playerUI.DisableGameplay();
            }
            
            endGameManager = FindFirstObjectByType<EndGameManager>();
            endGameManager.OnGameEnd();
        }

        public void UpdatePlayerList(string playerTag)
        {
            playerTags.Add(playerTag);
        }

        public List<string> GetPlayerTags()
        {
            return playerTags;
        }
        
        public bool IsGameEnded()
        {
            return isGameEnded;
        }
    }
}