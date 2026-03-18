using UnityEngine;
using _Gameplay.Scripts;

namespace _Prototyping.Scripts
{
    public class TuningSceneBootstrapper : MonoBehaviour
    {
        [Header("Tuning Scene Settings")]
        [SerializeField] private int humanPlayerCount = 1;

        private void Start()
        {
            // Set up GameplayManager so dependents like PointsManager don't complain
            if (GameplayManager.instance != null)
            {
                GameplayManager.instance.SetGameMode(GameMode.Multiplayer);
                GameplayManager.instance.SetMultiplayerPlayerCount(humanPlayerCount);
            }

            SpawnManager spawnManager = FindFirstObjectByType<SpawnManager>();
            if (spawnManager == null) { Debug.LogError("[TuningSceneBootstrapper] No SpawnManager!"); return; }

            spawnManager.StartGame(humanPlayerCount);

            if (GameFlowController.instance != null)
            {
                GameFlowController.instance.StartGame();
                GameFlowController.instance.EnableGameplayForAllPlayers();
            }
        }
    }
}