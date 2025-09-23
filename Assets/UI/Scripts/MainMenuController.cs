using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Scripts
{
    public class MainMenuController : MonoBehaviour
    {
        public void OnSingleplayerButtonClick()
        {
            SceneManager.LoadScene("Prototype Map");
        }

        public void OnMultiplayerButtonClick()
        {
            SceneManager.LoadScene("Prototype Map");
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

