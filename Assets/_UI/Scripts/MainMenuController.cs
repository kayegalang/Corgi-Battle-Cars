using _Gameplay.Scripts;
using Gameplay.Scripts;
using UnityEngine;

namespace _UI.Scripts
{
    public class MainMenuController : MonoBehaviour
    {
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
        }
        
    }
}

