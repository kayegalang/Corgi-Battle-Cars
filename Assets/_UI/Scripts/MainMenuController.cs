using Gameplay.Scripts;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Scripts
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

