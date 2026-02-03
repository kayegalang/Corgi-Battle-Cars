using System.Collections.Generic;
using System.Linq;
using _UI.Scripts;
using UnityEngine;

namespace _Gameplay.Scripts
{
    public class PointsManager : MonoBehaviour
    {
        public static PointsManager instance;
        
        [SerializeField] private EndGameManager endGameManager;
        
        private Dictionary<string, int> playerScores;
        private List<string> playerTags;
        
        private const int INITIAL_SCORE = 0;
        
        private void Awake()
        {
            InitializeSingleton();
        }
        
        private void Start()
        {
            InitializeScoreTracking();
        }
        
        private void InitializeSingleton()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Debug.LogWarning($"[{nameof(PointsManager)}] Duplicate instance found, destroying {gameObject.name}");
                Destroy(gameObject);
            }
        }
        
        private void InitializeScoreTracking()
        {
            playerScores = new Dictionary<string, int>();
            
            if (!ValidateGameplayManager())
            {
                return;
            }
            
            playerTags = GameplayManager.instance.GetPlayerTags();
            
            InitializePlayerScores();
        }
        
        private bool ValidateGameplayManager()
        {
            if (GameplayManager.instance == null)
            {
                Debug.LogError($"[{nameof(PointsManager)}] GameplayManager instance is null!");
                return false;
            }
            
            return true;
        }
        
        private void InitializePlayerScores()
        {
            foreach (string playerTag in playerTags)
            {
                playerScores.TryAdd(playerTag, INITIAL_SCORE);
            }
        }
        
        public void AddPoint(string playerTag)
        {
            if (!ValidatePlayerTag(playerTag))
            {
                return;
            }
            
            IncrementPlayerScore(playerTag);
            UpdateAllPlayerUIs();
        }
        
        private bool ValidatePlayerTag(string playerTag)
        {
            if (string.IsNullOrEmpty(playerTag))
            {
                Debug.LogWarning($"[{nameof(PointsManager)}] Cannot add point - player tag is null or empty");
                return false;
            }
            
            if (!playerScores.ContainsKey(playerTag))
            {
                Debug.LogWarning($"[{nameof(PointsManager)}] Player tag '{playerTag}' not found in score dictionary");
                return false;
            }
            
            return true;
        }
        
        private void IncrementPlayerScore(string playerTag)
        {
            playerScores[playerTag]++;
        }
        
        private void UpdateAllPlayerUIs()
        {
            PlayerUIManager[] allPlayerUIs = FindObjectsByType<PlayerUIManager>(FindObjectsSortMode.None);
            
            foreach (PlayerUIManager ui in allPlayerUIs)
            {
                string tag = ui.GetPlayerTag();
                if (playerScores.ContainsKey(tag))
                {
                    ui.UpdateScore(playerScores[tag]);
                }
            }
        }
        
        public int GetPoints(string playerTag)
        {
            return playerScores.GetValueOrDefault(playerTag, INITIAL_SCORE);
        }
        
        public void DisplayFinalScoreboard()
        {
            if (endGameManager == null)
            {
                Debug.LogWarning($"[{nameof(PointsManager)}] EndGameManager is not assigned!");
                return;
            }
            
            List<(string playerTag, int points)> sortedScores = GetSortedScores();
            endGameManager.DisplayResults(sortedScores);
        }
        
        private List<(string playerTag, int points)> GetSortedScores()
        {
            return playerScores
                .OrderByDescending(p => p.Value)
                .Select(p => (playerTag: p.Key, points: p.Value))
                .ToList();
        }
    }
}