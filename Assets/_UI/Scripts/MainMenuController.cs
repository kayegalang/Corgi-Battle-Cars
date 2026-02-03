using _Gameplay.Scripts;
using UnityEngine;

namespace _UI.Scripts
{
    public class MainMenuController : MonoBehaviour
    {
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
            GameplayManager.instance.SetGameMode(GameMode.Singleplayer);
            playerCountSelector.SelectPlayerCount(1);
        }
        
        public void OnMultiplayerButtonClicked()
        {
            GameplayManager.instance.SetGameMode(GameMode.Multiplayer);
        }
        
    }
}

