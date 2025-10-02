using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
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

    }
}

