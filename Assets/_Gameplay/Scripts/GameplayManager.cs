using System.Collections;
using System.Collections.Generic;
using _Player.Scripts;
using _UI.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Gameplay.Scripts
{
    public class GameplayManager : MonoBehaviour
    {
        public static GameplayManager instance;

        private GameMode currentGameMode;
        private string mapChosen;

        private SpawnManager spawnManager;
        private PlayerManager playerManager;

        private List<string> mapNames;
        private List<string> playerTags = new();

        private TextMeshProUGUI matchTimerText;
        private TextMeshProUGUI gameTimerText;

        private EndGameManager endGameManager;

        // Prevent duplicate timer starts
        private bool matchTimerStarted = false;
        private bool gameIsBeingSetup = false;

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
                return;
            }

            mapNames = new List<string> { "Park Map", "Prototype Map" };
        }

        public void SetGameMode(GameMode mode) => currentGameMode = mode;
        public void SetMap(string mapName) => mapChosen = mapName;
        public GameMode GetGameMode() => currentGameMode;

        public void StartGame()
        {
            Debug.Log("StartGame called");
            playerTags.Clear();
            matchTimerStarted = false;
            gameIsBeingSetup = false;
            
            // For singleplayer, ensure a player exists before loading the scene
            if (currentGameMode == GameMode.Singleplayer)
            {
                StartCoroutine(StartSingleplayerGame());
            }
            else
            {
                LoadingManager.instance.LoadScene(mapChosen);
            }
        }

        private IEnumerator StartSingleplayerGame()
        {
            // Check if Player1 already exists
            GameObject player = GameObject.FindGameObjectWithTag("Player1");
            
            if (player == null)
            {
                // Try to find generic "Player" tag
                player = GameObject.FindGameObjectWithTag("Player");
                
                if (player != null)
                {
                    player.tag = "Player1";
                    Debug.Log("Retagged existing player as Player1");
                }
                else
                {
                    // Auto-join player for singleplayer
                    var inputManager = FindAnyObjectByType<UnityEngine.InputSystem.PlayerInputManager>();
                    if (inputManager != null)
                    {
                        Debug.Log("Auto-joining Player1 for singleplayer");
                        inputManager.JoinPlayer(0, -1, null);
                        
                        // Wait for player to spawn
                        yield return null;
                        
                        // Tag the player
                        player = GameObject.FindGameObjectWithTag("Player");
                        if (player != null)
                        {
                            player.tag = "Player1";
                            Debug.Log("Tagged new player as Player1");
                        }
                    }
                }
            }
            
            // Make player persistent
            if (player != null)
            {
                DontDestroyOnLoad(player.transform.root.gameObject);
            }
            
            // Now load the scene
            LoadingManager.instance.LoadScene(mapChosen);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"OnSceneLoaded called for scene: {scene.name}, mode: {mode}, gameIsBeingSetup: {gameIsBeingSetup}");
            
            if (!mapNames.Contains(scene.name))
            {
                Debug.Log($"Scene {scene.name} is not a map scene, ignoring");
                return;
            }

            // Prevent multiple setup calls if already in progress
            if (gameIsBeingSetup)
            {
                Debug.LogWarning($"Game setup already in progress for {scene.name}, ignoring duplicate OnSceneLoaded call");
                return;
            }

            gameIsBeingSetup = true;
            Debug.Log($"Scene {scene.name} IS a map scene, proceeding with setup");

            spawnManager = FindFirstObjectByType<SpawnManager>();
            playerManager = FindFirstObjectByType<PlayerManager>();

            if (spawnManager == null)
            {
                Debug.LogError("SpawnManager not found in map scene!");
                gameIsBeingSetup = false;
                return;
            }

            SetupGame();
        }

        private void SetupGame()
        {
            Debug.Log($"SetupGame called. Mode: {currentGameMode}, matchTimerStarted: {matchTimerStarted}");

            if (currentGameMode == GameMode.Singleplayer)
            {
                spawnManager.StartSingleplayerGame();
                StartMatchTimer();
            }
            else if (currentGameMode == GameMode.Multiplayer)
            {
                spawnManager.StartMultiplayerGame(playerManager);
                StartMatchTimer();
            }
        }

        // ============================================================
        // TIMERS
        // ============================================================

        private IEnumerator GameTimer(int time)
        {
            while (time > 0)
            {
                if (gameTimerText != null)
                    gameTimerText.text = "Time: " + time;

                yield return new WaitForSeconds(1);
                time--;
            }

            if (gameTimerText != null)
                gameTimerText.text = "Time: 0";

            EndGame();
        }

        private IEnumerator MatchTimer()
        {
            Debug.Log("Match timer coroutine started");
            
            // Wait a moment for cameras to initialize
            yield return new WaitForSecondsRealtime(0.5f);
            
            Time.timeScale = 0;
            int time = 3;

            while (time >= 0)
            {
                if (matchTimerText != null)
                    matchTimerText.text = (time == 0) ? "Go!" : time.ToString();

                yield return new WaitForSecondsRealtime(1);
                time--;
            }

            if (matchTimerText != null)
                matchTimerText.text = "";

            Time.timeScale = 1;
            gameIsBeingSetup = false;
            Debug.Log("Match timer coroutine completed");
            StartGameTimer();
        }

        public void StartMatchTimer()
        {
            Debug.Log($"StartMatchTimer called. matchTimerStarted: {matchTimerStarted}");
            
            // Prevent starting timer multiple times
            if (matchTimerStarted)
            {
                Debug.LogWarning("Match timer already started, ignoring duplicate call");
                return;
            }

            matchTimerStarted = true;
            matchTimerText = GameObject.Find("MatchTimerText")?.GetComponent<TextMeshProUGUI>();
            
            if (matchTimerText == null)
                Debug.LogWarning("MatchTimerText not found in scene!");
            
            StartCoroutine(MatchTimer());
        }

        private void StartGameTimer()
        {
            gameTimerText = GameObject.Find("GameTimerText")?.GetComponent<TextMeshProUGUI>();
            
            if (gameTimerText == null)
                Debug.LogWarning("GameTimerText not found in scene!");
            
            StartCoroutine(GameTimer(60));
        }

        // ============================================================
        // END GAME / TAG TRACKING
        // ============================================================

        private void EndGame()
        {
            endGameManager = FindFirstObjectByType<EndGameManager>();
            if (endGameManager != null)
                endGameManager.OnGameEnd();
            
            // Destroy all player objects (they were DontDestroyOnLoad)
            foreach (string tag in playerTags)
            {
                if (tag.StartsWith("Player"))
                {
                    GameObject playerObj = GameObject.FindGameObjectWithTag(tag);
                    if (playerObj != null)
                    {
                        Debug.Log($"Destroying player {tag}");
                        Destroy(playerObj);
                    }
                }
            }
            
            // Restore cursor visibility
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public void UpdatePlayerList(string playerTag)
        {
            if (!playerTags.Contains(playerTag))
                playerTags.Add(playerTag);
        }
        
        public bool IsGameSetupComplete() { return GameObject.FindGameObjectWithTag("Player1") != null; }

        public List<string> GetPlayerTags() => playerTags;
    }
}