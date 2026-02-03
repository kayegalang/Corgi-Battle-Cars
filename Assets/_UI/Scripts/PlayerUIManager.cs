using UnityEngine;
using TMPro;
using _Gameplay.Scripts;

namespace _UI.Scripts
{
    public class PlayerUIManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private RectTransform scoreTransform;
        
        [Header("Position Settings")]
        [SerializeField] private Vector2 scoreOffset = new Vector2(-20, 20);
        
        [Header("UI Text")]
        [SerializeField] private string scoreFormat = "Score: {0}";
        
        [Header("Gameplay Control")]
        [SerializeField] private bool gameplayEnabled = false;
        
        private Camera playerCamera;
        private PauseController pauseController;
        
        private const int INITIAL_SCORE = 0;
        
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
        
        private void InitializePauseController()
        {
            pauseController = FindFirstObjectByType<PauseController>();
        }
        
        private void HideScoreInitially()
        {
            if (scoreText != null)
            {
                scoreText.gameObject.SetActive(false);
            }
        }
        
        private void ValidateReferences()
        {
            if (scoreText == null)
            {
                Debug.LogError($"[{nameof(PlayerUIManager)}] Score text is not assigned on {gameObject.name}!", this);
            }
            
            if (scoreTransform == null)
            {
                Debug.LogError($"[{nameof(PlayerUIManager)}] Score transform is not assigned on {gameObject.name}!", this);
            }
        }
        
        private void InitializeCamera()
        {
            playerCamera = GetComponentInChildren<Camera>();
            
            if (playerCamera == null)
            {
                Debug.LogError($"[{nameof(PlayerUIManager)}] No camera found on {gameObject.name}!", this);
            }
        }
        
        private void InitializeScore()
        {
            int currentScore = GetCurrentScoreFromPointsManager();
            UpdateScore(currentScore);
        }
        
        private int GetCurrentScoreFromPointsManager()
        {
            if (PointsManager.instance == null)
            {
                return INITIAL_SCORE;
            }
            
            string playerTag = GetPlayerTag();
            return PointsManager.instance.GetPoints(playerTag);
        }
        
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
        
        private void OnGamePaused()
        {
            UpdateScoreVisibility();
        }
        
        private void OnGameUnpaused()
        {
            UpdateScoreVisibility();
        }
        
        private void UpdateScoreVisibility()
        {
            if (scoreText == null)
            {
                return;
            }
            
            bool shouldShow = ShouldShowScore();
            scoreText.gameObject.SetActive(shouldShow);
        }
        
        private bool ShouldShowScore()
        {
            if (!gameplayEnabled)
            {
                return false;
            }
            
            if (pauseController != null && pauseController.GetIsPaused())
            {
                return false;
            }
            
            return true;
        }
        
        private void SetAnchors()
        {
            if (!CanSetAnchors())
            {
                return;
            }
            
            Rect viewportRect = playerCamera.rect;
            
            Vector2 anchorPosition = new Vector2(viewportRect.xMax, viewportRect.yMin);
            scoreTransform.anchorMin = anchorPosition;
            scoreTransform.anchorMax = anchorPosition;
            scoreTransform.anchoredPosition = scoreOffset;
        }
        
        private bool CanSetAnchors()
        {
            return scoreTransform != null && playerCamera != null;
        }
        
        public void UpdateScore(int score)
        {
            if (scoreText == null)
            {
                return;
            }
            
            scoreText.text = string.Format(scoreFormat, score);
        }
        
        public string GetPlayerTag()
        {
            return gameObject.tag;
        }
        
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
    }
}