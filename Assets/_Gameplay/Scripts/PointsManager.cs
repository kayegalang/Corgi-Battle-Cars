using System.Collections.Generic;
using System.Linq;
using _Cars.Scripts;
using _UI.Scripts;
using TMPro;
using UnityEngine;

namespace _Gameplay.Scripts
{
    public class PointsManager : MonoBehaviour
    {
        public static PointsManager instance;
        
        [SerializeField] private EndGameManager endGameManager;

        private Dictionary<string, int> playerScores;

        private List<string> playerTags;
        
        [SerializeField] private TextMeshProUGUI pointsText;


        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            playerScores = new Dictionary<string, int>();
            playerTags = GameplayManager.instance.GetPlayerTags();

            // Initialize dictionary for each tag
            foreach (string playerTag in playerTags)
            {
                playerScores.TryAdd(playerTag, 0);
            }
        }
        
        public void AddPoint(string playerTag)
        {
            playerScores[playerTag]++;
            
            if (playerTag.Equals("PlayerOne"))
            {
                UpdatePointsUI();
            }
            
            UpdateScoreboard();
        }
        
        public int GetPoints(string playerTag)
        {
            return playerScores.GetValueOrDefault(playerTag);
        }
        
        private void UpdatePointsUI()
        {
            pointsText.text = "Points: " + playerScores["PlayerOne"];
        }
        
        private void UpdateScoreboard()
        {
            var sorted = playerScores
                .OrderByDescending(p => p.Value)
                .Select(p => (playerTag: p.Key, points: p.Value))
                .ToList();

            // Update EndGameManager
            if (endGameManager != null)
                endGameManager.DisplayResults(sorted);
        }
    }
}
