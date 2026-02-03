using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace _UI.Scripts
{
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
            // Toggle selection if clicking the same button
            if (selectedButton == btn)
            {
                DeselectButton();
                return;
            }
            
            // Deselect previous button
            if (selectedButton != null)
            {
                SetButtonColor(selectedButton, normalColor);
            }
            
            // Select new button
            selectedButton = btn;
            SetButtonColor(selectedButton, highlightedColor);
            
            // Check if it's a valid map
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