using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using _UI.Scripts;
using Gameplay.Scripts;
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

        private TextMeshProUGUI matchTimerText = null;
        private TextMeshProUGUI gameTimerText = null;
        
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
            if (mapNames.Contains(scene.name))
            {
                SetupGame();
            }
        }

        private void SetupGame()
        {
            if (currentGameMode == GameMode.Singleplayer)
            {
                StartSingleplayerGame();
            }
        }

        public bool IsGameSetupComplete()
        {
            return GameObject.FindGameObjectWithTag("PlayerOne") != null;
        }

        private IEnumerator GameTimer(int time)
        {
            while (time > 0)
            {
                gameTimerText.text = time.ToString();
                
                yield return new WaitForSeconds(1);

                time--;
            }

            if (time == 0)
            {
                gameTimerText.text = time.ToString();

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
            StartCoroutine(GameTimer(30));
        }

        private void EndGame()
        {
            endGameManager = FindFirstObjectByType<EndGameManager>();
            endGameManager.OnGameEnd();
        }

    }
}

