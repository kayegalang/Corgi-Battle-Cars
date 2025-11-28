using UnityEngine;
using TMPro;
using UI.Scripts;

namespace _UI.Scripts
{
    /// <summary>
    /// Manages per-player UI (score display)
    /// Positions UI elements in the player's camera viewport
    /// </summary>
    public class PlayerUIManager : MonoBehaviour
    {
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private RectTransform scoreTransform;
        [SerializeField] private Canvas canvas;
        
        [Header("Gameplay Control")]
        [SerializeField] private bool gameplayEnabled = false; // Toggle this to enable/disable gameplay
        
        private Camera playerCamera;
        
        private PauseController pauseController;


        void Awake()
        {
            pauseController =  FindFirstObjectByType<PauseController>();
        }
        void Start()
        {
            // Get this player's camera
            playerCamera = GetComponentInChildren<Camera>();
            
            if (playerCamera == null)
            {
                Debug.LogError($"{gameObject.name}: No camera found!");
                return;
            }
            
            // Set anchors to bottom-right of this player's viewport
            SetAnchors();
            
            // Initialize score
            if (scoreText != null)
            {
                scoreText.text = "Score: 0";
            }
        }

        void Update()
        {
            // If gameplay not enabled, keep reticle hidden
            if (!gameplayEnabled)
            {
                if (scoreText != null)
                    scoreText.gameObject.SetActive(false);
            }

            if (gameplayEnabled)
            {
                if (pauseController != null && pauseController.GetIsPaused())
                {
                    scoreText.gameObject.SetActive(false);
                }
                else
                {
                    scoreText.gameObject.SetActive(true);
                }
            }
        }

        private void SetAnchors()
        {
            if (scoreTransform == null) return;
            
            // Get this player's camera viewport (normalized 0-1)
            Rect viewportRect = playerCamera.rect;
            
            // Set anchors to bottom-right of this player's viewport
            scoreTransform.anchorMin = new Vector2(viewportRect.xMax, viewportRect.yMin);
            scoreTransform.anchorMax = new Vector2(viewportRect.xMax, viewportRect.yMin);
            
            // Position relative to anchor (offset from bottom-right)
            scoreTransform.anchoredPosition = new Vector2(-20, 20);
        }
        
        public void UpdateScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score: {score}";
            }
        }
        
        public string GetPlayerTag()
        {
            return gameObject.tag;
        }
        
        public void EnableGameplay()
        {
            gameplayEnabled = true;
            scoreText.gameObject.SetActive(true);
        }
        
        public void DisableGameplay()
        {
            gameplayEnabled = false;
        }
    }
}