using System.Collections.Generic;
using System.Text;
using _Gameplay.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace _UI.Scripts
{
    public class EndGameManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject endScreen;
        [SerializeField] private TextMeshProUGUI resultsText;
        
        [Header("Scene Names")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        
        [Header("UI Text")]
        [SerializeField] private string noPlayersText = "No players found!";
        [SerializeField] private string resultLineFormat = "{0} - {1} ({2} points)";
        [SerializeField] private string unknownPlayerName = "Unknown";
        
        [Header("Events")]
        public UnityEvent onPlayAgain;
        public UnityEvent onReturnToMenu;

        public void OnGameEnd()
        {
            Time.timeScale = 0;
            endScreen.SetActive(true);
        }

        public void OnMainMenuButtonClicked()
        {
            ResetTimeScale();
            
            onReturnToMenu?.Invoke();
            
            SceneManager.LoadScene(mainMenuSceneName);
        }

        public void OnPlayAgainButtonClicked()
        {
            ResetTimeScale();
            
            onPlayAgain?.Invoke();
            
            GameplayManager.instance?.StartGame();
        }
        
        private void ResetTimeScale()
        {
            Time.timeScale = 1;
        }
        
        public void DisplayResults(List<(string tag, int points)> sorted)
        {
            if (sorted == null || sorted.Count == 0)
            {
                resultsText.text = noPlayersText;
                return;
            }

            StringBuilder results = new StringBuilder();
            int currentRank = 1;
            int playersWithSameScore = 1;

            for (int i = 0; i < sorted.Count; i++)
            {
                string playerName = GetPlayerDisplayName(sorted[i].tag);
                int playerPoints = sorted[i].points;
                string rankText = GetOrdinal(currentRank);
                
                string line = string.Format(resultLineFormat, rankText, playerName, playerPoints);
                results.AppendLine(line);

                UpdateRankTracking(sorted, i, ref currentRank, ref playersWithSameScore);
            }

            resultsText.text = results.ToString();
        }
        
        private string GetPlayerDisplayName(string tag)
        {
            return string.IsNullOrEmpty(tag) ? unknownPlayerName : tag;
        }
        
        private void UpdateRankTracking(List<(string tag, int points)> sorted, int currentIndex, 
                                       ref int currentRank, ref int playersWithSameScore)
        {
            if (currentIndex >= sorted.Count - 1) return;
            
            if (sorted[currentIndex + 1].points == sorted[currentIndex].points)
            {
                playersWithSameScore++;
            }
            else
            {
                currentRank += playersWithSameScore;
                playersWithSameScore = 1;
            }
        }

        private string GetOrdinal(int num)
        {
            if (num % 100 >= 11 && num % 100 <= 13)
                return num + "th";
            
            return (num % 10) switch
            {
                1 => num + "st",
                2 => num + "nd",
                3 => num + "rd",
                _ => num + "th"
            };
        }
    }
}