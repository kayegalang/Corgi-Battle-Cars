using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using _Gameplay.Scripts;

namespace _UI.Scripts
{
    /// <summary>
    /// Map selector that checks game mode before showing join screen.
    /// For singleplayer: goes straight to game!
    /// For multiplayer: shows join screen for P2/P3/P4.
    /// </summary>
    public class MapSelector : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button[] mapButtons;
        
        [Header("Join Screen (Multiplayer Only)")]
        [SerializeField] private GameObject joinScreenPanel;
        
        [Header("Button Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightedColor = Color.gray;
        
        [Header("Settings")]
        [SerializeField] private string comingSoonButtonName = "Coming Soon";
        
        [Header("Events")]
        public UnityEvent<string> onMapSelected;
        public UnityEvent onStartGameClicked;
        
        private Button selectedButton;

        void Start()
        {
            InitializeButtons();
        }
        
        private void InitializeButtons()
        {
            startGameButton.interactable = false;
            
            foreach (Button button in mapButtons)
            {
                Button btn = button; 
                button.onClick.AddListener(() => OnMapButtonClicked(btn));
            }
        }
        
        private void OnMapButtonClicked(Button btn)
        {
            if (selectedButton == btn)
            {
                DeselectButton();
                return;
            }
            
            if (selectedButton != null)
            {
                SetButtonColor(selectedButton, normalColor);
            }
            
            selectedButton = btn;
            SetButtonColor(selectedButton, highlightedColor);
            
            bool isValidMap = selectedButton.name != comingSoonButtonName;
            startGameButton.interactable = isValidMap;
            
            if (isValidMap)
            {
                onMapSelected?.Invoke(btn.name);
            }
        }
        
        private void DeselectButton()
        {
            SetButtonColor(selectedButton, normalColor);
            selectedButton = null;
            startGameButton.interactable = false;
        }
        
        private void SetButtonColor(Button button, Color color)
        {
            if (button != null)
            {
                Image image = button.GetComponent<Image>();
                if (image != null)
                {
                    image.color = color;
                }
            }
        }
        
        public void OnStartGameButtonClicked()
        {
            Debug.Log("[MapSelector] Start Game clicked");
            
            // Check game mode
            if (GameplayManager.instance != null)
            {
                GameMode mode = GameplayManager.instance.GetCurrentGameMode();
                Debug.Log($"[MapSelector] Current game mode: {mode}");
                
                if (mode == GameMode.Singleplayer)
                {
                    // Singleplayer: Skip join screen, start game immediately!
                    Debug.Log("[MapSelector] Singleplayer mode - starting game directly");
                    onStartGameClicked?.Invoke();
                }
                else
                {
                    // Multiplayer: Show join screen
                    Debug.Log("[MapSelector] Multiplayer mode - showing join screen");
                    
                    if (joinScreenPanel != null)
                    {
                        gameObject.SetActive(false);
                        joinScreenPanel.SetActive(true);
                    }
                    else
                    {
                        Debug.LogWarning("[MapSelector] Join screen panel not assigned!");
                    }
                }
            }
        }
        
        private void ResetSelection()
        {
            if (selectedButton != null)
            {
                DeselectButton();
            }
        }

        public void GoBack()
        {
            ResetSelection();
        }
    }
}