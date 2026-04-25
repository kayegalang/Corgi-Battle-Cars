using System.Collections;
using System.Collections.Generic;
using _Audio.scripts;
using _Gameplay.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _UI.Scripts
{
    public class EndGameManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════════
        //  INSPECTOR REFERENCES
        // ═══════════════════════════════════════════════

        [Header("Panels")]
        [SerializeField] private GameObject endScreen;

        [Header("Winner Section")]
        [SerializeField] private TextMeshProUGUI winnerTitleText;
        [SerializeField] private TextMeshProUGUI winnerNameText;
        [SerializeField] private TextMeshProUGUI winnerScoreText;
        [SerializeField] private GameObject      crownObject;

        [Header("Scoreboard — assign up to 3 loser rows")]
        [SerializeField] private List<PlayerResultRow> loserRows;

        [Header("Buttons")]
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Scene Names")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Header("Rank Titles")]
        [SerializeField] private string rank1Title = "TOP DOG! 👑";
        [SerializeField] private string rank2Title = "GOOD BOY! 🐾";
        [SerializeField] private string rank3Title = "ALMOST! 🦴";
        [SerializeField] private string rank4Title = "STILL LEARNING TO SIT 😅";

        [Header("Points Label")]
        [SerializeField] private string pointsFormat = "{0} pts";

        [Header("Animation")]
        [SerializeField] private float revealDelay   = 0.3f;
        [SerializeField] private float loserRowDelay = 0.2f;

        [Header("Events")]
        public UnityEvent onPlayAgain;
        public UnityEvent onReturnToMenu;

        [Header("Finished Sequence")]
        [SerializeField] private GameObject finishedTextObject;
        [SerializeField] private Animator   finishedAnimator;
        [SerializeField] private float      endBufferDuration = 2f;

        // ═══════════════════════════════════════════════
        //  NESTED CLASS
        // ═══════════════════════════════════════════════

        [System.Serializable]
        public class PlayerResultRow
        {
            public GameObject      rowRoot;
            public TextMeshProUGUI titleText;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI scoreText;
        }

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            if (endScreen != null) endScreen.SetActive(false);
            HideAllLoserRows();

            if (playAgainButton != null)
                playAgainButton.onClick.AddListener(OnPlayAgainButtonClicked);

            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);
        }

        // ═══════════════════════════════════════════════
        //  GAME END
        // ═══════════════════════════════════════════════

        public void OnGameEnd()
        {
            StartCoroutine(EndGameSequence());
        }

        // ═══════════════════════════════════════════════
        //  END GAME SEQUENCE
        // ═══════════════════════════════════════════════

        private IEnumerator EndGameSequence()
        {
            if (finishedTextObject != null)
                finishedTextObject.SetActive(true);

            // Play finished sound before muting SFX
            if (AudioManager.instance != null && FMODEvents.instance != null)
                AudioManager.instance.PlayOneShot(FMODEvents.instance.FinishedSound, transform.position);

            if (finishedAnimator != null)
                finishedAnimator.Play("FinishedTextAnimation");

            yield return new WaitForSecondsRealtime(endBufferDuration);

            // Mute all SFX — only music plays during end screen
            AudioManager.instance?.MuteSFX(true);

            Time.timeScale = 0f;

            if (endScreen != null)
                endScreen.SetActive(true);
        }

        // ═══════════════════════════════════════════════
        //  DISPLAY RESULTS
        // ═══════════════════════════════════════════════

        public void DisplayResults(List<(string tag, int points)> sorted)
        {
            if (sorted == null || sorted.Count == 0)
            {
                Debug.LogWarning("[EndGameManager] No results to display!");
                return;
            }

            OnGameEnd();
            StartCoroutine(RevealResults(sorted));
        }

        private IEnumerator RevealResults(List<(string tag, int points)> sorted)
        {
            SetWinnerVisible(false);
            HideAllLoserRows();

            yield return new WaitForSecondsRealtime(revealDelay);

            var winner = sorted[0];
            ShowWinner(winner.tag, winner.points);
            SetWinnerVisible(true);

            for (int i = 1; i < sorted.Count && i - 1 < loserRows.Count; i++)
            {
                yield return new WaitForSecondsRealtime(loserRowDelay);
                var loser = sorted[i];
                ShowLoserRow(i - 1, i + 1, loser.tag, loser.points);
            }
        }

        // ═══════════════════════════════════════════════
        //  WINNER
        // ═══════════════════════════════════════════════

        private void ShowWinner(string tag, int points)
        {
            if (winnerTitleText != null) winnerTitleText.text = rank1Title;
            if (winnerNameText  != null) winnerNameText.text  = GetDisplayName(tag);
            if (winnerScoreText != null) winnerScoreText.text = string.Format(pointsFormat, points);
        }

        private void SetWinnerVisible(bool visible)
        {
            if (winnerTitleText != null) winnerTitleText.gameObject.SetActive(visible);
            if (winnerNameText  != null) winnerNameText.gameObject.SetActive(visible);
            if (winnerScoreText != null) winnerScoreText.gameObject.SetActive(visible);
            if (crownObject     != null) crownObject.SetActive(visible);
        }

        // ═══════════════════════════════════════════════
        //  LOSER ROWS
        // ═══════════════════════════════════════════════

        private void ShowLoserRow(int rowIndex, int rank, string tag, int points)
        {
            if (rowIndex >= loserRows.Count) return;

            PlayerResultRow row = loserRows[rowIndex];
            if (row == null || row.rowRoot == null) return;

            if (row.titleText != null) row.titleText.text = GetRankTitle(rank);
            if (row.nameText  != null) row.nameText.text  = GetDisplayName(tag);
            if (row.scoreText != null) row.scoreText.text = string.Format(pointsFormat, points);

            row.rowRoot.SetActive(true);
        }

        private void HideAllLoserRows()
        {
            if (loserRows == null) return;
            foreach (var row in loserRows)
                if (row?.rowRoot != null)
                    row.rowRoot.SetActive(false);
        }

        // ═══════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════

        private string GetRankTitle(int rank)
        {
            return rank switch
            {
                2 => rank2Title,
                3 => rank3Title,
                4 => rank4Title,
                _ => rank4Title
            };
        }

        private string GetDisplayName(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return "???";

            return tag.Contains("One")   ? "P1" :
                   tag.Contains("Two")   ? "P2" :
                   tag.Contains("Three") ? "P3" :
                   tag.Contains("Four")  ? "P4" :
                   tag.Contains("Bot")   ? tag  : tag;
        }

        // ═══════════════════════════════════════════════
        //  BUTTONS
        // ═══════════════════════════════════════════════

        public void OnPlayAgainButtonClicked()
        {
            // Unmute SFX when returning to gameplay
            AudioManager.instance?.MuteSFX(false);
            Time.timeScale = 1f;
            onPlayAgain?.Invoke();
            GameplayManager.instance?.StartGame();
        }

        public void OnMainMenuButtonClicked()
        {
            // Unmute SFX when going to main menu (AudioManager handles muting per scene)
            AudioManager.instance?.MuteSFX(false);
            Time.timeScale = 1f;
            onReturnToMenu?.Invoke();
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}