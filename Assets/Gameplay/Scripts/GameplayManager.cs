using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;
using UI.Scripts;

namespace Gameplay.Scripts
{
    public class GameplayManager : MonoBehaviour
    {
        public static GameplayManager instance;

        private GameMode CurrentGameMode;
        private string mapChosen;
        private SpawnManager spawnManager;
        private List<string> mapNames;

        private TextMeshProUGUI timerText = null;

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
            CurrentGameMode = mode;
        }

        public void SetMap(string mapName)
        {
            mapChosen = mapName;
        }

        public void OnStartGameButtonClicked()
        {
            LoadingManager.instance.LoadScene(mapChosen);
        }

        private void StartSingleplayerGame()
        {
            spawnManager = FindFirstObjectByType<SpawnManager>();
            spawnManager.Spawn("PlayerOne");
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
            if (CurrentGameMode == GameMode.Singleplayer)
            {
                StartSingleplayerGame();
            }
        }

        public bool IsGameSetupComplete()
        {
            return GameObject.FindGameObjectWithTag("PlayerOne") != null;
        }

        private IEnumerator GameTimer()
        {
            Time.timeScale = 0;
            var time = 3;

            while (time >= 0)
            {
                if (time == 0)
                {
                    timerText.text = "Go!";
                }
                else
                {
                    timerText.text = time.ToString();
                }
                
                yield return new WaitForSecondsRealtime(1);
                time--;
            }
            
            timerText.text = "";
            Time.timeScale = 1;
        }

        public void StartGameTimer()
        {
            timerText = GameObject.Find("TimerText").GetComponent<TextMeshProUGUI>();
            StartCoroutine(GameTimer());
        }

    }
}

