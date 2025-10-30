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
            for (int i = 0; i < sorted.Count; i++)
            {
                int rank = i + 1;
                if (i > 0 && sorted[i].points == sorted[i - 1].points)
                    rank--; // handle tie

                string rankText = GetOrdinal(rank);
                string playerName = string.IsNullOrEmpty(sorted[i].tag) ? "Unknown" : sorted[i].tag;
                int playerPoints = sorted[i].points;

                // ✅ Fixed: use "\n", not "\\n"
                results += $"{rankText} - {playerName} ({playerPoints} points)\n";
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
