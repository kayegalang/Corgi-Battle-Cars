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

        [Header("Scoring")]
        [SerializeField] private int pointsPerKill = 100;

        private Dictionary<string, int>   playerScores;
        private Dictionary<string, float> playerScoreTimestamps; // tiebreaker — who scored last first
        private List<string>              playerTags;

        private const int INITIAL_SCORE = 0;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

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
                instance = this;
            else
            {
                Debug.LogWarning($"[{nameof(PointsManager)}] Duplicate instance, destroying {gameObject.name}");
                Destroy(gameObject);
            }
        }

        private void InitializeScoreTracking()
        {
            playerScores          = new Dictionary<string, int>();
            playerScoreTimestamps = new Dictionary<string, float>();

            if (!ValidateGameplayManager()) return;

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
                playerScoreTimestamps.TryAdd(playerTag, float.MaxValue);
            }
        }

        // ═══════════════════════════════════════════════
        //  SCORING
        // ═══════════════════════════════════════════════

        public void AddPoint(string playerTag)
        {
            if (!ValidatePlayerTag(playerTag)) return;

            playerScores[playerTag]         += pointsPerKill;
            playerScoreTimestamps[playerTag]  = Time.time;

            UpdateAllPlayerUIs(playerTag);
        }

        private bool ValidatePlayerTag(string playerTag)
        {
            if (string.IsNullOrEmpty(playerTag))
            {
                Debug.LogWarning($"[{nameof(PointsManager)}] Cannot add point — tag is null or empty");
                return false;
            }

            if (!playerScores.ContainsKey(playerTag))
            {
                Debug.LogWarning($"[{nameof(PointsManager)}] Tag '{playerTag}' not found in score dictionary");
                return false;
            }

            return true;
        }

        private void UpdateAllPlayerUIs(string scoringPlayerTag)
        {
            PlayerUIManager[] allPlayerUIs = FindObjectsByType<PlayerUIManager>(FindObjectsSortMode.None);

            foreach (PlayerUIManager ui in allPlayerUIs)
            {
                string tag = ui.GetPlayerTag();
                if (!playerScores.ContainsKey(tag)) continue;

                int newScore = playerScores[tag];

                if (tag == scoringPlayerTag)
                    ui.UpdateScoreWithFloatingText(newScore, pointsPerKill);
                else
                    ui.UpdateScore(newScore);
            }
        }

        // ═══════════════════════════════════════════════
        //  GETTERS
        // ═══════════════════════════════════════════════

        public int GetPoints(string playerTag) =>
            playerScores.GetValueOrDefault(playerTag, INITIAL_SCORE);

        // ═══════════════════════════════════════════════
        //  END GAME
        // ═══════════════════════════════════════════════

        public void DisplayFinalScoreboard()
        {
            if (endGameManager == null)
            {
                Debug.LogWarning($"[{nameof(PointsManager)}] EndGameManager is not assigned!");
                return;
            }

            endGameManager.DisplayResults(GetSortedScores());
        }

        // Sort by score descending, tiebreak by who reached that score earliest
        private List<(string playerTag, int points)> GetSortedScores()
        {
            return playerScores
                .OrderByDescending(p => p.Value)
                .ThenBy(p => playerScoreTimestamps.GetValueOrDefault(p.Key, float.MaxValue))
                .Select(p => (playerTag: p.Key, points: p.Value))
                .ToList();
        }
    }
}