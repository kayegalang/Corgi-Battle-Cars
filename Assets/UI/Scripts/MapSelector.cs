using Gameplay.Scripts;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.Scripts
{
    
    public class MapSelector : MonoBehaviour
    {
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button[] mapButtons;
        
        private Color normalColor = Color.white;
        private Color highlightedColor = Color.gray;
        
        private Button selectedButton;

        void Start()
        {
            startGameButton.interactable = false;
            foreach (Button button in mapButtons)
            {
                Button btn = button;
                button.onClick.AddListener(()=> OnButtonClicked(btn));
            }
        }
        
        private void OnButtonClicked(Button btn)
        {
            if (selectedButton == btn)
            {
                selectedButton.GetComponent<Image>().color = normalColor;
                selectedButton = null;
                startGameButton.interactable = false;
                return;
            }
            
            if (selectedButton != null)
            {
                selectedButton.GetComponent<Image>().color = normalColor;
            }
            
            selectedButton = btn;
            selectedButton.GetComponent<Image>().color = highlightedColor;
            
            if (selectedButton.name != "Coming Soon")
            {
                GameplayManager.instance.SetMap(btn.name);
                startGameButton.interactable = true;
            }
            else
            {
                startGameButton.interactable = false;
            }
        }
        
        public void OnStartGameButtonClicked()
        {
            GameplayManager.instance.StartGame();
        }
    }
}
