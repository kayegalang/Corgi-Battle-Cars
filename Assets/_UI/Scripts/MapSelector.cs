using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace _UI.Scripts
{
    /// <summary>
    /// FIXED VERSION - Always starts game when clicking Start Game button.
    /// Map selector comes AFTER join screen in multiplayer, so just start game!
    /// </summary>
    public class MapSelector : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button[] mapButtons;
        
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
            Debug.Log("[MapSelector] Start Game clicked - loading map!");
            
            // Just start the game! 
            // For singleplayer: Goes straight to game
            // For multiplayer: Players already joined, just start game
            onStartGameClicked?.Invoke();
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