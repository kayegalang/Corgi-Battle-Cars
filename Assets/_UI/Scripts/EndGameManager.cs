using System.Collections.Generic;
using _Cars.Scripts;
using _Gameplay.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _UI.Scripts
{
    public class EndGameManager : MonoBehaviour
    {
        [SerializeField] private GameObject endScreen;
        [SerializeField] private TextMeshProUGUI resultsText;

        public void OnGameEnd()
        {
            Time.timeScale = 0;
            endScreen.SetActive(true);
        }

        public void OnMainMenuButtonClicked()
        {
            Time.timeScale = 1;
            SceneManager.LoadScene("MainMenu");
        }

        public void OnPlayAgainButtonClicked()
        {
            Time.timeScale = 1;
            GameplayManager.instance.StartGame();
        }
        
        public void DisplayResults(List<(string tag, int points)> sorted)
        {
            if (sorted == null || sorted.Count == 0)
            {
                resultsText.text = "No players found!";
                return;
            }

            string results = "";
            int currentRank = 1;  // The rank number (1st, 2nd, etc.)
            int playersWithSameScore = 1; // How many players share the same rank

            for (int i = 0; i < sorted.Count; i++)
            {
                string playerName = string.IsNullOrEmpty(sorted[i].tag) ? "Unknown" : sorted[i].tag;
                int playerPoints = sorted[i].points;

                // Apply rank
                string rankText = GetOrdinal(currentRank);
                results += $"{rankText} - {playerName} ({playerPoints} points)\n";

                // Check next player's score
                if (i < sorted.Count - 1)
                {
                    if (sorted[i + 1].points == sorted[i].points)
                    {
                        // Next player is tied, same rank
                        playersWithSameScore++;
                    }
                    else
                    {
                        // Skip ranks equal to the number of tied players
                        currentRank += playersWithSameScore;
                        playersWithSameScore = 1;
                    }
                }
            }

            resultsText.text = results;
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
