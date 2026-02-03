using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using _UI.Scripts;

namespace _Gameplay.Scripts
{
    public class GameplayManager : MonoBehaviour
    {
        public static GameplayManager instance;
        
        [Header("Game Settings")]
        private GameMode currentGameMode;
        private string mapChosen;
        private int multiplayerPlayerCount = 0;
        
        [Header("Map Configuration")]
        private List<string> mapNames;
        
        private const string MAIN_MENU_SCENE_NAME = "MainMenu";
        
        private void Awake()
        {
            InitializeSingleton();
            InitializeMapNames();
        }
        
        private void Start()
        {
            SubscribeToMapEvents();
            SubscribeToTimerEvents();
        }
        
        private void OnDestroy()
        {
            CleanupEventSubscriptions();
        }
        
        private void InitializeSingleton()
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
        }
        
        private void InitializeMapNames()
        {
            mapNames = new List<string>
            {
                "Park Map",
                "Prototype Map"
            };
        }
        
        private void SubscribeToMapEvents()
        {
            MapSelector mapSelector = FindFirstObjectByType<MapSelector>();
            
            if (mapSelector != null)
            {
                mapSelector.onMapSelected.AddListener(SetMap);
                mapSelector.onStartGameClicked.AddListener(StartGame);
            }
        }
        
        private void SubscribeToTimerEvents()
        {
            if (MatchTimerManager.instance != null)
            {
                MatchTimerManager.instance.onMatchStart.AddListener(OnMatchStart);
                MatchTimerManager.instance.onGameEnd.AddListener(OnGameEnd);
            }
        }
        
        private void OnMatchStart()
        {
            Debug.Log($"[{nameof(GameplayManager)}] Match started - enabling gameplay");
            
            if (GameFlowController.instance != null)
            {
                GameFlowController.instance.EnableGameplayForAllPlayers();
            }
        }
        
        private void OnGameEnd()
        {
            Debug.Log($"[{nameof(GameplayManager)}] Game ended");
            
            if (GameFlowController.instance != null)
            {
                GameFlowController.instance.EndGame();
            }
        }
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[{nameof(GameplayManager)}] Scene loaded: {scene.name}");
            
            if (IsMainMenuScene(scene.name))
            {
                HandleMainMenuLoad();
                return;
            }
            
            if (IsGameplayScene(scene.name))
            {
                HandleGameplaySceneLoad();
            }
        }
        
        private bool IsMainMenuScene(string sceneName)
        {
            return sceneName == MAIN_MENU_SCENE_NAME;
        }
        
        private bool IsGameplayScene(string sceneName)
        {
            return mapNames.Contains(sceneName);
        }
        
        private void HandleMainMenuLoad()
        {
            Debug.Log($"[{nameof(GameplayManager)}] Resetting for main menu");
            
            ResetGameConfiguration();
            ResetManagers();
            StartCoroutine(ResubscribeToMapEventsDelayed());
        }
        
        private void ResetGameConfiguration()
        {
            mapChosen = null;
            multiplayerPlayerCount = 0;
        }
        
        private void ResetManagers()
        {
            if (PlayerRegistry.instance != null)
            {
                PlayerRegistry.instance.Clear();
            }
            
            if (GameFlowController.instance != null)
            {
                GameFlowController.instance.ResetGameState();
            }
            
            if (MatchTimerManager.instance != null)
            {
                MatchTimerManager.instance.StopAllTimers();
            }
        }
        
        private IEnumerator ResubscribeToMapEventsDelayed()
        {
            yield return null;
            SubscribeToMapEvents();
            Debug.Log($"[{nameof(GameplayManager)}] Re-subscribed to map events");
        }
        
        private void HandleGameplaySceneLoad()
        {
            Debug.Log($"[{nameof(GameplayManager)}] Setting up gameplay scene");
            
            ResetManagers();
            StartGameBasedOnMode();
        }
        
        private void StartGameBasedOnMode()
        {
            Debug.Log($"[{nameof(GameplayManager)}] Starting game: Mode={currentGameMode}");
            
            SpawnManager spawnManager = FindFirstObjectByType<SpawnManager>();
            
            if (spawnManager == null)
            {
                Debug.LogError($"[{nameof(GameplayManager)}] SpawnManager not found!");
                return;
            }
            
            int playerCount = GetPlayerCountForMode();
            spawnManager.StartGame(playerCount);
            
            if (GameFlowController.instance != null)
            {
                GameFlowController.instance.StartGame();
            }
        }
        
        private int GetPlayerCountForMode()
        {
            return currentGameMode == GameMode.Singleplayer ? 1 : multiplayerPlayerCount;
        }
        
        public void SetGameMode(GameMode mode)
        {
            currentGameMode = mode;
            Debug.Log($"[{nameof(GameplayManager)}] Game mode set to: {mode}");
        }
        
        public void SetMultiplayerPlayerCount(int count)
        {
            multiplayerPlayerCount = count;
            Debug.Log($"[{nameof(GameplayManager)}] Player count set to: {count}");
        }
        
        public void SetMap(string mapName)
        {
            mapChosen = mapName;
            Debug.Log($"[{nameof(GameplayManager)}] Map selected: {mapName}");
        }
        
        public void StartGame()
        {
            if (!ValidateGameConfiguration())
            {
                return;
            }
            
            Debug.Log($"[{nameof(GameplayManager)}] Starting game: Mode={currentGameMode}, Map={mapChosen}, Players={multiplayerPlayerCount}");
            
            LoadingManager.instance.LoadScene(mapChosen);
        }
        
        private bool ValidateGameConfiguration()
        {
            if (string.IsNullOrEmpty(mapChosen))
            {
                Debug.LogError($"[{nameof(GameplayManager)}] No map selected!");
                return false;
            }
            
            if (currentGameMode == GameMode.Multiplayer && multiplayerPlayerCount <= 0)
            {
                Debug.LogError($"[{nameof(GameplayManager)}] Multiplayer mode but no player count set!");
                return false;
            }
            
            return true;
        }
        
        public void StartMatchTimer()
        {
            if (MatchTimerManager.instance != null)
            {
                MatchTimerManager.instance.StartMatchCountdown();
            }
            else
            {
                Debug.LogError($"[{nameof(GameplayManager)}] MatchTimerManager instance not found!");
            }
        }
        
        public void UpdatePlayerList(string playerTag)
        {
            if (PlayerRegistry.instance != null)
            {
                PlayerRegistry.instance.RegisterPlayer(playerTag);
            }
        }
        
        public List<string> GetPlayerTags()
        {
            if (PlayerRegistry.instance != null)
            {
                return PlayerRegistry.instance.GetAllPlayerTags();
            }
            
            return new List<string>();
        }
        
        public bool IsGameSetupComplete()
        {
            if (PlayerRegistry.instance != null)
            {
                bool isComplete = PlayerRegistry.instance.HasPlayers();
                Debug.Log($"[{nameof(GameplayManager)}] IsGameSetupComplete: {isComplete}");
                return isComplete;
            }
            
            return false;
        }
        
        public bool IsGameEnded()
        {
            if (GameFlowController.instance != null)
            {
                return GameFlowController.instance.IsGameEnded();
            }
            
            return false;
        }
        
        private void CleanupEventSubscriptions()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            UnsubscribeFromMapEvents();
            UnsubscribeFromTimerEvents();
        }
        
        private void UnsubscribeFromMapEvents()
        {
            MapSelector mapSelector = FindFirstObjectByType<MapSelector>();
            
            if (mapSelector != null)
            {
                mapSelector.onMapSelected.RemoveListener(SetMap);
                mapSelector.onStartGameClicked.RemoveListener(StartGame);
            }
        }
        
        private void UnsubscribeFromTimerEvents()
        {
            if (MatchTimerManager.instance != null)
            {
                MatchTimerManager.instance.onMatchStart.RemoveListener(OnMatchStart);
                MatchTimerManager.instance.onGameEnd.RemoveListener(OnGameEnd);
            }
        }
    }
}