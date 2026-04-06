using UnityEngine;
using TMPro;
using _Gameplay.Scripts;

namespace _UI.Scripts
{
    public class PlayerUIManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text      scoreText;
        [SerializeField] private RectTransform scoreTransform;

        [Header("Floating Score")]
        [SerializeField] private GameObject floatingScorePrefab;

        [Header("Position Settings")]
        [SerializeField] private Vector2 scoreOffset = new Vector2(-20, 20);

        [Header("UI Text")]
        [SerializeField] private string scoreFormat = "{0}";

        [Header("Gameplay Control")]
        [SerializeField] private bool gameplayEnabled = false;

        private Camera          playerCamera;
        private PauseController pauseController;

        private const int INITIAL_SCORE = 0;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            InitializePauseController();
            HideScoreInitially();
        }

        private void Start()
        {
            ValidateReferences();
            InitializeCamera();
            SetAnchors();
            InitializeScore();
        }

        private void OnEnable()
        {
            SubscribeToPauseEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromPauseEvents();
        }

        // ═══════════════════════════════════════════════
        //  INITIALIZATION
        // ═══════════════════════════════════════════════

        private void InitializePauseController()
        {
            pauseController = FindFirstObjectByType<PauseController>();
        }

        private void HideScoreInitially()
        {
            if (scoreText != null)
                scoreText.gameObject.SetActive(false);
        }

        private void ValidateReferences()
        {
            if (scoreText == null)
                Debug.LogError($"[{nameof(PlayerUIManager)}] Score text is not assigned on {gameObject.name}!", this);

            if (scoreTransform == null)
                Debug.LogError($"[{nameof(PlayerUIManager)}] Score transform is not assigned on {gameObject.name}!", this);
        }

        private void InitializeCamera()
        {
            playerCamera = GetComponentInChildren<Camera>();

            if (playerCamera == null)
                Debug.LogError($"[{nameof(PlayerUIManager)}] No camera found on {gameObject.name}!", this);
        }

        private void InitializeScore()
        {
            int currentScore = GetCurrentScoreFromPointsManager();
            UpdateScore(currentScore);
        }

        private int GetCurrentScoreFromPointsManager()
        {
            if (PointsManager.instance == null) return INITIAL_SCORE;
            return PointsManager.instance.GetPoints(GetPlayerTag());
        }

        // ═══════════════════════════════════════════════
        //  PAUSE
        // ═══════════════════════════════════════════════

        private void SubscribeToPauseEvents()
        {
            if (pauseController != null)
            {
                pauseController.onPaused.AddListener(OnGamePaused);
                pauseController.onUnpaused.AddListener(OnGameUnpaused);
            }
        }

        private void UnsubscribeFromPauseEvents()
        {
            if (pauseController != null)
            {
                pauseController.onPaused.RemoveListener(OnGamePaused);
                pauseController.onUnpaused.RemoveListener(OnGameUnpaused);
            }
        }

        private void OnGamePaused()   => UpdateScoreVisibility();
        private void OnGameUnpaused() => UpdateScoreVisibility();

        private void UpdateScoreVisibility()
        {
            if (scoreText == null) return;
            scoreText.gameObject.SetActive(ShouldShowScore());
        }

        private bool ShouldShowScore()
        {
            if (!gameplayEnabled) return false;
            if (pauseController != null && pauseController.GetIsPaused()) return false;
            return true;
        }

        // ═══════════════════════════════════════════════
        //  ANCHORING — same as before
        // ═══════════════════════════════════════════════

        private void SetAnchors()
        {
            if (!CanSetAnchors()) return;

            Rect    viewportRect   = playerCamera.rect;
            Vector2 anchorPosition = new Vector2(viewportRect.xMax, viewportRect.yMin);
            scoreTransform.anchorMin        = anchorPosition;
            scoreTransform.anchorMax        = anchorPosition;
            scoreTransform.anchoredPosition = scoreOffset;
        }

        private bool CanSetAnchors() => scoreTransform != null && playerCamera != null;

        // ═══════════════════════════════════════════════
        //  SCORE UPDATE
        // ═══════════════════════════════════════════════

        public void UpdateScore(int score)
        {
            if (scoreText == null) return;
            scoreText.text = string.Format(scoreFormat, score);
        }

        /// <summary>
        /// Call this when a kill is scored — updates the score AND spawns the +100 float.
        /// </summary>
        public void UpdateScoreWithFloatingText(int score, int pointsAdded)
        {
            UpdateScore(score);
            SpawnFloatingText($"+{pointsAdded}");
        }

        private void SpawnFloatingText(string text)
        {
            if (floatingScorePrefab == null || scoreTransform == null) return;

            // Spawn as a sibling of the score text so it shares the same canvas space
            GameObject obj = Instantiate(
                floatingScorePrefab,
                scoreTransform.parent);

            RectTransform rt = obj.GetComponent<RectTransform>();
            if (rt != null)
            {
                // Start at same anchor/position as score text, floats upward from there
                rt.anchorMin        = scoreTransform.anchorMin;
                rt.anchorMax        = scoreTransform.anchorMax;
                rt.anchoredPosition = scoreTransform.anchoredPosition;
            }

            FloatingScoreText floater = obj.GetComponent<FloatingScoreText>();
            floater?.Play(text);
        }

        // ═══════════════════════════════════════════════
        //  GAMEPLAY
        // ═══════════════════════════════════════════════

        public void EnableGameplay()
        {
            gameplayEnabled = true;
            UpdateScoreVisibility();
        }

        public void DisableGameplay()
        {
            gameplayEnabled = false;
            UpdateScoreVisibility();
        }

        public string GetPlayerTag() => gameObject.tag;
    }
}