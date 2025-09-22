using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Scripts
{
    public class MainMenuController : MonoBehaviour
    {
        public void OnSingleplayerButtonClick()
        {
            SceneManager.LoadScene("Singleplayer");
        }

        public void OnMultiplayerButtonClick()
        {
            SceneManager.LoadScene("Multiplayer");
        }

        public void OnQuitButtonClick()
        {
            #if UNITY_EDITOR 
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        
    }
}

