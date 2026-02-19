using _Gameplay.Scripts;
using UnityEngine;

namespace _UI.Scripts
{
    /// <summary>
    /// Simple main menu - singleplayer skips join screen entirely!
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject mapSelectionPanel;
        [SerializeField] private GameObject joinScreenPanel;
        [SerializeField] private PlayerCountSelector playerCountSelector;
        
        public void OnQuitButtonClick()
        {
            #if UNITY_EDITOR 
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        public void OnSingleplayerButtonClicked()
        {
            Debug.Log("[MainMenuController] Singleplayer selected - skipping join screen!");
            
            // Set game mode
            if (GameplayManager.instance != null)
            {
                GameplayManager.instance.SetGameMode(GameMode.Singleplayer);
                GameplayManager.instance.SetMultiplayerPlayerCount(1);
            }
            
            // Go straight to map selection - NO JOIN SCREEN!
            ShowMapSelection();
        }
        
        public void OnMultiplayerButtonClicked()
        {
            Debug.Log("[MainMenuController] Multiplayer selected - showing join screen");
            
            // Set game mode
            if (GameplayManager.instance != null)
            {
                GameplayManager.instance.SetGameMode(GameMode.Multiplayer);
            }
            
            // Show player count selector (which leads to join screen)
            if (playerCountSelector != null)
            {
                mainMenuPanel.SetActive(false);
                playerCountSelector.gameObject.SetActive(true);
            }
        }
        
        private void ShowMapSelection()
        {
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(false);
            
            if (mapSelectionPanel != null)
                mapSelectionPanel.SetActive(true);
        }
    }
}