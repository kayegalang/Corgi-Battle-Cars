using _Gameplay.Scripts;
using UnityEngine;

namespace _UI.Scripts
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject characterSelectionPanel;
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
            Debug.Log("[MainMenuController] Singleplayer selected - going to character selection!");
            
            // Set game mode and player count
            if (GameplayManager.instance != null)
            {
                GameplayManager.instance.SetGameMode(GameMode.Singleplayer);
                GameplayManager.instance.SetMultiplayerPlayerCount(1);
            }
            
            // Show character selection directly
            if (characterSelectionPanel != null)
            {
                mainMenuPanel.SetActive(false);
                characterSelectionPanel.SetActive(true);
            }
            else
            {
                Debug.LogError("[MainMenuController] Character Selection Panel not assigned!");
            }
        }
        
        public void OnMultiplayerButtonClicked()
        {
            Debug.Log("[MainMenuController] Multiplayer selected - going to player count selection!");
            
            if (GameplayManager.instance != null)
            {
                GameplayManager.instance.SetGameMode(GameMode.Multiplayer);
            }
        }
    }
}